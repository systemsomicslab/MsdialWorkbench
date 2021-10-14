using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class ContinuousAxisManager<T> : BaseAxisManager<T> where T : IConvertible
    {
        public ContinuousAxisManager(Range range) : base(range) {

        }

        public ContinuousAxisManager(Range range, Range bounds) : base(range, bounds) {

        }

        public ContinuousAxisManager(Range range, IChartMargin margin) : base(range, margin) {

        }

        public ContinuousAxisManager(Range range, IChartMargin margin, Range bounds) : base(range, margin, bounds) {

        }

        public ContinuousAxisManager(T low, T high)
            : base(new Range(Convert.ToDouble(low), Convert.ToDouble(high))) {

        }

        public ContinuousAxisManager(Range range, T lowBound, T highBound)
            : base(range, new Range(Convert.ToDouble(lowBound), Convert.ToDouble(highBound))) {

        }

        public ContinuousAxisManager(T low, T high, T lowBound, T highBound)
            : base(new Range(Convert.ToDouble(low), Convert.ToDouble(high)),
                  new Range(Convert.ToDouble(lowBound), Convert.ToDouble(highBound))) {

        }

        public ContinuousAxisManager(T low, T high, Range bounds)
            : base(new Range(Convert.ToDouble(low), Convert.ToDouble(high)), bounds) {

        }

        public ContinuousAxisManager(ICollection<T> source) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max()) {

        }

        public ContinuousAxisManager(ICollection<T> source, T low, T high) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), low, high) {

        }

        public ContinuousAxisManager(ICollection<T> source, Range bounds) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), bounds) {

        }

        public LabelType LabelType {
            get => labelType;
            set => SetProperty(ref labelType, value);
        }
        private LabelType labelType = LabelType.Standard;

        private ILabelGenerator LabelGenerator {
            get {
                switch (LabelType) {
                    case LabelType.Order:
                        return labelGenerator is OrderLabelGenerator
                            ? labelGenerator
                            : labelGenerator = new OrderLabelGenerator();
                    case LabelType.Relative:
                        return labelGenerator is RelativeLabelGenerator
                            ? labelGenerator
                            : labelGenerator = new RelativeLabelGenerator();
                    case LabelType.Percent:
                        return labelGenerator is PercentLabelGenerator
                            ? labelGenerator
                            : labelGenerator = new PercentLabelGenerator();
                    case LabelType.Standard:
                    default:
                        return labelGenerator is StandardLabelGenerator
                            ? labelGenerator
                            : labelGenerator = new StandardLabelGenerator();
                }
            }
        }
        private ILabelGenerator labelGenerator;

        public void UpdateInitialRange(T low, T high) {
            UpdateInitialRange(new Range(Convert.ToDouble(low), Convert.ToDouble(high)));
        }

        public void UpdateInitialRange(ICollection<T> source) {
            UpdateInitialRange(new Range(Convert.ToDouble(source.DefaultIfEmpty().Min()), Convert.ToDouble(source.DefaultIfEmpty().Max())));
        }

        protected override void OnRangeChanged() {
            labelTicks = null;
            base.OnRangeChanged();
        }

        public override List<LabelTickData> GetLabelTicks() {
            var generator = LabelGenerator;
            var initialRangeCore = CoerceRange(InitialRangeCore, Bounds); 
            List<LabelTickData> ticks;
            (ticks, UnitLabel) = generator.Generate(Range.Minimum.Value, Range.Maximum.Value, initialRangeCore.Minimum.Value, initialRangeCore.Maximum.Value);
            return ticks;
        }

        public override AxisValue TranslateToAxisValue(T value) {
            return new AxisValue(Convert.ToDouble(value));
        }

        public static ContinuousAxisManager<T> Build<U>(IEnumerable<U> source, Func<U, T> map) {
            return new ContinuousAxisManager<T>(source.Select(map).ToList());
        }

        public static ContinuousAxisManager<T> Build<U>(IEnumerable<U> source, Func<U, T> map, T lowBound, T highBound) {
            return new ContinuousAxisManager<T>(source.Select(map).ToList(), lowBound, highBound);
        }

        public static ContinuousAxisManager<T> Build<U>(IEnumerable<U> source, Func<U, T> map, Range bound) {
            return new ContinuousAxisManager<T>(source.Select(map).ToList(), bound);
        }
    }

    public enum LabelType
    {
        Standard,
        Order,
        Relative,
        Percent,
    }

    interface ILabelGenerator
    {
        (List<LabelTickData>, string) Generate(double low, double high, double standardLow, double standardHigh);
    }

    class LabelGeneratorBase
    {
        protected static double GetExponent(double x) {
            return Math.Floor(Math.Log10(x));
        }

        protected static decimal GetLongInterval(double delta) {
            var order = (decimal)GetOrder(delta);
            if ((decimal)delta / order > 2) {
                return order;
            }
            else {
                return order / 2;
            }
        }

        protected static double GetOrder(double x) {
            return Math.Pow(10, GetExponent(x));
        }

        protected static decimal GetShortInterval(double delta, decimal longInterval) {
            if ((decimal)delta / longInterval >= 5) {
                return longInterval * (decimal)0.5;
            }
            else if ((decimal)delta / longInterval >= 2) {
                return longInterval * (decimal)0.25;
            }
            else {
                return longInterval * (decimal)0.1;
            }
        }

        protected List<LabelTickData> GetTicks(decimal lo, decimal hi, decimal interval, Func<decimal, decimal, LabelTickData> create) {
            var result = new List<LabelTickData>();
            for(var i = Math.Ceiling(lo / interval); i * interval <= hi; ++i)
            {
                result.Add(create(i, interval));
            }
            return result;
        }

        protected List<LabelTickData> GetLongTicks(decimal lo, decimal hi, decimal interval, decimal factor, string format) {
            return GetTicks(lo, hi, interval,
                (i, interval_) => new LabelTickData()
                {
                    Label = (i * interval_).ToString(format),
                    TickType = TickType.LongTick,
                    Center = (double)(i * interval_ * factor),
                    Width = (double)(interval_ * factor),
                    Source = (double)(i * interval_ * factor),
                });
        }

        protected List<LabelTickData> GetShortTicks(decimal lo, decimal hi, decimal interval, decimal factor, string format) {
            return GetTicks(lo, hi, interval,
                (i, interval_) => new LabelTickData()
                {
                    Label = (i * interval_).ToString(format),
                    TickType = TickType.ShortTick,
                    Center = (double)(i * interval_ * factor),
                    Width = 0,
                    Source = (double)(i * interval_ * factor),
                });
        }

    }

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

    class RelativeLabelGenerator : LabelGeneratorBase, ILabelGenerator
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
                GetTicks(ld, hd, longInterval,
                    (i, interval) => new LabelTickData
                    {
                        Label = (i * interval).ToString("g"),
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
                GetTicks(ld, hd, shortInterval,
                    (i, interval) => new LabelTickData
                    {
                        Label = (i * interval).ToString("g"),
                        TickType = TickType.ShortTick,
                        Center = (double)(i * interval * factor) + standardLow,
                        Width = 0,
                        Source = (double)(i * interval * factor) + standardLow,
                    }));
            return (result, label);
        }
    }

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
                GetTicks(ld, hd, longInterval,
                    (i, interval) => new LabelTickData
                    {
                        Label = (i * interval * 100).ToString("f0"),
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
                GetTicks(ld, hd, shortInterval,
                    (i, interval) => new LabelTickData
                    {
                        Label = (i * interval * 100).ToString("f0"),
                        TickType = TickType.ShortTick,
                        Center = (double)(i * interval * factor) + standardLow,
                        Width = 0,
                        Source = (double)(i * interval * factor) + standardLow,
                    }));
            return (result, label);
        }
    }
}
