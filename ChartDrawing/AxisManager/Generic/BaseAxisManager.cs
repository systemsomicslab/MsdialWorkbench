using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public abstract class BaseAxisManager<T> : ViewModelBase, IAxisManager<T>
    {
        public BaseAxisManager(Range range, Range bounds) {
            InitialRangeCore = InitialRange = range;
            Bounds = bounds;
            Range = InitialRange;
        }

        public BaseAxisManager(Range range, IChartMargin margin) {
            InitialRangeCore = InitialRange = range;
            ChartMargin = margin;
            Range = InitialRange;
        }

        public BaseAxisManager(Range range, IChartMargin margin, Range bounds) {
            InitialRangeCore = InitialRange = range;
            Bounds = bounds;
            ChartMargin = margin;
            Range = InitialRange;
        }

        public BaseAxisManager(Range range) {
            InitialRangeCore = InitialRange = range;
            Range = InitialRange;
        }

        public BaseAxisManager(BaseAxisManager<T> source) {
            InitialRangeCore = InitialRange = source.InitialRangeCore;
            Bounds = source.Bounds;
            Range = InitialRange;
        }

        public AxisValue Min => Range?.Minimum ?? new AxisValue(0d);

        public AxisValue Max => Range?.Maximum ?? new AxisValue(0d);

        public Range Range {
            get {
                return range;
            }
            private set {
                var r = CoerceRange(value, Bounds);
                if (InitialRange.Contains(r)) {
                    range = r;
                    OnRangeChanged();
                }
            }
        }
        private Range range;

        protected Range InitialRangeCore { get; private set; }

        public Range InitialRange { get; private set; }

        protected static Range CoerceRange(Range r, Range bound) {
            var range = r.Union(bound);
            if (range.Delta <= 1e-10) {
                range = new Range(range.Minimum - 1, range.Maximum + 1);
            }
            return range;
        }

        public Range Bounds { get; protected set; }

        public IChartMargin ChartMargin { get; set; } = new RelativeMargin(0, 0);

        public double ConstantMargin { get; set; } = 0;

        public List<LabelTickData> LabelTicks {
            get => labelTicks ?? GetLabelTicks();
        }

        protected List<LabelTickData> labelTicks = null;

        public string UnitLabel {
            get => unitLabel;
            set => SetProperty(ref unitLabel, value);
        }
        private string unitLabel = string.Empty;

        private static readonly EventArgs args = new EventArgs();

        public event EventHandler RangeChanged;

        protected virtual void OnRangeChanged() {
            RangeChanged?.Invoke(this, args);
        }

        public event EventHandler InitialRangeChanged;

        protected virtual void OnInitialRangeChanged() {
            InitialRangeChanged?.Invoke(this, args);
        }

        public void UpdateInitialRange(Range range) {
            InitialRangeCore = range;
            Focus(InitialRangeCore);
            OnInitialRangeChanged();
        }

        public bool Contains(AxisValue value) {
            return InitialRange.Contains(value);
        }

        public bool ContainsCurrent(AxisValue value) {
            return Range.Contains(value);
        }

        public void Focus(Range range) {
            Range = range.Union(Bounds).Intersect(InitialRange);
        }

        public void Recalculate(double drawableLength) {
            // var lo = -(1 + ChartMargin.Left + ChartMargin.Right) / (drawableLength - 2 * ConstantMargin) * ConstantMargin - ChartMargin.Left;
            // var hi = 1 - lo - ChartMargin.Left + ChartMargin.Right;
            (var lo, var hi) = ChartMargin.Add(0, drawableLength);
            InitialRange = TranslateFromRelativeRange(lo / drawableLength, hi / drawableLength, CoerceRange(InitialRangeCore, Bounds));
        }

        public void Reset() {
            Focus(InitialRange);
        }

        public void Reset(double drawableLength) {
            Recalculate(drawableLength);
            Focus(InitialRange);
        }

        public abstract List<LabelTickData> GetLabelTicks();

        public abstract AxisValue TranslateToAxisValue(T value);

        public virtual AxisValue TranslateToAxisValue(object value) {
            return TranslateToAxisValue((T)value);
        }

        private double TranslateRelativePointCore(AxisValue value, AxisValue min, AxisValue max) {
            return (value.Value - min.Value) / (max.Value - min.Value);
        }

        private List<double> TranslateToRelativePoints(IEnumerable<T> values) {
            double max = Max.Value, min = Min.Value;
            var result = new List<double>();
            foreach (var value in values) {
                result.Add(TranslateRelativePointCore(TranslateToAxisValue(value), min, max));
            }
            return result;
        }

        private double FlipRelative(double relative, bool isFlipped) {
            return isFlipped ? 1 - relative : relative;
        }

        public double TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength) {
            return FlipRelative(TranslateRelativePointCore(value, Min, Max), isFlipped) * drawableLength;
        }

        private AxisValue TranslateFromRelativePoint(double value) {
            return TranslateFromRelativePoint(value, Range);
        }

        private AxisValue TranslateFromRelativePoint(double value, Range range) {
            return new AxisValue(value * (range.Maximum.Value - range.Minimum.Value) + range.Minimum.Value);
        }

        private Range TranslateFromRelativeRange(double low, double hi, Range range) {
            return new Range(TranslateFromRelativePoint(low, range), TranslateFromRelativePoint(hi, range));
        }

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength) {
            return TranslateFromRelativePoint(FlipRelative(value / drawableLength, isFlipped));
        }

        public List<double> TranslateToRenderPoints(IEnumerable<T> values, bool isFlipped, double drawableLength) {
            var results = TranslateToRelativePoints(values);
            for (var i = 0; i < results.Count; i++) {
                results[i] = FlipRelative(results[i], isFlipped) * drawableLength;
            }
            return results;
        }

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength) {
            return TranslateToRenderPoints(values.OfType<T>(), isFlipped, drawableLength);
        }
    }
}
