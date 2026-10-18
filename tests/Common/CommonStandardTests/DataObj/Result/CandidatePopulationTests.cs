using CompMs.Common.DataObj.Result;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.DataObj.Result.Tests
{
    /// <summary>
    /// The recording rule for the candidate counts.
    /// </summary>
    /// <remarks>
    /// These pin the arithmetic and the two disciplines the type exists to enforce: that the
    /// reference-matched figure is counted with the run's own predicate rather than re-derived, and
    /// that one population's numbers can only reach the results the author hands it.
    /// </remarks>
    [TestClass()]
    public class CandidatePopulationTests
    {
        [TestMethod()]
        public void ThePopulationRecordsAllThreeTiers() {
            var scored = Candidates(0.9f, 0.8f, 0.7f, 0.6f, 0.5f);
            var named = scored.Take(4).ToList();

            var population = CandidatePopulation.Of(scored, named, r => r.TotalScore >= 0.8f);
            var stored = population.RecordOn(named[0]);

            Assert.AreEqual(5, stored.CandidatesFound);
            Assert.AreEqual(4, stored.CandidatesAboveThreshold);
            Assert.AreEqual(2, stored.CandidatesReferenceMatched);
        }

        [TestMethod()]
        public void TheReferenceMatchedCountUsesTheSuppliedPredicateNotTheStoredBoolean() {
            // The whole reason the predicate is a required parameter. Every candidate here has
            // IsReferenceMatched false as a stored value, but the run's own definition says two of
            // them are matches. Counting the property instead would silently answer a different
            // question than the selection the caller goes on to make.
            var scored = Candidates(0.9f, 0.8f, 0.4f);
            Assert.IsTrue(scored.All(r => !r.IsReferenceMatched), "fixture precondition");

            var population = CandidatePopulation.Of(scored, scored, r => r.TotalScore >= 0.8f);
            var stored = population.RecordOn(scored[0]);

            Assert.AreEqual(2, stored.CandidatesReferenceMatched);
        }

        [TestMethod()]
        public void APopulationWithNothingNamedRecordsZeroRatherThanNull() {
            // Pins the type's arithmetic, and nothing more -- read the caveat. Zero is the right
            // value for this state (the annotator did score three candidates and named none of
            // them; null would say it did not look), but NO PRODUCTION SITE CAN PERSIST IT: the
            // only carriers of these counts are the candidates that survived selection, so an
            // annotator that named nothing stores no result and leaves no carrier. This test
            // therefore proves the type is correct, not that the case is covered end to end.
            // Closing that gap needs a record that is not a survivor, which these three fields
            // cannot be. See the remarks on MsScanMatchResult.CandidatesFound.
            var scored = Candidates(0.4f, 0.3f, 0.2f);

            var population = CandidatePopulation.Of(scored, new List<MsScanMatchResult>(), r => r.IsReferenceMatched);
            var stored = population.RecordOn(scored[0]);

            Assert.AreEqual(3, stored.CandidatesFound);
            Assert.AreEqual(0, stored.CandidatesAboveThreshold);
            Assert.AreEqual(0, stored.CandidatesReferenceMatched);
        }

        [TestMethod()]
        public void AnUnrecordedResultKeepsNullOnAllThree() {
            var untouched = new MsScanMatchResult();

            Assert.IsNull(untouched.CandidatesFound);
            Assert.IsNull(untouched.CandidatesAboveThreshold);
            Assert.IsNull(untouched.CandidatesReferenceMatched,
                "null is how a result from a process that records nothing, or from an older "
                + "project, has to read -- see MsScanMatchResultBackwardCompatibilityTests");
        }

        [TestMethod()]
        public void RecordOnAllStampsEverySurvivorAndNothingElse() {
            var scored = Candidates(0.9f, 0.8f, 0.7f, 0.6f);
            var population = CandidatePopulation.Of(scored, scored, r => r.TotalScore >= 0.8f);

            var stored = population.RecordOnAll(scored.Take(2));

            Assert.AreEqual(2, stored.Count);
            Assert.IsTrue(stored.All(r => r.CandidatesFound == 4));
            Assert.IsNull(scored[2].CandidatesFound, "a discarded candidate is not stamped");
            Assert.IsNull(scored[3].CandidatesFound);
        }

        [TestMethod()]
        public void TwoPopulationsDoNotLeakIntoEachOther() {
            // The EAD lipid process holds two populations at once: a molecular-species query and a
            // generated-lipid query per factory. Each stored result must carry the numbers of the
            // population it came from.
            var moleculeCandidates = Candidates(0.9f, 0.8f, 0.7f);
            var lipidCandidates = Candidates(0.95f, 0.5f);
            var molecule = CandidatePopulation.Of(moleculeCandidates, moleculeCandidates, r => r.TotalScore >= 0.8f);
            var lipid = CandidatePopulation.Of(lipidCandidates, lipidCandidates, r => r.TotalScore >= 0.8f);

            var fromMolecule = molecule.RecordOn(moleculeCandidates[0]);
            var fromLipid = lipid.RecordOn(lipidCandidates[0]);

            Assert.AreEqual(3, fromMolecule.CandidatesFound);
            Assert.AreEqual(2, fromMolecule.CandidatesReferenceMatched);
            Assert.AreEqual(2, fromLipid.CandidatesFound);
            Assert.AreEqual(1, fromLipid.CandidatesReferenceMatched);
        }

        [TestMethod()]
        public void CountingDoesNotEnumerateTheScoredSequenceMoreThanOnce() {
            // A second enumeration is not merely wasted work at one site: the EAD lipid annotator's
            // FindCandidates mutates its database, so re-running it would change existing values.
            var enumerations = 0;
            var scored = Candidates(0.9f, 0.8f);
            IEnumerable<MsScanMatchResult> counted = Counting(scored, () => enumerations++);

            CandidatePopulation.Of(counted, scored, r => r.IsReferenceMatched);

            Assert.AreEqual(1, enumerations);
        }

        private static IEnumerable<MsScanMatchResult> Counting(IEnumerable<MsScanMatchResult> source, System.Action onEnumerate) {
            onEnumerate();
            foreach (var item in source) {
                yield return item;
            }
        }

        private static List<MsScanMatchResult> Candidates(params float[] scores) {
            return scores.Select(score => new MsScanMatchResult { TotalScore = score, }).ToList();
        }
    }
}
