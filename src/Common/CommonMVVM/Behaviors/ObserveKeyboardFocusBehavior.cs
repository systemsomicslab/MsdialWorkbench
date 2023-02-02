using System;
using System.Windows;

namespace CompMs.CommonMVVM.Behaviors
{
    public static class ObserveKeyboardFocusBehavior
    {
        public static Action GetObserveAction(DependencyObject obj) {
            return (Action)obj.GetValue(ObserveActionProperty);
        }

        public static void SetObserveAction(DependencyObject obj, object value) {
            obj.SetValue(ObserveActionProperty, value);
        }

        public static DependencyProperty ObserveActionProperty =
            DependencyProperty.RegisterAttached(
                "ObserveAction",
                typeof(Action),
                typeof(ObserveKeyboardFocusBehavior),
                new PropertyMetadata(OnObserveActionChanged));

        private static void OnObserveActionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var element = (UIElement)d;
            if (!(e.OldValue is null)) {
                element.IsKeyboardFocusWithinChanged -= OnKeyboardFocusWithinChanged;
            }
            if (!(e.NewValue is null)) {
                element.IsKeyboardFocusWithinChanged += OnKeyboardFocusWithinChanged;
            }
        }

        private static void OnKeyboardFocusWithinChanged(object obj, DependencyPropertyChangedEventArgs e) {
            if ((bool)e.NewValue) {
                var d = (DependencyObject)obj;
                var action = GetObserveAction(d);
                action?.Invoke();
            }
        }
    }
}
