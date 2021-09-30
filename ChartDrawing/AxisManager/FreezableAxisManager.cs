using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Windows;

namespace CompMs.Graphics.AxisManager
{
    public abstract class FreezableAxisManager : Freezable, IAxisManager
    {
        #region DependencyProperty
        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register(
                nameof(Range), typeof(Range), typeof(FreezableAxisManager),
                new FrameworkPropertyMetadata(
                    new Range(minimum: 0, maximum: 1),
                    OnRangeChanged,
                    CoerceRange));

        public Range Range {
            get => (Range)GetValue(RangeProperty);
            set => SetValue(RangeProperty, value);
        }

        static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = (FreezableAxisManager)d;
            axis.ShouldCoerceLabelTicksChanged = true;
            axis.CoerceValue(LabelTicksProperty);
            axis.RangeChanged?.Invoke(axis, args);
        }

        static object CoerceRange(DependencyObject d, object value) {
            var axis = (FreezableAxisManager)d;
            var range = (Range)value;

            var initial = axis.InitialRange;
            if (initial != null) {
                if (initial.Maximum < range.Maximum)
                    range = new Range(range.Minimum, initial.Maximum);
                if (initial.Minimum > range.Minimum)
                    range = new Range(initial.Minimum, range.Maximum);
            }

            var bounds = axis.Bounds;
            if (bounds != null) {
                if (bounds.Minimum < range.Minimum)
                    range = new Range(bounds.Minimum, range.Maximum);
                if (bounds.Maximum > range.Maximum)
                    range = new Range(range.Minimum, bounds.Maximum);
            }

            return range;
        }

        public event EventHandler RangeChanged;
        private static readonly EventArgs args = new EventArgs();

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register(
                nameof(Bounds), typeof(Range), typeof(FreezableAxisManager),
                new PropertyMetadata(
                    null,
                    OnBoundsChanged));
        public Range Bounds {
            get => (Range)GetValue(BoundsProperty);
            set => SetValue(BoundsProperty, value);
        }

        static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = (FreezableAxisManager)d;
            axis.CoerceValue(InitialRangeProperty);
            axis.CoerceValue(RangeProperty);
        }

        public static readonly DependencyProperty InitialRangeProperty =
            DependencyProperty.Register(
                nameof(InitialRange), typeof(Range), typeof(FreezableAxisManager),
                new PropertyMetadata(
                    new Range(minimum: 0, maximum: 1),
                    OnInitialRangeChanged,
                    CoerceInitialRange));

        public Range InitialRange {
            get => (Range)GetValue(InitialRangeProperty);
            set => SetValue(InitialRangeProperty, value);
        }

        static void OnInitialRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = (FreezableAxisManager)d;
            axis.Range = (Range)e.NewValue;
        }

        static object CoerceInitialRange(DependencyObject d, object value) {
            var axis = (FreezableAxisManager)d;
            var initial = (Range)value;

            var bounds = axis.Bounds;
            if (bounds != null) {
                if (bounds.Minimum < initial.Minimum)
                    initial = new Range(bounds.Minimum, initial.Maximum);
                if (bounds.Maximum > initial.Maximum)
                    initial = new Range(initial.Minimum, bounds.Maximum);
            }

            if (initial.Minimum == initial.Maximum) {
                initial = new Range(initial.Minimum - 0.5, initial.Maximum + 0.5);
            }

            return initial;
        }

        public Range InitialValueRange => InitialRange;

        public static readonly DependencyProperty LabelTicksProperty =
            DependencyProperty.Register(
                nameof(LabelTicks), typeof(List<LabelTickData>), typeof(FreezableAxisManager),
                new PropertyMetadata(
                    new List<LabelTickData>(),
                    OnLabelTicksChanged,
                    CoerceLabelTicks));

        public List<LabelTickData> LabelTicks {
            get => (List<LabelTickData>)GetValue(LabelTicksProperty);
            set => SetValue(LabelTicksProperty, value);
        }

        private bool ShouldCoerceLabelTicksChanged = false;
        static void OnLabelTicksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        }

        static object CoerceLabelTicks(DependencyObject d, object value) {
            var axis = (FreezableAxisManager)d;
            if (axis.ShouldCoerceLabelTicksChanged) {
                axis.ShouldCoerceLabelTicksChanged = false;
                return axis.GetLabelTicks();
            }
            return value;
        }

        #endregion

        #region Property
        public AxisValue Min {
            get => Range.Minimum;
            set => Range = new Range(minimum: value, maximum: Range.Maximum);
        }

        public AxisValue Max {
            get => Range.Maximum;
            set => Range = new Range(minimum: Range.Minimum, maximum: value);
        }

        #endregion

        private double FlipRelative(double relative, bool isFlipped) {
            return isFlipped ? 1 - relative : relative;
        }

        private double TranslateToRelativePointCore(AxisValue value, AxisValue min, AxisValue max) {
            return (value.Value - min.Value) / (max.Value - min.Value);
        }

        public virtual AxisValue TranslateToAxisValue(object value) {
            if (value is string s) {
                if (double.TryParse(s, out var d))
                    return new AxisValue(d);
            }
            else if (value is double d) {
                return new AxisValue(d);
            }
            else if (value is IConvertible convertible) {
                return new AxisValue(Convert.ToDouble(convertible));
            }

            return new AxisValue(double.NaN);
        }

        private double TranslateToRelativePoint(AxisValue val) {
            return TranslateToRelativePointCore(val, Min, Max);
        }

        private double TranslateToRelativePoint(object value) {
            return TranslateToRelativePoint(TranslateToAxisValue(value));
        }

        private List<double> TranslateToRelativePoints(IEnumerable<object> values) {
            double max = Max, min = Min;
            var result = new List<double>();

            foreach (var value in values) {
                var axVal = TranslateToAxisValue(value);
                result.Add(TranslateToRelativePointCore(axVal, min, max));
            }

            return result;
        }

        private AxisValue TranslateFromRelativePointCore(double value, double min, double max) {
            return new AxisValue(value * (max - min) + min);
        }

        private AxisValue TranslateFromRelativePoint(double value) {
            return TranslateFromRelativePointCore(value, Min.Value, Max.Value);
        }

        public void Focus(object low, object high) {
            var loval = TranslateToAxisValue(low);
            var hival = TranslateToAxisValue(high);
            Range = new Range(loval, hival);
        }

        public void Focus(Range range) {
            Range = range;
        }

        public bool Contains(AxisValue val) {
            return InitialRange.Minimum <= val && val <= InitialRange.Maximum;
        }

        public bool Contains(object obj) {
            return Contains(TranslateToAxisValue(obj));
        }

        public abstract List<LabelTickData> GetLabelTicks();

        public double TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength) {
            return FlipRelative(TranslateToRelativePoint(value), isFlipped) * drawableLength;
        }

        public double TranslateToRenderPoint(object value, bool isFlipped, double drawableLength) {
            return FlipRelative(TranslateToRelativePoint(value), isFlipped) * drawableLength;
        }

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength) {
            var results = TranslateToRelativePoints(values);
            for (var i = 0; i < results.Count; i++) {
                results[i] = FlipRelative(results[i], isFlipped) * drawableLength;
            }
            return results;
        }

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength) {
            return TranslateFromRelativePoint(FlipRelative(value / drawableLength, isFlipped));
        }
    }
}
