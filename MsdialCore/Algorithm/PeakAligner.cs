using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Extension;
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

        public AlignmentResultContainer Alignment(
            IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var master = GetMasterList(analysisFiles);
            var alignments = AlignAll(master, analysisFiles);
            var alignmentSpots = CollectPeakSpots(analysisFiles, alignmentFile, alignments, spotSerializer);
            var alignmentResult = PackingSpots(alignmentSpots);

            IsotopeAnalysis(alignmentResult);

            return alignmentResult;
        }

        protected virtual List<ChromatogramPeakFeature> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles) {

            var referenceId = Param.AlignmentReferenceFileID;
            var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
            if (referenceFile == null) return new List<ChromatogramPeakFeature>();

            var master = GetChromatogramPeakFeatures(referenceFile);
            foreach (var analysisFile in analysisFiles) {
                if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId)
                    continue;
                var target = GetChromatogramPeakFeatures(analysisFile);
                MergeChromatogramPeaks(master, target);
            }

            return master;
        }

        // TODO: too slow. O(n2) 
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
            IReadOnlyList<ChromatogramPeakFeature> master, IReadOnlyList<AnalysisFileBean> analysisFiles) {

            var result = new List<List<AlignmentChromPeakFeature>>(analysisFiles.Count);

            foreach (var analysisFile in analysisFiles) {
                var chromatogram = GetChromatogramPeakFeatures(analysisFile);
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
                    result[matchIdx.Value] = DataObjConverter.ConvertToAlignmentChromPeakFeature(peak);
            }

            return result;
        }

        private List<AlignmentSpotProperty> CollectPeakSpots(IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            List<List<AlignmentChromPeakFeature>> alignments, ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var aligns = new List<List<AlignmentChromPeakFeature>>();
            var files = new List<string>();
            var chromPeakInfoSerializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPSTMP");
            foreach ((var analysisFile, var alignment) in analysisFiles.Zip(alignments)) {
                (var peaks, var file) = CollectAlignmentPeaks(analysisFile, alignment, alignments, chromPeakInfoSerializer);
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

        private (List<AlignmentChromPeakFeature>, string) CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks,
            List<List<AlignmentChromPeakFeature>> alignments,
            ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {

            var results = new List<AlignmentChromPeakFeature>();
            var peakInfos = new List<ChromatogramPeakInfo>();
            using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePath, 0, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt)) {
                var spectra = DataAccess.GetAllSpectra(rawDataAccess);
                foreach ((var peak, var alignment) in peaks.Zip(alignments)) {
                    var align = peak;
                    if (align == null) {
                        align = GapFilling(spectra, alignment);
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

        private AlignmentChromPeakFeature GapFilling(List<RawSpectrum> spectra, List<AlignmentChromPeakFeature> alignment) {
            return GapFiller.GapFilling(
                spectra, Comparer.GetCenter(alignment.Select(x => x.ChromXsTop)),
                Param.Ms1AlignmentTolerance, (float)alignment.Average(x => x.Mass),
                Param.IonMode, Param.SmoothingMethod, Param.SmoothingLevel,
                Param.IsForceInsertForGapFilling);
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
