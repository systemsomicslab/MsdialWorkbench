using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CompMs.App.Msdial.View.Chart;

/// <summary>
/// Where pasted text goes in a DataGrid, as in Excel: a block of cells starts at the top left cell of the selection, and a single value fills every selected cell.
/// </summary>
internal static class DataGridRangePaste
{
    /// <summary>One value to put into one cell.</summary>
    public sealed class Cell(object item, DataGridColumn column, string value)
    {
        public object Item { get; } = item;
        public DataGridColumn Column { get; } = column;
        public string Value { get; } = value;
    }

    /// <summary>
    /// The rows and cells of copied text: rows end with a line break, cells are separated by tabs, and a cell in double quotes may hold either (as Excel copies them).
    /// The line break after the last row is not a row of its own.
    /// </summary>
    public static List<List<string>> Parse(string text) {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var atCellStart = true;
        for (int i = 0; i < text.Length; i++) {
            var c = text[i];
            if (atCellStart && c == '"' && text.IndexOf('"', i + 1) > 0) {
                for (i++; i < text.Length; i++) {
                    if (text[i] != '"') {
                        cell.Append(text[i]);
                    }
                    else if (i + 1 < text.Length && text[i + 1] == '"') {
                        cell.Append('"');
                        i++;
                    }
                    else {
                        break;
                    }
                }
                atCellStart = false;
            }
            else if (c == '\t') {
                row.Add(cell.ToString());
                cell.Clear();
                atCellStart = true;
            }
            else if (c == '\r' || c == '\n') {
                row.Add(cell.ToString());
                cell.Clear();
                rows.Add(row);
                row = new List<string>();
                atCellStart = true;
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') {
                    i++;
                }
            }
            else {
                cell.Append(c);
                atCellStart = false;
            }
        }
        if (!atCellStart || cell.Length > 0 || row.Count > 0) {
            row.Add(cell.ToString());
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>
    /// The cells the pasted values go into, with the values trimmed. Cells of read-only columns, and rows or columns past the end of the table, are left out.
    /// </summary>
    public static List<Cell> Plan(DataGrid grid, IReadOnlyList<IReadOnlyList<string>> values) {
        var result = new List<Cell>();
        if (values.Count == 0 || values.All(row => row.Count == 0)) {
            return result;
        }
        var selected = grid.SelectedCells.Where(cell => cell.IsValid && cell.Column.Visibility == Visibility.Visible).ToList();
        if (selected.Count == 0 && grid.CurrentCell.IsValid) {
            selected.Add(grid.CurrentCell);
        }
        if (selected.Count == 0) {
            return result;
        }

        if (values.Count == 1 && values[0].Count == 1) {
            var value = values[0][0].Trim();
            result.AddRange(selected.Where(cell => IsEditable(cell.Column)).Select(cell => new Cell(cell.Item, cell.Column, value)));
            return result;
        }

        var items = grid.Items.Cast<object>().Where(item => item != CollectionView.NewItemPlaceholder).ToList();
        var columns = grid.Columns.Where(column => column.Visibility == Visibility.Visible).OrderBy(column => column.DisplayIndex).ToList();
        var top = selected.Min(cell => items.IndexOf(cell.Item));
        var left = selected.Min(cell => columns.IndexOf(cell.Column));
        if (top < 0 || left < 0) {
            return result;
        }
        // The table copies its headers too, so a copy pasted back starts with them; they are not values.
        var first = IsHeaderRow(values[0], columns, left) ? 1 : 0;
        for (int i = first; i < values.Count && top + i - first < items.Count; i++) {
            for (int j = 0; j < values[i].Count && left + j < columns.Count; j++) {
                var column = columns[left + j];
                if (IsEditable(column)) {
                    result.Add(new Cell(items[top + i - first], column, values[i][j].Trim()));
                }
            }
        }
        return result;
    }

    private static bool IsHeaderRow(IReadOnlyList<string> row, IReadOnlyList<DataGridColumn> columns, int left) {
        return row.Count > 0
            && left + row.Count <= columns.Count
            && row.Select((value, j) => (value.Trim(), columns[left + j].Header?.ToString() ?? string.Empty)).All(pair => pair.Item1 == pair.Item2);
    }

    private static bool IsEditable(DataGridColumn column) => !column.IsReadOnly && column.ClipboardContentBinding is not null;
}
