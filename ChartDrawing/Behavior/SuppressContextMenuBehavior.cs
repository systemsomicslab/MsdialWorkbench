using CompMs.Graphics.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.Graphics.Behavior
{
    public class SuppressContextMenuBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(SuppressContextMenuBehavior),
                new PropertyMetadata(
                    BooleanBoxes.FalseBox,
                    OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject d) =>
            (bool)d.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject d, bool value) =>
            d.SetValue(IsEnabledProperty, BooleanBoxes.Box(value));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe) {
                if ((bool)e.OldValue) {
                    OnDetaching(fe);
                }
                if ((bool)e.NewValue) {
                    OnAttached(fe);
                }
            }
        }

        private static void OnAttached(FrameworkElement fe) {
            fe.MouseRightButtonDown += OnMouseRightButtonDown;
            fe.ContextMenuOpening += OnContextMenuOpening;
        }

        private static void OnDetaching(FrameworkElement fe) {
            fe.MouseRightButtonDown -= OnMouseRightButtonDown;
            fe.ContextMenuOpening -= OnContextMenuOpening;
        }

        private static void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe) {
                var point = e.GetPosition(fe);
                SetStartPoint(fe, point);
            }
        }

        private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e) {
            if (sender is FrameworkElement fe) {
                var current = Mouse.GetPosition(fe);
                var start = GetStartPoint(fe);

                if (Math.Abs(start.X - current.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(start.Y - current.Y) > SystemParameters.MinimumVerticalDragDistance)
                    e.Handled = true;
            }
        }

        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.RegisterAttached(
                "StartPoint", typeof(Point), typeof(SuppressContextMenuBehavior),
                new PropertyMetadata(default));

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Point GetStartPoint(DependencyObject d) =>
            (Point)d.GetValue(StartPointProperty);

        public static void SetStartPoint(DependencyObject d, Point value) =>
            d.SetValue(StartPointProperty, value);
    }
}
