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

        /// <summary>
        /// The inverse of the writers: a field that must hold a compound name gets the compound
        /// name, and the processing status goes somewhere it can be read as a status.
        /// </summary>
        [TestMethod]
        public void WithoutPrefixInvertsEveryWriter() {
            Assert.AreEqual("PC 34:1", AnnotationName.WithoutPrefix(AnnotationName.AsNoMs2("PC 34:1")));
            Assert.AreEqual("PC 34:1", AnnotationName.WithoutPrefix(AnnotationName.AsLowScore("PC 34:1")));
            Assert.AreEqual("PC 34:1", AnnotationName.WithoutPrefix(AnnotationName.AsWithoutMs2("PC 34:1")));
        }

        [TestMethod]
        public void WithoutPrefixToleratesTheMsdial4SpellingWithNoSpace() {
            Assert.AreEqual("PC 34:1", AnnotationName.WithoutPrefix("w/o MS2:PC 34:1"));
        }

        [TestMethod]
        public void WithoutPrefixLeavesANameThatHasNoneAlone() {
            Assert.AreEqual("Quercetin", AnnotationName.WithoutPrefix("Quercetin"));
            // "Unknown" is not a prefix on a name, it is the whole of one, so it survives: the
            // exporter turns it into the mzTab null token by its own separate rule.
            Assert.AreEqual("Unknown", AnnotationName.WithoutPrefix("Unknown"));
            // A colon inside a compound name is not a separator.
            Assert.AreEqual("TG 54:3", AnnotationName.WithoutPrefix("TG 54:3"));
        }

        /// <summary>
        /// THE PIPE MEANS OPPOSITE THINGS IN THE TWO PLACES THAT WRITE ONE, which is why the level
        /// is chosen from the match record and never from the text.
        /// </summary>
        /// <remarks>
        /// MsScanMatching.GetRefinedLipidAnnotationLevel emits "class|chains" only at annotation
        /// level 2 or above -- only when the characteristic-ion rules RESOLVED the chains -- and
        /// returns the bare class name at level 1. MsReferenceScorer builds the same punctuation
        /// inside its <c>if (!result.IsSpectrumMatch)</c> branch, where the chain-level half is
        /// copied off the reference record and nothing measured it. Identical strings, opposite
        /// evidence. IsLipidChainsMatch is what tells them apart, and it is set from the same call
        /// that produces the name.
        /// </remarks>
        [TestMethod]
        public void AtSupportedLevelTakesTheChainsOnlyWhenTheChainsWereResolved() {
            const string dual = "PC 34:1|PC 16:0_18:1";

            Assert.AreEqual("PC 16:0_18:1", AnnotationName.AtSupportedLevel(dual, chainsResolved: true));
            Assert.AreEqual("PC 34:1", AnnotationName.AtSupportedLevel(dual, chainsResolved: false),
                "precursor mass alone does not support an sn-chain composition");
        }

        [TestMethod]
        public void AtSupportedLevelLeavesASingleNameAlone() {
            // Annotation level 1 emits no pipe at all, so both readings must agree.
            Assert.AreEqual("PC 34:1", AnnotationName.AtSupportedLevel("PC 34:1", chainsResolved: true));
            Assert.AreEqual("PC 34:1", AnnotationName.AtSupportedLevel("PC 34:1", chainsResolved: false));
            Assert.AreEqual("Quercetin", AnnotationName.AtSupportedLevel("Quercetin", chainsResolved: false));
        }

        /// <summary>
        /// "no MS2" and "w/o MS2" say the same thing -- no product-ion spectrum was acquired --
        /// and "low score" says the opposite: one was acquired and compared.
        /// </summary>
        /// <remarks>
        /// The mzTab-M evidence gate used to test the substring "no MS2" and so caught neither the
        /// peptide spelling nor, correctly, "low score". Keeping the three apart is the whole point
        /// of the distinction the author drew: an MS2 that was never acquired and an MS2 that was
        /// acquired and failed are opposite findings.
        /// </remarks>
        [TestMethod]
        public void OnlyTheNoSpectrumPrefixesCountAsPrecursorOnly() {
            Assert.IsTrue(AnnotationName.IsPrecursorOnlySuggestion(AnnotationName.AsNoMs2("PC 34:1")));
            Assert.IsTrue(AnnotationName.IsPrecursorOnlySuggestion(AnnotationName.AsWithoutMs2("PEPTIDE")));
            Assert.IsFalse(AnnotationName.IsPrecursorOnlySuggestion(AnnotationName.AsLowScore("PC 34:1")),
                "a spectrum was acquired and compared here; that is a different finding");
            Assert.IsFalse(AnnotationName.IsPrecursorOnlySuggestion("Quercetin"));
            Assert.IsFalse(AnnotationName.IsPrecursorOnlySuggestion(null));
        }

        [TestMethod]
        public void IsPrecursorOnlySuggestionTestsAPrefixAndNotASubstring() {
            // The gate this replaces used Contains, so a compound whose name merely contained the
            // token anywhere would have been swept up with the real suggestions.
            Assert.IsFalse(AnnotationName.IsPrecursorOnlySuggestion("Compound with no MS2 in its name"));
        }
    }
}
