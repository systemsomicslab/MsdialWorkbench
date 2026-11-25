using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.Model.Search.Tests
{
    [TestClass]
    public class KeywordFilterModelTests
    {
        [DataTestMethod]
        [DataRow("pc", "PC")]
        [DataRow("PC", "pc")]
        [DataRow("Pc", "pC")]
        [DataRow("cer_ns", "Cer_NS")]
        public void TheOntologyFilterIgnoresCase(string typed, string ontology) {
            using var model = new KeywordFilterModel("Ontology filter", KeywordFilteringType.ExactMatch) { KeywordText = typed };

            Assert.IsTrue(model.Match(ontology));
            Assert.AreEqual(typed, model.KeywordText, "The text stays as typed.");
        }

        [TestMethod]
        public void TheOntologyFilterStillMatchesWholeClassNamesOnly() {
            using var model = new KeywordFilterModel("Ontology filter", KeywordFilteringType.ExactMatch) { KeywordText = "pc lpc" };

            Assert.IsTrue(model.Match("PC"));
            Assert.IsTrue(model.Match("LPC"));
            Assert.IsFalse(model.Match("PE"));
            Assert.IsFalse(model.Match("EtherPC"), "A class that only contains the keyword is not the class.");
            Assert.IsFalse(model.Match(null!));
        }

        [DataTestMethod]
        [DataRow(KeywordFilteringType.IgnoreIfWordIsNull, "glucose", "D-Glucose")]
        [DataRow(KeywordFilteringType.IgnoreIfWordIsNull, "PC 34:1", "pc 34:1")]
        [DataRow(KeywordFilteringType.KeepIfWordIsNull, "[m+h]+", "[M+H]+")]
        [DataRow(KeywordFilteringType.KeepIfWordIsNull, "ALBU", "Albumin")]
        public void NameProteinAdductAndCommentFiltersIgnoreCase(KeywordFilteringType type, string typed, string word) {
            using var model = new KeywordFilterModel("filter", type) { KeywordText = typed };

            Assert.IsTrue(model.Match(word));
        }
    }
}
