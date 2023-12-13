using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.CommonMVVM
{
    public sealed class DataGridPasteCommand : ICommand
    {
        public static DataGridPasteCommand Instance { get; } = new DataGridPasteCommand();

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            if (!(parameter is DataGrid datagrid)) {
                return;
            }

            if (datagrid.SelectedCells.Count == 0) {
                return;
            }
            var leftTopCell = datagrid.SelectedCells[0];
            if (datagrid.SelectionUnit == DataGridSelectionUnit.FullRow) {
                leftTopCell = datagrid.CurrentCell;
            }
            var clipText = Clipboard.GetText().Replace("\r\n", "\n").TrimEnd().Split('\n').Select(row => row.Split('\t')).ToArray();

            var items = datagrid.Items.Cast<object>().ToList();
            var startRow = items.IndexOf(leftTopCell.Item);
            var startCol = leftTopCell.Column.DisplayIndex;
            var errorRows = new PastingFailedRows();

            for (int i = 0; i < clipText.Length; i++)
            {
                if (i + startRow >= items.Count) break;
                var datas = clipText[i];
                var item = items[i + startRow];

                datagrid.BeginEdit();
                for (int j = 0; j < datas.Length; j++)
                {
                    if (j + startCol >= datagrid.Columns.Count) break;
                    var data = datas[j];
                    datagrid.Columns[j + startCol].OnPastingCellClipboardContent(item, data);
                }
                var row = datagrid.ItemContainerGenerator.ContainerFromItem(item);
                if (Validation.GetHasError(row)) {
                    datagrid.CancelEdit(DataGridEditingUnit.Row);
                    errorRows.Add(i + 1, string.Join("\t", datas));
                }
                else {
                    datagrid.CommitEdit();
                }
            }

            datagrid.CurrentCell = new DataGridCellInfo(datagrid.Items[startRow], datagrid.Columns[startCol]);
            if (errorRows.HasErrors) {
                MessageBox.Show(Window.GetWindow(datagrid), errorRows.ToString(), "Pasting error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        sealed class PastingFailedRows {
            private readonly List<PastingFailedRow> _rows;

            public PastingFailedRows() {
                _rows = new List<PastingFailedRow>();
                Rows = _rows.AsReadOnly();
            }

            public ReadOnlyCollection<PastingFailedRow> Rows { get; }

            public bool HasErrors => Rows.Any();

            public void Add(int lineNumber, string content) {
                _rows.Add(new PastingFailedRow(lineNumber, content));
            }

            public override string ToString() {
                var errors = new[]
                {
                    $"Falied to paste lines {string.Join(",", _rows.Select(row => row.LineNumber))}.",
                    "",
                    string.Join(Environment.NewLine, _rows.Select(row => $"Line {row.LineNumber}: {row.Content}")),
                };
                return string.Join(Environment.NewLine, errors);
            }
        }

        sealed class PastingFailedRow {
            public PastingFailedRow(int lineNumber, string content) {
                LineNumber = lineNumber;
                Content = content;
            }

            public int LineNumber { get; }
            public string Content { get; }
        }

    }
}
