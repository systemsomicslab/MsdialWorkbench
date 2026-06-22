using CompMs.Common.Components;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj.Tests;

[TestClass]
public class RetentionTimeCorrectionMethodTests {
    [TestMethod]
    public void MakeCommonStdList_ReflectsEditedSampleRetentionTime() {
        var reference = CreateReference();
        var file1 = CreateAnalysisFile(5.0, reference);
        var file2 = CreateAnalysisFile(7.0, reference);

        var original = RetentionTimeCorrectionMethod.MakeCommonStdList(
            new List<AnalysisFileBean> { file1, file2 },
            new List<MoleculeMsReference> { reference });

        Assert.AreEqual(6.0f, original[0].AverageRetentionTime, 1e-6);
        CollectionAssert.AreEqual(new[] { 5.0, 7.0 }, original[0].RetentionTimeList.ToArray());

        file1.RetentionTimeCorrectionBean.StandardList[0].SamplePeakAreaBean.ChromXs.RT = new RetentionTime(5.5, ChromXUnit.Min);

        var edited = RetentionTimeCorrectionMethod.MakeCommonStdList(
            new List<AnalysisFileBean> { file1, file2 },
            new List<MoleculeMsReference> { reference });

        Assert.AreEqual(6.25f, edited[0].AverageRetentionTime, 1e-6);
        CollectionAssert.AreEqual(new[] { 5.5, 7.0 }, edited[0].RetentionTimeList.ToArray());
    }

    private static AnalysisFileBean CreateAnalysisFile(double rt, MoleculeMsReference reference) {
        var samplePeak = new ChromatogramPeakFeature {
            PrecursorMz = reference.PrecursorMz,
            ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            PeakHeightTop = 2000d,
        };

        return new AnalysisFileBean {
            RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean("sample.rtc") {
                StandardList = new List<StandardPair> {
                    new StandardPair {
                        Reference = reference,
                        SamplePeakAreaBean = samplePeak,
                        Chromatogram = new List<ChromatogramPeak>(),
                    },
                },
            },
        };
    }

    private static MoleculeMsReference CreateReference() {
        return new MoleculeMsReference {
            ScanID = 1,
            Name = "STD",
            PrecursorMz = 100d,
            ChromXs = new ChromXs(5d, ChromXType.RT, ChromXUnit.Min),
            MassTolerance = 0.05f,
            RetentionTimeTolerance = 0.10f,
            MinimumPeakHeight = 1000f,
            IsTargetMolecule = true,
        };
    }
}
