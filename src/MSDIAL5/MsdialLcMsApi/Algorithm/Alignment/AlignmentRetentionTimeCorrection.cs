using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

public sealed class AlignmentRetentionTimeCorrectionControlPoint {
    public AlignmentRetentionTimeCorrectionControlPoint(
        int anchorId,
        double mass,
        double originalRt,
        double correctedRt,
        double qualityScore) {
        AnchorId = anchorId;
        Mass = mass;
        OriginalRt = originalRt;
        CorrectedRt = correctedRt;
        QualityScore = qualityScore;
    }

    public int AnchorId { get; }
    public double Mass { get; }
    public double OriginalRt { get; }
    public double CorrectedRt { get; }
    public double QualityScore { get; }
    public double Offset => CorrectedRt - OriginalRt;
}

public interface IAlignmentRetentionTimeCorrectionModel {
    IReadOnlyList<AlignmentRetentionTimeCorrectionControlPoint> ControlPoints { get; }
    double Correct(double originalRt);
    double Restore(double correctedRt);
}

/// <summary>
/// A monotonic, piecewise-linear map from the original peak-detection RT axis to the RT axis of
/// the selected alignment reference. The first and last anchor offsets are held constant outside
/// the anchor range so the ends of a run do not snap back to an unrelated identity transform.
/// </summary>
public sealed class PiecewiseLinearAlignmentRetentionTimeCorrectionModel : IAlignmentRetentionTimeCorrectionModel {
    private readonly IReadOnlyList<AlignmentRetentionTimeCorrectionControlPoint> _controlPoints;
    private readonly IReadOnlyList<AlignmentRetentionTimeCorrectionControlPoint> _inversePoints;

    public PiecewiseLinearAlignmentRetentionTimeCorrectionModel(
        IEnumerable<AlignmentRetentionTimeCorrectionControlPoint> controlPoints) {
        if (controlPoints is null) {
            throw new ArgumentNullException(nameof(controlPoints));
        }
        var points = controlPoints
            .OrderBy(point => point.OriginalRt)
            .GroupBy(point => point.OriginalRt)
            .Select(group => group.OrderByDescending(point => point.QualityScore).First())
            .ToList();
        if (points.Count < 2) {
            throw new ArgumentException("At least two distinct RT control points are required.", nameof(controlPoints));
        }
        for (var index = 1; index < points.Count; index++) {
            if (points[index].CorrectedRt <= points[index - 1].CorrectedRt) {
                throw new ArgumentException("RT correction control points must be strictly monotonic.", nameof(controlPoints));
            }
        }
        _controlPoints = points;
        _inversePoints = points.OrderBy(point => point.CorrectedRt).ToList();
    }

    public IReadOnlyList<AlignmentRetentionTimeCorrectionControlPoint> ControlPoints => _controlPoints;

    public double Correct(double originalRt) {
        return Interpolate(
            originalRt,
            _controlPoints,
            point => point.OriginalRt,
            point => point.CorrectedRt);
    }

    public double Restore(double correctedRt) {
        return Interpolate(
            correctedRt,
            _inversePoints,
            point => point.CorrectedRt,
            point => point.OriginalRt);
    }

    private static double Interpolate(
        double value,
        IReadOnlyList<AlignmentRetentionTimeCorrectionControlPoint> points,
        Func<AlignmentRetentionTimeCorrectionControlPoint, double> x,
        Func<AlignmentRetentionTimeCorrectionControlPoint, double> y) {
        if (value <= x(points[0])) {
            return value + y(points[0]) - x(points[0]);
        }
        var last = points.Count - 1;
        if (value >= x(points[last])) {
            return value + y(points[last]) - x(points[last]);
        }

        var low = 0;
        var high = last;
        while (high - low > 1) {
            var middle = low + (high - low) / 2;
            if (x(points[middle]) <= value) {
                low = middle;
            }
            else {
                high = middle;
            }
        }
        var leftX = x(points[low]);
        var rightX = x(points[high]);
        var fraction = (value - leftX) / (rightX - leftX);
        return y(points[low]) + fraction * (y(points[high]) - y(points[low]));
    }
}

/// <summary>
/// Interpolates two correction models by analytical order. This is used for Blank files that do
/// not carry enough biological signal to estimate their own anchors.
/// </summary>
public sealed class BlendedAlignmentRetentionTimeCorrectionModel : IAlignmentRetentionTimeCorrectionModel {
    private readonly IAlignmentRetentionTimeCorrectionModel _left;
    private readonly IAlignmentRetentionTimeCorrectionModel _right;
    private readonly double _rightWeight;

    public BlendedAlignmentRetentionTimeCorrectionModel(
        IAlignmentRetentionTimeCorrectionModel left,
        IAlignmentRetentionTimeCorrectionModel right,
        double rightWeight) {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _rightWeight = Math.Max(0d, Math.Min(1d, rightWeight));
    }

    public IReadOnlyList<AlignmentRetentionTimeCorrectionControlPoint> ControlPoints => Array.Empty<AlignmentRetentionTimeCorrectionControlPoint>();

    public double Correct(double originalRt) {
        return _left.Correct(originalRt) * (1d - _rightWeight)
            + _right.Correct(originalRt) * _rightWeight;
    }

    public double Restore(double correctedRt) {
        var margin = 1d;
        var low = correctedRt - margin;
        var high = correctedRt + margin;
        for (var index = 0; index < 24 && Correct(low) > correctedRt; index++) {
            margin *= 2d;
            low = correctedRt - margin;
        }
        margin = 1d;
        for (var index = 0; index < 24 && Correct(high) < correctedRt; index++) {
            margin *= 2d;
            high = correctedRt + margin;
        }
        for (var index = 0; index < 80; index++) {
            var middle = (low + high) / 2d;
            if (Correct(middle) < correctedRt) {
                low = middle;
            }
            else {
                high = middle;
            }
        }
        return (low + high) / 2d;
    }
}

public sealed class AlignmentRetentionTimeCorrectionCollection {
    private readonly IReadOnlyDictionary<int, IAlignmentRetentionTimeCorrectionModel> _models;

    public AlignmentRetentionTimeCorrectionCollection(
        int referenceFileId,
        IReadOnlyDictionary<int, IAlignmentRetentionTimeCorrectionModel> models) {
        ReferenceFileId = referenceFileId;
        _models = models ?? throw new ArgumentNullException(nameof(models));
    }

    public int ReferenceFileId { get; }
    public IReadOnlyDictionary<int, IAlignmentRetentionTimeCorrectionModel> Models => _models;

    public bool TryGetModel(int fileId, out IAlignmentRetentionTimeCorrectionModel model) {
        return _models.TryGetValue(fileId, out model!);
    }

    public double Correct(int fileId, double originalRt) {
        return TryGetModel(fileId, out var model) ? model.Correct(originalRt) : originalRt;
    }

    public double Restore(int fileId, double correctedRt) {
        return TryGetModel(fileId, out var model) ? model.Restore(correctedRt) : correctedRt;
    }

    public void Correct(ChromatogramPeakFeature feature, int fileId) {
        if (!TryGetModel(fileId, out var model)) {
            return;
        }
        Correct(feature, model);
    }

    public void Correct(AlignmentChromPeakFeature feature, int fileId) {
        if (!TryGetModel(fileId, out var model)
            || feature.ChromXsTop?.RT is null
            || feature.ChromXsTop.RT.Value < 0d) {
            return;
        }
        feature.ChromXsLeft = Correct(feature.ChromXsLeft, model);
        feature.ChromXsTop = Correct(feature.ChromXsTop, model);
        feature.ChromXsRight = Correct(feature.ChromXsRight, model);
    }

    private static void Correct(ChromatogramPeakFeature feature, IAlignmentRetentionTimeCorrectionModel model) {
        feature.PeakFeature.ChromXsLeft = Correct(feature.PeakFeature.ChromXsLeft, model);
        feature.PeakFeature.ChromXsTop = Correct(feature.PeakFeature.ChromXsTop, model);
        feature.PeakFeature.ChromXsRight = Correct(feature.PeakFeature.ChromXsRight, model);
        foreach (var driftFeature in feature.DriftChromFeatures ?? Enumerable.Empty<ChromatogramPeakFeature>()) {
            Correct(driftFeature, model);
        }
    }

    private static ChromXs Correct(ChromXs value, IAlignmentRetentionTimeCorrectionModel model) {
        if (value?.RT is null || value.RT.Value < 0d) {
            return value;
        }
        return new ChromXs(
            new RetentionTime(model.Correct(value.RT.Value), value.RT.Unit),
            value.RI,
            value.Drift,
            value.Mz,
            value.MainType);
    }
}
