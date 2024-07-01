using CompMs.Common.Mathematics.Basic;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    public enum StatsValue
    {
        Average,
        Stdev,
    }

    public interface IQuantValueAccessor
    {
        List<string> GetQuantHeaders(IReadOnlyList<AnalysisFileBean> files);
        List<string> GetStatHeaders();
        Dictionary<string, string> GetQuantValues(AlignmentSpotProperty spot);
        Dictionary<string, string> GetStatsValues(AlignmentSpotProperty spot, StatsValue stat);
    }

    public class LegacyQuantValueAccessor : IQuantValueAccessor
    {
        public LegacyQuantValueAccessor(string exportType, ParameterBase parameter) {
            this.exportType = exportType;
            this.parameter = parameter;
        }

        private readonly string exportType;
        private readonly ParameterBase parameter;

        public List<string> GetQuantHeaders(IReadOnlyList<AnalysisFileBean> files) {
            return files.OrderBy(file => file.AnalysisFileId).Select(file => file.AnalysisFileName).ToList();
        }

        public List<string> GetStatHeaders() {
            return parameter.ClassnameToOrder.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        }

        public Dictionary<string, string> GetQuantValues(AlignmentSpotProperty feature) {
            var quantValues = new Dictionary<string, string>();
            var peaks = feature.AlignedPeakProperties;
            var nonZeroMin = DataAccess.GetInterpolatedValueForMissingValue(peaks, parameter.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples, exportType);
            foreach (var peak in peaks) {
                var spotValue = DataAccess.GetSpotValueAsString(peak, exportType);
                if (nonZeroMin >= 0) {
                    double.TryParse(spotValue, out var doublevalue);
                    if (doublevalue == 0)
                        doublevalue = nonZeroMin * 0.1;
                    spotValue = doublevalue.ToString();
                }
                quantValues.Add(peak.FileName, spotValue);
            }
            return quantValues;
        }

        public Dictionary<string, string> GetStatsValues(AlignmentSpotProperty spot, StatsValue stat) {
            var groups = spot.AlignedPeakProperties.GroupBy(
                peak => parameter.FileID_ClassName[peak.FileID],
                peak => DataAccess.GetSpotValue(peak, exportType));
            switch (stat) {
                case StatsValue.Average:
                    return groups.ToDictionary(
                        group => group.Key,
                        group => BasicMathematics.Mean(group.ToArray()).ToString()
                    );
                case StatsValue.Stdev:
                    return groups.ToDictionary(
                        group => group.Key,
                        group => ReplaceNaN(BasicMathematics.Stdev(group.ToArray())).ToString()
                    );
            }
            return new Dictionary<string, string>();
        }

        private static double ReplaceNaN(double val) => double.IsNaN(val) ? 0d : val;
    }
}
