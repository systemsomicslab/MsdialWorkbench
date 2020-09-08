using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
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
        AlignmentRefiner Refiner { get; set; }
        AlignmentProcessFactory AProcessFactory { get; set; }
        ParameterBase Param { get; set; }
        IupacDatabase Iupac { get; set; }

        public PeakAligner(PeakComparer comparer, AlignmentProcessFactory factory, AlignmentRefiner refiner, ParameterBase param, IupacDatabase iupac) {
            Comparer = comparer;
            AProcessFactory = factory;
            Refiner = refiner;
            Param = param;
            Iupac = iupac;
        }

        public AlignmentResultContainer Alignment(
            IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var master = GetMasterList(analysisFiles);
            var alignments = AlignAll(master, analysisFiles);
            var alignmentSpots = CollectPeakSpots(analysisFiles, alignmentFile, alignments, spotSerializer);

            IsotopeAnalysis(alignmentSpots);
            var alignedSpots = GetRefinedAlignmentSpotProperties(alignmentSpots);

            return PackingSpots(alignedSpots);
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

        // TODO: too slow. O(nm) 
        private void MergeChromatogramPeaks(List<IMSScanProperty> masters, List<IMSScanProperty> targets) {
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
        private List<AlignmentChromPeakFeature> AlignPeaksToMaster(IEnumerable<IMSScanProperty> peaks, IReadOnlyList<IMSScanProperty> master) {
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
            // TODO: after alignment (or after merged), temporary file should be removed
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

        private void IsotopeAnalysis(IReadOnlyList<AlignmentSpotProperty> alignmentSpots) {
            foreach (var spot in alignmentSpots) {
                if (Param.TrackingIsotopeLabels || spot.IsReferenceMatched) {
                    spot.PeakCharacter.IsotopeParentPeakID = spot.AlignmentID;
                    spot.PeakCharacter.IsotopeWeightNumber = 0;
                }
                if (!spot.IsReferenceMatched) {
                    spot.AdductType.AdductIonName = string.Empty;
                }
            }
            if (Param.TrackingIsotopeLabels) return;

            IsotopeEstimator.Process(alignmentSpots, Param, Iupac);
        }

        private List<AlignmentSpotProperty> GetRefinedAlignmentSpotProperties(List<AlignmentSpotProperty> alignmentSpots) {
            if (alignmentSpots.Count <= 1) return alignmentSpots;
            return Refiner.Refine(alignmentSpots);
        }
        private AlignmentResultContainer PackingSpots(List<AlignmentSpotProperty> alignmentSpots) {
            var minInt = (double)alignmentSpots.Min(spot => spot.HeightMin);
            var maxInt = (double)alignmentSpots.Max(spot => spot.HeightMax);

            maxInt = maxInt > 1 ? Math.Log(maxInt, 2) : 1;
            minInt = minInt > 1 ? Math.Log(minInt, 2) : 0;

            for (int i = 0; i < alignmentSpots.Count; i++) {
                var relativeValue = (float)((Math.Log(alignmentSpots[i].HeightMax, 2) - minInt) / (maxInt - minInt));
                alignmentSpots[i].RelativeAmplitudeValue = Math.Min(1, Math.Max(0, relativeValue));
            }

            var spots = new ObservableCollection<AlignmentSpotProperty>(alignmentSpots);
            return new AlignmentResultContainer {
                Ionization = Param.Ionization,
                AlignmentResultFileID = -1,
                TotalAlignmentSpotCount = spots.Count,
                AlignmentSpotProperties = spots,
            };
        }


        private List<IMSScanProperty> GetChromatogramPeakFeatures(AnalysisFileBean analysisFile, MachineCategory category) {
            if (category == MachineCategory.GCMS) {
                var msdecResults = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out int dcl_version, out List<long> seekPoints);
                msdecResults.Sort(Comparer);
                return new List<IMSScanProperty>(msdecResults);
            }
            else {
                var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
                chromatogram.Sort(Comparer);
                return new List<IMSScanProperty>(chromatogram);
            }
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
