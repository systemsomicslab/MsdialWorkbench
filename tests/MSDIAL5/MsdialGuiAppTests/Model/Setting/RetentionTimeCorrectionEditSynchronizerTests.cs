using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting.Tests;

[TestClass]
public class RetentionTimeCorrectionEditSynchronizerTests {
    [TestMethod]
    public void SynchronizeSampleRtEdits_WritesEditedRtBackAndRebuildsCommonSummary() {
        var reference = CreateReference();
        var file1 = CreateAnalysisFile("sample1", reference, 5.0);
        var file2 = CreateAnalysisFile("sample2", reference, 7.0);
        var analysisFiles = new List<AnalysisFileBean> { file1, file2 };

        var vm1 = CreateSampleVm(file1, 5.0f);
        var vm2 = CreateSampleVm(file2, 7.0f);
        vm1.Values[0].CanBgChange = true;
        vm2.Values[0].CanBgChange = true;
        vm1.Values[0].Rt = 5.5f;

        var rtCorrectionCommon = new RetentionTimeCorrectionCommon {
            StandardLibrary = new List<MoleculeMsReference> { reference },
        };

        var commonStdList = RetentionTimeCorrectionEditSynchronizer.SynchronizeSampleRtEdits(
            analysisFiles,
            new List<SampleListVM> { vm1, vm2 },
            rtCorrectionCommon);

        Assert.AreEqual(5.5, file1.RetentionTimeCorrectionBean.StandardList[0].SamplePeakAreaBean.ChromXsTop.RT.Value, 1e-6);
        Assert.AreEqual(7.0, file2.RetentionTimeCorrectionBean.StandardList[0].SamplePeakAreaBean.ChromXsTop.RT.Value, 1e-6);
        Assert.AreEqual(2, rtCorrectionCommon.SampleCellInfoListList.Count);
        CollectionAssert.AreEqual(
            new[] { SampleListCellInfo.ManualModified },
            rtCorrectionCommon.SampleCellInfoListList[0].ToArray());
        CollectionAssert.AreEqual(
            new[] { SampleListCellInfo.Normal },
            rtCorrectionCommon.SampleCellInfoListList[1].ToArray());
        Assert.AreEqual(6.25f, commonStdList[0].AverageRetentionTime, 1e-6);
        CollectionAssert.AreEqual(new[] { 5.5, 7.0 }, commonStdList[0].RetentionTimeList.ToArray());
    }

    [TestMethod]
    public void SynchronizeSampleRtEdits_UsesEditedResultTableValues() {
        var reference = CreateReference();
        var file = CreateAnalysisFile("sample", reference, 5.0);
        var analysisFiles = new List<AnalysisFileBean> { file };

        var vm = CreateSampleVm(file, 5.0f);
        vm.Values[0].CanBgChange = true;
        vm.Values[0].Rt = 6.0f;

        var rtCorrectionCommon = new RetentionTimeCorrectionCommon {
            StandardLibrary = new List<MoleculeMsReference> { reference },
        };

        var commonStdList = RetentionTimeCorrectionEditSynchronizer.SynchronizeSampleRtEdits(
            analysisFiles,
            new List<SampleListVM> { vm },
            rtCorrectionCommon);

        Assert.AreEqual(6.0, file.RetentionTimeCorrectionBean.StandardList[0].SamplePeakAreaBean.ChromXsTop.RT.Value, 1e-6);
        Assert.AreEqual(6.0f, commonStdList[0].AverageRetentionTime, 1e-6);
    }

    [TestMethod]
    public void SynchronizeSampleRtEdits_UpdatesSampleEditStateWithoutManualOverrideTable() {
        var reference = CreateReference();
        var file = CreateAnalysisFile("sample", reference, 5.0);
        var analysisFiles = new List<AnalysisFileBean> { file };

        var vm = CreateSampleVm(file, 5.0f);
        vm.Values[0].CanBgChange = true;
        vm.Values[0].Rt = 6.0f;

        var rtCorrectionCommon = new RetentionTimeCorrectionCommon {
            StandardLibrary = new List<MoleculeMsReference> { reference },
        };

        var commonStdList = RetentionTimeCorrectionEditSynchronizer.SynchronizeSampleRtEdits(
            analysisFiles,
            new List<SampleListVM> { vm },
            rtCorrectionCommon);

        Assert.AreEqual(6.0, file.RetentionTimeCorrectionBean.StandardList[0].SamplePeakAreaBean.ChromXsTop.RT.Value, 1e-6);
        Assert.AreEqual(6.0f, commonStdList[0].AverageRetentionTime, 1e-6);
    }

    private static AnalysisFileBean CreateAnalysisFile(string name, MoleculeMsReference reference, double rt) {
        var samplePeak = new ChromatogramPeakFeature {
            PrecursorMz = reference.PrecursorMz,
            ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            PeakHeightTop = 2000d,
        };
        return new AnalysisFileBean {
            AnalysisFileName = name,
            RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean($"{name}.rtc") {
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

    private static SampleListVM CreateSampleVm(AnalysisFileBean file, float rt) {
        var vm = new SampleListVM(file);
        vm.Values.Add(new SampleListCell { Rt = rt });
        return vm;
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
