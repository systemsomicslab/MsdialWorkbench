using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public class CompositeFilter : ISpotFilter
    {
        public IList<ISpotFilter> Filters { get; } = new List<ISpotFilter>();
        public IEnumerable<AlignmentSpotProperty> Filter(IEnumerable<AlignmentSpotProperty> spots) {
            foreach (var filter in Filters) {
                spots = filter.Filter(spots);
            }
            return spots;
        }
    }

    public class PeakCountFilter : ISpotFilter
    {
        private readonly double threshold;
        public PeakCountFilter(double threshold) {
            this.threshold = threshold;
        }

        public IEnumerable<AlignmentSpotProperty> Filter(IEnumerable<AlignmentSpotProperty> spots) {
            foreach (var spot in spots) {
                if (spot.AlignedPeakProperties.Count(peak => peak.MasterPeakID >= 0) >= threshold) {
                    yield return spot;
                }
            }
        }
    }

    public class QcFilter : ISpotFilter
    {
        private readonly IReadOnlyDictionary<int, AnalysisFileType> FileID_AnalysisFileType; 

        public QcFilter(IReadOnlyDictionary<int, AnalysisFileType> fileID_analysisFileType) {
            FileID_AnalysisFileType = fileID_analysisFileType;
        }

        public IEnumerable<AlignmentSpotProperty> Filter(IEnumerable<AlignmentSpotProperty> spots) {
            foreach (var spot in spots) {
                var satisfied = spot.AlignedPeakProperties
                    .All(peak => FileID_AnalysisFileType[peak.FileID] != AnalysisFileType.QC
                              || peak.MasterPeakID >= 0);
                if (satisfied)
                    yield return spot;
            }
        }
    }

    public class DetectedNumberFilter : ISpotFilter
    {
        private readonly IReadOnlyDictionary<int, string> FileID_ClassName;
        private readonly IReadOnlyDictionary<string, double> Group_Threshold;
        public DetectedNumberFilter(IReadOnlyDictionary<int, string> FileID_ClassName, double threshold) {
            this.FileID_ClassName = FileID_ClassName;
            Group_Threshold = FileID_ClassName.Values.GroupBy(v => v).ToDictionary(group => group.Key, group => (group.Count() * threshold));
        }

        public IEnumerable<AlignmentSpotProperty> Filter(IEnumerable<AlignmentSpotProperty> spots) {
            foreach (var spot in spots) {
                var satisfied = spot.AlignedPeakProperties
                    .GroupBy(peak => FileID_ClassName[peak.FileID])
                    .Any(group => Group_Threshold[group.Key] <= group.Count(peak => peak.MasterPeakID >= 0));
                if (satisfied)
                    yield return spot;
            }
        }
    }

    public class BlankFilter : ISpotFilter
    {
        private static readonly double MINIMUM_PEAK_HEIGHT = 0.0001;

        private readonly Dictionary<int, AnalysisFileType> FileID2AnalysisFileType;
        private readonly float FoldChangeForBlankFiltering;
        private readonly BlankFiltering BlankFiltering;
        private readonly bool IsKeepRefMatchedMetaboliteFeatures;
        private readonly bool IsKeepSuggestedMetaboliteFeatures;
        private readonly bool IsKeepRemovableFeaturesAndAssignedTagForChecking;
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public BlankFilter(
            Dictionary<int, AnalysisFileType> FileID2AnalysisFileType_,
            float FoldChangeForBlankFiltering_,
            BlankFiltering BlankFiltering_,
            bool IsKeepRefMatchedMetaboliteFeatures_,
            bool IsKeepSuggestedMetaboliteFeatures_,
            bool IsKeepRemovableFeaturesAndAssignedTagForChecking_,
            IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            FileID2AnalysisFileType = FileID2AnalysisFileType_;
            FoldChangeForBlankFiltering = FoldChangeForBlankFiltering_;
            BlankFiltering = BlankFiltering_;
            IsKeepRefMatchedMetaboliteFeatures = IsKeepRefMatchedMetaboliteFeatures_;
            IsKeepSuggestedMetaboliteFeatures = IsKeepSuggestedMetaboliteFeatures_;
            IsKeepRemovableFeaturesAndAssignedTagForChecking = IsKeepRemovableFeaturesAndAssignedTagForChecking_;
            this.evaluator = evaluator;
        }

        public IEnumerable<AlignmentSpotProperty> Filter(IEnumerable<AlignmentSpotProperty> spots) {
            var blankExists = FileID2AnalysisFileType.Values.Any(v => v == AnalysisFileType.Blank);
            if (!blankExists) return spots;
            return FilterCore(spots);           
        }

        private IEnumerable<AlignmentSpotProperty> FilterCore(IEnumerable<AlignmentSpotProperty> spots) {
            foreach (var spot in spots) {
                var blankAve = spot.AlignedPeakProperties
                    .Where(peak => FileID2AnalysisFileType[peak.FileID] == AnalysisFileType.Blank)
                    .DefaultIfEmpty()
                    .Average(peak => peak?.PeakHeightTop) ?? 0d;
                if (blankAve == 0) {
                    var nonMinValue = spot.AlignedPeakProperties
                        .Where(peak => peak.PeakHeightTop < MINIMUM_PEAK_HEIGHT)
                        .DefaultIfEmpty()
                        .Min(peak => peak?.PeakHeightTop);
                    blankAve = nonMinValue * 0.1 ?? 1.0;
                }
                var blankThresh = blankAve * FoldChangeForBlankFiltering;

                var samplePeaks = spot.AlignedPeakProperties
                    .Where(peak => FileID2AnalysisFileType[peak.FileID] == AnalysisFileType.Sample)
                    .DefaultIfEmpty();
                var sampleThresh = BlankFiltering == BlankFiltering.SampleMaxOverBlankAve
                    ? samplePeaks.Max(peak => peak?.PeakHeightTop) ?? 0d
                    : samplePeaks.Average(peak => peak?.PeakHeightTop) ?? 0d;
            
                if (sampleThresh < blankThresh) {

                    if (IsKeepRefMatchedMetaboliteFeatures && spot.IsReferenceMatched(evaluator)) {

                    }
                    else if (IsKeepSuggestedMetaboliteFeatures && spot.IsAnnotationSuggested(evaluator)) {

                    }
                    else if (IsKeepRemovableFeaturesAndAssignedTagForChecking) {
                        spot.FeatureFilterStatus.IsBlankFiltered = true;
                    }
                    else {
                        continue;
                    }
                }
                yield return spot;
            }
        }
    }
}
