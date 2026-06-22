using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting.Tests;

[TestClass]
public class RetentionTimeCorrectionPeakSelectionEditPresenterTests {
    [TestMethod]
    public void CreateRows_MapsPerSampleStandardRowsAndAppliesManualRt() {
        var reference = CreateReference();
        var samplePeak = CreatePeak(100.01, 5.00, 1500d);
        var selection = new RetentionTimeCorrectionPeakSelectionResult(
            reference,
            samplePeak,
            new[] {
                new RetentionTimeCorrectionPeakCandidateResult(
                    samplePeak,
                    0.01d,
                    0.00d,
                    RetentionTimeCorrectionPeakRejectReason.None),
            },
            RetentionTimeCorrectionPeakSelectionReason.SelectedSingleCandidate);

        var analysisFile = new AnalysisFileBean {
            AnalysisFileName = "sample1",
            RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean("sample1.rtc") {
                StandardList = new List<StandardPair> {
                    new StandardPair {
                        Reference = reference,
                        SamplePeakAreaBean = samplePeak,
                        Chromatogram = new List<ChromatogramPeak>(),
                        PeakSelectionResult = selection,
                    },
                },
            },
        };

        int appliedSampleIndex = -1;
        int appliedStandardIndex = -1;
        double appliedRt = -1d;
        var rows = RetentionTimeCorrectionPeakSelectionEditPresenter.CreateRows(
            new[] { analysisFile },
            (sampleIndex, standardIndex, rt) => {
                appliedSampleIndex = sampleIndex;
                appliedStandardIndex = standardIndex;
                appliedRt = rt;
            });

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("sample1", rows[0].SampleName);
        Assert.AreEqual("STD", rows[0].StandardName);
        Assert.AreEqual(5d, rows[0].CurrentRt, 1e-6);

        rows[0].ManualRt = 5.75;
        rows[0].ApplyManualRtCommand.Execute(null);

        Assert.AreEqual(0, appliedSampleIndex);
        Assert.AreEqual(0, appliedStandardIndex);
        Assert.AreEqual(5.75d, appliedRt, 1e-6);
    }

    [TestMethod]
    public void CreateRows_ReturnsEmptyWhenNoAnalysisFilesExist() {
        var rows = RetentionTimeCorrectionPeakSelectionEditPresenter.CreateRows(new List<AnalysisFileBean>(), (_, _, _) => { });

        Assert.AreEqual(0, rows.Count);
    }

    private static MoleculeMsReference CreateReference() {
        return new MoleculeMsReference {
            ScanID = 10,
            Name = "STD",
            PrecursorMz = 100d,
            ChromXs = new ChromXs(5d, ChromXType.RT, ChromXUnit.Min),
            MassTolerance = 0.05f,
            RetentionTimeTolerance = 0.10f,
            MinimumPeakHeight = 1000f,
            IsTargetMolecule = true,
        };
    }

    private static ChromatogramPeakFeature CreatePeak(double mass, double rt, double height) {
        return new ChromatogramPeakFeature {
            PrecursorMz = mass,
            ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            PeakHeightTop = height,
        };
    }
}
