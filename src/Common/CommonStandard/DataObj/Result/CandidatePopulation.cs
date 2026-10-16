using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.DataObj.Result
{
    /// <summary>
    /// The candidate population one annotator scored for one peak, ready to be recorded on each
    /// result that survived selection.
    /// </summary>
    /// <remarks>
    /// This exists to make one discipline mechanical: every annotation process narrows its
    /// candidates in at least two steps before storing at most a handful, and more than one
    /// population can be live at once. The EAD lipid process runs a molecular-species query and
    /// then a generated-lipid query per factory; the LC-IM-MS process runs once per drift peak;
    /// AIF acquisition runs the same annotator once per collision energy. Giving one population's
    /// numbers to another's results would be a false record that nothing downstream could detect.
    /// Constructing the population as a value and then recording it forces the author to name which
    /// one they mean.
    ///
    /// A population is identified by (peak, annotator, product-ion spectrum), not by (peak,
    /// annotator) -- see the remarks on <see cref="MsScanMatchResult.CandidatesFound"/> for what a
    /// consumer has to group by, and for the one case these counts cannot express: an annotator
    /// that named nothing stores no result, so it leaves no carrier for its population size.
    ///
    /// It also keeps the counts consistent with each other by construction: the reference-matched
    /// figure is counted from the same list the above-threshold figure was counted from, rather
    /// than re-derived later from a different call.
    ///
    /// Counting is O(1) and cannot re-run a database search.
    /// <c>IMatchResultFinder.FindCandidates</c> is declared to return <c>List&lt;TResult&gt;</c>,
    /// and every evaluator's <c>FilterByThreshold</c> ends in <c>ToList()</c>, so both sequences
    /// reaching <see cref="Of"/> are already materialised and <c>Count()</c> resolves through
    /// <c>ICollection</c>. A caller holding a genuinely lazy sequence -- the GC-MS scorer yields --
    /// must materialise it before calling this, and the one such site does.
    /// </remarks>
    public sealed class CandidatePopulation
    {
        private readonly int _found;
        private readonly int _aboveThreshold;
        private readonly int _referenceMatched;

        private CandidatePopulation(int found, int aboveThreshold, int referenceMatched) {
            _found = found;
            _aboveThreshold = aboveThreshold;
            _referenceMatched = referenceMatched;
        }

        /// <summary>
        /// Counts a population from the candidates an annotator scored and the subset it was
        /// willing to name.
        /// </summary>
        /// <param name="scored">
        /// Everything the annotator scored for this peak, before any selection. This is the number
        /// that cannot be recovered afterwards.
        /// </param>
        /// <param name="named">
        /// The subset the annotator judged to be either a reference match or a precursor-only
        /// suggestion -- what <c>FilterByThreshold</c> returns, or at the sites that do not use an
        /// evaluator, whatever filter stands in its place.
        /// </param>
        /// <param name="isReferenceMatched">
        /// The run's own definition of a reference match, so this count cannot disagree with the
        /// selection the caller goes on to make. Pass the evaluator's own predicate --
        /// <c>_evaluator.IsReferenceMatched</c> -- rather than reading the property here: a custom
        /// evaluator is free to define it differently, and two sources for one number is how the
        /// defect this whole record exists to end gets reintroduced. Required rather than
        /// defaulted for the same reason.
        /// </param>
        public static CandidatePopulation Of(
            IEnumerable<MsScanMatchResult> scored,
            IEnumerable<MsScanMatchResult> named,
            Func<MsScanMatchResult, bool> isReferenceMatched) {

            var namedList = named as IReadOnlyCollection<MsScanMatchResult> ?? named.ToList();
            return new CandidatePopulation(
                scored.Count(),
                namedList.Count,
                namedList.Count(result => isReferenceMatched(result)));
        }

        /// <summary>
        /// Records the population on a result that survived selection, and returns it so this can
        /// be used inside an existing projection.
        /// </summary>
        public MsScanMatchResult RecordOn(MsScanMatchResult result) {
            result.CandidatesFound = _found;
            result.CandidatesAboveThreshold = _aboveThreshold;
            result.CandidatesReferenceMatched = _referenceMatched;
            return result;
        }

        /// <summary>Records the population on every result that survived selection.</summary>
        public IReadOnlyList<MsScanMatchResult> RecordOnAll(IEnumerable<MsScanMatchResult> results) {
            var recorded = new List<MsScanMatchResult>();
            foreach (var result in results) {
                recorded.Add(RecordOn(result));
            }
            return recorded;
        }
    }
}
