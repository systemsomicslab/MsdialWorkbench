using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.Graphics.AxisManager
{
    class BaseSelectableLogScaleLabelGenerator : LabelGeneratorBase, ILabelGenerator
    {
        public int Base { get; }

        public BaseSelectableLogScaleLabelGenerator(int base_) {
            if (base_ <= 1) {
                throw new ArgumentException(nameof(base_));
            }
            Base = base_;
        }

        public (List<LabelTickData>, string) Generate(double low, double high, double standardLow, double standardHigh) {
            if (high <= low || standardHigh <= standardLow) {
                return (new List<LabelTickData>(), string.Empty);
            }
            if (double.IsInfinity(low) || double.IsInfinity(high) || double.IsNaN(low) || double.IsNaN(high)) {
                return (new List<LabelTickData>(), string.Empty);
            }

            var result = new List<LabelTickData>();
            var label = $"log{Base}";

            var limLow = Math.Floor(low);
            var limHigh = Math.Ceiling(high);

            result.AddRange(
                GetIntTicks((decimal)limLow, (decimal)limHigh, 1,
                    (i, interval) => new LabelTickData
                    {
                        Label = Pow(Base, (double)(i * interval)).ToString(),
                        TickType = TickType.LongTick,
                        Center = (double)(i * interval),
                        Width = (double)(interval),
                        Source = Pow(Base, (double)(i * interval)),
                    }));
            return (result, label);
        }

        private static double Pow(int base_, double x) {
            return Math.Pow(base_, x);
        }
    }
}
