using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The GC-MS Console's alignment matrices: the matrix-export keys of a GUI GC-MS parameter text are read by the GC-MS
/// reader, and each matrix asked for is written to the export folder, or to the output folder when that is blank.
/// </summary>
[TestClass]
public sealed class GcmsAlignmentMatrixExportTests
{
    private static readonly string[] MatrixKeys = {
        "Height matrix export",
        "Peak ID matrix export",
        "Retention time matrix export",
        "Mass matrix export",
        "Peak area matrix export",
        "SN matrix export",
    };

    [TestMethod]
    public void ReadForGcms_AppliesTheMatrixKeysOfAGuiParameterText()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Export folder path: matrices
            Height matrix export: True
            Normalized height matrix export: False
            Peak ID matrix export: True
            Retention time matrix export: True
            Mass matrix export: True
            Peak area matrix export: True
            SN matrix export: True
            """);

        var parameter = ConfigParser.ReadForGcms(methodFile);

        Assert.IsTrue(parameter.IsHeightMatrixExport);
        Assert.IsTrue(parameter.IsPeakIdMatrixExport);
        Assert.IsTrue(parameter.IsRetentionTimeMatrixExport);
        Assert.IsTrue(parameter.IsMassMatrixExport);
        Assert.IsTrue(parameter.IsPeakAreaMatrixExport);
        Assert.IsTrue(parameter.IsSnMatrixExport);
        Assert.AreEqual(Path.Combine(directory.Path, "matrices"), parameter.ExportFolderPath, "relative to the method file");
        CollectionAssert.AreEqual(new[] { "Export folder path" }.Concat(MatrixKeys).ToArray(), RecordedKeys(methodFile, "applied").ToArray());
        CollectionAssert.AreEqual(new[] { "Normalized height matrix export" }, RecordedKeys(methodFile, "unrecognised").ToArray(), "the GC-MS Console does not normalize");
    }

    [TestMethod]
    public void ReadForGcms_RefusesAValueThatIsNotTrueOrFalse()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", "Peak area matrix export: yes\n");

        var parameter = ConfigParser.ReadForGcms(methodFile);

        Assert.IsFalse(parameter.IsPeakAreaMatrixExport);
        CollectionAssert.AreEqual(new[] { "Peak area matrix export: yes" }, RecordedKeys(methodFile, "unusable").ToArray());
    }

    /// <summary>The LC-MS reader is unchanged: there the keys stay unrecognised and the run keeps its defaults.</summary>
    [TestMethod]
    public void ReadForLcms_StillDoesNotReadTheMatrixKeys()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", "Peak area matrix export: True\n");

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.IsFalse(parameter.IsPeakAreaMatrixExport);
        CollectionAssert.AreEqual(new[] { "Peak area matrix export" }, RecordedKeys(methodFile, "unrecognised").ToArray());
    }

    [TestMethod]
    public void ExportAlignmentMatrices_WritesEachRequestedMatrixToTheExportFolder()
    {
        using var directory = new TemporaryDirectory();
        var parameter = new MsdialGcmsParameter
        {
            IsHeightMatrixExport = true,
            IsPeakAreaMatrixExport = true,
            IsPeakIdMatrixExport = true,
            IsSnMatrixExport = false,
            ExportFolderPath = Path.Combine(directory.Path, "matrices"),
        };
        var (spots, decResults, files) = TwoFilesOneSpot();

        var written = GcmsProcess.ExportAlignmentMatrices(parameter, directory.Path, "AlignmentResult", spots, decResults, files, new NameOnly(), Array.Empty<StatsValue>());

        CollectionAssert.AreEqual(
            new[] { "AlignmentResult_Height.txt", "AlignmentResult_Area.txt", "AlignmentResult_PeakID.txt" },
            written.Select(Path.GetFileName).ToArray());
        Assert.IsTrue(written.All(path => Path.GetDirectoryName(path) == parameter.ExportFolderPath));
        var area = File.ReadAllLines(written[1]);
        var header = area[area.Length - 2].Split('\t');
        var row = area[area.Length - 1].Split('\t');
        Assert.AreEqual("4000", row[Array.IndexOf(header, "sample1")]);
        Assert.AreEqual("6000", row[Array.IndexOf(header, "sample2")]);
        var peakId = File.ReadAllLines(written[2]);
        Assert.AreEqual("17", peakId[peakId.Length - 1].Split('\t')[Array.IndexOf(header, "sample2")]);
    }

    [TestMethod]
    public void ExportAlignmentMatrices_UsesTheOutputFolderWhenNoExportFolderIsGiven()
    {
        using var directory = new TemporaryDirectory();
        var parameter = new MsdialGcmsParameter { IsHeightMatrixExport = true, ExportFolderPath = "" };
        var (spots, decResults, files) = TwoFilesOneSpot();

        var written = GcmsProcess.ExportAlignmentMatrices(parameter, directory.Path, "AlignmentResult", spots, decResults, files, new NameOnly(), Array.Empty<StatsValue>());

        CollectionAssert.AreEqual(new[] { Path.Combine(directory.Path, "AlignmentResult_Height.txt") }, written);
    }

    [TestMethod]
    public void ExportAlignmentMatrices_WritesNothingWhenNoMatrixIsAskedFor()
    {
        using var directory = new TemporaryDirectory();
        var parameter = new MsdialGcmsParameter { IsHeightMatrixExport = false, ExportFolderPath = Path.Combine(directory.Path, "matrices") };
        var (spots, decResults, files) = TwoFilesOneSpot();

        var written = GcmsProcess.ExportAlignmentMatrices(parameter, directory.Path, "AlignmentResult", spots, decResults, files, new NameOnly(), Array.Empty<StatsValue>());

        Assert.AreEqual(0, written.Count);
        Assert.IsFalse(Directory.Exists(parameter.ExportFolderPath));
    }

    private static (List<AlignmentSpotProperty>, List<MSDecResult>, List<AnalysisFileBean>) TwoFilesOneSpot()
    {
        var files = new List<AnalysisFileBean>
        {
            new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "sample1", AnalysisFileClass = "A", },
            new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "sample2", AnalysisFileClass = "A", },
        };
        var spot = new AlignmentSpotProperty
        {
            AlignmentID = 0,
            MasterAlignmentID = 0,
            AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                new AlignmentChromPeakFeature { FileID = 0, FileName = "sample1", MasterPeakID = 3, PeakHeightTop = 1000, PeakAreaAboveZero = 4000, },
                new AlignmentChromPeakFeature { FileID = 1, FileName = "sample2", MasterPeakID = 17, PeakHeightTop = 1500, PeakAreaAboveZero = 6000, },
            },
        };
        return (new List<AlignmentSpotProperty> { spot }, new List<MSDecResult> { new MSDecResult() }, files);
    }

    private sealed class NameOnly : IMetadataAccessor
    {
        public string[] GetHeaders() => new[] { "Alignment ID" };

        public IReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, IMSScanProperty msdec)
            => new Dictionary<string, string> { ["Alignment ID"] = spot.MasterAlignmentID.ToString() };
    }

    private static List<string> RecordedKeys(string methodFile, string list)
    {
        var record = Path.Combine(
            Path.GetDirectoryName(methodFile)!,
            Path.GetFileNameWithoutExtension(methodFile) + ".keys.json");
        return JObject.Parse(File.ReadAllText(record))[list]!.Select(item => (string)item!).ToList();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MsdialCoreTestAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string name, string content = "")
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
