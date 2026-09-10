using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.DataObj.Result.Tests
{
    /// <summary>
    /// Three tiers for a reference spectrum that was compared: it settled the match, it fell short,
    /// or it explained nothing.
    /// </summary>
    /// <remarks>
    /// The tier exists because one cut-off cannot carry the distinction. At a 70% threshold, a
    /// candidate scoring 69% and a candidate that matched two fragments out of forty are both
    /// "below threshold", and treating them alike is what makes the threshold feel arbitrary. The
    /// author of MS-DIAL asked for exactly this split on 2026-09-10 -- "箸にも棒にもかからなかった
    /// Low score" against "まぁまぁ似てた Low score" -- and set the line himself.
    ///
    /// NO NEW NUMBER IS INTRODUCED. For metabolomics the line is the analyst's own
    /// MinimumSpectrumMatch, the number their acceptance criteria already use. For lipidomics the
    /// count is not consulted at all, because a single diagnostic fragment can settle a class --
    /// cholesteryl ester being the example given -- so the rules are asked instead.
    /// </remarks>
    [TestClass()]
    public class AnnotationEvidenceTests
    {
        private const float MinimumSpectrumMatch = 3f;

        [TestMethod()]
        public void AMatchedSpectrumKeepsItsEvidence() {
            var result = Compared(AnnotationEvidenceSource.ReferenceSpectrum, isSpectrumMatch: true, matchedPeaks: 8f);

            AnnotationEvidence.RecordSpectrumVerdict(result, TargetOmics.Metabolomics, MinimumSpectrumMatch);

            Assert.AreEqual(AnnotationEvidenceSource.ReferenceSpectrum, result.EvidenceSource);
        }

        [TestMethod()]
        public void AComparisonThatFellShortButExplainedFragmentsIsWeak() {
            // The 69%-against-70% case. Enough fragments matched to be worth reporting; the
            // conjunction of the acceptance criteria was not met.
            var result = Compared(AnnotationEvidenceSource.ReferenceSpectrum, isSpectrumMatch: false, matchedPeaks: 6f);

            AnnotationEvidence.RecordSpectrumVerdict(result, TargetOmics.Metabolomics, MinimumSpectrumMatch);

            Assert.AreEqual(AnnotationEvidenceSource.WeakSpectrumMatch, result.EvidenceSource);
        }

        [TestMethod()]
        public void AComparisonThatExplainedAlmostNothingIsUnmatched() {
            var result = Compared(AnnotationEvidenceSource.ReferenceSpectrum, isSpectrumMatch: false, matchedPeaks: 2f);

            AnnotationEvidence.RecordSpectrumVerdict(result, TargetOmics.Metabolomics, MinimumSpectrumMatch);

            Assert.AreEqual(AnnotationEvidenceSource.UnmatchedSpectrum, result.EvidenceSource,
                "two fragments against a minimum of three: nothing here identifies the compound");
        }

        [TestMethod()]
        public void TheLineIsTheAnalystsOwnMinimumAndNotAConstantOfOurs() {
            // Six matched fragments is weak under a minimum of eight and unmatched under none of
            // our choosing. Whoever sets the acceptance criteria sets this line too.
            var lenient = Compared(AnnotationEvidenceSource.ReferenceSpectrum, isSpectrumMatch: false, matchedPeaks: 6f);
            var strict = Compared(AnnotationEvidenceSource.ReferenceSpectrum, isSpectrumMatch: false, matchedPeaks: 6f);

            AnnotationEvidence.RecordSpectrumVerdict(lenient, TargetOmics.Metabolomics, 3f);
            AnnotationEvidence.RecordSpectrumVerdict(strict, TargetOmics.Metabolomics, 8f);

            Assert.AreEqual(AnnotationEvidenceSource.WeakSpectrumMatch, lenient.EvidenceSource);
            Assert.AreEqual(AnnotationEvidenceSource.UnmatchedSpectrum, strict.EvidenceSource);
        }

        [TestMethod()]
        public void WithTheMinimumSwitchedOffTheLineFallsBackToOneFragment() {
            // The analyst may set MinimumSpectrumMatch to zero. A count cannot then divide the two
            // tiers, so the floor becomes the only reading left that is true by any standard: a
            // comparison that matched no fragment at all explained nothing.
            var one = Compared(AnnotationEvidenceSource.ReferenceSpectrum, isSpectrumMatch: false, matchedPeaks: 1f);
            var none = Compared(AnnotationEvidenceSource.ReferenceSpectrum, isSpectrumMatch: false, matchedPeaks: 0f);

            AnnotationEvidence.RecordSpectrumVerdict(one, TargetOmics.Metabolomics, 0f);
            AnnotationEvidence.RecordSpectrumVerdict(none, TargetOmics.Metabolomics, 0f);

            Assert.AreEqual(AnnotationEvidenceSource.WeakSpectrumMatch, one.EvidenceSource);
            Assert.AreEqual(AnnotationEvidenceSource.UnmatchedSpectrum, none.EvidenceSource);
        }

        [TestMethod()]
        public void ALipidSettledByOneDiagnosticFragmentIsNeverCalledUnmatched() {
            // THE CHOLESTERYL ESTER CASE, and the reason lipidomics does not use the peak count.
            // One characteristic ion can establish a lipid class outright, so a count of one says
            // nothing about whether the spectrum explained the compound. Asking the count here
            // would demote a correct class assignment to "explained nothing".
            var result = Compared(AnnotationEvidenceSource.RuleBased, isSpectrumMatch: false, matchedPeaks: 1f);
            result.IsLipidClassMatch = true;

            AnnotationEvidence.RecordSpectrumVerdict(result, TargetOmics.Lipidomics, MinimumSpectrumMatch);

            Assert.AreEqual(AnnotationEvidenceSource.WeakSpectrumMatch, result.EvidenceSource);
            Assert.AreNotEqual(AnnotationEvidenceSource.UnmatchedSpectrum, result.EvidenceSource);
        }

        [TestMethod()]
        public void ALipidNoRuleRecognisedIsUnmatchedHoweverManyPeaksAgreed() {
            // The converse, and the reason this is not simply "lipids are always weak": a spectrum
            // can share plenty of fragments with a reference and still trigger no characteristic-ion
            // rule, which for a lipid means nothing was established.
            var result = Compared(AnnotationEvidenceSource.RuleBased, isSpectrumMatch: false, matchedPeaks: 20f);

            AnnotationEvidence.RecordSpectrumVerdict(result, TargetOmics.Lipidomics, MinimumSpectrumMatch);

            Assert.AreEqual(AnnotationEvidenceSource.UnmatchedSpectrum, result.EvidenceSource);
        }

        [TestMethod()]
        public void EachLipidRuleOnItsOwnIsEnoughToAvoidUnmatched() {
            foreach (var apply in new System.Action<MsScanMatchResult>[] {
                r => r.IsLipidChainsMatch = true,
                r => r.IsLipidClassMatch = true,
                r => r.IsLipidPositionMatch = true,
                r => r.IsOtherLipidMatch = true,
            }) {
                var result = Compared(AnnotationEvidenceSource.RuleBased, isSpectrumMatch: false, matchedPeaks: 0f);
                apply(result);

                AnnotationEvidence.RecordSpectrumVerdict(result, TargetOmics.Lipidomics, MinimumSpectrumMatch);

                Assert.AreEqual(AnnotationEvidenceSource.WeakSpectrumMatch, result.EvidenceSource);
            }
        }

        [TestMethod()]
        public void ACandidateWithNoSpectrumComparedIsLeftAlone() {
            // PrecursorOnly is not a failed comparison, it is the absence of one, and the author's
            // criteria treat the two oppositely: with no MS2 acquired a lipid may still be named at
            // class level, while an MS2 that was acquired and failed is unknown. Downgrading this
            // would erase that distinction.
            var result = Compared(AnnotationEvidenceSource.PrecursorOnly, isSpectrumMatch: false, matchedPeaks: 0f);

            AnnotationEvidence.RecordSpectrumVerdict(result, TargetOmics.Metabolomics, MinimumSpectrumMatch);

            Assert.AreEqual(AnnotationEvidenceSource.PrecursorOnly, result.EvidenceSource);
        }

        [TestMethod()]
        public void AManualOrPredictedRecordIsLeftAlone() {
            foreach (var untouched in new[] {
                AnnotationEvidenceSource.Manual,
                AnnotationEvidenceSource.PredictedSpectrum,
                AnnotationEvidenceSource.Unspecified,
            }) {
                var result = Compared(untouched, isSpectrumMatch: false, matchedPeaks: 0f);

                AnnotationEvidence.RecordSpectrumVerdict(result, TargetOmics.Metabolomics, MinimumSpectrumMatch);

                Assert.AreEqual(untouched, result.EvidenceSource,
                    "only a database search against a reference spectrum is graded here");
            }
        }

        [TestMethod()]
        public void TheNewMembersSitAfterTheExistingOnesSoStoredValuesDoNotMove() {
            // These are serialized under key 40 as a byte. Inserting a member anywhere but the end
            // would silently relabel every annotation in every existing project.
            Assert.AreEqual(0, (int)AnnotationEvidenceSource.Unspecified);
            Assert.AreEqual(1, (int)AnnotationEvidenceSource.PrecursorOnly);
            Assert.AreEqual(2, (int)AnnotationEvidenceSource.ReferenceSpectrum);
            Assert.AreEqual(3, (int)AnnotationEvidenceSource.PredictedSpectrum);
            Assert.AreEqual(4, (int)AnnotationEvidenceSource.RuleBased);
            Assert.AreEqual(5, (int)AnnotationEvidenceSource.Manual);
            Assert.AreEqual(6, (int)AnnotationEvidenceSource.WeakSpectrumMatch);
            Assert.AreEqual(7, (int)AnnotationEvidenceSource.UnmatchedSpectrum);
        }

        private static MsScanMatchResult Compared(AnnotationEvidenceSource evidence, bool isSpectrumMatch, float matchedPeaks) {
            return new MsScanMatchResult
            {
                EvidenceSource = evidence,
                MeasuredTerms = MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass,
                IsSpectrumMatch = isSpectrumMatch,
                MatchedPeaksCount = matchedPeaks,
            };
        }
    }
}
