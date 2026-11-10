using System;

namespace CompMs.Common.Utility
{
    /// <summary>
    /// Owns the prefixes MS-DIAL writes into a feature name when an annotation was
    /// not accepted as a reference match, and the predicate that recognizes them.
    /// </summary>
    /// <remarks>
    /// The suggestion prefixes are written by
    /// <c>DataAccess.SetMoleculeMsPropertyAsSuggested</c> and
    /// <c>DataAccess.SetPeptideMsPropertyAsSuggested</c> in MsdialCore. Both are
    /// reached only from the annotation branches where
    /// <c>MsScanMatchResult.IsReferenceMatched</c> is false, so every prefixed name is
    /// a suggestion rather than an accepted identification.
    ///
    /// Writers and the predicate live together here so a renamed prefix cannot leave a
    /// reader stale. MS-DIAL 4 wrote a single "w/o MS2:" prefix for its precursor-only
    /// suggestions; MS-DIAL 5 splits that bucket into <see cref="NoMs2Prefix"/> and
    /// <see cref="LowScorePrefix"/>, and a reader that knows only the MS-DIAL 4
    /// spelling silently treats both MS-DIAL 5 shapes as characterized.
    ///
    /// This type lives in CommonStandard because the vocabulary is shared by
    /// CommonStandard (molecular networking) and MsdialCore (export), and the project
    /// dependency runs MsdialCore -> CommonStandard only.
    /// </remarks>
    public static class AnnotationName
    {
        /// <summary>Separator between a prefix and the reference name.</summary>
        public const string PrefixSeparator = ": ";

        /// <summary>
        /// MS-DIAL 5 prefix for a precursor-only suggestion: the feature has no
        /// product-ion spectrum at all (MS2RawSpectrumID &lt; 0).
        /// </summary>
        public const string NoMs2Prefix = "no MS2";

        /// <summary>
        /// MS-DIAL 5 prefix for a suggestion that does have a product-ion spectrum but
        /// did not meet the reference-search acceptance criteria.
        /// </summary>
        public const string LowScorePrefix = "low score";

        /// <summary>
        /// Prefix written by the MS-DIAL 5 peptide suggestion path, and by every
        /// MS-DIAL 4 precursor-only suggestion.
        /// </summary>
        public const string WithoutMs2Prefix = "w/o MS2";

        /// <summary>Prefix of an unannotated feature.</summary>
        public const string UnknownPrefix = "Unknown";

        /// <summary>Placeholder written when a reference field is absent.</summary>
        public const string NullPrefix = "null";

        /// <summary>Placeholder written when a reference field is present but empty.</summary>
        public const string EmptyPrefix = "empty";

        /// <summary>Prefix of an in-house RIKEN placeholder record.</summary>
        public const string RikenPrefix = "RIKEN";

        /// <summary>
        /// Matched instead of <see cref="WithoutMs2Prefix"/> so that the MS-DIAL 4
        /// "w/o MS2:" spelling, which has no space before the colon, is also covered.
        /// </summary>
        private const string WITHOUT_MS2_MATCH_PREFIX = "w/o";

        /// <summary>The prefixes that <see cref="WithoutPrefix"/> will strip, longest spelling first.</summary>
        private static readonly string[] SUGGESTION_PREFIXES = {
            NoMs2Prefix,
            LowScorePrefix,
            WithoutMs2Prefix,
        };

        private static readonly string[] NOT_REFERENCE_MATCHED_PREFIXES = {
            UnknownPrefix,
            NullPrefix,
            EmptyPrefix,
            NoMs2Prefix,
            LowScorePrefix,
            WITHOUT_MS2_MATCH_PREFIX,
            RikenPrefix,
        };

        /// <summary>
        /// Separator between the class-level and the chain-resolved form of a lipid name, as in
        /// "PC 34:1|PC 16:0_18:1".
        /// </summary>
        /// <remarks>
        /// Here with the prefixes for the same reason they are here: this is part of the vocabulary
        /// MS-DIAL writes into a name, and a reader that splits on it by hand goes stale when the
        /// writer changes. Note that the two halves are NOT two candidates -- they are one
        /// annotation stated at two levels of structural resolution, and which one the evidence
        /// supports is a question for <see cref="AtSupportedLevel"/>, not for the string.
        /// </remarks>
        public const char DualNameSeparator = '|';

        /// <summary>
        /// The reference name with any suggestion prefix removed, for a field that must hold a
        /// compound name and nothing else.
        /// </summary>
        /// <remarks>
        /// Tolerates both spellings of the separator: MS-DIAL 5 writes ": " and MS-DIAL 4 wrote ":".
        /// A name with no prefix comes back unchanged, including "Unknown", which is not a prefix on
        /// a name but the whole of one.
        /// </remarks>
        public static string WithoutPrefix(string name) {
            if (string.IsNullOrEmpty(name)) {
                return name;
            }
            var value = name.TrimStart();
            foreach (var prefix in SUGGESTION_PREFIXES) {
                if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                var rest = value.Substring(prefix.Length);
                if (!rest.StartsWith(":", StringComparison.Ordinal)) {
                    continue;
                }
                return rest.Substring(1).TrimStart();
            }
            return name;
        }

        /// <summary>
        /// The half of a dual lipid name that the evidence actually supports: the chain-resolved
        /// form when the characteristic-ion rules resolved the chains, and the class-level form
        /// when they did not.
        /// </summary>
        /// <remarks>
        /// READ THE RECORD, NOT THE STRING, because the pipe means opposite things in the two
        /// places that write one. MsScanMatching.GetRefinedLipidAnnotationLevel emits
        /// "class|chains" only at annotation level 2 or above -- that is, only when the chains WERE
        /// resolved -- and returns the bare class name at level 1. MsReferenceScorer, by contrast,
        /// builds "class|chains" inside its <c>if (!result.IsSpectrumMatch)</c> branch, where the
        /// chain-level half is simply copied off the reference record and nothing supports it.
        /// So the same punctuation marks a resolved chain assignment in one path and an unsupported
        /// one in the other, and no rule phrased on the text can tell them apart.
        ///
        /// IsLipidChainsMatch can, and it is set from the same call that produces the name
        /// (MsScanMatching.cs, where GetRefinedLipidAnnotationLevel's out parameter and the name go
        /// onto the result together), so the two cannot disagree.
        ///
        /// This is the author's rule of 2026-09-10: a lipid identified on precursor mass without a
        /// product-ion spectrum should be reported as "PC 34:1", not as the reference's
        /// "PC 16:0_18:1", because nothing measured the chains.
        /// </remarks>
        public static string AtSupportedLevel(string name, bool chainsResolved) {
            if (string.IsNullOrEmpty(name)) {
                return name;
            }
            var separator = name.IndexOf(DualNameSeparator);
            if (separator < 0) {
                return name;
            }
            return chainsResolved
                ? name.Substring(separator + 1)
                : name.Substring(0, separator);
        }

        /// <summary>
        /// THE COMPOUND NAME EVERY EXPORT SHOWS: the processing status removed, and the structural
        /// level reduced to what the evidence supports.
        /// </summary>
        /// <remarks>
        /// One function because the four outputs of a run have to agree. The peak table, the
        /// alignment table, mzTab-M and the exported spectra are joined by peak ID -- that is the
        /// contract -- but the field a reader looks at first is the name, and a name that reads
        /// "low score: Quercetin" in one file and "Quercetin" in another invites the conclusion that
        /// they are different rows.
        ///
        /// What the prefix used to carry now travels as its own field, where it says more than the
        /// prefix could: "Evidence source" and "Measured terms" in the tables,
        /// opt_global_evidence_source in mzTab-M, EVIDENCE= in an exported spectrum's COMMENT. The
        /// prefix collapsed "a spectrum was compared and fell short" and "a spectrum was compared
        /// and explained nothing" into one word, and could not say which library was searched at all.
        ///
        /// Idempotent, so a caller that receives an already-canonical name may apply it again --
        /// which mzTab-M does, reading a name the metadata accessor has already cleaned.
        /// </remarks>
        public static string Canonical(string name, bool chainsResolved) {
            return AtSupportedLevel(WithoutPrefix(name), chainsResolved);
        }

        /// <summary>
        /// True when the name says no product-ion spectrum was acquired for the feature, under
        /// either the MS-DIAL 5 or the MS-DIAL 4 spelling.
        /// </summary>
        /// <remarks>
        /// A prefix test rather than a substring test, and it covers the peptide path's "w/o MS2"
        /// as well as "no MS2". Deliberately does NOT cover "low score", which is the opposite
        /// finding: there a spectrum was acquired and compared.
        /// </remarks>
        public static bool IsPrecursorOnlySuggestion(string name) {
            if (string.IsNullOrEmpty(name)) {
                return false;
            }
            var value = name.TrimStart();
            return value.StartsWith(NoMs2Prefix, StringComparison.OrdinalIgnoreCase)
                || value.StartsWith(WithoutMs2Prefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Builds the name of a precursor-only suggestion.</summary>
        public static string AsNoMs2(string referenceName) {
            return NoMs2Prefix + PrefixSeparator + referenceName;
        }

        /// <summary>Builds the name of a suggestion that failed the search criteria.</summary>
        public static string AsLowScore(string referenceName) {
            return LowScorePrefix + PrefixSeparator + referenceName;
        }

        /// <summary>Builds the name of a peptide suggestion without product-ion evidence.</summary>
        public static string AsWithoutMs2(string referenceName) {
            return WithoutMs2Prefix + PrefixSeparator + referenceName;
        }

        /// <summary>
        /// Returns true when <paramref name="name"/> reads as an accepted reference
        /// match rather than an unannotated feature or a suggestion.
        /// </summary>
        /// <remarks>
        /// This is a predicate on the exported name only. Where the authoritative
        /// <c>MsScanMatchResult</c> is available, evaluate that instead.
        /// </remarks>
        public static bool IsReferenceMatched(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return false;
            }
            var value = name.TrimStart();
            foreach (var prefix in NOT_REFERENCE_MATCHED_PREFIXES) {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                    return false;
                }
            }
            return true;
        }
    }
}
