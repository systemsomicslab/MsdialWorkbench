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

    public abstract class AxisManager : DependencyObject {
        #region DependencyProperty
        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            nameof(Range), typeof(Range), typeof(AxisManager),
            new PropertyMetadata(new Range(minimum: 0, maximum: 1), OnRangeChanged, CoerceRangeValue)
            );

        public static readonly DependencyProperty BoundsProperty = DependencyProperty.Register(
            nameof(Bounds), typeof(Range), typeof(AxisManager),
            new PropertyMetadata(null, OnBoundsChanged)
            );

        public static readonly DependencyProperty InitialRangeProperty = DependencyProperty.Register(
            nameof(InitialRange), typeof(Range), typeof(AxisManager),
            new PropertyMetadata(new Range(minimum: 0, maximum: 1), OnInitialRangeChanged)
            );

        public static readonly DependencyProperty AxisMapperProperty = DependencyProperty.Register(
            nameof(AxisMapper), typeof(AxisMapper), typeof(AxisManager),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty LabelTicksProperty = DependencyProperty.Register(
            nameof(LabelTicks), typeof(List<LabelTickData>), typeof(AxisManager),
            new PropertyMetadata(new List<LabelTickData>())
            );
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

        public Range Range {
            get => (Range)GetValue(RangeProperty);
            set => SetValue(RangeProperty, value);
        }

        public Range Bounds {
            get => (Range)GetValue(BoundsProperty);
            set => SetValue(BoundsProperty, value);
        }

        public Range InitialRange {
            get => (Range)GetValue(InitialRangeProperty);
            set => SetValue(InitialRangeProperty, value);
        }

        public AxisMapper AxisMapper {
            get => (AxisMapper)GetValue(AxisMapperProperty);
            set => SetValue(AxisMapperProperty, value);
        }

        public List<LabelTickData> LabelTicks {
            get => (List<LabelTickData>)GetValue(LabelTicksProperty);
            set => SetValue(LabelTicksProperty, value);
        }
        #endregion

        #region Method
        protected virtual double TranslateToRenderPointCore(AxisValue value, AxisValue min, AxisValue max, bool isFlipped) {
            return (isFlipped ? (max.Value - value.Value) : (value.Value - min.Value)) / (max.Value - min.Value);
        }

        public virtual AxisValue TranslateToAxisValue(object value) {
            if (value is double d)
                return new AxisValue(d);
            else if (value is IConvertible convertible)
                return new AxisValue(Convert.ToDouble(convertible));
            else
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
        #endregion

        #region Event
        static object CoerceRangeValue(DependencyObject d, object value) {
            var axis = d as AxisManager;

            var bounds = axis.Bounds;
            if (bounds == null || !(value is Range range)) return value;
            if (bounds.Minimum < range.Minimum) range.Minimum = bounds.Minimum;
            if (bounds.Maximum > range.Maximum) range.Maximum = bounds.Maximum;
            return range;
        }

        static void OnInitialRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is AxisManager axis)
                axis.Range = (Range)e.NewValue;
        }

        static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is AxisManager axis) {
                axis.LabelTicks = axis.GetLabelTicks();
                axis.AxisMapper = new AxisMapper(axis);
            }
        }

        static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is AxisManager axis) {
                axis.Range = axis.Range;
            }
        }
        #endregion

        public abstract List<LabelTickData> GetLabelTicks();
    }
}
