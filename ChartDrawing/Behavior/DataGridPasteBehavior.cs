using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.Graphics.Behavior
{
    public class DataGridPasteBehavior
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
            if (!(sender is DataGrid datagrid)) return;

            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var clipText = Clipboard.GetText().Replace("\r\n", "\n").TrimEnd().Split('\n').Select(row => row.Split('\t')).ToArray();

                var items = datagrid.Items.Cast<object>().ToList();
                var startRow = items.IndexOf(datagrid.CurrentItem);
                var startCol = datagrid.Columns.IndexOf(datagrid.CurrentCell.Column);
                var errorRows = new PastingFailedRows();

                for (int i = 0; i < clipText.Length; i++)
                {
                    if (i + startRow >= items.Count) break;
                    var datas = clipText[i];
                    var item = items[i + startRow];

                    datagrid.BeginEdit();
                    var row = datagrid.ItemContainerGenerator.ContainerFromItem(item);
                    for (int j = 0; j < datas.Length; j++)
                    {
                        if (j + startCol >= datagrid.Columns.Count) break;
                        var data = datas[j];
                        datagrid.Columns[j + startCol].OnPastingCellClipboardContent(item, data);
                    }
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
                    MessageBox.Show(System.Windows.Window.GetWindow(datagrid), errorRows.ToString(), "Pasting error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                e.Handled = true;
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
                    string.Join(System.Environment.NewLine, _rows.Select(row => $"Line {row.LineNumber}: {row.Content}")),
                };
                return string.Join(System.Environment.NewLine, errors);
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
