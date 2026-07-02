using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Tests;

[TestClass]
public class RetentionTimeCorrectionPeakSelectorTests {
    [TestMethod]
    public void Select_ReturnsSingleCandidateWhenOnlyOnePassesFilters() {
        var reference = CreateReference();
        var peak = CreatePeak(mass: 100.01, rt: 5.02, height: 1500d);

        var result = RetentionTimeCorrectionPeakSelector.Select(reference, new[] { peak });

        Assert.IsTrue(result.HasSelection);
        Assert.AreSame(peak, result.SelectedPeak);
        Assert.AreEqual(RetentionTimeCorrectionPeakSelectionReason.SelectedSingleCandidate, result.SelectedReason);
        Assert.AreEqual(1, result.Candidates.Count);
        Assert.AreEqual(RetentionTimeCorrectionPeakRejectReason.None, result.Candidates[0].RejectReason);
        Assert.AreEqual(0, result.RejectedCandidates.Count);
    }

    [TestMethod]
    public void Select_PicksHighestPeakHeightAmongAcceptedCandidates() {
        var reference = CreateReference();
        var lowPeak = CreatePeak(mass: 100.01, rt: 5.01, height: 1200d);
        var highPeak = CreatePeak(mass: 100.00, rt: 5.03, height: 2200d);

        var result = RetentionTimeCorrectionPeakSelector.Select(reference, new[] { lowPeak, highPeak });

        Assert.IsTrue(result.HasSelection);
        Assert.AreSame(highPeak, result.SelectedPeak);
        Assert.AreEqual(RetentionTimeCorrectionPeakSelectionReason.SelectedByHighestPeakHeight, result.SelectedReason);
        Assert.AreEqual(2, result.Candidates.Count);
        Assert.AreEqual(0, result.RejectedCandidates.Count);
    }

    [TestMethod]
    public void Select_PreservesSelectedPeakForCommonStandardSummaries() {
        var reference = CreateReference();
        var selectedPeak = CreatePeak(mass: 100.00, rt: 5.03, height: 2200d);
        var selection = new RetentionTimeCorrectionPeakSelectionResult(
            reference,
            selectedPeak,
            new[] {
                new RetentionTimeCorrectionPeakCandidateResult(
                    selectedPeak,
                    0.00d,
                    0.03d,
                    RetentionTimeCorrectionPeakRejectReason.None),
            },
            RetentionTimeCorrectionPeakSelectionReason.SelectedSingleCandidate);

        var standardPair = new StandardPair {
            Reference = reference,
            SamplePeakAreaBean = selectedPeak,
            Chromatogram = new List<ChromatogramPeak>(),
        };

        var commonStd = new CommonStdData(reference);
        commonStd.SetStandard(standardPair);

        Assert.AreEqual(5.03d, commonStd.RetentionTimeList[0], 1e-6);
        Assert.AreEqual(1, commonStd.NumHit);
    }

    [TestMethod]
    public void Select_RejectsCandidatesOutsideToleranceOrBelowMinimumHeight() {
        var reference = CreateReference();
        var candidate = CreatePeak(mass: 100.12, rt: 5.12, height: 900d);

        var result = RetentionTimeCorrectionPeakSelector.Select(reference, new[] { candidate });

        Assert.IsFalse(result.HasSelection);
        Assert.IsNull(result.SelectedPeak);
        Assert.AreEqual(RetentionTimeCorrectionPeakSelectionReason.NoCandidates, result.SelectedReason);
        Assert.AreEqual(1, result.Candidates.Count);
        Assert.AreEqual(
            RetentionTimeCorrectionPeakRejectReason.MassTolerance
            | RetentionTimeCorrectionPeakRejectReason.RetentionTimeTolerance
            | RetentionTimeCorrectionPeakRejectReason.MinimumPeakHeight,
            result.Candidates[0].RejectReason);
        Assert.AreEqual(1, result.RejectedCandidates.Count);
    }

    [TestMethod]
    public void Select_RejectsCandidateOutsideMassToleranceEvenIfRtAndHeightPass() {
        var reference = CreateReference();
        var candidate = CreatePeak(mass: 100.06, rt: 5.02, height: 1500d);

        var result = RetentionTimeCorrectionPeakSelector.Select(reference, new[] { candidate });

        Assert.IsFalse(result.HasSelection);
        Assert.AreEqual(RetentionTimeCorrectionPeakRejectReason.MassTolerance, result.Candidates[0].RejectReason);
    }

    [TestMethod]
    public void Select_RejectsCandidateOutsideRtToleranceEvenIfMassAndHeightPass() {
        var reference = CreateReference();
        var candidate = CreatePeak(mass: 100.00, rt: 5.11, height: 1500d);

        var result = RetentionTimeCorrectionPeakSelector.Select(reference, new[] { candidate });

        Assert.IsFalse(result.HasSelection);
        Assert.AreEqual(RetentionTimeCorrectionPeakRejectReason.RetentionTimeTolerance, result.Candidates[0].RejectReason);
    }

    [TestMethod]
    public void Select_ReturnsEmptySelectionWhenNoCandidatesAreProvided() {
        var reference = CreateReference();

        var result = RetentionTimeCorrectionPeakSelector.Select(reference, Enumerable.Empty<ChromatogramPeakFeature>());

        Assert.IsFalse(result.HasSelection);
        Assert.IsNull(result.SelectedPeak);
        Assert.AreEqual(RetentionTimeCorrectionPeakSelectionReason.NoCandidates, result.SelectedReason);
        Assert.AreEqual(0, result.Candidates.Count);
        Assert.AreEqual(0, result.RejectedCandidates.Count);
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

    private static ChromatogramPeakFeature CreatePeak(double mass, double rt, double height) {
        return new ChromatogramPeakFeature {
            PrecursorMz = mass,
            ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            PeakHeightTop = height,
        };
    }
}
