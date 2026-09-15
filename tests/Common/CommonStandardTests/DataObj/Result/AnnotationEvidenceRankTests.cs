using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.DataObj.Result.Tests
{
    /// <summary>
    /// The qualitative half of candidate priority: which kind of evidence beats which, as an order
    /// rather than a weight.
    /// </summary>
    /// <remarks>
    /// The author of MS-DIAL asked for this shape on 2026-09-15 -- "それを重み的に置くのか、それとも
    /// Enum などに順序を付けるのかが１つの考えるポイントですが、私は後者を推奨したいです". The
    /// difference is not cosmetic. As a weight, "a spectrum was compared" would be worth some number
    /// of similarity points and a high enough score could buy a precursor-only candidate past a
    /// spectral match. As a separate key it cannot be bought at any score, which is what the
    /// programme's annotation policy already says in words.
    /// </remarks>
    [TestClass()]
    public class AnnotationEvidenceRankTests
    {
        /// <summary>
        /// Every member is ranked deliberately.
        /// </summary>
        /// <remarks>
        /// Adding a member to <see cref="AnnotationEvidenceSource"/> fails here until it is added to
        /// this table, and the table sits next to the reminder that
        /// <see cref="AnnotationEvidence.Rank(AnnotationEvidenceSource)"/> needs the same visit. The
        /// fallback in Rank is neutral rather than fatal so that an omission costs precision in an
        /// ordering instead of failing an annotation run mid-way; this is what stops the omission
        /// reaching a run at all.
        /// </remarks>
        private static readonly Dictionary<AnnotationEvidenceSource, int> Expected =
            new Dictionary<AnnotationEvidenceSource, int>
            {
                [AnnotationEvidenceSource.ReferenceSpectrum] = 6,
                [AnnotationEvidenceSource.RuleBased] = 6,
                [AnnotationEvidenceSource.WeakSpectrumMatch] = 4,
                [AnnotationEvidenceSource.Unspecified] = 3,
                [AnnotationEvidenceSource.Manual] = 3,
                [AnnotationEvidenceSource.ByStructurePredictionTool] = 2,
                [AnnotationEvidenceSource.BySpectrumPredictionTool] = 2,
                [AnnotationEvidenceSource.PrecursorOnly] = 2,
                [AnnotationEvidenceSource.UnmatchedSpectrum] = 1,
            };

        [TestMethod()]
        public void EveryEvidenceSourceIsRankedOnPurpose() {
            foreach (AnnotationEvidenceSource source in System.Enum.GetValues(typeof(AnnotationEvidenceSource))) {
                Assert.IsTrue(Expected.ContainsKey(source),
                    $"{source} was added to the enum without being placed in the order; " +
                    "rank it in AnnotationEvidence.Rank and record it here");
                Assert.AreEqual(Expected[source], AnnotationEvidence.Rank(source), $"rank of {source}");
            }
        }

        /// <summary>
        /// THE RANK IS NOT THE SERIALIZED VALUE, and the two orders genuinely disagree.
        /// </summary>
        /// <remarks>
        /// This is the test that <c>return (int)source</c> has to fail. The serialized values are a
        /// compatibility record -- 3 is where PredictedSpectrum sat before it was renamed, 8 is the
        /// member appended after it -- and they run in the order the members happened to be written.
        /// Two pairs below are inverted between the two orders, so a cast cannot pass.
        /// </remarks>
        [TestMethod()]
        public void TheRankIsNotTheSerializedValue() {
            Assert.IsTrue((int)AnnotationEvidenceSource.UnmatchedSpectrum > (int)AnnotationEvidenceSource.PrecursorOnly);
            Assert.IsTrue(AnnotationEvidence.Rank(AnnotationEvidenceSource.UnmatchedSpectrum)
                < AnnotationEvidence.Rank(AnnotationEvidenceSource.PrecursorOnly));

            Assert.IsTrue((int)AnnotationEvidenceSource.BySpectrumPredictionTool > (int)AnnotationEvidenceSource.ReferenceSpectrum);
            Assert.IsTrue(AnnotationEvidence.Rank(AnnotationEvidenceSource.BySpectrumPredictionTool)
                < AnnotationEvidence.Rank(AnnotationEvidenceSource.ReferenceSpectrum));
        }

        [TestMethod()]
        public void AComparedSpectrumOutranksOneThatWasNeverCompared() {
            Assert.IsTrue(AnnotationEvidence.Rank(AnnotationEvidenceSource.ReferenceSpectrum)
                > AnnotationEvidence.Rank(AnnotationEvidenceSource.PrecursorOnly),
                "the programme's annotation policy, in as many words: an MS/MS reference match " +
                "outranks a precursor-only suggestion");
            Assert.IsTrue(AnnotationEvidence.Rank(AnnotationEvidenceSource.WeakSpectrumMatch)
                > AnnotationEvidence.Rank(AnnotationEvidenceSource.PrecursorOnly),
                "a partial agreement is still an observation of the compound");
        }

        /// <summary>
        /// A comparison that explained nothing is worse than no comparison at all.
        /// </summary>
        /// <remarks>
        /// The author's criterion of 2026-09-10: a precursor-mass match with no spectrum acquired
        /// can legitimately be reported at class level for a lipid, while a precursor-mass match
        /// WITH a spectrum acquired that failed is unknown unless retention time supports it. The
        /// second is a positive finding against the candidate, not an absence of evidence.
        /// </remarks>
        [TestMethod()]
        public void AFailedComparisonRanksBelowNoComparison() {
            Assert.IsTrue(AnnotationEvidence.Rank(AnnotationEvidenceSource.UnmatchedSpectrum)
                < AnnotationEvidence.Rank(AnnotationEvidenceSource.PrecursorOnly));
        }

        /// <summary>
        /// A computed spectrum or a computed structure is a hypothesis; a real spectrum that partly
        /// agreed is an observation.
        /// </summary>
        /// <remarks>
        /// Confirmed by the author on 2026-09-14. Both directions of in-silico work sit here, and
        /// they sit together: they are one tag on the way out and the ordering has no reason to
        /// separate them either.
        /// </remarks>
        [TestMethod()]
        public void InSilicoRanksBelowAnyComparedSpectrum() {
            foreach (var inSilico in new[] {
                AnnotationEvidenceSource.ByStructurePredictionTool,
                AnnotationEvidenceSource.BySpectrumPredictionTool,
            }) {
                Assert.IsTrue(AnnotationEvidence.Rank(inSilico)
                    < AnnotationEvidence.Rank(AnnotationEvidenceSource.WeakSpectrumMatch), $"{inSilico}");
                Assert.IsTrue(AnnotationEvidence.Rank(inSilico)
                    < AnnotationEvidence.Rank(AnnotationEvidenceSource.ReferenceSpectrum), $"{inSilico}");
            }
            Assert.AreEqual(
                AnnotationEvidence.Rank(AnnotationEvidenceSource.ByStructurePredictionTool),
                AnnotationEvidence.Rank(AnnotationEvidenceSource.BySpectrumPredictionTool));
        }

        /// <summary>
        /// An in-silico candidate and a bare precursor match are left tied on purpose.
        /// </summary>
        /// <remarks>
        /// Whether a calculation outranks a bare mass has not been decided, and an ordering should
        /// not invent an answer: tied, TotalScore decides between them exactly as it does today. If
        /// the question is settled later, this test is where the answer gets written down.
        /// </remarks>
        [TestMethod()]
        public void InSilicoAndPrecursorOnlyAreDeliberatelyTied() {
            Assert.AreEqual(
                AnnotationEvidence.Rank(AnnotationEvidenceSource.PrecursorOnly),
                AnnotationEvidence.Rank(AnnotationEvidenceSource.ByStructurePredictionTool));
        }

        /// <summary>
        /// For lipidomics the diagnostic-fragment rules are the evidence, so they rank with a
        /// reference spectrum rather than under it.
        /// </summary>
        [TestMethod()]
        public void TheLipidRuleSetRanksWithAReferenceSpectrum() {
            Assert.AreEqual(
                AnnotationEvidence.Rank(AnnotationEvidenceSource.ReferenceSpectrum),
                AnnotationEvidence.Rank(AnnotationEvidenceSource.RuleBased));
        }

        /// <summary>
        /// "Not recorded" is neither credited nor penalised.
        /// </summary>
        /// <remarks>
        /// Every candidate in a project written before the evidence record existed reads
        /// Unspecified. Ranked last, opening such a project would sink all of its candidates beneath
        /// anything a fresh annotation added; ranked first, an unrecorded candidate would outrank a
        /// measured one. Mid-rank, a project where nothing is recorded has every candidate tied and
        /// keeps exactly the order it has today.
        /// </remarks>
        [TestMethod()]
        public void AnUnrecordedSourceSitsBetweenTheComparedAndTheUncompared() {
            var unspecified = AnnotationEvidence.Rank(AnnotationEvidenceSource.Unspecified);

            Assert.IsTrue(unspecified < AnnotationEvidence.Rank(AnnotationEvidenceSource.WeakSpectrumMatch));
            Assert.IsTrue(unspecified > AnnotationEvidence.Rank(AnnotationEvidenceSource.PrecursorOnly));
        }

        /// <summary>
        /// A person's intervention is not decided here, so this key stays neutral about it.
        /// </summary>
        /// <remarks>
        /// Manual means a human assertion with no comparison behind it -- today, marking a peak
        /// unknown. A person ACCEPTING a candidate is a different fact and wins higher up: at
        /// IsManuallyModified, the first key of MsScanMatchResultContainer.ResultOrder, and at the
        /// Manual bit of SourceType, which is the top bit of the flags byte and so dominates the
        /// facade's first key. By the time this rank is read, the human has already won.
        /// </remarks>
        [TestMethod()]
        public void AHumanAssertionIsNeutralBecauseItWinsHigherUp() {
            Assert.AreEqual(
                AnnotationEvidence.Rank(AnnotationEvidenceSource.Unspecified),
                AnnotationEvidence.Rank(AnnotationEvidenceSource.Manual));
            Assert.IsTrue((byte)SourceType.Manual > (byte)(SourceType.DataBases | SourceType.Unknown),
                "which is the higher key this defers to");
        }

        /// <summary>
        /// The placeholder that DefaultIfEmpty introduces into every ordering key is not a candidate.
        /// </summary>
        [TestMethod()]
        public void RankOfGivesAPlaceholderNoStanding() {
            var worst = Expected.Values.Min();

            Assert.IsTrue(AnnotationEvidence.RankOf(null) < worst,
                "otherwise an empty slot would beat a candidate whose spectrum was compared and failed");
            Assert.AreEqual(
                AnnotationEvidence.Rank(AnnotationEvidenceSource.WeakSpectrumMatch),
                AnnotationEvidence.RankOf(new MsScanMatchResult { EvidenceSource = AnnotationEvidenceSource.WeakSpectrumMatch }));
        }
    }
}
