using System;

namespace CompMs.Graphics.Behavior
{
    using System.Windows;
    public static class DialogResultSetBehavior
    {
        public static DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached(
                "DialogResult",
                typeof(bool?),
                typeof(DialogResultSetBehavior),
                new PropertyMetadata(
                    null,
                    DialogResultChanged));
        
        private static void DialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is Window window) {
                window.DialogResult = (bool?)e.NewValue;
            }
        }

        public static bool? GetDialogResult(Window window) {
            return (bool?)window.GetValue(DialogResultProperty);
        }

        public static void SetDialogResult(Window window, bool? value) {
            window.SetValue(DialogResultProperty, value);
        }
    }
}
