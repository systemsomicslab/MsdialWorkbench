using CompMs.Common.Enum;

namespace CompMs.App.Msdial.Model.Setting
{
    /// <summary>
    /// The RI tolerance to offer for a retention-index scale, because one number cannot serve both.
    /// </summary>
    /// <remarks>
    /// <see cref="CompMs.Common.Parameter.MsRefSearchParameterBase.RiTolerance"/> is a single field
    /// used on two scales that are not the same size.
    ///
    /// Kovats (alkane) units run 100 per carbon, so the GC-MS default of 20 -- seeded in
    /// DatasetParameterSettingModel and kept here -- is a fifth of a carbon, a sensible window.
    ///
    /// The Fiehn (FAME) scale is FAME retention in MILLISECONDS.
    /// <c>RetentionIndexHandler.GetFiehnFamesDictionary</c> runs from 262320 at C8 to 1113100 at C30,
    /// and the MSP parser converts a library value back with <c>ri * 0.001 / 60</c>, so one carbon is
    /// near 39,350 units there against Kovats' 100 -- a factor of about 390. Carrying 20 onto that
    /// scale asks for agreement within twenty milliseconds of FAME retention, which no candidate can
    /// meet: the search window retrieves nothing, and the Gaussian that takes the same number as its
    /// width would score at zero whatever it did retrieve. 20000 Fiehn units is about half a carbon,
    /// the same order as 20 Kovats units.
    ///
    /// THIS IS THE SEARCH WINDOW, NOT THE MATCH VERDICT.
    /// <see cref="CompMs.Common.Algorithm.Scoring.RetentionMatchPolicy"/> bounds the verdict
    /// separately -- 150 Kovats units, 50000 Fiehn units -- and the two answer different questions:
    /// which references are worth scoring, and whether the retention indices actually agree.
    ///
    /// Requested by the author of MS-DIAL, 2026-09-10, as a GUI-side default.
    /// </remarks>
    internal static class RetentionIndexToleranceDefault
    {
        /// <summary>The default RI tolerance on the Kovats (alkane) scale, in Kovats units.</summary>
        public const float Kovats = 20f;

        /// <summary>The default RI tolerance on the Fiehn (FAME) scale, in Fiehn units.</summary>
        public const float Fiehn = 20000f;

        /// <summary>The default RI tolerance for the scale in use.</summary>
        public static float For(RiCompoundType riCompoundType) {
            return riCompoundType == RiCompoundType.Fames ? Fiehn : Kovats;
        }

        /// <summary>
        /// Whether a tolerance is still the untouched default for the scale it was shown on. A value
        /// that is not is the analyst's own, and is never replaced.
        /// </summary>
        public static bool IsDefaultFor(float tolerance, RiCompoundType riCompoundType) {
            return tolerance == For(riCompoundType);
        }
    }
}
