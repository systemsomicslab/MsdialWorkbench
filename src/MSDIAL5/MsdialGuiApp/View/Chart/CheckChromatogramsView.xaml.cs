using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Setting;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CompMs.App.Msdial.View.Chart
{
    /// <summary>
    /// Interaction logic for CheckChromatogramsView.xaml
    /// </summary>
    public partial class CheckChromatogramsView : UserControl
    {
        public CheckChromatogramsView() {
            InitializeComponent();
        }

        // A key typed into a selected cell starts the edit, but a template column does not pass that key on to the editing TextBox.
        // Put it there, so the cell takes what was typed, as DataGridTextColumn does.
        private void EicDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e) {
            if (sender is DataGrid { DataContext: EicSettingViewModel table }) {
                table.History.BeginEdit();
            }
            if (e.EditingEventArgs is not TextCompositionEventArgs { Text.Length: > 0 } input) {
                return;
            }
            e.EditingElement.ApplyTemplate();
            if (FindDescendant<TextBox>(e.EditingElement) is TextBox textBox) {
                StartWith(textBox, input.Text);
            }
            else {
                e.EditingElement.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => {
                    if (FindDescendant<TextBox>(e.EditingElement) is TextBox loaded) {
                        StartWith(loaded, input.Text);
                    }
                });
            }
        }

        // Delete on selected cells (not while a cell is being edited) clears their values.
        private void EicDataGrid_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key != Key.Delete || Keyboard.Modifiers != ModifierKeys.None || e.OriginalSource is TextBox || sender is not DataGrid grid) {
                return;
            }
            var cleared = false;
            foreach (var cell in grid.SelectedCells) {
                // The bound property is the first part of the column's clipboard binding path, e.g. "Mass" in "Mass.Value".
                if (cell.Item is PeakFeatureSearchValueViewModel row && cell.Column?.ClipboardContentBinding is Binding { Path.Path: string path }) {
                    CheckChromatogramsViewModel.ClearEicQueryValue(row, path.Split('.')[0]);
                    cleared = true;
                }
            }
            e.Handled = cleared;
        }

        // Ctrl+C on selected cells. While a cell is being edited, its TextBox copies the selected text instead.
        // DataGrid's own copy keeps the previous clipboard contents without a word when another program holds the clipboard; see DataGridSelectionCopy.
        private void EicDataGrid_CanCopy(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = sender is DataGrid { SelectedCells.Count: > 0 };
            e.Handled = true;
        }

        private void EicDataGrid_Copy(object sender, ExecutedRoutedEventArgs e) {
            e.Handled = true;
            if (sender is not DataGrid grid || DataGridSelectionCopy.CreateDataObject(grid) is not DataObject data) {
                return;
            }
            if (!DataGridSelectionCopy.TrySetClipboard(data)) {
                var owner = Window.GetWindow(grid);
                var message = "The cells were not copied because another application is using the clipboard. Please try again.";
                if (owner is null) {
                    MessageBox.Show(message, "Copy failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else {
                    MessageBox.Show(owner, message, "Copy failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // The edit goes on when the value could not be committed, so the undo step ends only once the DataGrid has finished with the cell.
        private void EicDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
            if (sender is not DataGrid { DataContext: EicSettingViewModel table } grid) {
                return;
            }
            grid.Dispatcher.BeginInvoke(DispatcherPriority.Background, () => {
                if (grid.CurrentCell.Column?.GetCellContent(grid.CurrentCell.Item)?.Parent is not DataGridCell { IsEditing: true }) {
                    table.History.EndEdit();
                }
            });
        }

        private void EicTable_CanUndo(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = sender is FrameworkElement { DataContext: EicSettingViewModel table } && table.History.CanUndo;
            e.Handled = true;
        }

        private void EicTable_Undo(object sender, ExecutedRoutedEventArgs e) {
            e.Handled = true;
            if (sender is FrameworkElement { DataContext: EicSettingViewModel table } element) {
                ShowChanges(FindDescendant<DataGrid>(element), table.History.Undo());
            }
        }

        private void EicTable_CanRedo(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = sender is FrameworkElement { DataContext: EicSettingViewModel table } && table.History.CanRedo;
            e.Handled = true;
        }

        private void EicTable_Redo(object sender, ExecutedRoutedEventArgs e) {
            e.Handled = true;
            if (sender is FrameworkElement { DataContext: EicSettingViewModel table } element) {
                ShowChanges(FindDescendant<DataGrid>(element), table.History.Redo());
            }
        }

        // Selects the cells an undo or redo changed and scrolls to the first, so the change can be seen in a long table.
        private static void ShowChanges(DataGrid? grid, IReadOnlyList<EicTableHistory.Change> changes) {
            if (grid is null || changes.Count == 0) {
                return;
            }
            var cells = changes
                .Select(change => new DataGridCellInfo(change.Row, grid.Columns.FirstOrDefault(column => column.ClipboardContentBinding is Binding { Path.Path: string path } && path.Split('.')[0] == change.Property)))
                .Where(cell => cell.Column is not null)
                .ToList();
            if (cells.Count == 0) {
                return;
            }
            grid.CurrentCell = cells[0];
            grid.SelectedCells.Clear();
            foreach (var cell in cells) {
                grid.SelectedCells.Add(cell);
            }
            grid.ScrollIntoView(cells[0].Item, cells[0].Column);
        }

        private static void StartWith(TextBox textBox, string text) {
            textBox.Text = text;
            textBox.CaretIndex = text.Length;
            textBox.Focus();
        }

        private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) {
                    return t;
                }
                if (FindDescendant<T>(child) is T found) {
                    return found;
                }
            }
            return null;
        }
    }
}
