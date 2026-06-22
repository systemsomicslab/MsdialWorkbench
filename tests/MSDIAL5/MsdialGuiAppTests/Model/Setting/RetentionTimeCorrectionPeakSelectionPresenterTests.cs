using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting.Tests;

[TestClass]
public class RetentionTimeCorrectionPeakSelectionPresenterTests {
    [TestMethod]
    public void CreateRows_MapsSelectionResultToReadOnlySummaryRows() {
        var reference = CreateReference();
        var selectedPeak = CreatePeak(100.01, 5.02, 2000d);
        var rejectedPeak = CreatePeak(100.08, 5.20, 500d);
        var selection = new RetentionTimeCorrectionPeakSelectionResult(
            reference,
            selectedPeak,
            new[] {
                new RetentionTimeCorrectionPeakCandidateResult(
                    selectedPeak,
                    0.01d,
                    0.02d,
                    RetentionTimeCorrectionPeakRejectReason.None),
                new RetentionTimeCorrectionPeakCandidateResult(
                    rejectedPeak,
                    0.08d,
                    0.20d,
                    RetentionTimeCorrectionPeakRejectReason.MassTolerance | RetentionTimeCorrectionPeakRejectReason.RetentionTimeTolerance | RetentionTimeCorrectionPeakRejectReason.MinimumPeakHeight),
            },
            RetentionTimeCorrectionPeakSelectionReason.SelectedByHighestPeakHeight);

        var rows = RetentionTimeCorrectionPeakSelectionPresenter.CreateRows(new[] {
            new CommonStdData(reference) { PeakSelectionResult = selection },
        });

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(reference.ScanID, rows[0].ReferenceId);
        Assert.AreEqual(reference.Name, rows[0].StandardName);
        Assert.AreEqual(5d, rows[0].ReferenceRt, 1e-6);
        Assert.AreEqual(5.02d, rows[0].SelectedRt, 1e-6);
        Assert.AreEqual(100.01d, rows[0].SelectedMz, 1e-6);
        Assert.AreEqual(2000d, rows[0].SelectedPeakHeight, 1e-6);
        Assert.AreEqual(2, rows[0].CandidateCount);
        Assert.AreEqual(1, rows[0].RejectedCount);
        Assert.AreEqual("SelectedByHighestPeakHeight", rows[0].SelectedReason);
    }

    [TestMethod]
    public void CreateRows_ReturnsEmptyWhenNoCommonStandardsExist() {
        var rows = RetentionTimeCorrectionPeakSelectionPresenter.CreateRows(new List<CommonStdData>());

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
