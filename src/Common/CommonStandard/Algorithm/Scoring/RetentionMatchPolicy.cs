using CompMs.Common.DataObj.Result;

namespace CompMs.Common.Algorithm.Scoring
{
    /// <summary>
    /// One statement of the rule that a reference with no retention time neither gains nor loses by
    /// retention time.
    /// </summary>
    /// <remarks>
    /// THE DEFECT THIS REPLACES. A <c>MoleculeMsReference</c> that carries no retention time holds
    /// <c>ChromX.RetentionTime.Default</c>, whose value is -1. Two places then treated that -1 as a
    /// measurement: the three-argument <c>GetGaussianSimilarity</c> overload, which evaluates
    /// <c>exp(-0.5 * ((actual - reference) / tolerance)^2)</c> on whatever it is handed, and a plain
    /// <c>Math.Abs</c> comparison in each annotator's ValidateBase, which set <c>IsRtMatch</c> on the
    /// same grounds. An early-eluting peak therefore agreed, on retention time, with a reference that
    /// had none -- and agreed more strongly the earlier it eluted.
    ///
    /// WHAT THE DAMAGE ACTUALLY WAS. Not primarily the total score. <c>DataAccess.GetAnnotationCode</c>
    /// reads <c>IsRtMatch</c> and nothing else to promote 430 ("m/z + MS/MS matched") to 330
    /// ("RT + MS/MS matched"), and 440 to 340 on the GC-MS branch. So the fabricated agreement raised
    /// the published confidence code in the "Annotation tag (VS1.0)" column of every analysis and
    /// alignment export. A claim of retention-time confirmation was written to file for references
    /// that had no retention time to confirm.
    ///
    /// WHY THIS IS AN EXEMPTION AND NOT A REJECTION. Forcing <c>IsRtMatch</c> to false would not
    /// demote such a candidate, it would delete it. <c>IsReferenceMatched</c> and
    /// <c>IsAnnotationSuggested</c> share the same retention clause, so both go false together;
    /// <c>MsScanMatchResultEvaluator.FilterByThreshold</c> is <c>suggested || matched</c>; and
    /// <c>StandardAnnotationProcess</c> stores only what that returns. The candidate would never
    /// reach the file, and nothing downstream could read its m/z or MS/MS evidence either. In a
    /// library where only some entries carry retention times -- the case this was reported for --
    /// that would silently discard every entry that does not.
    ///
    /// THE THREE STATES. A boolean cannot say "not applicable", so the verdict is read together with
    /// the evidence record: <c>MeasuredTerms.RetentionTime</c> set with <c>IsRtMatch</c> true means
    /// the values agreed, set with false means they disagreed, and clear means no comparison was
    /// possible. Only the middle case may withhold a verdict.
    ///
    /// SCOPE. Retention time only. Collision cross section has the same shape -- an unguarded
    /// Gaussian and a <c>Math.Abs</c> verdict against <c>CollisionCrossSection</c> -- and is
    /// deliberately left alone here. GC-MS needs no change: <c>CompareBasicMSScanProperties</c>, the
    /// single funnel for every EI comparison, already calls the guarded four-argument overload for
    /// both retention time and retention index.
    ///
    /// Confirmed as unintended by the author of MS-DIAL, 2026-09-10.
    /// </remarks>
    public static class RetentionMatchPolicy
    {
        /// <summary>
        /// Whether a comparison was possible at all: both sides must carry a positive value. This is
        /// the same test the guarded <c>GetGaussianSimilarity</c> overloads apply before they will
        /// score, and the same one <c>MeasuredTermsExtension.WithComparedValues</c> applies before it
        /// will record a term.
        /// </summary>
        public static bool CanCompare(double actual, double reference) {
            return actual > 0d && reference > 0d;
        }

        /// <summary>
        /// The retention-time requirement for a match verdict, from the values themselves. Satisfied
        /// when the run does not score retention time, when no comparison was possible, or when the
        /// values agreed.
        /// </summary>
        public static bool RetentionTimeRequirementMet(bool isRetentionTimeUsed, double actual, double reference, bool isRtMatch) {
            return !isRetentionTimeUsed || !CanCompare(actual, reference) || isRtMatch;
        }

        /// <summary>
        /// The same requirement, for callers that hold only the match result. Used where the verdict
        /// is re-derived after scoring and the peak and reference are no longer in scope; the record
        /// carries the answer because the scorer wrote it from those same two values.
        /// </summary>
        public static bool RetentionTimeRequirementMet(bool isRetentionTimeUsed, MsScanMatchResult result) {
            return !isRetentionTimeUsed
                || !result.MeasuredTerms.HasFlag(MeasuredTerms.RetentionTime)
                || result.IsRtMatch;
        }
    }
}
