using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.Graphics.Behavior;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CompMs.App.Msdial.View.Table.Tests;

/// <summary>
/// Editing the name, ontology or comment in the peak spot table, as the table views set it up: a DataGrid over the
/// navigator's collection view, with the editing behavior that holds the filters back while a row is in edit.
/// A row edit left without Enter must not keep the filters held back.
/// Each test shows the table window on screen and types into it, so it is in the UI category (see the project README).
/// </summary>
[TestClass]
[TestCategory("UI")]
public class PeakSpotTableEditTests
{
    private sealed class Evaluator : IMatchResultEvaluator<AlignmentSpotPropertyModel>
    {
        public List<AlignmentSpotPropertyModel> FilterByThreshold(IEnumerable<AlignmentSpotPropertyModel> results) => results.ToList();
        public bool IsAnnotationSuggested(AlignmentSpotPropertyModel result) => false;
        public bool IsReferenceMatched(AlignmentSpotPropertyModel result) => false;
        public List<AlignmentSpotPropertyModel> SelectReferenceMatchResults(IEnumerable<AlignmentSpotPropertyModel> results) => results.ToList();
        public AlignmentSpotPropertyModel SelectTopHit(IEnumerable<AlignmentSpotPropertyModel> results) => results.First();
    }

    private sealed class Harness
    {
        public ObservableCollection<AlignmentSpotPropertyModel> Spots = null!;
        public PeakSpotNavigatorViewModel Navigator = null!;
        public DataGrid Grid = null!;
        public Window Window = null!;
        public List<Exception> Errors = new();
    }

    private static void OnUi(Action action) => Application.Current.Dispatcher.Invoke(action);

    private static Harness Create() {
        TestApplication.Ensure();
        var h = new Harness();
        OnUi(() => {
            Application.Current.DispatcherUnhandledException += (s, e) => { h.Errors.Add(e.Exception); e.Handled = true; };
            h.Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Enumerable.Range(0, 6).Select(i => {
                var spot = new AlignmentSpotPropertyModel(new AlignmentSpotProperty { AlignmentID = i, MasterAlignmentID = i, MassCenter = 100d + i, TimesCenter = new CompMs.Common.Components.ChromXs(1d + i), });
                spot.Name = $"Name{i}";
                spot.Comment = $"Comment{i}";
                spot.Ontology = $"Ontology{i}";
                return spot;
            }));
            var filtering = new PeakSpotFiltering<AlignmentSpotPropertyModel>(FilterEnableStatus.All);
            var manager = new FilterRegistrationManager<AlignmentSpotPropertyModel>(h.Spots, filtering);
            manager.AttachFilter(h.Spots, new PeakFilterModel(DisplayFilter.All), new Evaluator(), FilterEnableStatus.All);
            h.Navigator = new PeakSpotNavigatorViewModel(manager.PeakSpotNavigatorModel);

            (h.Grid, h.Window) = OpenTable(h);
            h.Window.Activate();
        });
        Settle();
        return h;
    }

    // A table view as the XAML ones load: the DataGrid initialized as BAML does it, so it takes the implicit style then,
    // and its bindings attach once the view reaches the window.
    private sealed class TableView : UserControl
    {
        public TableView() {
            var grid = new DataGrid();
            grid.BeginInit();
            grid.AutoGenerateColumns = false;
            grid.CanUserAddRows = false;
            grid.CanUserDeleteRows = false;
            grid.IsSynchronizedWithCurrentItem = true;
            grid.SelectionUnit = DataGridSelectionUnit.FullRow;
            grid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(PeakSpotNavigatorViewModel.PeakSpotsView)));
            grid.SetBinding(DataGridIsEdittingBehavior.IsEdittingProperty, new Binding("IsEditting.Value") { Mode = BindingMode.OneWayToSource, });
            grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(AlignmentSpotPropertyModel.Name)), });
            grid.Columns.Add(new DataGridTextColumn { Header = "Ontology", Binding = new Binding(nameof(AlignmentSpotPropertyModel.Ontology)), });
            grid.Columns.Add(new DataGridTextColumn { Header = "Comment", Binding = new Binding(nameof(AlignmentSpotPropertyModel.Comment)), });
            grid.EndInit();
            Content = grid;
        }
    }

    // A table window as AlignmentSpotTable sets it up: the table view comes from a data template once the window is shown,
    // and the behaviors come from an implicit style, as App.xaml sets them.
    private static (DataGrid, Window) OpenTable(Harness h) {
        var style = new Style(typeof(DataGrid));
        style.Setters.Add(new Setter(DataGridCommitBehavior.EnableProperty, true));
        style.Setters.Add(new Setter(DataGridIsEdittingBehavior.EnabledProperty, true));
        var window = new Window { DataContext = h.Navigator, Width = 600, Height = 400, ShowActivated = true, WindowStartupLocation = WindowStartupLocation.Manual, Left = 0, Top = 0, };
        window.Resources.Add(typeof(DataGrid), style);
        var host = new ContentControl { ContentTemplate = new DataTemplate { VisualTree = new FrameworkElementFactory(typeof(TableView)), }, };
        host.SetBinding(ContentControl.ContentProperty, new Binding());
        window.Content = host;
        window.Show();
        var grid = FindChild<DataGrid>(window) ?? throw new AssertFailedException("the table view was not created");
        return (grid, window);
    }

    private static void Settle(int milliseconds = 150) {
        Thread.Sleep(milliseconds);
        Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
    }

    private static DataGridCell GetCell(DataGrid grid, int row, int column) {
        grid.UpdateLayout();
        var item = grid.Items[row];
        grid.ScrollIntoView(item);
        grid.UpdateLayout();
        var container = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(item);
        var content = grid.Columns[column].GetCellContent(container);
        return (DataGridCell)content.Parent;
    }

    private static void Click(DataGrid grid, int row, int column, int clickCount = 1) {
        var cell = GetCell(grid, row, column);
        var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent, Source = cell, };
        cell.RaiseEvent(args);
    }

    private static void Type(DataGrid grid, int row, int column, string text) {
        var cell = GetCell(grid, row, column);
        var box = FindChild<TextBox>(cell) ?? throw new AssertFailedException($"cell ({row}, {column}) is not in edit");
        Keyboard.Focus(box);
        box.Text = text;
    }

    private static T? FindChild<T>(DependencyObject root) where T : DependencyObject {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T t) {
                return t;
            }
            if (FindChild<T>(child) is T found) {
                return found;
            }
        }
        return null;
    }

    private static void EditByClicks(Harness h, int row, int column, string text) {
        OnUi(() => Click(h.Grid, row, column));
        Settle();
        OnUi(() => Click(h.Grid, row, column));
        Settle();
        OnUi(() => Type(h.Grid, row, column, text));
        Settle();
    }

    private static int VisibleCount(Harness h) {
        var n = 0;
        OnUi(() => n = h.Navigator.PeakSpotsView.Cast<object>().Count());
        return n;
    }

    private static void SetNameFilter(Harness h, string text) {
        OnUi(() => h.Navigator.KeywordFilterViewModels.First(vm => vm.Label == "Name filter").Keywords.Value = text);
        Settle(800);
    }

    [TestMethod]
    public void FiltersStillApplyAfterAnEditLeftWithoutEnter() {
        var h = Create();
        try {
            EditByClicks(h, 0, 0, "Edited name 0");
            EditByClicks(h, 1, 2, "Edited comment 1");
            // Leave the edit without Enter: focus goes elsewhere, as when typing into a filter box.
            OnUi(() => Keyboard.ClearFocus());
            Settle();
            SetNameFilter(h, "Edited");
            Assert.AreEqual(0, h.Errors.Count, string.Join(" ---- ", h.Errors));
            Assert.AreEqual(1, VisibleCount(h));
        }
        finally {
            OnUi(() => h.Window.Close());
        }
    }

    [TestMethod]
    public void FiltersStillApplyAfterTheSelectionMovesFromOutsideDuringAnEdit() {
        var h = Create();
        try {
            EditByClicks(h, 0, 0, "Edited name 0");
            // The main window's chart selects another spot.
            OnUi(() => h.Grid.SelectedItem = h.Spots[4]);
            Settle();
            SetNameFilter(h, "Edited");
            Assert.AreEqual(0, h.Errors.Count, string.Join(" ---- ", h.Errors));
            Assert.AreEqual(1, VisibleCount(h));
        }
        finally {
            OnUi(() => h.Window.Close());
        }
    }

    [TestMethod]
    public void AnEditLeftOpenOnTheViewIsClosedWhenTheNextEditBegins() {
        var h = Create();
        try {
            // What DataGrid leaves behind when the edited row was scrolled away: a transaction open on the view alone.
            OnUi(() => {
                ((System.ComponentModel.IEditableCollectionView)h.Navigator.PeakSpotsView).EditItem(h.Spots[5]);
                h.Spots[5].Name = "Edited name 5";
            });
            EditByClicks(h, 2, 0, "Edited name 2");
            OnUi(() => Click(h.Grid, 3, 0));
            Settle();
            SetNameFilter(h, "Edited");
            Assert.AreEqual(0, h.Errors.Count, string.Join(" ---- ", h.Errors));
            Assert.AreEqual(2, VisibleCount(h));
        }
        finally {
            OnUi(() => h.Window.Close());
        }
    }

    [TestMethod]
    public void MovingFromOneEditToTheNextWithoutEnterDoesNotThrow() {
        var h = Create();
        try {
            EditByClicks(h, 0, 0, "Edited name 0");
            EditByClicks(h, 0, 2, "Edited comment 0");
            EditByClicks(h, 1, 1, "Edited ontology 1");
            EditByClicks(h, 2, 0, "Edited name 2");
            EditByClicks(h, 1, 2, "Edited comment 1");
            OnUi(() => Click(h.Grid, 3, 0));
            Settle(800);

            Assert.AreEqual(0, h.Errors.Count, string.Join("\n----\n", h.Errors));
            Assert.AreEqual("Edited name 0", h.Spots[0].Name);
            Assert.AreEqual("Edited comment 0", h.Spots[0].Comment);
            Assert.AreEqual("Edited ontology 1", h.Spots[1].Ontology);
            Assert.AreEqual("Edited name 2", h.Spots[2].Name);
            Assert.AreEqual("Edited comment 1", h.Spots[1].Comment);
        }
        finally {
            OnUi(() => h.Window.Close());
        }
    }
    [TestMethod]
    public void AnotherTableCanBeOpenedWhileAnEditIsLeftWithoutEnter() {
        var h = Create();
        Window? second = null;
        try {
            EditByClicks(h, 0, 0, "Edited name 0");
            // "Show ion table" clicked again in the main window: a second table over the same collection view.
            OnUi(() => second = OpenTable(h).Item2);
            Settle();
            Assert.AreEqual(0, h.Errors.Count, string.Join(" ---- ", h.Errors));
            Assert.AreEqual("Edited name 0", h.Spots[0].Name);
            SetNameFilter(h, "Edited");
            Assert.AreEqual(1, VisibleCount(h));
        }
        finally {
            OnUi(() => { second?.Close(); h.Window.Close(); });
        }
    }

    [TestMethod]
    public void TheTableCanBeReopenedAfterItWasClosedDuringAnEdit() {
        var h = Create();
        Window? reopened = null;
        try {
            EditByClicks(h, 0, 2, "Edited comment 0");
            OnUi(() => h.Window.Close());
            Settle();
            OnUi(() => reopened = OpenTable(h).Item2);
            Settle();
            Assert.AreEqual(0, h.Errors.Count, string.Join(" ---- ", h.Errors));
            Assert.AreEqual("Edited comment 0", h.Spots[0].Comment);
        }
        finally {
            OnUi(() => reopened?.Close());
        }
    }
}
