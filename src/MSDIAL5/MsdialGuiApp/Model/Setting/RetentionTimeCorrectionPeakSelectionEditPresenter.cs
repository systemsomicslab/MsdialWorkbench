using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting {
    /// <summary>
    /// Builds editable RT override rows for RT correction peak selection.
    /// </summary>
    public static class RetentionTimeCorrectionPeakSelectionEditPresenter {
        /// <summary>
        /// Converts analysis files into editable per-sample, per-standard RT override rows.
        /// </summary>
        public static IReadOnlyList<RetentionTimeCorrectionPeakSelectionEditRow> CreateRows(
            IReadOnlyList<AnalysisFileBean> analysisFiles,
            Action<int, int, double> applyManualRt) {
            if (analysisFiles is null || analysisFiles.Count == 0) {
                return Array.Empty<RetentionTimeCorrectionPeakSelectionEditRow>();
            }

            var rows = new List<RetentionTimeCorrectionPeakSelectionEditRow>();
            for (var sampleIndex = 0; sampleIndex < analysisFiles.Count; sampleIndex++) {
                var analysisFile = analysisFiles[sampleIndex];
                var standardList = analysisFile.RetentionTimeCorrectionBean?.StandardList;
                if (standardList is null || standardList.Count == 0) {
                    continue;
                }

                for (var standardIndex = 0; standardIndex < standardList.Count; standardIndex++) {
                    rows.Add(new RetentionTimeCorrectionPeakSelectionEditRow(
                        analysisFile,
                        sampleIndex,
                        standardList[standardIndex],
                        standardIndex,
                        applyManualRt));
                }
            }

            return rows;
        }
    }

    /// <summary>
    /// Represents a single editable RT override row for a sample-standard pair.
    /// </summary>
    public sealed class RetentionTimeCorrectionPeakSelectionEditRow : ViewModelBase {
        private readonly Action<int, int, double> applyManualRt;
        private double manualRt;

        /// <summary>
        /// Initializes a new editable row.
        /// </summary>
        public RetentionTimeCorrectionPeakSelectionEditRow(
            AnalysisFileBean analysisFile,
            int sampleIndex,
            StandardPair standard,
            int standardIndex,
            Action<int, int, double> applyManualRt) {
            this.applyManualRt = applyManualRt ?? throw new ArgumentNullException(nameof(applyManualRt));
            SampleIndex = sampleIndex;
            StandardIndex = standardIndex;
            SampleName = analysisFile.AnalysisFileName ?? string.Empty;
            StandardName = standard.Reference?.Name ?? string.Empty;
            ReferenceId = standard.Reference?.ScanID ?? 0;
            ReferenceMz = standard.Reference?.PrecursorMz ?? 0d;
            ReferenceRt = standard.Reference?.ChromXs?.Value ?? 0d;
            CurrentRt = standard.SamplePeakAreaBean?.ChromXsTop?.Value ?? 0d;
            manualRt = CurrentRt;

            SelectedReason = standard.PeakSelectionResult?.SelectedReason.ToString() ?? "N/A";
            CandidateCount = standard.PeakSelectionResult?.Candidates.Count ?? 0;
            RejectedCount = standard.PeakSelectionResult?.RejectedCandidates.Count ?? 0;
            RejectedReasons = standard.PeakSelectionResult is null
                ? "N/A"
                : string.Join(", ", standard.PeakSelectionResult.RejectedCandidates
                    .Select(candidate => candidate.RejectReason.ToString())
                    .Distinct());

            var selectedPeak = standard.PeakSelectionResult?.SelectedPeak;
            DetectedRt = selectedPeak?.ChromXsTop?.Value ?? 0d;
            DetectedMz = selectedPeak?.PrecursorMz ?? 0d;
            DetectedPeakHeight = selectedPeak?.PeakHeightTop ?? 0d;
            HasDetectedPeak = selectedPeak is not null;
        }

        /// <summary>
        /// Gets the zero-based sample index.
        /// </summary>
        public int SampleIndex { get; }
        /// <summary>
        /// Gets the zero-based standard index.
        /// </summary>
        public int StandardIndex { get; }
        /// <summary>
        /// Gets the sample file name.
        /// </summary>
        public string SampleName { get; }
        /// <summary>
        /// Gets the reference standard name.
        /// </summary>
        public string StandardName { get; }
        /// <summary>
        /// Gets the reference ID.
        /// </summary>
        public int ReferenceId { get; }
        /// <summary>
        /// Gets the reference m/z.
        /// </summary>
        public double ReferenceMz { get; }
        /// <summary>
        /// Gets the reference RT.
        /// </summary>
        public double ReferenceRt { get; }
        /// <summary>
        /// Gets the currently stored RT for this sample-standard pair.
        /// </summary>
        public double CurrentRt { get; }
        /// <summary>
        /// Gets or sets the manual RT to apply.
        /// </summary>
        public double ManualRt {
            get => manualRt;
            set {
                if (manualRt == value) {
                    return;
                }
                manualRt = value;
                OnPropertyChanged(nameof(ManualRt));
            }
        }
        /// <summary>
        /// Gets the detected RT from the peak selector.
        /// </summary>
        public double DetectedRt { get; }
        /// <summary>
        /// Gets the detected m/z from the peak selector.
        /// </summary>
        public double DetectedMz { get; }
        /// <summary>
        /// Gets the detected peak height from the peak selector.
        /// </summary>
        public double DetectedPeakHeight { get; }
        /// <summary>
        /// Gets the peak selection reason.
        /// </summary>
        public string SelectedReason { get; }
        /// <summary>
        /// Gets the number of candidates evaluated for this pair.
        /// </summary>
        public int CandidateCount { get; }
        /// <summary>
        /// Gets the number of rejected candidates.
        /// </summary>
        public int RejectedCount { get; }
        /// <summary>
        /// Gets the rejection reasons for rejected candidates.
        /// </summary>
        public string RejectedReasons { get; }
        /// <summary>
        /// Gets a value indicating whether a peak was detected.
        /// </summary>
        public bool HasDetectedPeak { get; }

        /// <summary>
        /// Applies the current manual RT to the underlying analysis file entry.
        /// </summary>
        public DelegateCommand ApplyManualRtCommand => applyManualRtCommand ??= new DelegateCommand(ApplyManualRt, CanApplyManualRt);
        private DelegateCommand? applyManualRtCommand;

        private void ApplyManualRt() {
            applyManualRt(SampleIndex, StandardIndex, ManualRt);
        }

        private bool CanApplyManualRt() {
            return ManualRt >= 0d;
        }
    }
}
