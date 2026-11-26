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
/// The mass recalibration of the LC-MS Console, run on SWATH files whose MS1 and product-ion scans read a known error,
/// against a library of computed spectra searched with windows narrower than the error: the first pass finds both
/// errors, the file is read again recalibrated, and its peaks are picked and annotated from the recalibrated m/z. A file
/// without an error comes out as it would without the recalibration.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public sealed class MassRecalibrationTests
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

    // A computed library searched with 1 mDa in MS1 and 2 mDa in MS2: an 8 ppm MS1 error puts every precursor below
    // m/z 125 outside, and a 25 ppm MS2 error every fragment above m/z 80.
    private static (string Input, string Output, string MethodFile) Prepare(ScratchDirectory directory, string name, double ms1Shift, double ms2Shift, string recalibration) {
        var input = directory.Folder(name + "_raw");
        var output = directory.Folder(name + "_output");
        var compounds = Compounds.Select(compound => (ProtonatedMz(compound.Formula), compound.Rt, compound.Height)).ToList();
        var fragments = compounds.Select(compound => (IReadOnlyList<(double, double)>)Fragments(compound.Item1)).ToList();
        SyntheticLcmsMzml.WriteSwath(Path.Combine(input, "s1.mzML"), compounds, Windows, ms2Shift, ms1ShiftPpm: ms1Shift, fragments: fragments);
        var library = directory.File("computed.msp", Library());
        var annotators = directory.File(name + "_msp_annotators.tsv",
            "annotator_id\tmsp_file_path\tlibrary_kind\tms1_tolerance\tms2_tolerance\n" + $"Computed\t{library}\tpredicted\t0.001\t0.002\n");
        var methodFile = directory.File(
            name + "_method.txt",
            $"""
            Acquisition type: SWATH
            Ion mode: Positive
            MSP annotator settings file path: {annotators}
            Number of threads: 2
            {recalibration}

            """);
        return (input, output, methodFile);
    }

    [TestMethod]
    public void Run_RecalibratesTheMs1AndTheProductIonsAndAnnotatesFromTheRecalibratedMz()
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "on", Ms1Shift, Ms2Shift, "Mass recalibration: True");
        var (offInput, offOutput, offMethodFile) = Prepare(directory, "off", Ms1Shift, Ms2Shift, "Mass recalibration: False");

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);
        var (offResult, offStdout, offStderr) = RunLcms(offInput, offOutput, offMethodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        Assert.AreEqual(0, offResult, offStderr + offStdout);
        StringAssert.Contains(stdout, "Mass recalibration (s1): MS1 Offset, modelled error 8.0 ppm: apply; MS2 Offset, modelled error -25.0 ppm: apply.");
        StringAssert.Contains(stdout, "Mass recalibration: 1 of 1 file(s) recalibrated (MS1 in 1, MS2 in 1)");
        Assert.IsFalse(offStdout.Contains("Mass recalibration"), "off, the run says nothing of a recalibration");
        Assert.IsFalse(Directory.Exists(Path.Combine(offOutput, "mass_recalibration")));

        var models = ReadTsv(Path.Combine(output, "mass_recalibration", "mass_recalibration_models.tsv"));
        CollectionAssert.AreEqual(new[] { "MS1 precursor", "MS2 product ion" }, models.Select(row => row["Points"]).ToArray());
        Assert.IsTrue(models.All(row => row["Decision"] == "Apply"), string.Join(" | ", models.Select(row => row["Reason"])));
        Assert.AreEqual(Ms1Shift, Number(models[0]["Error at reference (ppm)"]), 0.1d);
        Assert.AreEqual(Ms2Shift, Number(models[1]["Error at reference (ppm)"]), 0.1d);
        Assert.AreEqual(Compounds.Length * (1 + FragmentShape.Length), ReadTsv(Path.Combine(output, "mass_recalibration", "s1.annotation_calibrants.tsv")).Count);

        var peaks = ReadTsv(Path.Combine(output, "s1.mdpeak"));
        var offPeaks = ReadTsv(Path.Combine(offOutput, "s1.mdpeak"));
        var aligned = ReadMatrix(Written("AlignResult-*_Height.txt", input, output));
        foreach (var compound in Compounds) {
            var theoretical = ProtonatedMz(compound.Formula);
            var spot = aligned.Single(row => row["Metabolite name"] == compound.Name);
            Assert.AreEqual(0d, (Number(spot["Average Mz"]) - theoretical) / theoretical * 1e6, 0.5d, $"{compound.Name}: the alignment reads the recalibrated m/z");
            var annotated = peaks.Where(row => row["Name"] == compound.Name).ToList();
            Assert.AreEqual(1, annotated.Count, $"{compound.Name} is annotated once with 1 mDa and 2 mDa windows after recalibration");
            Assert.AreEqual(0d, (Number(annotated[0]["Precursor m/z"]) - theoretical) / theoretical * 1e6, 0.5d, $"{compound.Name}: its m/z is recalibrated");
            Assert.AreEqual("True", annotated[0]["MS/MS matched"], $"{compound.Name}: its product ions match after recalibration");

            var asRead = offPeaks.OrderBy(row => Math.Abs(Number(row["Precursor m/z"]) - theoretical)).First();
            Assert.AreEqual(Ms1Shift, (Number(asRead["Precursor m/z"]) - theoretical) / theoretical * 1e6, 0.5d, $"{compound.Name}: off, its m/z is read as recorded");
        }
        Assert.IsTrue(offPeaks.Count(row => Compounds.Any(compound => compound.Name == row["Name"] && row["MS/MS matched"] == "True")) < Compounds.Length,
            "off, the narrow windows miss what the error moved out of them");
    }

    [TestMethod]
    public void Run_LeavesAFileWithoutAnErrorAsItWouldBeWithoutTheRecalibration()
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "on", 0d, 0d, "Mass recalibration: True");
        var (offInput, offOutput, offMethodFile) = Prepare(directory, "off", 0d, 0d, string.Empty);

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);
        var (offResult, offStdout, offStderr) = RunLcms(offInput, offOutput, offMethodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        Assert.AreEqual(0, offResult, offStderr + offStdout);
        StringAssert.Contains(stdout, "Mass recalibration: 0 of 1 file(s) recalibrated (MS1 in 0, MS2 in 0)");
        var models = ReadTsv(Path.Combine(output, "mass_recalibration", "mass_recalibration_models.tsv"));
        Assert.IsTrue(models.All(row => row["Decision"] == "Do not apply"), string.Join(" | ", models.Select(row => row["Reason"])));

        // The peaks and spectra of the file, and of the alignment, with its height matrix.
        foreach (var extension in new[] { "s1_*.pai2", "s1_*.dcl", "AlignResult-*.arf2", "AlignResult-*.dcl", "AlignResult-*_Height.txt" }) {
            var on = Written(extension, input, output);
            var off = Written(extension, offInput, offOutput);
            CollectionAssert.AreEqual(File.ReadAllBytes(off), File.ReadAllBytes(on), $"{extension} is byte for byte what it is without the recalibration");
        }
        Assert.AreEqual(File.ReadAllText(Path.Combine(offOutput, "s1.mdpeak")), File.ReadAllText(Path.Combine(output, "s1.mdpeak")));
    }

    [TestMethod]
    public void Run_RemovesAnEarlierRunsRecalibrationAudit()
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "off", 0d, 0d, "Mass recalibration: False");
        var folder = Directory.CreateDirectory(Path.Combine(output, "mass_recalibration")).FullName;
        File.WriteAllText(Path.Combine(folder, "mass_recalibration_models.tsv"), "from an earlier run");
        File.WriteAllText(Path.Combine(folder, "old.annotation_calibrants.tsv"), "from an earlier run");

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        StringAssert.Contains(stdout, "Removed 2 mass recalibration file(s) of an earlier run");
        Assert.AreEqual(0, Directory.GetFiles(folder).Length);
    }

    [TestMethod]
    [DataRow("Mass recalibration: maybe", "is neither True nor False")]
    [DataRow("Mass recalibration: True\nMass recalibration search window (ppm): 0", "is not a positive number")]
    [DataRow("Mass recalibration: True\nMass recalibration minimum correction (ppm): -1", "is not a number of ppm, zero or more")]
    public void Run_IsRefusedBeforeAnythingIsRead(string lines, string reason)
    {
        using var directory = new ScratchDirectory();
        var (input, output, methodFile) = Prepare(directory, "refused", Ms1Shift, Ms2Shift, lines);
        var before = Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray();

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(-1, result);
        StringAssert.Contains(stderr, reason);
        CollectionAssert.AreEqual(before, Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray(), "nothing was processed");
    }

    [TestMethod]
    public void Run_IsRefusedWithoutALibrary()
    {
        using var directory = new ScratchDirectory();
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        SyntheticLcmsMzml.WriteSwath(Path.Combine(input, "s1.mzML"), Compounds.Select(compound => (ProtonatedMz(compound.Formula), compound.Rt, compound.Height)).ToList(), Windows, Ms2Shift);
        var methodFile = directory.File("method.txt", "Acquisition type: SWATH\nIon mode: Positive\nMass recalibration: True\n");

        var (result, _, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(-1, result);
        StringAssert.Contains(stderr, "Mass recalibration needs an MSP or LBM library with MS/MS spectra");
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

    // An alignment matrix: the rows below its header line, the one that names the alignment ID.
    private static List<Dictionary<string, string>> ReadMatrix(string path)
    {
        var lines = File.ReadAllLines(path);
        var start = Array.FindIndex(lines, line => line.StartsWith("Alignment ID\t", StringComparison.Ordinal));
        Assert.IsTrue(start >= 0, $"no header line in {Path.GetFileName(path)}");
        var header = lines[start].Split('\t');
        return lines.Skip(start + 1)
            .Select(line => line.Split('\t'))
            .Select(cells => header.Select((name, index) => (name, value: index < cells.Length ? cells[index] : string.Empty)).GroupBy(pair => pair.name).ToDictionary(group => group.Key, group => group.First().value))
            .ToList();
    }

    // The one file a run wrote with this extension, wherever under its input and output folders it went.
    private static string Written(string extension, params string[] folders)
    {
        var found = folders.SelectMany(folder => Directory.GetFiles(folder, extension, SearchOption.AllDirectories)).ToList();
        Assert.AreEqual(1, found.Count, $"{extension}: {string.Join(", ", folders.SelectMany(folder => Directory.GetFiles(folder, "*", SearchOption.AllDirectories)).Select(Path.GetFileName))}");
        return found[0];
    }

    private sealed class ScratchDirectory : IDisposable
    {
        public ScratchDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "msdial-mass-recalibration-" + Guid.NewGuid().ToString("N"));
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
