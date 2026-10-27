using CompMs.App.MsdialConsole.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The suggestions column of the MSP annotator settings table, run by the Console on real files.
/// </summary>
/// <remarks>
/// The files hold MS1 scans only, so every candidate an MSP annotator finds is a precursor-only one: a suggestion when
/// suggestions are on, and nothing at all when they are off.
/// </remarks>
[TestClass]
public sealed class MspAnnotatorSuggestionsTests
{
    private static readonly (double Mz, double Rt, double Height)[] Compounds = { (300.1, 3d, 100000d), (400.2, 6d, 100000d) };

    private const string Library =
        "NAME: Synthetic in-silico compound\n"
        + "PRECURSORMZ: 300.1\n"
        + "PRECURSORTYPE: [M+H]+\n"
        + "IONMODE: Positive\n"
        + "Num Peaks: 3\n"
        + "100.05\t40\n"
        + "150.07\t100\n"
        + "200.09\t30\n"
        + "\n";

    [TestMethod]
    public void Run_SuggestsAPrecursorOnlyNameWhenTheColumnSaysTrue()
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "true");

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        StringAssert.Contains(stdout, "MSP annotator glacier: annotation suggestions enabled");
        StringAssert.Contains(File.ReadAllText(Path.Combine(output, "s1.mdpeak")), "Synthetic in-silico compound",
            "the control: the candidate is found, and stands as a suggestion");
    }

    [TestMethod]
    public void Run_StoresNoSuggestionWhenTheColumnSaysFalse()
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "false");

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        StringAssert.Contains(stdout, "MSP annotator glacier: annotation suggestions disabled");
        foreach (var exported in Directory.GetFiles(output).Where(path => path.EndsWith(".mdpeak", StringComparison.Ordinal) || path.EndsWith(".mdalign", StringComparison.Ordinal))) {
            Assert.IsFalse(File.ReadAllText(exported).Contains("Synthetic in-silico compound"), Path.GetFileName(exported));
        }
        Assert.IsTrue(File.Exists(Path.Combine(output, "s1.mdpeak")), "the run exported its peaks");
    }

    [TestMethod]
    public void Run_RefusesASuggestionsValueItCannotReadBeforeTheRawDataAreRead()
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "no");
        var before = Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray();

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(-1, result, stdout);
        StringAssert.Contains(stderr, "MSP annotator settings cannot be used");
        StringAssert.Contains(stderr, "suggestions 'no' for MSP annotator 'glacier'");
        CollectionAssert.AreEqual(before, Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray(),
            "nothing is written beside the raw data");
        Assert.AreEqual(0, Directory.GetFileSystemEntries(output).Length, "nothing is written to the output folder");
        Assert.IsFalse(stdout.Contains("Loading analysis files"), "refused before the analysis files are imported: " + stdout);
    }

    /// <summary>
    /// With RT correction on, the analysis files are first read for the correction's anchors, so a value the table refuses
    /// stops the run before that: the RT correction library is checked, then the table, and only then the raw data.
    /// </summary>
    [TestMethod]
    public void Run_RefusesASuggestionsValueItCannotReadBeforeRtCorrectionReadsTheRawData()
    {
        using var directory = new ScratchDirectory();
        var anchors = directory.File(
            "anchors.txt",
            "Name\tRT\tRT tolerance\tm/z\tm/z tolerance\tMinimum height\tInclude\n"
            + "Anchor A\t3.0\t0.5\t300.1\t0.01\t1000\ttrue\n");
        var (input, output, methodFile) = Prepare(directory, "no", $"Execute RT correction: True\nCompounds library file path for RT correction: {anchors}\n");
        var before = Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray();

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(-1, result, stdout);
        StringAssert.Contains(stderr, "suggestions 'no' for MSP annotator 'glacier'");
        Assert.IsFalse(stderr.Contains("RT correction library could not be used"), "the RT correction library is valid: " + stderr);
        Assert.IsFalse(stdout.Contains("Loading analysis files"), stdout);
        Assert.IsFalse(stdout.Contains("RT correction started"), "no raw file is read for the anchors: " + stdout);
        CollectionAssert.AreEqual(before, Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray(),
            "nothing is written beside the raw data");
        Assert.AreEqual(0, Directory.GetFileSystemEntries(output).Length, "nothing is written to the output folder");
    }

    private static (string Input, string Output, string MethodFile) Prepare(ScratchDirectory directory, string suggestions, string extraMethodLines = "")
    {
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        foreach (var name in new[] { "s1", "s2" }) {
            SyntheticLcmsMzml.Write(Path.Combine(input, name + ".mzML"), Compounds);
        }
        directory.File("in_silico.msp", Library);
        directory.File(
            "msp_annotators.tsv",
            "annotator_id\tmsp_file_path\tpriority\ttarget_omics\tlibrary_kind\tsuggestions\n"
            + $"glacier\tin_silico.msp\t1\tMetabolomics\tpredicted\t{suggestions}\n");
        var methodFile = directory.File(
            "method.txt",
            """
            MSP annotator settings file path: msp_annotators.tsv
            Number of threads: 2

            """ + extraMethodLines);
        return (input, output, methodFile);
    }

    private static (int Result, string Output, string Error) RunLcms(string input, string output, string methodFile)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        try {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var result = new LcmsProcess().Run(input, output, methodFile, isProjectSaved: false, targetMz: -1f);
            return (result, stdout.ToString(), stderr.ToString());
        }
        finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private sealed class ScratchDirectory : IDisposable
    {
        public ScratchDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "msdial-msp-suggestions-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Folder(string name)
        {
            var path = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public string File(string name, string content)
        {
            var path = System.IO.Path.Combine(Path, name);
            System.IO.File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path)) {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
