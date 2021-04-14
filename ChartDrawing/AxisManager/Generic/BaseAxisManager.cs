using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public abstract class BaseAxisManager<T> : IAxisManager<T>
    {
        public BaseAxisManager(Range range, Range bounds) {
            InitialRange = range;
            Bounds = bounds;
        }

        public BaseAxisManager(Range range) {
            InitialRange = range;
        }

        public BaseAxisManager(BaseAxisManager<T> source) {
            InitialRange = source.InitialRange;
            Bounds = source.Bounds;
        }

        public AxisValue Min => Range.Minimum;

        public AxisValue Max => Range.Maximum;

        public Range Range {
            get => range;
            set {
                if (range != value) {
                    range = value;
                    OnRangeChanged();
                }
            }
        }
        private Range range;

        public Range InitialRange { get; protected set; }

        public Range Bounds { get; protected set; }

        public List<LabelTickData> LabelTicks {
            get => labelTicks ?? GetLabelTicks();
        }

        protected List<LabelTickData> labelTicks = null;

        public event EventHandler RangeChanged;

        private static readonly EventArgs args = new EventArgs();

        protected virtual void OnRangeChanged() {
            RangeChanged?.Invoke(this, args);
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
            return TranslateToRenderPoints(values.Cast<T>(), isFlipped);
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
