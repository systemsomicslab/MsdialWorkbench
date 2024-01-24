using CompMs.Graphics.Base;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CompMs.Graphics.Behavior
{
    public class DataGridScrollToSelectionBehavior
    {
        public static bool GetEnable(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableProperty);
        }

        public static void SetEnable(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(DataGridScrollToSelectionBehavior),
                new PropertyMetadata(
                    BooleanBoxes.FalseBox,
                    EnablePropertyChanged));

        private static void EnablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is DataGrid datagrid) {
                if ((bool)e.NewValue) {
                    datagrid.SelectionChanged += Datagrid_SelectionChanged;
                }
                else {
                    datagrid.SelectionChanged -= Datagrid_SelectionChanged;
                }
            }
        }

        private static void Datagrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid grid) {
                var addedItems = e.AddedItems;
                if (addedItems.Count > 0 && addedItems[0] != null) {
                    try {
                        grid.ScrollIntoView(addedItems[0]);
                    }
                    catch (InvalidOperationException) {
                        // Ignore
                    }
                }
            }

        }
    }
}
