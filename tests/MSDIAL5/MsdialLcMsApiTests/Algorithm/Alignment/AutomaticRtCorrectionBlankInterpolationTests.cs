using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests;

/// <summary>
/// A Blank's RT map, interpolated from the injections next to it by analytical order, within its
/// analysis batch (decision 2 of the final review of the automatic alignment RT correction:
/// CONF-20, CONF-60, CONF-63).
/// </summary>
[TestClass]
public class AutomaticRtCorrectionBlankInterpolationTests {
    // Three user anchors at 2, 6 and 10 min in the reference file; every other file is shifted
    // by a constant, so each file's map is a constant offset and a blend of two maps is the
    // weighted mean of their offsets.
    private static readonly MoleculeMsReference[] Library = {
        Anchor(0, "A", 150d, 2d),
        Anchor(1, "B", 250d, 6d),
        Anchor(2, "C", 350d, 10d),
    };

    [TestMethod]
    public void OrdersThatRestartPerBatch_TakeTheNeighboursFromTheBlanksOwnBatch() {
        var files = new[] {
            File(0, "S1", AnalysisFileType.Sample, batch: 1, order: 1),
            File(1, "B1", AnalysisFileType.Blank, batch: 1, order: 2),
            File(2, "S2", AnalysisFileType.Sample, batch: 1, order: 3),
            File(3, "S3", AnalysisFileType.Sample, batch: 2, order: 1),
            File(4, "B2", AnalysisFileType.Blank, batch: 2, order: 2),
            File(5, "S4", AnalysisFileType.Sample, batch: 2, order: 3),
        };
        var shifts = new Dictionary<int, double> { [0] = 0d, [1] = 0d, [2] = 0.2d, [3] = 0.4d, [4] = 0d, [5] = 0.6d };

        var result = Build(files, shifts);

        var b1 = result.Files.Single(file => file.FileId == 1);
        Assert.AreEqual(AutomaticRtCorrectionModelSource.InterpolatedBlank, b1.ModelSource);
        Assert.AreEqual("Interpolated from file IDs 0 and 2 (analysis batch 1).", b1.Note);
        Assert.AreEqual(6d, result.Correction.Correct(1, 6.1d), 1e-9, "half way between S1 (0) and S2 (+0.2)");
        var b2 = result.Files.Single(file => file.FileId == 4);
        Assert.AreEqual("Interpolated from file IDs 3 and 5 (analysis batch 2).", b2.Note);
        Assert.AreEqual(6d, result.Correction.Correct(4, 6.5d), 1e-9, "half way between S3 (+0.4) and S4 (+0.6)");
        Assert.AreEqual(0, result.Warnings.Count);

        var folder = Path.Combine(Path.GetTempPath(), "autort-blank-batch-" + Guid.NewGuid().ToString("N"));
        try {
            result.WriteAudit(folder);
            var lines = System.IO.File.ReadAllLines(Path.Combine(folder, AutomaticAlignmentRetentionTimeCorrectionResult.SummaryFileName));
            var header = lines[0].Split('\t');
            Assert.AreEqual("Analysis batch", header[^1]);
            var rows = lines.Skip(1).Select(line => line.Split('\t')).ToList();
            CollectionAssert.AreEqual(new[] { "0", "1", "2", "3", "4", "5" }, rows.Select(cells => cells[0]).ToArray(), "batch, then order");
            CollectionAssert.AreEqual(new[] { "1", "1", "1", "2", "2", "2" }, rows.Select(cells => cells[^1]).ToArray());
        }
        finally {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void ABlankAfterTheLastInjectionOfItsBatch_TakesTheNearestFileOfThatBatch() {
        var files = new[] {
            File(0, "S1", AnalysisFileType.Sample, batch: 1, order: 1),
            File(1, "S2", AnalysisFileType.Sample, batch: 1, order: 2),
            File(2, "B1", AnalysisFileType.Blank, batch: 1, order: 3),
            File(3, "S3", AnalysisFileType.Sample, batch: 2, order: 4),
        };
        var shifts = new Dictionary<int, double> { [0] = 0d, [1] = 0.2d, [2] = 0d, [3] = 0.5d };

        var result = Build(files, shifts);

        var blank = result.Files.Single(file => file.FileId == 2);
        Assert.AreEqual(AutomaticRtCorrectionModelSource.NearestBlank, blank.ModelSource);
        Assert.AreEqual("Used nearest non-Blank model from file ID 1 (analysis batch 1).", blank.Note, "not file 3, the next order but another batch");
        Assert.AreEqual(6d, result.Correction.Correct(2, 6.2d), 1e-9);
        Assert.AreEqual(0, result.Warnings.Count);
    }

    [TestMethod]
    public void ABatchWithNoCorrectedFile_TakesTheNearestCorrectedFileOverall_AndSaysSo() {
        var files = new[] {
            File(0, "S1", AnalysisFileType.Sample, batch: 1, order: 1),
            File(1, "S2", AnalysisFileType.Sample, batch: 2, order: 1),
            File(2, "S3", AnalysisFileType.Sample, batch: 2, order: 5),
            File(3, "B", AnalysisFileType.Blank, batch: 3, order: 4),
        };
        var shifts = new Dictionary<int, double> { [0] = 0d, [1] = 0.2d, [2] = 0.3d, [3] = 0d };

        var result = Build(files, shifts);

        var blank = result.Files.Single(file => file.FileId == 3);
        Assert.AreEqual(AutomaticRtCorrectionModelSource.NearestBlank, blank.ModelSource);
        StringAssert.Contains(blank.Note, "No corrected non-Blank file in analysis batch 3; used the model of file ID 2 (analysis batch 2)");
        Assert.AreEqual(6d, result.Correction.Correct(3, 6.3d), 1e-9, "the nearest batch, then the nearest order");
        StringAssert.Contains(result.Warnings.Single(), "analysis batch 3 has no corrected non-Blank file");
    }

    // CONF-60: tied orders gave a 50/50 blend of whichever files were listed first and last.
    [TestMethod]
    public void ABlankWhoseOrderIsShared_IsNotInterpolated_AndTheRunSaysWhy() {
        var files = new[] {
            File(0, "S1", AnalysisFileType.Sample, batch: 1, order: 5),
            File(1, "S2", AnalysisFileType.Sample, batch: 1, order: 5),
            File(2, "B", AnalysisFileType.Blank, batch: 1, order: 5),
            File(3, "S3", AnalysisFileType.Sample, batch: 1, order: 6),
        };
        var shifts = new Dictionary<int, double> { [0] = 0d, [1] = 0.2d, [2] = 0d, [3] = 0.4d };

        var result = Build(files, shifts);

        var blank = result.Files.Single(file => file.FileId == 2);
        Assert.AreEqual(AutomaticRtCorrectionModelSource.Uncorrected, blank.ModelSource);
        Assert.AreEqual("Not interpolated: its analytical order 5 in analysis batch 1 is also that of file ID(s) 0, 1; original RT is retained.", blank.Note);
        Assert.IsFalse(result.Correction.Models.ContainsKey(2));
        StringAssert.Contains(result.Warnings.Single(), "Blank B (file ID 2) is not interpolated");
    }

    [TestMethod]
    public void ABlankBesideTwoFilesAtOneOrder_IsNotInterpolated() {
        var files = new[] {
            File(0, "S1", AnalysisFileType.Sample, batch: 1, order: 1),
            File(1, "S2", AnalysisFileType.Sample, batch: 1, order: 1),
            File(2, "B", AnalysisFileType.Blank, batch: 1, order: 2),
            File(3, "S3", AnalysisFileType.Sample, batch: 1, order: 3),
        };
        var shifts = new Dictionary<int, double> { [0] = 0d, [1] = 0.2d, [2] = 0d, [3] = 0.4d };

        var result = Build(files, shifts);

        var blank = result.Files.Single(file => file.FileId == 2);
        Assert.AreEqual(AutomaticRtCorrectionModelSource.Uncorrected, blank.ModelSource);
        StringAssert.Contains(blank.Note, "file IDs 0, 1 share the analytical order 1 next to it in analysis batch 1");
        Assert.AreEqual(1, result.Warnings.Count);
    }

    // CONF-60: a cleared order field reaches the Console as 0, for every row.
    [TestMethod]
    public void AllOrdersZero_NoBlankIsInterpolated_AndEachIsWarned() {
        var files = new[] {
            File(0, "S1", AnalysisFileType.Sample, batch: 1, order: 0),
            File(1, "B1", AnalysisFileType.Blank, batch: 1, order: 0),
            File(2, "S2", AnalysisFileType.Sample, batch: 1, order: 0),
            File(3, "B2", AnalysisFileType.Blank, batch: 1, order: 0),
            File(4, "S3", AnalysisFileType.Sample, batch: 1, order: 0),
        };
        var shifts = new Dictionary<int, double> { [0] = 0d, [1] = 0d, [2] = 0.2d, [3] = 0d, [4] = 0.4d };

        var result = Build(files, shifts);

        foreach (var id in new[] { 1, 3 }) {
            var blank = result.Files.Single(file => file.FileId == id);
            Assert.AreEqual(AutomaticRtCorrectionModelSource.Uncorrected, blank.ModelSource);
            Assert.AreEqual("Not interpolated: its analytical order 0 is not an injection position (1 or more); original RT is retained.", blank.Note);
        }
        Assert.AreEqual(2, result.Warnings.Count);
        Assert.AreEqual(3, result.Correction.Models.Count, "the three samples are corrected as before");
    }

    private static AutomaticAlignmentRetentionTimeCorrectionResult Build(IReadOnlyList<AnalysisFileBean> files, IReadOnlyDictionary<int, double> shifts) {
        var parameter = new AutomaticAlignmentRetentionTimeCorrectionParameter {
            Execute = true,
            AnchorMode = AutomaticRtCorrectionAnchorMode.UserOnly,
            ReferenceFileId = 0,
            MinimumAnchorCount = 3,
            MaximumAnchorCount = 12,
            InterpolateBlankByAnalyticalOrder = true,
        };
        return AutomaticAlignmentRetentionTimeCorrection.Build(
            files,
            parameter,
            0.01d,
            _ => new List<ChromatogramPeakFeature>(),
            Library,
            (file, anchors) => new AutomaticRtCorrectionRawFileReading(
                EveryHundredthMinute,
                anchors.Select(anchor => (AutomaticRtCorrectionUserAnchorObservation?)new AutomaticRtCorrectionUserAnchorObservation(
                    anchor.ChromXs.RT.Value + shifts[file.AnalysisFileId], anchor.PrecursorMz, 50000d)).ToArray()));
    }

    private static readonly Ms1CycleProfile EveryHundredthMinute = new Ms1CycleProfile(Enumerable.Range(0, 2001).Select(scan => scan * 0.01d));

    private static MoleculeMsReference Anchor(int id, string name, double mz, double rt) {
        return new MoleculeMsReference {
            ScanID = id,
            Name = name,
            PrecursorMz = mz,
            ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            RetentionTimeTolerance = 1f,
            MassTolerance = 0.01f,
            MinimumPeakHeight = 1000f,
            IsTargetMolecule = true,
        };
    }

    private static AnalysisFileBean File(int id, string name, AnalysisFileType type, int batch, int order) {
        return new AnalysisFileBean {
            AnalysisFileId = id,
            AnalysisFileName = name,
            AnalysisFileType = type,
            AnalysisBatch = batch,
            AnalysisFileAnalyticalOrder = order,
        };
    }
}
