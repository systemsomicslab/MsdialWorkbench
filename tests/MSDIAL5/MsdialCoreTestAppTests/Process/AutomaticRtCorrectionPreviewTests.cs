using CompMs.App.MsdialConsole.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The automatic alignment RT correction's preview, run by the Console on real files: it stops
/// after the anchors and the audit, and saves nothing beside the raw data.
/// </summary>
[TestClass]
public sealed class AutomaticRtCorrectionPreviewTests
{
    private static readonly (double Mz, double Rt)[] Standards = { (200.1, 2d), (300.2, 5d), (400.3, 8d) };

    [TestMethod]
    public void Preview_WritesTheAnchorsAuditAndSavesNothingBesideTheRawData()
    {
        using var directory = new ScratchDirectory();
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        // Three injections, the second migrating 0.1 min later and the third 0.1 min earlier.
        var shifts = new Dictionary<string, double> { ["s1"] = 0d, ["s2"] = 0.1d, ["s3"] = -0.1d };
        foreach (var (name, shift) in shifts) {
            SyntheticLcmsMzml.Write(Path.Combine(input, name + ".mzML"), Standards.Select(standard => (standard.Mz, standard.Rt + shift, 100000d)).ToList());
        }
        var before = Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray();
        var library = directory.File(
            "anchors.txt",
            "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F\n"
            + "IS1\t2\t1\t200.1\t0.01\t1000\tTrue\n"
            + "IS2\t5\t1\t300.2\t0.01\t1000\tTrue\n"
            + "IS3\t8\t1\t400.3\t0.01\t1000\tTrue\n");
        var methodFile = directory.File(
            "method.txt",
            """
            Execute automatic RT correction for alignment: True
            Together with alignment: True
            Automatic RT correction anchor mode: UserOnly
            Automatic RT correction anchor library file path: anchors.txt
            Automatic RT correction preview only: True
            Number of threads: 2
            """);
        // An earlier run's audit in the output folder goes before anything is read.
        File.WriteAllText(Path.Combine(output, "automatic_alignment_rt_correction_exclusions.tsv"), "from an earlier run");

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        StringAssert.Contains(stdout, "Removed 1 automatic alignment RT correction audit file(s) of an earlier run");
        StringAssert.Contains(stdout, "preview: 3 anchor(s) in reference file s1 (3 user, 0 automatic)");
        Assert.IsFalse(stdout.Contains("Alignment started"), stdout);
        CollectionAssert.AreEqual(before, Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray(),
            "no .pai, .dcl or anything else beside the raw data");
        Assert.AreEqual(0, Directory.GetDirectories(input).Length);
        Assert.AreEqual(0, Directory.GetDirectories(output, "automatic_alignment_rt_correction_preview_peaks*").Length, "the scratch peaks are removed");
        Assert.IsFalse(File.Exists(Path.Combine(output, "automatic_alignment_rt_correction_exclusions.tsv")));

        var summary = ReadTsv(Path.Combine(output, "automatic_alignment_rt_correction_summary.tsv"));
        Assert.AreEqual(3, summary.Count);
        Assert.IsTrue(summary.All(row => row["Run"] == "Preview" && row["Anchor mode"] == "UserOnly" && row["Anchor library"] == "anchors.txt"));
        using (var sha = System.Security.Cryptography.SHA256.Create()) {
            var checksum = string.Concat(sha.ComputeHash(File.ReadAllBytes(library)).Select(value => value.ToString("x2")));
            Assert.IsTrue(summary.All(row => row["Anchor library SHA-256"] == checksum));
        }
        Assert.IsTrue(summary.All(row => row["Used anchors"] == "3"), "every file corrected from the three standards");

        var anchors = ReadTsv(Path.Combine(output, "automatic_alignment_rt_correction_anchors.tsv"));
        Assert.AreEqual(9, anchors.Count);
        Assert.IsTrue(anchors.All(row => row["Anchor source"] == "User" && row["Used"] == "True"));
        foreach (var row in anchors) {
            var file = row["File name"];
            var standard = Standards.Single(item => row["Anchor name"] == $"IS{Array.IndexOf(Standards, item) + 1}");
            Assert.AreEqual(standard.Rt + shifts[file], Number(row["Original RT (min)"]), 0.03d, $"{file} {row["Anchor name"]}");
            Assert.AreEqual(standard.Rt, Number(row["Reference RT (min)"]), 0.03d, "the reference file's observed RT");
        }
    }

    [TestMethod]
    public void Preview_ThatFailsWritesWhatItReadAndSaysWhy()
    {
        using var directory = new ScratchDirectory();
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        foreach (var name in new[] { "s1", "s2" }) {
            // The third standard is in no file.
            SyntheticLcmsMzml.Write(Path.Combine(input, name + ".mzML"), Standards.Take(2).Select(standard => (standard.Mz, standard.Rt, 100000d)).ToList());
        }
        directory.File(
            "anchors.txt",
            "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F\n"
            + "IS1\t2\t1\t200.1\t0.01\t1000\tTrue\n"
            + "IS2\t5\t1\t300.2\t0.01\t1000\tTrue\n"
            + "IS3\t8\t1\t400.3\t0.01\t1000\tTrue\n");
        var methodFile = directory.File(
            "method.txt",
            """
            Execute automatic RT correction for alignment: True
            Together with alignment: True
            Automatic RT correction anchor mode: UserOnly
            Automatic RT correction anchor library file path: anchors.txt
            Automatic RT correction preview only: True
            """);

        var (result, _, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(-1, result);
        StringAssert.Contains(stderr, "at least 3 of the 3 user anchors");
        var anchors = ReadTsv(Path.Combine(output, "automatic_alignment_rt_correction_anchors.tsv"));
        Assert.AreEqual(6, anchors.Count, "every standard in every file");
        Assert.IsTrue(anchors.Where(row => row["Anchor name"] == "IS3").All(row => row["Status"] == "Missing" && row["Detection"] == "No signal at the anchor's m/z."));
        Assert.IsTrue(anchors.Where(row => row["Anchor name"] != "IS3").All(row => row["Status"] == "Detected"));
        Assert.IsTrue(ReadTsv(Path.Combine(output, "automatic_alignment_rt_correction_summary.tsv")).All(row => row["Run"] == "Preview" && row["Note"].StartsWith("Not corrected: ")));
        Assert.AreEqual(0, Directory.GetDirectories(output, "automatic_alignment_rt_correction_preview_peaks*").Length, "the scratch peaks are removed on failure too");
    }

    [TestMethod]
    public void Preview_ChoosesEachAnchorsPeakAsTheMethodFileSays()
    {
        using var directory = new ScratchDirectory();
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        // Two peaks in IS2's window: a taller one 0.6 min from its library RT, a lower one 0.1 min.
        var compounds = new List<(double Mz, double Rt, double Height)> {
            (200.1, 2d, 100000d),
            (300.2, 4.4d, 300000d),
            (300.204, 5.1d, 100000d),
            (400.3, 8d, 100000d),
        };
        foreach (var name in new[] { "s1", "s2" }) {
            SyntheticLcmsMzml.Write(Path.Combine(input, name + ".mzML"), compounds);
        }
        directory.File(
            "anchors.txt",
            "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F\n"
            + "IS1\t2\t1\t200.1\t0.01\t1000\tTrue\n"
            + "IS2\t5\t1\t300.2\t0.01\t1000\tTrue\n"
            + "IS3\t8\t1\t400.3\t0.01\t1000\tTrue\n");
        var methodFile = directory.File(
            "method.txt",
            """
            Execute automatic RT correction for alignment: True
            Together with alignment: True
            Automatic RT correction anchor mode: UserOnly
            Automatic RT correction anchor library file path: anchors.txt
            Automatic RT correction anchor peak selection mode: ClosestToReferenceRt
            Automatic RT correction preview only: True
            """);

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        var is2 = ReadTsv(Path.Combine(output, "automatic_alignment_rt_correction_anchors.tsv")).Where(row => row["Anchor name"] == "IS2").ToList();
        Assert.AreEqual(2, is2.Count);
        Assert.IsTrue(is2.All(row => Math.Abs(Number(row["Original RT (min)"]) - 5.1d) < 0.03d), "the closer peak, not the taller one");
        Assert.IsTrue(is2.All(row => row["Detection"] == "The closest to the library RT of 2 peaks in the window."));
    }

    [TestMethod]
    public void Run_RefusesWhenAnEarlierAuditItWouldReplaceIsOpen()
    {
        using var directory = new ScratchDirectory();
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        SyntheticLcmsMzml.Write(Path.Combine(input, "s1.mzML"), Standards.Select(standard => (standard.Mz, standard.Rt, 100000d)).ToList());
        directory.File(
            "anchors.txt",
            "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F\n"
            + "IS1\t2\t1\t200.1\t0.01\t1000\tTrue\n"
            + "IS2\t5\t1\t300.2\t0.01\t1000\tTrue\n"
            + "IS3\t8\t1\t400.3\t0.01\t1000\tTrue\n");
        var methodFile = directory.File(
            "method.txt",
            """
            Execute automatic RT correction for alignment: True
            Together with alignment: True
            Automatic RT correction anchor mode: UserOnly
            Automatic RT correction anchor library file path: anchors.txt
            Automatic RT correction preview only: True
            """);
        var anchorsAudit = Path.Combine(output, "automatic_alignment_rt_correction_anchors.tsv");
        var summaryAudit = Path.Combine(output, "automatic_alignment_rt_correction_summary.tsv");
        File.WriteAllText(summaryAudit, "an earlier run's summary");
        File.WriteAllText(anchorsAudit, "an earlier run's anchors");

        int result;
        string stderr;
        using (File.Open(anchorsAudit, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            (result, _, stderr) = RunLcms(input, output, methodFile);
        }

        Assert.AreEqual(-1, result);
        StringAssert.Contains(stderr, "automatic_alignment_rt_correction_anchors.tsv cannot be replaced");
        Assert.AreEqual("an earlier run's summary", File.ReadAllText(summaryAudit), "nothing is removed when one file cannot be");
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "msdial-autort-preview-" + Guid.NewGuid().ToString("N"));
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
