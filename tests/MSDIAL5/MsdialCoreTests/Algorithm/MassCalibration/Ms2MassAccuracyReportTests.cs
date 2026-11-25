using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration.Tests;

[TestClass]
public class Ms2MassAccuracyReportTests
{
    // Pairs whose error grows with m/z, +10 ppm at m/z 100 to +40 ppm at m/z 900, in 25 Da SWATH
    // windows, every other one isotope-confirmed, among unrelated ions over the ±200 ppm window.
    private static List<Ms2PrecursorCalibrant> Pairs() {
        var random = new Random(17);
        var pairs = new List<Ms2PrecursorCalibrant>();
        for (var i = 0; i < 1600; i++) {
            var ms1 = 100d + 800d * random.NextDouble();
            var isTrue = i % 4 != 3;
            var ppm = isTrue ? 10d + 30d * (ms1 - 100d) / 800d + (random.NextDouble() - 0.5d) * 4d : -200d + 400d * random.NextDouble();
            var lower = Math.Floor(ms1 / 25d) * 25d;
            pairs.Add(new Ms2PrecursorCalibrant(
                i, 1d + 9d * random.NextDouble(), ms1, 1e5, ms1 * (1d + ppm * 1e-6), Math.Pow(10d, 2d + 3d * random.NextDouble()),
                i, 1 + (int)(lower / 25d), lower, lower + 25d, 30d, 1e5 * (1 + i % 10), isTrue && i % 2 == 0));
        }
        return pairs;
    }

    [TestMethod]
    public void SummarizesOverallAndByEachGroupForAllAndForIsotopeConfirmedPairs() {
        var rows = Ms2MassAccuracyReport.Summarize("s1", Pairs());

        var overall = rows.Single(row => row.Pairs == Ms2MassAccuracyReport.AllPairs && row.Group == "Overall");
        Assert.AreEqual(1600, overall.Estimate.PairCount);
        Assert.AreEqual(25d, overall.Estimate.OffsetPpm, 3d);
        Assert.IsTrue(rows.Any(row => row.Pairs == Ms2MassAccuracyReport.IsotopeConfirmedPairs && row.Group == "Overall"));
        CollectionAssert.AreEquivalent(
            new[] { "Overall", "m/z", "RT (min)", "Isolation window (m/z)", "MS2 intensity", "MS2 scan total intensity" },
            rows.Where(row => row.Pairs == Ms2MassAccuracyReport.AllPairs).Select(row => row.Group).Distinct().ToArray());
        Assert.IsTrue(rows.All(row => row.FileName == "s1"));

        var byMz = rows.Where(row => row.Pairs == Ms2MassAccuracyReport.AllPairs && row.Group == "m/z").ToList();
        CollectionAssert.AreEqual(new[] { "100-200", "200-300", "300-400", "400-500", "500-600", "600-700", "700-800", "800-900" }, byMz.Select(row => row.Range).ToArray());
        // 10 to 13.75 ppm over m/z 100-200, 36.25 to 40 ppm over m/z 800-900.
        Assert.AreEqual(11.875d, byMz.First().Estimate.OffsetPpm, 1.5d, "the m/z groups show the error rising with m/z");
        Assert.AreEqual(38.125d, byMz.Last().Estimate.OffsetPpm, 1.5d);

        Assert.AreEqual(10, rows.Count(row => row.Pairs == Ms2MassAccuracyReport.AllPairs && row.Group == "RT (min)"));
        Assert.AreEqual(32, rows.Count(row => row.Pairs == Ms2MassAccuracyReport.AllPairs && row.Group == "Isolation window (m/z)"));
        Assert.AreEqual(4, rows.Count(row => row.Pairs == Ms2MassAccuracyReport.AllPairs && row.Group == "MS2 scan total intensity"));
    }

    [TestMethod]
    public void NarrowIsolationWindowsOfDdaAreNotReportedOneByOne() {
        var pairs = Enumerable.Range(0, 20)
            .Select(i => new Ms2PrecursorCalibrant(i, 1d, 300d + i, 1e5, (300d + i) * (1d + 5e-6), 1e4, i, 1, 299.5d + i, 300.5d + i, 30d, 1e5, true))
            .ToList();

        var rows = Ms2MassAccuracyReport.Summarize("dda", pairs);

        Assert.IsFalse(rows.Any(row => row.Group == "Isolation window (m/z)"));
        Assert.AreEqual(5d, rows.Single(row => row.Pairs == Ms2MassAccuracyReport.AllPairs && row.Group == "Overall").Estimate.OffsetPpm, 1e-6);
    }

    [TestMethod]
    public void AGroupWithTooFewPairsAboveTheBackgroundHasNoOffsetButSaysWhy() {
        var pairs = Enumerable.Range(0, 5)
            .Select(i => new Ms2PrecursorCalibrant(i, 1d, 250d + i, 1e5, (250d + i) * (1d - 66e-6), 1e3, i, 1, 240d, 265d, 30d, 1e5, false))
            .ToList();
        var rows = Ms2MassAccuracyReport.Summarize("few", pairs);
        var text = new StringWriter(CultureInfo.InvariantCulture);
        Ms2MassAccuracyReport.WriteSummaryHeader(text);
        Ms2MassAccuracyReport.WriteSummaryRows(text, rows);
        var lines = text.ToString().TrimEnd('\r', '\n').Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var header = lines[0].Split('\t').ToList();
        var overall = lines.Skip(1).Select(line => line.Split('\t')).Single(cells => cells[1] == "All" && cells[2] == "Overall");

        Assert.AreEqual("5", overall[header.IndexOf("Candidate pairs")]);
        Assert.AreEqual(string.Empty, overall[header.IndexOf("Offset (ppm)")]);
        Assert.AreEqual(string.Empty, overall[header.IndexOf("Robust SD (ppm)")]);
        StringAssert.StartsWith(overall[header.IndexOf("Note")], "Fewer than 10 pairs above the background");
        StringAssert.Contains(Ms2MassAccuracyReport.Describe(rows), "no offset is reported");
    }

    [TestMethod]
    public void NoPairsGiveNoRowsAndASentenceSayingSo() {
        var rows = Ms2MassAccuracyReport.Summarize("empty", new List<Ms2PrecursorCalibrant>());

        Assert.AreEqual(0, rows.Count);
        StringAssert.StartsWith(Ms2MassAccuracyReport.Describe(rows), "no product-ion scan");
    }

    [TestMethod]
    public void DescribesTheOverallOffsetOfAllAndOfIsotopeConfirmedPairs() {
        var text = Ms2MassAccuracyReport.Describe(Ms2MassAccuracyReport.Summarize("s1", Pairs()));

        StringAssert.StartsWith(text, "offset 2");
        StringAssert.Contains(text, "from 1600 candidate pairs");
        StringAssert.Contains(text, "isotope-confirmed: offset ");
    }

    [TestMethod]
    public void WritesOneLinePerPairAndOnePerSummaryRowWithTheirHeaders() {
        var pairs = Pairs().Take(3).ToList();
        var pairsText = new StringWriter(CultureInfo.InvariantCulture);
        Ms2MassAccuracyReport.WritePairs(pairsText, pairs);
        var pairLines = pairsText.ToString().TrimEnd('\r', '\n').Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        Assert.AreEqual(4, pairLines.Length);
        Assert.AreEqual(14, pairLines[0].Split('\t').Length);
        Assert.IsTrue(pairLines.Skip(1).All(line => line.Split('\t').Length == 14));
        Assert.AreEqual(pairs[0].ErrorPpm.ToString("0.00", CultureInfo.InvariantCulture), pairLines[1].Split('\t')[6]);

        var rows = Ms2MassAccuracyReport.Summarize("s1", Pairs());
        var summaryText = new StringWriter(CultureInfo.InvariantCulture);
        Ms2MassAccuracyReport.WriteSummaryHeader(summaryText);
        Ms2MassAccuracyReport.WriteSummaryRows(summaryText, rows);
        var summaryLines = summaryText.ToString().TrimEnd('\r', '\n').Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        Assert.AreEqual(rows.Count + 1, summaryLines.Length);
        var header = summaryLines[0].Split('\t');
        Assert.AreEqual("Offset (ppm)", header[8]);
        Assert.IsTrue(summaryLines.All(line => line.Split('\t').Length == header.Length));
    }
}
