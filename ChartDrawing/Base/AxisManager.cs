using System;
using System.Collections.Generic;
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
            new PropertyMetadata(default(double))
            );

        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
            nameof(Max), typeof(double), typeof(AxisManager),
            new PropertyMetadata(default(double))
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
            new PropertyMetadata(false)
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
        #endregion

        #region Method
        public virtual double ValueToRenderPosition(double value) =>
            (IsFlipped ? (Max - value) : (value - Min)) / (Max - Min);

        public virtual double ValueToRenderPosition(IConvertible value) =>
            ValueToRenderPosition(Convert.ToDouble(value));

        public virtual double ValueToRenderPosition(object value) =>
            throw new NotImplementedException();

        public virtual double RenderPositionToValue(double value) =>
            IsFlipped ? (Max - value * (Max - Min)) : (value * (Max - Min) + Min);
        #endregion

        #region Event handler
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
        #endregion


        public virtual List<LabelTickData> GetLabelTicks()
        {
            throw new NotImplementedException();
        }
    }
}
