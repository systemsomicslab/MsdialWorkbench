using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using System;
using System.Windows;

namespace CompMs.Graphics.Behavior
{
    public static class AxisRangeRecalculationBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(AxisRangeRecalculationBehavior),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.TrueBox,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject d) => (bool)d.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject d, bool value) => d.SetValue(IsEnabledProperty, BooleanBoxes.Box(value));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is FrameworkElement fe)) {
                return;
            }
            var hAxis = GetHorizontalAxis(fe);
            var vAxis = GetVerticalAxis(fe);
            if ((bool)e.NewValue) {
                if (!(hAxis is null)) {
                    fe.SizeChanged += OnSizeChangedRecalculateHorizontal;
                    hAxis.Recalculate(fe.ActualWidth);
                    EventHandler handler = (_s, _e) => { hAxis.Recalculate(fe.ActualWidth); hAxis.Reset(); };
                    hAxis.InitialRangeChanged += handler;
                    SetInitialRangeUpdatorOnHorizontal(fe, handler);
                }
                if (!(vAxis is null)) {
                    fe.SizeChanged += OnSizeChangedRecalculateVertical;
                    vAxis.Recalculate(fe.ActualHeight);
                    EventHandler handler = (_s, _e) => { vAxis.Recalculate(fe.ActualHeight); vAxis.Reset(); };
                    vAxis.InitialRangeChanged += handler;
                    SetInitialRangeUpdatorOnVertical(fe, handler);
                }
            }
            else {
                if (!(hAxis is null)) {
                    fe.SizeChanged -= OnSizeChangedRecalculateHorizontal;
                    hAxis.InitialRangeChanged -= GetInitialRangeUpdatorOnHorizontal(fe);
                    SetInitialRangeUpdatorOnHorizontal(fe, null);
                }
                if (!(vAxis is null)) {
                    fe.SizeChanged -= OnSizeChangedRecalculateVertical;
                    vAxis.InitialRangeChanged -= GetInitialRangeUpdatorOnVertical(fe);
                    SetInitialRangeUpdatorOnVertical(fe, null);
                }
            }
        }

        public static readonly DependencyProperty HorizontalAxisProperty =
            DependencyProperty.RegisterAttached(
                "HorizontalAxis", typeof(IAxisManager), typeof(AxisRangeRecalculationBehavior),
                new PropertyMetadata(null, OnHorizontalAxisChanged));

        public static IAxisManager GetHorizontalAxis(DependencyObject d) => (IAxisManager)d.GetValue(HorizontalAxisProperty);
        public static void SetHorizontalAxis(DependencyObject d, IAxisManager value) => d.SetValue(HorizontalAxisProperty, value);

        private static void OnHorizontalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (e.OldValue == e.NewValue) {
                return;
            }
            var fe = (FrameworkElement)d;
            if (e.OldValue is IAxisManager oldAxis && GetIsEnabled(fe)) {
                fe.SizeChanged -= OnSizeChangedRecalculateHorizontal;
                oldAxis.InitialRangeChanged -= GetInitialRangeUpdatorOnHorizontal(fe);
                SetInitialRangeUpdatorOnHorizontal(fe, null);
            }
            if (e.NewValue is IAxisManager newAxis && GetIsEnabled(fe)) {
                fe.SizeChanged += OnSizeChangedRecalculateHorizontal;
                newAxis.Recalculate(fe.ActualWidth);
                newAxis.Reset();
                EventHandler handler = (_s, _e) => { newAxis.Recalculate(fe.ActualWidth); newAxis.Reset(); };
                newAxis.InitialRangeChanged += handler;
                SetInitialRangeUpdatorOnHorizontal(fe, handler);
            }
        }

        private static void OnSizeChangedRecalculateHorizontal(object s, SizeChangedEventArgs e) {
            var fe = (FrameworkElement)s;
            if (e.WidthChanged) {
                var hAxis = GetHorizontalAxis(fe);
                hAxis.Recalculate(fe.ActualWidth);
                if (e.PreviousSize.Width == 0) {
                    hAxis.Reset();
                }
            }
        }

        private static readonly DependencyProperty InitialRangeUpdatorOnHorizontalProperty =
            DependencyProperty.RegisterAttached(
                "InitialRangeUpdatorOnHorizontal", typeof(EventHandler), typeof(AxisRangeRecalculationBehavior),
                new PropertyMetadata(null));

        private static EventHandler GetInitialRangeUpdatorOnHorizontal(DependencyObject d) => (EventHandler)d.GetValue(InitialRangeUpdatorOnHorizontalProperty);
        private static void SetInitialRangeUpdatorOnHorizontal(DependencyObject d, EventHandler value) => d.SetValue(InitialRangeUpdatorOnHorizontalProperty, value);

        public static readonly DependencyProperty VerticalAxisProperty =
            DependencyProperty.RegisterAttached(
                "VerticalAxis", typeof(IAxisManager), typeof(AxisRangeRecalculationBehavior),
                new PropertyMetadata(null, OnVerticalAxisChanged));

        public static IAxisManager GetVerticalAxis(DependencyObject d) => (IAxisManager)d.GetValue(VerticalAxisProperty);
        public static void SetVerticalAxis(DependencyObject d, IAxisManager value) => d.SetValue(VerticalAxisProperty, value);

        private static void OnVerticalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (e.OldValue == e.NewValue) {
                return;
            }
            var fe = (FrameworkElement)d;
            if (e.OldValue is IAxisManager oldAxis && GetIsEnabled(fe)) {
                fe.SizeChanged -= OnSizeChangedRecalculateVertical;
                oldAxis.InitialRangeChanged -= GetInitialRangeUpdatorOnVertical(fe);
                SetInitialRangeUpdatorOnVertical(fe, null);
            }
            if (e.NewValue is IAxisManager newAxis && GetIsEnabled(fe)) {
                fe.SizeChanged += OnSizeChangedRecalculateVertical;
                newAxis.Recalculate(fe.ActualHeight);
                newAxis.Reset();
                EventHandler handler = (_s, _e) => { newAxis.Recalculate(fe.ActualHeight); newAxis.Reset(); };
                newAxis.InitialRangeChanged += handler;
                SetInitialRangeUpdatorOnVertical(fe, handler);
            }
        }

        private static void OnSizeChangedRecalculateVertical(object s, SizeChangedEventArgs e) {
            var fe = (FrameworkElement)s;
            if (e.HeightChanged) {
                var vAxis = GetVerticalAxis(fe);
                vAxis.Recalculate(fe.ActualHeight);
                if (e.PreviousSize.Height == 0) {
                    vAxis.Reset();
                }
            }
        }

        private static readonly DependencyProperty InitialRangeUpdatorOnVerticalProperty =
            DependencyProperty.RegisterAttached(
                "InitialRangeUpdatorOnVertical", typeof(EventHandler), typeof(AxisRangeRecalculationBehavior),
                new PropertyMetadata(null));

        private static EventHandler GetInitialRangeUpdatorOnVertical(DependencyObject d) => (EventHandler)d.GetValue(InitialRangeUpdatorOnVerticalProperty);
        private static void SetInitialRangeUpdatorOnVertical(DependencyObject d, EventHandler value) => d.SetValue(InitialRangeUpdatorOnVerticalProperty, value);
    }
}
