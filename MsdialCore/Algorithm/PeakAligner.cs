using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;

namespace CompMs.MsdialCore.Algorithm
{
    public class PeakAligner
    {
        PeakComparer Comparer { get; set; }
        ParameterBase Param { get; set; }
        IupacDatabase Iupac { get; set; }

        public PeakAligner(PeakComparer comparer, ParameterBase param, IupacDatabase iupac) {
            Comparer = comparer;
            Param = param;
            Iupac = iupac;
        }

        public PeakAligner(PeakComparer comparer, ParameterBase param, AlignmentProcessFactory factory) {
            Comparer = comparer;
            Param = param;
            AProcessFactory = factory;
        }

        public AlignmentResultContainer Alignment(
            IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var master = GetMasterList(analysisFiles);
            var alignments = AlignAll(master, analysisFiles);
            var alignmentSpots = CollectPeakSpots(analysisFiles, alignmentFile, alignments, spotSerializer);
            var alignmentResult = PackingSpots(alignmentSpots);

            IsotopeAnalysis(alignmentResult);

            var alignedSpots = GetRefinedAlignmentSpotProperties(alignmentResult.AlignmentSpotProperties);

            var minInt = (double)alignmentResult.AlignmentSpotProperties.Min(spot => spot.HeightMin);
            var maxInt = (double)alignmentResult.AlignmentSpotProperties.Max(spot => spot.HeightMax);

            if (maxInt > 1) maxInt = Math.Log(maxInt, 2); else maxInt = 1;
            if (minInt > 1) minInt = Math.Log(minInt, 2); else minInt = 0;

            for (int i = 0; i < alignedSpots.Count; i++) {
                var relativeValue = (float)((Math.Log(alignedSpots[i].HeightMax, 2) - minInt)
                    / (maxInt - minInt));
                if (relativeValue < 0)
                    relativeValue = 0;
                else if (relativeValue > 1)
                    relativeValue = 1;
                alignedSpots[i].RelativeAmplitudeValue = relativeValue;
            }

            alignmentResult.AlignmentSpotProperties = alignedSpots;
            alignmentResult.TotalAlignmentSpotCount = alignedSpots.Count;

            return alignmentResult;
        }

        protected virtual List<IMSScanProperty> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles) {

            var referenceId = Param.AlignmentReferenceFileID;
            var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
            if (referenceFile == null) return new List<IMSScanProperty>();

            var master = GetChromatogramPeakFeatures(referenceFile, Param.MachineCategory);
            foreach (var analysisFile in analysisFiles) {
                if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId)
                    continue;
                var target = GetChromatogramPeakFeatures(analysisFile, Param.MachineCategory);
                MergeChromatogramPeaks(master, target);
            }

            return master;
        }

        private void MergeChromatogramPeaks(List<ChromatogramPeakFeature> masters, List<ChromatogramPeakFeature> targets) {
            foreach (var target in targets) {
                var samePeakExists = false;
                foreach (var master in masters) {
                    if (Comparer.Equals(master, target)) {
                        samePeakExists = true;
                        break;
                    }
                }
                if (!samePeakExists) masters.Add(target);
            }
        }

        protected virtual List<List<AlignmentChromPeakFeature>> AlignAll(
            IReadOnlyList<IMSScanProperty> master, IReadOnlyList<AnalysisFileBean> analysisFiles) {

            var result = new List<List<AlignmentChromPeakFeature>>(analysisFiles.Count);

            foreach (var analysisFile in analysisFiles) {
                var chromatogram = GetChromatogramPeakFeatures(analysisFile, Param.MachineCategory);
                var peaks = AlignPeaksToMaster(chromatogram, master);
                result.Add(peaks);
            }

            return result;
        }

        // TODO: too slow.
        private List<AlignmentChromPeakFeature> AlignPeaksToMaster(IEnumerable<ChromatogramPeakFeature> peaks, IReadOnlyList<ChromatogramPeakFeature> master) {
            var n = master.Count;
            var result = Enumerable.Repeat<AlignmentChromPeakFeature>(null, n).ToList();
            var maxMatchs = new double[n];

            foreach (var peak in peaks) {
                int? matchIdx = null;
                double matchFactor = double.MinValue;
                for (var i = 0; i < n; i++) {
                    var factor = Comparer.GetSimilality(master[i], peak);
                    if (factor > maxMatchs[i] && factor > matchFactor) {
                        matchIdx = i;
                        matchFactor = factor;
                    }
                }
                if (matchIdx.HasValue)
                    result[matchIdx.Value] = DataObjConverter.ConvertToAlignmentChromPeakFeature(peak, Param.MachineCategory);
            }

            return result;
        }

        private List<AlignmentSpotProperty> CollectPeakSpots(IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            List<List<AlignmentChromPeakFeature>> alignments, ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var aligns = new List<List<AlignmentChromPeakFeature>>();
            var files = new List<string>();
            var chromPeakInfoSerializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPSTMP");
            var quantMasses = GetQuantmassDictionary(analysisFiles, alignments);
            var tAlignments = alignments.Sequence().ToList();

            foreach ((var analysisFile, var alignment) in analysisFiles.Zip(alignments)) {
                (var peaks, var file) = CollectAlignmentPeaks(analysisFile, alignment, tAlignments, quantMasses, chromPeakInfoSerializer);
                foreach (var peak in peaks) {
                    peak.FileID = analysisFile.AnalysisFileId;
                    peak.FileName = analysisFile.AnalysisFileName;
                }
                aligns.Add(peaks);
                files.Add(file);
            }
            var spots = PackingAlignmentsToSpots(aligns.Sequence().ToList());

            SerializeSpotInfo(spots, files, alignmentFile, spotSerializer, chromPeakInfoSerializer);
            foreach (var f in files)
                if (File.Exists(f))
                    File.Delete(f);

            return spots;
        }

        private List<double> GetQuantmassDictionary(IReadOnlyList<AnalysisFileBean> analysisFiles, List<List<AlignmentChromPeakFeature>> alignments) {
            var sampleNum = alignments.Count;
            var peakNum = alignments[0].Count;
            var quantmasslist = Enumerable.Repeat<double>(-1, peakNum).ToList();

            if (Param.MachineCategory != MachineCategory.GCMS) return quantmasslist;

            var isReplaceMode = this.AProcessFactory.MspDB.IsEmptyOrNull() ? false : true;
            var bin = this.Param.AccuracyType == AccuracyType.IsAccurate ? 2 : 0;
            for (int i = 0; i < peakNum; i++) {
                var alignment = new List<AlignmentChromPeakFeature>();
                for (int j = 0; j < sampleNum; j++) alignment.Add(alignments[j][i]);
                var repFileID = DataObjConverter.GetRepresentativeFileID(alignment);
                
                if (isReplaceMode && alignment[repFileID].MspID() >= 0) {
                    var refQuantMass = this.AProcessFactory.MspDB[alignment[repFileID].MspID()].QuantMass;
                    if (refQuantMass >= this.Param.MassRangeBegin && refQuantMass <= this.Param.MassRangeEnd) {
                        quantmasslist[i] = refQuantMass; continue;
                    }
                }

                var dclFile = analysisFiles[repFileID].DeconvolutionFilePath;
                var msdecResult = MsdecResultsReader.ReadMSDecResult(dclFile, alignment[repFileID].SeekPointToDCLFile);
                var spectrum = msdecResult.Spectrum;
                var quantMassDict = new Dictionary<double, List<double>>();
                var maxPeakHeight = alignment.Max(n => n.PeakHeightTop);
                var repQuantMass = alignment[repFileID].Mass;

                foreach (var feature in alignment.Where(n => n.PeakHeightTop > maxPeakHeight * 0.1)) {
                    var quantMass = Math.Round(feature.Mass, bin);
                    if (!quantMassDict.Keys.Contains(quantMass))
                        quantMassDict[quantMass] = new List<double>() { feature.Mass };
                    else
                        quantMassDict[quantMass].Add(feature.Mass);
                }
                var maxQuant = 0.0; var maxCount = 0;
                foreach (var pair in quantMassDict)
                    if (pair.Value.Count > maxCount) { maxCount = pair.Value.Count; maxQuant = pair.Key; }

                var quantMassCandidate = quantMassDict[maxQuant].Average();
                var basepeakMz = 0.0;
                var basepeakIntensity = 0.0;
                var isQuantMassExist = isQuantMassExistInSpectrum(quantMassCandidate, spectrum, this.Param.CentroidMs1Tolerance, 10.0F, out basepeakMz, out basepeakIntensity);
                if (AProcessFactory.IsRepresentativeQuantMassBasedOnBasePeakMz) {
                    quantmasslist[i] = basepeakMz; continue;
                }
                if (isQuantMassExist) {
                    quantmasslist[i] = quantMassCandidate; continue;
                }

                var isSuitableQuantMassExist = false;
                foreach (var peak in spectrum) {
                    if (peak.Mass < repQuantMass - bin) continue;
                    if (peak.Mass > repQuantMass + bin) break;
                    var diff = Math.Abs(peak.Mass - repQuantMass);
                    if (diff <= bin && peak.Intensity > basepeakIntensity * 10.0 * 0.01) {
                        isSuitableQuantMassExist = true;
                        break;
                    }
                }
                if (isSuitableQuantMassExist)
                    quantmasslist[i] = repQuantMass;
                else
                    quantmasslist[i] = basepeakMz;
            }
            return quantmasslist;
        }

        // spectrum should be ordered by m/z value
        private static bool isQuantMassExistInSpectrum(double quantMass, List<SpectrumPeak> spectrum, float bin, float threshold,
            out double basepeakMz, out double basepeakIntensity) {

            basepeakMz = 0.0;
            basepeakIntensity = 0.0;
            foreach (var peak in spectrum) {
                if (peak.Intensity > basepeakIntensity) {
                    basepeakIntensity = peak.Intensity;
                    basepeakMz = peak.Mass;
                }
            }

            var maxIntensity = basepeakIntensity;
            foreach (var peak in spectrum) {
                if (peak.Mass < quantMass - bin) continue;
                if (peak.Mass > quantMass + bin) break;
                var diff = Math.Abs(peak.Mass - quantMass);
                if (diff <= bin && peak.Intensity > maxIntensity * threshold * 0.01) {
                    return true;
                }
            }
            return false;
        }


        private (List<AlignmentChromPeakFeature>, string) CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks,
            List<List<AlignmentChromPeakFeature>> alignments, List<double> quantMasses,
            ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {

            var results = new List<AlignmentChromPeakFeature>();
            var peakInfos = new List<ChromatogramPeakInfo>();
            using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePath, 0, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt)) {
                var spectra = DataAccess.GetAllSpectra(rawDataAccess);
                foreach ((var peak, var alignment, var quantmass) in peaks.Zip(alignments, quantMasses)) {
                    var align = peak;
                    if (align == null) {
                        align = GapFilling(spectra, alignment, quantmass, analysisFile.AnalysisFileId);
                    }
                    results.Add(align);

                    var detected = alignment.Where(x => x != null);
                    var peaklist = DataAccess.GetMs1Peaklist(
                        spectra, (float)align.Mass,
                        (float)(detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f,
                        align.IonMode);
                    var peakInfo = new ChromatogramPeakInfo(
                        align.FileID, peaklist,
                        (float)align.ChromXsTop.Value,
                        (float)align.ChromXsLeft.Value,
                        (float)align.ChromXsRight.Value
                        );
                    peakInfos.Add(peakInfo);
                }
            }
            var file = Path.GetTempFileName();
            serializer?.SerializeAllToFile(file, peakInfos);
            return (results, file);
        }

        private AlignmentChromPeakFeature GapFilling(List<RawSpectrum> spectra, List<AlignmentChromPeakFeature> alignment, double quantmass, int fileID) {
            var features = alignment.Where(n => n != null);
            var chromXCenter = Comparer.GetCenter(features);
            if (quantmass > 0) chromXCenter.Mz = new MzValue(quantmass);

            return GapFiller.GapFilling(this.AProcessFactory,
                spectra, chromXCenter, Comparer.GetAveragePeakWidth(features), fileID);
        }

        private AlignmentResultContainer PackingSpots(List<AlignmentSpotProperty> alignmentSpots) {
            var spots = new System.Collections.ObjectModel.ObservableCollection<AlignmentSpotProperty>(alignmentSpots);
            return new AlignmentResultContainer {
                Ionization = Param.Ionization,
                AlignmentResultFileID = -1,
                TotalAlignmentSpotCount = spots.Count,
                AlignmentSpotProperties = spots,
            };
        }

        private void IsotopeAnalysis(AlignmentResultContainer alignmentResult) {
            foreach (var spot in alignmentResult.AlignmentSpotProperties) {
                if (Param.TrackingIsotopeLabels || spot.IsReferenceMatched) {
                    spot.PeakCharacter.IsotopeParentPeakID = spot.AlignmentID;
                    spot.PeakCharacter.IsotopeWeightNumber = 0;
                }
                if (!spot.IsReferenceMatched) {
                    spot.AdductType.AdductIonName = string.Empty;
                }
            }
            if (Param.TrackingIsotopeLabels) return;

            IsotopeEstimator.Process(alignmentResult.AlignmentSpotProperties, Param, Iupac);
        }

        private ObservableCollection<AlignmentSpotProperty> GetRefinedAlignmentSpotProperties(ObservableCollection<AlignmentSpotProperty> alignmentSpots) {
            if (alignmentSpots.Count <= 1) return alignmentSpots;

            var param = Param;

            var alignmentSpotList = new List<AlignmentSpotProperty>(alignmentSpots);
            if (Param.OnlyReportTopHitInTextDBSearch) { //to remove duplicate identifications

                alignmentSpotList = alignmentSpotList.OrderByDescending(spot => spot.MspID).ToList();

                var currentLibraryId = alignmentSpotList[0].MspID;
                var currentPeakId = 0;

                for (int i = 1; i < alignmentSpotList.Count; i++) {
                    if (alignmentSpotList[i].MspID < 0) break;
                    if (alignmentSpotList[i].MspID != currentLibraryId) {
                        currentLibraryId = alignmentSpotList[i].MspID;
                        currentPeakId = i;
                        continue;
                    }
                    else {
                        if (alignmentSpotList[currentPeakId].MspBasedMatchResult.TotalScore < alignmentSpotList[i].MspBasedMatchResult.TotalScore) {
                            DataObjConverter.SetDefaultCompoundInformation(alignmentSpotList[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            DataObjConverter.SetDefaultCompoundInformation(alignmentSpotList[i]);
                        }
                    }
                }

                alignmentSpotList = alignmentSpotList.OrderByDescending(n => n.TextDbID).ToList();

                currentLibraryId = alignmentSpotList[0].TextDbID;
                currentPeakId = 0;

                for (int i = 1; i < alignmentSpotList.Count; i++) {
                    if (alignmentSpotList[i].TextDbID < 0) break;
                    if (alignmentSpotList[i].TextDbID != currentLibraryId) {
                        currentLibraryId = alignmentSpotList[i].TextDbID;
                        currentPeakId = i;
                        continue;
                    }
                    else {
                        if (alignmentSpotList[currentPeakId].TextDbBasedMatchResult.TotalScore < alignmentSpotList[i].TextDbBasedMatchResult.TotalScore) {
                            DataObjConverter.SetDefaultCompoundInformation(alignmentSpotList[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            DataObjConverter.SetDefaultCompoundInformation(alignmentSpotList[i]);
                        }
                    }
                }
            }

            //cleaning duplicate spots
            var cSpots = new List<AlignmentSpotProperty>();
            var donelist = new List<int>();

            foreach (var spot in alignmentSpotList.Where(spot => spot.MspID >= 0 && !spot.Name.Contains("w/o"))) {
                TryMergeToMaster(spot, cSpots, donelist, param);
            }

            foreach (var spot in alignmentSpotList.Where(spot => spot.TextDbID >= 0 && !spot.Name.Contains("w/o"))) {
                TryMergeToMaster(spot, cSpots, donelist, param);
            }

            foreach (var spot in alignmentSpotList) {
                if (spot.MspID >= 0 && !spot.Name.Contains("w/o")) continue;
                if (spot.TextDbID >= 0 && !spot.Name.Contains("w/o")) continue;
                if (spot.PeakCharacter.IsotopeWeightNumber > 0) continue;
                TryMergeToMaster(spot, cSpots, donelist, param);
            }

            // further cleaing by blank features
            var fcSpots = new List<AlignmentSpotProperty>();
            int blankNumber = 0;
            int sampleNumber = 0;
            foreach (var value in param.FileID_AnalysisFileType.Values) {
                if (value == Common.Enum.AnalysisFileType.Blank) blankNumber++;
                if (value == Common.Enum.AnalysisFileType.Sample) sampleNumber++;
            }

            if (blankNumber > 0 && param.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange) {
              
                foreach (var spot in cSpots) {
                    var sampleMax = 0.0;
                    var sampleAve = 0.0;
                    var blankAve = 0.0;
                    var nonMinValue = double.MaxValue;

                    foreach (var peak in spot.AlignedPeakProperties) {
                        var filetype = param.FileID_AnalysisFileType[peak.FileID];
                        if (filetype == Common.Enum.AnalysisFileType.Blank) {
                            blankAve += peak.PeakHeightTop;
                        }
                        else if (filetype == Common.Enum.AnalysisFileType.Sample) {
                            if (peak.PeakHeightTop > sampleMax)
                                sampleMax = peak.PeakHeightTop;
                            sampleAve += peak.PeakHeightTop;
                        }

                        if (nonMinValue > peak.PeakHeightTop && peak.PeakHeightTop > 0.0001) {
                            nonMinValue = peak.PeakHeightTop;
                        }
                    }

                    sampleAve = sampleAve / sampleNumber;
                    blankAve = blankAve / blankNumber;
                    if (blankAve == 0) {
                        if (nonMinValue != double.MaxValue)
                            blankAve = nonMinValue * 0.1;
                        else
                            blankAve = 1.0;
                    }

                    var blankThresh = blankAve * param.FoldChangeForBlankFiltering;
                    var sampleThresh = param.BlankFiltering == Common.Enum.BlankFiltering.SampleMaxOverBlankAve ? sampleMax : sampleAve;
                
                    if (sampleThresh < blankThresh) {
                        if (param.IsKeepRemovableFeaturesAndAssignedTagForChecking) {

                            if (param.IsKeepRefMatchedMetaboliteFeatures
                              && (spot.MspID >= 0 || spot.TextDbID >= 0) && !spot.Name.Contains("w/o")) {

                            }
                            else if (param.IsKeepSuggestedMetaboliteFeatures
                              && (spot.MspID >= 0 || spot.TextDbID >= 0) && spot.Name.Contains("w/o")) {

                            }
                            else {
                                spot.FeatureFilterStatus.IsBlankFiltered = true;
                            }
                        }
                        else {

                            if (param.IsKeepRefMatchedMetaboliteFeatures
                             && (spot.MspID >= 0 || spot.TextDbID >= 0) && !spot.Name.Contains("w/o")) {

                            }
                            else if (param.IsKeepSuggestedMetaboliteFeatures
                              && (spot.MspID >= 0 || spot.TextDbID >= 0) && spot.Name.Contains("w/o")) {

                            }
                            else {
                                continue;
                            }
                        }
                    }

                    fcSpots.Add(spot);
                }
            }
            else {
                fcSpots = cSpots;
            }

            fcSpots = fcSpots.OrderBy(n => n.MassCenter).ToList();
            if (param.IsIonMobility) {
                foreach (var spot in fcSpots) {
                    spot.AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>(spot.AlignmentDriftSpotFeatures.OrderBy(p => p.TimesCenter.Value));
                }
            }

            List<int> newIdList;
            if (param.IsIonMobility) {
                newIdList = new List<int>();
                foreach (var spot in fcSpots) {
                    newIdList.Add(spot.MasterAlignmentID);
                    foreach (var dspot in spot.AlignmentDriftSpotFeatures) {
                        newIdList.Add(dspot.MasterAlignmentID);
                    }
                }
            }
            else {
                newIdList = fcSpots.Select(x => x.AlignmentID).ToList();
            }
            var masterID = 0;
            for (int i = 0; i < fcSpots.Count; i++) {
                fcSpots[i].AlignmentID = i;
                if (param.IsIonMobility) {
                    fcSpots[i].MasterAlignmentID = masterID;
                    masterID++;
                    var driftSpots = fcSpots[i].AlignmentDriftSpotFeatures;
                    for (int j = 0; j < driftSpots.Count; j++) {
                        driftSpots[j].MasterAlignmentID = masterID;
                        driftSpots[j].AlignmentID = j;
                        driftSpots[j].ParentAlignmentID = i;
                        masterID++;
                    }
                }
            }

            //checking alignment spot variable correlations
            var rtMargin = 0.06F;
            AssignLinksByIonAbundanceCorrelations(fcSpots, rtMargin);

            // assigning peak characters from the identified spots
            AssignLinksByIdentifiedIonFeatures(fcSpots);

            // assigning peak characters from the representative file information
            fcSpots = fcSpots.OrderByDescending(spot => spot.HeightAverage).ToList();
            foreach (var fcSpot in fcSpots) {

                var repFileID = fcSpot.RepresentativeFileID;
                var repIntensity = fcSpot.AlignedPeakProperties[repFileID].PeakHeightTop;
                for (int i = 0; i < fcSpot.AlignedPeakProperties.Count; i++) {
                    var peak = fcSpot.AlignedPeakProperties[i];
                    if (peak.PeakHeightTop > repIntensity) {
                        repFileID = i;
                        repIntensity = peak.PeakHeightTop;
                    }
                }

                var repProp = fcSpot.AlignedPeakProperties[repFileID];
                var repLinks = repProp.PeakCharacter.PeakLinks;
                foreach (var rLink in repLinks) {
                    var rLinkID = rLink.LinkedPeakID;
                    var rLinkProp = rLink.Character;
                    if (rLinkProp == Common.Enum.PeakLinkFeatureEnum.Isotope) continue; // for isotope labeled tracking
                    foreach (var rSpot in fcSpots) {
                        if (rSpot.AlignedPeakProperties[repFileID].PeakID == rLinkID) {
                            if (rLinkProp == Common.Enum.PeakLinkFeatureEnum.Adduct) {
                                if (rSpot.PeakCharacter.AdductType.AdductIonName != string.Empty) continue;
                                var adductCharge = AdductIonParser.GetChargeNumber(rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName);
                                if (rSpot.PeakCharacter.Charge != adductCharge) continue;
                                adductCharge = AdductIonParser.GetChargeNumber(fcSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName);
                                if (fcSpot.PeakCharacter.Charge != adductCharge) continue;

                                RegisterLinks(fcSpot, rSpot, rLinkProp);
                                rSpot.AdductType.AdductIonName = rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName;
                                if (fcSpot.AdductType.AdductIonName == string.Empty) {
                                    fcSpot.AdductType.AdductIonName = fcSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName;
                                }
                            }
                            else {
                                RegisterLinks(fcSpot, rSpot, rLinkProp);
                            }
                            break;
                        }
                    }
                }
            }

            #region // finalize adduct features
            foreach (var fcSpot in fcSpots.Where(spot => spot.AdductType.AdductIonName == string.Empty)) {
                var chargeNum = fcSpot.PeakCharacter.Charge;
                if (param.IonMode == Common.Enum.IonMode.Positive) {
                    if (chargeNum >= 2) {
                        fcSpot.AdductType.AdductIonName = "[M+" + chargeNum + "H]" + chargeNum + "+";
                    }
                    else {
                        fcSpot.AdductType.AdductIonName = "[M+H]+";
                    }
                }
                else {
                    if (chargeNum >= 2) {
                        fcSpot.AdductType.AdductIonName = "[M-" + chargeNum + "H]" + chargeNum + "-";
                    }
                    else {
                        fcSpot.AdductType.AdductIonName = "[M-H]-";
                    }
                }
            }
            #endregion

            // assign putative group IDs
            fcSpots = fcSpots.OrderBy(n => n.AlignmentID).ToList();
            AssignPutativePeakgroupIDs(fcSpots);

            return new ObservableCollection<AlignmentSpotProperty>(fcSpots);
        }

        private static void TryMergeToMaster(AlignmentSpotProperty spot, List<AlignmentSpotProperty> cSpots, List<int> donelist, ParameterBase param) {
            var spotRt = spot.TimesCenter.Value;
            var spotMz = spot.MassCenter;

            var flg = false;
            var rtTol = param.RetentionTimeAlignmentTolerance < 0.1 ? param.RetentionTimeAlignmentTolerance : 0.1;
            foreach (var cSpot in cSpots.Where(n => Math.Abs(n.MassCenter - spotMz) < param.Ms1AlignmentTolerance)) {
                var cSpotRt = cSpot.TimesCenter.Value;
                if (Math.Abs(cSpotRt - spotRt) < rtTol * 0.5) {
                    flg = true;
                    break;
                }
            }
            if (!flg && !donelist.Contains(spot.AlignmentID)) {
                cSpots.Add(spot);
                donelist.Add(spot.AlignmentID);
            }
        }

        private static void AssignLinksByIonAbundanceCorrelations(List<AlignmentSpotProperty> alignSpots, float rtMargin) {
            if (alignSpots == null || alignSpots.Count == 0) return;
            if (alignSpots[0].AlignedPeakProperties == null || alignSpots[0].AlignedPeakProperties.Count == 0) return;

            if (alignSpots[0].AlignedPeakProperties.Count() > 9) {
                alignSpots = alignSpots.OrderBy(n => n.TimesCenter.Value).ToList();
                foreach (var spot in alignSpots) {
                    if (spot.PeakCharacter.IsotopeWeightNumber > 0) continue;
                    var spotRt = spot.TimesCenter.Value;
                    var startScanIndex = SearchCollection.LowerBound(
                        alignSpots,
                        new AlignmentSpotProperty { TimesCenter = new Common.Components.ChromXs(spotRt - rtMargin - 0.01f) },
                        (a, b) => a.TimesCenter.Value.CompareTo(b.TimesCenter.Value)
                        );

                    var searchedSpots = new List<AlignmentSpotProperty>();

                    for (int i = startScanIndex; i < alignSpots.Count; i++) {
                        if (spot.AlignmentID == alignSpots[i].AlignmentID) continue;
                        if (alignSpots[i].TimesCenter.Value < spotRt - rtMargin) continue;
                        if (alignSpots[i].PeakCharacter.IsotopeWeightNumber > 0) continue;
                        if (alignSpots[i].TimesCenter.Value > spotRt + rtMargin) break;

                        searchedSpots.Add(alignSpots[i]);
                    }

                    AlignmentSpotVariableCorrelationSearcher(spot, searchedSpots);
                }
            }
        }

        private static void AlignmentSpotVariableCorrelationSearcher(AlignmentSpotProperty spot, List<AlignmentSpotProperty> searchedSpots)
        {
            var sampleCount = spot.AlignedPeakProperties.Count;
            var spotPeaks = spot.AlignedPeakProperties;

            foreach (var searchSpot in searchedSpots) {

                var searchedSpotPeaks = searchSpot.AlignedPeakProperties;

                double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0, sqrt1 = 0, sqrt2 = 0;
                for (int i = 0; i < sampleCount; i++) {
                    sum1 += spotPeaks[i].PeakHeightTop;
                    sum2 += spotPeaks[i].PeakHeightTop;
                }
                mean1 = (double)(sum1 / sampleCount);
                mean2 = (double)(sum2 / sampleCount);

                for (int i = 0; i < sampleCount; i++) {
                    covariance += (spotPeaks[i].PeakHeightTop - mean1) * (searchedSpotPeaks[i].PeakHeightTop - mean2);
                    sqrt1 += Math.Pow(spotPeaks[i].PeakHeightTop - mean1, 2);
                    sqrt2 += Math.Pow(searchedSpotPeaks[i].PeakHeightTop - mean2, 2);
                }
                if (sqrt1 == 0 || sqrt2 == 0)
                    continue;
                else {
                    var correlation = (double)(covariance / Math.Sqrt(sqrt1 * sqrt2));
                    if (correlation >= 0.95) {
                        spot.AlignmentSpotVariableCorrelations.Add(
                            new AlignmentSpotVariableCorrelation() {
                                CorrelateAlignmentID = searchSpot.AlignmentID,
                                CorrelationScore = (float)correlation
                            });
                        spot.PeakCharacter.IsLinked = true;
                        spot.PeakCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                            LinkedPeakID = searchSpot.AlignmentID,
                            Character = Common.Enum.PeakLinkFeatureEnum.CorrelSimilar
                        });
                    }
                }
            }

            if (spot.AlignmentSpotVariableCorrelations.Count > 1)
                spot.AlignmentSpotVariableCorrelations = spot.AlignmentSpotVariableCorrelations.OrderBy(n => n.CorrelateAlignmentID).ToList();
        }

        private static void AssignLinksByIdentifiedIonFeatures(List<AlignmentSpotProperty> cSpots) {
            foreach (var cSpot in cSpots) {
                if ((cSpot.MspID >= 0 || cSpot.TextDbID >= 0) && !cSpot.Name.Contains("w/o")) {

                    var repFileID = cSpot.RepresentativeFileID;
                    var repProp = cSpot.AlignedPeakProperties[repFileID];
                    var repLinks = repProp.PeakCharacter.PeakLinks;

                    foreach (var rLink in repLinks) {
                        var rLinkID = rLink.LinkedPeakID;
                        var rLinkProp = rLink.Character;
                        if (rLinkProp == Common.Enum.PeakLinkFeatureEnum.Isotope) continue; // for isotope tracking
                        foreach (var rSpot in cSpots) {
                            if (rSpot.AlignedPeakProperties[repFileID].PeakID == rLinkID) {

                                if ((rSpot.MspID >= 0 || rSpot.TextDbID >= 0) && !rSpot.Name.Contains("w/o")) {
                                    if (rLinkProp == Common.Enum.PeakLinkFeatureEnum.Adduct) {
                                        if (cSpot.AdductType.AdductIonName == rSpot.AdductType.AdductIonName) continue;
                                        RegisterLinks(cSpot, rSpot, rLinkProp);
                                    }
                                    else {
                                        RegisterLinks(cSpot, rSpot, rLinkProp);
                                    }
                                }
                                else {
                                    if (rLinkProp == Common.Enum.PeakLinkFeatureEnum.Adduct) {
                                        var rAdductCharge = AdductIonParser.GetChargeNumber(rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName);
                                        if (rAdductCharge == rSpot.PeakCharacter.Charge) {
                                            rSpot.AdductType.AdductIonName = rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName;
                                            RegisterLinks(cSpot, rSpot, rLinkProp);
                                        }
                                    }
                                    else {
                                        RegisterLinks(cSpot, rSpot, rLinkProp);
                                    }
                                }

                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void RegisterLinks(AlignmentSpotProperty cSpot, AlignmentSpotProperty rSpot, Common.Enum.PeakLinkFeatureEnum rLinkProp) {
            if (cSpot.PeakCharacter.PeakLinks.Count(n => n.LinkedPeakID == rSpot.AlignmentID && n.Character == rLinkProp) == 0) {
                cSpot.PeakCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = rSpot.AlignmentID,
                    Character = rLinkProp
                });
                cSpot.PeakCharacter.IsLinked = true;
            }
            if (rSpot.PeakCharacter.PeakLinks.Count(n => n.LinkedPeakID == cSpot.AlignmentID && n.Character == rLinkProp) == 0) {
                rSpot.PeakCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = cSpot.AlignmentID,
                    Character = rLinkProp
                });
                rSpot.PeakCharacter.IsLinked = true;
            }
        }

        private static void AssignPutativePeakgroupIDs(List<AlignmentSpotProperty> alignedSpots) {
            var groupID = 0;
            foreach (var spot in alignedSpots) {
                if (spot.PeakCharacter.PeakGroupID >= 0) continue;
                if (spot.PeakCharacter.PeakLinks.Count == 0) {
                    spot.PeakCharacter.PeakGroupID = groupID;
                }
                else {
                    var crawledPeaks = new List<int>();
                    spot.PeakCharacter.PeakGroupID = groupID;
                    RecPeakGroupAssignment(spot, alignedSpots, groupID, crawledPeaks);
                }
                groupID++;
            }
        }

        private static void RecPeakGroupAssignment(AlignmentSpotProperty spot, List<AlignmentSpotProperty> alignedSpots, 
            int groupID, List<int> crawledPeaks) {
            if (spot.PeakCharacter.PeakLinks == null || spot.PeakCharacter.PeakLinks.Count == 0) return;

            foreach (var linkedPeak in spot.PeakCharacter.PeakLinks) {
                var linkedPeakID = linkedPeak.LinkedPeakID;
                var character = linkedPeak.Character;
                if (character == Common.Enum.PeakLinkFeatureEnum.ChromSimilar) continue;
                if (character == Common.Enum.PeakLinkFeatureEnum.CorrelSimilar) continue;
                if (character == Common.Enum.PeakLinkFeatureEnum.FoundInUpperMsMs) continue;
                if (crawledPeaks.Contains(linkedPeakID)) continue;

                alignedSpots[linkedPeakID].PeakCharacter.PeakGroupID = groupID;
                crawledPeaks.Add(linkedPeakID);

                if (isCrawledPeaks(alignedSpots[linkedPeakID].PeakCharacter.PeakLinks, crawledPeaks, spot.AlignmentID)) continue;
                RecPeakGroupAssignment(alignedSpots[linkedPeakID], alignedSpots, groupID, crawledPeaks);
            }
        }

        private static bool isCrawledPeaks(List<LinkedPeakFeature> peakLinks, List<int> crawledPeaks, int peakID) {
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

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            chromatogram.Sort(Comparer);
            return chromatogram;
        }

        private List<AlignmentSpotProperty> PackingAlignmentsToSpots(List<List<AlignmentChromPeakFeature>> alignments) {
            var results = new List<AlignmentSpotProperty>(alignments.Count);
            foreach ((var alignment, var idx) in alignments.WithIndex()) {
                var spot = DataObjConverter.ConvertFeatureToSpot(alignment);
                spot.AlignmentID = idx;
                spot.MasterAlignmentID = idx;
                spot.ParentAlignmentID = -1;
                spot.InternalStandardAlignmentID = idx;
                results.Add(spot);
            }
            return results;
        }

        private void SerializeSpotInfo(
            IReadOnlyCollection<AlignmentSpotProperty> spots, IEnumerable<string> files,
            AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer,
            ChromatogramSerializer<ChromatogramPeakInfo> peakSerializer) {
            var pss = files.Select(file => peakSerializer.DeserializeAllFromFile(file)).ToList();
            var qss = pss.Sequence();

            using (var fs = File.OpenWrite(alignmentFile.EicFilePath)) {
                spotSerializer.SerializeN(fs, spots.Zip(qss, (spot, qs) => new ChromatogramSpotInfo(qs, spot.TimesCenter)), spots.Count);
            }

            pss.ForEach(ps => ((IDisposable)ps).Dispose());
        }
    }
}
