using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>One model tried: its terms and its mean loss on the points held out from it.</summary>
public sealed class MassErrorCandidate
{
    public MassErrorCandidate(MassErrorTerms terms, double heldOutLoss) {
        Terms = terms;
        HeldOutLoss = heldOutLoss;
    }

    public MassErrorTerms Terms { get; }

    /// <summary>
    /// Mean over all points of the squared residual on held-out points, in units of each point's scatter and capped at
    /// the biweight cut-off; NaN when the model could not be fitted (too few points, or terms that cannot be told apart).
    /// </summary>
    public double HeldOutLoss { get; }

    public bool CouldBeFitted => !double.IsNaN(HeldOutLoss);
}

/// <summary>
/// Whether one file's MS1 or MS2 m/z should be recalibrated, and by which model: the model chosen by cross-validation
/// among a constant offset, a 1/√(m/z) term, a retention-time drift and isolation-window offsets, and the rule that
/// decides whether it is worth applying.
/// </summary>
/// <remarks>
/// <para>
/// The points are split into <see cref="Folds"/> by feature. Each model is fitted to the other folds and scored on
/// each held-out one; the simplest model whose loss is within one standard error of the best (the paired difference,
/// or <see cref="NegligibleLoss"/>) is chosen, "None" (the m/z as read) being the simplest of all.
/// </para>
/// <para>
/// The model is applied only when all of these hold: at least <see cref="MassErrorEstimate.MinimumSignalPairs"/> points
/// stand above the background; a model, not "None", is chosen; and the largest error it models over the points within
/// its band is at least the minimum correction and at least <see cref="SignificanceFactor"/> standard errors. Otherwise
/// nothing is to be corrected, and <see cref="Reason"/> says which condition failed.
/// </para>
/// </remarks>
public sealed class MassCalibrationDecision
{
    /// <summary>Changes whenever the model, its fit or the rule changes, so that a recorded decision can be traced to its rule.</summary>
    public const string AlgorithmVersion = "1";

    public const double DefaultMinimumCorrectionPpm = 1.5d;
    public const double SignificanceFactor = 3d;
    public const int Folds = 5;

    /// <summary>Points within the band each fixed term needs in the fit to all points.</summary>
    public const int MinimumInliersPerTerm = 10;

    /// <summary>A loss lower by less than this (a tenth of a standard deviation, squared, per point) is no improvement.</summary>
    public const double NegligibleLoss = 0.01d;

    /// <summary>The 1/√(m/z) term is tried only when the points span at least this ratio of m/z; over less it is an offset.</summary>
    public const double MinimumMzRatioForSlope = 1.5d;

    /// <summary>A window gets an offset of its own only with at least this many points.</summary>
    public const int MinimumPointsPerWindow = 3;

    private MassCalibrationDecision(
        MassErrorEstimate estimate,
        IReadOnlyList<MassErrorCandidate> candidates,
        double heldOutLossWithoutModel,
        MassErrorModel model,
        int inlierCount,
        double smallestErrorPpm,
        double largestErrorPpm,
        double largestErrorStandardErrorPpm,
        MassErrorEstimate? residualEstimate,
        bool isApplied,
        string reason) {
        Estimate = estimate;
        Candidates = candidates;
        HeldOutLossWithoutModel = heldOutLossWithoutModel;
        Model = model;
        InlierCount = inlierCount;
        SmallestErrorPpm = smallestErrorPpm;
        LargestErrorPpm = largestErrorPpm;
        LargestErrorStandardErrorPpm = largestErrorStandardErrorPpm;
        ResidualEstimate = residualEstimate;
        IsApplied = isApplied;
        Reason = reason;
    }

    /// <summary>The error of the points as read: a constant offset over the background.</summary>
    public MassErrorEstimate Estimate { get; }

    /// <summary>The models tried, simplest first.</summary>
    public IReadOnlyList<MassErrorCandidate> Candidates { get; }

    /// <summary>The held-out loss of leaving the m/z as read; NaN when no model was tried.</summary>
    public double HeldOutLossWithoutModel { get; }

    /// <summary>The model chosen and fitted to all points; <see cref="MassErrorModel.None"/> when none was chosen.</summary>
    public MassErrorModel Model { get; }

    /// <summary>Points within the chosen model's band.</summary>
    public int InlierCount { get; }

    /// <summary>The smallest modelled error over the points within the band, in ppm; NaN without a model.</summary>
    public double SmallestErrorPpm { get; }

    /// <summary>The largest modelled error over the points within the band, in ppm; NaN without a model.</summary>
    public double LargestErrorPpm { get; }

    /// <summary>The standard error of the modelled error where it is largest in size, in ppm.</summary>
    public double LargestErrorStandardErrorPpm { get; }

    /// <summary>The error left after the model: the residuals as <see cref="Estimate"/> measures the errors; null without a model.</summary>
    public MassErrorEstimate? ResidualEstimate { get; }

    public bool IsApplied { get; }

    public string Reason { get; }

    /// <summary>
    /// Chooses and judges a mass error model for one file's points. Points with an error beyond
    /// <paramref name="searchPpm"/>, or without a positive m/z, are left out.
    /// </summary>
    public static MassCalibrationDecision Decide(IReadOnlyList<MassCalibrationPoint> points, double searchPpm, double minimumCorrectionPpm = DefaultMinimumCorrectionPpm) {
        if (points is null) {
            throw new ArgumentNullException(nameof(points));
        }
        if (!(searchPpm > 0d)) {
            throw new ArgumentOutOfRangeException(nameof(searchPpm), searchPpm, "The search window must be positive.");
        }
        if (!(minimumCorrectionPpm >= 0d)) {
            throw new ArgumentOutOfRangeException(nameof(minimumCorrectionPpm), minimumCorrectionPpm, "The minimum correction must not be negative.");
        }

        var usable = points
            .Where(point => point.Mz > 0d && !double.IsInfinity(point.Mz) && !double.IsNaN(point.ErrorPpm) && Math.Abs(point.ErrorPpm) <= searchPpm
                && !double.IsNaN(point.RetentionTime) && !double.IsInfinity(point.RetentionTime))
            .ToList();
        var errors = usable.Select(point => point.ErrorPpm).ToArray();
        var estimate = MassErrorEstimate.Estimate(errors, searchPpm);
        if (!estimate.IsReliable) {
            return NotApplied(estimate, Array.Empty<MassErrorCandidate>(), double.NaN,
                $"Fewer than {MassErrorEstimate.MinimumSignalPairs.ToString(CultureInfo.InvariantCulture)} points above the background.");
        }

        // The references and ranges are the points' own: a window or an m/z range that reads far from the overall offset
        // is still within them.
        var windows = usable
            .Where(point => point.Window != MassCalibrationPoint.NoWindow)
            .GroupBy(point => point.Window)
            .Where(group => group.Count() >= MinimumPointsPerWindow)
            .Select(group => group.Key)
            .OrderBy(window => window)
            .ToArray();
        var full = new MassErrorDesign(
            MassErrorTerms.Offset | MassErrorTerms.InverseSqrtMz | MassErrorTerms.RetentionTime | MassErrorTerms.Window,
            Median(usable.Select(point => point.Mz)), Median(usable.Select(point => point.RetentionTime)),
            usable.Min(point => point.Mz), usable.Max(point => point.Mz),
            usable.Min(point => point.RetentionTime), usable.Max(point => point.RetentionTime),
            windows);
        var termSets = TermSets(full, windows.Length);

        var all = Enumerable.Range(0, usable.Count).ToArray();
        var sigmas = MassErrorModelFitter.Sigmas(usable, errors, searchPpm);
        // The scatter is measured again about the richest model that fits, so that an error that follows m/z or RT is not
        // taken for scatter.
        var richest = Enumerable.Reverse(termSets).Select(terms => Fit(usable, sigmas, all, full.With(terms), estimate.OffsetPpm)).FirstOrDefault(fit => fit is not null);
        if (richest is not null) {
            sigmas = MassErrorModelFitter.Sigmas(usable, Residuals(usable, richest.Model), searchPpm);
        }

        var folds = usable.Select(point => ((point.FeatureId % Folds) + Folds) % Folds).ToArray();
        var withoutModel = usable.Select((point, i) => MassErrorModelFitter.Loss(point.ErrorPpm, sigmas[i])).ToArray();
        var losses = new List<(MassErrorTerms Terms, double[]? Loss)> { (MassErrorTerms.None, withoutModel) };
        foreach (var terms in termSets) {
            losses.Add((terms, CrossValidate(usable, sigmas, folds, full.With(terms), estimate.OffsetPpm, searchPpm)));
        }
        var candidates = losses.Skip(1).Select(pair => new MassErrorCandidate(pair.Terms, pair.Loss is null ? double.NaN : pair.Loss.Average())).ToList();
        var chosen = Choose(losses);
        if (chosen == MassErrorTerms.None) {
            return NotApplied(estimate, candidates, withoutModel.Average(),
                "No model improved the held-out points on the m/z as read.");
        }

        var fit = Fit(usable, sigmas, all, full.With(chosen), estimate.OffsetPpm)!;
        var model = fit.Model;
        var inliers = all.Where(i => fit.IsInlier[i]).ToArray();
        var modelled = inliers.Select(i => model.PredictPpm(usable[i].Mz, usable[i].RetentionTime, usable[i].Window)).ToArray();
        var largestAt = inliers[Enumerable.Range(0, inliers.Length).OrderByDescending(k => Math.Abs(modelled[k])).First()];
        var largest = model.PredictPpm(usable[largestAt].Mz, usable[largestAt].RetentionTime, usable[largestAt].Window);
        var standardError = model.StandardErrorPpm(usable[largestAt].Mz, usable[largestAt].RetentionTime, usable[largestAt].Window);
        var residualEstimate = MassErrorEstimate.Estimate(Residuals(usable, model), searchPpm);

        string reason;
        var applied = false;
        if (Math.Abs(largest) < minimumCorrectionPpm) {
            reason = $"The largest modelled error, {Format(largest)} ppm, is smaller than the minimum correction of {Format(minimumCorrectionPpm)} ppm.";
        }
        else if (Math.Abs(largest) < SignificanceFactor * standardError) {
            reason = $"The largest modelled error, {Format(largest)} ppm, is within {Format(SignificanceFactor)} standard errors ({Format(standardError)} ppm).";
        }
        else {
            applied = true;
            reason = $"The modelled error reaches {Format(largest)} ppm, above the minimum correction of {Format(minimumCorrectionPpm)} ppm and {Format(SignificanceFactor)} standard errors ({Format(standardError)} ppm), and it improved the held-out points.";
        }
        return new MassCalibrationDecision(estimate, candidates, withoutModel.Average(), model, fit.InlierCount, modelled.Min(), modelled.Max(), standardError, residualEstimate, applied, reason);
    }

    // The models to try, simplest first: an offset, with 1/√m, with RT drift, with both, then each of those with the
    // window offsets. A term the points cannot pin down is not tried.
    private static List<MassErrorTerms> TermSets(MassErrorDesign full, int windowCount) {
        var slope = full.MaximumMz / full.MinimumMz >= MinimumMzRatioForSlope;
        var drift = full.MaximumRetentionTime > full.MinimumRetentionTime;
        var basic = new List<MassErrorTerms> { MassErrorTerms.Offset };
        if (slope) {
            basic.Add(MassErrorTerms.Offset | MassErrorTerms.InverseSqrtMz);
        }
        if (drift) {
            basic.Add(MassErrorTerms.Offset | MassErrorTerms.RetentionTime);
        }
        if (slope && drift) {
            basic.Add(MassErrorTerms.Offset | MassErrorTerms.InverseSqrtMz | MassErrorTerms.RetentionTime);
        }
        var sets = new List<MassErrorTerms>(basic);
        if (windowCount >= 2) {
            sets.AddRange(basic.Select(terms => terms | MassErrorTerms.Window));
        }
        return sets;
    }

    private static MassErrorFit? Fit(IReadOnlyList<MassCalibrationPoint> points, double[] sigmas, IReadOnlyList<int> rows, MassErrorDesign design, double startPpm) {
        return MassErrorModelFitter.Fit(points, sigmas, rows, design, startPpm, MinimumInliersPerTerm * Parameters(design));
    }

    // The fixed terms, and the window offsets counted as one: they are drawn toward the overall offset.
    private static int Parameters(MassErrorDesign design) => design.FixedCount + (design.Has(MassErrorTerms.Window) ? 1 : 0);

    // Per point, the loss when its fold is held out; null when the model cannot be fitted to all points or to one fold's complement.
    private static double[]? CrossValidate(IReadOnlyList<MassCalibrationPoint> points, double[] sigmas, int[] folds, MassErrorDesign design, double offsetPpm, double searchPpm) {
        var all = Enumerable.Range(0, points.Count).ToArray();
        if (Fit(points, sigmas, all, design, offsetPpm) is null) {
            return null;
        }
        var losses = new double[points.Count];
        for (var fold = 0; fold < Folds; fold++) {
            var heldOut = all.Where(i => folds[i] == fold).ToArray();
            if (heldOut.Length == 0) {
                continue;
            }
            var training = all.Where(i => folds[i] != fold).ToArray();
            var start = MassErrorEstimate.Estimate(training.Select(i => points[i].ErrorPpm), searchPpm);
            if (!start.IsEstimated) {
                return null;
            }
            var fit = MassErrorModelFitter.Fit(points, sigmas, training, design, start.OffsetPpm, Parameters(design) + 2);
            if (fit is null) {
                return null;
            }
            foreach (var i in heldOut) {
                var point = points[i];
                losses[i] = MassErrorModelFitter.Loss(point.ErrorPpm - fit.Model.PredictPpm(point.Mz, point.RetentionTime, point.Window), sigmas[i]);
            }
        }
        return losses;
    }

    // The simplest model whose loss is within one standard error of the paired difference from the best, or within
    // NegligibleLoss of it.
    private static MassErrorTerms Choose(List<(MassErrorTerms Terms, double[]? Loss)> losses) {
        var fitted = losses.Where(pair => pair.Loss is not null).ToList();
        var best = fitted.OrderBy(pair => pair.Loss!.Average()).First();
        foreach (var (terms, loss) in fitted) {
            var differences = loss!.Zip(best.Loss!, (a, b) => a - b).ToArray();
            var mean = differences.Average();
            var sd = Math.Sqrt(differences.Sum(d => (d - mean) * (d - mean)) / Math.Max(1, differences.Length - 1));
            if (mean <= Math.Max(sd / Math.Sqrt(differences.Length), NegligibleLoss)) {
                return terms;
            }
        }
        return best.Terms;
    }

    private static double[] Residuals(IReadOnlyList<MassCalibrationPoint> points, MassErrorModel model) {
        return points.Select(point => point.ErrorPpm - model.PredictPpm(point.Mz, point.RetentionTime, point.Window)).ToArray();
    }

    private static MassCalibrationDecision NotApplied(MassErrorEstimate estimate, IReadOnlyList<MassErrorCandidate> candidates, double heldOutLossWithoutModel, string reason) {
        return new MassCalibrationDecision(estimate, candidates, heldOutLossWithoutModel, MassErrorModel.None, 0, double.NaN, double.NaN, double.NaN, null, false, reason);
    }

    private static double Median(IEnumerable<double> values) {
        var sorted = values.OrderBy(value => value).ToArray();
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : 0.5d * (sorted[middle - 1] + sorted[middle]);
    }

    private static string Format(double value) => value.ToString("0.0#", CultureInfo.InvariantCulture);
}
