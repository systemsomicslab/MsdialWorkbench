using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Adorner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Documents;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Behavior
{
    public static class ZoomByDragBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(ZoomByDragBehavior),
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
            fe.MouseRightButtonDown += ZoomOnMouseRightButtonDown;
            fe.MouseMove += ZoomOnMouseMove;
            fe.MouseRightButtonUp += ZoomOnMouseRightButtonUp;
        }

        static void OnDetaching(FrameworkElement fe) {
            fe.MouseRightButtonDown -= ZoomOnMouseRightButtonDown;
            fe.MouseMove -= ZoomOnMouseMove;
            fe.MouseRightButtonUp -= ZoomOnMouseRightButtonUp;
        }

        static readonly DependencyProperty InitialPointProperty =
            DependencyProperty.RegisterAttached(
                "InitialPoint", typeof(Point), typeof(ZoomByDragBehavior),
                new PropertyMetadata(new Point(0, 0)));

        static Point GetInitialPoint(DependencyObject d) =>
            (Point)d.GetValue(InitialPointProperty);

        static void SetInitialPoint(DependencyObject d, Point value) =>
            d.SetValue(InitialPointProperty, value);

        static readonly DependencyProperty AdornerProperty =
            DependencyProperty.RegisterAttached(
                "Adorner", typeof(RubberAdorner), typeof(ZoomByDragBehavior),
                new PropertyMetadata(null));

        static RubberAdorner GetAdorner(DependencyObject d) =>
            (RubberAdorner)d.GetValue(AdornerProperty);

        static void SetAdorner(DependencyObject d, RubberAdorner value) =>
            d.SetValue(AdornerProperty, value);

        static void ZoomOnMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe) {
                var initial = e.GetPosition(fe);
                SetInitialPoint(fe, initial);
                SetAdorner(fe, new RubberAdorner(fe, initial));
                fe.CaptureMouse();
            }
        }

        static void ZoomOnMouseMove(object sender, MouseEventArgs e) {
            if (sender is FrameworkElement fe) {
                var adorner = GetAdorner(fe);
                if (adorner != null) {
                    var initial = GetInitialPoint(fe);
                    var current = e.GetPosition(fe);
                    adorner.Offset = current - initial;
                }
            }
        }

        static void ZoomOnMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe) {
                var adorner = GetAdorner(fe);
                if (adorner != null) {
                    fe.ReleaseMouseCapture();

                    adorner.Detach();
                    SetAdorner(fe, null);
                    var current = e.GetPosition(fe);
                    var initial = GetInitialPoint(fe);

                    if (Math.Abs((current - initial).X) < SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs((current - initial).Y) < SystemParameters.MinimumVerticalDragDistance)
                        return;

                    var haxis = ChartBaseControl.GetHorizontalAxis(fe);
                    if (haxis != null) {
                        var flippedX = ChartBaseControl.GetFlippedX(fe);
                        var rangeX = haxis.InitialRange.Intersect(RenderPointsToRange(initial.X / fe.ActualWidth, current.X / fe.ActualWidth, haxis, flippedX));
                        if (rangeX.Minimum < rangeX.Maximum)
                            haxis.Range = rangeX;
                    }

                    var vaxis = ChartBaseControl.GetVerticalAxis(fe);
                    if (vaxis != null) {
                        var flippedY = ChartBaseControl.GetFlippedY(fe);
                        var rangeY = vaxis.InitialRange.Intersect(RenderPointsToRange(initial.Y / fe.ActualHeight, current.Y / fe.ActualHeight, vaxis, flippedY));
                        if (rangeY.Minimum < rangeY.Maximum)
                            vaxis.Range = rangeY;
                    }
                }
            }
        }

        static Range RenderPointsToRange(double u, double v, IAxisManager am, bool flipped) {
            var uu = am.TranslateFromRenderPoint(u, flipped);
            var vv = am.TranslateFromRenderPoint(v, flipped);
            if (uu.Value <= vv.Value)
                return new Range(uu, vv);
            else
                return new Range(vv, uu);
        }
    }
}
