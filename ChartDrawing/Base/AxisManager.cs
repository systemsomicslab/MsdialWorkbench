using System;
using System.Collections.Generic;
using System.Windows;

using CompMs.Graphics.Base;

namespace CompMs.Graphics.Core.Base
{
    public enum TickType
    {
        LongTick, ShortTick
    }

    public class LabelTickData
    {
        public string Label { get; set; }
        public TickType TickType { get; set; }
        public double Center { get; set; }
        public double Width { get; set; }
        public object Source { get; set; }
    }

    public interface IAxisManager {
        AxisValue Min { get; }
        AxisValue Max { get; }
        Range Range { get; }
        Range InitialRange { get; }
        Range Bounds { get; }

        AxisValue TranslateToAxisValue(object value);
        double TranslateToRenderPoint(AxisValue val, bool isFlipped);
        double TranslateToRenderPoint(object value, bool isFlipped);
        List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped);
        AxisValue TranslateFromRenderPoint(double value, bool isFlipped);
        bool Contains(AxisValue value);
        bool Contains(object obj);
        void Focus(object low, object high);
        List<LabelTickData> GetLabelTicks();
    }

    public abstract class AxisManager : Freezable, IAxisManager {
        #region DependencyProperty
        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register(
                nameof(Range), typeof(Range), typeof(AxisManager),
                new PropertyMetadata(
                    new Range(minimum: 0, maximum: 1),
                    OnRangeChanged,
                    CoerceRange));

        public Range Range {
            get => (Range)GetValue(RangeProperty);
            set => SetValue(RangeProperty, value);
        }

        static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = (AxisManager)d;
            axis.LabelTicks = axis.GetLabelTicks();
            axis.AxisMapper = new AxisMapper(axis);
        }

        static object CoerceRange(DependencyObject d, object value) {
            var axis = (AxisManager)d;
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

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register(
                nameof(Bounds), typeof(Range), typeof(AxisManager),
                new PropertyMetadata(
                    null,
                    OnBoundsChanged));
        public Range Bounds {
            get => (Range)GetValue(BoundsProperty);
            set => SetValue(BoundsProperty, value);
        }

        static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = (AxisManager)d;
            axis.CoerceValue(InitialRangeProperty);
            axis.CoerceValue(RangeProperty);
        }

        public static readonly DependencyProperty InitialRangeProperty =
            DependencyProperty.Register(
                nameof(InitialRange), typeof(Range), typeof(AxisManager),
                new PropertyMetadata(
                    new Range(minimum: 0, maximum: 1),
                    OnInitialRangeChanged,
                    CoerceInitialRange));

        public Range InitialRange {
            get => (Range)GetValue(InitialRangeProperty);
            set => SetValue(InitialRangeProperty, value);
        }

        static void OnInitialRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = (AxisManager)d;
            axis.Range = (Range)e.NewValue;
        }

        static object CoerceInitialRange(DependencyObject d, object value) {
            var axis = (AxisManager)d;
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

        public static readonly DependencyProperty AxisMapperProperty =
            DependencyProperty.Register(
                nameof(AxisMapper), typeof(IAxisManager), typeof(AxisManager),
                new PropertyMetadata(null));

        public IAxisManager AxisMapper {
            get => (IAxisManager)GetValue(AxisMapperProperty);
            set => SetValue(AxisMapperProperty, value);
        }

        public static readonly DependencyProperty LabelTicksProperty =
            DependencyProperty.Register(
                nameof(LabelTicks), typeof(List<LabelTickData>), typeof(AxisManager),
                new PropertyMetadata(new List<LabelTickData>()));

        public List<LabelTickData> LabelTicks {
            get => (List<LabelTickData>)GetValue(LabelTicksProperty);
            set => SetValue(LabelTicksProperty, value);
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

        #region Method
        protected virtual double TranslateToRenderPointCore(AxisValue value, AxisValue min, AxisValue max, bool isFlipped) {
            return (isFlipped ? (max.Value - value.Value) : (value.Value - min.Value)) / (max.Value - min.Value);
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

        public virtual double TranslateToRenderPoint(AxisValue val, bool isFlipped) {
            return TranslateToRenderPointCore(val, Min, Max, isFlipped);
        }

        public virtual double TranslateToRenderPoint(object value, bool isFlipped) {
            return TranslateToRenderPoint(TranslateToAxisValue(value), isFlipped);
        }

        public virtual List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped) {
            double max = Max, min = Min;
            var result = new List<double>();

            foreach (var value in values) {
                var axVal = TranslateToAxisValue(value);
                result.Add(TranslateToRenderPointCore(axVal, min, max, isFlipped));
            }

            return result;
        }

        protected virtual AxisValue TranslateFromRenderPointCore(double value, double min, double max, bool isFlipped) {
            return new AxisValue(isFlipped ? (max - value * (max - min)) : (value * (max - min) + min));
        }

        public virtual AxisValue TranslateFromRenderPoint(double value, bool isFlipped) {
            return TranslateFromRenderPointCore(value, Min.Value, Max.Value, isFlipped);
        }

        public void Focus(object low, object high) {
            var loval = TranslateToAxisValue(low);
            var hival = TranslateToAxisValue(high);
            Range = new Range(loval, hival);
        }

        public bool Contains(AxisValue val) {
            return InitialRange.Minimum <= val && val <= InitialRange.Maximum;
        }

        public bool Contains(object obj) {
            return Contains(TranslateToAxisValue(obj));
        }
        #endregion

        public abstract List<LabelTickData> GetLabelTicks();
    }
}
