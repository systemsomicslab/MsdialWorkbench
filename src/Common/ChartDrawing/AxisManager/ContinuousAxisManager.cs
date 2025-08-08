using System;
using System.Collections.Generic;
using System.Windows;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.AxisManager
{
    public class ContinuousAxisManager : FreezableAxisManager
    {
        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register(
                nameof(MinValue), typeof(IConvertible), typeof(ContinuousAxisManager),
                new PropertyMetadata(
                    default,
                    OnMinValueChanged));

        public IConvertible MinValue
        {
            get => (IConvertible)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        static void OnMinValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = (ContinuousAxisManager)d;
            axis.CoreRange = new AxisRange(
                minimum: Convert.ToDouble(axis.MinValue),
                maximum: axis.CoreRange.Maximum);
        }

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(
                nameof(MaxValue), typeof(IConvertible), typeof(ContinuousAxisManager),
                new PropertyMetadata(
                    default,
                    OnMaxValueChanged));

        public IConvertible MaxValue
        {
            get => (IConvertible)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        static void OnMaxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = (ContinuousAxisManager)d;
            axis.CoreRange = new AxisRange(
                minimum: axis.CoreRange.Minimum,
                maximum: Convert.ToDouble(axis.MaxValue));
        }

        public override List<LabelTickData> GetLabelTicks()
        {
            var result = new List<LabelTickData>();

            if (Min >= Max || double.IsNaN(Min) || double.IsNaN(Max)) return result;
            var TickInterval = (decimal)Math.Pow(10, Math.Floor(Math.Log10(Max - Min)));
            if (TickInterval == 0) return result;
            var fold = (decimal)(Max - Min) / TickInterval;
            if (fold <= 2) {
                TickInterval /= 2;
                fold *= 2;
            }
            decimal shortTickInterval =
                TickInterval * (decimal)(fold >= 5 ? 0.5 :
                                         fold >= 2 ? 0.25 :
                                                     0.1);

            var exp = Math.Floor(Math.Log10(Max));
            var LabelFormat = exp >= 3 ? "0.00e0" : exp < 0 ? "0.0e0" : TickInterval >= 1 ? "f0" : "f3";
            for(var i = Math.Ceiling((decimal)Min.Value / TickInterval); i * TickInterval <= (decimal)Max.Value; ++i)
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
            for(var i = Math.Ceiling((decimal)Min.Value / shortTickInterval); i * shortTickInterval <= (decimal)Max.Value; ++i)
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

        protected override Freezable CreateInstanceCore()
        {
            return new ContinuousAxisManager();
        }
    }
}
