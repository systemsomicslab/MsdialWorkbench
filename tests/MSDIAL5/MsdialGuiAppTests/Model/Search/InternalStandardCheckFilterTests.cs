using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.Model.Search.Tests
{
    /// <summary>
    /// The "IS candidates" check box of the alignment tables keeps only isotope-labelled internal standards, and Reset filter clears it.
    /// </summary>
    [TestClass]
    public class InternalStandardCheckFilterTests
    {
        private static AlignmentSpotPropertyModel Spot(string name, string ontology) {
            return new AlignmentSpotPropertyModel(new AlignmentSpotProperty { Name = name, Ontology = ontology, });
        }

        private static ObservableCollection<AlignmentSpotPropertyModel> Spots() {
            return new ObservableCollection<AlignmentSpotPropertyModel>
            {
                Spot("PC 34:1", "PC"),
                Spot("PC 15:0_18:1(d7)", "PC"),
                Spot("Unknown", ""),
                Spot("PC 33:1", "PC_d5"),
                Spot("Leucine-d10", "Amino acids"),
            };
        }

        [TestMethod]
        public void CheckingTheFilterKeepsOnlyCandidatesInTheView() {
            var spots = Spots();
            var view = new ListCollectionView(spots);
            // Not disposed: DetatchFilter expects the disposables a value filter registers, and this view has none.
            var filtering = new PeakSpotFiltering<AlignmentSpotPropertyModel>(FilterEnableStatus.All);
            var manager = filtering.CheckFilterManagers.Single();
            Assert.IsTrue(manager.TryAttachFilter(filtering, view, FilterEnableStatus.All));

            view.Refresh();
            Assert.AreEqual(5, view.Cast<object>().Count());

            manager.Filter.IsEnabled = true;
            view.Refresh();
            CollectionAssert.AreEqual(
                new[] { "PC 15:0_18:1(d7)", "PC 33:1", "Leucine-d10", },
                view.Cast<AlignmentSpotPropertyModel>().Select(s => s.Name).ToArray());

            manager.Filter.IsEnabled = false;
            view.Refresh();
            Assert.AreEqual(5, view.Cast<object>().Count());
        }

        [TestMethod]
        public void ApplyFiltersOnlyWhileChecked() {
            var spots = Spots();
            using var filtering = new PeakSpotFiltering<AlignmentSpotPropertyModel>(FilterEnableStatus.All);
            var manager = filtering.CheckFilterManagers.Single();

            Assert.AreEqual(5, manager.Apply(spots, FilterEnableStatus.All).Count());

            manager.Filter.IsEnabled = true;
            Assert.AreEqual(3, manager.Apply(spots, FilterEnableStatus.All).Count());
            Assert.AreEqual(5, manager.Apply(spots, FilterEnableStatus.All & ~FilterEnableStatus.InternalStandard).Count());
        }

        [TestMethod]
        public void NoCheckFilterWithoutTheInternalStandardStatus() {
            using var filtering = new PeakSpotFiltering<AlignmentSpotPropertyModel>(FilterEnableStatus.All & ~FilterEnableStatus.InternalStandard);
            Assert.AreEqual(0, filtering.CheckFilterManagers.Count);
        }

        [TestMethod]
        public void ResetFiltersUnchecksTheFilter() {
            using var filtering = new PeakSpotFiltering<AlignmentSpotPropertyModel>(FilterEnableStatus.All);
            var checkFilter = filtering.CheckFilterManagers.Single().Filter;
            var navigator = new PeakSpotNavigatorModel(
                Spots(),
                filtering.ValueFilterManagers.Select(m => m.Filter).ToArray(),
                filtering.KeywordFilterManagers.Select(m => m.Filter).ToArray(),
                filtering.AmplitudeFilterModel,
                filtering.TagSearchQueryBuilder,
                filtering.CheckFilterManagers.Select(m => m.Filter).ToArray());

            checkFilter.IsEnabled = true;
            navigator.ResetFilters();

            Assert.IsFalse(checkFilter.IsEnabled);
        }
    }
}
