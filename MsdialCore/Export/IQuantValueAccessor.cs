using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Export
{
    public interface IQuantValueAccessor
    {
        List<string> GetQuantValues(AlignmentSpotProperty spot);
    }

    [Obsolete]
    public class LegacyQuantValueAccessor : IQuantValueAccessor
    {
        public LegacyQuantValueAccessor(string exportType, ParameterBase parameter) {
            this.exportType = exportType;
            this.parameter = parameter;
        }

        private readonly string exportType;
        private readonly ParameterBase parameter;

        public List<string> GetQuantValues(AlignmentSpotProperty feature) {
            var quantValues = new List<string>();
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
                quantValues.Add(spotValue);
            }
            return quantValues;
        }
    }
}
