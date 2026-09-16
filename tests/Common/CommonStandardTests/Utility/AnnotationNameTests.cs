using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Utility.Tests
{
    [TestClass]
    public class AnnotationNameTests
    {
        [TestMethod]
        public void IsReferenceMatchedAcceptsPlainName() {
            Assert.IsTrue(AnnotationName.IsReferenceMatched("PC 34:1"));
            Assert.IsTrue(AnnotationName.IsReferenceMatched("FA 16:0"));
            Assert.IsTrue(AnnotationName.IsReferenceMatched("Reference compound"));
        }

        [TestMethod]
        public void IsReferenceMatchedRejectsNoMs2Suggestion() {
            // MS-DIAL 5 precursor-only suggestion: no product-ion spectrum at all.
            Assert.IsFalse(AnnotationName.IsReferenceMatched("no MS2: PC 34:1"));
        }

        [TestMethod]
        public void IsReferenceMatchedRejectsLowScoreSuggestion() {
            // MS-DIAL 5 suggestion that has a product-ion spectrum but failed the
            // reference-search acceptance criteria.
            Assert.IsFalse(AnnotationName.IsReferenceMatched("low score: PC 34:1"));
        }

        [TestMethod]
        public void IsReferenceMatchedRejectsUnknown() {
            Assert.IsFalse(AnnotationName.IsReferenceMatched("Unknown"));
            Assert.IsFalse(AnnotationName.IsReferenceMatched("unknown feature"));
        }

        [TestMethod]
        public void IsReferenceMatchedRejectsWithoutMs2Suggestion() {
            // Still written by the MS-DIAL 5 peptide suggestion path, and by MS-DIAL 4.
            Assert.IsFalse(AnnotationName.IsReferenceMatched("w/o MS2: PC 34:1"));
            Assert.IsFalse(AnnotationName.IsReferenceMatched("w/o MS2:PC 34:1"));
        }

        [TestMethod]
        public void IsReferenceMatchedRejectsPlaceholders() {
            Assert.IsFalse(AnnotationName.IsReferenceMatched(null));
            Assert.IsFalse(AnnotationName.IsReferenceMatched(""));
            Assert.IsFalse(AnnotationName.IsReferenceMatched(" "));
            Assert.IsFalse(AnnotationName.IsReferenceMatched("null"));
            Assert.IsFalse(AnnotationName.IsReferenceMatched("empty"));
            Assert.IsFalse(AnnotationName.IsReferenceMatched("RIKEN MS/MS"));
        }

        [TestMethod]
        public void IsReferenceMatchedIgnoresLeadingWhitespace() {
            Assert.IsFalse(AnnotationName.IsReferenceMatched("  no MS2: PC 34:1"));
            Assert.IsFalse(AnnotationName.IsReferenceMatched("  low score: PC 34:1"));
        }

        [TestMethod]
        public void WritersProduceTheHistoricalSpellings() {
            Assert.AreEqual("no MS2: PC 34:1", AnnotationName.AsNoMs2("PC 34:1"));
            Assert.AreEqual("low score: PC 34:1", AnnotationName.AsLowScore("PC 34:1"));
            Assert.AreEqual("w/o MS2: PC 34:1", AnnotationName.AsWithoutMs2("PC 34:1"));
        }

        /// <summary>
        /// The invariant that the stale "w/o MS2"-only predicate violated: every name a
        /// suggestion writer produces must be rejected by the reader.
        /// </summary>
        [TestMethod]
        public void EverySuggestionWriterProducesANonMatchedName() {
            Assert.IsFalse(AnnotationName.IsReferenceMatched(AnnotationName.AsNoMs2("PC 34:1")));
            Assert.IsFalse(AnnotationName.IsReferenceMatched(AnnotationName.AsLowScore("PC 34:1")));
            Assert.IsFalse(AnnotationName.IsReferenceMatched(AnnotationName.AsWithoutMs2("PC 34:1")));
        }
    }
}
