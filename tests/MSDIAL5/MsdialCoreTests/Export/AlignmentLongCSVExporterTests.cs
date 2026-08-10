using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export.Tests;

[TestClass]
public class AlignmentLongCSVExporterTests
{
    [TestMethod]
    public void ExportFileMetaEscapesDelimiterAndQuotes()
    {
        var exporter = new AlignmentLongCSVExporter(",");
        var files = new[]
        {
            new AnalysisFileBean
            {
                AnalysisFileName = "sample,\"1\"",
                AnalysisFileClass = "class",
                AnalysisFileType = AnalysisFileType.Sample,
            },
        };
        using var stream = new MemoryStream();

        exporter.ExportFileMeta(stream, files, new FakeFileMetaAccessor());

        Assert.AreEqual("Name,Class\r\n\"sample,\"\"1\"\"\",class\r\n", Encoding.ASCII.GetString(stream.ToArray()));
    }

    [TestMethod]
    public void ExportValueUsesFileIdWhenAlignmentPeakHasOldFileName()
    {
        var exporter = new AlignmentLongCSVExporter();
        var file = new AnalysisFileBean
        {
            AnalysisFileId = 7,
            AnalysisFileName = "renamed.raw",
            AnalysisFileClass = "class",
        };
        var spot = new AlignmentSpotProperty
        {
            MasterAlignmentID = 1,
            AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                new AlignmentChromPeakFeature
                {
                    FileID = 7,
                    FileName = "original.raw",
                },
            },
        };
        using var stream = new MemoryStream();

        exporter.ExportValue(stream, new[] { spot }, new[] { file }, ("Height", new FakeQuantValueAccessor()));

        Assert.AreEqual("ID\tFile\tClass\tHeight\r\n1\trenamed.raw\tclass\tvalue\r\n", Encoding.ASCII.GetString(stream.ToArray()));
    }

    private sealed class FakeFileMetaAccessor : IFileClassMetaAccessor
    {
        public IReadOnlyList<string> GetHeaders() => new[] { "Name", "Class" };

        public string[] GetContent(AnalysisFileBean file) => new[] { file.AnalysisFileName, file.AnalysisFileClass };

        public string[][] GetContents(IEnumerable<AnalysisFileBean> files)
            => files.Select(GetContent).ToArray();
    }

    private sealed class FakeQuantValueAccessor : IQuantValueAccessor
    {
        public List<string> GetQuantHeaders(IReadOnlyList<AnalysisFileBean> files) => new();

        public List<string> GetStatHeaders() => new();

        public Dictionary<string, string> GetQuantValues(AlignmentSpotProperty spot)
            => spot.AlignedPeakProperties.ToDictionary(peak => peak.FileName, _ => "value");

        public Dictionary<string, string> GetStatsValues(AlignmentSpotProperty spot, StatsValue stat) => new();
    }
}
