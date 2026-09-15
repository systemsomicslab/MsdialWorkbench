using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.DataObj.Result.Tests
{
    /// <summary>
    /// Whether the spectra in a library were acquired or generated, and the claim the evidence
    /// record is therefore allowed to make about a match against it.
    /// </summary>
    /// <remarks>
    /// The author raised this on 2026-09-15 for GC-MS, where the libraries in practice are Wiley,
    /// NIST and MassBank on one side and in-silico EI generators such as NEIMS on the other, and
    /// asked for the two to be tagged apart. Nothing about it is specific to GC-MS: CFM-ID and its
    /// kin produce MS/MS libraries the same way.
    ///
    /// MS-DIAL cannot tell by looking. An MSP holds no field that says so, and a generated spectrum
    /// parses, searches and scores exactly like an acquired one. So the analyst says which, by
    /// choosing the library kind, and every annotator reads it off the database it is searching.
    /// Before this, "an experimentally acquired reference spectrum was compared" was asserted
    /// unconditionally -- in ForDatabaseMatch for the LC and IM modes, and separately in the GC-MS
    /// funnel.
    /// </remarks>
    [TestClass()]
    public class LibraryQualityIsRecordedTests
    {
        private const MeasuredTerms Compared = MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass;

        [TestMethod()]
        public void AnAcquiredLibraryIsStillCalledAReferenceSpectrum() {
            Assert.AreEqual(
                AnnotationEvidenceSource.ReferenceSpectrum,
                AnnotationEvidence.ForDatabaseMatch(Compared, TargetOmics.Metabolomics, DataBaseSource.Msp));
        }

        [TestMethod()]
        public void APredictedLibraryIsCalledInSilico() {
            Assert.AreEqual(
                AnnotationEvidenceSource.BySpectrumPredictionTool,
                AnnotationEvidence.ForDatabaseMatch(Compared, TargetOmics.Metabolomics, DataBaseSource.PredictedMsp),
                "a structure went in and a spectrum came out; that is the spectrum-prediction direction");
        }

        /// <summary>
        /// And the ordering follows, without the ordering knowing what a library is.
        /// </summary>
        /// <remarks>
        /// The two halves meet here: the library kind picks the evidence, the evidence picks the
        /// rank. A match against a generated spectrum loses to a match against an acquired one at
        /// equal score, which is the whole point of tagging them apart.
        /// </remarks>
        [TestMethod()]
        public void AMatchAgainstAGeneratedSpectrumRanksBelowOneAgainstAnAcquiredSpectrum() {
            var acquired = AnnotationEvidence.ForDatabaseMatch(Compared, TargetOmics.Metabolomics, DataBaseSource.Msp);
            var predicted = AnnotationEvidence.ForDatabaseMatch(Compared, TargetOmics.Metabolomics, DataBaseSource.PredictedMsp);

            Assert.IsTrue(AnnotationEvidence.Rank(predicted) < AnnotationEvidence.Rank(acquired));
        }

        /// <summary>
        /// DataBaseSource.Lbm is NOT treated as predicted, and the reason is a defect rather than a
        /// principle.
        /// </summary>
        /// <remarks>
        /// .lbm2 spectra genuinely are in silico. But MassAnnotationSettingModel.LoadDataBase stores
        /// a plain MSP library under DataBaseSource.Lbm -- it switches on DBSource and then passes
        /// the Lbm constant to the MoleculeDataBase constructor regardless -- so an experimental
        /// library can arrive wearing that label. Treating Lbm as predicted would mislabel those,
        /// which is worse than the current understatement. When that assignment is fixed, this test
        /// is where the change gets recorded.
        /// </remarks>
        [TestMethod()]
        public void TheLbmLabelIsNotYetTrustworthyEnoughToCountAsPredicted() {
            Assert.IsFalse(AnnotationEvidence.SpectraArePredicted(DataBaseSource.Lbm));
        }

        /// <summary>
        /// The EAD lipid databases are known to be generated, because MS-DIAL generates them.
        /// </summary>
        /// <remarks>
        /// They do not reach this question through ForDatabaseMatch today -- lipidomics answers
        /// RuleBased first, which is the more specific truth -- but a metabolomics run against one
        /// should not be told the spectra were acquired.
        /// </remarks>
        [TestMethod()]
        public void TheGeneratedLipidDatabasesAreKnownToBePredicted() {
            Assert.IsTrue(AnnotationEvidence.SpectraArePredicted(DataBaseSource.EieioLipid));
            Assert.IsTrue(AnnotationEvidence.SpectraArePredicted(DataBaseSource.OadLipid));
            Assert.IsTrue(AnnotationEvidence.SpectraArePredicted(DataBaseSource.EidLipid));
        }

        /// <summary>
        /// For lipidomics the rules are the evidence whatever the library was.
        /// </summary>
        [TestMethod()]
        public void TheLipidRuleSetOutranksTheLibraryKind() {
            Assert.AreEqual(
                AnnotationEvidenceSource.RuleBased,
                AnnotationEvidence.ForDatabaseMatch(Compared, TargetOmics.Lipidomics, DataBaseSource.PredictedMsp));
        }

        /// <summary>
        /// And a candidate with no spectrum compared is precursor-only whatever the library was.
        /// </summary>
        /// <remarks>
        /// The order of the three questions matters: what was compared comes first, because a
        /// library whose spectra are predicted says nothing about a feature that had none.
        /// </remarks>
        [TestMethod()]
        public void NoSpectrumComparedStillOutranksTheLibraryKind() {
            Assert.AreEqual(
                AnnotationEvidenceSource.PrecursorOnly,
                AnnotationEvidence.ForDatabaseMatch(MeasuredTerms.AccurateMass, TargetOmics.Metabolomics, DataBaseSource.PredictedMsp));
        }

        /// <summary>
        /// The new member is appended, so every project on disk keeps the number it holds.
        /// </summary>
        /// <remarks>
        /// DataBaseSource is serialized -- MoleculeDataBase key 2, and the restoration keys -- so
        /// inserting a member would silently relabel every database in every saved project.
        /// </remarks>
        [TestMethod()]
        public void TheNewLibraryKindIsAppendedAndTheOldOnesKeepTheirValues() {
            Assert.AreEqual(0, (int)DataBaseSource.None);
            Assert.AreEqual(1, (int)DataBaseSource.Msp);
            Assert.AreEqual(2, (int)DataBaseSource.Lbm);
            Assert.AreEqual(3, (int)DataBaseSource.Text);
            Assert.AreEqual(4, (int)DataBaseSource.Fasta);
            Assert.AreEqual(5, (int)DataBaseSource.EieioLipid);
            Assert.AreEqual(6, (int)DataBaseSource.OadLipid);
            Assert.AreEqual(7, (int)DataBaseSource.EidLipid);
            Assert.AreEqual(8, (int)DataBaseSource.MsFinder);
            Assert.AreEqual(9, (int)DataBaseSource.PredictedMsp);
        }

        /// <summary>
        /// A library kind nobody has classified is treated as acquired, which is what every library
        /// was assumed to be before this existed.
        /// </summary>
        [TestMethod()]
        public void AnUnclassifiedLibraryKeepsTheOldAssumption() {
            Assert.IsFalse(AnnotationEvidence.SpectraArePredicted(DataBaseSource.None));
            Assert.IsFalse(AnnotationEvidence.SpectraArePredicted(DataBaseSource.Text));
            Assert.IsFalse(AnnotationEvidence.SpectraArePredicted(DataBaseSource.Fasta));
        }
    }
}
