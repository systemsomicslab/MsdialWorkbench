using CompMs.App.MsdialConsole.Parser;
using CompMs.MsdialCore.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Parser;

/// <summary>
/// The Console records each raw data file's format when it reads the analysis files, while the files are surely there,
/// so the format survives in the project after the raw data are deleted.
/// </summary>
[TestClass]
public sealed class AnalysisFilesParserRawFileFormatTests
{
    [TestMethod]
    public void ReadFolderContents_RecordsEachFormat()
    {
        using var directory = VendorFolder();

        var files = AnalysisFilesParser.ReadFolderContents(directory.Path);

        AssertFormats(files.Select(file => (Path.GetFileName(file.AnalysisFilePath), file.RawFileFormat)));
    }

    [TestMethod]
    public void ReadCsvContents_RecordsEachFormat()
    {
        using var directory = VendorFolder();
        var names = new[] { "thermo.raw", "waters.raw", "agilent.d", "bruker.d", "sample.mzML", "sample.lcd" };
        var csv = directory.CreateFile(
            "analysis_files.csv",
            "file_path,file_name,file_type,class_id,acquisition_type,batch_order,analytical_order,factor" + Environment.NewLine
            + string.Join(Environment.NewLine, names.Select((name, i) => $"{System.IO.Path.Combine(directory.Path, name)},{Path.GetFileNameWithoutExtension(name)}_{i},Sample,A,DDA,1,{i + 1},1")));

        var files = AnalysisFilesParser.ReadCsvContents(csv);

        Assert.AreEqual(names.Length, files.Count);
        AssertFormats(files.Select(file => (Path.GetFileName(file.AnalysisFilePath), file.RawFileFormat)));
    }

    [TestMethod]
    public void ReadInput_ASingleFile_RecordsItsFormat()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.CreateFile("thermo.raw");

        var files = AnalysisFilesParser.ReadInput(path, new StringReader(string.Empty), new StringWriter(), inputRedirected: true, outputRedirected: true);

        Assert.IsNotNull(files);
        Assert.AreEqual(RawFileFormat.ThermoRaw, files.Single().RawFileFormat);
    }

    private static void AssertFormats(System.Collections.Generic.IEnumerable<(string Name, RawFileFormat Format)> actual)
    {
        var formats = actual.ToDictionary(pair => pair.Name, pair => pair.Format);
        Assert.AreEqual(RawFileFormat.ThermoRaw, formats["thermo.raw"]);
        Assert.AreEqual(RawFileFormat.WatersRaw, formats["waters.raw"]);
        Assert.AreEqual(RawFileFormat.AgilentMassHunterD, formats["agilent.d"]);
        Assert.AreEqual(RawFileFormat.BrukerTdf, formats["bruker.d"]);
        Assert.AreEqual(RawFileFormat.MzML, formats["sample.mzML"]);
        Assert.AreEqual(RawFileFormat.ShimadzuLcd, formats["sample.lcd"]);
    }

    private static TemporaryDirectory VendorFolder()
    {
        var directory = new TemporaryDirectory();
        directory.CreateFile("thermo.raw");
        directory.CreateDirectory("waters.raw");
        directory.CreateDirectory(System.IO.Path.Combine("agilent.d", "AcqData"));
        directory.CreateDirectory("bruker.d");
        directory.CreateFile(System.IO.Path.Combine("bruker.d", "analysis.tdf"));
        directory.CreateFile("sample.mzML");
        directory.CreateFile("sample.lcd");
        return directory;
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
