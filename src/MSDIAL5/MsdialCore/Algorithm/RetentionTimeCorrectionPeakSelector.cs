using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm {
    /// <summary>
    /// Describes why a candidate peak was rejected during RT correction peak selection.
    /// </summary>
    [Flags]
    public enum RetentionTimeCorrectionPeakRejectReason {
        /// <summary>
        /// The candidate passed every filter.
        /// </summary>
        None = 0,
        /// <summary>
        /// The candidate mass exceeded the allowed tolerance.
        /// </summary>
        MassTolerance = 1,
        /// <summary>
        /// The candidate RT exceeded the allowed tolerance.
        /// </summary>
        RetentionTimeTolerance = 2,
        /// <summary>
        /// The candidate peak height was below the minimum threshold.
        /// </summary>
        MinimumPeakHeight = 4,
    }

    /// <summary>
    /// Describes how a peak was selected for RT correction.
    /// </summary>
    public enum RetentionTimeCorrectionPeakSelectionReason {
        /// <summary>
        /// No candidate passed the configured filters.
        /// </summary>
        None = 0,
        /// <summary>
        /// No candidate was available for selection.
        /// </summary>
        NoCandidates = 1,
        /// <summary>
        /// Exactly one candidate passed the filters.
        /// </summary>
        SelectedSingleCandidate = 2,
        /// <summary>
        /// Multiple candidates passed the filters, and the highest peak height was selected.
        /// </summary>
        SelectedByHighestPeakHeight = 3,
    }

    /// <summary>
    /// Represents a single evaluated candidate peak and the rejection conditions that applied to it.
    /// </summary>
    public sealed class RetentionTimeCorrectionPeakCandidateResult {
        /// <summary>
        /// Initializes a new candidate evaluation result.
        /// </summary>
        public RetentionTimeCorrectionPeakCandidateResult(
            ChromatogramPeakFeature peak,
            double massDifference,
            double rtDifference,
            RetentionTimeCorrectionPeakRejectReason rejectReason) {
            Peak = peak;
            MassDifference = massDifference;
            RtDifference = rtDifference;
            RejectReason = rejectReason;
        }

        /// <summary>
        /// Gets the evaluated chromatogram peak.
        /// </summary>
        public ChromatogramPeakFeature Peak { get; }
        /// <summary>
        /// Gets the absolute difference between the candidate m/z and the reference m/z.
        /// </summary>
        public double MassDifference { get; }
        /// <summary>
        /// Gets the absolute difference between the candidate RT and the reference RT.
        /// </summary>
        public double RtDifference { get; }
        /// <summary>
        /// Gets the rejection reasons applied to this candidate.
        /// </summary>
        public RetentionTimeCorrectionPeakRejectReason RejectReason { get; }
        /// <summary>
        /// Gets a value indicating whether this candidate passed every filter.
        /// </summary>
        public bool IsAccepted => RejectReason == RetentionTimeCorrectionPeakRejectReason.None;
    }

    /// <summary>
    /// Represents the outcome of selecting the RT correction peak for a reference compound.
    /// </summary>
    public sealed class RetentionTimeCorrectionPeakSelectionResult {
        /// <summary>
        /// Initializes a new peak selection result.
        /// </summary>
        public RetentionTimeCorrectionPeakSelectionResult(
            MoleculeMsReference reference,
            ChromatogramPeakFeature? selectedPeak,
            IReadOnlyList<RetentionTimeCorrectionPeakCandidateResult> candidates,
            RetentionTimeCorrectionPeakSelectionReason reason) {
            Reference = reference;
            SelectedPeak = selectedPeak;
            Candidates = candidates;
            SelectedReason = reason;
        }

        /// <summary>
        /// Gets the reference standard used for the selection.
        /// </summary>
        public MoleculeMsReference Reference { get; }
        /// <summary>
        /// Gets the peak chosen for correction, or null when no candidate passed the filters.
        /// </summary>
        public ChromatogramPeakFeature? SelectedPeak { get; }
        /// <summary>
        /// Gets all evaluated candidates, including rejected ones.
        /// </summary>
        public IReadOnlyList<RetentionTimeCorrectionPeakCandidateResult> Candidates { get; }
        /// <summary>
        /// Gets the rejected subset of <see cref="Candidates"/>.
        /// </summary>
        public IReadOnlyList<RetentionTimeCorrectionPeakCandidateResult> RejectedCandidates => Candidates.Where(candidate => !candidate.IsAccepted).ToArray();
        /// <summary>
        /// Gets the reason the selector used to choose the result.
        /// </summary>
        public RetentionTimeCorrectionPeakSelectionReason SelectedReason { get; }
        /// <summary>
        /// Gets a value indicating whether a candidate peak was selected.
        /// </summary>
        public bool HasSelection => SelectedPeak is not null;
    }

    /// <summary>
    /// Selects the best RT correction peak from a set of chromatogram candidates.
    /// </summary>
    public static class RetentionTimeCorrectionPeakSelector {
        /// <summary>
        /// Evaluates all candidates against the reference tolerances and returns the selected peak.
        /// </summary>
        public static RetentionTimeCorrectionPeakSelectionResult Select(
            MoleculeMsReference reference,
            IEnumerable<ChromatogramPeakFeature>? candidates) {
            if (reference is null) {
                throw new ArgumentNullException(nameof(reference));
            }

            var evaluatedCandidates = (candidates ?? Enumerable.Empty<ChromatogramPeakFeature>())
                .Select(candidate => Evaluate(reference, candidate))
                .ToArray();
            var acceptedCandidates = evaluatedCandidates.Where(candidate => candidate.IsAccepted).ToArray();
            if (acceptedCandidates.Length == 0) {
                return new RetentionTimeCorrectionPeakSelectionResult(
                    reference,
                    null,
                    evaluatedCandidates,
                    RetentionTimeCorrectionPeakSelectionReason.NoCandidates);
            }

            var selected = acceptedCandidates
                .OrderByDescending(candidate => candidate.Peak.PeakFeature.PeakHeightTop)
                .First();
            return new RetentionTimeCorrectionPeakSelectionResult(
                reference,
                selected.Peak,
                evaluatedCandidates,
                acceptedCandidates.Length == 1
                    ? RetentionTimeCorrectionPeakSelectionReason.SelectedSingleCandidate
                    : RetentionTimeCorrectionPeakSelectionReason.SelectedByHighestPeakHeight);
        }

        /// <summary>
        /// Calculates the per-candidate differences and filter violations.
        /// </summary>
        private static RetentionTimeCorrectionPeakCandidateResult Evaluate(
            MoleculeMsReference reference,
            ChromatogramPeakFeature candidate) {
            var massDifference = Math.Abs(candidate.PrecursorMz - reference.PrecursorMz);
            var rtDifference = Math.Abs(candidate.PeakFeature.ChromXsTop.RT.Value - reference.ChromXs.RT.Value);
            var rejectReason = RetentionTimeCorrectionPeakRejectReason.None;

            if (massDifference > reference.MassTolerance) {
                rejectReason |= RetentionTimeCorrectionPeakRejectReason.MassTolerance;
            }
            if (rtDifference >= reference.RetentionTimeTolerance) {
                rejectReason |= RetentionTimeCorrectionPeakRejectReason.RetentionTimeTolerance;
            }
            if (candidate.PeakFeature.PeakHeightTop <= reference.MinimumPeakHeight) {
                rejectReason |= RetentionTimeCorrectionPeakRejectReason.MinimumPeakHeight;
            }

            return new RetentionTimeCorrectionPeakCandidateResult(
                candidate,
                massDifference,
                rtDifference,
                rejectReason);
        }
    }
}
