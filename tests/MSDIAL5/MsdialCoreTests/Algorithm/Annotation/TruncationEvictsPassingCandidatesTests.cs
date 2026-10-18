using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    /// <summary>
    /// Pins what the per-annotator cap discards, and shows that the candidate counts are the only
    /// thing that now makes the loss visible.
    /// </summary>
    /// <remarks>
    /// This one is a characterization test rather than a defect pin: the cap is deliberate.
    /// NUMBER_OF_ANNOTATION_RESULTS is 3 in StandardAnnotationProcess,
    /// EadLipidomicsAnnotationProcess and LcimmsStandardAnnotationProcess; the IMMS and
    /// proteo-metabolomics processes keep one; GC-MS keeps five. What was not deliberate is that
    /// the discarded candidates left no trace of having existed, so a fourth equally-good
    /// reference was indistinguishable from there having been only three references in the library.
    ///
    /// It will fail if the cap, the ordering, or the recording changes -- which is the point. If
    /// someone raises the cap, this test says so out loud instead of the export quietly gaining
    /// rows. Uses the production MsScanMatchResultEvaluator rather than a mock, because its
    /// behaviour here is the subject: FilterByThreshold is IsAnnotationSuggested ||
    /// IsReferenceMatched, and its constructor ignores the search parameter entirely.
    /// </remarks>
    [TestClass()]
    public class TruncationEvictsPassingCandidatesTests
    {
        private const int Cap = 3;

        [TestMethod()]
        public void TheCapDiscardsACandidateThatPassedTheThreshold() {
            var evaluator = new MsScanMatchResultEvaluator(new MsRefSearchParameterBase());
            var candidates = FourReferenceMatches();

            var passing = evaluator.FilterByThreshold(candidates);
            var kept = evaluator.SelectTopN(passing, Cap).ToList();

            Assert.AreEqual(4, passing.Count, "all four are reference matches");
            Assert.AreEqual(Cap, kept.Count);
            var evicted = candidates.Except(kept).Single();
            Assert.AreEqual(0.61f, evicted.TotalScore, "the lowest-scoring of the four is dropped");
        }

        [TestMethod()]
        public void TheStoredResultsSayHowManyPassedEvenThoughOnlyThreeAreStored() {
            // The recovery. Each stored row reports 4 above threshold while three rows exist, so a
            // reader can tell that a candidate was discarded -- and, from the reference-matched
            // figure, that it was a reference match rather than a suggestion. Before these keys
            // existed neither was recoverable from the file.
            var evaluator = new MsScanMatchResultEvaluator(new MsRefSearchParameterBase());
            var candidates = FourReferenceMatches();
            var passing = evaluator.FilterByThreshold(candidates);
            var population = CandidatePopulation.Of(candidates, passing, evaluator.IsReferenceMatched);

            var container = new MsScanMatchResultContainer();
            container.AddResults(population.RecordOnAll(evaluator.SelectTopN(passing, Cap)));

            Assert.AreEqual(Cap, container.MatchResults.Count);
            foreach (var stored in container.MatchResults) {
                Assert.AreEqual(4, stored.CandidatesFound);
                Assert.AreEqual(4, stored.CandidatesAboveThreshold,
                    "so the reader knows one more passed than was kept");
                Assert.AreEqual(4, stored.CandidatesReferenceMatched,
                    "and that the discarded one was a reference match, not a suggestion");
            }
        }

        [TestMethod()]
        public void TheEvictedCandidateLeavesNoOtherTrace() {
            // What the counts are compensating for. The evicted object is not in the container, is
            // not referenced by it, and carries no recorded population of its own -- so without
            // the counts on its surviving siblings there would be nothing at all.
            var evaluator = new MsScanMatchResultEvaluator(new MsRefSearchParameterBase());
            var candidates = FourReferenceMatches();
            var passing = evaluator.FilterByThreshold(candidates);
            var population = CandidatePopulation.Of(candidates, passing, evaluator.IsReferenceMatched);
            var kept = population.RecordOnAll(evaluator.SelectTopN(passing, Cap));
            var container = new MsScanMatchResultContainer();
            container.AddResults(kept);

            var evicted = candidates.Except(kept).Single();
            Assert.IsFalse(container.MatchResults.Contains(evicted));
            Assert.IsNull(evicted.CandidatesFound, "a discarded candidate is never stamped");
            Assert.AreNotEqual(evicted, container.Representative);
        }

        /// <summary>
        /// Four candidates that all pass, with distinct scores so the eviction is deterministic.
        /// </summary>
        /// <remarks>
        /// IsReferenceMatched is set as a stored value because that is what the production
        /// evaluator reads; the annotators set it during their own Validate step before any of this
        /// runs.
        /// </remarks>
        private static List<MsScanMatchResult> FourReferenceMatches() {
            return new List<MsScanMatchResult>
            {
                Candidate("first", 0.94f),
                Candidate("second", 0.83f),
                Candidate("third", 0.72f),
                Candidate("fourth", 0.61f),
            };
        }

        private static MsScanMatchResult Candidate(string name, float score) {
            return new MsScanMatchResult
            {
                Name = name,
                Source = SourceType.MspDB,
                AnnotatorID = "MspDB",
                TotalScore = score,
                IsPrecursorMzMatch = true,
                IsSpectrumMatch = true,
                IsReferenceMatched = true,
            };
        }
    }
}
