using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests;

/// <summary>
/// The correction map saved with an alignment, and the conversions between a file's measured RT
/// axis and the aligned RT axis that the GUI's curation windows use.
/// </summary>
[TestClass]
public class AlignmentRtAxisTests
{
    private static AlignmentRetentionTimeCorrectionControlPoint Point(int id, double original, double corrected, double quality = 1d)
        => new(id, 100d + id, original, corrected, quality);

    // File 0: the reference (identity through its anchors). File 1: +0.30 min early, +0.10 late, an
    // odd offset in the middle. File 2: a Blank blended from files 0 and 1. File 3: uncorrected.
    private static AlignmentRetentionTimeCorrectionCollection Correction() {
        var reference = new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(new[] { Point(0, 1d, 1d), Point(1, 5d, 5d), Point(2, 9d, 9d) });
        var shifted = new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(new[] { Point(0, 0.7d, 1d, 0.9d), Point(1, 4.83d, 5d, 0.7d), Point(2, 8.9d, 9d, 0.123456789d) });
        var blank = new BlendedAlignmentRetentionTimeCorrectionModel(reference, shifted, 1d / 3d);
        return new AlignmentRetentionTimeCorrectionCollection(0, new Dictionary<int, IAlignmentRetentionTimeCorrectionModel> {
            [0] = reference,
            [1] = shifted,
            [2] = blank,
        });
    }

    private static List<AnalysisFileBean> Files() => Enumerable.Range(0, 4)
        .Select(id => new AnalysisFileBean { AnalysisFileId = id, AnalysisFileName = $"file{id}" })
        .ToList();

    private static IEnumerable<double> Grid() {
        for (var rt = -1d; rt <= 12d; rt += 0.0173d) {
            yield return rt;
        }
    }

    [TestMethod]
    public void MapFileRoundTripCorrectsAndRestoresEveryRtToTheSameDouble() {
        var correction = Correction();
        using var stream = new MemoryStream();
        AlignmentRetentionTimeCorrectionMapFile.Write(stream, correction, Files());
        stream.Position = 0;
        var read = AlignmentRetentionTimeCorrectionMapFile.Read(stream);

        Assert.AreEqual(0, read.ReferenceFileId);
        CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, read.Models.Keys.ToArray(), "an uncorrected file has no model");
        foreach (var fileId in new[] { 0, 1, 2, 3 }) {
            foreach (var rt in Grid()) {
                Assert.AreEqual(correction.Correct(fileId, rt), read.Correct(fileId, rt), $"Correct file {fileId} at {rt}");
                Assert.AreEqual(correction.Restore(fileId, rt), read.Restore(fileId, rt), $"Restore file {fileId} at {rt}");
            }
        }
        Assert.IsInstanceOfType(read.Models[2], typeof(BlendedAlignmentRetentionTimeCorrectionModel));
        Assert.AreEqual(1d / 3d, ((BlendedAlignmentRetentionTimeCorrectionModel)read.Models[2]).RightWeight);
    }

    [TestMethod]
    public void MapFileSaysWhichAxisEachColumnIsOnAndNamesTheReference() {
        using var stream = new MemoryStream();
        AlignmentRetentionTimeCorrectionMapFile.Write(stream, Correction(), Files());
        var text = Encoding.UTF8.GetString(stream.ToArray());

        StringAssert.Contains(text, "# Reference file ID\t0");
        StringAssert.Contains(text, "# Reference file name\tfile0");
        StringAssert.Contains(text, "Original RT (min)\tCorrected RT (min)");
        StringAssert.Contains(text, "Original RT is each file's measured RT");
    }

    [TestMethod]
    public void MapFileWithANonMonotonicModelIsRefused() {
        var text = "# MS-DIAL automatic alignment RT correction map\n# Format version\t1\n# Reference file ID\t0\n"
            + "File ID\tFile name\tNode\tParent node\tSide\tModel\tRight weight\tOriginal RT (min)\tCorrected RT (min)\tAnchor ID\tm/z\tQuality score\n"
            + "1\tf\t0\t-1\t\tPiecewise\t\t1\t2\t0\t100\t1\n"
            + "1\tf\t0\t-1\t\tPiecewise\t\t2\t1.5\t1\t101\t1\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        Assert.ThrowsException<InvalidDataException>(() => AlignmentRetentionTimeCorrectionMapFile.Read(stream));
    }

    [TestMethod]
    public void MapFileOfANewerFormatIsRefused() {
        var text = "# Format version\t99\n# Reference file ID\t0\n"
            + "File ID\tFile name\tNode\tParent node\tSide\tModel\tRight weight\tOriginal RT (min)\tCorrected RT (min)\tAnchor ID\tm/z\tQuality score\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        Assert.ThrowsException<InvalidDataException>(() => AlignmentRetentionTimeCorrectionMapFile.Read(stream));
    }

    [TestMethod]
    public void MapFileIsNamedAfterTheAlignmentResultBesideIt() {
        var alignment = new AlignmentFileBean { FileName = "AlignResult-1", FilePath = Path.Combine("C:", "project", "AlignResult-1.arf") };
        Assert.AreEqual(Path.Combine("C:", "project", "AlignResult-1.rtcorrection.tsv"), AlignmentRetentionTimeCorrectionMapFile.PathFor(alignment));
        Assert.AreEqual(Path.Combine("C:", "project", "AlignResult-1_automatic_rt_correction"), AutomaticAlignmentRtCorrectionWorkflow.AuditFolderFor(alignment));
    }

    [TestMethod]
    public void MapFromTheCorrectionIsTheModelAndAnUncorrectedFileIsTheIdentity() {
        var correction = Correction();
        var map = AlignedPeakRtAxisMap.FromCorrection(correction, 1);
        Assert.AreEqual(AlignedPeakRtAxisSource.CorrectionMap, map.Source);
        Assert.IsTrue(map.IsCorrected);
        Assert.AreEqual(correction.Correct(1, 4.83d), map.ToAligned(4.83d));
        Assert.AreEqual(5d, map.ToAligned(4.83d), 1e-12);
        Assert.AreEqual(4.83d, map.ToOriginal(5d), 1e-12);

        var uncorrected = AlignedPeakRtAxisMap.FromCorrection(correction, 3);
        Assert.AreEqual(AlignedPeakRtAxisSource.Uncorrected, uncorrected.Source);
        Assert.IsFalse(uncorrected.IsCorrected);
        Assert.IsTrue(uncorrected.CanWrite);
        Assert.AreEqual(7.123d, uncorrected.ToAligned(7.123d));
    }

    [TestMethod]
    public void EveryModelIsInvertedByItsRestore() {
        var correction = Correction();
        foreach (var fileId in new[] { 0, 1, 2 }) {
            var map = AlignedPeakRtAxisMap.FromCorrection(correction, fileId);
            foreach (var rt in Grid()) {
                Assert.AreEqual(rt, map.ToOriginal(map.ToAligned(rt)), 1e-9, $"file {fileId} at {rt}");
            }
        }
    }

    [TestMethod]
    public void StoredBoundsMapIsExactAtTheThreePointsAndInvertible() {
        // Measured 8.25 / 8.30 / 8.36; aligned 8.55 / 8.60 / 8.65 (an offset that shrinks over the peak).
        var map = AlignedPeakRtAxisMap.FromStoredBounds(8.25d, 8.30d, 8.36d, 8.55d, 8.60d, 8.65d);
        Assert.AreEqual(AlignedPeakRtAxisSource.StoredPeakBounds, map.Source);
        Assert.AreEqual(8.55d, map.ToAligned(8.25d), 1e-12);
        Assert.AreEqual(8.60d, map.ToAligned(8.30d), 1e-12);
        Assert.AreEqual(8.65d, map.ToAligned(8.36d), 1e-12);
        Assert.AreEqual(8.30d, map.ToOriginal(8.60d), 1e-12);
        // Outside the bounds the edge offsets hold.
        Assert.AreEqual(8.0d + 0.30d, map.ToAligned(8.0d), 1e-12);
        Assert.AreEqual(9.0d + 0.29d, map.ToAligned(9.0d), 1e-12);
        foreach (var rt in Grid()) {
            Assert.AreEqual(rt, map.ToOriginal(map.ToAligned(rt)), 1e-9);
        }
    }

    [TestMethod]
    public void StoredBoundsThatAreNotIncreasingFallBackToTheTopOffset() {
        // A gap-filled cell without a peak: left, top and right all at one RT.
        var map = AlignedPeakRtAxisMap.FromStoredBounds(4d, 4d, 4d, 4.2d, 4.2d, 4.2d);
        Assert.AreEqual(AlignedPeakRtAxisSource.StoredPeakBounds, map.Source);
        Assert.AreEqual(5.2d, map.ToAligned(5d), 1e-12);
        Assert.AreEqual(5d, map.ToOriginal(5.2d), 1e-12);
    }

    [TestMethod]
    public void StoredBoundsWithoutAUsableTopAreUnavailable() {
        var map = AlignedPeakRtAxisMap.FromStoredBounds(1d, double.NaN, 2d, 1d, 1.5d, 2d);
        Assert.AreEqual(AlignedPeakRtAxisSource.Unavailable, map.Source);
        Assert.IsFalse(map.CanWrite);
        Assert.IsTrue(map.IsCorrected);
    }

    private static ChromatogramPeakInfo Info(int fileId, float left, float top, float right)
        => new(fileId, new[] { new ChromatogramPeak(0, 100d, 1d, new RetentionTime(left)), new ChromatogramPeak(1, 100d, 2d, new RetentionTime(top)), new ChromatogramPeak(2, 100d, 1d, new RetentionTime(right)) }, top, left, right);

    [TestMethod]
    public void WithoutMapAndWithoutTheCorrectionEveryPeakIsNotCorrected() {
        using var directory = new TemporaryDirectory();
        var alignment = new AlignmentFileBean { FileName = "A", FilePath = Path.Combine(directory.Path, "A.arf") };
        var axis = AlignmentRtAxis.Load(alignment, Files(), correctionRequested: false);
        Assert.AreSame(AlignmentRtAxis.NotCorrected, axis);
        Assert.IsNull(axis.ExportNote());
        var spot = axis.ForSpot(new[] { Info(1, 8.25f, 8.3f, 8.36f) }, new[] { (1, 8.55d, 8.6d, 8.65d, false) });
        Assert.AreEqual(AlignedPeakRtAxisSource.NotCorrected, spot.For(1).Source);
        Assert.AreEqual(8.6d, spot.For(1).ToOriginal(8.6d));
    }

    [TestMethod]
    public void WithTheMapEveryPeakUsesItsFilesModel() {
        using var directory = new TemporaryDirectory();
        var alignment = new AlignmentFileBean { FileName = "A", FilePath = Path.Combine(directory.Path, "A.arf") };
        var correction = Correction();
        AlignmentRetentionTimeCorrectionMapFile.Write(AlignmentRetentionTimeCorrectionMapFile.PathFor(alignment), correction, Files());

        var axis = AlignmentRtAxis.Load(alignment, Files(), correctionRequested: false);
        Assert.IsTrue(axis.IsCorrected);
        Assert.IsNotNull(axis.Correction);
        Assert.AreEqual("file0", axis.ReferenceFileName);
        var spot = axis.ForSpot(null, new[] { (1, 8.55d, 8.6d, 8.65d, true), (3, 1d, 2d, 3d, false) });
        Assert.AreEqual(AlignedPeakRtAxisSource.CorrectionMap, spot.For(1).Source, "a peak edited by hand is still moved exactly with the map");
        Assert.AreEqual(correction.Restore(1, 8.6d), spot.For(1).ToOriginal(8.6d));
        Assert.AreEqual(AlignedPeakRtAxisSource.Uncorrected, spot.For(3).Source);
        Assert.AreEqual(correction.Restore(1, 6d), axis.ToOriginal(1, 6d));
        StringAssert.Contains(axis.ExportNote(), "reference file file0");
        Assert.IsFalse(axis.ExportNote()!.Contains(','), "the note fits an mzTab-M parameter value");
    }

    [TestMethod]
    public void WithoutMapButWithTheCorrectionEachPeakUsesItsStoredBoundsAndAnEditedOneIsRefused() {
        using var directory = new TemporaryDirectory();
        var alignment = new AlignmentFileBean { FileName = "A", FilePath = Path.Combine(directory.Path, "A.arf") };
        var axis = AlignmentRtAxis.Load(alignment, Files(), correctionRequested: true);
        Assert.IsTrue(axis.IsCorrected);
        Assert.IsNull(axis.Correction);
        StringAssert.Contains(axis.Note, "No RT correction map");

        var spot = axis.ForSpot(
            new[] { Info(1, 8.25f, 8.3f, 8.36f), Info(2, 8.25f, 8.3f, 8.36f) },
            new[] { (1, 8.55d, 8.6d, 8.65d, false), (2, 8.55d, 8.6d, 8.65d, true) });
        Assert.AreEqual(AlignedPeakRtAxisSource.StoredPeakBounds, spot.For(1).Source);
        Assert.AreEqual(8.3d, spot.For(1).ToOriginal(8.6d), 1e-6);
        Assert.AreEqual(AlignedPeakRtAxisSource.Unavailable, spot.For(2).Source);
        Assert.IsFalse(spot.For(2).CanWrite);
        Assert.AreEqual(AlignedPeakRtAxisSource.Unavailable, spot.For(7).Source, "a file the spot does not hold");
    }

    [TestMethod]
    public void AnUnreadableMapFallsBackToTheStoredBoundsAndSaysWhy() {
        using var directory = new TemporaryDirectory();
        var alignment = new AlignmentFileBean { FileName = "A", FilePath = Path.Combine(directory.Path, "A.arf") };
        File.WriteAllText(AlignmentRetentionTimeCorrectionMapFile.PathFor(alignment), "not a map");
        var axis = AlignmentRtAxis.Load(alignment, Files(), correctionRequested: false);
        Assert.IsTrue(axis.IsCorrected);
        Assert.IsNull(axis.Correction);
        StringAssert.Contains(axis.Note, "could not be read");
    }

    [TestMethod]
    public void RemoveMapDeletesAnEarlierAlignmentsMap() {
        using var directory = new TemporaryDirectory();
        var alignment = new AlignmentFileBean { FileName = "A", FilePath = Path.Combine(directory.Path, "A.arf") };
        AlignmentRetentionTimeCorrectionMapFile.Write(AlignmentRetentionTimeCorrectionMapFile.PathFor(alignment), Correction(), Files());
        AutomaticAlignmentRtCorrectionWorkflow.RemoveMap(alignment);
        Assert.IsFalse(File.Exists(AlignmentRetentionTimeCorrectionMapFile.PathFor(alignment)));
        AutomaticAlignmentRtCorrectionWorkflow.RemoveMap(alignment);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory() {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "msdial-rtaxis-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() {
            try {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException) {
            }
        }
    }
}
