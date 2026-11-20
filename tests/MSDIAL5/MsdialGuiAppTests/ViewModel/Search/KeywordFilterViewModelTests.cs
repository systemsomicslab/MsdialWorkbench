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
            CollectionAssert.AreEqual(new[] { "FAFIFA", "other" }, model.Keywords);
            Assert.IsTrue(model.Match("FAFIFA"));
            Assert.IsTrue(model.Match("other"));

            model.ClearKeywords();
            Assert.AreEqual(string.Empty, vm.Keywords.Value);
            Assert.IsFalse(model.IsEnabled);
        }
    }
}
