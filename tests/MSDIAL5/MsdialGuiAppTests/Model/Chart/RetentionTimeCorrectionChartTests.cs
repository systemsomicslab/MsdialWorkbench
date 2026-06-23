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
        return new MoleculeMsReference {
            ScanID = scanId,
            Name = $"STD-{scanId}",
            PrecursorMz = 100d,
            ChromXs = new ChromXs(5d, ChromXType.RT, ChromXUnit.Min),
            MassTolerance = 0.05f,
            RetentionTimeTolerance = 0.10f,
            MinimumPeakHeight = 1000f,
            IsTargetMolecule = true,
        };
    }
}
