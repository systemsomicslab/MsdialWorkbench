using CompMs.Graphics.Base;
using System.Windows;
using System.Windows.Controls;

namespace CompMs.Graphics.Behavior
{
    public class DataGridCommitBehavior
    {
        public static bool GetEnable(DependencyObject obj) {
            return (bool)obj.GetValue(EnableProperty);
        }

        public static void SetEnable(DependencyObject obj, bool value) {
            obj.SetValue(EnableProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(DataGridCommitBehavior),
                new PropertyMetadata(BooleanBoxes.FalseBox, EnablePropertyChanged));

        private static void EnablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is DataGrid dataGrid) {
                if ((bool)e.NewValue) {
                    dataGrid.Unloaded += DataGrid_Unloaded;
                }
                else {
                    dataGrid.Unloaded -= DataGrid_Unloaded;
                }
            }
        }

        private static void DataGrid_Unloaded(object sender, RoutedEventArgs e) {
            if (sender is DataGrid grid) {
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }
    }
}
