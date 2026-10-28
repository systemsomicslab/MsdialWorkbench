#nullable enable
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The Console's light-mode alignment streams the detected peaks of a spot one file at a time and keeps the best so far. It
/// must arrive at the peak the full aligner's <see cref="DataObjConverter.GetRepresentativePeak"/> chooses, by the same
/// precedence (author decision D-A, 2026-10-28).
/// </summary>
[TestClass]
public sealed class LcmsAlignmentLightRunnerRepresentativeTests
{
    [TestMethod]
    public void AnInSilicoCandidateThatDoesNotRepresentItsPeakDoesNotMoveTheSpot() {
        var a = Peak(0, 1000, Result("acquired high", priority: 4, 1.30f, AnnotationEvidenceSource.ReferenceSpectrum));
        var b = Peak(1, 5000,
            Result("acquired low", priority: 3, 1.49f, AnnotationEvidenceSource.ReferenceSpectrum),
            Result("in silico", priority: 1, 1.70f, AnnotationEvidenceSource.BySpectrumPredictionTool));

        Assert.AreSame(a, StreamedRepresentative(a, b));
        Assert.AreSame(a, StreamedRepresentative(b, a));
        Assert.AreSame(DataObjConverter.GetRepresentativePeak([a, b]), StreamedRepresentative(a, b), "the full aligner agrees");
    }

    [TestMethod]
    public void ACompleteTieKeepsTheFirstPeakStreamedIn() {
        var first = Peak(0, 1000, Result("acquired", priority: 4, 0.9f, AnnotationEvidenceSource.ReferenceSpectrum));
        var second = Peak(1, 1000, Result("acquired", priority: 4, 0.9f, AnnotationEvidenceSource.ReferenceSpectrum));

        Assert.AreSame(first, StreamedRepresentative(first, second));
        Assert.AreSame(DataObjConverter.GetRepresentativePeak([first, second]), StreamedRepresentative(first, second));
    }

    [TestMethod]
    public void AmongEqualsTheHigherScoreThenTheTallerPeakWins() {
        var low = Peak(0, 9000, Result("acquired", priority: 4, 0.80f, AnnotationEvidenceSource.ReferenceSpectrum));
        var high = Peak(1, 1000, Result("acquired", priority: 4, 0.95f, AnnotationEvidenceSource.ReferenceSpectrum));
        Assert.AreSame(high, StreamedRepresentative(low, high));

        var shorter = Peak(0, 1000);
        var taller = Peak(1, 2000);
        Assert.AreSame(taller, StreamedRepresentative(shorter, taller));
    }

    private static AlignmentChromPeakFeature StreamedRepresentative(params AlignmentChromPeakFeature[] peaks) {
        var type = typeof(LcmsAlignmentLightRunner).GetNestedType("LightSpotAccumulator", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LightSpotAccumulator not found");
        var accumulator = Activator.CreateInstance(type, new AlignmentSpotProperty(), peaks.Length)!;
        var add = type.GetMethod("AddDetectedPeak")!;
        foreach (var peak in peaks) {
            add.Invoke(accumulator, [peak, AnalysisFileType.Sample, "A"]);
        }
        return (AlignmentChromPeakFeature)type.GetProperty("RepresentativePeak")!.GetValue(accumulator)!;
    }

    private static AlignmentChromPeakFeature Peak(int fileId, double height, params MsScanMatchResult[] results) {
        var peak = new AlignmentChromPeakFeature {
            FileID = fileId,
            PeakID = fileId,
            PeakHeightTop = height,
            Mass = 500d,
            ChromXsTop = new ChromXs(new RetentionTime(5.0, ChromXUnit.Min)),
            ChromXsLeft = new ChromXs(new RetentionTime(4.9, ChromXUnit.Min)),
            ChromXsRight = new ChromXs(new RetentionTime(5.1, ChromXUnit.Min)),
            PeakShape = new ChromatogramPeakShape(),
            MS2RawSpectrumID2CE = new Dictionary<int, double> { [100 + fileId] = 35d },
        };
        foreach (var result in results) {
            peak.MatchResults.AddResult(result);
        }
        return peak;
    }

    private static MsScanMatchResult Result(string annotator, int priority, float score, AnnotationEvidenceSource evidence) => new() {
        Name = annotator, Source = SourceType.MspDB, AnnotatorID = annotator, Priority = priority, TotalScore = score,
        IsReferenceMatched = true, IsSpectrumMatch = true, IsPrecursorMzMatch = true, EvidenceSource = evidence,
    };
}
