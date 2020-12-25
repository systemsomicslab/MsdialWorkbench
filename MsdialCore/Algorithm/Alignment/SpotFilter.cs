using CompMs.Common.Enum;
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
        private readonly IReadOnlyDictionary<string, int> Group_Threshold;
        public DetectedNumberFilter(IReadOnlyDictionary<int, string> FileID_ClassName, double threshold) {
            this.FileID_ClassName = FileID_ClassName;
            Group_Threshold = FileID_ClassName.Values.GroupBy(v => v).ToDictionary(group => group.Key, group => (int)(group.Count() * threshold));
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
        public IEnumerable<AlignmentSpotProperty> Filter(IEnumerable<AlignmentSpotProperty> spots) {
            throw new NotImplementedException();
        }
    }
}
