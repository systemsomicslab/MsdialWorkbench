using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// One line of a mass accuracy summary: the m/z error of one file's pairs over one group of them.
/// </summary>
public sealed class MassAccuracyRow
{
    public MassAccuracyRow(string fileName, string pairs, string group, string range, MassErrorEstimate estimate) {
        FileName = fileName;
        Pairs = pairs;
        Group = group;
        Range = range;
        Estimate = estimate;
    }

    public string FileName { get; }

    /// <summary>Which pairs: all, the isotope-confirmed ones, the MS1 precursors or the MS2 product ions.</summary>
    public string Pairs { get; }

    /// <summary>What the pairs are grouped by: overall, m/z, RT, isolation window, intensity, library and so on.</summary>
    public string Group { get; }

    public string Range { get; }

    public MassErrorEstimate Estimate { get; }
}

/// <summary>
/// The summary table of the mass accuracy reports: one line per file, pairs and group, with the error, its spread and
/// the pairs it rests on.
/// </summary>
public static class MassAccuracySummary
{
    private static readonly string[] Header =
    {
        "File name", "Pairs", "Group", "Range", "Candidate pairs", "Pairs in peak", "Pairs above background",
        "Background (pairs/ppm)", "Offset (ppm)", "Robust SD (ppm)", "Standard error (ppm)", "Peak half width (ppm)", "Mode (ppm)", "Note",
    };

    private static readonly string TooFewPairs =
        $"Fewer than {MassErrorEstimate.MinimumSignalPairs.ToString(CultureInfo.InvariantCulture)} pairs above the background; no offset is reported.";

    public static void WriteHeader(TextWriter writer) {
        writer.WriteLine(string.Join("\t", Header));
    }

    public static void WriteRows(TextWriter writer, IEnumerable<MassAccuracyRow> rows) {
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

    /// <summary>"offset X ppm (robust SD Y, Z pairs above background)", for a console line.</summary>
    public static string Describe(MassErrorEstimate estimate) {
        return $"offset {Format(estimate.OffsetPpm, "0.0")} ppm (robust SD {Format(estimate.RobustSdPpm, "0.0")}, {Format(estimate.SignalCount, "0")} pairs above background)";
    }

    internal static string Format(double value, string format) {
        return double.IsNaN(value) || double.IsInfinity(value) ? string.Empty : value.ToString(format, CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Adds the rows of one set of one file's pairs to a summary, one group at a time. A group without pairs within the
/// search window is left out.
/// </summary>
internal sealed class MassAccuracyGroups<T>
{
    private readonly List<MassAccuracyRow> _rows;
    private readonly string _fileName;
    private readonly string _pairs;
    private readonly IReadOnlyList<T> _items;
    private readonly Func<T, double> _error;
    private readonly double _searchPpm;

    public MassAccuracyGroups(List<MassAccuracyRow> rows, string fileName, string pairs, IReadOnlyList<T> items, Func<T, double> error, double searchPpm) {
        _rows = rows;
        _fileName = fileName;
        _pairs = pairs;
        _items = items;
        _error = error;
        _searchPpm = searchPpm;
    }

    public void Add(string group, string range, IEnumerable<T> members) {
        var estimate = MassErrorEstimate.Estimate(members.Select(_error), _searchPpm);
        if (estimate.PairCount > 0) {
            _rows.Add(new MassAccuracyRow(_fileName, _pairs, group, range, estimate));
        }
    }

    public void Overall() {
        Add("Overall", "All", _items);
    }

    /// <summary>Groups ordered by key, each labelled from its key; over <paramref name="members"/>, or all pairs.</summary>
    public void By<TKey>(string group, Func<T, TKey> key, Func<TKey, string> label, IEnumerable<T>? members = null) {
        foreach (var bin in (members ?? _items).GroupBy(key).OrderBy(bin => bin.Key)) {
            Add(group, label(bin.Key), bin);
        }
    }

    /// <summary>Bins of <paramref name="width"/> m/z.</summary>
    public void ByMz(string group, Func<T, double> mz, double width = 100d) {
        By(group, item => (int)Math.Floor(mz(item) / width), key => $"{Format(key * width, "0")}-{Format((key + 1) * width, "0")}");
    }

    /// <summary><paramref name="bins"/> equal bins from the first pair's retention time to the last's.</summary>
    public void ByRetentionTime(Func<T, double> rt, int bins = 10) {
        if (_items.Count == 0) {
            return;
        }
        var first = _items.Min(rt);
        var last = _items.Max(rt);
        var width = (last - first) / bins;
        if (!(width > 0d)) {
            return;
        }
        By("RT (min)", item => Math.Min(bins - 1, (int)Math.Floor((rt(item) - first) / width)), key => $"{Format(first + key * width, "0.00")}-{Format(first + (key + 1) * width, "0.00")}");
    }

    /// <summary>Decades of a positive value, such as an intensity.</summary>
    public void ByDecade(string group, Func<T, double> value) {
        By(group, item => (int)Math.Floor(Math.Log10(value(item))), key => $"1e{key.ToString(CultureInfo.InvariantCulture)}-1e{(key + 1).ToString(CultureInfo.InvariantCulture)}", _items.Where(item => value(item) > 0d));
    }

    /// <summary>Quartiles of a value among the pairs, labelled with the range each covers.</summary>
    public void ByQuartile(string group, Func<T, double> value) {
        const int quantiles = 4;
        var sorted = _items.Select(value).OrderBy(v => v).ToArray();
        if (sorted.Length < quantiles) {
            return;
        }
        var bounds = Enumerable.Range(1, quantiles - 1).Select(q => sorted[q * sorted.Length / quantiles]).ToArray();
        int Quantile(double v) {
            var q = 0;
            while (q < bounds.Length && v >= bounds[q]) {
                q++;
            }
            return q;
        }
        foreach (var bin in _items.GroupBy(item => Quantile(value(item))).OrderBy(bin => bin.Key)) {
            var lower = bin.Min(value);
            var upper = bin.Max(value);
            Add(group, $"Q{(bin.Key + 1).ToString(CultureInfo.InvariantCulture)} ({Format(lower, "G3")}-{Format(upper, "G3")})", bin);
        }
    }

    private static string Format(double value, string format) => MassAccuracySummary.Format(value, format);
}
