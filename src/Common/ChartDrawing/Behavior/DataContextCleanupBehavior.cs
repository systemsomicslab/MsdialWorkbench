using CompMs.Graphics.Base;
using System;
using System.Windows;

namespace CompMs.Graphics.Behavior
{
    public static class DataContextCleanupBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(AddMovabilityBehavior),
                new PropertyMetadata(
                    BooleanBoxes.FalseBox,
                    OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, BooleanBoxes.Box(value));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is System.Windows.Window window) {
                if ((bool)e.NewValue) {
                    window.Closed += WindowClosed;
                }
                else {
                    window.Closed -= WindowClosed;
                }
            }
        }

        private static void WindowClosed(object sender, EventArgs e) {
            ((sender as FrameworkElement)?.DataContext as IDisposable)?.Dispose();
        }
    }
}
