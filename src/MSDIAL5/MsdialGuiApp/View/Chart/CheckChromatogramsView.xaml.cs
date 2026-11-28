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

        // Delete on selected cells (not while a cell is being edited) clears their values, as one undo step.
        private void EicDataGrid_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key != Key.Delete || Keyboard.Modifiers != ModifierKeys.None || e.OriginalSource is TextBox || sender is not DataGrid { DataContext: EicSettingViewModel table } grid) {
                return;
            }
            var cells = grid.SelectedCells.Where(cell => cell.Item is PeakFeatureSearchValueViewModel && EicProperty(cell.Column) is not null).ToList();
            if (cells.Count == 0) {
                return;
            }
            table.History.Record();
            foreach (var cell in cells) {
                CheckChromatogramsViewModel.ClearEicQueryValue((PeakFeatureSearchValueViewModel)cell.Item, EicProperty(cell.Column)!);
            }
            table.History.Record();
            e.Handled = true;
        }

        // Ctrl+C, Ctrl+X and Ctrl+V anywhere on the tab. These handlers run before DataGrid's and TextBox's own, which drop a copy without a word when the clipboard is busy.
        // On selected cells: Ctrl+C copies them and Ctrl+V pastes into them. While a cell is being edited: Ctrl+C and Ctrl+X take the selected text,
        // Ctrl+C with no text selected copies the cell, and Ctrl+V pastes one value into the text or several cells into the table.
        private void EicTable_CanExecuteClipboard(object sender, CanExecuteRoutedEventArgs e) {
            if (sender is not FrameworkElement root || FindDescendant<DataGrid>(root) is not DataGrid grid) {
                return;
            }
            if (EditingTextBox(grid, e.OriginalSource) is TextBox textBox) {
                e.CanExecute = e.Command == ApplicationCommands.Paste
                    || textBox.SelectionLength > 0
                    || e.Command == ApplicationCommands.Copy && grid.SelectedCells.Count > 0;
            }
            else {
                e.CanExecute = e.Command != ApplicationCommands.Cut && grid.SelectedCells.Count > 0;
            }
            e.Handled = true;
        }

        private void EicTable_ExecuteClipboard(object sender, ExecutedRoutedEventArgs e) {
            if (sender is not FrameworkElement { DataContext: EicSettingViewModel table } root || FindDescendant<DataGrid>(root) is not DataGrid grid) {
                return;
            }
            e.Handled = true;
            var textBox = EditingTextBox(grid, e.OriginalSource);
            if (e.Command == ApplicationCommands.Paste) {
                Paste(grid, table, textBox);
            }
            else if (textBox is { SelectionLength: > 0 }) {
                if (Copy(grid, new WindowsClipboard.Content(textBox.SelectedText)) && e.Command == ApplicationCommands.Cut) {
                    textBox.SelectedText = string.Empty;
                }
            }
            else if (e.Command == ApplicationCommands.Copy && DataGridSelectionCopy.CreateContent(grid) is WindowsClipboard.Content content) {
                Copy(grid, content);
            }
        }

        private static bool Copy(DataGrid grid, WindowsClipboard.Content content) {
            if (WindowsClipboard.TryWrite(content, WindowsClipboard.DefaultTimeout)) {
                return true;
            }
            Warn(grid, "Nothing was copied because another application is using the clipboard. Please try again.", "Copy failed");
            return false;
        }

        private static void Paste(DataGrid grid, EicSettingViewModel table, TextBox? textBox) {
            if (!WindowsClipboard.TryReadText(WindowsClipboard.DefaultTimeout, out var text)) {
                Warn(grid, "Nothing was pasted because another application is using the clipboard. Please try again.", "Paste failed");
                return;
            }
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            var values = DataGridRangePaste.Parse(text!);
            if (textBox is not null) {
                if (values.Count <= 1 && values.All(row => row.Count <= 1)) {
                    // One value goes into the text being edited, without the line break Excel adds after it.
                    textBox.SelectedText = values.FirstOrDefault()?.FirstOrDefault() ?? string.Empty;
                    textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
                    return;
                }
                // Several cells end the edit and are pasted from the edited cell on.
            }
            EndCellEdit(grid, table);

            var cells = DataGridRangePaste.Plan(grid, values);
            if (cells.Count == 0) {
                return;
            }
            table.History.Record();
            foreach (var cell in cells) {
                if (cell.Item is PeakFeatureSearchValueViewModel row && EicProperty(cell.Column) is string property) {
                    CheckChromatogramsViewModel.SetEicQueryValue(row, property, cell.Value);
                }
            }
            table.History.Record();
            Select(grid, cells.Select(cell => new DataGridCellInfo(cell.Item, cell.Column)).ToList());
        }

        // Ends a cell edit that is still going on, so what it changed is an undo step before the paste.
        private static void EndCellEdit(DataGrid grid, EicSettingViewModel table) {
            if (table.History.IsEditing) {
                grid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
                table.History.EndEdit();
            }
        }

        // The TextBox of the cell being edited, when the command comes from it.
        private static TextBox? EditingTextBox(DataGrid grid, object source) {
            return source is TextBox textBox && grid.IsAncestorOf(textBox) ? textBox : null;
        }

        private static void Warn(DataGrid grid, string message, string caption) {
            if (Window.GetWindow(grid) is Window owner) {
                MessageBox.Show(owner, message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else {
                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        // Selects the cells an undo or redo changed, so the change can be seen in a long table.
        private static void ShowChanges(DataGrid? grid, IReadOnlyList<EicTableHistory.Change> changes) {
            if (grid is null) {
                return;
            }
            Select(grid, changes
                .Select(change => new DataGridCellInfo(change.Row, grid.Columns.FirstOrDefault(column => EicProperty(column) == change.Property)))
                .Where(cell => cell.Column is not null)
                .ToList());
        }

        // Selects the cells and scrolls to the first.
        private static void Select(DataGrid grid, IReadOnlyList<DataGridCellInfo> cells) {
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

        // The row property a column shows: the first part of its clipboard binding path, e.g. "Mass" in "Mass.Value".
        private static string? EicProperty(DataGridColumn? column) {
            return column?.ClipboardContentBinding is Binding { Path.Path: string path } ? path.Split('.')[0] : null;
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
