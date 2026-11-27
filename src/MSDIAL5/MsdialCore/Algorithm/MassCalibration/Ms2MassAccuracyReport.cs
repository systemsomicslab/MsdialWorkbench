using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// Measures, and writes down, how far the product-ion scans' m/z lie from the MS1 survey's: the candidate pairs of
/// each file, and their error overall and by m/z, retention time, isolation window, MS2 intensity and the ion load of
/// the scan, so that an error constant over a run can be told from one that follows any of these.
/// </summary>
/// <remarks>
/// It reports and changes nothing: no m/z is corrected.
/// </remarks>
public static class Ms2MassAccuracyReport
{
    public const string FolderName = "ms2_mass_accuracy";
    public const string SummaryFileName = "ms2_mass_accuracy_summary.tsv";
    public const string PairsFileSuffix = ".ms2_precursor_pairs.tsv";
    public const string AllPairs = "All";
    public const string IsotopeConfirmedPairs = "Isotope confirmed";

    /// <summary>Isolation windows narrower than this are a DDA precursor's own, too many to report one by one.</summary>
    private const double MinimumReportedIsolationWidth = 5d;

    private static readonly string[] PairsHeader =
    {
        "Peak ID", "RT (min)", "MS1 m/z", "MS1 intensity", "MS2 m/z", "MS2 intensity", "Error (ppm)", "MS2 scan index",
        "Experiment ID", "Isolation lower m/z", "Isolation upper m/z", "Collision energy", "MS2 scan total intensity", "Isotope confirmed",
    };

    public static string PairsFileName(string analysisFileName) => analysisFileName + PairsFileSuffix;

    public static void WritePairs(TextWriter writer, IEnumerable<Ms2PrecursorCalibrant> pairs) {
        writer.WriteLine(string.Join("\t", PairsHeader));
        foreach (var pair in pairs) {
            writer.WriteLine(string.Join("\t", new[]
            {
                pair.PeakId.ToString(CultureInfo.InvariantCulture),
                Format(pair.RetentionTime, "0.0000"),
                Format(pair.Ms1Mz, "0.00000"),
                Format(pair.Ms1Intensity, "0"),
                Format(pair.Ms2Mz, "0.00000"),
                Format(pair.Ms2Intensity, "0"),
                Format(pair.ErrorPpm, "0.00"),
                pair.Ms2ScanIndex.ToString(CultureInfo.InvariantCulture),
                pair.ExperimentId.ToString(CultureInfo.InvariantCulture),
                Format(pair.IsolationLowerMz, "0.0000"),
                Format(pair.IsolationUpperMz, "0.0000"),
                Format(pair.CollisionEnergy, "0.##"),
                Format(pair.Ms2ScanTotalIntensity, "0"),
                pair.IsotopeConfirmed ? "True" : "False",
            }));
        }
    }

    public static void WriteSummaryHeader(TextWriter writer) => MassAccuracySummary.WriteHeader(writer);

    public static void WriteSummaryRows(TextWriter writer, IEnumerable<MassAccuracyRow> rows) => MassAccuracySummary.WriteRows(writer, rows);

    /// <summary>
    /// The error of one file's pairs: overall, then by m/z, RT, isolation window, MS2 intensity and MS2 scan load, first
    /// over all pairs and then over the isotope-confirmed ones. A group without pairs is left out.
    /// </summary>
    public static List<MassAccuracyRow> Summarize(string fileName, IReadOnlyList<Ms2PrecursorCalibrant> pairs, double searchPpm = PrecursorCalibrantCollector.DefaultSearchPpm) {
        if (pairs is null) {
            throw new ArgumentNullException(nameof(pairs));
        }
        var rows = new List<MassAccuracyRow>();
        Summarize(rows, fileName, AllPairs, pairs, searchPpm);
        Summarize(rows, fileName, IsotopeConfirmedPairs, pairs.Where(pair => pair.IsotopeConfirmed).ToList(), searchPpm);
        return rows;
    }

    /// <summary>One line for the console: the overall error of all pairs and of the isotope-confirmed ones.</summary>
    public static string Describe(IReadOnlyList<MassAccuracyRow> rows) {
        var all = rows.FirstOrDefault(row => row.Pairs == AllPairs && row.Group == "Overall");
        if (all is null || !all.Estimate.IsEstimated) {
            return "no product-ion scan held an ion near a feature's MS1 m/z.";
        }
        if (!all.Estimate.IsReliable) {
            return $"{all.Estimate.PairCount.ToString(CultureInfo.InvariantCulture)} candidate pairs, fewer than {MassErrorEstimate.MinimumSignalPairs.ToString(CultureInfo.InvariantCulture)} of them above the background: no offset is reported.";
        }
        var text = $"{Describe(all.Estimate)} from {all.Estimate.PairCount.ToString(CultureInfo.InvariantCulture)} candidate pairs";
        var confirmed = rows.FirstOrDefault(row => row.Pairs == IsotopeConfirmedPairs && row.Group == "Overall");
        if (confirmed is not null && confirmed.Estimate.IsReliable) {
            text += $"; isotope-confirmed: {Describe(confirmed.Estimate)} from {confirmed.Estimate.PairCount.ToString(CultureInfo.InvariantCulture)}";
        }
        return text + ".";
    }

    private static string Describe(MassErrorEstimate estimate) => MassAccuracySummary.Describe(estimate);

    private static void Summarize(List<MassAccuracyRow> rows, string fileName, string subset, IReadOnlyList<Ms2PrecursorCalibrant> pairs, double searchPpm) {
        var groups = new MassAccuracyGroups<Ms2PrecursorCalibrant>(rows, fileName, subset, pairs, pair => pair.ErrorPpm, searchPpm);
        groups.Overall();
        groups.ByMz("m/z", pair => pair.Ms1Mz);
        groups.ByRetentionTime(pair => pair.RetentionTime);
        groups.By(
            "Isolation window (m/z)",
            pair => (Math.Round(pair.IsolationLowerMz, 1), Math.Round(pair.IsolationUpperMz, 1)),
            key => $"{Format(key.Item1, "0.0")}-{Format(key.Item2, "0.0")}",
            pairs.Where(pair => pair.HasIsolationWindow && pair.IsolationUpperMz - pair.IsolationLowerMz >= MinimumReportedIsolationWidth));
        groups.ByDecade("MS2 intensity", pair => pair.Ms2Intensity);
        // Quartiles of the scans' total intensity, among the scans the pairs come from: the ion load of the scan.
        groups.ByQuartile("MS2 scan total intensity", pair => pair.Ms2ScanTotalIntensity);
    }

    private static string Format(double value, string format) => MassAccuracySummary.Format(value, format);
}
