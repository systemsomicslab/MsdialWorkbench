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
using System.Diagnostics;
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
/// The keys of the EIC setting table in the Display chromatograms dialog (copy, cut, paste, Delete, undo and redo), with the table its view builds.
/// </summary>
/// <remarks>
/// The clipboard is shared with every other program on the machine. When it stays busy for seconds, a test cannot tell anything and is inconclusive.
/// </remarks>
[TestClass]
public class EicTableKeyTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetClipboardData(uint format);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr memory);

    private static IntPtr CreateMessageWindow() => CreateWindowEx(0, "STATIC", "clipboard test", 0, 0, 0, 0, 0, new IntPtr(-3), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

    private sealed class Table
    {
        public List<PeakFeatureSearchValueViewModel> Rows = null!;
        public EicSettingViewModel ViewModel = null!;
        public DataGrid Grid = null!;
        public Window Window = null!;

        public DataGridColumn Name => Grid.Columns[0];
        public DataGridColumn Mass => Grid.Columns[1];
        public DataGridColumn Tolerance => Grid.Columns[2];

        public void Select(params (int Row, int Column)[] cells) {
            Grid.SelectedCells.Clear();
            foreach (var (row, column) in cells) {
                Grid.SelectedCells.Add(new DataGridCellInfo(Rows[row], Grid.Columns[column]));
            }
        }

        public string Values(int row) => $"{Rows[row].Title.Value}|{Rows[row].Mass.Value}|{Rows[row].MassTolerance.Value}";

        public (object, DataGridColumn)[] SelectedCells() => Grid.SelectedCells.Select(cell => (cell.Item, cell.Column)).ToArray();

        // Starts editing the cell and returns its editing TextBox.
        public TextBox Edit(int row, int column) {
            TextBox textBox = null!;
            OnUi(() => {
                Grid.CurrentCell = new DataGridCellInfo(Rows[row], Grid.Columns[column]);
                Grid.SelectedCells.Clear();
                Grid.SelectedCells.Add(Grid.CurrentCell);
                Assert.IsTrue(Grid.BeginEdit());
            });
            TestApplication.WaitForBackgroundWork();
            OnUi(() => textBox = Descendants<TextBox>(Grid.Columns[column].GetCellContent(Rows[row])).Single());
            return textBox;
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
            table.ViewModel = new EicSettingViewModel(
                new ObservableCollection<PeakFeatureSearchValueViewModel>(table.Rows).ToReadOnlyReactiveCollection(x => x, ImmediateScheduler.Instance),
                new ReactivePropertySlim<bool>(), new ReactivePropertySlim<bool>(), new ReactivePropertySlim<bool>(), new ReactivePropertySlim<bool>(),
                new ReactiveCommand(), new ReactiveCommand(), new ReactiveCommand<PeakFeatureSearchValueViewModel>());
            host.Content = table.ViewModel;
            table.Window = new Window { Content = host, Resources = view.Resources, Width = 1200, Height = 700, Left = -3000, Top = -3000, ShowActivated = false, ShowInTaskbar = false, };
            table.Window.Show();
        });
        TestApplication.WaitForBackgroundWork();
        OnUi(() => table.Grid = Descendants<DataGrid>(table.Window).Single(grid => grid.Name == "Datagrid_EicDiplayProperties"));
        return table;
    }

    private static void Run(Action<Table> test) {
        var table = Open();
        try {
            test(table);
        }
        finally {
            OnUi(() => table.Window.Close());
        }
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

    private static void SetClipboardText(string text) {
        if (!WindowsClipboard.TryWrite(new WindowsClipboard.Content(text), TestTimeout)) {
            Assert.Inconclusive("Another program kept the clipboard busy.");
        }
    }

    private static string? GetClipboardText() {
        if (!WindowsClipboard.TryReadText(TestTimeout, out var text)) {
            Assert.Inconclusive("Another program kept the clipboard busy.");
        }
        return text;
    }

    // Keeps the clipboard open on another thread with a window of its own, as another program does, until the returned object is disposed.
    // Opened without a window, the clipboard can still be opened by the other threads of the process.
    private sealed class ClipboardHolder : IDisposable
    {
        private readonly ManualResetEventSlim _opened = new();
        private readonly ManualResetEventSlim _release = new();
        private readonly Thread _thread;

        public ClipboardHolder() {
            _thread = new Thread(() => {
                var window = CreateMessageWindow();
                try {
                    var deadline = Stopwatch.StartNew();
                    while (!OpenClipboard(window)) {
                        if (deadline.Elapsed > TestTimeout) {
                            return;
                        }
                        Thread.Sleep(1);
                    }
                    _opened.Set();
                    _release.Wait();
                    CloseClipboard();
                }
                finally {
                    DestroyWindow(window);
                }
            });
            _thread.Start();
            if (!_opened.Wait(TestTimeout)) {
                _release.Set();
                Assert.Inconclusive("Another program kept the clipboard busy.");
            }
        }

        public void ReleaseAfter(TimeSpan delay) {
            new Thread(() => {
                Thread.Sleep(delay);
                _release.Set();
            }).Start();
        }

        public void Dispose() {
            _release.Set();
            _thread.Join();
        }
    }

    // What DataGrid's own copy put on the clipboard for these selections. Its copy is not run here, because it fails now and then (see WindowsClipboard).
    private static readonly ((int Row, int Column)[] Cells, string Text, string Html, string Csv)[] DataGridCopies = [
        ([(0, 2), (1, 2), (2, 2)],
            "Mass tolerance [Da]\r\n0.001\r\n0.002\r\n0.003\r\n",
            "Version:1.0\r\nStartHTML:00000097\r\nEndHTML:00000292\r\nStartFragment:00000133\r\nEndFragment:00000256\r\n<HTML>\r\n<BODY>\r\n<!--StartFragment--><TABLE><TR><TD>Mass tolerance [Da]</TD></TR><TR><TD>0.001</TD></TR><TR><TD>0.002</TD></TR><TR><TD>0.003</TD></TR></TABLE>\r\n<!--EndFragment-->\r\n</BODY>\r\n</HTML>",
            "Mass tolerance [Da]\r\n0.001\r\n0.002\r\n0.003\r\n"),
        ([(0, 1), (1, 1), (2, 1), (0, 2), (1, 2), (2, 2)],
            "Exact mass\tMass tolerance [Da]\r\n100\t0.001\r\n101\t0.002\r\n102\t0.003\r\n",
            "Version:1.0\r\nStartHTML:00000097\r\nEndHTML:00000347\r\nStartFragment:00000133\r\nEndFragment:00000311\r\n<HTML>\r\n<BODY>\r\n<!--StartFragment--><TABLE><TR><TD>Exact mass</TD><TD>Mass tolerance [Da]</TD></TR><TR><TD>100</TD><TD>0.001</TD></TR><TR><TD>101</TD><TD>0.002</TD></TR><TR><TD>102</TD><TD>0.003</TD></TR></TABLE>\r\n<!--EndFragment-->\r\n</BODY>\r\n</HTML>",
            "Exact mass,Mass tolerance [Da]\r\n100,0.001\r\n101,0.002\r\n102,0.003\r\n"),
        ([(0, 1), (2, 2)],
            "Exact mass\tMass tolerance [Da]\r\n100\t\r\n\t0.003\r\n",
            "Version:1.0\r\nStartHTML:00000097\r\nEndHTML:00000316\r\nStartFragment:00000133\r\nEndFragment:00000280\r\n<HTML>\r\n<BODY>\r\n<!--StartFragment--><TABLE><TR><TD>Exact mass</TD><TD>Mass tolerance [Da]</TD></TR><TR><TD>100</TD><TD>&nbsp;</TD></TR><TR><TD>&nbsp;</TD><TD>0.003</TD></TR></TABLE>\r\n<!--EndFragment-->\r\n</BODY>\r\n</HTML>",
            "Exact mass,Mass tolerance [Da]\r\n100,\r\n,0.003\r\n"),
    ];

    [TestMethod]
    [Timeout(60000)]
    public void CopiesTheSelectedCellsInTheFormatsDataGridUses() => Run(table => {
        foreach (var copy in DataGridCopies) {
            OnUi(() => {
                table.Select(copy.Cells);
                var actual = DataGridSelectionCopy.CreateContent(table.Grid)!;
                Assert.AreEqual(copy.Text, actual.Text);
                Assert.AreEqual(copy.Html, actual.Html);
                Assert.AreEqual(copy.Csv, actual.Csv);
            });
        }
        OnUi(() => {
            table.Select((0, 1), (1, 1), (2, 1), (0, 2), (1, 2), (2, 2));
            SetClipboardText("previous");
            ApplicationCommands.Copy.Execute(null, table.Grid);
            Assert.AreEqual("Exact mass\tMass tolerance [Da]\r\n100\t0.001\r\n101\t0.002\r\n102\t0.003\r\n", GetClipboardText());

            // The cells are copied while the focus is on the Clear button too.
            table.Select((3, 0));
            var clear = Descendants<Button>(table.Window).Single(button => "Clear".Equals(button.Content));
            ApplicationCommands.Copy.Execute(null, clear);
            Assert.AreEqual("Name\r\nn3\r\n", GetClipboardText());
        });
    });

    [TestMethod]
    [Timeout(60000)]
    public void CopyWaitsWhileAnotherProgramHoldsTheClipboard() => Run(table => {
        OnUi(() => {
            SetClipboardText("previous");
            table.Select((0, 1), (1, 1));
        });
        // DataGrid's own copy waits about a second for the clipboard and then leaves the previous contents in place.
        using (var holder = new ClipboardHolder()) {
            holder.ReleaseAfter(TimeSpan.FromSeconds(1.2));
            OnUi(() => ApplicationCommands.Copy.Execute(null, table.Grid));
        }
        OnUi(() => Assert.AreEqual("Exact mass\r\n100\r\n101\r\n", GetClipboardText()));
    });

    [TestMethod]
    [Timeout(60000)]
    public void AWriteGivesUpWhenTheClipboardStaysBusy() {
        TestApplication.Ensure();
        OnUi(() => SetClipboardText("previous"));
        using (var holder = new ClipboardHolder()) {
            var elapsed = Stopwatch.StartNew();
            var written = true;
            OnUi(() => written = WindowsClipboard.TryWrite(new WindowsClipboard.Content("new"), TimeSpan.FromSeconds(0.5)));
            Assert.IsFalse(written);
            Assert.IsTrue(elapsed.Elapsed < TimeSpan.FromSeconds(3), elapsed.Elapsed.ToString());
            Assert.IsFalse(WindowsClipboard.TryReadText(TimeSpan.FromSeconds(0.2), out _), "A read gives up too.");
        }
        OnUi(() => Assert.AreEqual("previous", GetClipboardText()));
    }

    // What the clipboard history does after a WPF copy: it opens the clipboard and asks this thread to render the data, and keeps the clipboard open until it is answered.
    [TestMethod]
    [Timeout(60000)]
    public void AWriteAnswersARenderRequestFromAnotherProgram() {
        TestApplication.Ensure();
        string? readByOther = null;
        Thread reader = null!;
        var written = false;
        var took = TimeSpan.Zero;
        OnUi(() => {
            // A WPF copy that is not flushed leaves its data to be rendered on request by this thread.
            for (int attempt = 0; ; attempt++) {
                try {
                    Clipboard.SetDataObject(new DataObject(DataFormats.UnicodeText, "rendered on request"), copy: false);
                    break;
                }
                catch (ExternalException) when (attempt < 10) {
                }
            }
            using var opened = new ManualResetEventSlim();
            reader = new Thread(() => {
                var window = CreateMessageWindow();
                try {
                    while (!OpenClipboard(window)) {
                        Thread.Sleep(1);
                    }
                    opened.Set();
                    Thread.Sleep(200);
                    var handle = GetClipboardData(13);
                    if (handle != IntPtr.Zero) {
                        readByOther = Marshal.PtrToStringUni(GlobalLock(handle));
                        GlobalUnlock(handle);
                    }
                    CloseClipboard();
                }
                finally {
                    DestroyWindow(window);
                }
            });
            reader.Start();
            opened.Wait();
            var elapsed = Stopwatch.StartNew();
            written = WindowsClipboard.TryWrite(new WindowsClipboard.Content("new"), TimeSpan.FromSeconds(2));
            took = elapsed.Elapsed;
        });
        reader.Join();
        Assert.AreEqual("rendered on request", readByOther);
        Assert.IsTrue(written, "The write waited until it gave up.");
        Assert.IsTrue(took < TimeSpan.FromSeconds(1.5), took.ToString());
        OnUi(() => Assert.AreEqual("new", GetClipboardText()));
    }

    [TestMethod]
    [Timeout(60000)]
    public void CopyAndCutWhileACellIsEdited() => Run(table => {
        var textBox = table.Edit(0, 1);
        OnUi(() => {
            SetClipboardText("previous");
            // With no text selected, the TextBox would copy nothing and leave the previous contents; the cell is copied instead.
            textBox.Select(textBox.Text.Length, 0);
            Assert.IsTrue(ApplicationCommands.Copy.CanExecute(null, textBox));
            ApplicationCommands.Copy.Execute(null, textBox);
            Assert.AreEqual("Exact mass\r\n100\r\n", GetClipboardText());

            textBox.Select(1, 2);
            ApplicationCommands.Copy.Execute(null, textBox);
            Assert.AreEqual("00", GetClipboardText());

            textBox.Select(0, 1);
            ApplicationCommands.Cut.Execute(null, textBox);
            Assert.AreEqual("1", GetClipboardText());
            Assert.AreEqual("00", textBox.Text);
            Assert.AreEqual("00", table.Rows[0].Mass.Value);
        });
    });

    [TestMethod]
    [Timeout(60000)]
    public void PastesABlockFromTheTopLeftOfTheSelection() => Run(table => {
        OnUi(() => {
            SetClipboardText("1\t0.1\r\n2\t0.2\r\n3\t0.3\r\n");
            // Selected from the bottom right, so DataGrid lists that cell first.
            table.Select((2, 2), (1, 1), (2, 1), (1, 2));
            ApplicationCommands.Paste.Execute(null, table.Grid);

            Assert.AreEqual("n0|100|0.001", table.Values(0));
            Assert.AreEqual("n1|1|0.1", table.Values(1));
            Assert.AreEqual("n2|2|0.2", table.Values(2));
            Assert.AreEqual("n3|3|0.3", table.Values(3));
            Assert.AreEqual("n4|104|0.005", table.Values(4));
            Assert.AreEqual(6, table.Grid.SelectedCells.Count, "The pasted cells are selected.");

            ApplicationCommands.Undo.Execute(null, table.Grid);
            Assert.AreEqual("n1|101|0.002", table.Values(1));
            Assert.AreEqual("n3|103|0.004", table.Values(3));
            Assert.IsFalse(table.ViewModel.History.CanUndo, "The paste is one undo step.");
        });
    });

    [TestMethod]
    [Timeout(60000)]
    public void PastesOneValueIntoEverySelectedCell() => Run(table => {
        OnUi(() => {
            SetClipboardText("0.05\r\n");
            table.Select((0, 2), (1, 2), (3, 2), (3, 3));
            ApplicationCommands.Paste.Execute(null, table.Grid);
            CollectionAssert.AreEqual(new[] { "0.05", "0.05", "0.003", "0.05", "0.005" }, table.Rows.Select(row => row.MassTolerance.Value).ToArray());

            ApplicationCommands.Undo.Execute(null, table.Grid);
            CollectionAssert.AreEqual(new[] { "0.001", "0.002", "0.003", "0.004", "0.005" }, table.Rows.Select(row => row.MassTolerance.Value).ToArray());
        });
    });

    [TestMethod]
    [Timeout(60000)]
    public void PastesWhatExcelCopiesAndStopsAtTheEdges() => Run(table => {
        OnUi(() => {
            // Four columns from Name: the last one falls on the clear button column and is left out. Three rows from the fourth row: only two fit.
            SetClipboardText("\"a \"\"b\"\"\"\t 200 \t0.2\tx\r\nc\t201\t0.3\tx\r\nd\t202\t0.4\tx\r\n");
            table.Select((3, 0));
            ApplicationCommands.Paste.Execute(null, table.Grid);
            Assert.AreEqual("a \"b\"|200|0.2", table.Values(3));
            Assert.AreEqual("c|201|0.3", table.Values(4));
            Assert.AreEqual("n2|102|0.003", table.Values(2));

            // A value that is not a number shows as an error in its cell, and a later paste fixes it.
            SetClipboardText("abc");
            table.Select((0, 1));
            ApplicationCommands.Paste.Execute(null, table.Grid);
            Assert.AreEqual("abc", table.Rows[0].Mass.Value);
            Assert.IsTrue(table.Rows[0].HasErrors.Value);
            SetClipboardText("150");
            ApplicationCommands.Paste.Execute(null, table.Grid);
            Assert.AreEqual("150", table.Rows[0].Mass.Value);
            Assert.IsFalse(table.Rows[0].HasErrors.Value);
        });
    });

    [TestMethod]
    [Timeout(60000)]
    public void PastesACopyOfTheTableBackWithoutItsHeaders() => Run(table => {
        OnUi(() => {
            table.Select((0, 1), (1, 1), (0, 2), (1, 2));
            ApplicationCommands.Copy.Execute(null, table.Grid);
            table.Select((3, 1));
            ApplicationCommands.Paste.Execute(null, table.Grid);
            Assert.AreEqual("n3|100|0.001", table.Values(3));
            Assert.AreEqual("n4|101|0.002", table.Values(4));

            // Whole rows, selected by their headers, take the clear button column along, with an empty header.
            table.Grid.SelectedCells.Clear();
            foreach (var column in table.Grid.Columns) {
                table.Grid.SelectedCells.Add(new DataGridCellInfo(table.Rows[2], column));
            }
            ApplicationCommands.Copy.Execute(null, table.Grid);
            table.Select((0, 0));
            ApplicationCommands.Paste.Execute(null, table.Grid);
            Assert.AreEqual("n2|102|0.003", table.Values(0));
            Assert.IsFalse(table.Rows[0].HasErrors.Value);
        });
    });

    [TestMethod]
    [Timeout(60000)]
    public void PasteWhileACellIsEdited() => Run(table => {
        var textBox = table.Edit(0, 1);
        OnUi(() => {
            // One value goes into the text at the caret, without Excel's line break.
            SetClipboardText("5\r\n");
            textBox.Select(1, 0);
            ApplicationCommands.Paste.Execute(null, textBox);
            Assert.AreEqual("1500", textBox.Text);
            Assert.AreEqual("1500", table.Rows[0].Mass.Value);

            // Several cells end the edit and are pasted from the edited cell.
            SetClipboardText("300\t0.3\r\n301\t0.4\r\n");
            ApplicationCommands.Paste.Execute(null, textBox);
            Assert.AreEqual("n0|300|0.3", table.Values(0));
            Assert.AreEqual("n1|301|0.4", table.Values(1));
            Assert.IsFalse(table.ViewModel.History.IsEditing);

            ApplicationCommands.Undo.Execute(null, table.Grid);
            Assert.AreEqual("n0|1500|0.001", table.Values(0), "The paste is undone first.");
            Assert.AreEqual("n1|101|0.002", table.Values(1));
            ApplicationCommands.Undo.Execute(null, table.Grid);
            Assert.AreEqual("n0|100|0.001", table.Values(0), "Then the edit.");
        });
    });

    [TestMethod]
    [Timeout(60000)]
    public void DeleteIsOneUndoStep() => Run(table => {
        OnUi(() => {
            table.Select((0, 0), (1, 1), (2, 2));
            table.Grid.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(table.Grid), 0, Key.Delete) { RoutedEvent = Keyboard.PreviewKeyDownEvent, });
            Assert.AreEqual("|100|0.001", table.Values(0));
            Assert.AreEqual("n1|0|0.002", table.Values(1));
            Assert.AreEqual("n2|102|0.01", table.Values(2));

            ApplicationCommands.Undo.Execute(null, table.Grid);
            Assert.AreEqual("n0|100|0.001", table.Values(0));
            Assert.AreEqual("n1|101|0.002", table.Values(1));
            Assert.AreEqual("n2|102|0.003", table.Values(2));
            Assert.IsFalse(table.ViewModel.History.CanUndo);
        });
    });

    [TestMethod]
    [Timeout(60000)]
    public void UndoAndRedoChangeBackTheCellsAnActionChanged() => Run(table => {
        OnUi(() => {
            SetClipboardText("200\r\n");
            table.Select((0, 1), (2, 1));
            ApplicationCommands.Paste.Execute(null, table.Grid);
            table.Select((4, 0));
            ApplicationCommands.Undo.Execute(null, table.Grid);
            Assert.AreEqual("100", table.Rows[0].Mass.Value);
            Assert.AreEqual("102", table.Rows[2].Mass.Value);
            CollectionAssert.AreEquivalent(new (object, DataGridColumn)[] { (table.Rows[0], table.Mass), (table.Rows[2], table.Mass), }, table.SelectedCells(), "The changed cells are selected.");
            Assert.IsFalse(ApplicationCommands.Undo.CanExecute(null, table.Grid));

            // The Clear button takes the focus when it is clicked, and the keys still work there.
            var clear = Descendants<Button>(table.Window).Single(button => "Clear".Equals(button.Content));
            ApplicationCommands.Redo.Execute(null, clear);
            Assert.AreEqual("200", table.Rows[0].Mass.Value);
            Assert.AreEqual("200", table.Rows[2].Mass.Value);
        });
    });

    [TestMethod]
    [Timeout(60000)]
    public void UndoTakesBackAWholeCellEdit() => Run(table => {
        var textBox = table.Edit(0, 1);
        OnUi(() => {
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
    });
}
