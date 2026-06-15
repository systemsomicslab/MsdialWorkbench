using CompMs.App.MsdialConsole.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Parser;

[TestClass]
public sealed class AnalysisFilesParserTests
{
    [TestMethod]
    public void ReadCsvContents_AcceptsFolderTypeVendorData()
    {
        using var directory = new TemporaryDirectory();
        var waters = directory.CreateDirectory("waters.raw");
        var agilent = directory.CreateDirectory("agilent.d");
        var csv = directory.CreateFile(
            "analysis_files.csv",
            $"""
            file_path,file_name,file_type,class_id,acquisition_type,batch_order,analytical_order,factor
            {waters},waters,Sample,Sample,DDA,1,1,1
            {agilent},agilent,Sample,Sample,DDA,1,2,1
            """);

        var files = AnalysisFilesParser.ReadCsvContents(csv);

        Assert.AreEqual(2, files.Count);
        CollectionAssert.AreEqual(
            new[] { waters, agilent },
            files.Select(file => file.AnalysisFilePath).ToArray());
    }

    [TestMethod]
    public void ReadFolderContents_IncludesFolderTypeVendorData()
    {
        using var directory = new TemporaryDirectory();
        directory.CreateDirectory("waters.raw");
        directory.CreateDirectory("agilent.d");
        directory.CreateFile("sample.mzML");

        var files = AnalysisFilesParser.ReadFolderContents(directory.Path);

        CollectionAssert.AreEquivalent(
            new[] { "waters.raw", "agilent.d", "sample.mzML" },
            files.Select(file => Path.GetFileName(file.AnalysisFilePath)).ToArray());
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MsdialCoreTestAppTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateDirectory(string name)
        {
            var path = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public string CreateFile(string name, string content = "")
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
