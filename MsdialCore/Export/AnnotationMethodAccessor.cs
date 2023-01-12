using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    public sealed class AnnotationMethodAccessor : IQuantValueAccessor
    {
        private static readonly string NO_PEAK_VALUE = "null";
        private static readonly string UNKNOWN_PEAK_VALUE = "unknonw";

        List<string> IQuantValueAccessor.GetQuantHeaders(IReadOnlyList<AnalysisFileBean> files) {
            return files.OrderBy(file => file.AnalysisFileId).Select(file => file.AnalysisFileName).ToList();
        }

        Dictionary<string, string> IQuantValueAccessor.GetQuantValues(AlignmentSpotProperty spot) {
            var result = new Dictionary<string, string>(spot.AlignedPeakProperties.Count);
            foreach (var peak in spot.AlignedPeakProperties) {
                if (peak.MasterPeakID < 0) {
                    result.Add(peak.FileName, NO_PEAK_VALUE);
                }
                else {
                    result.Add(peak.FileName, ToAnnotatorID(peak?.MatchResults));
                }
            }
            return result;
        }

        List<string> IQuantValueAccessor.GetStatHeaders() {
            return new List<string>(0);
        }

        Dictionary<string, string> IQuantValueAccessor.GetStatsValues(AlignmentSpotProperty spot, StatsValue stat) {
            return new Dictionary<string, string>(0);
        }

        private static string ToAnnotatorID(MsScanMatchResultContainer container) {
            if (container is null) {
                return NO_PEAK_VALUE;
            }
            if (container.Representative.IsUnknown) {
                return UNKNOWN_PEAK_VALUE;
            }
            return container.Representative.AnnotatorID;
        }
    }
}
