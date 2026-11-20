using CompMs.App.Msdial.Model.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.ViewModel.Search.Tests
{
    [TestClass]
    public class KeywordFilterViewModelTests
    {
        [TestMethod]
        public void TypingSpacesPreservesInputAndStillFiltersByWords() {
            using var model = new KeywordFilterModel("Ontology", KeywordFilteringType.ExactMatch);
            using var vm = new KeywordFilterViewModel(model);

            vm.Keywords.Value = "FAFIFA ";
            Assert.AreEqual("FAFIFA ", vm.Keywords.Value);
            vm.Keywords.Value = "FAFIFA  other";
            Assert.AreEqual("FAFIFA  other", vm.Keywords.Value);
            Assert.AreEqual("FAFIFA  other", model.KeywordText);
            CollectionAssert.AreEqual(new[] { "FAFIFA", "other" }, model.Keywords);
            Assert.IsTrue(model.Match("FAFIFA"));
            Assert.IsTrue(model.Match("other"));

            model.KeywordText = " external ";
            Assert.AreEqual(" external ", vm.Keywords.Value);
            Assert.IsTrue(model.Match("external"));
            Assert.IsFalse(model.Match("FAFIFA"));

            model.ClearKeywords();
            Assert.AreEqual(string.Empty, vm.Keywords.Value);
            Assert.IsFalse(model.IsEnabled);
        }

        [TestMethod]
        public void NewViewModelUsesExistingModelText() {
            using var model = new KeywordFilterModel("Name") { KeywordText = "  alpha  beta " };
            using var vm = new KeywordFilterViewModel(model);

            Assert.AreEqual("  alpha  beta ", vm.Keywords.Value);
            Assert.IsTrue(model.Match("alpha and beta"));
        }
    }
}
