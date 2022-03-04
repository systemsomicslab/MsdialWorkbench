using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.Graphics.AxisManager
{
    class StandardLabelGenerator : LabelGeneratorBase, ILabelGenerator
    {
        public (List<LabelTickData>, string) Generate(double low, double high, double standardLow, double standardHigh) {
            if (low > high) {
                return (new List<LabelTickData>(), string.Empty);
            }
            if (double.IsInfinity(low) || double.IsInfinity(high) || double.IsNaN(low) || double.IsNaN(high)) {
                return (new List<LabelTickData>(), string.Empty);
            }

            var result = new List<LabelTickData>();
            var longInterval = GetLongInterval(high - low);

            if (longInterval == 0) return (result, string.Empty);

            var exp = GetExponent(Math.Max(Math.Abs(high), Math.Abs(low)));
            var format = exp > 3 ? "0.00e0" : exp < -2 ? "0.0e0" : longInterval >= 1 ? "f0" : "f3";
            result.AddRange(GetLongTicks((decimal)low, (decimal)high, longInterval, 1, format));

            var shortTickInterval = GetShortInterval(high - low, longInterval);
            if (shortTickInterval == 0) return (result, string.Empty);
            result.AddRange(GetShortTicks((decimal)low, (decimal)high, shortTickInterval, 1, format));

            return (result, string.Empty);
        }
    }
}