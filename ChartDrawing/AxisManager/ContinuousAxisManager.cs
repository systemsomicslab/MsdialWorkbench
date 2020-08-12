using System;
using System.Collections.Generic;
using System.Windows;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.AxisManager
{
    public class ContinuousAxisManager : Core.Base.AxisManager
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
            nameof(ChartMargin), typeof(ChartMargin), typeof(ContinuousAxisManager),
            new PropertyMetadata(new ChartMargin { Left = 0d, Right = 0d }, OnChartMarginChanged)
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

        public ChartMargin ChartMargin
        {
            get => (ChartMargin)GetValue(ChartMarginProperty);
            set => SetValue(ChartMarginProperty, value);
        }
        #endregion

        public override List<LabelTickData> GetLabelTicks()
        {
            var result = new List<LabelTickData>();

            if (Min >= Max || double.IsNaN(Min) || double.IsNaN(Max)) return result;
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
            var r = axis.ChartMargin;
            axis.InitialRange = new Range {
                Minimum = min - (max - min) * r?.Left ?? 0d,
                Maximum = max + (max - min) * r?.Right ?? 0d,
            };
        }

        static void OnMaxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as ContinuousAxisManager;
            if (axis == null) return;

            var min = Convert.ToDouble(axis.MinValue);
            var max = Convert.ToDouble((IConvertible)e.NewValue);
            var r = axis.ChartMargin;
            axis.InitialRange = new Range {
                Minimum = min - (max - min) * r?.Left ?? 0d,
                Maximum = max + (max - min) * r?.Right ?? 0d,
            };
        }

        static void OnChartMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as ContinuousAxisManager;
            if (axis == null) return;

            var min = Convert.ToDouble(axis.MinValue);
            var max = Convert.ToDouble(axis.MaxValue);
            var r = (ChartMargin)e.NewValue;
            axis.InitialRange = new Range {
                Minimum = min - (max - min) * r?.Left ?? 0d,
                Maximum = max + (max - min) * r?.Right ?? 0d,
            };
        }
        #endregion
    }
}
