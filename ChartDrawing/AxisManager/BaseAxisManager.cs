using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.Graphics.AxisManager
{
    public abstract class BaseAxisManager : IAxisManager
    {
        public BaseAxisManager(Range range, Range bounds) {
            InitialRange = range;
            Bounds = bounds;
        }

        public BaseAxisManager(Range range) {
            InitialRange = range;
        }

        public AxisValue Min => Range.Minimum;

        public AxisValue Max => Range.Maximum;

        public Range Range {
            get => range;
            set {
                if (range != value) {
                    range = value;
                    RangeChanged?.Invoke(this, args);
                }
            }
        }
        private Range range;

        public Range InitialRange { get; }

        public Range Bounds { get; }

        public event EventHandler RangeChanged;
        private static readonly EventArgs args = new EventArgs();

        public bool Contains(AxisValue value) {
            return InitialRange.Contains(value);
        }

        public bool Contains(object obj) {
            return InitialRange.Contains(TranslateToAxisValue(obj));
        }

        public void Focus(object low, object high) {
            Range = new Range(TranslateToAxisValue(low), TranslateToAxisValue(high)).Intersect(InitialRange);
        }

        public abstract List<LabelTickData> GetLabelTicks();

        protected virtual AxisValue TranslateFromRenderPointCore(double value, double min, double max, bool isFlipped) {
            return new AxisValue(isFlipped ? max - value * (max - min) : value * (max - min) + min);
        }

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped) {
            return TranslateFromRenderPointCore(value, Min.Value, Max.Value, isFlipped);
        }

        public abstract AxisValue TranslateToAxisValue(object value);

        protected virtual double TranslateToRenderPointCore(AxisValue value, AxisValue min, AxisValue max, bool isFlipped) {
            return (isFlipped ? max.Value - value.Value : value.Value - min.Value) / (max.Value - min.Value);
        }

        public double TranslateToRenderPoint(AxisValue val, bool isFlipped) {
            return TranslateToRenderPointCore(val, Min, Max, isFlipped);
        }

        public double TranslateToRenderPoint(object value, bool isFlipped) {
            return TranslateToRenderPointCore(TranslateToAxisValue(value), Min, Max, isFlipped);
        }

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped) {
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
