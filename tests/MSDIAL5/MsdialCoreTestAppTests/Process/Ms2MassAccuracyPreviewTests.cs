using CompMs.App.MsdialConsole.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The MS2 mass accuracy preview, run by the Console on SWATH files whose product-ion scans read a known error: it
/// finds that error in each file, stops after the report, and saves nothing beside the raw data.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public sealed class Ms2MassAccuracyPreviewTests
{
    // Twelve, three per window: the report gives an offset only from ten pairs above the background.
    private static readonly (double Mz, double Rt, double Height)[] Compounds =
    {
        (152.0706d, 1.0d, 200000d),
        (198.1852d, 1.7d, 160000d),
        (245.1131d, 2.4d, 150000d),
        (331.2843d, 3.1d, 300000d),
        (377.0855d, 3.8d, 140000d),
        (418.2005d, 4.5d, 120000d),
        (523.3270d, 5.2d, 250000d),
        (566.3810d, 5.9d, 170000d),
        (611.1610d, 6.6d, 180000d),
        (734.5694d, 7.3d, 400000d),
        (780.5538d, 8.0d, 260000d),
        (812.6007d, 8.7d, 220000d),
    };

    private static readonly (double Lower, double Upper)[] Windows = { (100d, 300d), (300d, 500d), (500d, 700d), (700d, 900d) };

    [TestMethod]
    public void Preview_FindsEachFilesProductIonErrorAndSavesNothingBesideTheRawData()
    {
        using var directory = new ScratchDirectory();
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        var shifts = new Dictionary<string, double> { ["s1"] = 30d, ["s2"] = -12.5d, ["s3"] = 0d };
        foreach (var (name, shift) in shifts) {
            SyntheticLcmsMzml.WriteSwath(Path.Combine(input, name + ".mzML"), Compounds, Windows, shift);
        }
        var before = Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray();
        var folder = Path.Combine(output, "ms2_mass_accuracy");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "old.ms2_precursor_pairs.tsv"), "from an earlier preview");
        var methodFile = directory.File(
            "method.txt",
            """
            Acquisition type: SWATH
            MS2 mass accuracy preview only: True
            Number of threads: 2
            """);

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        StringAssert.Contains(stdout, "Removed 1 MS2 mass accuracy file(s) of an earlier preview");
        StringAssert.Contains(stdout, "No m/z was corrected");
        Assert.IsFalse(stdout.Contains("Deconvolution started"), stdout);
        Assert.IsFalse(stdout.Contains("Alignment started"), stdout);
        CollectionAssert.AreEqual(before, Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray(),
            "no .pai, .dcl or anything else beside the raw data");
        Assert.AreEqual(0, Directory.GetDirectories(input).Length);
        Assert.IsFalse(File.Exists(Path.Combine(folder, "old.ms2_precursor_pairs.tsv")));

        var summary = ReadTsv(Path.Combine(folder, "ms2_mass_accuracy_summary.tsv"));
        foreach (var (name, shift) in shifts) {
            var pairs = ReadTsv(Path.Combine(folder, name + ".ms2_precursor_pairs.tsv"));
            Assert.AreEqual(Compounds.Length, pairs.Count, $"{name}: one pair for each compound, none for its M+1 feature");
            Assert.IsTrue(pairs.All(row => row["Isotope confirmed"] == "True"), name);
            Assert.IsTrue(pairs.All(row => Math.Abs(Number(row["Error (ppm)"]) - shift) < 0.2d), $"{name}: every pair reads the {shift} ppm error");

            var overall = summary.Single(row => row["File name"] == name && row["Pairs"] == "All" && row["Group"] == "Overall");
            Assert.AreEqual(shift, Number(overall["Offset (ppm)"]), 0.1d, name);
            Assert.AreEqual(Compounds.Length.ToString(CultureInfo.InvariantCulture), overall["Candidate pairs"]);
            var confirmed = summary.Single(row => row["File name"] == name && row["Pairs"] == "Isotope confirmed" && row["Group"] == "Overall");
            Assert.AreEqual(shift, Number(confirmed["Offset (ppm)"]), 0.1d, name);
            Assert.AreEqual(Windows.Length, summary.Count(row => row["File name"] == name && row["Pairs"] == "All" && row["Group"] == "Isolation window (m/z)"));
            StringAssert.Contains(stdout, $"({name}): offset ");
        }
    }

    [TestMethod]
    [DataRow("MS2 mass accuracy preview only: yes\n", "neither True nor False")]
    [DataRow("MS2 mass accuracy preview only: True\nAutomatic RT correction preview only: True\nExecute automatic RT correction for alignment: True\nTogether with alignment: True\n", "run them one at a time")]
    public void Preview_IsRefusedBeforeAnythingIsRead(string lines, string reason)
    {
        using var directory = new ScratchDirectory();
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        var methodFile = directory.File("method.txt", "Acquisition type: SWATH\n" + lines);

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(-1, result);
        StringAssert.Contains(stderr, reason);
        Assert.IsFalse(stdout.Contains("MS2 mass accuracy preview:"), stdout);
        Assert.IsFalse(Directory.Exists(Path.Combine(output, "ms2_mass_accuracy")));
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

    private static List<Dictionary<string, string>> ReadTsv(string path)
    {
        var lines = File.ReadAllLines(path);
        var header = lines[0].Split('\t');
        return lines.Skip(1)
            .Select(line => line.Split('\t'))
            .Select(cells => header.Select((name, index) => (name, value: index < cells.Length ? cells[index] : string.Empty)).ToDictionary(pair => pair.name, pair => pair.value))
            .ToList();
    }

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    private sealed class ScratchDirectory : IDisposable
    {
        public ScratchDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "msdial-ms2-accuracy-preview-" + Guid.NewGuid().ToString("N"));
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
