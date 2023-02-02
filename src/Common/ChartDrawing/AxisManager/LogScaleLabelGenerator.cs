using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.Graphics.AxisManager
{
    class LogScaleLabelGenerator : LabelGeneratorBase, ILabelGenerator
    {
        public (List<LabelTickData>, string) Generate(double low, double high, double standardLow, double standardHigh) {
            if (high <= low || standardHigh <= standardLow) {
                return (new List<LabelTickData>(), string.Empty);
            }
            if (double.IsInfinity(low) || double.IsInfinity(high) || double.IsNaN(low) || double.IsNaN(high)) {
                return (new List<LabelTickData>(), string.Empty);
            }

            var result = new List<LabelTickData>();
            var label = "log10";

            var limLow = Math.Floor(low);
            var limHigh = Math.Ceiling(high);

            result.AddRange(
                GetIntTicks((decimal)limLow, (decimal)limHigh, 1,
                    (i, interval) => new LabelTickData
                    {
                        Label = Pow10((double)(i * interval)).ToString("e1"),
                        TickType = TickType.LongTick,
                        Center = (double)(i * interval),
                        Width = (double)(interval),
                        Source = Pow10((double)(i * interval)),
                    }));

            result.AddRange(
                GetRealTicks((decimal)(limLow + Math.Log10(5)), (decimal)(limHigh + Math.Log10(5)), 1,
                    (i, interval) => new LabelTickData
                    {
                        Label = Pow10((double)(i * interval)).ToString("e1"),
                        TickType = TickType.LongTick,
                        Center = (double)(i * interval),
                        Width = (double)(interval),
                        Source = Pow10((double)(i * interval)),
                    }));

            foreach (var v in new[] {2, 3, 4, 6, 7, 8, 9, }) {
                result.AddRange(
                    GetRealTicks((decimal)(limLow + Math.Log10(v)), (decimal)(limHigh + Math.Log10(v)), 1,
                        (i, interval) => new LabelTickData
                        {
                            Label = Pow10((double)(i * interval)).ToString("e1"),
                            TickType = TickType.ShortTick,
                            Center = (double)(i * interval),
                            Width = 0d,
                            Source = Pow10((double)(i * interval)),
                        }));
            }

            return (result, label);
        }

        private static double Pow10(double x) {
            return Math.Pow(10, x);
        }
    }
}
