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

    public abstract class AxisManager : DependencyObject
    {
        #region DependencyProperty
        public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
            nameof(Min), typeof(double), typeof(AxisManager),
            new PropertyMetadata(default(double), OnMinChanged)
            );

        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
            nameof(Max), typeof(double), typeof(AxisManager),
            new PropertyMetadata(default(double), OnMaxChanged)
            );

        public static readonly DependencyProperty InitialMinProperty = DependencyProperty.Register(
            nameof(InitialMin), typeof(double), typeof(AxisManager),
            new PropertyMetadata(default(double), OnInitialMinChanged)
            );

        public static readonly DependencyProperty InitialMaxProperty = DependencyProperty.Register(
            nameof(InitialMax), typeof(double), typeof(AxisManager),
            new PropertyMetadata(default(double), OnInitialMaxChanged)
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
        public double Min
        {
            get => (double)GetValue(MinProperty);
            set => SetValue(MinProperty, value);
        }

        public double Max
        {
            get => (double)GetValue(MaxProperty);
            set => SetValue(MaxProperty, value);
        }

        public double InitialMin
        {
            get => (double)GetValue(InitialMinProperty);
            set => SetValue(InitialMinProperty, value);
        }

        public double InitialMax
        {
            get => (double)GetValue(InitialMaxProperty);
            set => SetValue(InitialMaxProperty, value);
        }

        public bool IsFlipped
        {
            get => (bool)GetValue(IsFlippedProperty);
            set => SetValue(IsFlippedProperty, value);
        }

        public AxisMapper AxisMapper
        {
            get => (AxisMapper)GetValue(AxisMapperProperty);
            set => SetValue(AxisMapperProperty, value);
        }

        public List<LabelTickData> LabelTicks
        {
            get => (List<LabelTickData>)GetValue(LabelTicksProperty);
            set => SetValue(LabelTicksProperty, value);
        }
        #endregion

        #region Method
        protected virtual double ValueToRenderPositionCore(double value) =>
            (IsFlipped ? (Max - value) : (value - Min)) / (Max - Min);

        protected virtual double ValueToRenderPositionCore(IConvertible value) =>
            ValueToRenderPositionCore(Convert.ToDouble(value));

        protected virtual double ValueToRenderPositionCore(object value) =>
            throw new NotImplementedException();

        public virtual double ValueToRenderPosition(object value)
        {
            if (value is double)
                return ValueToRenderPositionCore((double)value);
            else if (value is IConvertible)
                return ValueToRenderPositionCore(value as IConvertible);
            else
                return ValueToRenderPositionCore(value);
        }

        public virtual double RenderPositionToValue(double value) =>
            IsFlipped ? (Max - value * (Max - Min)) : (value * (Max - Min) + Min);
        #endregion

        #region Event
        static void OnInitialMinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisManager;
            if (axis == null) return;

            axis.Min = (double)e.NewValue;
        }

        static void OnInitialMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisManager;
            if (axis == null) return;

            axis.Max = (double)e.NewValue;
        }

        static void OnMinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisManager;
            if (axis == null) return;

            axis.LabelTicks = axis.GetLabelTicks();
            axis.AxisMapper = new AxisMapper(axis);
        }

        static void OnMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
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
