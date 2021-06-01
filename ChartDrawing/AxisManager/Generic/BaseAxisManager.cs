using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public abstract class BaseAxisManager<T> : IAxisManager<T>
    {
        public BaseAxisManager(Range range, Range bounds) {
            InitialRangeCore = range;
            Bounds = bounds;
            Range = InitialRange;
        }

        public BaseAxisManager(Range range, ChartMargin margin) {
            InitialRangeCore = range;
            ChartMargin = margin;
            Range = InitialRange;
        }

        public BaseAxisManager(Range range, ChartMargin margin, Range bounds) {
            InitialRangeCore = range;
            Bounds = bounds;
            ChartMargin = margin;
            Range = InitialRange;
        }

        public BaseAxisManager(Range range) {
            InitialRangeCore = range;
            Range = InitialRange;
        }

        public BaseAxisManager(BaseAxisManager<T> source) {
            InitialRangeCore = source.InitialRangeCore;
            Bounds = source.Bounds;
            Range = InitialRange;
        }

        public AxisValue Min => Range.Minimum;

        public AxisValue Max => Range.Maximum;

        public Range Range {
            get {
                return range;
            }
            set {
                var r = CoerceRange(value, Bounds);
                if (InitialRange.Contains(r)) {
                    range = r;
                    OnRangeChanged();
                }
            }
        }
        private Range range;

        protected Range InitialRangeCore { get; set; }

        public Range InitialRange {
            get => ChartMargin.Add(CoerceRange(InitialRangeCore, Bounds));
        }

        private static Range CoerceRange(Range r, Range bound) {
            var range = r.Union(bound);
            if (range.Delta <= 1e-10) {
                range = new Range(range.Minimum - 1, range.Maximum + 1);
            }
            return range;
        }

        public Range Bounds { get; protected set; }

        public ChartMargin ChartMargin { get; set; } = new ChartMargin(0, 0);

        public List<LabelTickData> LabelTicks {
            get => labelTicks ?? GetLabelTicks();
        }

        protected List<LabelTickData> labelTicks = null;

        public event EventHandler RangeChanged;

        private static readonly EventArgs args = new EventArgs();

        protected virtual void OnRangeChanged() {
            RangeChanged?.Invoke(this, args);
        }

        public void UpdateInitialRange(Range range) {
            InitialRangeCore = range;
            Range = InitialRange;
        }

        public bool Contains(AxisValue value) {
            return InitialRange.Contains(value);
        }

        public virtual bool Contains(object obj) {
            return Contains((T)obj);
        }

        public bool Contains(T obj) {
            return InitialRange.Contains(TranslateToAxisValue(obj));
        }

        public virtual void Focus(object low, object high) {
            Focus((T)low, (T)high);
        }

        public void Focus(T low, T high) {
            Range = new Range(TranslateToAxisValue(low), TranslateToAxisValue(high)).Intersect(InitialRange);
        }

        public void Focus(Range range) {
            Range = range.Intersect(InitialRange);
        }

        public abstract List<LabelTickData> GetLabelTicks();

        protected virtual AxisValue TranslateFromRenderPointCore(double value, double min, double max, bool isFlipped) {
            return new AxisValue(isFlipped ? max - value * (max - min) : value * (max - min) + min);
        }

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped) {
            return TranslateFromRenderPointCore(value, Min.Value, Max.Value, isFlipped);
        }

        public abstract AxisValue TranslateToAxisValue(T value);

        public virtual AxisValue TranslateToAxisValue(object value) {
            return TranslateToAxisValue((T)value);
        }

        protected virtual double TranslateToRenderPointCore(AxisValue value, AxisValue min, AxisValue max, bool isFlipped) {
            return (isFlipped ? max.Value - value.Value : value.Value - min.Value) / (max.Value - min.Value);
        }

        public double TranslateToRenderPoint(AxisValue val, bool isFlipped) {
            return TranslateToRenderPointCore(val, Min, Max, isFlipped);
        }

        public virtual double TranslateToRenderPoint(object value, bool isFlipped) {
            return TranslateToRenderPoint((T)value, isFlipped);
        }

        public double TranslateToRenderPoint(T value, bool isFlipped) {
            return TranslateToRenderPointCore(TranslateToAxisValue(value), Min, Max, isFlipped);
        }

        public virtual List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped) {
            return TranslateToRenderPoints(values.OfType<T>(), isFlipped);
        }

        public List<double> TranslateToRenderPoints(IEnumerable<T> values, bool isFlipped) {
            double max = Max.Value, min = Min.Value;
            var result = new List<double>();
            foreach (var value in values) {
                var axVal = TranslateToAxisValue(value);
                result.Add(TranslateToRenderPointCore(axVal, min, max, isFlipped));
            }
            return result;
        }
    }
}
