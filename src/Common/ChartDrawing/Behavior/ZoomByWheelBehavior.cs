using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.Behavior
{
    public static class ZoomByWheelBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(ZoomByWheelBehavior),
                new PropertyMetadata(BooleanBoxes.FalseBox, OnIsEnableChanged));

        public static bool GetIsEnabled(DependencyObject d) =>
            (bool)d.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject d, bool value) =>
            d.SetValue(IsEnabledProperty, BooleanBoxes.Box(value));

        static void OnIsEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe) {
                if ((bool)e.OldValue)
                    OnDetaching(fe);
                if ((bool)e.NewValue)
                    OnAttached(fe);
            }
        }

        static void OnAttached(FrameworkElement fe) {
            fe.MouseWheel += ZoomOnMouseWheel;
        }

        static void OnDetaching(FrameworkElement fe) {
            fe.MouseWheel -= ZoomOnMouseWheel;
        }

        public static readonly DependencyProperty DeltaProperty =
            DependencyProperty.RegisterAttached(
                "Delta", typeof(double), typeof(ZoomByWheelBehavior),
                new PropertyMetadata(0.1d));

        public static double GetDelta(DependencyObject d) =>
            (double)d.GetValue(DeltaProperty);

        public static void SetDelta(DependencyObject d, double value) =>
            d.SetValue(DeltaProperty, value);

        static void ZoomOnMouseWheel(object sender, MouseWheelEventArgs e) {
            if (sender is FrameworkElement fe) {
                var p = e.GetPosition(fe);
                var scale = e.Delta < 0 ? 1 + GetDelta(fe) : 1 / (1 + GetDelta(fe));

                var haxis = ChartBaseControl.GetHorizontalAxis(fe);
                if (haxis != null) {
                    var flippedX = ChartBaseControl.GetFlippedX(fe);
                    var rangeX = haxis.Range;
                    var point = haxis.TranslateFromRenderPoint(p.X, flippedX, fe.ActualWidth);
                    var range = new AxisRange((1 - scale) * point + rangeX.Minimum * scale, rangeX.Maximum * scale + (1 - scale) * point);
                    haxis.Focus(range);
                }

                var vaxis = ChartBaseControl.GetVerticalAxis(fe);
                if (vaxis != null) {
                    var flippedY = ChartBaseControl.GetFlippedY(fe);
                    var rangeY = vaxis.Range;
                    var point = vaxis.TranslateFromRenderPoint(p.Y, flippedY, fe.ActualHeight);
                    var range = new AxisRange((1 - scale) * point + rangeY.Minimum * scale, rangeY.Maximum * scale + (1 - scale) * point);
                    vaxis.Focus(range);
                }
            }
        }
    }
}
