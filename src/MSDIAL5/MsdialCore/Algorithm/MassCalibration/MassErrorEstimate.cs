using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// The m/z error of a set of candidate pairs, found as the peak that the true pairs make over the flat background of
/// unrelated ions that happened to lie within the search window.
/// </summary>
/// <remarks>
/// The errors are counted in 1 ppm bins over ±search window, smoothed over 5 ppm, and the fullest bin is the start.
/// The median of the errors within a half width of it is taken, the half width is set to four robust standard
/// deviations (1.4826 × the median absolute deviation) within <see cref="MinimumHalfWidthPpm"/> and
/// <see cref="MaximumHalfWidthPpm"/>, and the two steps are repeated until neither moves. The mean or the median of all
/// pairs would be pulled toward zero by the background, which is spread evenly over the window wherever the error is.
/// <para>
/// The background is measured as the density of the errors more than three half widths from the offset, and
/// <see cref="SignalCount"/> is the number of pairs in the peak above it: a peak of a few pairs over a dense
/// background is not an error, however narrow.
/// </para>
/// </remarks>
public sealed class MassErrorEstimate
{
    public const double MinimumHalfWidthPpm = 3d;
    public const double MaximumHalfWidthPpm = 20d;
    private const double InitialHalfWidthPpm = 15d;
    private const double BinWidthPpm = 1d;
    private const int SmoothingBins = 2;
    private const int MaximumIterations = 50;

    private MassErrorEstimate(int pairCount, int inlierCount, double modePpm, double offsetPpm, double robustSdPpm, double halfWidthPpm, double backgroundPerPpm) {
        PairCount = pairCount;
        InlierCount = inlierCount;
        ModePpm = modePpm;
        OffsetPpm = offsetPpm;
        RobustSdPpm = robustSdPpm;
        HalfWidthPpm = halfWidthPpm;
        BackgroundPerPpm = backgroundPerPpm;
    }

    /// <summary>Candidate pairs given, within the search window.</summary>
    public int PairCount { get; }

    /// <summary>Pairs within <see cref="HalfWidthPpm"/> of <see cref="OffsetPpm"/>.</summary>
    public int InlierCount { get; }

    /// <summary>Centre of the fullest smoothed bin, where the search started.</summary>
    public double ModePpm { get; }

    /// <summary>Median error of the pairs in the peak: the error of the product-ion scans, NaN when there are no pairs.</summary>
    public double OffsetPpm { get; }

    /// <summary>1.4826 × the median absolute deviation of the pairs in the peak.</summary>
    public double RobustSdPpm { get; }

    public double HalfWidthPpm { get; }

    /// <summary>Pairs per ppm away from the peak, the unrelated ions.</summary>
    public double BackgroundPerPpm { get; }

    /// <summary>
    /// Pairs in the peak above the background, never below zero; all pairs in the peak when the peak leaves no room
    /// in the search window to measure a background.
    /// </summary>
    public double SignalCount {
        get {
            if (InlierCount == 0) {
                return 0d;
            }
            return double.IsNaN(BackgroundPerPpm) ? InlierCount : Math.Max(0d, InlierCount - BackgroundPerPpm * 2d * HalfWidthPpm);
        }
    }

    /// <summary>Standard error of <see cref="OffsetPpm"/>, as of a median: 1.2533 × <see cref="RobustSdPpm"/> / √n.</summary>
    public double StandardErrorPpm => InlierCount > 0 ? 1.2533d * RobustSdPpm / Math.Sqrt(InlierCount) : double.NaN;

    public bool IsEstimated => InlierCount > 0;

    /// <summary>
    /// Pairs above the background below which the offset is not reported: with a handful of pairs over a ±200 ppm
    /// window, the fullest bin is as often a chance cluster of unrelated ions as the error.
    /// </summary>
    public const int MinimumSignalPairs = 10;

    /// <summary>Whether at least <see cref="MinimumSignalPairs"/> pairs stand above the background.</summary>
    public bool IsReliable => IsEstimated && SignalCount >= MinimumSignalPairs;

    public static MassErrorEstimate Estimate(IEnumerable<double> errorsPpm, double searchPpm = PrecursorCalibrantCollector.DefaultSearchPpm) {
        if (errorsPpm is null) {
            throw new ArgumentNullException(nameof(errorsPpm));
        }
        if (!(searchPpm > 0d)) {
            throw new ArgumentOutOfRangeException(nameof(searchPpm), searchPpm, "The search window must be positive.");
        }

        var errors = errorsPpm.Where(error => !double.IsNaN(error) && Math.Abs(error) <= searchPpm).OrderBy(error => error).ToArray();
        if (errors.Length == 0) {
            return new MassErrorEstimate(0, 0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
        }

        var mode = Mode(errors, searchPpm);
        var centre = mode;
        var halfWidth = InitialHalfWidthPpm;
        var robustSd = double.NaN;
        var inliers = Within(errors, centre, halfWidth);
        for (var i = 0; i < MaximumIterations && inliers.Length > 0; i++) {
            var median = Median(inliers);
            robustSd = 1.4826d * Median(inliers.Select(error => Math.Abs(error - median)).OrderBy(deviation => deviation).ToArray());
            var nextHalfWidth = Math.Min(MaximumHalfWidthPpm, Math.Max(MinimumHalfWidthPpm, 4d * robustSd));
            var converged = Math.Abs(median - centre) < 1e-9 && Math.Abs(nextHalfWidth - halfWidth) < 1e-9;
            centre = median;
            halfWidth = nextHalfWidth;
            inliers = Within(errors, centre, halfWidth);
            if (converged) {
                break;
            }
        }
        if (inliers.Length == 0) {
            return new MassErrorEstimate(errors.Length, 0, mode, double.NaN, double.NaN, halfWidth, double.NaN);
        }

        var offset = Median(inliers);
        robustSd = 1.4826d * Median(inliers.Select(error => Math.Abs(error - offset)).OrderBy(deviation => deviation).ToArray());
        return new MassErrorEstimate(errors.Length, inliers.Length, mode, offset, robustSd, halfWidth, Background(errors, offset, halfWidth, searchPpm));
    }

    private static double Mode(double[] sorted, double searchPpm) {
        var binCount = (int)Math.Ceiling(2d * searchPpm / BinWidthPpm);
        var counts = new int[binCount];
        foreach (var error in sorted) {
            var bin = (int)Math.Floor((error + searchPpm) / BinWidthPpm);
            counts[Math.Min(binCount - 1, Math.Max(0, bin))]++;
        }
        var best = -1;
        var bestCount = -1;
        var bestDistance = double.MaxValue;
        for (var bin = 0; bin < binCount; bin++) {
            var smoothed = 0;
            for (var j = Math.Max(0, bin - SmoothingBins); j <= Math.Min(binCount - 1, bin + SmoothingBins); j++) {
                smoothed += counts[j];
            }
            var centre = -searchPpm + (bin + 0.5d) * BinWidthPpm;
            // A tie goes to the bin nearer zero, so the start does not depend on the scan direction.
            if (smoothed > bestCount || (smoothed == bestCount && Math.Abs(centre) < bestDistance)) {
                best = bin;
                bestCount = smoothed;
                bestDistance = Math.Abs(centre);
            }
        }
        return -searchPpm + (best + 0.5d) * BinWidthPpm;
    }

    private static double Background(double[] sorted, double offset, double halfWidth, double searchPpm) {
        var exclusion = 3d * halfWidth;
        var lower = Math.Max(-searchPpm, offset - exclusion);
        var upper = Math.Min(searchPpm, offset + exclusion);
        var span = 2d * searchPpm - (upper - lower);
        if (span <= 0d) {
            return double.NaN;
        }
        var outside = sorted.Count(error => error < lower || error > upper);
        return outside / span;
    }

    private static double[] Within(double[] sorted, double centre, double halfWidth) {
        return sorted.Where(error => Math.Abs(error - centre) <= halfWidth).ToArray();
    }

    private static double Median(double[] sorted) {
        if (sorted.Length == 0) {
            return double.NaN;
        }
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : 0.5d * (sorted[middle - 1] + sorted[middle]);
    }
}
