using CompMs.Common.DataObj.Result;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Export
{
    /// <summary>
    /// Which peaks and spots become MassBank records, the same for the alignment export and the peak list export.
    /// </summary>
    /// <remarks>
    /// By default only reference-matched annotations (an accepted MSP, LBM or text library match, not a suggestion) are
    /// exported; suggestions and unknowns are opt-in. The match flags decide first. A spot set to Unsettled by hand, or
    /// renamed "Putative:" because another spot kept the same reference, still carries the reference-matched flag, so
    /// the name tells those from an accepted match. A named peak that is neither matched nor suggested is not taken as an
    /// identification.
    /// </remarks>
    internal static class MassBankExportPolicy
    {
        internal const string SkippedSuggested = "suggested annotations (exported only with \"Include suggested annotations\")";
        internal const string SkippedUnknown = "unknowns (exported only with \"Include unknowns\")";
        internal const string SkippedNoSpectrum = "without a deconvoluted MS/MS spectrum";
        internal const string SkippedNoPeaks = "without MS/MS peaks";

        public static MassBankAnnotationLevel Classify(AlignmentSpotProperty spot, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            return Classify(spot.IsReferenceMatched(evaluator), spot.IsAnnotationSuggested(evaluator), spot.IsUnknown, spot.Name);
        }

        public static MassBankAnnotationLevel Classify(ChromatogramPeakFeature feature, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            return Classify(feature.IsReferenceMatched(evaluator), feature.IsAnnotationSuggested(evaluator), feature.IsUnknown, feature.Name);
        }

        public static MassBankAnnotationLevel Classify(bool isReferenceMatched, bool isAnnotationSuggested, bool isUnknown, string? name) {
            if (isReferenceMatched) {
                return MassBankRecordHandler.IsSuggestionName(name ?? string.Empty) ? MassBankAnnotationLevel.Suggested : MassBankAnnotationLevel.ReferenceMatched;
            }
            if (isAnnotationSuggested) {
                return MassBankAnnotationLevel.Suggested;
            }
            if (isUnknown || string.IsNullOrWhiteSpace(name)) {
                return MassBankAnnotationLevel.Unknown;
            }
            return MassBankAnnotationLevel.Suggested;
        }

        /// <summary>Why a peak or spot of <paramref name="level"/> is not exported with these options; null when it is.</summary>
        public static string? Exclusion(MassBankAnnotationLevel level, bool includeSuggested, bool includeUnknowns) {
            if (level == MassBankAnnotationLevel.Suggested && !includeSuggested) {
                return SkippedSuggested;
            }
            if (level == MassBankAnnotationLevel.Unknown && !includeUnknowns) {
                return SkippedUnknown;
            }
            return null;
        }
    }
}
