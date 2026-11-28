using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Chart;

/// <summary>
/// Copies the selected cells of a DataGrid in the same formats as DataGrid's own copy, without losing the copy when the clipboard is busy.
/// </summary>
/// <remarks>
/// DataGrid's own copy tries to open the clipboard for about a second.
/// If another program (Excel, a clipboard manager, a remote desktop session) holds it longer, DataGrid gives up without telling anyone.
/// The clipboard then still holds what was copied before, and that is what gets pasted.
/// </remarks>
internal static class DataGridSelectionCopy
{
    private const int Attempts = 3;
    private const string HtmlHeader = "Version:1.0\r\nStartHTML:{0:00000000}\r\nEndHTML:{1:00000000}\r\nStartFragment:{2:00000000}\r\nEndFragment:{3:00000000}\r\n";
    private const string HtmlStart = "<HTML>\r\n<BODY>\r\n<!--StartFragment-->";
    private const string HtmlEnd = "<!--EndFragment-->\r\n</BODY>\r\n</HTML>";
    private static readonly string[] Formats = [DataFormats.Html, DataFormats.Text, DataFormats.UnicodeText, DataFormats.CommaSeparatedValue];

    /// <summary>
    /// The selected cells as DataGrid copies them: the columns from the leftmost to the rightmost selected cell, the rows that have a selected cell,
    /// the headers first when <see cref="DataGrid.ClipboardCopyMode"/> includes them, and empty values for cells in that range that are not selected.
    /// Null when nothing is selected.
    /// </summary>
    public static DataObject? CreateDataObject(DataGrid grid) {
        var cells = grid.SelectedCells.Where(cell => cell.IsValid && cell.Column.Visibility == Visibility.Visible).ToList();
        if (cells.Count == 0) {
            return null;
        }
        var first = cells.Min(cell => cell.Column.DisplayIndex);
        var last = cells.Max(cell => cell.Column.DisplayIndex);
        var columns = grid.Columns.Where(column => column.Visibility == Visibility.Visible && column.DisplayIndex >= first && column.DisplayIndex <= last).OrderBy(column => column.DisplayIndex).ToList();
        var selected = new HashSet<(object, DataGridColumn)>(cells.Select(cell => (cell.Item, cell.Column)));
        var selectedItems = new HashSet<object>(cells.Select(cell => cell.Item));

        var rows = new List<DataGridRowClipboardEventArgs>();
        if (grid.ClipboardCopyMode == DataGridClipboardCopyMode.IncludeHeader) {
            var header = new DataGridRowClipboardEventArgs(null, first, last, isColumnHeadersRow: true);
            header.ClipboardRowContent.AddRange(columns.Select(column => new DataGridClipboardCellContent(null, column, column.Header)));
            rows.Add(header);
        }
        foreach (var item in grid.Items) {
            if (!selectedItems.Contains(item)) {
                continue;
            }
            var row = new DataGridRowClipboardEventArgs(item, first, last, isColumnHeadersRow: false);
            row.ClipboardRowContent.AddRange(columns.Select(column => new DataGridClipboardCellContent(item, column, selected.Contains((item, column)) ? column.OnCopyingCellClipboardContent(item) : null)));
            rows.Add(row);
        }

        var data = new DataObject();
        foreach (var format in Formats) {
            var content = string.Concat(rows.Select(row => row.FormatClipboardCellValues(format)));
            data.SetData(format, format == DataFormats.Html ? WrapHtml(content) : content, autoConvert: false);
        }
        return data;
    }

    /// <summary>
    /// Puts the data on the clipboard, trying again while another program holds it. False when the clipboard stayed busy.
    /// </summary>
    public static bool TrySetClipboard(DataObject data) {
        for (int attempt = 1; ; attempt++) {
            try {
                // Each call already retries for about a second.
                Clipboard.SetDataObject(data, copy: true);
                return true;
            }
            catch (ExternalException) when (attempt < Attempts) {
                Thread.Sleep(100);
            }
            catch (ExternalException) {
                return false;
            }
        }
    }

    // The CF_HTML format: a header with the byte offsets of the HTML and of the copied fragment, as DataGrid writes it.
    // DataGrid ends the fragment after the line break that follows the table.
    private static string WrapHtml(string rows) {
        var fragment = "<TABLE>" + rows + "</TABLE>\r\n";
        var startHtml = string.Format(HtmlHeader, 0, 0, 0, 0).Length;
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(HtmlStart);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
        var endHtml = endFragment + Encoding.UTF8.GetByteCount(HtmlEnd);
        return string.Format(HtmlHeader, startHtml, endHtml, startFragment, endFragment) + HtmlStart + fragment + HtmlEnd;
    }
}
