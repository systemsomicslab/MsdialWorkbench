namespace CompMs.Common.StructureFinder.DiagnosticRules.Results
{
    /// <summary>
    /// A veto term (veto_present or veto_stronger_than) of one hypothesis (design 4.7, 4.11): whether its condition
    /// holds, I_raw of the term and the largest I_raw of its than-terms. The design lists a blocking veto "with its peak
    /// and I_raw" (4.7), so the peak that gives <see cref="IA"/> is reported too: the first hit with that relative
    /// intensity, in member order then m/z order, or none when nothing hits.
    /// </summary>
    public sealed class VetoOutcome
    {
        internal VetoOutcome(string termId, bool fired, double iA, double iBMax, int? peakIndex, double? peakMz) {
            TermId = termId;
            Fired = fired;
            IA = iA;
            IBMax = iBMax;
            PeakIndex = peakIndex;
            PeakMz = peakMz;
        }

        public string TermId { get; }

        /// <summary>The veto condition holds, so the not node above it is false.</summary>
        public bool Fired { get; }

        /// <summary>I_raw of the term: the largest relative intensity of its hits, floors ignored; 0 when none.</summary>
        public double IA { get; }

        /// <summary>The largest I_raw over the than-terms; 0 for veto_present, which has none.</summary>
        public double IBMax { get; }

        /// <summary>The input index of the peak that gives <see cref="IA"/>, or null when nothing hits.</summary>
        public int? PeakIndex { get; }

        /// <summary>The m/z of that peak, or null.</summary>
        public double? PeakMz { get; }
    }
}
