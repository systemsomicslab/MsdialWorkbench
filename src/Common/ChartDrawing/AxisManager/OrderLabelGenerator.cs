using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.Graphics.AxisManager
{
    class OrderLabelGenerator : LabelGeneratorBase, ILabelGenerator
    {
        public (List<LabelTickData>, string) Generate(double low, double high, double standardLow, double standardHigh) {
            if (high <= low) {
                return (new List<LabelTickData>(), string.Empty);
            }
            if (double.IsInfinity(low) || double.IsInfinity(high) || double.IsNaN(low) || double.IsNaN(high)) {
                return (new List<LabelTickData>(), string.Empty);
            }
            var result = new List<LabelTickData>();
            var label = string.Empty;

            var delta = high - low;
            var factor = (decimal)1;
            decimal ld = (decimal)low, hd = (decimal)high;
            var exp = GetExponent(Math.Max(Math.Abs(low), Math.Abs(high)));
            if (exp >= 3 || exp <= -2) {
                label = $"10^{exp}";
                factor = (decimal)Math.Pow(10, exp);
                hd /= factor;
                ld /= factor;
                delta = (high - low) / (double)factor;
            }

            var longInterval = GetLongInterval(delta);
            if (longInterval == 0) {
                return (result, label);
            }
            result.AddRange(GetLongTicks(ld, hd, longInterval, factor, "f2"));

            var shortInterval = GetShortInterval(delta, longInterval);
            if (shortInterval == 0) {
                return (result, label);
            }
            result.AddRange(GetShortTicks(ld, hd, shortInterval, factor, "f2"));
            return (result, label);
        }
    }
}
