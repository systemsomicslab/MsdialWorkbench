using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>A fitted mass error model and the points it rests on.</summary>
internal sealed class MassErrorFit
{
    public MassErrorFit(MassErrorModel model, bool[] isInlier, int inlierCount) {
        Model = model;
        IsInlier = isInlier;
        InlierCount = inlierCount;
    }

    public MassErrorModel Model { get; }

    /// <summary>Per point given, whether it lies within the band the fit kept; false for points not given.</summary>
    public bool[] IsInlier { get; }

    public int InlierCount { get; }
}

/// <summary>
/// Fits a mass error model to points of which a share are unrelated ions or wrong matches, by iteratively reweighted
/// least squares with Tukey's biweight: each point weighs 1/σ² of its intensity class, times the biweight of its
/// residual in units of that σ, so that the scattered weak ions count for less and a point more than
/// <see cref="TuningConstant"/> σ from the model counts for nothing.
/// </summary>
/// <remarks>
/// The window offsets are drawn toward the overall offset by a normal prior of <see cref="WindowPriorSdPpm"/>, so a
/// window with few points keeps close to the rest. The standard errors are those of an M-estimator:
/// (Σψ²/(n − p)) / (Σψ′/n)² × (XᵀΣ⁻¹X + prior)⁻¹ over the points within the band.
/// </remarks>
internal static class MassErrorModelFitter
{
    /// <summary>The biweight's cut-off, in standard deviations: 95% efficiency for normal errors.</summary>
    public const double TuningConstant = 4.685d;

    /// <summary>The spread expected of the isolation windows' offsets about the overall one, in ppm.</summary>
    public const double WindowPriorSdPpm = 5d;

    /// <summary>The smallest scatter a class of points is taken to have, in ppm, so that no class of a few close points outweighs the rest.</summary>
    public const double MinimumSdPpm = 0.5d;

    private const int MaximumIterations = 100;
    private const double ConvergencePpm = 1e-7;
    private static readonly double[] BandScales = { 4d, 2d, 1d };

    /// <summary>
    /// Fits <paramref name="design"/> to the points at <paramref name="rows"/>, starting from a constant error of
    /// <paramref name="startPpm"/>; null when fewer than <paramref name="minimumInliers"/> points stay within the band
    /// or the terms cannot be told apart.
    /// </summary>
    public static MassErrorFit? Fit(IReadOnlyList<MassCalibrationPoint> points, IReadOnlyList<double> sigmas, IReadOnlyList<int> rows, MassErrorDesign design, double startPpm, int minimumInliers) {
        var count = design.Count;
        var fixedCount = design.FixedCount;
        var coefficients = new double[count];
        coefficients[design.OffsetColumn] = startPpm;
        var row = new double[fixedCount];
        var windowColumns = new int[rows.Count];
        var features = new double[rows.Count][];
        for (var i = 0; i < rows.Count; i++) {
            var point = points[rows[i]];
            features[i] = new double[fixedCount];
            windowColumns[i] = design.Row(point.Mz, point.RetentionTime, point.Window, features[i]);
        }

        // The band starts wide and narrows, so that points the start leaves far off (an error that follows m/z, or a
        // window that reads apart) are drawn in before the band closes on them.
        foreach (var bandScale in BandScales) {
            for (var iteration = 0; iteration < MaximumIterations; iteration++) {
                var normal = new double[count, count];
                var rhs = new double[count];
                var inliers = 0;
                for (var i = 0; i < rows.Count; i++) {
                    var point = points[rows[i]];
                    var sigma = sigmas[rows[i]] * bandScale;
                    var residual = point.ErrorPpm - Predict(coefficients, features[i], windowColumns[i]);
                    var weight = Biweight(residual / sigma) / (sigma * sigma);
                    if (weight <= 0d) {
                        continue;
                    }
                    inliers++;
                    Accumulate(normal, rhs, features[i], windowColumns[i], weight, point.ErrorPpm);
                }
                if (inliers < minimumInliers) {
                    return null;
                }
                AddPrior(normal, design, bandScale);
                var factor = Cholesky(normal);
                if (factor is null) {
                    return null;
                }
                var next = Solve(factor, rhs);
                var change = 0d;
                for (var j = 0; j < count; j++) {
                    change = Math.Max(change, Math.Abs(next[j] - coefficients[j]));
                }
                coefficients = next;
                if (change < ConvergencePpm) {
                    break;
                }
            }
        }

        // The band and the standard errors at the solution.
        var isInlier = new bool[points.Count];
        var information = new double[count, count];
        var unused = new double[count];
        var sumPsiSquared = 0d;
        var sumPsiDerivative = 0d;
        var inlierCount = 0;
        for (var i = 0; i < rows.Count; i++) {
            var point = points[rows[i]];
            var sigma = sigmas[rows[i]];
            var u = (point.ErrorPpm - Predict(coefficients, features[i], windowColumns[i])) / sigma;
            if (Math.Abs(u) >= TuningConstant) {
                continue;
            }
            isInlier[rows[i]] = true;
            inlierCount++;
            var v = u / TuningConstant;
            var psi = u * (1d - v * v) * (1d - v * v);
            sumPsiSquared += psi * psi;
            sumPsiDerivative += (1d - v * v) * (1d - 5d * v * v);
            Accumulate(information, unused, features[i], windowColumns[i], 1d / (sigma * sigma), 0d);
        }
        var parameters = fixedCount + (design.Has(MassErrorTerms.Window) ? 1 : 0);
        if (inlierCount < minimumInliers || inlierCount <= parameters || sumPsiDerivative <= 0d) {
            return null;
        }
        AddPrior(information, design);
        var informationFactor = Cholesky(information);
        if (informationFactor is null) {
            return null;
        }
        var meanPsiDerivative = sumPsiDerivative / inlierCount;
        var scale = sumPsiSquared / (inlierCount - parameters) / (meanPsiDerivative * meanPsiDerivative);
        var covariance = Inverse(informationFactor);
        for (var j = 0; j < count; j++) {
            for (var k = 0; k < count; k++) {
                covariance[j, k] *= scale;
            }
        }
        return new MassErrorFit(new MassErrorModel(design, coefficients, covariance), isInlier, inlierCount);
    }

    /// <summary>
    /// The scatter of each point, in ppm: the robust SD of the residuals of its intensity decade, or of all points when
    /// its decade has fewer than <see cref="MassErrorEstimate.MinimumSignalPairs"/> above the background; never below
    /// <see cref="MinimumSdPpm"/>.
    /// </summary>
    public static double[] Sigmas(IReadOnlyList<MassCalibrationPoint> points, IReadOnlyList<double> residuals, double searchPpm) {
        var overall = MassErrorEstimate.Estimate(residuals, searchPpm);
        var fallback = overall.IsEstimated && overall.RobustSdPpm > 0d ? overall.RobustSdPpm : MassErrorEstimate.MaximumHalfWidthPpm / 4d;
        var byClass = Enumerable.Range(0, points.Count)
            .GroupBy(i => IntensityClass(points[i].Intensity))
            .ToDictionary(
                group => group.Key,
                group => {
                    var estimate = MassErrorEstimate.Estimate(group.Select(i => residuals[i]), searchPpm);
                    return estimate.IsReliable && estimate.RobustSdPpm > 0d ? estimate.RobustSdPpm : fallback;
                });
        return points.Select(point => Math.Max(MinimumSdPpm, byClass[IntensityClass(point.Intensity)])).ToArray();
    }

    /// <summary>The held-out loss of one point: its squared residual in units of its σ, capped at the biweight's cut-off.</summary>
    public static double Loss(double residualPpm, double sigma) {
        var u = residualPpm / sigma;
        return Math.Min(u * u, TuningConstant * TuningConstant);
    }

    private static int IntensityClass(double intensity) {
        return intensity > 0d && !double.IsInfinity(intensity) ? (int)Math.Floor(Math.Log10(intensity)) : int.MinValue;
    }

    private static double Biweight(double u) {
        var v = u / TuningConstant;
        return Math.Abs(v) < 1d ? (1d - v * v) * (1d - v * v) : 0d;
    }

    private static double Predict(double[] coefficients, double[] features, int windowColumn) {
        var value = 0d;
        for (var j = 0; j < features.Length; j++) {
            value += features[j] * coefficients[j];
        }
        return windowColumn >= 0 ? value + coefficients[windowColumn] : value;
    }

    // Adds weight · x xᵀ and weight · x y for one row, x being the fixed features and a 1 in the window's column.
    private static void Accumulate(double[,] normal, double[] rhs, double[] features, int windowColumn, double weight, double y) {
        for (var j = 0; j < features.Length; j++) {
            var wx = weight * features[j];
            rhs[j] += wx * y;
            for (var k = 0; k < features.Length; k++) {
                normal[j, k] += wx * features[k];
            }
            if (windowColumn >= 0) {
                normal[j, windowColumn] += wx;
                normal[windowColumn, j] += wx;
            }
        }
        if (windowColumn >= 0) {
            normal[windowColumn, windowColumn] += weight;
            rhs[windowColumn] += weight * y;
        }
    }

    // The window prior, in the same units as the data's weights: both scale with the band.
    private static void AddPrior(double[,] normal, MassErrorDesign design, double bandScale = 1d) {
        var precision = 1d / (WindowPriorSdPpm * WindowPriorSdPpm * bandScale * bandScale);
        for (var j = design.FixedCount; j < design.Count; j++) {
            normal[j, j] += precision;
        }
    }

    // The lower triangular L with L Lᵀ = A, or null when A is not positive definite.
    private static double[,]? Cholesky(double[,] a) {
        var n = a.GetLength(0);
        var l = new double[n, n];
        for (var j = 0; j < n; j++) {
            var diagonal = a[j, j];
            for (var k = 0; k < j; k++) {
                diagonal -= l[j, k] * l[j, k];
            }
            if (!(diagonal > 1e-12 * Math.Max(1d, Math.Abs(a[j, j])))) {
                return null;
            }
            l[j, j] = Math.Sqrt(diagonal);
            for (var i = j + 1; i < n; i++) {
                var sum = a[i, j];
                for (var k = 0; k < j; k++) {
                    sum -= l[i, k] * l[j, k];
                }
                l[i, j] = sum / l[j, j];
            }
        }
        return l;
    }

    private static double[] Solve(double[,] l, double[] b) {
        var n = b.Length;
        var y = new double[n];
        for (var i = 0; i < n; i++) {
            var sum = b[i];
            for (var k = 0; k < i; k++) {
                sum -= l[i, k] * y[k];
            }
            y[i] = sum / l[i, i];
        }
        var x = new double[n];
        for (var i = n - 1; i >= 0; i--) {
            var sum = y[i];
            for (var k = i + 1; k < n; k++) {
                sum -= l[k, i] * x[k];
            }
            x[i] = sum / l[i, i];
        }
        return x;
    }

    private static double[,] Inverse(double[,] l) {
        var n = l.GetLength(0);
        var inverse = new double[n, n];
        for (var j = 0; j < n; j++) {
            var unit = new double[n];
            unit[j] = 1d;
            var column = Solve(l, unit);
            for (var i = 0; i < n; i++) {
                inverse[i, j] = column[i];
            }
        }
        return inverse;
    }
}
