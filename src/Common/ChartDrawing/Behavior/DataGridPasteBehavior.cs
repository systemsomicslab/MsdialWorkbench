using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.Graphics.Behavior
{
    public static class DataGridPasteBehavior
    {
        public static bool GetEnable(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableProperty);
        }

        public static void SetEnable(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableProperty, value);
        }

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(DataGridPasteBehavior), new PropertyMetadata(false, EnablePropertyChanged));

        private static void EnablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is DataGrid datagrid)) return;

            if ((bool)e.NewValue)
            {
                datagrid.PreviewKeyDown += Datagrid_KeyDown;
            }
            else
            {
                datagrid.PreviewKeyDown -= Datagrid_KeyDown;
            }
        }

        private static void Datagrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control) {
                CommonMVVM.DataGridPasteCommand.Instance.Execute(sender);
                e.Handled = true;
            }
        }
    }
}
