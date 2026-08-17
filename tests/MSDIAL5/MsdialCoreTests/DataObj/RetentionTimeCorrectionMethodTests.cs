using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj.Tests;

[TestClass]
public class RetentionTimeCorrectionMethodTests {
    [TestMethod]
    public void MakeCommonStdList_ReflectsEditedSampleRetentionTime() {
        var reference = CreateReference(1, "TARGET", 5d, true);
        var excluded = CreateReference(2, "EXCLUDED", 2d, false);
        var file1 = CreateAnalysisFile((excluded, 2.5), (reference, 5.0));
        var file2 = CreateAnalysisFile((excluded, 3.5), (reference, 7.0));

        var original = RetentionTimeCorrectionMethod.MakeCommonStdList(
            new List<AnalysisFileBean> { file1, file2 },
            new List<MoleculeMsReference> { excluded, reference });

        Assert.AreEqual(6.0f, original[0].AverageRetentionTime, 1e-6);
        CollectionAssert.AreEqual(new[] { 5.0, 7.0 }, original[0].RetentionTimeList.ToArray());

        file1.RetentionTimeCorrectionBean.StandardList[1].SamplePeakAreaBean.ChromXs.RT = new RetentionTime(5.5, ChromXUnit.Min);

        var edited = RetentionTimeCorrectionMethod.MakeCommonStdList(
            new List<AnalysisFileBean> { file1, file2 },
            new List<MoleculeMsReference> { excluded, reference });

        Assert.AreEqual(6.25f, edited[0].AverageRetentionTime, 1e-6);
        CollectionAssert.AreEqual(new[] { 5.5, 7.0 }, edited[0].RetentionTimeList.ToArray());
    }

    [TestMethod]
    public void SampleMinusAverage_IgnoresExcludedStandardsAndUsesMatchingScanId() {
        var reference = CreateReference(1, "TARGET", 5d, true);
        var excluded = CreateReference(2, "EXCLUDED", 2d, false);
        var stdList = new List<StandardPair> {
            CreateStandardPair(excluded, 2.5),
            CreateStandardPair(reference, 5.5),
        };
        var commonStdList = new List<CommonStdData> {
            new CommonStdData(reference) { AverageRetentionTime = 6.0f },
        };
        var rtParam = new RetentionTimeCorrectionParam {
            InterpolationMethod = InterpolationMethod.Linear,
            ExtrapolationMethodBegin = ExtrapolationMethodBegin.FirstPoint,
            ExtrapolationMethodEnd = ExtrapolationMethodEnd.LinearExtrapolation,
        };

        var result = RetentionTimeCorrection.GetRetentionTimeCorrectionBean_SampleMinusAverage(
            rtParam,
            stdList,
            new[] { 5.0, 6.0 },
            commonStdList);

        CollectionAssert.AreEqual(new[] { 5.0, 6.0 }, result.originalRt.ToArray());
        CollectionAssert.AreEqual(new[] { -0.5, -0.5 }, result.rtDiff.ToArray());
        CollectionAssert.AreEqual(new[] { 5.5, 6.5 }, result.predictedRt.ToArray());
    }

    [TestMethod]
    public void UpdateRtCorrectionBean_RefreshesCachedPredictedRtForReferenceMode() {
        var reference = CreateReference(1, "TARGET", 5d, true);
        var stdList = new List<StandardPair> {
            CreateStandardPair(reference, 6.0),
        };
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.rtc");

        try {
            RetentionTimeCorrectionMethod.SaveRetentionCorrectionResult(
                tempPath,
                new List<double> { 5.0, 6.0, 7.0 },
                new List<double> { 9.0, 9.0, 9.0 },
                new List<double> { 1.0, 2.0, 3.0 });

            var bean = new RetentionTimeCorrectionBean(tempPath, new List<double> { 5.0, 6.0, 7.0 }) {
                StandardList = stdList,
            };
            var file = new AnalysisFileBean {
                RetentionTimeCorrectionBean = bean,
            };
            var rtParam = new RetentionTimeCorrectionParam {
                InterpolationMethod = InterpolationMethod.Linear,
                ExtrapolationMethodBegin = ExtrapolationMethodBegin.FirstPoint,
                ExtrapolationMethodEnd = ExtrapolationMethodEnd.LinearExtrapolation,
                RtDiffCalcMethod = RtDiffCalcMethod.SampleMinusReference,
            };

            CollectionAssert.AreEqual(new[] { 1.0, 2.0, 3.0 }, bean.PredictedRt.ToArray());

            RetentionTimeCorrectionMethod.UpdateRtCorrectionBean(
                new List<AnalysisFileBean> { file },
                new ParallelOptions { MaxDegreeOfParallelism = 1 },
                rtParam,
                new List<CommonStdData>());

            CollectionAssert.AreEqual(new[] { 4.0, 5.0, 6.0 }, bean.PredictedRt.ToArray());
        }
        finally {
            if (File.Exists(tempPath)) {
                File.Delete(tempPath);
            }
        }
    }

    [TestMethod]
    public void MakeCommonStdList_ThrowsWhenScanIdIsDuplicated() {
        var reference1 = CreateReference(1, "TARGET1", 5d, true);
        var reference2 = CreateReference(1, "TARGET2", 7d, true);
        var file1 = CreateAnalysisFile((reference1, 5.0), (reference2, 7.0));
        var file2 = CreateAnalysisFile((reference1, 5.5), (reference2, 7.5));

        Assert.ThrowsException<System.InvalidOperationException>(() => RetentionTimeCorrectionMethod.MakeCommonStdList(
            new List<AnalysisFileBean> { file1, file2 },
            new List<MoleculeMsReference> { reference1, reference2 }));
    }

    [TestMethod]
    public void CommonStdData_SetStandard_PopulatesRetentionStatisticsWithoutPeakSelectionResult() {
        var reference = CreateReference();
        var peak = new ChromatogramPeakFeature {
            PrecursorMz = reference.PrecursorMz,
            ChromXs = new ChromXs(5.0, ChromXType.RT, ChromXUnit.Min),
        };
        peak.PeakFeature.PeakHeightTop = 2000d;
        var std = new StandardPair {
            Reference = reference,
            SamplePeakAreaBean = peak,
            Chromatogram = new List<ChromatogramPeak>(),
        };

        var common = new CommonStdData(reference);
        common.SetStandard(std);

        Assert.AreEqual(5d, common.RetentionTimeList[0], 1e-6);
        Assert.AreEqual(1, common.NumHit);
    }

    private static AnalysisFileBean CreateAnalysisFile(params (MoleculeMsReference Reference, double Rt)[] standards) {
        return new AnalysisFileBean {
            RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean("sample.rtc") {
                StandardList = standards.Select(item => CreateStandardPair(item.Reference, item.Rt)).ToList(),
            },
        };
    }

    private static StandardPair CreateStandardPair(MoleculeMsReference reference, double rt) {
        var samplePeak = new ChromatogramPeakFeature {
            PrecursorMz = reference.PrecursorMz,
            ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
        };
        samplePeak.PeakFeature.PeakHeightTop = 2000d;

        return new StandardPair {
            Reference = reference,
            SamplePeakAreaBean = samplePeak,
            Chromatogram = new List<ChromatogramPeak>(),
        };
    }

    private static MoleculeMsReference CreateReference() {
        return CreateReference(1, "STD", 5d, true);
    }

    private static MoleculeMsReference CreateReference(int scanId, string name, double rt, bool isTarget) {
        return new MoleculeMsReference {
            ScanID = scanId,
            Name = name,
            PrecursorMz = 100d,
            ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            MassTolerance = 0.05f,
            RetentionTimeTolerance = 0.10f,
            MinimumPeakHeight = 1000f,
            IsTargetMolecule = isTarget,
        };
    }
}
