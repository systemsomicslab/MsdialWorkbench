using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;

namespace CompMs.App.Msdial.Model.Search.Tests
{
    /// <summary>
    /// A filter change refreshes every peak spot view at once, except a view in an edit transaction (refreshing it throws):
    /// that view is refreshed once, after its edit ends and the UI dispatcher has run.
    /// </summary>
    [TestClass]
    public class PeakSpotNavigatorModelTests
    {
        // An editable row, so the view supports both CommitEdit and CancelEdit as it does for the peak spot table.
        private sealed class Row : IEditableObject
        {
            public Row(int value) => Value = value;
            public int Value { get; }
            public void BeginEdit() { }
            public void CancelEdit() { }
            public void EndEdit() { }
        }

        // A view over rows 0..9 whose filter keeps the values below a threshold that the test moves,
        // so a refresh shows in what the view contains and in its Reset notifications.
        private sealed class FilteredView
        {
            public FilteredView() {
                Items = new ObservableCollection<Row>(Enumerable.Range(0, 10).Select(i => new Row(i)));
                View = new ListCollectionView(Items) { Filter = o => Error is null ? ((Row)o).Value < Threshold : throw Error };
                ((INotifyCollectionChanged)View).CollectionChanged += (s, e) => {
                    if (e.Action == NotifyCollectionChangedAction.Reset) {
                        Refreshes++;
                    }
                };
            }

            public ObservableCollection<Row> Items { get; }
            public ListCollectionView View { get; }
            public int Threshold { get; set; } = 10;
            // When set, the filter throws it, so refreshing the view fails.
            public Exception? Error { get; set; }
            public int Refreshes { get; private set; }
            public int[] Contents => View.Cast<Row>().Select(row => row.Value).ToArray();
        }

        private static PeakSpotNavigatorModel CreateNavigator(params FilteredView[] views) {
            var navigator = new PeakSpotNavigatorModel(new object(), Array.Empty<ValueFilterModel>(), Array.Empty<KeywordFilterModel>(), null!, null!);
            foreach (var view in views) {
                navigator.PeakSpotsCollection.Add(view.View);
            }
            return navigator;
        }

        private static void RunDispatcher() {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        [TestMethod]
        public void AViewNotInEditIsRefreshedImmediately() {
            var spots = new FilteredView();
            var navigator = CreateNavigator(spots);

            spots.Threshold = 3;
            navigator.RefreshCollectionViews();

            Assert.AreEqual(1, spots.Refreshes);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, spots.Contents);
        }

        [TestMethod]
        public void AViewInEditIsRefreshedOnceAfterTheEditIsCommitted() {
            var spots = new FilteredView();
            var navigator = CreateNavigator(spots);

            spots.View.EditItem(spots.Items[1]);
            spots.Threshold = 3;
            navigator.RefreshCollectionViews();

            Assert.AreEqual(0, spots.Refreshes);

            spots.View.CommitEdit();
            RunDispatcher();

            Assert.AreEqual(1, spots.Refreshes);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, spots.Contents);
        }

        [TestMethod]
        public void AViewInEditIsRefreshedOnceAfterTheEditIsCancelled() {
            var spots = new FilteredView();
            var navigator = CreateNavigator(spots);

            spots.View.EditItem(spots.Items[1]);
            spots.Threshold = 3;
            navigator.RefreshCollectionViews();

            Assert.AreEqual(0, spots.Refreshes);

            spots.View.CancelEdit();
            RunDispatcher();

            Assert.AreEqual(1, spots.Refreshes);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, spots.Contents);
        }

        [TestMethod]
        public void AViewAddingANewItemWaitsLikeAViewEditingOne() {
            var spots = new FilteredView();
            var navigator = CreateNavigator(spots);

            spots.View.AddNewItem(new Row(10));
            spots.Threshold = 3;
            navigator.RefreshCollectionViews();

            Assert.AreEqual(0, spots.Refreshes);

            spots.View.CommitNew();
            RunDispatcher();

            Assert.AreEqual(1, spots.Refreshes);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, spots.Contents);
        }

        [TestMethod]
        public void SeveralRequestsDuringOneEditLeadToOneRefresh() {
            var spots = new FilteredView();
            var navigator = CreateNavigator(spots);

            spots.View.EditItem(spots.Items[1]);
            spots.Threshold = 5;
            navigator.RefreshCollectionViews();
            spots.Threshold = 4;
            navigator.RefreshCollectionViews();
            spots.Threshold = 3;
            navigator.RefreshCollectionViews();
            spots.View.CommitEdit();
            RunDispatcher();

            Assert.AreEqual(1, spots.Refreshes);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, spots.Contents);
        }

        [TestMethod]
        public void AnotherViewIsRefreshedImmediatelyWhileOneWaits() {
            var edited = new FilteredView();
            var other = new FilteredView();
            var navigator = CreateNavigator(edited, other);

            edited.View.EditItem(edited.Items[1]);
            edited.Threshold = 3;
            other.Threshold = 3;
            navigator.RefreshCollectionViews();

            Assert.AreEqual(0, edited.Refreshes);
            Assert.AreEqual(1, other.Refreshes);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, other.Contents);

            edited.View.CommitEdit();
            RunDispatcher();

            Assert.AreEqual(1, edited.Refreshes);
            Assert.AreEqual(1, other.Refreshes);
        }

        [TestMethod]
        public void AViewRemovedWhileWaitingIsNotRefreshedWhenItsEditEnds() {
            var spots = new FilteredView();
            var navigator = CreateNavigator(spots);

            spots.View.EditItem(spots.Items[1]);
            spots.Threshold = 3;
            navigator.RefreshCollectionViews();
            navigator.PeakSpotsCollection.Remove(spots.View);
            spots.View.CommitEdit();
            RunDispatcher();

            Assert.AreEqual(0, spots.Refreshes);
            Assert.AreEqual(10, spots.Contents.Length);
        }

        [TestMethod]
        public void AViewRemovedAfterItsEditEndedButBeforeTheDispatcherRanIsNotRefreshed() {
            var spots = new FilteredView();
            var navigator = CreateNavigator(spots);

            spots.View.EditItem(spots.Items[1]);
            spots.Threshold = 3;
            navigator.RefreshCollectionViews();
            spots.View.CommitEdit();
            navigator.PeakSpotsCollection.Remove(spots.View);
            RunDispatcher();

            Assert.AreEqual(0, spots.Refreshes);
        }

        [TestMethod]
        public void DisposingTheNavigatorDropsEveryWaitingRefresh() {
            var first = new FilteredView();
            var second = new FilteredView();
            var navigator = CreateNavigator(first, second);

            first.View.EditItem(first.Items[1]);
            second.View.EditItem(second.Items[1]);
            navigator.RefreshCollectionViews();
            navigator.Dispose();
            first.View.CommitEdit();
            second.View.CancelEdit();
            RunDispatcher();

            Assert.AreEqual(0, first.Refreshes);
            Assert.AreEqual(0, second.Refreshes);
        }

        [TestMethod]
        public void ANewEditBegunBeforeTheDispatcherRanMakesTheRefreshWaitForThatEdit() {
            var spots = new FilteredView();
            var navigator = CreateNavigator(spots);

            spots.View.EditItem(spots.Items[1]);
            spots.Threshold = 3;
            navigator.RefreshCollectionViews();
            spots.View.CommitEdit();
            spots.View.EditItem(spots.Items[2]);
            RunDispatcher();

            Assert.AreEqual(0, spots.Refreshes);

            spots.View.CommitEdit();
            RunDispatcher();

            Assert.AreEqual(1, spots.Refreshes);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, spots.Contents);
        }

        [TestMethod]
        public void ARequestAfterTheEditEndedButBeforeTheDispatcherRanRefreshesOnce() {
            var spots = new FilteredView();
            var navigator = CreateNavigator(spots);

            spots.View.EditItem(spots.Items[1]);
            spots.Threshold = 4;
            navigator.RefreshCollectionViews();
            spots.View.CommitEdit();
            spots.Threshold = 3;
            navigator.RefreshCollectionViews();

            // The refresh already posted for the ended edit does it: refreshing now could run inside the end of the row commit.
            Assert.AreEqual(0, spots.Refreshes);

            RunDispatcher();

            Assert.AreEqual(1, spots.Refreshes);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, spots.Contents);
        }

        [TestMethod]
        public void AViewThatFailsToRefreshDoesNotStopTheOthersAndItsErrorIsRethrown() {
            var failing = new FilteredView();
            var other = new FilteredView();
            var navigator = CreateNavigator(failing, other);
            var error = new InvalidOperationException("filter failed");
            failing.Error = error;

            other.Threshold = 3;
            var thrown = Assert.ThrowsException<InvalidOperationException>(() => navigator.RefreshCollectionViews());

            Assert.AreSame(error, thrown);
            Assert.AreEqual(1, other.Refreshes);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, other.Contents);
        }

        [TestMethod]
        public void SeveralViewsFailingToRefreshAreReportedTogether() {
            var first = new FilteredView();
            var second = new FilteredView();
            var navigator = CreateNavigator(first, second);
            var firstError = new InvalidOperationException("first filter failed");
            var secondError = new InvalidOperationException("second filter failed");
            first.Error = firstError;
            second.Error = secondError;

            var thrown = Assert.ThrowsException<AggregateException>(() => navigator.RefreshCollectionViews());

            CollectionAssert.AreEqual(new Exception[] { firstError, secondError }, thrown.InnerExceptions.ToArray());
        }

        [TestMethod]
        public void AViewAddedAfterTheNavigatorWasDisposedIsSkipped() {
            var spots = new FilteredView();
            var navigator = CreateNavigator();
            navigator.Dispose();
            navigator.PeakSpotsCollection.Add(spots.View);

            navigator.RefreshCollectionViews();

            Assert.AreEqual(0, spots.Refreshes);
        }
    }
}
