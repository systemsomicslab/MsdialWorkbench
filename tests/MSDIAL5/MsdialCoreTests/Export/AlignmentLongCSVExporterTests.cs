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

    private sealed class FakeFileMetaAccessor : IFileClassMetaAccessor
    {
        public IReadOnlyList<string> GetHeaders() => new[] { "Name", "Class" };

        public string[] GetContent(AnalysisFileBean file) => new[] { file.AnalysisFileName, file.AnalysisFileClass };

        public string[][] GetContents(IEnumerable<AnalysisFileBean> files)
            => files.Select(GetContent).ToArray();
    }
}
