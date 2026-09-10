using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using System;

namespace CompMs.Common.Algorithm.Scoring
{
    /// <summary>
    /// What it takes for a retention comparison to count as a match, in one place.
    /// </summary>
    /// <remarks>
    /// TWO RULES LIVE HERE, and they answer different questions.
    ///
    /// (1) A reference that carries no retention time neither gains nor loses by retention time.
    /// A MoleculeMsReference with no retention time holds ChromX.RetentionTime.Default, whose value
    /// is -1, and two places used to treat that -1 as a measurement: the three-argument
    /// GetGaussianSimilarity, which evaluates exp(-0.5 * ((actual - reference) / tolerance)^2) on
    /// whatever it is handed, and a plain Math.Abs comparison in each annotator's ValidateBase. An
    /// early-eluting peak therefore agreed, on retention time, with a reference that had none -- and
    /// agreed more strongly the earlier it eluted.
    ///
    /// The damage was not primarily the total score. DataAccess.GetAnnotationCode reads IsRtMatch and
    /// nothing else to promote 430 ("m/z + MS/MS matched") to 330 ("RT + MS/MS matched"), and 440 to
    /// 340 on the GC-MS branch, so the fabricated agreement published a claim of retention-time
    /// confirmation in the "Annotation tag (VS1.0)" column of every export.
    ///
    /// The rule is an EXEMPTION, not a rejection. IsReferenceMatched and IsAnnotationSuggested share
    /// the retention clause, MsScanMatchResultEvaluator.FilterByThreshold is their disjunction, and
    /// StandardAnnotationProcess stores only what that returns -- so failing such a candidate would
    /// delete it from the output rather than demote it, taking its m/z and MS/MS evidence with it.
    /// A boolean cannot say "not applicable", so the verdict is read together with the evidence
    /// record: MeasuredTerms.RetentionTime set with IsRtMatch true means the values agreed, set with
    /// false means they disagreed, and clear means no comparison was possible. Only the middle case
    /// may withhold a verdict.
    ///
    /// (2) A search tolerance is not a match criterion. The tolerance says which references are worth
    /// scoring; the verdict says whether the retention times actually agree. Searching a
    /// predicted-retention-time library loosely is reasonable, and publishing "retention time
    /// matched" on the strength of that search is not. So the verdict is capped and the score is not:
    /// the Gaussian keeps the user's tolerance as its width, which is what stops the cap from
    /// re-ranking existing projects.
    ///
    /// Confirmed with the author of MS-DIAL, 2026-09-10.
    ///
    /// SCOPE. Retention time and retention index. Collision cross section has the same shape -- an
    /// unguarded Gaussian and a Math.Abs verdict -- and is deliberately left alone. GC-MS needed no
    /// change for rule (1): CompareBasicMSScanProperties, the single funnel for every EI comparison,
    /// already calls the guarded four-argument overload for both axes.
    /// </remarks>
    public static class RetentionMatchPolicy
    {
        /// <summary>
        /// The widest retention-time difference that may be called a match, in minutes, however wide
        /// the user's tolerance is. The default RtTolerance is 100 minutes, which is a search window
        /// and not a claim about agreement.
        /// </summary>
        public const double RetentionTimeMatchCapMinutes = 2d;

        /// <summary>
        /// The same bound in Kovats (alkane) retention-index units. One carbon is 100 units on that
        /// scale and roughly 1.1 to 1.3 minutes on a typical column, so 150 units is about the same
        /// two minutes.
        /// </summary>
        public const double KovatsRetentionIndexMatchCap = 150d;

        /// <summary>
        /// The same bound in Fiehn (FAME) retention-index units. That scale is FAME retention in
        /// milliseconds -- RetentionIndexHandler.GetFiehnFamesDictionary runs from 262320 at C8 to
        /// 1113100 at C30, and the MSP parser converts it with <c>ri * 0.001 / 60</c> -- so one carbon
        /// is near 39,350 units there against Kovats' 100, and 50,000 units is the same order as the
        /// 150 Kovats units above.
        /// </summary>
        public const double FiehnRetentionIndexMatchCap = 50000d;

        /// <summary>
        /// Whether a comparison was possible at all: both sides must carry a positive value. This is
        /// the same test the guarded <c>GetGaussianSimilarity</c> overloads apply before they will
        /// score, and the same one <c>MeasuredTermsExtension.WithComparedValues</c> applies before it
        /// will record a term.
        /// </summary>
        public static bool CanCompare(double actual, double reference) {
            return actual > 0d && reference > 0d;
        }

        /// <summary>The user's retention-time tolerance, capped for verdict purposes.</summary>
        public static double EffectiveRetentionTimeTolerance(double userTolerance) {
            return Math.Min(userTolerance, RetentionTimeMatchCapMinutes);
        }

        /// <summary>The user's retention-index tolerance, capped on the scale actually in use.</summary>
        public static double EffectiveRetentionIndexTolerance(double userTolerance, RiCompoundType riCompoundType) {
            var cap = riCompoundType == RiCompoundType.Fames
                ? FiehnRetentionIndexMatchCap
                : KovatsRetentionIndexMatchCap;
            return Math.Min(userTolerance, cap);
        }

        /// <summary>
        /// The retention-time verdict: a comparison must have been possible, and the values must
        /// agree within the capped tolerance.
        /// </summary>
        public static bool IsRetentionTimeMatch(double actual, double reference, double userTolerance) {
            return CanCompare(actual, reference)
                && Math.Abs(actual - reference) <= EffectiveRetentionTimeTolerance(userTolerance);
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
