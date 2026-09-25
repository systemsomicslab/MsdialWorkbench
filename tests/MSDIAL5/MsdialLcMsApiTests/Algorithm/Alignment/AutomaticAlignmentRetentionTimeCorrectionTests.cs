using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests;

[TestClass]
public class AutomaticAlignmentRetentionTimeCorrectionTests {
    [TestMethod]
    public void PiecewiseLinearModel_CorrectAndRestoreRoundTrip() {
        var model = new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(new[] {
            new AlignmentRetentionTimeCorrectionControlPoint(1, 100d, 1.2d, 1d, 1d),
            new AlignmentRetentionTimeCorrectionControlPoint(2, 200d, 5.1d, 5d, 1d),
            new AlignmentRetentionTimeCorrectionControlPoint(3, 300d, 8.8d, 9d, 1d),
        });

        Assert.AreEqual(1d, model.Correct(1.2d), 1e-10);
        Assert.AreEqual(5d, model.Correct(5.1d), 1e-10);
        Assert.AreEqual(9d, model.Correct(8.8d), 1e-10);
        foreach (var original in new[] { 0.5d, 2d, 6d, 10d }) {
            Assert.AreEqual(original, model.Restore(model.Correct(original)), 1e-9);
        }
    }

    [TestMethod]
    public void Build_SelectsCentralReferenceCorrectsSamplesAndInterpolatesBlank() {
        var files = new[] {
            File(0, "early", AnalysisFileType.Sample, 1),
            File(1, "middle", AnalysisFileType.QC, 2),
            File(3, "blank", AnalysisFileType.Blank, 3),
            File(2, "late", AnalysisFileType.Sample, 4),
        };
        var peaks = new Dictionary<int, List<ChromatogramPeakFeature>> {
            [0] = Peaks(-0.2d),
            [1] = Peaks(0d),
            [2] = Peaks(0.2d),
            [3] = new List<ChromatogramPeakFeature>(),
        };
        var parameter = new AutomaticAlignmentRetentionTimeCorrectionParameter {
            Execute = true,
            RtBinWidth = 0.5f,
            MatchRtTolerance = 0.5f,
            MinimumAnchorCount = 3,
            MaximumAnchorCount = 3,
            MinimumSampleCoverage = 0.66f,
            IntensityQuantile = 0f,
            MaximumPeakWidthQuantile = 1f,
            MinimumSignalToNoise = 3f,
            ReferenceCentralityWeight = 0.9f,
            InterpolateBlankByAnalyticalOrder = true,
        };

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(
            files,
            parameter,
            0.01d,
            file => peaks[file.AnalysisFileId]);

        Assert.AreEqual(1, result.Correction.ReferenceFileId);
        Assert.AreEqual(1d, result.Correction.Correct(0, 0.8d), 1e-8);
        Assert.AreEqual(1d, result.Correction.Correct(1, 1d), 1e-8);
        Assert.AreEqual(1d, result.Correction.Correct(2, 1.2d), 1e-8);
        Assert.AreEqual(5d, result.Correction.Correct(3, 5.1d), 1e-8);

        Assert.AreEqual(AutomaticRtCorrectionModelSource.Reference, result.Files[1].ModelSource);
        Assert.AreEqual(AutomaticRtCorrectionModelSource.InterpolatedBlank, result.Files[2].ModelSource);
        Assert.AreEqual(3, result.Files[0].UsedAnchorCount);
        Assert.AreEqual(12, result.Anchors.Count);
    }

    [TestMethod]
    public void Build_DoesNotReportAnchorsAsUsedWhenFileHasTooFewMatches() {
        var files = new[] {
            File(0, "reference", AnalysisFileType.QC, 1),
            File(1, "incomplete", AnalysisFileType.Sample, 2),
        };
        var peaks = new Dictionary<int, List<ChromatogramPeakFeature>> {
            [0] = Peaks(0d),
            [1] = new List<ChromatogramPeakFeature> {
                Peak(0, 100d, 1.1d, 10000d),
                Peak(1, 200d, 5.1d, 20000d),
            },
        };
        var parameter = new AutomaticAlignmentRetentionTimeCorrectionParameter {
            Execute = true,
            ReferenceFileId = 0,
            RtBinWidth = 0.5f,
            MatchRtTolerance = 0.5f,
            MinimumAnchorCount = 3,
            MaximumAnchorCount = 3,
            MinimumSampleCoverage = 0.5f,
            IntensityQuantile = 0f,
            MaximumPeakWidthQuantile = 1f,
            MinimumSignalToNoise = 3f,
        };

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(
            files,
            parameter,
            0.01d,
            file => peaks[file.AnalysisFileId]);

        var audit = result.Files.Single(file => file.FileId == 1);
        Assert.AreEqual(AutomaticRtCorrectionModelSource.Uncorrected, audit.ModelSource);
        Assert.AreEqual(0, audit.UsedAnchorCount);
        Assert.IsFalse(result.Correction.Models.ContainsKey(1));
        Assert.IsFalse(result.Anchors.Where(anchor => anchor.FileId == 1).Any(anchor => anchor.Used));
        Assert.IsTrue(result.Anchors.Where(anchor => anchor.FileId == 1 && anchor.OriginalRt.HasValue)
            .All(anchor => anchor.Status == "InsufficientAnchors"));
    }

    [TestMethod]
    public void Build_PrefersUbiquitousAnchorsOverStrongerGroupSpecificPeaksInTheSameRtBins() {
        var files = new[] {
            File(0, "reference", AnalysisFileType.QC, 1),
            File(1, "sample1", AnalysisFileType.Sample, 2),
            File(2, "sample2", AnalysisFileType.Sample, 3),
        };
        var referencePeaks = new List<ChromatogramPeakFeature>();
        referencePeaks.AddRange(Peaks(0d));
        referencePeaks.Add(Peak(10, 101d, 1.1d, 100000d));
        referencePeaks.Add(Peak(11, 201d, 5.1d, 100000d));
        referencePeaks.Add(Peak(12, 301d, 9.1d, 100000d));
        var peaks = new Dictionary<int, List<ChromatogramPeakFeature>> {
            [0] = referencePeaks,
            [1] = Peaks(0.1d),
            [2] = Peaks(-0.1d),
        };
        var parameter = new AutomaticAlignmentRetentionTimeCorrectionParameter {
            Execute = true,
            ReferenceFileId = 0,
            RtBinWidth = 0.5f,
            MatchRtTolerance = 0.5f,
            MinimumAnchorCount = 3,
            MaximumAnchorCount = 3,
            MinimumSampleCoverage = 0.5f,
            IntensityQuantile = 0f,
            MaximumPeakWidthQuantile = 1f,
            MinimumSignalToNoise = 3f,
        };

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(
            files,
            parameter,
            0.01d,
            file => peaks[file.AnalysisFileId]);

        CollectionAssert.AreEquivalent(
            new[] { 100d, 200d, 300d },
            result.Anchors.Where(anchor => anchor.FileId == 0).Select(anchor => anchor.Mass).ToArray());
        Assert.IsTrue(result.Anchors.All(anchor => anchor.SampleCoverage == 1d));
        Assert.IsTrue(result.Files.All(file => file.ModelSource != AutomaticRtCorrectionModelSource.Uncorrected));
    }

    private static AnalysisFileBean File(int id, string name, AnalysisFileType type, int order) {
        return new AnalysisFileBean {
            AnalysisFileId = id,
            AnalysisFileName = name,
            AnalysisFileType = type,
            AnalysisFileAnalyticalOrder = order,
        };
    }

    private static List<ChromatogramPeakFeature> Peaks(double shift) {
        return new List<ChromatogramPeakFeature> {
            Peak(0, 100d, 1d + shift, 10000d),
            Peak(1, 200d, 5d + shift, 20000d),
            Peak(2, 300d, 9d + shift, 15000d),
        };
    }

    private static ChromatogramPeakFeature Peak(int id, double mz, double rt, double height) {
        var feature = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature {
            ChromXsLeft = new ChromXs(rt - 0.05d, ChromXType.RT, ChromXUnit.Min),
            ChromXsTop = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            ChromXsRight = new ChromXs(rt + 0.05d, ChromXType.RT, ChromXUnit.Min),
            PeakHeightLeft = height * 0.1d,
            PeakHeightTop = height,
            PeakHeightRight = height * 0.1d,
            Mass = mz,
        }) {
            PeakID = id,
            MasterPeakID = id,
            PeakShape = new ChromatogramPeakShape {
                SignalToNoise = 20f,
                GaussianSimilarityValue = 0.95f,
                IdealSlopeValue = 0.95f,
                SymmetryValue = 0.95f,
            },
        };
        return feature;
    }
}
