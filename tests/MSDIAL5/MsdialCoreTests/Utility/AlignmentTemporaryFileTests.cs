using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Utility.Tests;

/// <summary>
/// The alignment's temporary files are created beside the alignment result, on the drive the data is on, and not in
/// %TEMP% on the system drive; only when no result folder is known do they fall back to %TEMP%.
/// </summary>
[TestClass()]
public class AlignmentTemporaryFileTests
{
    private string _folder = string.Empty;

    [TestInitialize()]
    public void Initialize() {
        _folder = Path.Combine(Path.GetTempPath(), $"msdial-alignment-temp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup()]
    public void Cleanup() {
        if (Directory.Exists(_folder)) {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [TestMethod()]
    public void CreateBesideCreatesAnEmptyUniqueFileInTheResultFolderNamedAfterTheResult() {
        var eicFile = Path.Combine(_folder, "AlignResult-2026112510.EIC.aef");

        var first = AlignmentTemporaryFile.CreateBeside(eicFile);
        var second = AlignmentTemporaryFile.CreateBeside(eicFile);

        Assert.AreEqual(_folder, Path.GetDirectoryName(first));
        Assert.AreEqual(_folder, Path.GetDirectoryName(second));
        StringAssert.StartsWith(Path.GetFileName(first), "AlignResult-2026112510.EIC.aef.");
        StringAssert.EndsWith(first, ".tmp");
        Assert.AreNotEqual(first, second);
        Assert.IsTrue(File.Exists(first));
        Assert.AreEqual(0, new FileInfo(first).Length);
        Assert.IsFalse(File.Exists(eicFile), "The result file itself is not touched.");
    }

    [TestMethod()]
    public void CreateBesideFallsBackToTheSystemTemporaryFolderWhenNoResultFolderIsKnown() {
        var systemTemp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
        foreach (var resultPath in new[] { null, string.Empty, Path.Combine(_folder, "missing", "AlignResult.EIC.aef") }) {
            var path = AlignmentTemporaryFile.CreateBeside(resultPath);
            try {
                Assert.AreEqual(systemTemp, Path.GetDirectoryName(Path.GetFullPath(path)), $"result path: '{resultPath}'");
                Assert.IsTrue(File.Exists(path));
            }
            finally {
                File.Delete(path);
            }
        }
        Assert.IsFalse(Directory.Exists(Path.Combine(_folder, "missing")), "A missing result folder is not created.");
    }

    [TestMethod()]
    public void LightPeakStoreSpillIsCreatedBesideTheResultAndDeletedOnDispose() {
        var spectraFile = Path.Combine(_folder, "AlignResult-2026112510.dcl");
        var files = new[] {
            new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "a" },
            new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "b" },
        };

        string spill;
        using (var store = AlignmentLightPeakStore.CreateTemp(spectraFile)) {
            store.Initialize(3, files);
            store.WriteSpotPeak(2, new AlignmentChromPeakFeature { FileID = 1, FileName = "b", PeakHeightTop = 5d });
            var inFolder = Directory.GetFiles(_folder);
            Assert.AreEqual(1, inFolder.Length);
            spill = inFolder.Single();
            StringAssert.StartsWith(Path.GetFileName(spill), "AlignResult-2026112510.dcl.");
            Assert.IsTrue(new FileInfo(spill).Length > 0);
            Assert.AreEqual(5d, store.ReadSpotPeaks(2)[1].PeakHeightTop);
        }

        Assert.IsFalse(File.Exists(spill));
        Assert.AreEqual(0, Directory.GetFiles(_folder).Length);
    }
}
