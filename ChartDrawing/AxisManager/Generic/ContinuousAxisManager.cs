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

        public ContinuousAxisManager(Range range, ChartMargin margin) : base(range, margin) {

        }

        public ContinuousAxisManager(Range range, ChartMargin margin, Range bounds) : base(range, margin, bounds) {

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
            if (labelTicks != null) return labelTicks;

            labelTicks = new List<LabelTickData>();

            if (Min >= Max || double.IsNaN(Min) || double.IsNaN(Max)) return labelTicks;
            var TickInterval = (decimal)Math.Pow(10, Math.Floor(Math.Log10(Max - Min)));
            if (TickInterval == 0) return labelTicks;
            var fold = (decimal)(Max - Min) / TickInterval;
            if (fold <= 2) {
                TickInterval /= 2;
                fold *= 2;
            }
            decimal shortTickInterval =
                TickInterval * (decimal)(fold >= 5 ? 0.5 :
                                         fold >= 2 ? 0.25 :
                                                     0.1);

            var exp = Math.Floor(Math.Log10(Max));
            // var LabelFormat = exp >= 3 ? "0.00e0" : exp < 0 ? "0.0e0" : TickInterval >= 1 ? "f0" : "f3";
            var LabelFormat = TickInterval >= 1 ? "f0" : "f3";
            for(var i = Math.Ceiling((decimal)Min.Value / TickInterval); i * TickInterval <= (decimal)Max.Value; ++i)
            {
                var item = new LabelTickData()
                {
                    Label = (i * TickInterval).ToString(LabelFormat),
                    TickType = TickType.LongTick,
                    Center = (double)(i * TickInterval),
                    Width = (double)TickInterval,
                    Source = (double)(i * TickInterval),
                };
                labelTicks.Add(item);
            }

            if (shortTickInterval == 0) return labelTicks;
            for(var i = Math.Ceiling((decimal)Min.Value / shortTickInterval); i * shortTickInterval <= (decimal)Max.Value; ++i)
            {
                var item = new LabelTickData()
                {
                    Label = (i * shortTickInterval).ToString(LabelFormat), 
                    TickType = TickType.ShortTick,
                    Center = (double)(i * shortTickInterval),
                    Width = 0,
                    Source = (double)(i * shortTickInterval),
                };
                labelTicks.Add(item);
            }

            return labelTicks;
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
}
