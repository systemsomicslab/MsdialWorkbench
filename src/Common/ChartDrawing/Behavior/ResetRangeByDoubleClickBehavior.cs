using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using System;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.Behavior
{
    public static class ResetRangeByDoubleClickBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(ResetRangeByDoubleClickBehavior),
                new PropertyMetadata(BooleanBoxes.FalseBox, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject d) =>
            (bool)d.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject d, bool value) =>
            d.SetValue(IsEnabledProperty, BooleanBoxes.Box(value));

        static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe) {
                if ((bool)e.OldValue)
                    OnDetaching(fe);
                if ((bool)e.NewValue)
                    OnAttached(fe);
            }
        }

        static void OnAttached(FrameworkElement fe) {
            fe.MouseLeftButtonDown += ResetChartAreaOnDoubleClick;
        }

        static void OnDetaching(FrameworkElement fe) {
            fe.MouseLeftButtonDown -= ResetChartAreaOnDoubleClick;
        }

        static void ResetChartAreaOnDoubleClick(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe) {
                if (e.ClickCount == 2) {
                    if (ChartBaseControl.GetHorizontalAxis(fe) is IAxisManager haxis) {
                        haxis.Reset();
                    }
                    if (ChartBaseControl.GetVerticalAxis(fe) is IAxisManager vaxis) {
                        vaxis.Reset();
                    }
                }
            }
        }
    }
}
