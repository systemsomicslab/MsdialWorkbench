using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Adorner;
using CompMs.Graphics.Core.Base;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Behavior;

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

    public static readonly DependencyProperty StrechHorizontalProperty =
        DependencyProperty.RegisterAttached(
            "StrechHorizontal", typeof(bool), typeof(ZoomByDragBehavior),
            new PropertyMetadata(BooleanBoxes.FalseBox));

    public static bool GetStrechHorizontal(DependencyObject d) =>
        (bool)d.GetValue(StrechHorizontalProperty);

    public static void SetStrechHorizontal(DependencyObject d, bool value) =>
        d.SetValue(StrechHorizontalProperty, BooleanBoxes.Box(value));

    public static readonly DependencyProperty StrechVerticalProperty =
        DependencyProperty.RegisterAttached(
            "StrechVertical", typeof(bool), typeof(ZoomByDragBehavior),
            new PropertyMetadata(BooleanBoxes.FalseBox));

    public static bool GetStrechVertical(DependencyObject d) =>
        (bool)d.GetValue(StrechVerticalProperty);

    public static void SetStrechVertical(DependencyObject d, bool value) =>
        d.SetValue(StrechVerticalProperty, BooleanBoxes.Box(value));

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
        RubberAdorner.InitialPointProperty.AddOwner(
            typeof(ZoomByDragBehavior));

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

    public static readonly DependencyProperty RubberBrushProperty
        = RubberAdorner.RubberBrushProperty.AddOwner(typeof(ZoomByDragBehavior));

    public static Brush GetRubberBrush(DependencyObject d)
        => (Brush)d.GetValue(RubberBrushProperty);

    public static void SetRubberBrush(DependencyObject d, Brush value)
        => d.SetValue(RubberBrushProperty, value);

    public static readonly DependencyProperty BorderPenProperty
        = RubberAdorner.BorderPenProperty.AddOwner(typeof(ZoomByDragBehavior));

    public static Pen GetBorderPen(DependencyObject d)
        => (Pen)d.GetValue(BorderPenProperty);

    public static void SetBorderPen(DependencyObject d, Pen value)
        => d.SetValue(BorderPenProperty, value);

    static void ZoomOnMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
        if (sender is FrameworkElement fe) {
            var initial = e.GetPosition(fe);
            SetInitialPoint(fe, initial);
            var adorner = new RubberAdorner(fe, initial)
            {
                Invert = true,
                Shape = (GetStrechHorizontal(fe), GetStrechVertical(fe)) switch
                {
                    (true, false) => RubberShape.Vertical,
                    (false, true) => RubberShape.Horizontal,
                    _ => RubberShape.Rectangle,
                }
            };
            adorner.Attach();
            SetAdorner(fe, adorner);
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
                    var rangeX = RenderPointsToRange(initial.X, current.X, fe.ActualWidth, haxis, flippedX);
                    if (rangeX.Minimum < rangeX.Maximum)
                        haxis.Focus(rangeX);
                }

                var vaxis = ChartBaseControl.GetVerticalAxis(fe);
                if (vaxis != null) {
                    var flippedY = ChartBaseControl.GetFlippedY(fe);
                    var rangeY = RenderPointsToRange(initial.Y, current.Y, fe.ActualHeight, vaxis, flippedY);
                    if (rangeY.Minimum < rangeY.Maximum)
                        vaxis.Focus(rangeY);
                }
            }
        }
    }

    static AxisRange RenderPointsToRange(double u, double v, double w, IAxisManager am, bool flipped) {
        var uu = am.TranslateFromRenderPoint(u, flipped, w);
        var vv = am.TranslateFromRenderPoint(v, flipped, w);
        if (uu.Value <= vv.Value)
            return new AxisRange(uu, vv);
        else
            return new AxisRange(vv, uu);
    }
}
