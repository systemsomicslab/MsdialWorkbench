using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CompMs.Graphics.AxisManager
{
    public abstract class FreezableAxisManager : Freezable, IAxisManager
    {
        #region DependencyProperty
        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register(
                nameof(Range), typeof(AxisRange), typeof(FreezableAxisManager),
                new FrameworkPropertyMetadata(
                    new AxisRange(minimum: 0, maximum: 1),
                    OnRangeChanged,
                    CoerceRange));

        public AxisRange Range {
            get => (AxisRange)GetValue(RangeProperty);
            set => SetValue(RangeProperty, value);
        }

        static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = (FreezableAxisManager)d;
            axis.ShouldCoerceLabelTicksChanged = true;
            axis.CoerceValue(LabelTicksProperty);
            axis.RangeChanged?.Invoke(axis, args);
        }

        static object CoerceRange(DependencyObject d, object value) {
            var range = (AxisRange)value;

            var axis = (FreezableAxisManager)d;
            range = range.Intersect(axis.InitialRange);
            range = range.Union(axis.Bounds);

            return range;
        }

        public event EventHandler RangeChanged;
        private static readonly EventArgs args = new EventArgs();

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register(
                nameof(Bounds), typeof(AxisRange), typeof(FreezableAxisManager),
                new PropertyMetadata(
                    null,
                    OnBoundsChanged));
        public AxisRange Bounds {
            get => (AxisRange)GetValue(BoundsProperty);
            set => SetValue(BoundsProperty, value);
        }

        static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = (FreezableAxisManager)d;
            axis.CoerceValue(RangeProperty);
        }

        public static readonly DependencyProperty InitialRangeProperty =
            DependencyProperty.Register(
                nameof(InitialRange), typeof(AxisRange), typeof(FreezableAxisManager),
                new PropertyMetadata(
                    new AxisRange(minimum: 0, maximum: 1),
                    OnInitialRangeChanged,
                    CoerceInitialRange));

        public AxisRange InitialRange {
            get => (AxisRange)GetValue(InitialRangeProperty);
            set => SetValue(InitialRangeProperty, value);
        }

        public event EventHandler InitialRangeChanged;

        static void OnInitialRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = (FreezableAxisManager)d;
            axis.Focus((AxisRange)e.NewValue);
            axis.InitialRangeChanged?.Invoke(axis, args);
        }

        static object CoerceInitialRange(DependencyObject d, object value) {
            var initial = (AxisRange)value;
            if (initial.Minimum == initial.Maximum) {
                initial = new AxisRange(initial.Minimum - 0.5, initial.Maximum + 0.5);
            }

            return initial;
        }

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

        public event EventHandler AxisValueMappingChanged;

        #endregion

        #region Property
        public AxisValue Min {
            get => Range.Minimum;
            set => Focus(new AxisRange(minimum: value, maximum: Range.Maximum));
        }

        public AxisValue Max {
            get => Range.Maximum;
            set => Focus(new AxisRange(minimum: Range.Minimum, maximum: value));
        }

        #endregion

        public void Focus(AxisRange range) {
            Range = range;
        }

        public void Reset() {
            Focus(InitialRange);
        }

        public void Recalculate(double drawableLength) {
            // TODO: Recalculate initial range
        }

        public bool Contains(AxisValue val) {
            return InitialRange.Minimum <= val && val <= InitialRange.Maximum;
        }

        public bool ContainsCurrent(AxisValue value) {
            return Range.Contains(value);
        }

        public abstract List<LabelTickData> GetLabelTicks();

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

        private List<double> TranslateToRelativePoints(IEnumerable<object> values) {
            double max = Max, min = Min;
            var result = new List<double>();

            foreach (var value in values) {
                var axVal = TranslateToAxisValue(value);
                result.Add(TranslateToRelativePointCore(axVal, min, max));
            }
            return result;
        }

        private List<double> TranslateToRelativePoints(IEnumerable<AxisValue> values) {
            double max = Max, min = Min;
            return values.Select(axVal => TranslateToRelativePointCore(axVal, min, max)).ToList();
        }

        public double TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength) {
            return FlipRelative(TranslateToRelativePointCore(value, Min, Max), isFlipped) * drawableLength;
        }

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength) {
            var results = TranslateToRelativePoints(values);
            for (var i = 0; i < results.Count; i++) {
                results[i] = FlipRelative(results[i], isFlipped) * drawableLength;
            }
            return results;
        }

        public List<double> TranslateToRenderPoints(IEnumerable<AxisValue> values, bool isFlipped, double drawableLength) {
            var results = TranslateToRelativePoints(values);
            for (var i = 0; i < results.Count; i++) {
                results[i] = FlipRelative(results[i], isFlipped) * drawableLength;
            }
            return results;
        }

        private AxisValue TranslateFromRelativePointCore(double value, double min, double max) {
            return new AxisValue(value * (max - min) + min);
        }

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength) {
            return TranslateFromRelativePointCore(FlipRelative(value / drawableLength, isFlipped), Min.Value, Max.Value);
        }
    }
}
