using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Graphics.UI;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.App.Msdial.View.Chart.Tests;

/// <summary>
/// Ctrl+C, Ctrl+Z and Ctrl+Y in the EIC setting table of the Display chromatograms dialog, with the table its view builds.
/// </summary>
[TestClass]
public class EicTableKeyTests
{
    private static readonly string[] Formats = [DataFormats.Html, DataFormats.Text, DataFormats.UnicodeText, DataFormats.CommaSeparatedValue];

    [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();

    private sealed class Table
    {
        public List<PeakFeatureSearchValueViewModel> Rows = null!;
        public DataGrid Grid = null!;
        public Window Window = null!;

        public void Select(params (int Row, int Column)[] cells) {
            Grid.SelectedCells.Clear();
            foreach (var (row, column) in cells) {
                Grid.SelectedCells.Add(new DataGridCellInfo(Rows[row], Grid.Columns[column]));
            }
        }
    }

    private static void OnUi(Action action) => Application.Current.Dispatcher.Invoke(action);

    private static Table Open() {
        TestApplication.Ensure();
        var table = new Table();
        OnUi(() => {
            table.Rows = Enumerable.Range(0, 5)
                .Select(i => new PeakFeatureSearchValueViewModel(new PeakFeatureSearchValueModel(new PeakFeatureSearchValue { Title = "n" + i, Mass = 100 + i, MassTolerance = 0.001 * (i + 1), })))
                .ToList();
            var view = new CheckChromatogramsView();
            // The setting table is the dialog's EicSettingViewModel shown with the view's own templates.
            var host = (ContentControl)((DockItemsControl)view.FindName("Contents")).ContentTemplate.LoadContent();
            host.Content = new EicSettingViewModel(
                new ObservableCollection<PeakFeatureSearchValueViewModel>(table.Rows).ToReadOnlyReactiveCollection(x => x, ImmediateScheduler.Instance),
                new ReactivePropertySlim<bool>(), new ReactivePropertySlim<bool>(), new ReactivePropertySlim<bool>(), new ReactivePropertySlim<bool>(),
                new ReactiveCommand(), new ReactiveCommand(), new ReactiveCommand<PeakFeatureSearchValueViewModel>());
            table.Window = new Window { Content = host, Resources = view.Resources, Width = 1200, Height = 700, Left = -3000, Top = -3000, ShowActivated = false, ShowInTaskbar = false, };
            table.Window.Show();
        });
        TestApplication.WaitForBackgroundWork();
        OnUi(() => table.Grid = Descendants<DataGrid>(table.Window).Single(grid => grid.Name == "Datagrid_EicDiplayProperties"));
        return table;
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) {
                yield return t;
            }
            foreach (var descendant in Descendants<T>(child)) {
                yield return descendant;
            }
        }
    }

    // What DataGrid's own copy puts on the clipboard, with the table's copy binding taken away for the moment.
    private static Dictionary<string, string> CopyAsDataGridDoes(DataGrid grid) {
        var bindings = grid.CommandBindings.Cast<CommandBinding>().ToList();
        grid.CommandBindings.Clear();
        try {
            ApplicationCommands.Copy.Execute(null, grid);
        }
        finally {
            grid.CommandBindings.AddRange(bindings);
        }
        return WithClipboard(() => {
            var data = Clipboard.GetDataObject();
            return Formats.ToDictionary(format => format, format => (string)data.GetData(format));
        });
    }

    // Other programs, such as the clipboard history, open the clipboard whenever it changes, so the test's own reads and writes try again too.
    private static T WithClipboard<T>(Func<T> action) {
        for (int attempt = 1; ; attempt++) {
            try {
                return action();
            }
            catch (ExternalException) when (attempt < 20) {
                Thread.Sleep(100);
            }
        }
    }

    private static void SetClipboardText(string text) => WithClipboard(() => { Clipboard.SetText(text); return 0; });

    private static string GetClipboardText() => WithClipboard(Clipboard.GetText);

    [TestMethod]
    [Timeout(60000)]
    public void CopiesTheSelectedCellsInTheFormatsDataGridUses() {
        var table = Open();
        try {
            foreach (var cells in new[] {
                new[] { (0, 2), (1, 2), (2, 2), },
                new[] { (0, 1), (1, 1), (2, 1), (0, 2), (1, 2), (2, 2), },
                new[] { (0, 1), (2, 2), },
            }) {
                OnUi(() => {
                    table.Select(cells);
                    var expected = CopyAsDataGridDoes(table.Grid);
                    var actual = DataGridSelectionCopy.CreateDataObject(table.Grid)!;
                    foreach (var format in Formats) {
                        Assert.AreEqual(expected[format], (string)actual.GetData(format), format);
                    }
                });
            }
            OnUi(() => {
                table.Select((0, 1), (1, 1), (2, 1), (0, 2), (1, 2), (2, 2));
                SetClipboardText("previous");
                ApplicationCommands.Copy.Execute(null, table.Grid);
                Assert.AreEqual("Exact mass\tMass tolerance [Da]\r\n100\t0.001\r\n101\t0.002\r\n102\t0.003\r\n", GetClipboardText());
            });
        }
        finally {
            OnUi(() => table.Window.Close());
        }
    }

    [TestMethod]
    [Timeout(60000)]
    public void CopyWaitsWhileAnotherProgramHoldsTheClipboard() {
        var table = Open();
        try {
            OnUi(() => {
                SetClipboardText("previous");
                table.Select((0, 1), (1, 1));
            });
            using var opened = new ManualResetEventSlim();
            // DataGrid's own copy waits about a second for the clipboard and then leaves the previous contents in place.
            var holder = new Thread(() => {
                OpenClipboard(IntPtr.Zero);
                opened.Set();
                Thread.Sleep(1500);
                CloseClipboard();
            });
            holder.Start();
            opened.Wait();

            OnUi(() => ApplicationCommands.Copy.Execute(null, table.Grid));
            holder.Join();

            OnUi(() => Assert.AreEqual("Exact mass\r\n100\r\n101\r\n", GetClipboardText()));
        }
        finally {
            OnUi(() => table.Window.Close());
        }
    }

    private static (object, DataGridColumn)[] SelectedCells(DataGrid grid) => grid.SelectedCells.Select(cell => (cell.Item, cell.Column)).ToArray();

    [TestMethod]
    [Timeout(60000)]
    public void UndoAndRedoChangeBackTheCellsAnActionChanged() {
        var table = Open();
        try {
            // Like a paste or Delete: changes to several cells in one go.
            OnUi(() => {
                table.Rows[0].Mass.Value = "200";
                table.Rows[2].MassTolerance.Value = "0.5";
            });
            TestApplication.WaitForBackgroundWork();
            OnUi(() => {
                table.Select((4, 0));
                ApplicationCommands.Undo.Execute(null, table.Grid);
                Assert.AreEqual("100", table.Rows[0].Mass.Value);
                Assert.AreEqual("0.003", table.Rows[2].MassTolerance.Value);
                CollectionAssert.AreEquivalent(new (object, DataGridColumn)[] { (table.Rows[0], table.Grid.Columns[1]), (table.Rows[2], table.Grid.Columns[2]), }, SelectedCells(table.Grid), "The changed cells are selected.");
                Assert.IsFalse(ApplicationCommands.Undo.CanExecute(null, table.Grid));

                // The Clear button takes the focus when it is clicked, and the keys still work there.
                var clear = Descendants<Button>(table.Window).Single(button => "Clear".Equals(button.Content));
                ApplicationCommands.Redo.Execute(null, clear);
                Assert.AreEqual("200", table.Rows[0].Mass.Value);
                Assert.AreEqual("0.5", table.Rows[2].MassTolerance.Value);
            });
        }
        finally {
            OnUi(() => table.Window.Close());
        }
    }

    [TestMethod]
    [Timeout(60000)]
    public void UndoTakesBackAWholeCellEdit() {
        var table = Open();
        try {
            OnUi(() => {
                table.Grid.CurrentCell = new DataGridCellInfo(table.Rows[0], table.Grid.Columns[1]);
                Assert.IsTrue(table.Grid.BeginEdit());
            });
            TestApplication.WaitForBackgroundWork();
            OnUi(() => {
                var textBox = Descendants<TextBox>(table.Grid.Columns[1].GetCellContent(table.Rows[0])).Single();
                foreach (var typed in new[] { "1", "12", "123" }) {
                    textBox.Text = typed;
                }
                Assert.AreEqual("123", table.Rows[0].Mass.Value);
                Assert.IsFalse(ApplicationCommands.Undo.CanExecute(null, table.Grid), "While the cell is edited, its TextBox undoes the typing.");
                Assert.IsTrue(table.Grid.CommitEdit());
            });
            TestApplication.WaitForBackgroundWork();
            OnUi(() => {
                ApplicationCommands.Undo.Execute(null, table.Grid);
                Assert.AreEqual("100", table.Rows[0].Mass.Value);
                Assert.IsFalse(ApplicationCommands.Undo.CanExecute(null, table.Grid));
                ApplicationCommands.Redo.Execute(null, table.Grid);
                Assert.AreEqual("123", table.Rows[0].Mass.Value);
            });
        }
        finally {
            OnUi(() => table.Window.Close());
        }
    }
}
