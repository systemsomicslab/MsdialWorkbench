using CompMs.Graphics.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Behavior
{
    public class ChildDoubleClickBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(ChildDoubleClickBehavior));

        public static ICommand GetCommand(DependencyObject obj) {
            return (ICommand)obj.GetValue(CommandProperty);
        }
        public static void SetCommand(DependencyObject obj, ICommand value) {
            obj.SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty IsEnableProperty =
            DependencyProperty.RegisterAttached(
                "IsEnable",
                typeof(bool),
                typeof(ChildDoubleClickBehavior),
                new PropertyMetadata(BooleanBoxes.FalseBox, OnIsEnableChanged));

        public static bool GetIsEnable(DependencyObject obj) {
            return (bool)obj.GetValue(IsEnableProperty);
        }
        public static void SetIsEnable(DependencyObject obj, bool value) {
            obj.SetValue(IsEnableProperty, BooleanBoxes.Box(value));
        }

        static void OnIsEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is Control control) {
                if ((bool)e.OldValue)
                    OnDetaching(control);
                if ((bool)e.NewValue)
                    OnAttached(control);
            }
        }

        static void OnAttached(Control control) {
            control.PreviewMouseDoubleClick += OnPreviewMouseDoubleClick;
        }

        static void OnDetaching(Control control) {
            control.PreviewMouseDoubleClick -= OnPreviewMouseDoubleClick;
        }

        static void OnPreviewMouseDoubleClick(object sender, RoutedEventArgs e) {
            if (sender is Control control) {
                var command = GetCommand(control);
                command.Execute(control);
            }
        }
    }
}
