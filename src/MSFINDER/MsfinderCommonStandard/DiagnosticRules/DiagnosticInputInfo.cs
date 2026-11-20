namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// What the adapter did to turn a query into the evaluator's input (design 5.4). Reported in the rule result
    /// file; none of it is part of the canonical result.
    /// </summary>
    public sealed class DiagnosticInputInfo
    {
        /// <summary><c>peaks_source</c>: the query's centroid peaks as read.</summary>
        public const string PeaksAsRead = "as_read";
        /// <summary><c>peaks_source</c>: a profile query, centroided by MS-FINDER's own centroiding.</summary>
        public const string PeaksMsfinderCentroid = "msfinder_centroid";

        /// <summary><c>instrument_type_source</c>: INSTRUMENTTYPE (MSP/MAT) or AC$INSTRUMENT_TYPE (MassBank).</summary>
        public const string InstrumentTypeFromQuery = "query";
        /// <summary><c>instrument_type_source</c>: the DiagnosticRuleInstrumentType setting.</summary>
        public const string InstrumentTypeFromOption = "option";
        /// <summary><c>instrument_type_source</c>: neither; the instrument class is <c>unknown</c>.</summary>
        public const string InstrumentTypeNone = "none";

        /// <summary>The peak count differs from the declared one (Num Peaks, PK$NUM_PEAK).</summary>
        public const string PeakCountMismatch = "peak_count_mismatch";
        /// <summary>A peak below 1.0 Da.</summary>
        public const string Sub1DaPeak = "sub_1da_peak";
        /// <summary>The strict re-read of the MS2 peak block differs from the MS-FINDER reader's peaks.</summary>
        public const string PeakBlockDisagrees = "peak_block_disagrees";

        /// <summary>The row status prefix of a query whose peaks a guard refused.</summary>
        public const string PeaksSuspectPrefix = "skipped:peaks_suspect:";

        public DiagnosticInputInfo(string peaksSource, string instrumentTypeSource, int nonPositiveDropped, string peaksSuspect) {
            PeaksSource = peaksSource;
            InstrumentTypeSource = instrumentTypeSource;
            NonPositiveDropped = nonPositiveDropped;
            PeaksSuspect = peaksSuspect;
        }

        /// <summary><see cref="PeaksAsRead"/> or <see cref="PeaksMsfinderCentroid"/>.</summary>
        public string PeaksSource { get; }

        /// <summary><see cref="InstrumentTypeFromQuery"/>, <see cref="InstrumentTypeFromOption"/> or
        /// <see cref="InstrumentTypeNone"/>.</summary>
        public string InstrumentTypeSource { get; }

        /// <summary><c>n_peaks_nonpositive_dropped</c>: peaks with an intensity that is not positive, dropped before
        /// indexing and hashing.</summary>
        public int NonPositiveDropped { get; }

        /// <summary>The guard that refused the peaks (<see cref="PeakCountMismatch"/>, <see cref="Sub1DaPeak"/> or
        /// <see cref="PeakBlockDisagrees"/>), or null when every guard passed.</summary>
        public string PeaksSuspect { get; }

        /// <summary><c>skipped:peaks_suspect:&lt;reason&gt;</c>, or null when every guard passed.</summary>
        public string SkipStatus => PeaksSuspect == null ? null : PeaksSuspectPrefix + PeaksSuspect;
    }
}
