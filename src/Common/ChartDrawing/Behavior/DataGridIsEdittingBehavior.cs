using CompMs.Graphics.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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
                    grid.Sorting -= OnSorting;
                    grid.IsKeyboardFocusWithinChanged -= OnIsKeyboardFocusWithinChanged;
                    grid.SelectionChanged -= OnSelectionChanged;
                }
                if ((bool)e.NewValue) {
                    // A new table is about to bind its collection view, which throws "'DeferRefresh' is not allowed during an
                    // AddNew or EditItem transaction" while another table still holds a row of that view in edit (e.g. the ion
                    // table is opened again while a name is being edited in it). Such edits are committed first, as Enter would.
                    CommitPendingEditsOfOtherGrids(grid);
                    grid.BeginningEdit += OnBeginningEdit;
                    grid.Sorting += OnSorting;
                    grid.IsKeyboardFocusWithinChanged += OnIsKeyboardFocusWithinChanged;
                    grid.SelectionChanged += OnSelectionChanged;
                }
            }
        }

        private static void OnSorting(object sender, DataGridSortingEventArgs e) {
            // DataGrid commits the pending edit before sorting, but only through the current cell's container.
            // Once the edited row has been scrolled out of view that container is virtualized away, the edit transaction
            // survives, and sorting throws "'Sorting' is not allowed during an AddNew or EditItem transaction".
            // Close the transaction on the collection view directly so the sort can go ahead.
            if (sender is DataGrid grid) {
                CommitPendingEdit(grid);
            }
        }

        private static void OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e) {
            if (sender is DataGrid grid) {
                System.Diagnostics.Debug.WriteLine("Edit beginning");
                // An edit of another row that DataGrid could not close (its row was scrolled away) would otherwise stay open under the new one.
                if (grid.Items is IEditableCollectionView view && view.IsEditingItem && !Equals(view.CurrentEditItem, e.Row?.Item)) {
                    CommitPendingEdit(grid);
                }
                RegisterEditingGrid(grid);
            }
        }

        // Grids that began an edit which has not ended yet; their collection views may still be in an edit transaction.
        private static readonly List<WeakReference<DataGrid>> _editingGrids = new List<WeakReference<DataGrid>>();

        private static void RegisterEditingGrid(DataGrid grid) {
            _editingGrids.RemoveAll(r => !r.TryGetTarget(out var g) || ReferenceEquals(g, grid));
            _editingGrids.Add(new WeakReference<DataGrid>(grid));
        }

        private static void UnregisterEditingGrid(DataGrid grid) {
            _editingGrids.RemoveAll(r => !r.TryGetTarget(out var g) || ReferenceEquals(g, grid));
        }

        private static void CommitPendingEditsOfOtherGrids(DataGrid grid) {
            foreach (var reference in _editingGrids.ToArray()) {
                if (reference.TryGetTarget(out var other) && !ReferenceEquals(other, grid)) {
                    CommitPendingEdit(other);
                }
            }
        }

        // DataGrid keeps a row in edit until Enter is pressed or another row of the grid is clicked. A row left in edit
        // in any other way, by moving the focus out of the grid (to a filter box, to the main window) or by selecting
        // another spot elsewhere, kept the collection view in its edit transaction: every filter was held back until the
        // edit ended, and the table looked frozen. Such an edit is committed here, as Enter would.
        private static void OnIsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (sender is DataGrid grid && !(bool)e.NewValue) {
                // Wait until the focus has settled: it may only be passing through, e.g. to a cell's own popup.
                grid.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => {
                    if (!grid.IsKeyboardFocusWithin) {
                        CommitPendingEdit(grid);
                    }
                }));
            }
        }

        private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender is DataGrid grid && e.OriginalSource == grid) {
                // The selection was moved away from the edited row by something other than a click in the grid (e.g. a spot chosen
                // on the chart). A click in the grid has already committed the row by the time this runs.
                grid.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => {
                    if (grid.Items is IEditableCollectionView view && view.IsEditingItem && !Equals(view.CurrentEditItem, grid.SelectedItem)) {
                        CommitPendingEdit(grid);
                    }
                }));
            }
        }

        /// <summary>Commits the edit left open on the grid and its collection view, if any.</summary>
        public static void CommitPendingEdit(DataGrid grid) {
            if (!(grid.Items is IEditableCollectionView view) || !(view.IsEditingItem || view.IsAddingNew)) {
                UnregisterEditingGrid(grid);
                return;
            }
            System.Diagnostics.Debug.WriteLine("Commit the edit left open");
            // The values are written and the transaction on the view is closed before DataGrid ends the row. Ending the row
            // first runs the pending UI work, and a table that is just opening binds this view in that work and throws while
            // the view is still in edit. DataGrid also ends the row through the current cell, which leaves the transaction
            // open once that cell is no longer on the edited row (e.g. it was scrolled away).
            try {
                grid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
            }
            catch (InvalidOperationException ex) {
                System.Diagnostics.Debug.WriteLine($"DataGrid could not commit the cell: {ex.Message}");
            }
            if (view.IsEditingItem && grid.ItemContainerGenerator.ContainerFromItem(view.CurrentEditItem) is DataGridRow row) {
                // DataGrid holds the edited values in the row's binding group until the row ends.
                row.BindingGroup?.CommitEdit();
            }
            if (view.IsAddingNew) {
                view.CommitNew();
            }
            if (view.IsEditingItem) {
                view.CommitEdit();
            }
            try {
                grid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);
            }
            catch (InvalidOperationException ex) {
                System.Diagnostics.Debug.WriteLine($"DataGrid could not commit the row: {ex.Message}");
            }
            UnregisterEditingGrid(grid);
        }
    }
}
