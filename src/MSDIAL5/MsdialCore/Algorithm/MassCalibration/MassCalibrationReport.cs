using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// Writes down the mass error model chosen for each file and set of points, and whether it would be applied: one line
/// per file and set, with the model's terms and coefficients, the errors it models, the scatter before and after, the
/// held-out loss of every model tried, and the reason for the decision.
/// </summary>
public static class MassCalibrationReport
{
    public const string Apply = "Apply";
    public const string DoNotApply = "Do not apply";

    private static readonly string[] Header =
    {
        "File name", "Points", "Candidate points", "Points above background", "Offset as read (ppm)", "Robust SD as read (ppm)",
        "Model", "Points in band", "Reference m/z", "Reference RT (min)", "Error at reference (ppm)",
        "1/sqrt(m/z) coefficient (ppm*sqrt(m/z))", "RT slope (ppm/min)", "Windows", "Smallest window offset (ppm)", "Largest window offset (ppm)",
        "Smallest modelled error (ppm)", "Largest modelled error (ppm)", "SE of largest error (ppm)", "Residual offset (ppm)", "Residual robust SD (ppm)",
        "Held-out loss as read", "Held-out loss of models tried", "Decision", "Reason", "Algorithm version",
    };

    public static void WriteHeader(TextWriter writer) {
        writer.WriteLine(string.Join("\t", Header));
    }

    public static void WriteRow(TextWriter writer, string fileName, string points, MassCalibrationDecision decision) {
        if (decision is null) {
            throw new ArgumentNullException(nameof(decision));
        }
        var model = decision.Model;
        var hasModel = model.Terms != MassErrorTerms.None;
        var windowOffsets = model.WindowOffsetsPpm.Values.ToArray();
        var estimate = decision.Estimate;
        writer.WriteLine(string.Join("\t", new[]
        {
            fileName,
            points,
            estimate.PairCount.ToString(CultureInfo.InvariantCulture),
            Format(estimate.IsEstimated ? estimate.SignalCount : double.NaN, "0"),
            Format(estimate.IsReliable ? estimate.OffsetPpm : double.NaN, "0.00"),
            Format(estimate.IsReliable ? estimate.RobustSdPpm : double.NaN, "0.00"),
            MassErrorModel.Describe(model.Terms),
            hasModel ? decision.InlierCount.ToString(CultureInfo.InvariantCulture) : string.Empty,
            Format(hasModel ? model.ReferenceMz : double.NaN, "0.0000"),
            Format(hasModel ? model.ReferenceRetentionTime : double.NaN, "0.00"),
            Format(hasModel ? model.ErrorAtReferencePpm : double.NaN, "0.00"),
            Format(Has(model, MassErrorTerms.InverseSqrtMz) ? model.InverseSqrtMzCoefficient : double.NaN, "0.0"),
            Format(Has(model, MassErrorTerms.RetentionTime) ? model.RetentionTimeSlope : double.NaN, "0.000"),
            windowOffsets.Length > 0 ? windowOffsets.Length.ToString(CultureInfo.InvariantCulture) : string.Empty,
            Format(windowOffsets.Length > 0 ? windowOffsets.Min() : double.NaN, "0.00"),
            Format(windowOffsets.Length > 0 ? windowOffsets.Max() : double.NaN, "0.00"),
            Format(decision.SmallestErrorPpm, "0.00"),
            Format(decision.LargestErrorPpm, "0.00"),
            Format(decision.LargestErrorStandardErrorPpm, "0.000"),
            Format(decision.ResidualEstimate is { IsReliable: true } after ? after.OffsetPpm : double.NaN, "0.00"),
            Format(decision.ResidualEstimate is { IsReliable: true } spread ? spread.RobustSdPpm : double.NaN, "0.00"),
            Format(decision.HeldOutLossWithoutModel, "0.000"),
            string.Join("; ", decision.Candidates.Select(candidate => $"{MassErrorModel.Describe(candidate.Terms)} {(candidate.CouldBeFitted ? Format(candidate.HeldOutLoss, "0.000") : "not fitted")}")),
            decision.IsApplied ? Apply : DoNotApply,
            decision.Reason,
            MassCalibrationDecision.AlgorithmVersion,
        }));
    }

    /// <summary>"Offset + 1/sqrt(m/z), modelled error -3.2 to 1.0 ppm: apply", or "do not apply: reason", for a console line.</summary>
    public static string Describe(MassCalibrationDecision decision) {
        if (decision is null) {
            throw new ArgumentNullException(nameof(decision));
        }
        if (decision.Model.Terms == MassErrorTerms.None) {
            return $"do not apply: {decision.Reason}";
        }
        var model = $"{MassErrorModel.Describe(decision.Model.Terms)}, modelled error {Range(decision.SmallestErrorPpm, decision.LargestErrorPpm)} ppm";
        return decision.IsApplied ? $"{model}: apply" : $"{model}: do not apply: {decision.Reason}";
    }

    private static string Range(double smallest, double largest) {
        var low = Format(smallest, "0.0");
        var high = Format(largest, "0.0");
        return low == high ? low : $"{low} to {high}";
    }

    private static bool Has(MassErrorModel model, MassErrorTerms term) => (model.Terms & term) != 0;

    private static string Format(double value, string format) => MassAccuracySummary.Format(value, format);
}
