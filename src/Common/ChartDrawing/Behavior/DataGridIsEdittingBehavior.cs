using CompMs.Graphics.Base;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CompMs.Graphics.Behavior
{
    public sealed class DataGridIsEdittingBehavior
    {
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(bool),
                typeof(DataGridIsEdittingBehavior),
                new PropertyMetadata(
                    BooleanBoxes.FalseBox,
                    OnEnabledChanged));

        public static bool GetEnabled(DependencyObject d) {
            return (bool)d.GetValue(EnabledProperty);
        }

        public static void SetEnabled(DependencyObject d, bool value) {
            d.SetValue(EnabledProperty, BooleanBoxes.Box(value));
        }

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            // DO NOT USE CellEditEnding
            // If cell editting is cancelled, CellEditEnding event is fired. However, RowEditting is not cancelled. ICollectionView is still not editable.
            if (d is DataGrid grid) {
                if ((bool)e.OldValue) {
                    grid.BeginningEdit -= OnBeginningEdit;
                    grid.RowEditEnding -= OnRowEditEnding;
                    grid.Sorting -= OnSorting;
                }
                if ((bool)e.NewValue) {
                    grid.BeginningEdit += OnBeginningEdit;
                    grid.RowEditEnding += OnRowEditEnding;
                    grid.Sorting += OnSorting;
                }
            }
        }

        private static void OnSorting(object sender, DataGridSortingEventArgs e) {
            // DataGrid commits the pending edit before sorting, but only through the current cell's container.
            // Once the edited row has been scrolled out of view that container is virtualized away, the edit transaction
            // survives, and sorting throws "'Sorting' is not allowed during an AddNew or EditItem transaction".
            // Close the transaction on the collection view directly so the sort can go ahead.
            if (sender is DataGrid grid && grid.Items is IEditableCollectionView view && (view.IsEditingItem || view.IsAddingNew)) {
                System.Diagnostics.Debug.WriteLine("Commit the edit left open before sorting");
                if (view.IsAddingNew) {
                    view.CommitNew();
                }
                if (view.IsEditingItem) {
                    view.CommitEdit();
                }
                SetIsEditting(grid, false);
            }
        }

        private static void OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e) {
            if (sender is DataGrid grid) {
                System.Diagnostics.Debug.WriteLine("Edit beginning");
                SetIsEditting(grid, true);
            }
        }

        private static void OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e) {
            if (sender is DataGrid grid) {
                System.Diagnostics.Debug.WriteLine("Row edit ending");
                SetIsEditting(grid, false);
            }
        }

        public static readonly DependencyProperty IsEdittingProperty =
            DependencyProperty.RegisterAttached(
                "IsEditting",
                typeof(bool),
                typeof(DataGridIsEdittingBehavior),
                new PropertyMetadata(BooleanBoxes.FalseBox));

        public static bool GetIsEditting(DependencyObject d) {
            return (bool)d.GetValue(IsEdittingProperty);
        }

        public static void SetIsEditting(DependencyObject d, bool value) {
            d.SetValue(IsEdittingProperty, BooleanBoxes.Box(value));
        }
    }
}
