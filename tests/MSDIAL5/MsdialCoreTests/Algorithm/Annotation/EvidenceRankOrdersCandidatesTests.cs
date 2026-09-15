using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    /// <summary>
    /// The evidence rank reaching the three orderings that decide which candidates are kept and
    /// which one becomes the annotation.
    /// </summary>
    /// <remarks>
    /// Three keys, one rule. MsScanMatchResultEvaluator orders candidates from ONE annotator,
    /// FacadeMatchResultEvaluator orders them ACROSS annotators, and
    /// MsScanMatchResultContainer.ResultOrder picks the Representative -- the one whose name is
    /// exported. They were written at different times and had drifted apart; the rank goes into all
    /// three, in the same position relative to the keys they share, so a candidate cannot win one
    /// ordering and lose another for reasons neither of them states.
    ///
    /// SelectTopN is untouched, as agreed: it holds no ordering expression of its own and defers
    /// entirely to SelectTopHit, which is why <see cref="TheCapKeepsTheBetterEvidenceNotTheBetterScore"/>
    /// can show the rank propagating through it without a line changing there.
    /// </remarks>
    [TestClass()]
    public class EvidenceRankOrdersCandidatesTests
    {
        /// <summary>
        /// A PROJECT WRITTEN BEFORE THE EVIDENCE RECORD EXISTED IS ORDERED EXACTLY AS IT WAS.
        /// </summary>
        /// <remarks>
        /// This is the safety property of putting "not recorded" mid-rank rather than last. Every
        /// candidate deserialized from such a project reads Unspecified -- absent MessagePack keys
        /// give default(T), not the property initializer -- so every candidate ties on the new key
        /// and the keys beneath it decide, as they did before. Argmax keeps the first of a tie, so
        /// even the tie-breaking is unchanged.
        /// </remarks>
        [TestMethod()]
        public void ALegacyContainerKeepsTodaysOrder() {
            var legacy = new[] {
                Candidate("low", 0.40f, AnnotationEvidenceSource.Unspecified),
                Candidate("high", 0.90f, AnnotationEvidenceSource.Unspecified),
                Candidate("middle", 0.70f, AnnotationEvidenceSource.Unspecified),
            };
            foreach (var candidate in legacy) {
                Assert.AreEqual(AnnotationEvidenceSource.Unspecified, candidate.EvidenceSource);
            }

            var evaluator = new MsScanMatchResultEvaluator(new MsRefSearchParameterBase());
            var container = new MsScanMatchResultContainer();
            container.AddResults(legacy);

            CollectionAssert.AreEqual(
                new[] { "high", "middle", "low" },
                evaluator.SelectTopN(legacy, 3).Select(r => r.Name).ToArray(),
                "score alone still decides when nothing was recorded");
            Assert.AreEqual("high", container.Representative.Name);
            Assert.AreEqual("high", new FacadeMatchResultEvaluator().SelectTopHit(legacy).Name);
        }

        /// <summary>
        /// A CONTAINER HOLDING BOTH: a candidate from before the evidence record and one graded
        /// since. The unrecorded one is not sunk by the mere fact that it says nothing.
        /// </summary>
        /// <remarks>
        /// This is what mid-rank buys, and the only place it is observable. It happens whenever a
        /// project saved earlier is reopened and something is annotated into it -- accepting an
        /// MS-FINDER structure, re-running one annotator -- and it is the case that would make
        /// ranking "not recorded" last actively wrong: a candidate whose spectrum was compared and
        /// explained nothing would be promoted over a perfectly good older one purely because the
        /// older one predates the bookkeeping.
        /// </remarks>
        [TestMethod()]
        public void AnUnrecordedCandidateIsNotSunkByAFreshlyGradedOne() {
            var legacy = Candidate("annotated before the record existed", 0.75f, AnnotationEvidenceSource.Unspecified);
            var graded = Candidate("explained nothing", 0.80f, AnnotationEvidenceSource.UnmatchedSpectrum);

            var container = new MsScanMatchResultContainer();
            container.AddResults(new[] { graded, legacy });

            Assert.AreEqual("annotated before the record existed", container.Representative.Name,
                "silence about the evidence is not a finding against it");
            Assert.AreEqual("annotated before the record existed",
                new MsScanMatchResultEvaluator(new MsRefSearchParameterBase()).SelectTopHit(new[] { graded, legacy }).Name);
        }

        /// <summary>
        /// Within one annotator, a name a spectrum decided beats a better-scoring name it did not.
        /// </summary>
        /// <remarks>
        /// The scores are the wrong way round on purpose: the precursor-only candidate scores
        /// higher, which is the ordinary case rather than a contrived one. A precursor-only result
        /// is scored on fewer terms, and the terms it keeps -- accurate mass, sometimes retention
        /// time -- are the ones that agree most easily, so averaging them produces a larger number
        /// than a genuine spectral comparison usually does. Under the old key that number won.
        /// </remarks>
        [TestMethod()]
        public void ASpectrumInformedCandidateOutranksABetterScoringPrecursorOnlyOne() {
            var evaluator = new MsScanMatchResultEvaluator(new MsRefSearchParameterBase());
            var candidates = new[] {
                Candidate("mass only", 0.95f, AnnotationEvidenceSource.PrecursorOnly),
                Candidate("spectrum compared", 0.72f, AnnotationEvidenceSource.ReferenceSpectrum),
            };

            Assert.AreEqual("spectrum compared", evaluator.SelectTopHit(candidates).Name);
        }

        /// <summary>
        /// Even a comparison that only partly agreed beats one that never happened.
        /// </summary>
        [TestMethod()]
        public void AWeakMatchOutranksABetterScoringPrecursorOnlyCandidate() {
            var evaluator = new MsScanMatchResultEvaluator(new MsRefSearchParameterBase());
            var candidates = new[] {
                Candidate("mass only", 0.95f, AnnotationEvidenceSource.PrecursorOnly),
                Candidate("fell short", 0.55f, AnnotationEvidenceSource.WeakSpectrumMatch),
            };

            Assert.AreEqual("fell short", evaluator.SelectTopHit(candidates).Name);
        }

        /// <summary>
        /// A spectrum that explained nothing loses to a candidate with no spectrum at all.
        /// </summary>
        [TestMethod()]
        public void AnUnmatchedSpectrumLosesToACandidateWithNoSpectrum() {
            var evaluator = new MsScanMatchResultEvaluator(new MsRefSearchParameterBase());
            var candidates = new[] {
                Candidate("explained nothing", 0.80f, AnnotationEvidenceSource.UnmatchedSpectrum),
                Candidate("mass only", 0.60f, AnnotationEvidenceSource.PrecursorOnly),
            };

            Assert.AreEqual("mass only", evaluator.SelectTopHit(candidates).Name);
        }

        /// <summary>
        /// The verdicts still outrank the evidence, which is why this change does not flip what a
        /// text database decides.
        /// </summary>
        /// <remarks>
        /// A text database sets IsReferenceMatched on precursor-mass agreement alone. That is a
        /// separate question from this one and deliberately untouched here: the rank sits BELOW
        /// IsReferenceMatched, so the text-database match still wins, and a change to that would be
        /// a decision taken on its own.
        /// </remarks>
        [TestMethod()]
        public void TheRankSitsBelowTheAnnotatorsOwnVerdict() {
            var evaluator = new MsScanMatchResultEvaluator(new MsRefSearchParameterBase());
            var textDb = Candidate("from a text database", 0.50f, AnnotationEvidenceSource.PrecursorOnly);
            var suggestion = Candidate("a suggestion", 0.99f, AnnotationEvidenceSource.ReferenceSpectrum);
            suggestion.IsReferenceMatched = false;
            suggestion.IsAnnotationSuggested = true;

            Assert.AreEqual("from a text database", evaluator.SelectTopHit(new[] { suggestion, textDb }).Name);
        }

        /// <summary>
        /// ACROSS annotators, what was compared outranks which library the analyst listed first.
        /// </summary>
        /// <remarks>
        /// This is the programme's annotation policy written as code: "a lower-priority MS/MS
        /// reference match outranks a higher-priority precursor-only suggestion". Priority is the
        /// analyst's ordering of their databases -- IdentifySettingModel hands out
        /// <c>AnnotatorModels.Count - index</c> -- so before this the first library in their list
        /// won even when a later one had matched a spectrum and it had not.
        /// </remarks>
        [TestMethod()]
        public void TheFacadePrefersWhatWasComparedOverTheAnalystsLibraryOrder() {
            var preferred = Candidate("from the preferred library", 0.90f, AnnotationEvidenceSource.PrecursorOnly);
            preferred.Priority = 2;
            var later = Candidate("from a later library", 0.60f, AnnotationEvidenceSource.ReferenceSpectrum);
            later.Priority = 1;

            var facade = new FacadeMatchResultEvaluator();

            Assert.AreEqual("from a later library", facade.SelectTopHit(new[] { preferred, later }).Name);
        }

        /// <summary>
        /// Priority still decides when the evidence is the same kind.
        /// </summary>
        [TestMethod()]
        public void TheFacadeStillHonoursLibraryOrderBetweenEqualEvidence() {
            var preferred = Candidate("from the preferred library", 0.60f, AnnotationEvidenceSource.ReferenceSpectrum);
            preferred.Priority = 2;
            var later = Candidate("from a later library", 0.90f, AnnotationEvidenceSource.ReferenceSpectrum);
            later.Priority = 1;

            var facade = new FacadeMatchResultEvaluator();

            Assert.AreEqual("from the preferred library", facade.SelectTopHit(new[] { preferred, later }).Name);
        }

        /// <summary>
        /// A candidate a person accepted still wins, because Source is read before the rank.
        /// </summary>
        /// <remarks>
        /// SourceType.Manual is the top bit of the flags byte, so it dominates the facade's first
        /// key outright. This is why AnnotationEvidenceSource.Manual can be neutral in the rank
        /// without a human's decision losing anything.
        /// </remarks>
        [TestMethod()]
        public void AnAcceptedCandidateStillWinsOnSource() {
            var accepted = Candidate("what the analyst chose", 0.30f, AnnotationEvidenceSource.PrecursorOnly);
            accepted.Source = SourceType.MspDB | SourceType.Manual;
            accepted.Priority = 0;
            var measured = Candidate("what the software preferred", 0.95f, AnnotationEvidenceSource.ReferenceSpectrum);
            measured.Priority = 9;

            var facade = new FacadeMatchResultEvaluator();

            Assert.AreEqual("what the analyst chose", facade.SelectTopHit(new[] { measured, accepted }).Name);
        }

        /// <summary>
        /// The Representative -- the candidate whose name is exported -- follows the same rule.
        /// </summary>
        /// <remarks>
        /// Without this the change would be half-applied in the worst possible way: the better
        /// evidence would be kept by SelectTopN and then the exported name would still be taken from
        /// the higher-scoring candidate.
        /// </remarks>
        [TestMethod()]
        public void TheRepresentativeIsChosenByEvidenceBeforePriority() {
            var preferred = Candidate("from the preferred library", 0.90f, AnnotationEvidenceSource.PrecursorOnly);
            preferred.Priority = 2;
            var later = Candidate("from a later library", 0.60f, AnnotationEvidenceSource.ReferenceSpectrum);
            later.Priority = 1;

            var container = new MsScanMatchResultContainer();
            container.AddResults(new[] { preferred, later });

            Assert.AreEqual("from a later library", container.Representative.Name);
        }

        /// <summary>
        /// A person's edit is still final for the Representative.
        /// </summary>
        /// <remarks>
        /// IsManuallyModified is not a field: it reads the Manual bit of Source. So the container's
        /// first key and the facade's first key are the same fact asked twice, which is why the rank
        /// can stay neutral about a human's decision in both.
        /// </remarks>
        [TestMethod()]
        public void AManualEditStillDecidesTheRepresentative() {
            var edited = Candidate("what the analyst chose", 0.10f, AnnotationEvidenceSource.PrecursorOnly);
            edited.Source = SourceType.MspDB | SourceType.Manual;
            Assert.IsTrue(edited.IsManuallyModified);
            var measured = Candidate("what the software preferred", 0.95f, AnnotationEvidenceSource.ReferenceSpectrum);

            var container = new MsScanMatchResultContainer();
            container.AddResults(new[] { measured, edited });

            Assert.AreEqual("what the analyst chose", container.Representative.Name);
        }

        /// <summary>
        /// The per-annotator cap now discards the weakest EVIDENCE rather than the lowest score.
        /// </summary>
        /// <remarks>
        /// The companion to TruncationEvictsPassingCandidatesTests, and the proof that the rank
        /// reaches truncation without SelectTopN being touched: SelectTopN calls SelectTopHit
        /// repeatedly and removes what it returns, so an ordering change there is the only change
        /// needed. Four candidates pass the threshold and three survive; the one discarded is the
        /// one whose spectrum was compared and explained nothing, even though it outscores two
        /// survivors.
        /// </remarks>
        [TestMethod()]
        public void TheCapKeepsTheBetterEvidenceNotTheBetterScore() {
            var evaluator = new MsScanMatchResultEvaluator(new MsRefSearchParameterBase());
            var candidates = new List<MsScanMatchResult> {
                Candidate("explained nothing", 0.88f, AnnotationEvidenceSource.UnmatchedSpectrum),
                Candidate("compared", 0.70f, AnnotationEvidenceSource.ReferenceSpectrum),
                Candidate("fell short", 0.64f, AnnotationEvidenceSource.WeakSpectrumMatch),
                Candidate("mass only", 0.61f, AnnotationEvidenceSource.PrecursorOnly),
            };

            var passing = evaluator.FilterByThreshold(candidates);
            var kept = evaluator.SelectTopN(passing, 3).ToList();

            Assert.AreEqual(4, passing.Count, "all four pass; the cap is what removes one");
            CollectionAssert.AreEqual(
                new[] { "compared", "fell short", "mass only" },
                kept.Select(r => r.Name).ToArray());
            Assert.AreEqual("explained nothing", candidates.Except(kept).Single().Name,
                "the highest-scoring candidate is the one discarded, because its spectrum disagreed");
        }

        /// <summary>
        /// One annotator's candidates, distinguished only by score and by what was compared.
        /// </summary>
        /// <remarks>
        /// IsReferenceMatched is a stored value here because that is what the production evaluator
        /// reads; the annotators set it during their own Validate step before any of this runs.
        /// </remarks>
        private static MsScanMatchResult Candidate(string name, float score, AnnotationEvidenceSource evidence) {
            return new MsScanMatchResult
            {
                Name = name,
                Source = SourceType.MspDB,
                AnnotatorID = "MspDB",
                TotalScore = score,
                IsPrecursorMzMatch = true,
                IsReferenceMatched = true,
                EvidenceSource = evidence,
            };
        }
    }
}
