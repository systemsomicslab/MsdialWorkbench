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

    [TestMethod]
    public void ReadInput_MixedFormatsWithRedirectedInput_ContinuesWithoutReadingAndWarns()
    {
        using var directory = MixedFormatFolder();
        var output = new StringWriter();

        var files = AnalysisFilesParser.ReadInput(directory.Path, new UnreadableReader(), output, inputRedirected: true);

        Assert.IsNotNull(files);
        Assert.AreEqual(3, files.Count);
        var text = output.ToString();
        StringAssert.Contains(text, "Warning: the input mixes raw data formats (.lcd, .mzml).");
        Assert.IsFalse(text.Contains("Do you want to continue"), text);
    }

    [TestMethod]
    public void ReadInput_MixedFormatsWhenReadLineReturnsNull_ContinuesAndWarns()
    {
        using var directory = MixedFormatFolder();
        var output = new StringWriter();

        var files = AnalysisFilesParser.ReadInput(directory.Path, new StringReader(string.Empty), output, inputRedirected: false);

        Assert.IsNotNull(files);
        Assert.AreEqual(3, files.Count);
        var text = output.ToString();
        StringAssert.Contains(text, "Do you want to continue this process? Y/N");
        StringAssert.Contains(text, "Warning: the input mixes raw data formats (.lcd, .mzml).");
    }

    [TestMethod]
    public void ReadInput_PublicOverloadWithEmptyStandardInput_DoesNotThrow()
    {
        // The unattended Console run: whichever way the test host's standard input is attached,
        // an empty Console.In must not end the run.
        using var directory = MixedFormatFolder();
        var originalIn = Console.In;
        var originalOut = Console.Out;
        var output = new StringWriter();
        try
        {
            Console.SetIn(new StringReader(string.Empty));
            Console.SetOut(output);

            var files = AnalysisFilesParser.ReadInput(directory.Path);

            Assert.IsNotNull(files);
            Assert.AreEqual(3, files.Count);
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }
        StringAssert.Contains(output.ToString(), "Warning: the input mixes raw data formats (.lcd, .mzml).");
    }

    [TestMethod]
    public void ReadInput_PublicOverload_AsksOnlyWhenStandardInputIsNotRedirected()
    {
        // Pins that ReadInput(string) passes Console.IsInputRedirected on. Started by dotnet test or by
        // a workflow, the process's standard input is redirected: then a reader that fails the test when
        // touched stands in for a pipe that never closes, and must not be read. With standard input on a
        // console, the question is asked and an empty answer stream ends it with the warning.
        using var directory = MixedFormatFolder();
        var redirected = Console.IsInputRedirected;
        var originalIn = Console.In;
        var originalOut = Console.Out;
        var output = new StringWriter();
        try
        {
            Console.SetIn(redirected ? new UnreadableReader() : new StringReader(string.Empty));
            Console.SetOut(output);

            var files = AnalysisFilesParser.ReadInput(directory.Path);

            Assert.IsNotNull(files);
            Assert.AreEqual(3, files.Count);
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }
        var text = output.ToString();
        StringAssert.Contains(text, "Warning: the input mixes raw data formats (.lcd, .mzml).");
        Assert.AreEqual(!redirected, text.Contains("Do you want to continue this process? Y/N"), $"Standard input redirected: {redirected}.{Environment.NewLine}{text}");
    }

    [TestMethod]
    public void ReadInput_MixedFormatsAnsweredNo_ReturnsNull()
    {
        using var directory = MixedFormatFolder();
        var output = new StringWriter();

        var files = AnalysisFilesParser.ReadInput(directory.Path, new StringReader("n" + Environment.NewLine), output, inputRedirected: false);

        Assert.IsNull(files);
        StringAssert.Contains(output.ToString(), "Do you want to continue this process? Y/N");
        Assert.IsFalse(output.ToString().Contains("Warning:"), output.ToString());
    }

    [TestMethod]
    public void ContinueWithMultipleFormats_AsksAgainUntilYesOrNo()
    {
        var output = new StringWriter();
        var answers = new StringReader(string.Join(Environment.NewLine, "maybe", " Y ") + Environment.NewLine);

        var result = AnalysisFilesParser.ContinueWithMultipleFormats(new[] { ".lcd", ".mzml" }, answers, output, inputRedirected: false);

        Assert.IsTrue(result);
        var text = output.ToString();
        Assert.AreEqual(2, CountOf(text, "(.lcd, .mzml). Do you want to continue this process? Y/N"), text);
        Assert.IsFalse(text.Contains("Warning:"), text);
    }

    [TestMethod]
    public void ContinueWithMultipleFormats_InputEndsAfterAnUnclearAnswer_ContinuesAndWarns()
    {
        var output = new StringWriter();

        var result = AnalysisFilesParser.ContinueWithMultipleFormats(new[] { ".lcd", ".mzml" }, new StringReader("maybe"), output, inputRedirected: false);

        Assert.IsTrue(result);
        var text = output.ToString();
        Assert.AreEqual(2, CountOf(text, "Do you want to continue this process? Y/N"), text);
        StringAssert.Contains(text, "Warning: the input mixes raw data formats (.lcd, .mzml).");
    }

    [TestMethod]
    public void ReadInput_SingleFormat_NeitherAsksNorWarns()
    {
        using var directory = new TemporaryDirectory();
        directory.CreateFile("a.mzML");
        directory.CreateFile("b.mzML");
        var output = new StringWriter();

        var files = AnalysisFilesParser.ReadInput(directory.Path, new UnreadableReader(), output, inputRedirected: false);

        Assert.IsNotNull(files);
        Assert.AreEqual(2, files.Count);
        Assert.AreEqual(string.Empty, output.ToString());
    }

    private static TemporaryDirectory MixedFormatFolder()
    {
        // The shape the preflight met: Shimadzu .lcd files beside one mzML.
        var directory = new TemporaryDirectory();
        directory.CreateFile("sample_01.lcd");
        directory.CreateFile("sample_02.lcd");
        directory.CreateFile("blank.mzML");
        return directory;
    }

    private static int CountOf(string text, string value)
    {
        var count = 0;
        for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }
        return count;
    }

    /// <summary>Fails the test if anything tries to read an answer.</summary>
    private sealed class UnreadableReader : TextReader
    {
        public override int Peek() => throw new AssertFailedException("Standard input was read.");
        public override int Read() => throw new AssertFailedException("Standard input was read.");
        public override string ReadLine() => throw new AssertFailedException("Standard input was read.");
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
