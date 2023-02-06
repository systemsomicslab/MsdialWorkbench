using CompMs.Common.DataObj;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msdial.Lcms.Dataprocess.Algorithm {
    public class PeakCharacterEvaluator {
        private const float rtMargin = 0.0177F;
        private const double initialProgress = 80.0;
        private const double progressMax = 20.0;

        private List<Rfx.Riken.OsakaUniv.AdductIon> searchedAdducts;

        public void Run(AnalysisFileBean file, 
            ObservableCollection<RawSpectrum> spectrumCollection,
            List<PeakAreaBean> peakSpots, List<MspFormatCompoundInformationBean> mspDB,
            List<PostIdentificatioinReferenceBean> postIdentDB,
            AnalysisParametersBean param, ProjectPropertyBean projectProp, Action<int> reportAction) {

            // some adduct features are automatically insearted even if users did not select any type of adduct
            searchedAdductInitialize(param, projectProp);

            // collecting the same RT region spots
            var counter = 0;
            var lastGroupID = 0;

            var seekpointList = new List<long>();

            using (var fs = File.Open(file.AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite)) {
                seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                peakSpots = peakSpots.OrderBy(n => n.PeakID).ToList();
                initializations(peakSpots);
                isotopeDataInitialization(peakSpots);

                peakSpots = peakSpots.OrderBy(n => n.AccurateMass).ToList();
                foreach (var peak in peakSpots) {
                    counter++;

                    var peakRt = peak.RtAtPeakTop;
                    var peakMz = peak.AccurateMass;
                    var startScanIndex = DataAccessLcUtility.GetScanStartIndexByMz(peakMz - param.CentroidMs1Tolerance, peakSpots);
                    var searchedPeakSpots = new List<PeakAreaBean>() { peak };

                    for (int i = startScanIndex; i < peakSpots.Count; i++) {
                        if (peakSpots[i].RtAtPeakTop < peakRt - rtMargin) continue;
                        if (peakSpots[i].RtAtPeakTop > peakRt + rtMargin) continue;
                        if (peak.PeakID == peakSpots[i].PeakID) continue;
                        searchedPeakSpots.Add(peakSpots[i]);
                    }

                    characterAssigner(fs, seekpointList, searchedPeakSpots, spectrumCollection, mspDB, postIdentDB, param, projectProp, ref lastGroupID);
                    progressReports(counter, peakSpots.Count, reportAction);
                }
            }

            peakSpots = peakSpots.OrderBy(n => n.PeakID).ToList();
            finalizationForAdduct(file.AnalysisFilePropertyBean.AnalysisFileId, peakSpots, projectProp.IonMode, param.IsIonMobility, param.IonMobilityType, param);
            assignPutativePeakgroupIDs(peakSpots);
        }

        private void initializations(List<PeakAreaBean> peakSpots) {
            if (peakSpots == null || peakSpots.Count == 0) return;
            if (peakSpots[0].PeakGroupID >= 0) {
                foreach (var spot in peakSpots) {
                    spot.PeakGroupID = -1;
                    spot.IsLinked = false;
                    spot.PeakLinks = new List<LinkedPeakFeature>();
                }
            }
        }

        private void isotopeDataInitialization(List<PeakAreaBean> peakSpots) {
            foreach (var peak in peakSpots) {

                if (peak.IsotopeWeightNumber != 0) {
                    var parentID = peak.IsotopeParentPeakID;
                    if (peak.PeakLinks.Count(n => n.LinkedPeakID == parentID &&
                        n.Character == PeakLinkFeatureEnum.Isotope) == 0) {

                        peak.PeakLinks.Add(new LinkedPeakFeature() {
                            LinkedPeakID = parentID,
                            Character = PeakLinkFeatureEnum.Isotope
                        });
                        peak.IsLinked = true;

                        if (peakSpots[parentID].PeakLinks.Count(n => n.LinkedPeakID == peak.PeakID &&
                        n.Character == PeakLinkFeatureEnum.Isotope) == 0) {
                            peakSpots[parentID].PeakLinks.Add(new LinkedPeakFeature() {
                                LinkedPeakID = peak.PeakID,
                                Character = PeakLinkFeatureEnum.Isotope
                            });
                        }
                    }
                }
            }
        }

        // currently, the links for same metabolite, isotope, and adduct are grouped.
        // the others such as found in upper msms and chromatogram correlation are not grouped.
        // in future, I have to create the merge GUI for user side
        private void assignPutativePeakgroupIDs(List<PeakAreaBean> peakSpots) {
            var groupID = 0;
            foreach (var peak in peakSpots) {
                if (peak.PeakGroupID >= 0) continue;
                if (peak.PeakLinks.Count == 0) {
                    peak.PeakGroupID = groupID;
                } else {
                    var crawledPeaks = new List<int>();
                    peak.PeakGroupID = groupID;
                    recPeakGroupAssignment(peak, peakSpots, groupID, crawledPeaks);
                }
                groupID++;
            }
        }

        private void recPeakGroupAssignment(PeakAreaBean peak, List<PeakAreaBean> peakSpots, int groupID, List<int> crawledPeaks) {
            if (peak.PeakLinks == null || peak.PeakLinks.Count == 0) return;
            foreach (var linkedPeak in peak.PeakLinks) {
                var linkedPeakID = linkedPeak.LinkedPeakID;
                var character = linkedPeak.Character;
                if (crawledPeaks.Contains(linkedPeakID)) continue;

                peakSpots[linkedPeakID].PeakGroupID = groupID;
                crawledPeaks.Add(linkedPeakID);

                if (isCrawledPeaks(peakSpots[linkedPeakID].PeakLinks, crawledPeaks, peak.PeakID)) continue;
                recPeakGroupAssignment(peakSpots[linkedPeakID], peakSpots, groupID, crawledPeaks);
            }
        }

        private bool isCrawledPeaks(List<LinkedPeakFeature> peakLinks, List<int> crawledPeaks, int peakID) {
            if (peakLinks.Count(n => n.LinkedPeakID != peakID) == 0) return true;
            var frag = false;
            foreach (var linkID in peakLinks.Select(n => n.LinkedPeakID)) {
                if (crawledPeaks.Contains(linkID)) continue;
                frag = true;
                break;
            }
            if (frag == true) return false;
            else return true;
        }

        private void finalizationForAdduct(int fileid, List<PeakAreaBean> peakSpots, IonMode ionMode, bool isIonMobility, IonMobilityType mobilitytype, AnalysisParametersBean param) {
            var defaultAdduct = searchedAdducts[0];
            var defaultAdduct2 = AdductIonParcer.ConvertDifferentChargedAdduct(defaultAdduct, 2);

            //AdductIonParcer.GetAdductIonBean("[M+H]+")

            foreach (var peak in peakSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                //if (peak.AdductParent >= 0 && peak.AdductIonName != string.Empty)
                //    continue;
                if (peak.AdductParent < 0)
                    peak.AdductParent = peak.PeakID;
                if (peak.AdductIonName != null && peak.AdductIonName != string.Empty)
                    continue;

                if (peak.ChargeNumber >= 2) {

                    var adductString = string.Empty;
                    if (ionMode == IonMode.Positive) {
                        adductString = "[M+" + peak.ChargeNumber + "H]" + peak.ChargeNumber + "+";
                    }
                    else {
                        adductString = "[M-" + peak.ChargeNumber + "H]" + peak.ChargeNumber + "-";
                    }
                    var estimatedAdduct = AdductIonParcer.GetAdductIonBean(adductString);

                    peak.AdductIonName = estimatedAdduct.AdductIonName;
                    peak.AdductIonXmer = estimatedAdduct.AdductIonXmer;
                    peak.AdductIonChargeNumber = estimatedAdduct.ChargeNumber;
                    peak.AdductIonAccurateMass = (float)estimatedAdduct.AdductIonAccurateMass;
                }
                else {
                    peak.AdductIonName = defaultAdduct.AdductIonName;
                    peak.AdductIonXmer = defaultAdduct.AdductIonXmer;
                    peak.AdductIonChargeNumber = defaultAdduct.ChargeNumber;
                    peak.AdductIonAccurateMass = (float)defaultAdduct.AdductIonAccurateMass;
                }
            }

            foreach (var peak in peakSpots) {
                if (peak.IsotopeParentPeakID >= 0 && peak.IsotopeWeightNumber > 0) {

                    var parentPeak = peakSpots[peak.IsotopeParentPeakID];

                    peak.AdductParent = parentPeak.AdductParent;
                    peak.AdductIonName = parentPeak.AdductIonName;
                    peak.AdductIonXmer = parentPeak.AdductIonXmer;
                    peak.AdductIonChargeNumber = parentPeak.ChargeNumber;
                    peak.AdductIonAccurateMass = (float)parentPeak.AdductIonAccurateMass;
                }
            }

            //refine the dependency
            foreach (var peak in peakSpots) {
                if (peak.PeakID == peak.AdductParent) continue;
                var parentID = peak.AdductParent;

                if (peakSpots[parentID].AdductParent != peakSpots[parentID].PeakID) {
                    var parentParentID = peakSpots[parentID].AdductParent;
                    if (peakSpots[parentParentID].AdductParent == peakSpots[parentParentID].PeakID)
                        peak.AdductParent = peakSpots[parentParentID].PeakID;
                    else {
                        var parentParentParentID = peakSpots[parentParentID].AdductParent;
                        if (peakSpots[parentParentParentID].AdductParent == peakSpots[parentParentParentID].PeakID)
                            peak.AdductParent = peakSpots[parentParentParentID].PeakID;
                    }
                }
            }

            // currently, just copy the isotope and adduct of parent spot to drift spots
            if (isIonMobility) {
                var calinfo = param.FileidToCcsCalibrantData[fileid];
                foreach (var peak in peakSpots) {
                    foreach (var drift in peak.DriftSpots) {
                        drift.ChargeNumber = peak.ChargeNumber;
                        if (drift.IsotopeWeightNumber < 0)
                            drift.IsotopeWeightNumber = peak.IsotopeWeightNumber;
                        if (drift.AdductIonName == null || drift.AdductIonName == string.Empty) {
                            drift.AdductIonAccurateMass = peak.AdductIonAccurateMass;
                            drift.AdductIonChargeNumber = peak.AdductIonChargeNumber;
                            drift.AdductIonName = peak.AdductIonName;
                            drift.AdductIonXmer = peak.AdductIonXmer;
                        }

                        //var adduct = AdductIonParcer.GetAdductIonBean(drift.AdductIonName);
                        //var exactmass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(adduct, drift.AccurateMass);
                        drift.Ccs = (float)IonMobilityUtility.MobilityToCrossSection(mobilitytype,
                            drift.DriftTimeAtPeakTop, Math.Abs(drift.ChargeNumber), drift.AccurateMass, calinfo, param.IsAllCalibrantDataImported);
                    }
                }
            }
        }

        private void progressReports(int currentProgress, int maxProgress, Action<int> reportAction) {
            var progress = initialProgress + (double)currentProgress / (double)maxProgress * progressMax;
            reportAction?.Invoke((int)progress);
        }

        // the RT deviations of peakspots should be less than 0.03 min
        // here, each peak is evaluated.
        // the purpose is to group the ions which are recognized as the same metabolite
        private void characterAssigner(FileStream fs, List<long> seekPoints,
            List<PeakAreaBean> peakSpots, 
            ObservableCollection<RawSpectrum> spectrumCollection, List<MspFormatCompoundInformationBean> mspDB,
            List<PostIdentificatioinReferenceBean> postIdentDB,
            AnalysisParametersBean param, ProjectPropertyBean projectProp, ref int lastGroupID) {
            if (peakSpots == null || peakSpots.Count == 0) return;

            // if the first inchikey is same, it's recognized as the same metabolite.
            assignLinksBasedOnInChIKeys(peakSpots, mspDB, postIdentDB);

            // The identified compound is used for searching.
            assignLinksBasedOnIdentifiedCompound(peakSpots, mspDB, postIdentDB, projectProp, param);

            if (projectProp.IonMode == IonMode.Negative)
                assignAdductByMsMs(fs, seekPoints, peakSpots, param);

            // The identified adduct is used for searching
            assignLinksBasedOnDeterminedAdduct(peakSpots, mspDB, postIdentDB, projectProp, param);

            // adduct pairing method
            assignLinksBasedOnAdductPairingMethod(peakSpots, projectProp, param);

            // linkage by chromatogram correlations
            assignLinksBasedOnChromatogramCorrelation(peakSpots, spectrumCollection, param, projectProp);

            // linked by partial matching of MS1 and MS2
            if (projectProp.CheckAIF == true || projectProp.MethodType == MethodType.diMSMS) return;
            assignLinksBasedOnPartialMatchingOfMS1MS2(fs, seekPoints, peakSpots, spectrumCollection, param, projectProp);
        }

        private void assignAdductByMsMs(FileStream fs, List<long> seekPoints, List<PeakAreaBean> peakSpots, AnalysisParametersBean param) {

            var isAcetateAdduct = false;
            var isFormateAdduct = false;

            Rfx.Riken.OsakaUniv.AdductIon acetateAdduct = null;
            Rfx.Riken.OsakaUniv.AdductIon formateAdduct = null;

            foreach (var adduct in searchedAdducts) {
                if (Math.Abs(adduct.AdductIonAccurateMass - 44.998201) < 0.01) {
                    isFormateAdduct = true;
                    formateAdduct = adduct;
                    break;
                }
                if (Math.Abs(adduct.AdductIonAccurateMass - 59.013851) < 0.01) {
                    isAcetateAdduct = true;
                    acetateAdduct = adduct;
                    break;
                }
            }

            if (isAcetateAdduct == false && isFormateAdduct == false) return;

            foreach (var peak in peakSpots) {
                if (peak.AdductIonName != null && peak.AdductIonName != string.Empty) continue;
                if (peak.Ms2LevelDatapointNumber < 0) continue;
                if (peak.IsotopeWeightNumber != 0) continue;

                var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekPoints, peak.PeakID);
                if (ms2Dec.MassSpectra.Count == 0) continue;

                var precursorMz = peak.AccurateMass;
                var precursorIntensity = 1.0;
                var maxIntensity = ms2Dec.MassSpectra.Max(n => n[1]);
                var massSpec = ms2Dec.MassSpectra.OrderByDescending(n => n[0]).ToList();

                foreach (var spec in massSpec) {

                    var neutralloss = precursorMz - spec[0];
                    var relativeIntensity = spec[1] / maxIntensity * 100;
                    if (neutralloss < param.CentroidMs2Tolerance) {
                        precursorIntensity = relativeIntensity;
                    }

                    if (isFormateAdduct && Math.Abs(neutralloss - 46.005477) < param.CentroidMs2Tolerance && relativeIntensity > 80.0 && precursorIntensity < 40.0) {
                        peak.AdductParent = peak.PeakID;
                        peak.AdductIonName = formateAdduct.AdductIonName;
                        peak.AdductIonChargeNumber = formateAdduct.ChargeNumber;
                        peak.AdductIonXmer = formateAdduct.AdductIonXmer;
                        peak.AdductIonAccurateMass = (float)formateAdduct.AdductIonAccurateMass;
                        break;
                    }

                    if (isAcetateAdduct && Math.Abs(neutralloss - 60.021127) < param.CentroidMs2Tolerance && relativeIntensity > 80.0 && precursorIntensity < 40.0) {
                        peak.AdductParent = peak.PeakID;
                        peak.AdductIonName = acetateAdduct.AdductIonName;
                        peak.AdductIonChargeNumber = acetateAdduct.ChargeNumber;
                        peak.AdductIonXmer = acetateAdduct.AdductIonXmer;
                        peak.AdductIonAccurateMass = (float)acetateAdduct.AdductIonAccurateMass;
                        break;
                    }
                }
            }
        }

        // currently, the method is very simple.
        // if a peak (at least 10% relative abundance) in MS/MS is found in MS1 spectrum,
        // the peak of MS1 is assigned as "Found in upper MSMS"
        private void assignLinksBasedOnPartialMatchingOfMS1MS2(FileStream fs, List<long> seekPoints, 
            List<PeakAreaBean> peakSpots, ObservableCollection<RawSpectrum> spectrumCollection, 
            AnalysisParametersBean param, ProjectPropertyBean projectProp) {
            
            for (int i = peakSpots.Count - 1; i >= 0; i--) {
                var peak = peakSpots[i];
                if (peak.Ms2LevelDatapointNumber < 0) continue;
                if (peak.IsotopeWeightNumber != 0) continue;

                var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekPoints, peak.PeakID);
                if (ms2Dec.MassSpectra.Count == 0) continue;
                var maxIntensity = ms2Dec.MassSpectra.Max(n => n[1]);

                for (int j = i - 1; j >= 0; j--) {
                    var cPeak = peakSpots[j];
                    if (cPeak.IsotopeWeightNumber != 0) continue;
                    var peakMz = cPeak.AccurateMass;
                    foreach (var specPeak in ms2Dec.MassSpectra) {
                        var mz = specPeak[0];
                        var revInt = specPeak[1] / maxIntensity;
                        if (revInt < 0.1) continue;

                        if (Math.Abs(peakMz - mz) < param.CentroidMs2Tolerance) {

                            //if (cPeak.PeakID == peak.PeakID) {
                            //    //Console.WriteLine();
                            //}

                            cPeak.IsLinked = true;
                            peak.IsLinked = true;

                            registerLinks(peak, cPeak, PeakLinkFeatureEnum.FoundInUpperMsMs);
                        }
                    }
                }
            }
        }

        // currently, only pure peaks are evaluated by this way.
        private void assignLinksBasedOnChromatogramCorrelation(List<PeakAreaBean> peakSpots, 
            ObservableCollection<RawSpectrum> spectrumCollection, AnalysisParametersBean param, ProjectPropertyBean projectProp) {
            foreach (var peak in peakSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                if (peak.PeakPureValue < 0.9) continue;
                if (peak.IsotopeWeightNumber != 0) continue;
                var tTopRt = peak.RtAtPeakTop;
                var tLeftRt = peak.RtAtLeftPeakEdge;
                var tRightRt = peak.RtAtRightPeakEdge;
                var tChrom = DataAccessLcUtility.GetSmoothedPeaklist(DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProp,
                    peak.AccurateMass, param.CentroidMs1Tolerance, tLeftRt, tRightRt), param.SmoothingMethod, param.SmoothingLevel);

                foreach (var cPeak in peakSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                    if (cPeak.IsLinked) continue;
                    if (peak.PeakID == cPeak.PeakID) continue;
                    if (cPeak.PeakPureValue < 0.9) continue;

                    var cChrom = DataAccessLcUtility.GetSmoothedPeaklist(DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProp,
                    cPeak.AccurateMass, param.CentroidMs1Tolerance, tLeftRt, tRightRt), param.SmoothingMethod, param.SmoothingLevel);

                    //Debug.WriteLine("tChrom count {0}, cChrom count {1}", tChrom.Count, cChrom.Count);

                    double scaT = 0.0, scaC = 0.0, cov = 0.0;
                    for (int i = 0; i < tChrom.Count; i++) {
                        if (cChrom.Count - 1 < i) break;
                        scaT += Math.Pow(tChrom[i][3], 2);
                        scaC += Math.Pow(cChrom[i][3], 2);
                        cov += tChrom[i][3] * cChrom[i][3];
                    }
                    if (scaT == 0 || scaC == 0) continue;
                    var col = cov / Math.Sqrt(scaT) / Math.Sqrt(scaC);

                    if (col > 0.95) {
                        cPeak.IsLinked = true;
                        peak.IsLinked = true;

                        registerLinks(peak, cPeak, PeakLinkFeatureEnum.ChromSimilar);
                    }
                }
            }
        }

        // just copied from the previous adduct estimator, should be checked for the improvement
        private void assignLinksBasedOnAdductPairingMethod(List<PeakAreaBean> peakSpots, ProjectPropertyBean projectProp, AnalysisParametersBean param) {
            foreach (var peak in peakSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                if (peak.IsLinked) continue;
                var adduct = peak.AdductIonName;
                if (adduct != null && adduct != string.Empty) continue;
                var flg = false;
                var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.AdductAndIsotopeMassTolerance); //based on m/z 200

                foreach (var centralAdduct in searchedAdducts) {

                    var rCentralAdduct = AdductIonParcer.ConvertDifferentChargedAdduct(centralAdduct, peak.ChargeNumber);

                    var centralExactMass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(peak.AccurateMass, rCentralAdduct.AdductIonAccurateMass,
                        rCentralAdduct.ChargeNumber, rCentralAdduct.AdductIonXmer, projectProp.IonMode);

                    var searchedPrecursors = new List<SearchedPrecursor>();
                    foreach (var searchedAdduct in searchedAdducts) {
                        if (rCentralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                        var searchedPrecursorMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(searchedAdduct, centralExactMass);
                        searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
                    }

                    foreach (var searchedPeak in peakSpots) {
                        if (searchedPeak.IsLinked) continue;
                        if (peak.PeakID == searchedPeak.PeakID) continue;
                        if (searchedPeak.AdductIonName != null && searchedPeak.AdductIonName != string.Empty) continue;

                        foreach (var searchedPrecursor in searchedPrecursors) {

                            var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.AccurateMass, ppm);

                            if (Math.Abs(searchedPeak.AccurateMass - searchedPrecursor.PrecursorMz) < adductTol) {

                                var searchedAdduct = searchedPrecursor.AdductIon;

                                //if (searchedPeak.MetaboliteName.Contains("PC ")) {
                                //    Console.WriteLine();
                                //}

                                searchedPeak.AdductParent = peak.PeakID;
                                searchedPeak.AdductIonName = searchedAdduct.AdductIonName;
                                searchedPeak.AdductIonChargeNumber = searchedAdduct.ChargeNumber;
                                searchedPeak.AdductIonXmer = searchedAdduct.AdductIonXmer;
                                searchedPeak.AdductIonAccurateMass = (float)searchedAdduct.AdductIonAccurateMass;
                                searchedPeak.IsLinked = true;

                                registerLinks(peak, searchedPeak, PeakLinkFeatureEnum.Adduct);
                                flg = true;
                                break;
                            }
                        }
                    }

                    if (flg) {

                        peak.AdductParent = peak.PeakID;
                        peak.AdductIonName = rCentralAdduct.AdductIonName;
                        peak.AdductIonXmer = rCentralAdduct.AdductIonXmer;
                        peak.AdductIonChargeNumber = rCentralAdduct.ChargeNumber;
                        peak.AdductIonAccurateMass = (float)rCentralAdduct.AdductIonAccurateMass;
                        peak.IsLinked = true;
                        break;
                    }
                }
            }
        }

        private void assignLinksBasedOnDeterminedAdduct(List<PeakAreaBean> peakSpots, 
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> postIdentDB, 
            ProjectPropertyBean projectProp, AnalysisParametersBean param) {

            foreach (var peak in peakSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                var adduct = peak.AdductIonName;
                if (adduct == null || adduct == string.Empty) continue;
                var centralAdduct = AdductIonParcer.GetAdductIonBean(adduct);
                var centralExactMass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(peak.AccurateMass,
                    centralAdduct.AdductIonAccurateMass,
                    centralAdduct.ChargeNumber, centralAdduct.AdductIonXmer, projectProp.IonMode);
                var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200

                var searchedPrecursors = new List<SearchedPrecursor>();
                foreach (var searchedAdduct in searchedAdducts) {
                    if (centralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                    var searchedPrecursorMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(searchedAdduct, centralExactMass);
                    searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
                }

                foreach (var searchedPeak in peakSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                    if (searchedPeak.IsLinked) continue;
                    if (peak.PeakID == searchedPeak.PeakID) continue;

                    foreach (var searchedPrecursor in searchedPrecursors) {

                        var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.AccurateMass, ppm);

                        if ((searchedPeak.LibraryID >= 0 || searchedPeak.PostIdentificationLibraryId >= 0) &&
                              !searchedPeak.MetaboliteName.Contains("w/o")) {
                            continue;
                        }

                        if (Math.Abs(searchedPeak.AccurateMass - searchedPrecursor.PrecursorMz) < adductTol) {
                            var searchedAdduct = searchedPrecursor.AdductIon;

                            searchedPeak.AdductParent = peak.PeakID;
                            searchedPeak.AdductIonName = searchedAdduct.AdductIonName;
                            searchedPeak.AdductIonChargeNumber = searchedAdduct.ChargeNumber;
                            searchedPeak.AdductIonXmer = searchedAdduct.AdductIonXmer;
                            searchedPeak.AdductIonAccurateMass = (float)searchedAdduct.AdductIonAccurateMass;
                            searchedPeak.IsLinked = true;

                            peak.AdductParent = peak.PeakID;
                            registerLinks(peak, searchedPeak, PeakLinkFeatureEnum.Adduct);

                            break;
                        }
                    }
                }
            }
        }

        private void assignLinksBasedOnIdentifiedCompound(List<PeakAreaBean> peakSpots, List<MspFormatCompoundInformationBean> mspDB, 
            List<PostIdentificatioinReferenceBean> postIdentDB, ProjectPropertyBean projectProp,
            AnalysisParametersBean param) {
            foreach (var peak in peakSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                if (peak.LibraryID < 0 && peak.PostIdentificationLibraryId < 0) continue;
                if (peak.MetaboliteName.Contains("w/o")) continue;

         
                //adduct null check
                if (peak.LibraryID >= 0 && mspDB[peak.LibraryID].AdductIonBean == null) continue;
                if (peak.PostIdentificationLibraryId >= 0 && postIdentDB[peak.PostIdentificationLibraryId].AdductIon == null) continue;

                var adduct = peak.LibraryID >= 0
                    ? mspDB[peak.LibraryID].AdductIonBean.AdductIonName
                    : postIdentDB[peak.PostIdentificationLibraryId].AdductIon.AdductIonName;

                var inchikey = peak.Inchikey;
                if (adduct == null || adduct == string.Empty) continue;

                var centralAdduct = AdductIonParcer.GetAdductIonBean(adduct);
                var centralExactMass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(peak.AccurateMass,
                    centralAdduct.AdductIonAccurateMass,
                    centralAdduct.ChargeNumber, centralAdduct.AdductIonXmer, projectProp.IonMode);
                var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200

                var searchedPrecursors = new List<SearchedPrecursor>();
                foreach (var searchedAdduct in searchedAdducts) {
                    if (centralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                    var searchedPrecursorMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(searchedAdduct, centralExactMass);
                    searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
                }

                foreach (var searchedPeak in peakSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                    if (searchedPeak.IsLinked) continue;
                    if (peak.PeakID == searchedPeak.PeakID) continue;

                    foreach (var searchedPrecursor in searchedPrecursors) {

                        var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.AccurateMass, ppm);

                        if (Math.Abs(searchedPeak.AccurateMass - searchedPrecursor.PrecursorMz) < adductTol) {

                            if ((searchedPeak.LibraryID >= 0 || searchedPeak.PostIdentificationLibraryId >= 0) &&
                                !searchedPeak.MetaboliteName.Contains("w/o")) {
                                if (searchedPeak.AdductIonName != searchedPrecursor.AdductIon.AdductIonName)
                                    continue;
                            }

                            var searchedAdduct = searchedPrecursor.AdductIon;
                            searchedPeak.AdductParent = peak.PeakID;
                            searchedPeak.AdductIonName = searchedAdduct.AdductIonName;
                            searchedPeak.AdductIonChargeNumber = searchedAdduct.ChargeNumber;
                            searchedPeak.AdductIonXmer = searchedAdduct.AdductIonXmer;
                            searchedPeak.AdductIonAccurateMass = (float)searchedAdduct.AdductIonAccurateMass;
                            searchedPeak.IsLinked = true;

                            peak.AdductParent = peak.PeakID;
                            registerLinks(peak, searchedPeak, PeakLinkFeatureEnum.Adduct);

                            break;
                        }
                    }
                }
            }
        }

        private void assignLinksBasedOnInChIKeys(List<PeakAreaBean> peakSpots, 
            List<MspFormatCompoundInformationBean> mspDB, 
            List<PostIdentificatioinReferenceBean> postIdentDB) {
            foreach (var peak in peakSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                if (peak.LibraryID < 0 && peak.PostIdentificationLibraryId < 0) continue;
                if (peak.MetaboliteName.Contains("w/o")) continue;
                var inchikey = peak.Inchikey;
                if (inchikey == null || inchikey == string.Empty || inchikey.Length != 27) continue;

                var shortInChIKey = inchikey.Substring(0, 14);

                foreach (var cPeak in peakSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                    if (peak.PeakID == cPeak.PeakID) continue;
                    if (cPeak.IsLinked) continue;
                    if (cPeak.LibraryID < 0 && cPeak.PostIdentificationLibraryId < 0) continue;
                    if (cPeak.MetaboliteName.Contains("w/o")) continue;
                    var cInchikey = cPeak.Inchikey;
                    if (cInchikey == null || cInchikey == string.Empty || cInchikey.Length != 27) continue;

                    var cShortInChIKey = cInchikey.Substring(0, 14);

                    if (shortInChIKey == cShortInChIKey) {
                        peak.IsLinked = true;
                        peak.AdductParent = peak.PeakID;

                        cPeak.IsLinked = true;
                        cPeak.AdductParent = peak.PeakID;
                        registerLinks(peak, cPeak, PeakLinkFeatureEnum.Adduct);
                    }
                }
            }
        }

        private void searchedAdductInitialize(AnalysisParametersBean param, ProjectPropertyBean projectProp) {
            this.searchedAdducts = new List<Rfx.Riken.OsakaUniv.AdductIon>();
            for (int i = 0; i < param.AdductIonInformationBeanList.Count; i++) {
                if (param.AdductIonInformationBeanList[i].Included)
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean(param.AdductIonInformationBeanList[i].AdductName));
            }
            if (this.searchedAdducts.Count == 0) {
                if (projectProp.IonMode == IonMode.Positive) searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[M+H]+"));
                else searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[M-H]-"));
            }

            if (this.searchedAdducts.Count == 1 && projectProp.IsLabPrivateVersion) {
                if (projectProp.IonMode == IonMode.Positive) {
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[M+NH4]+"));
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[M+Na]+"));
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[M-C6H10O5+H]+"));
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[2M+H]+"));
                }
                else {
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[M-H2O-H]-"));
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[M-C6H10O5-H]-"));
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[2M-H]-"));
                }
            }
        }

        private static void registerLinks(PeakAreaBean cSpot, PeakAreaBean rSpot, PeakLinkFeatureEnum rLinkProp) {
            if (cSpot.PeakLinks.Count(n => n.LinkedPeakID == rSpot.PeakID && n.Character == rLinkProp) == 0) {
                cSpot.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = rSpot.PeakID,
                    Character = rLinkProp
                });
                cSpot.IsLinked = true;
            }
            if (rSpot.PeakLinks.Count(n => n.LinkedPeakID == cSpot.PeakID && n.Character == rLinkProp) == 0) {
                rSpot.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = cSpot.PeakID,
                    Character = rLinkProp
                });
                rSpot.IsLinked = true;
            }
        }
    }
}
