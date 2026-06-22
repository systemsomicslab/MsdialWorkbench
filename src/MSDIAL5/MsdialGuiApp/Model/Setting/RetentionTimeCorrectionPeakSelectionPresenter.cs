using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting {
    /// <summary>
    /// Builds UI-friendly rows from RT correction peak selection results.
    /// </summary>
    public static class RetentionTimeCorrectionPeakSelectionPresenter {
        /// <summary>
        /// Converts the current common standard list into a flat summary list for the RT correction window.
        /// </summary>
        public static IReadOnlyList<RetentionTimeCorrectionPeakSelectionRow> CreateRows(IReadOnlyList<CommonStdData> commonStdList) {
            if (commonStdList is null || commonStdList.Count == 0) {
                return Array.Empty<RetentionTimeCorrectionPeakSelectionRow>();
            }

            return commonStdList.Select((std, index) => new RetentionTimeCorrectionPeakSelectionRow(std, index)).ToArray();
        }
    }

    /// <summary>
    /// Represents a single row in the RT correction peak selection summary table.
    /// </summary>
    public sealed class RetentionTimeCorrectionPeakSelectionRow {
        /// <summary>
        /// Initializes a new summary row from a common standard entry.
        /// </summary>
        public RetentionTimeCorrectionPeakSelectionRow(CommonStdData standard, int index) {
            StandardIndex = index;
            StandardName = standard.Reference?.Name ?? string.Empty;
            ReferenceId = standard.Reference?.ScanID ?? 0;
            ReferenceMz = standard.Reference?.PrecursorMz ?? 0d;
            ReferenceRt = standard.Reference?.ChromXs?.Value ?? 0d;
            SelectedReason = standard.PeakSelectionResult?.SelectedReason.ToString() ?? "N/A";
            CandidateCount = standard.PeakSelectionResult?.Candidates.Count ?? 0;
            RejectedCount = standard.PeakSelectionResult?.RejectedCandidates.Count ?? 0;
            RejectedReasons = standard.PeakSelectionResult is null
                ? "N/A"
                : string.Join(", ", standard.PeakSelectionResult.RejectedCandidates
                    .Select(candidate => candidate.RejectReason.ToString())
                    .Distinct());

            var selectedPeak = standard.PeakSelectionResult?.SelectedPeak;
            SelectedMz = selectedPeak?.PrecursorMz ?? 0d;
            SelectedRt = selectedPeak?.ChromXsTop?.Value ?? 0d;
            SelectedPeakHeight = selectedPeak?.PeakHeightTop ?? 0d;
            HasSelection = selectedPeak is not null;
        }

        /// <summary>
        /// Gets the zero-based index of the standard in the current list.
        /// </summary>
        public int StandardIndex { get; }
        /// <summary>
        /// Gets the reference ID.
        /// </summary>
        public int ReferenceId { get; }
        /// <summary>
        /// Gets the reference name.
        /// </summary>
        public string StandardName { get; }
        /// <summary>
        /// Gets the reference m/z.
        /// </summary>
        public double ReferenceMz { get; }
        /// <summary>
        /// Gets the reference RT.
        /// </summary>
        public double ReferenceRt { get; }
        /// <summary>
        /// Gets the RT correction selection reason.
        /// </summary>
        public string SelectedReason { get; }
        /// <summary>
        /// Gets the selected peak m/z, or zero when no selection exists.
        /// </summary>
        public double SelectedMz { get; }
        /// <summary>
        /// Gets the selected peak RT, or zero when no selection exists.
        /// </summary>
        public double SelectedRt { get; }
        /// <summary>
        /// Gets the selected peak height, or zero when no selection exists.
        /// </summary>
        public double SelectedPeakHeight { get; }
        /// <summary>
        /// Gets the number of evaluated candidates.
        /// </summary>
        public int CandidateCount { get; }
        /// <summary>
        /// Gets the number of rejected candidates.
        /// </summary>
        public int RejectedCount { get; }
        /// <summary>
        /// Gets the aggregated rejection reasons for the rejected candidates.
        /// </summary>
        public string RejectedReasons { get; }
        /// <summary>
        /// Gets a value indicating whether a peak was selected.
        /// </summary>
        public bool HasSelection { get; }
    }
}
