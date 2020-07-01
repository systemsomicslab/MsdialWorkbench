using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Parameter;

namespace CompMs.MsdialDimsCore
{
    public class ProcessJointAligner
    {
        public AlignmentResultContainer Join(IReadOnlyList<AnalysisFileBean> files, ParameterBase param) {
            // TODO: implement Ion mobility

            var tolerance = param.Ms1AlignmentTolerance;
            var masterList = MergeFiles(files, tolerance, param.AlignmentReferenceFileID);
            if (masterList == null || masterList.Count == 0) return null;

            var aligns = AlignAll(files, masterList, tolerance);
            var result = Packing(aligns, masterList, param);

            return result;
        }

        private List<ChromatogramPeakFeature> MergeFiles(IEnumerable<AnalysisFileBean> files, double tolerance, int refId) {
            var analysisFiles = new List<AnalysisFileBean>(files);
            var master = GetMasterPeakList(analysisFiles.FirstOrDefault(file => file.AnalysisFileId == refId));

            master.AddFirst(new ChromatogramPeakFeature { Mass = double.MinValue }); // Add Sentinel
            master.AddLast(new ChromatogramPeakFeature { Mass = double.MaxValue });  // Add Sentinel

            foreach (var file in analysisFiles) {
                if (file.AnalysisFileId == refId) continue;

                var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
                chromatogram.Sort((a, b) => a.Mass.CompareTo(b.Mass));

                MergeChromatogram(master, chromatogram, tolerance);
            }

            master.RemoveFirst(); // Remove Sentinel
            master.RemoveLast();  // Remove Sentinel

            return new List<ChromatogramPeakFeature>(master);
        }

        private List<List<AlignmentChromPeakFeature>> AlignAll(
            IReadOnlyCollection<AnalysisFileBean> files,
            IReadOnlyList<ChromatogramPeakFeature> masterList,
            double tolerance
            ) {
            var aligns = new List<List<AlignmentChromPeakFeature>>(masterList.Count);
            for (int i = 0; i < masterList.Count; i++)
                aligns.Add(new List<AlignmentChromPeakFeature>(files.Count));
            
            foreach(var file in files){
                var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
                var align = AlignFileToMaster(chromatogram, masterList, tolerance, file.AnalysisFileName, file.AnalysisFileId);
                for (int i = 0; i < masterList.Count; i++)
                    aligns[i].Add(align[i]);
            }
            return aligns;
        }

        private AlignmentResultContainer Packing(
            IReadOnlyList<IReadOnlyList<AlignmentChromPeakFeature>> alignments,
            IReadOnlyList<ChromatogramPeakFeature> masterList,
            ParameterBase param
            ){
            if (alignments == null || alignments.Count == 0) return new AlignmentResultContainer();

            var n = alignments.Count;
            var maxQcNumber = param.FileID_AnalysisFileType.Values.Count(value => value == CompMs.Common.Enum.AnalysisFileType.QC);
            var masterGroupCountDict = alignments[0].GroupBy(peak => param.FileID_ClassName[peak.FileID])
                                                    .ToDictionary(group => group.Key, group => group.Count());
            var result = new AlignmentResultContainer(){
                AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(),
            };

            var masterId = 0;
            for (int i = 0; i < alignments.Count; i++) {
                var spot = FilterAlignmentSpot(alignments[i], param, maxQcNumber, masterGroupCountDict);
                spot.AlignmentID = i;
                spot.MasterAlignmentID = masterId++;
                if (spot == null) continue;
                result.AlignmentSpotProperties.Add(spot);
            }

            return result;
        }

        private void MergeChromatogram(LinkedList<ChromatogramPeakFeature> master, IEnumerable<ChromatogramPeakFeature> sub, double tolerance) {
            var itr = master.First;
            foreach (var peak in sub) {
                while (itr.Next.Value.Mass < peak.Mass) itr = itr.Next;
                if (itr.Value.Mass + tolerance < peak.Mass && peak.Mass < itr.Next.Value.Mass - tolerance)
                    master.AddAfter(itr, peak);
            }
        }

        private LinkedList<ChromatogramPeakFeature> GetMasterPeakList(AnalysisFileBean file) {
            if (file == null) return new LinkedList<ChromatogramPeakFeature>();
            var master = MsdialSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
            master.Sort((a, b) => a.Mass.CompareTo(b.Mass));
            return new LinkedList<ChromatogramPeakFeature>(master);
        }

        private List<AlignmentChromPeakFeature> AlignFileToMaster(
            IEnumerable<ChromatogramPeakFeature> chromatogram, IReadOnlyList<ChromatogramPeakFeature> master, double tolerance,
            string filename, int fileId){

            var n = master.Count;
            var result = Enumerable.Range(0, n).Select(_ => new AlignmentChromPeakFeature{
                MasterPeakID = -1, PeakID = -1,
                FileID = fileId, FileName = filename
            }).ToList();

            var target = new ChromatogramPeakFeature();
            var maxFactors = new double[n];
            foreach(var chrom in chromatogram){
                int matchIdx = -1;
                double maxMatch = double.MinValue;

                target.Mass = chrom.Mass - tolerance;
                var idx = SearchCollection.LowerBound(master, target, (a, b) => a.Mass.CompareTo(b.Mass));
                while (idx < n && master[idx].Mass < chrom.Mass + tolerance){
                    var factor = CalculateMatchFactor(chrom.Mass - master[idx].Mass, tolerance);                   
                    if (factor > maxMatch && factor > maxFactors[idx]){
                        maxMatch = factor;
                        matchIdx = idx;
                    }
                }
                if (matchIdx == -1) continue;
                maxFactors[matchIdx] = maxMatch;

                SetAlignmentChromPeakFeature(result[matchIdx], chrom);
            }
            return result;
        }

        private AlignmentSpotProperty FilterAlignmentSpot(
            IReadOnlyCollection<AlignmentChromPeakFeature> peaks,
            ParameterBase param, int maxQcNumber, IReadOnlyDictionary<string, int> masterGroupCountDict
            ){

            var alignedPeaks = peaks.Where(peak => peak.PeakID >= 0).ToArray();

            var peakCount = alignedPeaks.Length;
            if (peakCount == 0) return null;
            if (peakCount * 100F / peaks.Count < param.PeakCountFilter) return null;

            var qcCount = alignedPeaks.Count(peak => param.FileID_AnalysisFileType[peak.FileID] == CompMs.Common.Enum.AnalysisFileType.QC);
            if (maxQcNumber != qcCount) return null;

            var localGroupCountDict = alignedPeaks.GroupBy(peak => param.FileID_ClassName[peak.FileID])
                                                    .ToDictionary(group => group.Key, group => group.Count());
            var isNPerCentDetectedAtOneGroup = false;
            foreach (var pair in localGroupCountDict) {
                var id = pair.Key;
                var count = pair.Value;
                var totalCount = masterGroupCountDict[id];
                if (count * 100F / totalCount >= param.NPercentDetectedInOneGroup) {
                    isNPerCentDetectedAtOneGroup = true;
                    break;
                }
            }
            if (!isNPerCentDetectedAtOneGroup) return null;

            var result = new AlignmentSpotProperty() {
                PeakWidthAverage = (float)alignedPeaks.Average(peak => peak.PeakWidth(ChromXType.Mz)),

                TimesMax = alignedPeaks.Max(peak => (peak.ChromXsTop.Value, peak.ChromXsTop)).ChromXsTop,
                TimesMin = alignedPeaks.Min(peak => (peak.ChromXsTop.Value, peak.ChromXsTop)).ChromXsTop,

                MassMax = (float)alignedPeaks.Max(peak => peak.Mass),
                MassMin = (float)alignedPeaks.Min(peak => peak.Mass),

                HeightMax = (float)alignedPeaks.Max(peak => peak.PeakHeightTop),
                HeightMin = (float)alignedPeaks.Min(peak => peak.PeakHeightTop),
                HeightAverage = (float)alignedPeaks.Average(peak => peak.PeakHeightTop),

                SignalToNoiseMax = alignedPeaks.Max(peak => peak.PeakShape.SignalToNoise),
                SignalToNoiseMin = alignedPeaks.Min(peak => peak.PeakShape.SignalToNoise),
                SignalToNoiseAve = alignedPeaks.Average(peak => peak.PeakShape.SignalToNoise),

                EstimatedNoiseMax = alignedPeaks.Max(peak => peak.PeakShape.EstimatedNoise),
                EstimatedNoiseMin = alignedPeaks.Min(peak => peak.PeakShape.EstimatedNoise),
                EstimatedNoiseAve = alignedPeaks.Average(peak => peak.PeakShape.EstimatedNoise),

                IonMode = param.IonMode,
                FillParcentage = peaks.Count / (float)alignedPeaks.Length,
                MonoIsotopicPercentage = alignedPeaks.Count(peak => peak.PeakCharacter.IsotopeWeightNumber == 0) / (float)alignedPeaks.Length,
            };
            result.TimesCenter = new ChromXs(alignedPeaks.Average(peak => peak.ChromXsTop.Value), alignedPeaks[0].ChromXsTop.Type, alignedPeaks[0].ChromXsTop.Unit);
            result.MassCenter = alignedPeaks.Max(peak => (peak.ChromXsTop.Value, peak.Mass)).Mass;

            return result;
        }

        private static double CalculateMatchFactor(double width, double tolerance){
            return Math.Exp(-.5 * Math.Pow(width, 2) / Math.Pow(tolerance, 2));
        }

        private void SetAlignmentChromPeakFeature(AlignmentChromPeakFeature alignment, ChromatogramPeakFeature feature){
            alignment.MasterPeakID = feature.MasterPeakID;
            alignment.PeakID = feature.PeakID;
            alignment.ParentPeakID = feature.ParentPeakID;
            alignment.DeconvolutionID = feature.DeconvolutionID;
            alignment.MS1RawSpectrumID = feature.ScanID;
            alignment.MS1RawSpectrumIDatAccumulatedMS1 = feature.MS1AccumulatedMs1RawSpectrumIdTop;
            alignment.MS2RawSpectrumID = feature.MS2RawSpectrumID;
            alignment.MS2RawSpectrumIDs = feature.MS2RawSpectrumIDs;
            alignment.ChromScanIdLeft = feature.ChromScanIdLeft;
            alignment.ChromScanIdRight = feature.ChromScanIdRight;
            alignment.ChromScanIdTop = feature.ChromScanIdTop;
            alignment.ChromXsLeft = feature.ChromXsLeft;
            alignment.ChromXsTop = feature.ChromXsTop;
            alignment.ChromXsRight = feature.ChromXsRight;
            alignment.PeakHeightLeft = feature.PeakHeightLeft;
            alignment.PeakHeightTop = feature.PeakHeightTop;
            alignment.PeakHeightRight = feature.PeakHeightRight;
            alignment.PeakAreaAboveZero = feature.PeakAreaAboveZero;
            alignment.PeakAreaAboveBaseline = feature.PeakAreaAboveBaseline;
            alignment.Mass = feature.Mass;
            alignment.PrecursorMz = feature.PrecursorMz;
            alignment.IonMode = feature.IonMode;
            alignment.Name = feature.Name;
            alignment.Formula = feature.Formula;
            alignment.Ontology = feature.Ontology;
            alignment.SMILES = feature.SMILES;
            alignment.InChIKey = feature.InChIKey;
            alignment.CollisionCrossSection = feature.CollisionCrossSection;
            alignment.MspID = feature.MspID;
            alignment.MspIDs = feature.MspIDs;
            alignment.TextDbID = feature.TextDbID;
            alignment.TextDbIDs = feature.TextDbIDs;
            alignment.MspBasedMatchResult = feature.MspBasedMatchResult;
            alignment.TextDbBasedMatchResult = feature.TextDbBasedMatchResult;
            alignment.PeakCharacter = feature.PeakCharacter;
            alignment.PeakShape = feature.PeakShape;
        }
    }
}
