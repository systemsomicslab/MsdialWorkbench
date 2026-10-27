using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Windows.Data;

namespace CompMs.App.Msdial.Model.Search.Tests
{
    /// <summary>
    /// Typing into a filter box while a peak table row is still being edited must not throw
    /// "'Filter' is not allowed during an AddNew or EditItem transaction"; the refresh is left to the navigator, which waits for the edit to end.
    /// </summary>
    [TestClass]
    public class AttachedPeakFiltersTests
    {
        [TestMethod]
        public void SwitchingAFilterOnDuringAnEditDoesNotTouchTheView() {
            var items = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var view = new ListCollectionView(items);
            var filters = new AttachedPeakFilters<int>(view);
            var enabled = new Subject<bool>();
            filters.Attatch(_ => true);
            filters.Attatch(i => i % 2 == 0, enabled, initial: false);

            view.EditItem(items[1]);
            enabled.OnNext(true);
            view.CommitEdit();
            view.Refresh();

            CollectionAssert.AreEqual(new[] { 0, 2, 4, 6, 8 }, view.Cast<int>().ToArray());

            enabled.OnNext(false);
            view.Refresh();

            Assert.AreEqual(10, view.Cast<int>().Count());
        }
    }
}
