using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Chart.Tests;

[TestClass]
public class RetentionTimeCorrectionChartTests {
    [TestMethod]
    public void CalcRtDiff_SampleMinusAverage_ReturnsZeroWhenScanIdIsMissing() {
        var standard = CreateStandardPair(2, 5.5);
        var commonStdLookup = new Dictionary<int, CommonStdData> {
            [1] = new CommonStdData(CreateReference(1)) { AverageRetentionTime = 6.0f },
        };

        var diff = RetentionTimeCorrectionChart.CalcRtDiff_SampleMinusAverage(standard, commonStdLookup);

        Assert.AreEqual(0f, diff, 1e-6);
    }

    [TestMethod]
    public void CalcRtDiff_SampleMinusAverage_UsesMatchingScanId() {
        var standard = CreateStandardPair(2, 5.5);
        var commonStdLookup = new Dictionary<int, CommonStdData> {
            [2] = new CommonStdData(CreateReference(2)) { AverageRetentionTime = 6.0f },
        };

        var diff = RetentionTimeCorrectionChart.CalcRtDiff_SampleMinusAverage(standard, commonStdLookup);

        Assert.AreEqual(-30f, diff, 1e-6);
    }

    [TestMethod]
    public void GetEicDisplayRange_UsesOverlapBetweenReferenceAndPeakWindows() {
        var commonStd = new CommonStdData(CreateReference(1, rt: 5.0, rtTolerance: 0.2f)) {
            RetentionTimeList = new List<double> { 5.05, 6.0 },
            PeakWidthList = new List<double> { 0.1, 0.3 },
        };

        var (minX, maxX) = RetentionTimeCorrectionChart.GetEicDisplayRange(commonStd);

        Assert.AreEqual(4.8f, minX, 1e-6);
        Assert.AreEqual(6.2f, maxX, 1e-6);
    }

    [TestMethod]
    public void GetEicDisplayRange_FallsBackToReferenceWindowWhenNoPeakWindowOverlaps() {
        var commonStd = new CommonStdData(CreateReference(1, rt: 5.0, rtTolerance: 0.2f)) {
            RetentionTimeList = new List<double> { 7.0 },
            PeakWidthList = new List<double> { 0.1 },
        };

        var (minX, maxX) = RetentionTimeCorrectionChart.GetEicDisplayRange(commonStd);

        Assert.AreEqual(4.8f, minX, 1e-6);
        Assert.AreEqual(7.1f, maxX, 1e-6);
    }

    private static StandardPair CreateStandardPair(int scanId, double rt) {
        var reference = CreateReference(scanId);
        return new StandardPair {
            Reference = reference,
            SamplePeakAreaBean = new ChromatogramPeakFeature {
                PrecursorMz = reference.PrecursorMz,
                ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
                PeakHeightTop = 2000d,
            },
            Chromatogram = new List<ChromatogramPeak>(),
        };
    }

    private static MoleculeMsReference CreateReference(int scanId) {
        return CreateReference(scanId, 5d, 0.05f);
    }

    private static MoleculeMsReference CreateReference(int scanId, double rt, float rtTolerance) {
        return new MoleculeMsReference {
            ScanID = scanId,
            Name = $"STD-{scanId}",
            PrecursorMz = 100d,
            ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            MassTolerance = 0.05f,
            RetentionTimeTolerance = rtTolerance,
            MinimumPeakHeight = 1000f,
            IsTargetMolecule = true,
        };
    }
}
