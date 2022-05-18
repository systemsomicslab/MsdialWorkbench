using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{
    public class SearchedPrecursor {
        public double PrecursorMz { get; set; }
        public AdductIon AdductIon { get; set; }
    }
    public class PeakCharacterEstimator {
        private PeakCharacterEstimator() { }
        private static float rtMargin = 0.0177F;
        public double InitialProgress { get; set; } = 60.0;
        public double ProgressMax { get; set; } = 30.0;

        public PeakCharacterEstimator(double InitialProgress, double ProgressMax) {
            this.InitialProgress = InitialProgress;
            this.ProgressMax = ProgressMax;
        }
        public List<AdductIon> SearchedAdducts { get; set; } = new List<AdductIon>();

        public void Process(IReadOnlyList<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures,
            List<MSDecResult> msdecResults, IMatchResultEvaluator<MsScanMatchResult> evaluator, ParameterBase parameter, Action<int> reportAction) {
            
            // some adduct features are automatically insearted even if users did not select any type of adduct
            SearchedAdductInitialize(parameter);

            // collecting the same RT region spots
            chromPeakFeatures = chromPeakFeatures.OrderBy(n => n.PeakID).ToList();
            Initialization(chromPeakFeatures);

            chromPeakFeatures = chromPeakFeatures.OrderBy(n => n.Mass).ToList();
            if (chromPeakFeatures.Count < 10000) {
                for (int i = 0; i < chromPeakFeatures.Count; i++) {
                    var feature = chromPeakFeatures[i];
                    var peakRt = feature.ChromXs.RT.Value > 0 ? feature.ChromXs.RT.Value : 0;
                    var peakMz = feature.Mass;
                    var startScanIndex = SearchCollection.LowerBound(chromPeakFeatures, new ChromatogramPeakFeature() { Mass = peakMz - parameter.CentroidMs1Tolerance }, (a, b) => a.Mass.CompareTo(b.Mass));
                    var searchedPeakSpots = new List<ChromatogramPeakFeature>() { feature };

                    for (int j = startScanIndex; j < chromPeakFeatures.Count; j++) {
                        var sPeakCandidate = chromPeakFeatures[j];
                        var sPeakRt = sPeakCandidate.ChromXs.RT.Value > 0 ? sPeakCandidate.ChromXs.RT.Value : 0;
                        if (feature.PeakID == sPeakCandidate.PeakID) continue;
                        if (Math.Abs(sPeakRt - peakRt) <= rtMargin) {
                            searchedPeakSpots.Add(sPeakCandidate);
                        }
                    }

                    // CharacterAssigner(searchedPeakSpots, spectrumList, msdecResults, evaluator, param); // TODO: temporarily comment out. fix algorithm. Don't delete!
                    ReportProgress.Show(InitialProgress, ProgressMax, i, chromPeakFeatures.Count, reportAction);
                }
            }
            ReportProgress.Show(InitialProgress, ProgressMax, chromPeakFeatures.Count, chromPeakFeatures.Count, reportAction);

            chromPeakFeatures = chromPeakFeatures.OrderBy(n => n.PeakID).ToList();
            FinalizationForAdduct(chromPeakFeatures, parameter);
            AssignPutativePeakgroupIDs(chromPeakFeatures);
        }

        // currently, the links for same metabolite, isotope, and adduct are grouped.
        // the others such as found in upper msms and chromatogram correlation are not grouped.
        // in future, I have to create the merge GUI for user side
        private void AssignPutativePeakgroupIDs(List<ChromatogramPeakFeature> chromPeakFeatures) {
            var groupID = 0;
            foreach (var peak in chromPeakFeatures) {
                var peakCharacter = peak.PeakCharacter;
                if (peakCharacter.PeakGroupID >= 0) continue;
                if (peakCharacter.PeakLinks.Count == 0) {
                    peakCharacter.PeakGroupID = groupID;
                }
                else {
                    var crawledPeaks = new List<int>();
                    peakCharacter.PeakGroupID = groupID;
                    recPeakGroupAssignment(peak, chromPeakFeatures, groupID, crawledPeaks);
                }
                groupID++;
            }
        }

        private void recPeakGroupAssignment(ChromatogramPeakFeature peak, List<ChromatogramPeakFeature> peakSpots, int groupID, List<int> crawledPeaks) {
            var peakCharacter = peak.PeakCharacter;
            if (peakCharacter.PeakLinks == null || peakCharacter.PeakLinks.Count == 0) return;
            foreach (var linkedPeak in peak.PeakCharacter.PeakLinks) {
                var linkedPeakID = linkedPeak.LinkedPeakID;
                var character = linkedPeak.Character;
                if (crawledPeaks.Contains(linkedPeakID)) continue;

                peakSpots[linkedPeakID].PeakCharacter.PeakGroupID = groupID;
                crawledPeaks.Add(linkedPeakID);

                if (isCrawledPeaks(peakSpots[linkedPeakID].PeakCharacter.PeakLinks, crawledPeaks, peak.PeakID)) continue;
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


        private void Initialization(List<ChromatogramPeakFeature> chromPeakFeatures) {
            foreach (var peak in chromPeakFeatures) {
                var character = peak.PeakCharacter;
                if (character.IsotopeWeightNumber > 0) {
                    var parentID = character.IsotopeParentPeakID;
                    var parentCharacter = chromPeakFeatures[parentID].PeakCharacter;
                    if (parentCharacter.AdductType != null && parentCharacter.AdductType.FormatCheck) {
                        peak.AddAdductType(parentCharacter.AdductType);
                    }
                    if (character.PeakLinks.Count(n => n.LinkedPeakID == parentID &&
                        n.Character == PeakLinkFeatureEnum.Isotope) == 0) {

                        character.PeakLinks.Add(new LinkedPeakFeature() {
                            LinkedPeakID = parentID,
                            Character = PeakLinkFeatureEnum.Isotope
                        });
                        character.IsLinked = true;

                        if (parentCharacter.PeakLinks.Count(n => n.LinkedPeakID == peak.PeakID && n.Character == PeakLinkFeatureEnum.Isotope) == 0) {
                            parentCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                                LinkedPeakID = peak.PeakID,
                                Character = PeakLinkFeatureEnum.Isotope
                            });
                        }
                    }
                }
            }
        }

        private void SearchedAdductInitialize(ParameterBase param) {
            var paramAdducts = param.SearchedAdductIons;
            foreach (var adduct in paramAdducts.OrEmptyIfNull().Where(n => n.IsIncluded)) {
                SearchedAdducts.Add(adduct);
            }
            if (SearchedAdducts.Count == 0) {
                var protonAdduct = param.IonMode == IonMode.Positive ? AdductIonParser.GetAdductIonBean("[M+H]+") : AdductIonParser.GetAdductIonBean("[M-H]-");
                SearchedAdducts.Add(protonAdduct);
            }
        }

        private void ObjectsRefresh(List<ChromatogramPeakFeature> chromPeakFeatures) {
            if (chromPeakFeatures.IsEmptyOrNull()) return;
            foreach (var spot in chromPeakFeatures) {
                spot.PeakCharacter = new IonFeatureCharacter();
                foreach (var dSpot in spot.DriftChromFeatures.OrEmptyIfNull()) {
                    dSpot.PeakCharacter = new IonFeatureCharacter();
                }
            }
        }

        private void FinalizationForAdduct(List<ChromatogramPeakFeature> chromPeakFeatures, ParameterBase param) {
            var defaultAdduct = SearchedAdducts[0];

            foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0)) {
                if (peak.PeakCharacter.AdductParent < 0)
                    peak.PeakCharacter.AdductParent = peak.PeakID;
                if (peak.IsAdductTypeFormatted) continue;

                if (peak.PeakCharacter.Charge >= 2) {

                    var adductString = string.Empty;
                    if (param.IonMode == IonMode.Positive) {
                        adductString = "[M+" + peak.PeakCharacter.Charge + "H]" + peak.PeakCharacter.Charge + "+";
                    }
                    else {
                        adductString = "[M-" + peak.PeakCharacter.Charge + "H]" + peak.PeakCharacter.Charge + "-";
                    }
                    var estimatedAdduct = AdductIonParser.GetAdductIonBean(adductString);
                    peak.AddAdductType(estimatedAdduct);
                }
                else {
                    peak.AddAdductType(defaultAdduct);
                }
            }

            foreach (var peak in chromPeakFeatures) {
                if (peak.PeakCharacter.IsotopeParentPeakID >= 0 && peak.PeakCharacter.IsotopeWeightNumber > 0) {

                    var parentPeak = chromPeakFeatures[peak.PeakCharacter.IsotopeParentPeakID];

                    peak.PeakCharacter.AdductParent = parentPeak.PeakCharacter.AdductParent;
                    peak.AddAdductType(parentPeak.AdductType);
                }
            }

            //refine the dependency
            foreach (var peak in chromPeakFeatures) {
                var currentPeak = peak;
                var ancestorId = currentPeak.PeakCharacter.AdductParent;
                while (currentPeak.PeakID != ancestorId) {
                    currentPeak = chromPeakFeatures[ancestorId];
                    ancestorId = currentPeak.PeakCharacter.AdductParent;
                }
                peak.PeakCharacter.AdductParent = ancestorId;
            }

            //// currently, just copy the isotope and adduct of parent spot to drift spots
            //if (ccsCalInfo != null) {
            //    var calinfo = ccsCalInfo;
            //    foreach (var peak in chromPeakFeatures) {
            //        foreach (var drift in peak.DriftChromFeatures) {
            //            if (drift.PeakCharacter.IsotopeWeightNumber < 0)
            //                drift.PeakCharacter.IsotopeWeightNumber = peak.PeakCharacter.IsotopeWeightNumber;
            //            if (!drift.IsAdductTypeFormatted()) {
            //                drift.AddAdductType(peak.AdductType);
            //            }
            //            drift.CollisionCrossSection = (float)IonMobilityUtility.MobilityToCrossSection(mobilitytype,
            //                drift.ChromXsTop.Drift.Value, Math.Abs(drift.PeakCharacter.Charge), drift.Mass, calinfo, isAllCalibrantImported);
            //        }
            //    }
            //}
        }

        // the RT deviations of peakspots should be less than 0.03 min
        // here, each peak is evaluated.
        // the purpose is to group the ions which are recognized as the same metabolite
        private void CharacterAssigner(List<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<RawSpectrum> spectrumList, List<MSDecResult> msdecResults, IMatchResultEvaluator<MsScanMatchResult> evaluator, ParameterBase param) {
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return;

            // if the first inchikey is same, it's recognized as the same metabolite.
            assignLinksBasedOnInChIKeys(chromPeakFeatures, evaluator);

            // The identified compound is used for searching.
            assignLinksBasedOnIdentifiedCompound(chromPeakFeatures, evaluator, param);

            if (param.IonMode == IonMode.Negative)
                assignAdductByMsMs(chromPeakFeatures, msdecResults, param);

            // The identified adduct is used for searching
            assignLinksBasedOnDeterminedAdduct(chromPeakFeatures, evaluator, param);

            // adduct pairing method
            assignLinksBasedOnAdductPairingMethod(chromPeakFeatures, param);

            // linkage by chromatogram correlations
            assignLinksBasedOnChromatogramCorrelation(chromPeakFeatures, spectrumList, param);

            // linked by partial matching of MS1 and MS2
            if (param.AcquisitionType == AcquisitionType.AIF || param.AcquisitionType == AcquisitionType.SWATH) return;
            assignLinksBasedOnPartialMatchingOfMS1MS2(chromPeakFeatures, msdecResults, param);
        }

        private void assignAdductByMsMs(List<ChromatogramPeakFeature> chromPeakFeatures, List<MSDecResult> msdecResults, ParameterBase param) {

            var isAcetateAdduct = false;
            var isFormateAdduct = false;

            AdductIon acetateAdduct = null;
            AdductIon formateAdduct = null;

            foreach (var adduct in SearchedAdducts) {
                if (Math.Abs(adduct.AdductIonAccurateMass - 44.998201) < param.CentroidMs2Tolerance) {
                    isFormateAdduct = true;
                    formateAdduct = adduct;
                    break;
                }
                if (Math.Abs(adduct.AdductIonAccurateMass - 59.013851) < param.CentroidMs2Tolerance) {
                    isAcetateAdduct = true;
                    acetateAdduct = adduct;
                    break;
                }
            }

            if (isAcetateAdduct == false && isFormateAdduct == false) return;

            foreach (var peak in chromPeakFeatures.Where(n => !n.IsAdductTypeFormatted && n.MS2RawSpectrumID >= 0 && n.PeakCharacter.IsotopeWeightNumber == 0)) {

                var spectrum = !peak.Spectrum.IsEmptyOrNull() 
                    ? peak.Spectrum 
                    : !msdecResults.IsEmptyOrNull() 
                        ? msdecResults[peak.GetMSDecResultID()].Spectrum 
                        : null;
                if (spectrum.IsEmptyOrNull()) continue;

                var precursorMz = peak.Mass;
                var precursorIntensity = 1.0;
                
                var maxIntensity = spectrum.Max(n => n.Intensity);
                var massSpec = spectrum.OrderByDescending(n => n.Mass).ToList();

                foreach (var spec in massSpec) {

                    var neutralloss = precursorMz - spec.Mass;
                    var relativeIntensity = spec.Intensity / maxIntensity * 100;
                    if (neutralloss < param.CentroidMs2Tolerance) {
                        precursorIntensity = relativeIntensity;
                    }

                    if (isFormateAdduct && Math.Abs(neutralloss - 46.005477) < param.CentroidMs2Tolerance && relativeIntensity > 80.0 && precursorIntensity < 40.0) {
                        peak.PeakCharacter.AdductParent = peak.PeakID;
                        peak.AddAdductType(formateAdduct);
                        break;
                    }

                    if (isAcetateAdduct && Math.Abs(neutralloss - 60.021127) < param.CentroidMs2Tolerance && relativeIntensity > 80.0 && precursorIntensity < 40.0) {
                        peak.PeakCharacter.AdductParent = peak.PeakID;
                        peak.AddAdductType(acetateAdduct);
                        break;
                    }
                }
            }
        }
        
        // currently, the method is very simple.
        // if a peak (at least 10% relative abundance) in MS/MS is found in MS1 spectrum,
        // the peak of MS1 is assigned as "Found in upper MSMS"
        private void assignLinksBasedOnPartialMatchingOfMS1MS2(
            List<ChromatogramPeakFeature> chromPeakFeatures,
            List<MSDecResult> msdecResults,
            ParameterBase param) {

            chromPeakFeatures = chromPeakFeatures.OrderBy(chromPeakFeature => chromPeakFeature.Mass).ToList();
            for (int i = chromPeakFeatures.Count - 1; i >= 0; i--) {
                var peak = chromPeakFeatures[i];
                if (peak.MS2RawSpectrumID < 0) continue;
                if (peak.PeakCharacter.IsotopeWeightNumber != 0) continue;

                var spectrum = !peak.Spectrum.IsEmptyOrNull()
                    ? peak.Spectrum
                    : !msdecResults.IsEmptyOrNull()
                        ? msdecResults[peak.GetMSDecResultID()].Spectrum
                        : null;
                if (spectrum.IsEmptyOrNull())
                    continue;
                var maxIntensity = spectrum.Max(n => n.Intensity);
                spectrum = spectrum
                    .Where(specPeak => specPeak.Intensity / maxIntensity >= 0.1)
                    .OrderBy(specPeak => specPeak.Mass)
                    .ToList();

                var k = 0;
                for (int j = 0; j < i; j++) {
                    var cPeak = chromPeakFeatures[j];
                    if (cPeak.PeakCharacter.IsotopeWeightNumber != 0)
                        continue;
                    while (k < spectrum.Count && spectrum[k].Mass < cPeak.Mass - param.CentroidMs2Tolerance) {
                        k++;
                    }
                    if (k < spectrum.Count && Math.Abs(spectrum[k].Mass - cPeak.Mass) < param.CentroidMs2Tolerance) {
                        cPeak.PeakCharacter.IsLinked = true;
                        peak.PeakCharacter.IsLinked = true;

                        registerLinks(peak, cPeak, PeakLinkFeatureEnum.FoundInUpperMsMs);
                    }
                }
            }
        }

        // currently, only pure peaks are evaluated by this way.
        private void assignLinksBasedOnChromatogramCorrelation(List<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<RawSpectrum> spectrumList, ParameterBase param) {
            if (chromPeakFeatures[0].ChromXs.RT.Value < 0) return;
            RawSpectra rawSpectra = new RawSpectra(spectrumList, ChromXType.RT, ChromXUnit.Min, param.IonMode);
            foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 && n.PeakShape.PeakPureValue >= 0.9)) {
                
                var tTopRt = peak.ChromXsTop.RT.Value;
                var tLeftRt = peak.ChromXsLeft.RT.Value;
                var tRightRt = peak.ChromXsRight.RT.Value;
                var tPeaklist = rawSpectra.GetMs1Chromatogram(peak.Mass, param.CentroidMs1Tolerance, tLeftRt, tRightRt);
                var tChrom = tPeaklist.Smoothing(param.SmoothingMethod, param.SmoothingLevel);

                foreach (var cPeak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 
                && !n.PeakCharacter.IsLinked && n.PeakID != peak.PeakID && n.PeakShape.PeakPureValue >= 0.9)) {

                    var cPeaklist = rawSpectra.GetMs1Chromatogram(cPeak.Mass, param.CentroidMs1Tolerance, tLeftRt, tRightRt);
                    var cChrom = cPeaklist.Smoothing(param.SmoothingMethod, param.SmoothingLevel);

                    //Debug.WriteLine("tChrom count {0}, cChrom count {1}", tChrom.Count, cChrom.Count);

                    double scaT = 0.0, scaC = 0.0, cov = 0.0;
                    for (int i = 0; i < tChrom.Count; i++) {
                        if (cChrom.Count - 1 < i) break;
                        scaT += Math.Pow(tChrom[i].Intensity, 2);
                        scaC += Math.Pow(cChrom[i].Intensity, 2);
                        cov += tChrom[i].Intensity * cChrom[i].Intensity;
                    }
                    if (scaT == 0 || scaC == 0) continue;
                    var col = cov / Math.Sqrt(scaT) / Math.Sqrt(scaC);

                    if (col > 0.95) {
                        cPeak.PeakCharacter.IsLinked = true;
                        peak.PeakCharacter.IsLinked = true;

                        registerLinks(peak, cPeak, PeakLinkFeatureEnum.ChromSimilar);
                    }
                }
            }
        }

        // just copied from the previous adduct estimator, should be checked for the improvement
        private void assignLinksBasedOnAdductPairingMethod(List<ChromatogramPeakFeature> chromPeakFeatures, ParameterBase param) {
            foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 && !n.PeakCharacter.IsLinked && !n.IsAdductTypeFormatted)) {
                var flg = false;
                var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200

                foreach (var centralAdduct in SearchedAdducts) {

                    var rCentralAdduct = AdductIonParser.ConvertDifferentChargedAdduct(centralAdduct, peak.PeakCharacter.Charge);
                    var centralExactMass = rCentralAdduct.ConvertToExactMass(peak.Mass);

                    var searchedPrecursors = new List<SearchedPrecursor>();
                    foreach (var searchedAdduct in SearchedAdducts) {
                        if (rCentralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                        var searchedPrecursorMz = searchedAdduct.ConvertToMz(centralExactMass);
                        searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
                    }

                    foreach (var searchedPeak in chromPeakFeatures.Where(n => !n.PeakCharacter.IsLinked && peak.PeakID != n.PeakID && !n.IsAdductTypeFormatted)) {
                        foreach (var searchedPrecursor in searchedPrecursors) {

                            var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.Mass, ppm);

                            if (Math.Abs(searchedPeak.Mass - searchedPrecursor.PrecursorMz) < adductTol) {

                                var searchedAdduct = searchedPrecursor.AdductIon;

                                searchedPeak.AddAdductType(searchedAdduct);
                                searchedPeak.PeakCharacter.AdductParent = peak.PeakID;
                                searchedPeak.PeakCharacter.IsLinked = true;

                                registerLinks(peak, searchedPeak, PeakLinkFeatureEnum.Adduct);
                                flg = true;
                                break;
                            }
                        }
                    }

                    if (flg) {

                        peak.AddAdductType(rCentralAdduct);
                        peak.PeakCharacter.AdductParent = peak.PeakID;
                        peak.PeakCharacter.IsLinked = true;
                       
                        break;
                    }
                }
            }
        }

        private void assignLinksBasedOnDeterminedAdduct(List<ChromatogramPeakFeature> chromPeakFeatures, IMatchResultEvaluator<MsScanMatchResult> evaluator, ParameterBase param) {
            foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 && n.IsAdductTypeFormatted)) {
                var centralAdduct = peak.AdductType;
                var centralExactMass = centralAdduct.ConvertToExactMass(peak.Mass);
                var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200

                var searchedPrecursors = new List<SearchedPrecursor>();
                foreach (var searchedAdduct in SearchedAdducts) {
                    if (centralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                    var searchedPrecursorMz = searchedAdduct.ConvertToMz(centralExactMass);
                    searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
                }

                foreach (var searchedPeak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 && !n.PeakCharacter.IsLinked && n.PeakID != peak.PeakID)) {
                    foreach (var searchedPrecursor in searchedPrecursors) {
                        var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.Mass, ppm);
                        if (searchedPeak.IsReferenceMatched(evaluator)) {
                            continue;
                        }

                        if (Math.Abs(searchedPeak.Mass - searchedPrecursor.PrecursorMz) < adductTol) {
                            var searchedAdduct = searchedPrecursor.AdductIon;
                            searchedPeak.AddAdductType(searchedAdduct);
                            searchedPeak.PeakCharacter.AdductParent = peak.PeakID;
                            searchedPeak.PeakCharacter.IsLinked = true;

                            peak.PeakCharacter.AdductParent = peak.PeakID;
                            registerLinks(peak, searchedPeak, PeakLinkFeatureEnum.Adduct);

                            break;
                        }
                    }
                }
            }
        }

        private void assignLinksBasedOnIdentifiedCompound(List<ChromatogramPeakFeature> chromPeakFeatures, IMatchResultEvaluator<MsScanMatchResult> evaluator, ParameterBase param) {
            foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0)) {
                if (peak.IsUnknown || peak.IsAnnotationSuggested(evaluator)) continue;

                //adduct null check
                if (peak.AdductType == null) continue;
                var inchikey = peak.InChIKey;
                var centralAdduct = peak.AdductType;
                var centralExactMass = centralAdduct.ConvertToExactMass(peak.Mass);
                var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200

                var searchedPrecursors = new List<SearchedPrecursor>();
                foreach (var searchedAdduct in SearchedAdducts) {
                    if (centralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                    var searchedPrecursorMz = searchedAdduct.ConvertToMz(centralExactMass);
                    searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
                }

                foreach (var searchedPeak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 && !n.PeakCharacter.IsLinked)) {
                    if (peak.PeakID == searchedPeak.PeakID) continue;
                    foreach (var searchedPrecursor in searchedPrecursors) {

                        var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.Mass, ppm);

                        if (Math.Abs(searchedPeak.Mass - searchedPrecursor.PrecursorMz) < adductTol) {

                            if (searchedPeak.IsReferenceMatched(evaluator)) {
                                if (searchedPeak.AdductType.AdductIonName != searchedPrecursor.AdductIon.AdductIonName)
                                    continue;
                            }

                            var searchedAdduct = searchedPrecursor.AdductIon;
                            searchedPeak.AddAdductType(searchedAdduct);
                            searchedPeak.PeakCharacter.AdductParent = peak.PeakID;
                            searchedPeak.PeakCharacter.IsLinked = true;

                            peak.PeakCharacter.AdductParent = peak.PeakID;
                            registerLinks(peak, searchedPeak, PeakLinkFeatureEnum.Adduct);

                            break;
                        }
                    }
                }
            }
        }

        private void assignLinksBasedOnInChIKeys(List<ChromatogramPeakFeature> chromPeakFeatures, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0)) {
                
                if (peak.IsUnknown || peak.IsAnnotationSuggested(evaluator)) continue;
                if (!peak.IsValidInChIKey()) continue;
                var inchikey = peak.InChIKey;
                var shortInChIKey = inchikey.Substring(0, 14);

                foreach (var cPeak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0)) {

                    var peakCharacter = peak.PeakCharacter;
                    var cPeakCharacter = cPeak.PeakCharacter;
                    
                    if (peak.PeakID == cPeak.PeakID) continue;
                    if (cPeakCharacter.IsLinked) continue;
                    if (cPeak.IsUnknown || cPeak.IsAnnotationSuggested(evaluator)) continue;
                    if (!cPeak.IsValidInChIKey()) continue;

                    var cInchikey = cPeak.InChIKey;
                    var cShortInChIKey = cInchikey.Substring(0, 14);

                    if (shortInChIKey == cShortInChIKey) {
                        peakCharacter.IsLinked = true;
                        peakCharacter.AdductParent = peak.PeakID;

                        cPeakCharacter.IsLinked = true;
                        cPeakCharacter.AdductParent = peak.PeakID;
                        registerLinks(peak, cPeak, PeakLinkFeatureEnum.Adduct);
                    }
                }
            }
        }

        private void registerLinks(ChromatogramPeakFeature cSpot, ChromatogramPeakFeature rSpot, PeakLinkFeatureEnum rLinkProp) {
            var cSpotCharacter = cSpot.PeakCharacter;
            var rSpotCharacter = rSpot.PeakCharacter;
            if (cSpotCharacter.PeakLinks.All(n => n.LinkedPeakID != rSpot.PeakID || n.Character != rLinkProp)) {
                cSpotCharacter.PeakLinks.Add(new LinkedPeakFeature()
                {
                    LinkedPeakID = rSpot.PeakID,
                    Character = rLinkProp
                });
                cSpotCharacter.IsLinked = true;
            }
            if (rSpotCharacter.PeakLinks.All(n => n.LinkedPeakID != cSpot.PeakID || n.Character != rLinkProp)) {
                rSpotCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = cSpot.PeakID,
                    Character = rLinkProp
                });
                rSpotCharacter.IsLinked = true;
            }
        }
    }
}
