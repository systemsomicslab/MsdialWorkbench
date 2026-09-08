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

        private static readonly string[] NOT_REFERENCE_MATCHED_PREFIXES = {
            UnknownPrefix,
            NullPrefix,
            EmptyPrefix,
            NoMs2Prefix,
            LowScorePrefix,
            WITHOUT_MS2_MATCH_PREFIX,
            RikenPrefix,
        };

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
