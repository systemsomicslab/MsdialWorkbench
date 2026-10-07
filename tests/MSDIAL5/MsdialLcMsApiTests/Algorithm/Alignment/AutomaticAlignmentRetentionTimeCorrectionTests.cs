using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcMsApi.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
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
    public void AlignmentEic_ExtractsCorrectedWindowButSavesOriginalRt() {
        var model = new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(new[] {
            new AlignmentRetentionTimeCorrectionControlPoint(1, 100d, 4d, 4.2d, 1d),
            new AlignmentRetentionTimeCorrectionControlPoint(2, 100d, 6d, 6.4d, 1d),
        });
        var correction = new AlignmentRetentionTimeCorrectionCollection(
            0, new Dictionary<int, IAlignmentRetentionTimeCorrectionModel> { [1] = model });
        var alignedPeak = new AlignmentChromPeakFeature {
            FileID = 1, MasterPeakID = 1, PeakID = 1, Mass = 100d,
            ChromXsLeft = new ChromXs(model.Correct(4.9d)),
            ChromXsTop = new ChromXs(model.Correct(5d)),
            ChromXsRight = new ChromXs(model.Correct(5.1d)),
        };
        var spot = new AlignmentSpotProperty {
            TimesCenter = new ChromXs(model.Correct(5d)),
            AlignedPeakProperties = new List<AlignmentChromPeakFeature> { alignedPeak },
        };
        var spectra = new Ms1Spectra(new[] { 4.8d, 4.9d, 5d, 5.1d, 5.2d }
            .Select((rt, index) => new RawSpectrum {
                Index = index, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = rt,
                Spectrum = new[] { new RawPeakElement { Mz = 100d, Intensity = 10d + index } },
            }).ToArray(), IonMode.Positive, AcquisitionType.DDA);
        var parameter = new MsdialLcmsParameter { SmoothingLevel = 0 };
        var storage = new MsdialLcmsDataStorage { MsdialLcmsParameter = parameter };
        var accessor = new LcmsAlignmentProcessFactory(storage,
            FacadeMatchResultEvaluator.FromDataBases(DataBaseStorage.CreateEmpty())) {
            AlignmentRtCorrection = correction,
        }.CreateDataAccessor();

        var info = accessor.AccumulateChromatogram(alignedPeak, spot, spectra, 0.01f);

        Assert.AreEqual(5.3d, alignedPeak.ChromXsTop.RT.Value, 1e-5);
        Assert.AreEqual(5d, info.ChromXsTop.RT.Value, 1e-5);
        Assert.IsTrue(info.Chromatogram.Any(peak => System.Math.Abs(peak.ChromXs.RT.Value - 5d) < 1e-5));
        Assert.IsTrue(info.Chromatogram.All(peak => peak.ChromXs.RT.Value >= 4.8d && peak.ChromXs.RT.Value <= 5.2d));

        var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1")!;
        using var stream = new MemoryStream();
        serializer.SerializeN(stream, new[] { new ChromatogramSpotInfo(new[] { info }, spot.TimesCenter) }, 1);
        stream.Position = 0;
        var restored = serializer.DeserializeAt(stream, 0);
        Assert.AreEqual(5.3d, restored.ChromXs.RT.Value, 1e-5);
        Assert.AreEqual(5d, restored.PeakInfos[0].ChromXsTop.RT.Value, 1e-5);
        Assert.IsTrue(restored.PeakInfos[0].Chromatogram.Any(peak => System.Math.Abs(peak.ChromXs.RT.Value - 5d) < 1e-5));
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
            file => peaks[file.AnalysisFileId],
            _ => 0d);

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
            file => peaks[file.AnalysisFileId],
            _ => 0d);

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
            file => peaks[file.AnalysisFileId],
            _ => 0d);

        CollectionAssert.AreEquivalent(
            new[] { 100d, 200d, 300d },
            result.Anchors.Where(anchor => anchor.FileId == 0).Select(anchor => anchor.Mass).ToArray());
        Assert.IsTrue(result.Anchors.All(anchor => anchor.SampleCoverage == 1d));
        Assert.IsTrue(result.Files.All(file => file.ModelSource != AutomaticRtCorrectionModelSource.Uncorrected));
    }

    // A DDA file: each MS1 survey scan is followed by ten MS2 scans, and a scan of the other
    // polarity is ignored too. The MS1 cycle is the spacing of the MS1 scans, eleven raw spectra
    // apart, not the spacing of the raw spectrum list.
    [TestMethod]
    public void EstimateMs1CycleTime_UsesMs1ScansOnlyWhenMs2ScansAreInterleaved() {
        const double cycle = 0.0212d;
        const int ms2PerCycle = 10;
        var spectra = InterleavedSpectra(0d, 2d, cycle, ms2PerCycle).ToList();
        spectra.Add(new RawSpectrum { Index = spectra.Count, MsLevel = 1, ScanPolarity = ScanPolarity.Negative, ScanStartTime = 1.0001d });

        Assert.AreEqual(cycle, AutomaticAlignmentRetentionTimeCorrection.EstimateMs1CycleTime(spectra, IonMode.Positive), 1e-9);
        // The raw spectrum list advances once per cycle / 11: the spacing the peak-edge estimate read.
        var rawSpacing = spectra.Where(spectrum => spectrum.ScanPolarity == ScanPolarity.Positive)
            .Select(spectrum => spectrum.ScanStartTime).OrderBy(time => time).Skip(1).First();
        Assert.AreEqual(cycle / (ms2PerCycle + 1), rawSpacing, 1e-9);
        // Spectra listed out of time order give the same cycle.
        Assert.AreEqual(cycle, AutomaticAlignmentRetentionTimeCorrection.EstimateMs1CycleTime(Enumerable.Reverse(spectra), IonMode.Positive), 1e-9);
        Assert.AreEqual(0d, AutomaticAlignmentRetentionTimeCorrection.EstimateMs1CycleTime(spectra.Take(1), IonMode.Positive));
        Assert.AreEqual(0d, AutomaticAlignmentRetentionTimeCorrection.EstimateMs1CycleTime(spectra, IonMode.Negative));
    }

    [TestMethod]
    public void DefaultParameter_JudgesAnchorsWithinOneAndAHalfMinutes() {
        Assert.AreEqual(1.5f, new AutomaticAlignmentRetentionTimeCorrectionParameter().LocalSupportRtWindow, 1e-7f);
    }

    // MTBKS236 (SCIEX SWATH, MS1 cycle 0.0135 min): offsets are whole scans, most of them 0, so
    // the MAD of a file's offsets is 0. The pinned #810 skipped the test whenever MAD <= 1e-12 and
    // kept DEN_4's 5-scan offset (-0.0675 min) at anchor 5, while it rejected 1-scan offsets in
    // MDX_4. The local test with a one-scan floor rejects the first and keeps the second.
    [TestMethod]
    public void Build_ScanQuantisedOffsets_RejectFiveScanAnchorAndKeepOneScanAnchors() {
        const double cycle = 0.0135d;
        var files = new[] {
            File(0, "reference", AnalysisFileType.Sample, 1),
            File(1, "DEN_4-like", AnalysisFileType.Sample, 2),
        };
        var rts = Enumerable.Range(1, 20).Select(index => index * 0.5d).ToArray();
        var offsets = rts.Select(_ => 0d).ToArray();
        offsets[3] = cycle;            // one scan late
        offsets[7] = -cycle;           // one scan early
        offsets[12] = -5d * cycle;     // five scans: a wrong peak, not drift
        var peaks = new Dictionary<int, List<ChromatogramPeakFeature>> {
            [0] = ScannedPeaks(rts, rts.Select(_ => 0d).ToArray(), cycle),
            [1] = ScannedPeaks(rts, offsets, cycle),
        };

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, AllCandidatesAsAnchors(), 0.01d, file => peaks[file.AnalysisFileId], Ms1Cycle(cycle));

        var audit = result.Files.Single(file => file.FileId == 1);
        // The peaks' scan indexes count the interleaved MS2 scans; the floor does not come from them.
        Assert.AreEqual(cycle, audit.EstimatedScanInterval, 1e-9);
        var anchors = AnchorsOf(result, 1);
        Assert.AreEqual(20, anchors.Count);
        Assert.AreEqual("LocalOutlier", anchors[12].Status);
        Assert.IsFalse(anchors[12].Used);
        Assert.AreEqual(cycle, anchors[12].OutlierScale!.Value, 1e-9);
        Assert.AreEqual(0d, anchors[12].ExpectedOffset!.Value, 1e-9);
        Assert.IsTrue(anchors[3].Used);
        Assert.IsTrue(anchors[7].Used);
        Assert.AreEqual(19, anchors.Count(anchor => anchor.Used));
        Assert.IsTrue(anchors.All(anchor => anchor.OutlierTest == "Local" && anchor.LocalSupportCount >= 3));
        // The rejected anchor's peak is mapped by its neighbours, not pulled 5 scans off.
        Assert.AreEqual(rts[12] + 5d * cycle, result.Correction.Correct(1, rts[12] + 5d * cycle), 1e-9);
    }

    [TestMethod]
    public void Build_GlobalTestWithZeroMad_UsesScanIntervalFloorInsteadOfSkipping() {
        const double cycle = 0.0135d;
        var files = new[] {
            File(0, "reference", AnalysisFileType.Sample, 1),
            File(1, "target", AnalysisFileType.Sample, 2),
        };
        var rts = Enumerable.Range(1, 20).Select(index => index * 0.5d).ToArray();
        var offsets = rts.Select(_ => 0d).ToArray();
        offsets[5] = cycle;
        offsets[12] = -5d * cycle;
        var parameter = AllCandidatesAsAnchors();
        parameter.LocalSupportRtWindow = 0f;

        var withScans = new Dictionary<int, List<ChromatogramPeakFeature>> {
            [0] = ScannedPeaks(rts, rts.Select(_ => 0d).ToArray(), cycle),
            [1] = ScannedPeaks(rts, offsets, cycle),
        };
        var anchors = AnchorsOf(AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, file => withScans[file.AnalysisFileId], Ms1Cycle(cycle)), 1);
        Assert.IsTrue(anchors.All(anchor => anchor.OutlierTest == "Global"));
        Assert.AreEqual("MadOutlier", anchors[12].Status);
        Assert.IsTrue(anchors[5].Used);
        Assert.AreEqual(19, anchors.Count(anchor => anchor.Used));

        // Without an MS1 cycle time no floor is known; a zero scale cannot judge, so nothing is rejected.
        var unscaledResult = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, file => withScans[file.AnalysisFileId], _ => 0d);
        Assert.AreEqual(0d, unscaledResult.Files.Single(file => file.FileId == 1).EstimatedScanInterval);
        Assert.AreEqual(20, AnchorsOf(unscaledResult, 1).Count(anchor => anchor.Used));
    }

    // MTBLS417 (60 files, 20-min runs): the anchor at m/z 910.548, 17.0 min, drifts by up to
    // -0.43 min, and independent peaks around it drift the same way, yet the pinned #810 rejected
    // it as a MAD outlier in 37 of 59 files because the other anchors sit near 0. The local test
    // keeps the drifting anchors and still rejects a wrong match inside the drifting region.
    [TestMethod]
    public void Build_LateRtDependentDrift_IsKeptWhileAMismatchInsideItIsRejected() {
        const double cycle = 0.0212d;
        var files = new[] {
            File(0, "reference", AnalysisFileType.Sample, 1),
            File(1, "drifting", AnalysisFileType.Sample, 2),
        };
        var rts = Enumerable.Range(0, 37).Select(index => 1d + 0.5d * index).ToArray(); // 1.0 .. 19.0 min
        var offsets = rts.Select((rt, index) => (rt <= 13d ? 0d : -0.40d * (rt - 13d) / 6d) + (index % 3 - 1) * 0.004d).ToArray();
        var mismatch = Array.FindIndex(rts, rt => Math.Abs(rt - 17d) < 1e-9);
        offsets[mismatch] = 0.30d; // a wrong peak, against neighbours at about -0.2 min
        var peaks = new Dictionary<int, List<ChromatogramPeakFeature>> {
            [0] = ScannedPeaks(rts, rts.Select(_ => 0d).ToArray(), cycle),
            [1] = ScannedPeaks(rts, offsets, cycle),
        };
        var parameter = AllCandidatesAsAnchors();

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, file => peaks[file.AnalysisFileId], Ms1Cycle(cycle));

        var local = AnchorsOf(result, 1);
        var last = local.Count - 1;
        var late = local.Where((anchor, index) => anchor.ReferenceRt >= 15d && index != mismatch && index != last).ToList();
        Assert.AreEqual(7, late.Count);
        Assert.IsTrue(late.All(anchor => anchor.Used && anchor.OutlierTest == "Local"),
            string.Join(", ", late.Select(anchor => $"{anchor.ReferenceRt}:{anchor.Status}")));
        Assert.AreEqual("LocalOutlier", local[mismatch].Status);
        // Fewer than three neighbours: the run-wide test (with the scan floor) still decides.
        Assert.AreEqual("Global", local[last].OutlierTest);
        var at = Array.FindIndex(rts, rt => Math.Abs(rt - 16.5d) < 1e-9);
        Assert.AreEqual(rts[at], result.Correction.Correct(1, rts[at] - offsets[at]), 1e-9);

        // The same file judged only against the median of all of its anchors, as the pinned #810
        // did, loses the late drift: this is the behaviour the local test replaces.
        parameter.LocalSupportRtWindow = 0f;
        var global = AnchorsOf(AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, file => peaks[file.AnalysisFileId], Ms1Cycle(cycle)), 1);
        Assert.IsTrue(global.Where(anchor => anchor.ReferenceRt >= 15d).All(anchor => anchor.Status == "MadOutlier"),
            string.Join(", ", global.Select(anchor => $"{anchor.ReferenceRt}:{anchor.Status}")));
    }

    [TestMethod]
    public void WriteAudit_AddsOutlierAndCoverageColumnsAfterTheOriginalOnes() {
        const double cycle = 0.0135d;
        var files = new[] {
            File(0, "reference", AnalysisFileType.Sample, 1),
            File(1, "target", AnalysisFileType.Sample, 2),
        };
        var rts = Enumerable.Range(1, 20).Select(index => index * 0.5d).ToArray();
        var offsets = rts.Select(_ => 0d).ToArray();
        offsets[12] = -5d * cycle;
        var peaks = new Dictionary<int, List<ChromatogramPeakFeature>> {
            [0] = ScannedPeaks(rts, rts.Select(_ => 0d).ToArray(), cycle),
            [1] = ScannedPeaks(rts, offsets, cycle),
        };
        peaks[1].Add(ScannedPeak(99, 999d, 0.2d, 5000d, cycle));  // before the first anchor
        peaks[1].Add(ScannedPeak(98, 998d, 11.0d, 5000d, cycle)); // after the last anchor
        peaks[1].Add(ScannedPeak(97, 997d, 12.0d, 5000d, cycle));

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, AllCandidatesAsAnchors(), 0.01d, file => peaks[file.AnalysisFileId], Ms1Cycle(cycle));
        var audit = result.Files.Single(file => file.FileId == 1);
        Assert.AreEqual(0.5d, audit.FirstAnchorRt!.Value, 1e-9);
        Assert.AreEqual(10d, audit.LastAnchorRt!.Value, 1e-9);
        Assert.AreEqual(1, audit.PeaksBeforeFirstAnchor);
        Assert.AreEqual(2, audit.PeaksAfterLastAnchor);

        var folder = Path.Combine(Path.GetTempPath(), "autort-audit-" + Guid.NewGuid().ToString("N"));
        try {
            result.WriteAudit(folder);
            var summary = System.IO.File.ReadAllLines(Path.Combine(folder, "automatic_alignment_rt_correction_summary.tsv"));
            var summaryHeader = summary[0].Split('\t');
            CollectionAssert.AreEqual(
                new[] { "Note", "Estimated scan interval (min)", "First used anchor RT (min)", "Last used anchor RT (min)", "Peaks before first used anchor", "Peaks after last used anchor" },
                summaryHeader.Skip(11).ToArray());
            Assert.IsTrue(summary.Skip(1).All(line => line.Split('\t').Length == summaryHeader.Length));

            var anchorLines = System.IO.File.ReadAllLines(Path.Combine(folder, "automatic_alignment_rt_correction_anchors.tsv"));
            var anchorHeader = anchorLines[0].Split('\t');
            CollectionAssert.AreEqual(
                new[] { "Status", "Outlier test", "Local support count", "Expected offset (min)", "Outlier scale (min)" },
                anchorHeader.Skip(10).ToArray());
            Assert.IsTrue(anchorLines.Skip(1).All(line => line.Split('\t').Length == anchorHeader.Length));
            Assert.AreEqual(1, anchorLines.Count(line => line.Split('\t')[10] == "LocalOutlier"));
        }
        finally {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    private static List<AutomaticRtCorrectionAnchorAudit> AnchorsOf(AutomaticAlignmentRetentionTimeCorrectionResult result, int fileId) {
        return result.Anchors.Where(anchor => anchor.FileId == fileId).OrderBy(anchor => anchor.ReferenceRt).ToList();
    }

    private static AutomaticAlignmentRetentionTimeCorrectionParameter AllCandidatesAsAnchors() {
        return new AutomaticAlignmentRetentionTimeCorrectionParameter {
            Execute = true,
            ReferenceFileId = 0,
            RtBinWidth = 0.5f,
            MatchRtTolerance = 0.5f,
            MinimumAnchorCount = 3,
            MaximumAnchorCount = 100,
            MinimumSampleCoverage = 0.5f,
            IntensityQuantile = 0f,
            MaximumPeakWidthQuantile = 1f,
            MinimumSignalToNoise = 3f,
        };
    }

    // The MS1 cycle time of a DDA file whose MS1 scans are `cycle` apart with MS2 scans between
    // them, computed as the Console computes it: from the scans' retention times.
    private static Func<AnalysisFileBean, double> Ms1Cycle(double cycle) {
        var time = AutomaticAlignmentRetentionTimeCorrection.EstimateMs1CycleTime(InterleavedSpectra(0d, 20d, cycle, ScannedPeakMs2PerCycle), IonMode.Positive);
        return _ => time;
    }

    private static IEnumerable<RawSpectrum> InterleavedSpectra(double start, double end, double cycle, int ms2PerCycle) {
        var index = 0;
        for (var scan = 0; start + scan * cycle <= end; scan++) {
            var rt = start + scan * cycle;
            yield return new RawSpectrum { Index = index++, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = rt };
            for (var ms2 = 1; ms2 <= ms2PerCycle; ms2++) {
                yield return new RawSpectrum { Index = index++, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, ScanStartTime = rt + ms2 * cycle / (ms2PerCycle + 1) };
            }
        }
    }

    private const int ScannedPeakMs2PerCycle = 10;

    // Peaks at the reference RT minus the file's offset (offset = reference RT - original RT),
    // each eight MS1 scans wide. Their scan indexes are raw spectrum indexes, as peak spotting
    // sets them: ten MS2 scans follow each MS1 scan, so the indexes advance 11 per MS1 scan.
    private static List<ChromatogramPeakFeature> ScannedPeaks(IReadOnlyList<double> referenceRts, IReadOnlyList<double> offsets, double cycle) {
        return referenceRts
            .Select((rt, index) => ScannedPeak(index, 100d + 10d * index, rt - offsets[index], 10000d + index, cycle))
            .ToList();
    }

    private static ChromatogramPeakFeature ScannedPeak(int id, double mz, double rt, double height, double cycle) {
        var feature = Peak(id, mz, rt, height);
        const int stride = ScannedPeakMs2PerCycle + 1;
        var top = (int)Math.Round(rt / cycle) * stride;
        feature.PeakFeature.ChromXsLeft = new ChromXs(rt - 4d * cycle, ChromXType.RT, ChromXUnit.Min);
        feature.PeakFeature.ChromXsRight = new ChromXs(rt + 4d * cycle, ChromXType.RT, ChromXUnit.Min);
        feature.PeakFeature.ChromScanIdLeft = top - 4 * stride;
        feature.PeakFeature.ChromScanIdTop = top;
        feature.PeakFeature.ChromScanIdRight = top + 4 * stride;
        return feature;
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
