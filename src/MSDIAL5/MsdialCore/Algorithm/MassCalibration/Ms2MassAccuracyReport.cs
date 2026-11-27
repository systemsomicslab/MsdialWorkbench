using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// One line of the MS2 mass accuracy summary: the error of one file's product-ion scans over one group of pairs.
/// </summary>
public sealed class Ms2MassAccuracyRow
{
    public Ms2MassAccuracyRow(string fileName, string pairs, string group, string range, MassErrorEstimate estimate) {
        FileName = fileName;
        Pairs = pairs;
        Group = group;
        Range = range;
        Estimate = estimate;
    }

    public string FileName { get; }

    /// <summary><see cref="Ms2MassAccuracyReport.AllPairs"/> or <see cref="Ms2MassAccuracyReport.IsotopeConfirmedPairs"/>.</summary>
    public string Pairs { get; }

    /// <summary>What the pairs are grouped by: overall, m/z, RT, isolation window, MS2 intensity or MS2 scan load.</summary>
    public string Group { get; }

    public string Range { get; }

    public MassErrorEstimate Estimate { get; }
}

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
    private const int RetentionTimeBins = 10;
    private const int LoadQuantiles = 4;

    private static readonly string[] PairsHeader =
    {
        "Peak ID", "RT (min)", "MS1 m/z", "MS1 intensity", "MS2 m/z", "MS2 intensity", "Error (ppm)", "MS2 scan index",
        "Experiment ID", "Isolation lower m/z", "Isolation upper m/z", "Collision energy", "MS2 scan total intensity", "Isotope confirmed",
    };

    private static readonly string[] SummaryHeader =
    {
        "File name", "Pairs", "Group", "Range", "Candidate pairs", "Pairs in peak", "Pairs above background",
        "Background (pairs/ppm)", "Offset (ppm)", "Robust SD (ppm)", "Standard error (ppm)", "Peak half width (ppm)", "Mode (ppm)", "Note",
    };

    private static readonly string TooFewPairs =
        $"Fewer than {MassErrorEstimate.MinimumSignalPairs.ToString(CultureInfo.InvariantCulture)} pairs above the background; no offset is reported.";

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

    public static void WriteSummaryHeader(TextWriter writer) {
        writer.WriteLine(string.Join("\t", SummaryHeader));
    }

    public static void WriteSummaryRows(TextWriter writer, IEnumerable<Ms2MassAccuracyRow> rows) {
        foreach (var row in rows) {
            var estimate = row.Estimate;
            // Below the minimum the offset, its spread and its peak are left empty, so that no one reads a chance
            // cluster of a few unrelated ions as the error of that m/z or RT range.
            var reliable = estimate.IsReliable;
            writer.WriteLine(string.Join("\t", new[]
            {
                row.FileName,
                row.Pairs,
                row.Group,
                row.Range,
                estimate.PairCount.ToString(CultureInfo.InvariantCulture),
                estimate.InlierCount.ToString(CultureInfo.InvariantCulture),
                Format(estimate.IsEstimated ? estimate.SignalCount : double.NaN, "0"),
                Format(estimate.BackgroundPerPpm, "0.00"),
                Format(reliable ? estimate.OffsetPpm : double.NaN, "0.00"),
                Format(reliable ? estimate.RobustSdPpm : double.NaN, "0.00"),
                Format(reliable ? estimate.StandardErrorPpm : double.NaN, "0.000"),
                Format(reliable ? estimate.HalfWidthPpm : double.NaN, "0.0"),
                Format(reliable ? estimate.ModePpm : double.NaN, "0.0"),
                reliable ? string.Empty : TooFewPairs,
            }));
        }
    }

    /// <summary>
    /// The error of one file's pairs: overall, then by m/z, RT, isolation window, MS2 intensity and MS2 scan load, first
    /// over all pairs and then over the isotope-confirmed ones. A group without pairs is left out.
    /// </summary>
    public static List<Ms2MassAccuracyRow> Summarize(string fileName, IReadOnlyList<Ms2PrecursorCalibrant> pairs, double searchPpm = PrecursorCalibrantCollector.DefaultSearchPpm) {
        if (pairs is null) {
            throw new ArgumentNullException(nameof(pairs));
        }
        var rows = new List<Ms2MassAccuracyRow>();
        Summarize(rows, fileName, AllPairs, pairs, searchPpm);
        Summarize(rows, fileName, IsotopeConfirmedPairs, pairs.Where(pair => pair.IsotopeConfirmed).ToList(), searchPpm);
        return rows;
    }

    /// <summary>One line for the console: the overall error of all pairs and of the isotope-confirmed ones.</summary>
    public static string Describe(IReadOnlyList<Ms2MassAccuracyRow> rows) {
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

    private static string Describe(MassErrorEstimate estimate) {
        return $"offset {Format(estimate.OffsetPpm, "0.0")} ppm (robust SD {Format(estimate.RobustSdPpm, "0.0")}, {Format(estimate.SignalCount, "0")} pairs above background)";
    }

    private static void Summarize(List<Ms2MassAccuracyRow> rows, string fileName, string subset, IReadOnlyList<Ms2PrecursorCalibrant> pairs, double searchPpm) {
        void Add(string group, string range, IEnumerable<Ms2PrecursorCalibrant> members) {
            var estimate = MassErrorEstimate.Estimate(members.Select(pair => pair.ErrorPpm), searchPpm);
            if (estimate.PairCount > 0) {
                rows.Add(new Ms2MassAccuracyRow(fileName, subset, group, range, estimate));
            }
        }

        Add("Overall", "All", pairs);

        foreach (var bin in pairs.GroupBy(pair => (int)Math.Floor(pair.Ms1Mz / 100d)).OrderBy(bin => bin.Key)) {
            Add("m/z", $"{(bin.Key * 100).ToString(CultureInfo.InvariantCulture)}-{((bin.Key + 1) * 100).ToString(CultureInfo.InvariantCulture)}", bin);
        }

        if (pairs.Count > 0) {
            var first = pairs.Min(pair => pair.RetentionTime);
            var last = pairs.Max(pair => pair.RetentionTime);
            var width = (last - first) / RetentionTimeBins;
            if (width > 0d) {
                foreach (var bin in pairs.GroupBy(pair => Math.Min(RetentionTimeBins - 1, (int)Math.Floor((pair.RetentionTime - first) / width))).OrderBy(bin => bin.Key)) {
                    Add("RT (min)", $"{Format(first + bin.Key * width, "0.00")}-{Format(first + (bin.Key + 1) * width, "0.00")}", bin);
                }
            }
        }

        var windows = pairs.Where(pair => pair.HasIsolationWindow && pair.IsolationUpperMz - pair.IsolationLowerMz >= MinimumReportedIsolationWidth);
        foreach (var bin in windows.GroupBy(pair => (Math.Round(pair.IsolationLowerMz, 1), Math.Round(pair.IsolationUpperMz, 1))).OrderBy(bin => bin.Key.Item1).ThenBy(bin => bin.Key.Item2)) {
            Add("Isolation window (m/z)", $"{Format(bin.Key.Item1, "0.0")}-{Format(bin.Key.Item2, "0.0")}", bin);
        }

        foreach (var bin in pairs.Where(pair => pair.Ms2Intensity > 0d).GroupBy(pair => (int)Math.Floor(Math.Log10(pair.Ms2Intensity))).OrderBy(bin => bin.Key)) {
            Add("MS2 intensity", $"1e{bin.Key.ToString(CultureInfo.InvariantCulture)}-1e{(bin.Key + 1).ToString(CultureInfo.InvariantCulture)}", bin);
        }

        // Quartiles of the scans' total intensity, among the scans the pairs come from: the ion load of the scan.
        var loads = pairs.Select(pair => pair.Ms2ScanTotalIntensity).OrderBy(load => load).ToArray();
        if (loads.Length >= LoadQuantiles) {
            var bounds = Enumerable.Range(1, LoadQuantiles - 1).Select(q => loads[q * loads.Length / LoadQuantiles]).ToArray();
            int Quantile(double load) {
                var q = 0;
                while (q < bounds.Length && load >= bounds[q]) {
                    q++;
                }
                return q;
            }
            foreach (var bin in pairs.GroupBy(pair => Quantile(pair.Ms2ScanTotalIntensity)).OrderBy(bin => bin.Key)) {
                var lower = bin.Min(pair => pair.Ms2ScanTotalIntensity);
                var upper = bin.Max(pair => pair.Ms2ScanTotalIntensity);
                Add("MS2 scan total intensity", $"Q{(bin.Key + 1).ToString(CultureInfo.InvariantCulture)} ({Format(lower, "G3")}-{Format(upper, "G3")})", bin);
            }
        }
    }

    private static string Format(double value, string format) {
        return double.IsNaN(value) || double.IsInfinity(value) ? string.Empty : value.ToString(format, CultureInfo.InvariantCulture);
    }
}
