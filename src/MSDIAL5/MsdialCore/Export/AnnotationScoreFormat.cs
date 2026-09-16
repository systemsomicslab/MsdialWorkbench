using CompMs.Common.DataObj.Result;
using System;

namespace CompMs.MsdialCore.Export
{
    /// <summary>
    /// One representation of the annotation score columns shared by the analysis (.mdpeak) and the
    /// alignment (.mdalign) text exports.
    /// </summary>
    /// <remarks>
    /// A score column is written as "null" only when the score was never computed, and as its value
    /// otherwise, including an exact 0.
    ///
    /// The distinction matters scientifically. "Compared against the reference and scored zero" is a
    /// weaker claim than "no comparison was possible", and only the first is a measurement. A
    /// precursor-only suggestion (named "no MS2: " because MS2RawSpectrumID is negative) had no
    /// product-ion spectrum to compare, and a text-database annotation has no reference spectrum to
    /// compare against, so neither ever produced a spectral score. A real search that found no
    /// overlapping fragment does produce one, and it is 0.
    ///
    /// Before this was centralised the two exporters disagreed. The analysis accessor bound the
    /// nullable ValueOrNull overload, so it printed 0.000 unless the whole match result was null, and
    /// the alignment accessor bound a float overload that printed "null" for every value within 1e-10
    /// of zero. So .mdpeak reported a score for a comparison that never happened, and .mdalign
    /// discarded a genuine zero. Both columns now come from the same decision.
    /// </remarks>
    public static class AnnotationScoreFormat
    {
        /// <summary>
        /// The text written for a score that was never computed. Matches the other "not available"
        /// columns of both formats.
        /// </summary>
        public const string NotComputed = "null";

        /// <summary>
        /// Formats one annotation score column.
        /// </summary>
        /// <param name="result">The representative match result of the peak or spot, or null when there is none.</param>
        /// <param name="hadProductIonSpectrum">
        /// Whether a product-ion spectrum existed for the exported peak or spot at all, which is what the
        /// "no MS2: " name prefix reports. It is needed because DimsMspAnnotator scores through
        /// Ms2MatchCalculator, which collapses the -1 sentinel to Ms2MatchResult.Empty before it reaches
        /// the match result. This is only consulted when the whole score block is still at its unset
        /// default, so it can never discard a score that was genuinely computed.
        /// </param>
        /// <param name="value">
        /// Reads the score from <paramref name="result"/>. Only called when the score exists. The
        /// stored fields are Single, and the selector widens to double on purpose: .NET Framework
        /// formats a Single through a 7-significant-digit intermediate, so a value just below a
        /// half-way point, 0.59349996 for instance, snaps to 0.5935 and then "F3" rounds it up to
        /// 0.594 instead of down to 0.593.
        /// </param>
        /// <param name="format">A numeric format string, for example "F3".</param>
        public static string Score(MsScanMatchResult? result, bool hadProductIonSpectrum, Func<MsScanMatchResult, double> value, string format) {
            if (!IsComputed(result, hadProductIonSpectrum)) {
                return NotComputed;
            }
            return value(result).ToString(format);
        }

        /// <summary>
        /// Whether the spectral score fields of <paramref name="result"/> hold measurements.
        /// </summary>
        /// <remarks>
        /// Exposed separately because the audit sidecars render an absent value as an empty cell rather
        /// than as the text "null" that the .mdpeak and .mdalign columns use. The decision of whether a
        /// score exists must not fork with the rendering, so both go through this.
        /// </remarks>
        public static bool IsComputed(MsScanMatchResult? result, bool hadProductIonSpectrum) {
            if (result is null || !result.IsSpectrumComparisonPerformed) {
                return false;
            }
            return hadProductIonSpectrum || !IsScoreBlockUnset(result);
        }

        /// <summary>
        /// True when every spectral score field still holds the default 0, so the block carries no
        /// evidence that a comparison happened. A real comparison can also produce all zeros, which is
        /// why this is only trusted together with the absence of a product-ion spectrum.
        /// </summary>
        private static bool IsScoreBlockUnset(MsScanMatchResult result) {
            return result.SquaredSimpleDotProduct == 0f
                && result.SquaredWeightedDotProduct == 0f
                && result.SquaredReverseDotProduct == 0f
                && result.MatchedPeaksCount == 0f
                && result.MatchedPeaksPercentage == 0f;
        }
    }
}
