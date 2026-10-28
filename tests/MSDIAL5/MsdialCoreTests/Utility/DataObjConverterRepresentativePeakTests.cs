using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Utility.Tests;

/// <summary>
/// Which aligned peak represents its spot: a peak with a product-ion spectrum first, then the precedence of each peak's own
/// representative annotation (the key that chose that representative), then peak height (author decision D-A, 2026-10-28).
/// </summary>
[TestClass]
public class DataObjConverterRepresentativePeakTests
{
    private const int AcquiredLow = 3;
    private const int AcquiredHigh = 4;
    private const int InSilico = 1;

    /// <summary>
    /// The case the decision was taken on. Peak B's higher-priority acquired match represents B, so B represents the spot
    /// by tier, and it would do so without its in-silico candidate: the candidate decides nothing.
    /// </summary>
    [TestMethod]
    public void TheHigherPriorityAcquiredMatchRepresentsTheSpotAndTheInSilicoCandidateDecidesNothing() {
        var a = Peak(0, height: 1000, Acquired(AcquiredLow, 1.49f));
        var b = Peak(1, height: 1000, Acquired(AcquiredHigh, 1.30f), Predicted(InSilico, 1.70f));

        Assert.AreSame(b, DataObjConverter.GetRepresentativePeak([a, b]));
        Assert.AreSame(b, DataObjConverter.GetRepresentativePeak([b, a]), "independent of file order");
        Assert.AreEqual(AcquiredHigh, b.MatchResults.Representative.Priority, "B's own representative is its acquired match");

        var bWithoutInSilico = Peak(1, height: 1000, Acquired(AcquiredHigh, 1.30f));
        Assert.AreSame(bWithoutInSilico, DataObjConverter.GetRepresentativePeak([a, bWithoutInSilico]));
    }

    /// <summary>
    /// The regression as it was observed: a higher-scoring in-silico candidate on the peak with the lower-priority acquired
    /// match no longer moves the spot to that peak. The highest total score over all candidates chose B here.
    /// </summary>
    [TestMethod]
    public void AnInSilicoCandidateThatDoesNotRepresentItsPeakDoesNotMoveTheSpot() {
        var a = Peak(0, height: 1000, Acquired(AcquiredHigh, 1.30f));
        var b = Peak(1, height: 5000, Acquired(AcquiredLow, 1.49f), Predicted(InSilico, 1.70f));

        Assert.AreSame(a, DataObjConverter.GetRepresentativePeak([a, b]));
        Assert.AreEqual(0, DataObjConverter.GetRepresentativeFileID([a, b]));
    }

    /// <summary>Among equals -- same verdicts, evidence and tier -- the higher score still wins, then the taller peak.</summary>
    [TestMethod]
    public void AmongEqualsTheHigherScoreWinsThenTheTallerPeak() {
        var low = Peak(0, height: 9000, Acquired(AcquiredHigh, 0.80f));
        var high = Peak(1, height: 1000, Acquired(AcquiredHigh, 0.95f));
        Assert.AreSame(high, DataObjConverter.GetRepresentativePeak([low, high]));

        var shorter = Peak(0, height: 1000, Acquired(AcquiredHigh, 0.95f));
        var taller = Peak(1, height: 2000, Acquired(AcquiredHigh, 0.95f));
        Assert.AreSame(taller, DataObjConverter.GetRepresentativePeak([shorter, taller]));
    }

    [TestMethod]
    public void ACompleteTieKeepsTheFirstPeak() {
        var first = Peak(0, height: 1000, Acquired(AcquiredHigh, 0.95f));
        var second = Peak(1, height: 1000, Acquired(AcquiredHigh, 0.95f));

        Assert.AreSame(first, DataObjConverter.GetRepresentativePeak([first, second]));
        Assert.AreSame(second, DataObjConverter.GetRepresentativePeak([second, first]));
        Assert.AreEqual(0, DataObjConverter.CompareRepresentativePrecedence(first, second));
    }

    /// <summary>Without any annotation the choice is peak height alone, as it was.</summary>
    [TestMethod]
    public void WithoutAnyAnnotationTheTallerPeakWins() {
        var shorter = Peak(0, height: 1000);
        var taller = Peak(1, height: 2000);

        Assert.AreSame(taller, DataObjConverter.GetRepresentativePeak([shorter, taller]));
    }

    [TestMethod]
    public void AReferenceMatchOutranksAHigherScoringHigherPrioritySuggestion() {
        var matched = Peak(0, height: 1000, Acquired(AcquiredLow, 0.70f));
        var suggested = Peak(1, height: 9000, Suggested(Acquired(AcquiredHigh, 0.99f)));

        Assert.AreSame(matched, DataObjConverter.GetRepresentativePeak([suggested, matched]));
    }

    /// <summary>
    /// The evidence rank sits above the tier, as it does inside a peak: an accepted in-silico match does not outrank an
    /// accepted acquired one even when its library is given the higher priority.
    /// </summary>
    [TestMethod]
    public void AnAcceptedAcquiredMatchOutranksAnAcceptedInSilicoMatchOfHigherPriority() {
        var acquired = Peak(0, height: 1000, Acquired(AcquiredLow, 0.70f));
        var predicted = Peak(1, height: 9000, Predicted(priority: 10, 1.90f));

        Assert.AreSame(acquired, DataObjConverter.GetRepresentativePeak([predicted, acquired]));
    }

    [TestMethod]
    public void AManualModificationStillWins() {
        var automatic = Peak(0, height: 9000, Acquired(AcquiredHigh, 0.99f));
        var manual = Acquired(AcquiredLow, 0.40f);
        manual.Source |= SourceType.Manual;
        var curated = Peak(1, height: 1000, manual);

        Assert.AreSame(curated, DataObjConverter.GetRepresentativePeak([automatic, curated]));
    }

    /// <summary>
    /// An unknown a person set is a manual modification and wins as one, so the spot takes that file and is unknown. Pinned
    /// so that a change to it is a decision; it is listed as a question for the author.
    /// </summary>
    [TestMethod]
    public void AManualUnknownWinsAsAManualModification() {
        var automatic = Peak(0, height: 9000, Acquired(AcquiredHigh, 0.99f));
        var setUnknown = Peak(1, height: 1000, new MsScanMatchResult {
            Source = SourceType.Manual | SourceType.Unknown, EvidenceSource = AnnotationEvidenceSource.Manual,
        });

        Assert.AreSame(setUnknown, DataObjConverter.GetRepresentativePeak([automatic, setUnknown]));
    }

    /// <summary>
    /// A product-ion spectrum is still the first key, above a manual modification, as it was above the score before. Pinned
    /// for the same reason as the manual unknown.
    /// </summary>
    [TestMethod]
    public void AManualModificationOnAPeakWithoutMsmsDoesNotOutrankAPeakWithMsms() {
        var manual = Acquired(AcquiredHigh, 0.99f);
        manual.Source |= SourceType.Manual;
        var curatedWithoutMsms = Peak(0, height: 9000, manual);
        curatedWithoutMsms.MS2RawSpectrumID2CE = null;
        var automaticWithMsms = Peak(1, height: 1000);

        Assert.AreSame(automaticWithMsms, DataObjConverter.GetRepresentativePeak([curatedWithoutMsms, automaticWithMsms]));
    }

    /// <summary>Decoys never represent a peak, so a peak whose only candidates are decoys has none.</summary>
    [TestMethod]
    public void APeakWithOnlyDecoysHasNoCandidate() {
        var decoy = Acquired(AcquiredHigh, 0.99f);
        decoy.IsDecoy = true;
        var decoyOnly = Peak(0, height: 9000, decoy);
        var target = Peak(1, height: 1000, Suggested(Acquired(AcquiredLow, 0.20f)));

        Assert.AreSame(target, DataObjConverter.GetRepresentativePeak([decoyOnly, target]));
    }

    /// <summary>
    /// A peak with no candidate reports the synthetic unknown result, which is not a candidate: it loses to a real one even
    /// when that one is neither accepted nor suggested and its spectrum explained nothing.
    /// </summary>
    [TestMethod]
    public void APeakWithoutCandidatesLosesToAnyCandidate() {
        var failed = new MsScanMatchResult {
            Source = SourceType.MspDB, AnnotatorID = "acquired", Priority = AcquiredHigh, TotalScore = 0.1f,
            EvidenceSource = AnnotationEvidenceSource.UnmatchedSpectrum,
        };
        var withCandidate = Peak(1, height: 1000, failed);
        var withoutCandidate = Peak(0, height: 9000);

        Assert.AreSame(withCandidate, DataObjConverter.GetRepresentativePeak([withoutCandidate, withCandidate]));
    }

    [TestMethod]
    public void APeakWithAProductIonSpectrumOutranksEveryPeakWithout() {
        var annotated = Peak(0, height: 9000, Acquired(AcquiredHigh, 0.99f));
        annotated.MS2RawSpectrumID2CE = new Dictionary<int, double>();
        var plain = Peak(1, height: 1000);

        Assert.AreSame(plain, DataObjConverter.GetRepresentativePeak([annotated, plain]));
    }

    [TestMethod]
    public void AnEmptyAlignmentHasNoRepresentative() {
        Assert.IsNull(DataObjConverter.GetRepresentativePeak([]));
        Assert.AreEqual(-1, DataObjConverter.GetRepresentativeFileID([]));
    }

    private static AlignmentChromPeakFeature Peak(int fileId, double height, params MsScanMatchResult[] results) {
        var peak = new AlignmentChromPeakFeature {
            FileID = fileId,
            PeakID = fileId,
            PeakHeightTop = height,
            MS2RawSpectrumID2CE = new Dictionary<int, double> { [100 + fileId] = 35d },
        };
        foreach (var result in results) {
            peak.MatchResults.AddResult(result);
        }
        return peak;
    }

    private static MsScanMatchResult Acquired(int priority, float score) => new() {
        Name = $"acquired {priority}", Source = SourceType.MspDB, AnnotatorID = $"acquired {priority}", Priority = priority,
        TotalScore = score, IsReferenceMatched = true, IsSpectrumMatch = true, IsPrecursorMzMatch = true,
        EvidenceSource = AnnotationEvidenceSource.ReferenceSpectrum,
    };

    private static MsScanMatchResult Predicted(int priority, float score) => new() {
        Name = $"in silico {priority}", Source = SourceType.MspDB, AnnotatorID = $"in silico {priority}", Priority = priority,
        TotalScore = score, IsReferenceMatched = true, IsSpectrumMatch = true, IsPrecursorMzMatch = true,
        EvidenceSource = AnnotationEvidenceSource.BySpectrumPredictionTool,
    };

    private static MsScanMatchResult Suggested(MsScanMatchResult result) {
        result.IsReferenceMatched = false;
        result.IsSpectrumMatch = false;
        result.IsAnnotationSuggested = true;
        result.EvidenceSource = AnnotationEvidenceSource.WeakSpectrumMatch;
        return result;
    }
}
