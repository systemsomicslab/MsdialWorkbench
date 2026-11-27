using CompMs.Common.DataObj.Result;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration.Tests;

[TestClass]
public class AnnotationMassAccuracyReportTests
{
    // 60 annotated precursors reading +8 ppm and 200 product ions reading -20 ppm, from two libraries, with a quarter
    // of each spread over the ±100 ppm window as wrong matches.
    private static List<AnnotationCalibrant> Calibrants() {
        var random = new Random(23);
        var calibrants = new List<AnnotationCalibrant>();
        for (var i = 0; i < 60; i++) {
            var reference = 150d + 700d * random.NextDouble();
            var ppm = i % 4 == 3 ? -100d + 200d * random.NextDouble() : 8d + (random.NextDouble() - 0.5d) * 3d;
            calibrants.Add(new AnnotationCalibrant(i, 1d + 9d * random.NextDouble(), 1, reference * (1d + ppm * 1e-6), reference, AnnotationCalibrant.FromFormula,
                Math.Pow(10d, 4d + 2d * random.NextDouble()), reference * (1d + ppm * 1e-6), true, $"compound {i}", i % 2 == 0 ? "lbm" : "msp", i % 2 == 0 ? DataBaseSource.Lbm : DataBaseSource.Msp, 0.8d));
        }
        for (var i = 0; i < 200; i++) {
            var reference = 60d + 700d * random.NextDouble();
            var ppm = i % 4 == 3 ? -100d + 200d * random.NextDouble() : -20d + (random.NextDouble() - 0.5d) * 4d;
            calibrants.Add(new AnnotationCalibrant(i % 60, 1d + 9d * random.NextDouble(), 2, reference * (1d + ppm * 1e-6), reference, AnnotationCalibrant.FromLibraryFragment,
                Math.Pow(10d, 2d + 3d * random.NextDouble()), 800d, i % 10 == 0, $"compound {i % 60}", "lbm", DataBaseSource.Lbm, 0.8d));
        }
        return calibrants;
    }

    [TestMethod]
    public void SummarizesTheMs1PrecursorsAndTheMs2ProductIonsApart() {
        var rows = AnnotationMassAccuracyReport.Summarize("s1", Calibrants());

        var ms1 = rows.Single(row => row.Pairs == AnnotationMassAccuracyReport.Ms1Pairs && row.Group == "Overall");
        var ms2 = rows.Single(row => row.Pairs == AnnotationMassAccuracyReport.Ms2Pairs && row.Group == "Overall");
        Assert.AreEqual(60, ms1.Estimate.PairCount);
        Assert.AreEqual(200, ms2.Estimate.PairCount);
        Assert.AreEqual(8d, ms1.Estimate.OffsetPpm, 1d);
        Assert.AreEqual(-20d, ms2.Estimate.OffsetPpm, 1d);
        Assert.IsTrue(ms1.Estimate.IsReliable && ms2.Estimate.IsReliable);

        CollectionAssert.AreEquivalent(
            new[] { "Overall", "m/z", "RT (min)", "MS1 intensity", "Library" },
            rows.Where(row => row.Pairs == AnnotationMassAccuracyReport.Ms1Pairs).Select(row => row.Group).Distinct().ToArray());
        CollectionAssert.AreEquivalent(
            new[] { "Overall", "Product ion m/z", "RT (min)", "MS2 intensity", "Library", "Ion" },
            rows.Where(row => row.Pairs == AnnotationMassAccuracyReport.Ms2Pairs).Select(row => row.Group).Distinct().ToArray());
        CollectionAssert.AreEqual(new[] { "lbm", "msp" }, rows.Where(row => row.Pairs == AnnotationMassAccuracyReport.Ms1Pairs && row.Group == "Library").Select(row => row.Range).ToArray());
        CollectionAssert.AreEqual(new[] { "Fragment", "Precursor" }, rows.Where(row => row.Group == "Ion").Select(row => row.Range).ToArray());
        Assert.AreEqual("0-100", rows.First(row => row.Group == "Product ion m/z").Range, "product ions below m/z 100 have their own bin");
    }

    [TestMethod]
    public void DescribesBothLevelsAndSaysWhenOneHasNothing() {
        var both = AnnotationMassAccuracyReport.Describe(AnnotationMassAccuracyReport.Summarize("s1", Calibrants()));
        StringAssert.StartsWith(both, "MS1 offset ");
        StringAssert.Contains(both, "from 60 annotated precursors; MS2 offset -");
        StringAssert.EndsWith(both, "from 200 product ions of libraries with computed fragment m/z.");

        var ms1Only = AnnotationMassAccuracyReport.Describe(AnnotationMassAccuracyReport.Summarize("s1", Calibrants().Where(c => c.MsLevel == 1).ToList()));
        StringAssert.EndsWith(ms1Only, "; MS2 no product ions of libraries with computed fragment m/z.");

        var few = AnnotationMassAccuracyReport.Describe(AnnotationMassAccuracyReport.Summarize("s1", Calibrants().Where(c => c.MsLevel == 1).Take(5).ToList()));
        StringAssert.StartsWith(few, "MS1 5 annotated precursors, fewer than 10 of them above the background: no offset");

        Assert.AreEqual("MS1 no annotated precursors; MS2 no product ions of libraries with computed fragment m/z.", AnnotationMassAccuracyReport.Describe(new List<MassAccuracyRow>()));
    }

    [TestMethod]
    public void WritesOneLinePerCalibrantWithNoTabOrLineBreakFromANameShiftingTheColumns() {
        var calibrants = Calibrants().Take(3).ToList();
        calibrants.Add(new AnnotationCalibrant(99, 2d, 2, 100.001d, 100d, AnnotationCalibrant.FromLibraryFragment, 1e3, 500d, false, "PC 34:1\t[M+H]+\r\nsecond line", "lib\tA", DataBaseSource.PredictedMsp, 0.5d));
        var text = new StringWriter(CultureInfo.InvariantCulture);

        AnnotationMassAccuracyReport.WriteCalibrants(text, calibrants);

        var lines = text.ToString().TrimEnd('\r', '\n').Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        Assert.AreEqual(5, lines.Length);
        var header = lines[0].Split('\t').ToList();
        Assert.AreEqual(17, header.Count);
        Assert.IsTrue(lines.All(line => line.Split('\t').Length == 17));
        var last = lines[4].Split('\t');
        Assert.AreEqual("10.00", last[header.IndexOf("Error (ppm)")]);
        Assert.AreEqual("PredictedMsp", last[header.IndexOf("Library kind")]);
        Assert.AreEqual("2", last[header.IndexOf("MS level")]);
    }
}
