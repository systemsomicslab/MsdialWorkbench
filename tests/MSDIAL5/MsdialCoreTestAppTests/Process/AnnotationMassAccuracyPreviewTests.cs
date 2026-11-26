using CompMs.App.MsdialConsole.Process;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The annotation mass accuracy preview, run by the Console on SWATH files whose MS1 and product-ion scans read a known
/// error, against a library of computed spectra: it finds both errors, stops after the report, and saves nothing beside
/// the raw data.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public sealed class AnnotationMassAccuracyPreviewTests
{
    private const double Ms1Shift = 8d;
    private const double Ms2Shift = -25d;

    // Twelve compounds, three per isolation window, each [M+H]+ with three fragments at fixed fractions of its m/z.
    private static readonly (string Name, string Formula, double Rt, double Height)[] Compounds =
    {
        ("Phenylalanine", "C9H11NO2", 1.0d, 200000d),
        ("Caffeine", "C8H10N4O2", 1.7d, 160000d),
        ("Adenosine", "C10H13N5O4", 2.4d, 150000d),
        ("Chlorogenic acid", "C16H18O9", 3.1d, 300000d),
        ("Cholic acid", "C24H40O5", 3.8d, 140000d),
        ("Kaempferol 3-glucoside", "C21H20O11", 4.5d, 120000d),
        ("Compound 565", "C26H28O14", 5.2d, 250000d),
        ("Rutin", "C27H30O16", 5.9d, 170000d),
        ("Compound 625", "C28H32O16", 6.6d, 180000d),
        ("PE 36:2", "C41H78NO8P", 7.3d, 400000d),
        ("PC 34:1", "C42H82NO8P", 8.0d, 260000d),
        ("PC 36:2", "C44H84NO8P", 8.7d, 220000d),
    };

    private static readonly (double Fraction, double Relative)[] FragmentShape = { (0.4d, 0.6d), (0.6d, 1.0d), (0.8d, 0.3d) };

    private static readonly (double Lower, double Upper)[] Windows = { (100d, 300d), (300d, 500d), (500d, 700d), (700d, 900d) };

    private static double ProtonatedMz(string formula) {
        return AdductIon.GetAdductIon("[M+H]+").ConvertToMz(FormulaStringParcer.OrganicElementsReader(formula).Mass);
    }

    private static List<(double Mz, double Relative)> Fragments(double precursorMz) {
        return FragmentShape.Select(shape => (Math.Round(shape.Fraction * precursorMz, 5), shape.Relative)).ToList();
    }

    private static string Library() {
        var text = new StringBuilder();
        foreach (var compound in Compounds) {
            var mz = ProtonatedMz(compound.Formula);
            text.Append("NAME: ").AppendLine(compound.Name);
            text.Append("PRECURSORMZ: ").AppendLine(mz.ToString("0.00000", CultureInfo.InvariantCulture));
            text.AppendLine("PRECURSORTYPE: [M+H]+");
            text.Append("FORMULA: ").AppendLine(compound.Formula);
            text.AppendLine("IONMODE: Positive");
            text.AppendLine($"Num Peaks: {FragmentShape.Length}");
            foreach (var (fragment, relative) in Fragments(mz)) {
                text.AppendLine($"{fragment.ToString("0.00000", CultureInfo.InvariantCulture)}\t{(relative * 1000d).ToString("0", CultureInfo.InvariantCulture)}");
            }
            text.AppendLine();
        }
        return text.ToString();
    }

    private static (string Input, string Output, string MethodFile) Prepare(ScratchDirectory directory, string libraryKind, string extraMethodLines = "") {
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        var compounds = Compounds.Select(compound => (ProtonatedMz(compound.Formula), compound.Rt, compound.Height)).ToList();
        var fragments = compounds.Select(compound => (IReadOnlyList<(double, double)>)Fragments(compound.Item1)).ToList();
        SyntheticLcmsMzml.WriteSwath(Path.Combine(input, "s1.mzML"), compounds, Windows, Ms2Shift, ms1ShiftPpm: Ms1Shift, fragments: fragments);
        directory.File("computed.msp", Library());
        directory.File("msp_annotators.tsv", "annotator_id\tmsp_file_path\tlibrary_kind\n" + $"Computed\tcomputed.msp\t{libraryKind}\n");
        var methodFile = directory.File(
            "method.txt",
            """
            Acquisition type: SWATH
            Ion mode: Positive
            MSP annotator settings file path: msp_annotators.tsv
            Annotation mass accuracy preview only: True
            Number of threads: 2

            """ + extraMethodLines);
        return (input, output, methodFile);
    }

    [TestMethod]
    public void Preview_FindsTheMs1AndTheProductIonErrorAgainstAComputedLibraryAndSavesNothingBesideTheRawData()
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "predicted");
        var before = Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray();
        var folder = Path.Combine(output, "annotation_mass_accuracy");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "old.annotation_calibrants.tsv"), "from an earlier preview");

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        StringAssert.Contains(stdout, "Removed 1 annotation mass accuracy file(s) of an earlier preview");
        StringAssert.Contains(stdout, "±100 ppm m/z windows");
        StringAssert.Contains(stdout, "No m/z was corrected and no annotation was kept");
        Assert.IsFalse(stdout.Contains("Alignment started"), stdout);
        CollectionAssert.AreEqual(before, Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray(),
            "no .pai, .dcl or anything else beside the raw data");
        Assert.AreEqual(0, Directory.GetDirectories(input).Length);
        Assert.IsFalse(File.Exists(Path.Combine(folder, "old.annotation_calibrants.tsv")));

        var calibrants = ReadTsv(Path.Combine(folder, "s1.annotation_calibrants.tsv"));
        var ms1 = calibrants.Where(row => row["MS level"] == "1").ToList();
        var ms2 = calibrants.Where(row => row["MS level"] == "2").ToList();
        Assert.AreEqual(Compounds.Length, ms1.Count, "one precursor for each compound, none for its M+1 feature");
        Assert.AreEqual(Compounds.Length * FragmentShape.Length, ms2.Count, "every fragment of every compound");
        Assert.IsTrue(ms1.All(row => row["Reference m/z from"] == "Formula and adduct"));
        Assert.IsTrue(ms1.All(row => Math.Abs(Number(row["Error (ppm)"]) - Ms1Shift) < 0.2d), "every precursor reads the MS1 error");
        Assert.IsTrue(ms2.All(row => Math.Abs(Number(row["Error (ppm)"]) - Ms2Shift) < 0.2d), "every product ion reads the MS2 error");
        Assert.IsTrue(calibrants.All(row => row["Library"] == "Computed" && row["Library kind"] == "PredictedMsp"));
        CollectionAssert.AreEquivalent(Compounds.Select(compound => compound.Name).ToArray(), ms1.Select(row => row["Reference"]).ToArray());

        var summary = ReadTsv(Path.Combine(folder, "annotation_mass_accuracy_summary.tsv"));
        var ms1Overall = summary.Single(row => row["Pairs"] == "MS1 precursor" && row["Group"] == "Overall");
        var ms2Overall = summary.Single(row => row["Pairs"] == "MS2 product ion" && row["Group"] == "Overall");
        Assert.AreEqual(Ms1Shift, Number(ms1Overall["Offset (ppm)"]), 0.1d);
        Assert.AreEqual(Ms2Shift, Number(ms2Overall["Offset (ppm)"]), 0.1d);
        StringAssert.Contains(stdout, "(s1): MS1 offset ");
    }

    [TestMethod]
    public void Preview_MeasuresOnlyTheMs1AgainstAnAcquiredLibrary()
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "acquired");

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        var calibrants = ReadTsv(Path.Combine(output, "annotation_mass_accuracy", "s1.annotation_calibrants.tsv"));
        Assert.AreEqual(Compounds.Length, calibrants.Count);
        Assert.IsTrue(calibrants.All(row => row["MS level"] == "1"));
        StringAssert.Contains(stdout, "MS2 no product ions of libraries with computed fragment m/z");
    }

    [TestMethod]
    [DataRow("Annotation mass accuracy preview only: yes\n", "neither True nor False")]
    [DataRow("Annotation mass accuracy search window (ppm): 0\n", "is not a positive number")]
    [DataRow("MS2 mass accuracy preview only: True\n", "are both True; run them one at a time")]
    public void Preview_IsRefusedBeforeAnythingIsRead(string lines, string reason)
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "predicted", lines);

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(-1, result);
        StringAssert.Contains(stderr, reason);
        Assert.IsFalse(stdout.Contains("Annotation mass accuracy preview:"), stdout);
        Assert.IsFalse(Directory.Exists(Path.Combine(output, "annotation_mass_accuracy")));
    }

    [TestMethod]
    public void Preview_IsRefusedWithoutALibrary()
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "predicted");
        File.WriteAllText(methodFile, File.ReadAllText(methodFile).Replace("MSP annotator settings file path: msp_annotators.tsv", string.Empty));

        var (result, _, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(-1, result);
        StringAssert.Contains(stderr, "needs an MSP or LBM library with MS/MS spectra");
        Assert.IsFalse(Directory.Exists(Path.Combine(output, "annotation_mass_accuracy")));
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "msdial-annotation-accuracy-preview-" + Guid.NewGuid().ToString("N"));
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
