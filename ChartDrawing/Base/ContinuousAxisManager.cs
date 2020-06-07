using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Base
{
    public class ContinuousAxisManager : AxisManager
    {
        #region DependencyProperty
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            nameof(MinValue), typeof(IConvertible), typeof(ContinuousAxisManager),
            new PropertyMetadata(default, OnMinValueChanged)
            );

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            nameof(MaxValue), typeof(IConvertible), typeof(ContinuousAxisManager),
            new PropertyMetadata(default, OnMaxValueChanged)
            );

        public static readonly DependencyProperty ChartMarginProperty = DependencyProperty.Register(
            nameof(ChartMargin), typeof(double), typeof(ContinuousAxisManager),
            new PropertyMetadata(0d, OnChartMarginChanged)
            );
        #endregion

        #region Property
        public IConvertible MinValue
        {
            get => (IConvertible)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public IConvertible MaxValue
        {
            get => (IConvertible)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public double ChartMargin
        {
            get => (double)GetValue(ChartMarginProperty);
            set => SetValue(ChartMarginProperty, value);
        }
        #endregion

        public override List<LabelTickData> GetLabelTicks()
        {
            var result = new List<LabelTickData>();

            var TickInterval = (decimal)Math.Pow(10, Math.Floor(Math.Log10(Max - Min)));
            var exp = Math.Floor(Math.Log10(Max));
            var LabelFormat = exp > 3 ? "0.00e0" : exp < 0 ? "0.0e0" : TickInterval >= 1 ? "f0" : "f3";
            if (TickInterval == 0) return result;
            var fold = (decimal)(Max - Min) / TickInterval;
            decimal shortTickInterval =
                TickInterval * (decimal)(fold >= 5 ? 0.5 :
                                         fold >= 2 ? 0.25 :
                                                     0.1);

            for(var i = Math.Ceiling((decimal)Min / TickInterval); i * TickInterval <= (decimal)Max; ++i)
            {
                var item = new LabelTickData()
                {
                    Label = (i * TickInterval).ToString(LabelFormat),
                    TickType = TickType.LongTick,
                    Center = (double)(i * TickInterval),
                    Width = (double)TickInterval,
                    Source = (double)(i * TickInterval),
                };
                result.Add(item);
            }

            if (shortTickInterval == 0) return result;
            for(var i = Math.Ceiling((decimal)Min / shortTickInterval); i * shortTickInterval <= (decimal)Max; ++i)
            {
                var item = new LabelTickData()
                {
                    Label = (i * shortTickInterval).ToString(LabelFormat), 
                    TickType = TickType.ShortTick,
                    Center = (double)(i * shortTickInterval),
                    Width = 0,
                    Source = (double)(i * shortTickInterval),
                };
                result.Add(item);
            }

            return result;
        }

        #region Event handler
        static void OnMinValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as ContinuousAxisManager;
            if (axis == null) return;

            var min = Convert.ToDouble((IConvertible)e.NewValue);
            var max = Convert.ToDouble(axis.MaxValue);
            axis.InitialMin = min - (max - min) * axis.ChartMargin;
            axis.InitialMax = max + (max - min) * axis.ChartMargin;
        }

        static void OnMaxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as ContinuousAxisManager;
            if (axis == null) return;

            var min = Convert.ToDouble(axis.MinValue);
            var max = Convert.ToDouble((IConvertible)e.NewValue);
            axis.InitialMin = min - (max - min) * axis.ChartMargin;
            axis.InitialMax = max + (max - min) * axis.ChartMargin;
        }

        static void OnChartMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as ContinuousAxisManager;
            if (axis == null) return;

            var min = Convert.ToDouble(axis.MinValue);
            var max = Convert.ToDouble((IConvertible)e.NewValue);
            var r = (double)e.NewValue;
            axis.InitialMin = min - (max - min) * r;
            axis.InitialMax = max + (max - min) * r;
        }
        #endregion
    }
}
