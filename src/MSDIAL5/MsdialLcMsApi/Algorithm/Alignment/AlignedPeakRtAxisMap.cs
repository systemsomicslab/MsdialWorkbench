using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

/// <summary>Where an <see cref="AlignedPeakRtAxisMap"/> comes from.</summary>
public enum AlignedPeakRtAxisSource {
    /// <summary>The alignment did not correct RTs: an aligned RT is the file's measured RT.</summary>
    NotCorrected,
    /// <summary>The automatic correction ran and left this file on its measured RT axis.</summary>
    Uncorrected,
    /// <summary>The file's own map, as the alignment applied it (the map file beside the result).</summary>
    CorrectionMap,
    /// <summary>
    /// No map file: a local map through the peak's measured left, top and right, kept in the spot's
    /// chromatogram file, and the aligned peak's. Exact at those three points, linear between them,
    /// the edge offsets held outside them.
    /// </summary>
    StoredPeakBounds,
    /// <summary>Neither the map nor the peak's stored bounds can say where the aligned peak lies on the measured axis.</summary>
    Unavailable,
}

/// <summary>
/// Moves RTs of one aligned peak between its file's measured RT axis (the axis of the per-file peak
/// list, the raw data and the spot's chromatograms) and the aligned RT axis (the axis of the aligned
/// peak's ChromXs and of the spot's TimesCenter).
/// </summary>
/// <remarks>
/// Every map is strictly increasing, so <see cref="ToOriginal"/> and <see cref="ToAligned"/> are
/// inverse to each other; a local map whose points are not increasing falls back to the constant
/// offset at the peak top, and to <see cref="AlignedPeakRtAxisSource.Unavailable"/> when even that
/// is unknown.
/// </remarks>
public sealed class AlignedPeakRtAxisMap {
    private readonly Func<double, double> _toOriginal;
    private readonly Func<double, double> _toAligned;

    private AlignedPeakRtAxisMap(AlignedPeakRtAxisSource source, Func<double, double> toOriginal, Func<double, double> toAligned, string note) {
        Source = source;
        _toOriginal = toOriginal;
        _toAligned = toAligned;
        Note = note;
    }

    public AlignedPeakRtAxisSource Source { get; }

    /// <summary>Why the map is what it is, for a person; empty when there is nothing to say.</summary>
    public string Note { get; }

    /// <summary>True when an aligned RT can differ from the measured one, so a caller must convert.</summary>
    public bool IsCorrected => Source is AlignedPeakRtAxisSource.CorrectionMap or AlignedPeakRtAxisSource.StoredPeakBounds or AlignedPeakRtAxisSource.Unavailable;

    /// <summary>True when values may be written back through this map.</summary>
    public bool CanWrite => Source != AlignedPeakRtAxisSource.Unavailable;

    /// <summary>The measured RT of an aligned RT.</summary>
    public double ToOriginal(double alignedRt) => _toOriginal(alignedRt);

    /// <summary>The aligned RT of a measured RT.</summary>
    public double ToAligned(double originalRt) => _toAligned(originalRt);

    public static AlignedPeakRtAxisMap NotCorrected { get; } = new AlignedPeakRtAxisMap(AlignedPeakRtAxisSource.NotCorrected, rt => rt, rt => rt, string.Empty);

    public static AlignedPeakRtAxisMap Uncorrected { get; } = new AlignedPeakRtAxisMap(
        AlignedPeakRtAxisSource.Uncorrected, rt => rt, rt => rt,
        "The automatic RT correction left this file on its measured RT axis (too few anchors, or a Blank it did not interpolate).");

    /// <summary>A file's map from the alignment's correction: the model, or <see cref="Uncorrected"/> when the file has none.</summary>
    public static AlignedPeakRtAxisMap FromCorrection(AlignmentRetentionTimeCorrectionCollection correction, int fileId) {
        if (correction is null) throw new ArgumentNullException(nameof(correction));
        if (!correction.TryGetModel(fileId, out var model)) {
            return Uncorrected;
        }
        return new AlignedPeakRtAxisMap(AlignedPeakRtAxisSource.CorrectionMap, model.Restore, model.Correct, string.Empty);
    }

    /// <summary>A map that cannot convert: the reason is <paramref name="reason"/>.</summary>
    public static AlignedPeakRtAxisMap Unavailable(string reason, double? alignedMinusOriginal = null) {
        // Drawing still needs a position: with the top's offset when it is known, else as stored.
        var offset = alignedMinusOriginal ?? 0d;
        return new AlignedPeakRtAxisMap(AlignedPeakRtAxisSource.Unavailable, rt => rt - offset, rt => rt + offset, reason);
    }

    /// <summary>
    /// A local map through the peak's measured bounds (left, top, right, from the spot's
    /// chromatogram file) and the aligned peak's bounds.
    /// </summary>
    public static AlignedPeakRtAxisMap FromStoredBounds(
        double originalLeft, double originalTop, double originalRight,
        double alignedLeft, double alignedTop, double alignedRight) {
        if (!IsUsable(originalTop) || !IsUsable(alignedTop)) {
            return Unavailable("The peak's measured RT is not known, so its aligned RT cannot be moved onto the measured axis.");
        }
        var points = new List<(double Original, double Aligned)> { (originalTop, alignedTop) };
        if (IsUsable(originalLeft) && IsUsable(alignedLeft)) {
            points.Add((originalLeft, alignedLeft));
        }
        if (IsUsable(originalRight) && IsUsable(alignedRight)) {
            points.Add((originalRight, alignedRight));
        }
        points = points.OrderBy(point => point.Original).ToList();
        var increasing = true;
        for (var i = 1; i < points.Count; i++) {
            if (!(points[i].Original > points[i - 1].Original && points[i].Aligned > points[i - 1].Aligned)) {
                increasing = false;
                break;
            }
        }
        const string note = "No RT correction map was saved with this alignment; the peak's own measured and aligned bounds are used to move between the axes.";
        if (!increasing || points.Count == 1) {
            var offset = alignedTop - originalTop;
            return new AlignedPeakRtAxisMap(AlignedPeakRtAxisSource.StoredPeakBounds, rt => rt - offset, rt => rt + offset, note);
        }
        var forward = points.ToArray();
        var backward = points.Select(point => (point.Aligned, point.Original)).ToArray();
        return new AlignedPeakRtAxisMap(
            AlignedPeakRtAxisSource.StoredPeakBounds,
            rt => Interpolate(backward, rt),
            rt => Interpolate(forward, rt),
            note);
    }

    private static bool IsUsable(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;

    // Linear between the points, the end offsets held outside them, as the correction's own
    // piecewise-linear model does.
    private static double Interpolate((double X, double Y)[] points, double value) {
        if (value <= points[0].X) {
            return value + points[0].Y - points[0].X;
        }
        var last = points.Length - 1;
        if (value >= points[last].X) {
            return value + points[last].Y - points[last].X;
        }
        for (var i = 1; i <= last; i++) {
            if (value <= points[i].X) {
                var fraction = (value - points[i - 1].X) / (points[i].X - points[i - 1].X);
                return points[i - 1].Y + fraction * (points[i].Y - points[i - 1].Y);
            }
        }
        return value + points[last].Y - points[last].X;
    }
}
