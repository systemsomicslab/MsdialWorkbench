using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;

namespace CompMs.MsdialDimsCore
{
    public class JointAligner
    {
        public AlignmentResultContainer Joint(IReadOnlyList<(IReadOnlyList<ChromatogramPeakFeature> peaks, string file, int fileId)> chromatograms, MsdialDimsParameter param) {
            // TODO: implement Ion mobility

            var masterList = MergeChromatograms(chromatograms.Select(chromatogram => chromatogram.peaks), param.CentroidMs1Tolerance);
            if (masterList == null || masterList.Count == 0) return null;

            var aligns = AlignAll(chromatograms, masterList, param);
            var result = Packing(aligns, masterList, param);

            return result;
        }

        private List<ChromatogramPeakFeature> MergeChromatograms(IEnumerable<IEnumerable<ChromatogramPeakFeature>> chromatograms, double tolerance) {
            var merged = new List<ChromatogramPeakFeature>();
            foreach (var chromatogram in chromatograms)
                merged.AddRange(chromatogram);
            merged.Sort((a, b) => a.Mass.CompareTo(b.Mass));
            var n = merged.Count;
            var result = new List<ChromatogramPeakFeature>(n);

            var i = 0;
            while (i < n) {
                while (result.Count > 0 && i < n && merged[i].Mass - result[result.Count - 1].Mass < tolerance) ++i;
                result.Add(merged[i++]);
            }

            result.TrimExcess();
            return result;
        }

        private List<List<AlignmentChromPeakFeature>> AlignAll(
            IReadOnlyList<(IReadOnlyList<ChromatogramPeakFeature> peaks, string file, int fileId)> chromatograms,
            List<ChromatogramPeakFeature> masterList,
            MsdialDimsParameter param
            ) {
            var aligns = new List<List<AlignmentChromPeakFeature>>(masterList.Count);
            for (int i = 0; i < masterList.Count; i++)
                aligns.Add(new List<AlignmentChromPeakFeature>(chromatograms.Count));

            foreach(var chromatogram in chromatograms){
                var align = AlignFileToMaster(chromatogram.peaks, masterList, param.CentroidMs1Tolerance, chromatogram.file, chromatogram.fileId);
                for (int i = 0; i < masterList.Count; i++)
                    aligns[i].Add(align[i]);
            }
            return aligns;
        }

        private List<AlignmentChromPeakFeature> AlignFileToMaster(
            IEnumerable<ChromatogramPeakFeature> chromatogram, IReadOnlyList<ChromatogramPeakFeature> master, double tolerance,
            string file, int fileId){

            var n = master.Count;
            var result = Enumerable.Range(0, n).Select(_ => new AlignmentChromPeakFeature{ FileID = fileId, FileName = file }).ToList();

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

        private AlignmentResultContainer Packing(
            IReadOnlyList<IReadOnlyList<AlignmentChromPeakFeature>> alignments,
            IReadOnlyList<ChromatogramPeakFeature> masterList,
            MsdialDimsParameter param
            ){
            if (alignments == null || alignments.Count == 0) return new AlignmentResultContainer();

            var n = alignments.Count;
            var maxQcNumber = param.FileID_AnalysisFileType.Values.Count(value => value == CompMs.Common.Enum.AnalysisFileType.QC);
            var masterGroupCountDict = alignments[0].GroupBy(peak => param.FileID_ClassName[peak.FileID])
                                                    .ToDictionary(group => group.Key, group => group.Count());
            var result = new AlignmentResultContainer(){
                AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(),
            };

            for (int i = 0; i < alignments.Count; i++) {
                var spot = FilterAlignmentSpot(alignments[i], param, maxQcNumber, masterGroupCountDict);
                spot.AlignmentID = i;
                spot.TimesCenter = masterList[i].ChromXsTop;
                spot.MassCenter = masterList[i].Mass;
                if (spot == null) continue;
                result.AlignmentSpotProperties.Add(spot);
            }

            return result;
        }

        private AlignmentSpotProperty FilterAlignmentSpot(
            IReadOnlyCollection<AlignmentChromPeakFeature> peaks,
            MsdialDimsParameter param, int maxQcNumber, Dictionary<string, int> masterGroupCountDict
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
                // TimesCenter

                MassMax = (float)alignedPeaks.Max(peak => peak.Mass),
                MassMin = (float)alignedPeaks.Min(peak => peak.Mass),
                // MassCenter

                HeightMax = (float)alignedPeaks.Max(peak => peak.PeakHeightTop),
                HeightMin = (float)alignedPeaks.Min(peak => peak.PeakHeightTop),
                HeightAverage = (float)alignedPeaks.Average(peak => peak.PeakHeightTop),

                // SignalToNoiseMax
                // SignalToNoiseMin
                // SignalToNoiseAve

                // EstimatedNoiseMax
                // EstimatedNoiseMin
                // EstimatedNoiseAve
            };

            return result;
        }

        private double CalculateMatchFactor(double width, double tolerance){
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
