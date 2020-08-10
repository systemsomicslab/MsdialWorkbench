using CompMs.Graphics.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

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
            new PropertyMetadata(new Range { Minimum = 0, Maximum = 1}, OnRangeChanged)
            );

        public static readonly DependencyProperty InitialRangeProperty = DependencyProperty.Register(
            nameof(InitialRange), typeof(Range), typeof(AxisManager),
            new PropertyMetadata(new Range { Minimum = 0, Maximum = 1}, OnInitialRangeChanged)
            );

        public static readonly DependencyProperty IsFlippedProperty = DependencyProperty.Register(
            nameof(IsFlipped), typeof(bool), typeof(AxisManager),
            new PropertyMetadata(false, OnIsFlippedChanged)
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
        public double Min {
            get => Range.Minimum;
            set => Range = new Range { Minimum = value, Maximum = Range.Maximum };
        }

        public double Max {
            get => Range.Maximum;
            set => Range = new Range { Minimum = Range.Minimum, Maximum = value };
        }

        public Range Range {
            get => (Range)GetValue(RangeProperty);
            set => SetValue(RangeProperty, value);
        }

        public Range InitialRange {
            get => (Range)GetValue(InitialRangeProperty);
            set => SetValue(InitialRangeProperty, value);
        }

        public bool IsFlipped {
            get => (bool)GetValue(IsFlippedProperty);
            set => SetValue(IsFlippedProperty, value);
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
        protected virtual double ValueToRenderPositionCore(double value, double min, double max, bool isFlipped) {
            return (isFlipped ? (max - value) : (value - min)) / (max - min);
        }

        public virtual double ValueToRenderPosition(object value) {
            double max = Max, min = Min;
            bool isFlipped = IsFlipped;

            if (value is double d)
                return ValueToRenderPositionCore(d, min, max, isFlipped);
            else if (value is IConvertible convertible)
                return ValueToRenderPositionCore(Convert.ToDouble(convertible), min, max, isFlipped);
            else
                throw new NotImplementedException();
        }

        public virtual double RenderPositionToValue(double value) =>
            IsFlipped ? (Max - value * (Max - Min)) : (value * (Max - Min) + Min);

        public virtual List<double> ValuesToRenderPositions(IEnumerable<object> values) {
            double max = Max, min = Min;
            bool isFlipped = IsFlipped;
            var result = new List<double>();

            foreach (var value in values) {
                if (value is double d)
                    result.Add(ValueToRenderPositionCore(d, min, max, isFlipped));
                else if (value is IConvertible convertible)
                    result.Add(ValueToRenderPositionCore(Convert.ToDouble(convertible), min, max, isFlipped));
                else
                    result.Add(double.NaN);
            }

            return result;
        }
        #endregion

        #region Event
        static void OnInitialRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = d as AxisManager;
            if (axis == null) return;

            axis.Range = (Range)e.NewValue;
        }

        static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = d as AxisManager;
            if (axis == null) return;

            axis.LabelTicks = axis.GetLabelTicks();
            axis.AxisMapper = new AxisMapper(axis);
        }

        static void OnIsFlippedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisManager;
            if (axis == null) return;

            axis.LabelTicks = axis.GetLabelTicks();
            axis.AxisMapper = new AxisMapper(axis);
        }
        #endregion

        public virtual List<LabelTickData> GetLabelTicks()
        {
            throw new NotImplementedException();
        }
    }
}
