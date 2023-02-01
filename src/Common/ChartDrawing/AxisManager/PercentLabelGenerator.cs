using CompMs.Graphics.Core.Base;
using System.Collections.Generic;

namespace CompMs.Graphics.AxisManager
{
    class PercentLabelGenerator : LabelGeneratorBase, ILabelGenerator
    {
        public (List<LabelTickData>, string) Generate(double low, double high, double standardLow, double standardHigh) {
            if (high <= low || standardHigh <= standardLow) {
                return (new List<LabelTickData>(), string.Empty);
            }
            if (double.IsInfinity(low) || double.IsInfinity(high) || double.IsNaN(low) || double.IsNaN(high)) {
                return (new List<LabelTickData>(), string.Empty);
            }
            var result = new List<LabelTickData>();
            var label = string.Empty;

            var delta = high - low;
            var factor = (decimal)(standardHigh - standardLow);
            decimal ld = (decimal)(low - standardLow) / factor, hd = (decimal)(high - standardLow) / factor;
            delta = (high - low) / (double)factor;

            var longInterval = GetLongInterval(delta);
            if (longInterval == 0) {
                return (result, label);
            }
            result.AddRange(
                GetIntTicks(ld, hd, longInterval,
                    (i, interval) => new LabelTickData
                    {
                        Label = (i * interval * 100).ToString("f"),
                        TickType = TickType.LongTick,
                        Center = (double)(i * interval * factor) + standardLow,
                        Width = (double)(interval * factor),
                        Source = (double)(i * interval * factor) + standardLow,
                    }));

            var shortInterval = GetShortInterval(delta, longInterval);
            if (shortInterval == 0) {
                return (result, label);
            }
            result.AddRange(
                GetIntTicks(ld, hd, shortInterval,
                    (i, interval) => new LabelTickData
                    {
                        Label = (i * interval * 100).ToString("f"),
                        TickType = TickType.ShortTick,
                        Center = (double)(i * interval * factor) + standardLow,
                        Width = 0,
                        Source = (double)(i * interval * factor) + standardLow,
                    }));
            return (result, label);
        }
    }
}