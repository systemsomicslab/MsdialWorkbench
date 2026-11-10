using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The automatic alignment RT correction run by the Console on real files beyond a UserOnly
/// preview: a Hybrid preview with an exclusion, and production runs that go on through alignment
/// and gap filling, in the standard and the light alignment (the final review's CONF-15, CONF-19
/// and CONF-58, and decision 2 at the Console level).
/// </summary>
[TestClass]
public sealed class AutomaticRtCorrectionProductionTests
{
    private const string LibraryHeader = "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F\n";

    [TestMethod]
    public void HybridPreview_WritesTheExclusionAuditAndDoesNotChooseTheExcludedCandidate()
    {
        using var directory = new ScratchDirectory();
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        var compounds = new List<(double Mz, double Rt, double Height)> {
            (150.1, 1.5, 100000d), (250.2, 3d, 100000d), (350.3, 4.5, 100000d), (305.15, 5.25, 100000d),
            (450.4, 6d, 100000d), (550.5, 7.5, 100000d), (650.6, 9d, 100000d),
        };
        // The raw data reader expects every spectrum in m/z order.
        compounds = compounds.OrderBy(compound => compound.Mz).ToList();
        foreach (var (name, shift) in new[] { ("s1", 0d), ("s2", 0.1d), ("s3", -0.1d) }) {
            SyntheticLcmsMzml.Write(Path.Combine(input, name + ".mzML"), compounds.Select(compound => (compound.Mz, compound.Rt + shift, compound.Height)).ToList());
        }
        var before = Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray();
        directory.File(
            "anchors.txt",
            LibraryHeader
            + "IS1\t5.25\t0.5\t305.15\t0.01\t1000\tTrue\n"
            + "rejected on review\t6\t0.1\t450.4\t0.01\t1000\tFalse\n");
        var methodFile = directory.File(
            "method.txt",
            """
            Execute automatic RT correction for alignment: True
            Together with alignment: True
            Automatic RT correction anchor mode: Hybrid
            Automatic RT correction anchor library file path: anchors.txt
            Automatic RT correction preview only: True
            Automatic RT correction intensity quantile: 0
            Automatic RT correction maximum peak width quantile: 1
            Number of threads: 2
            """);

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        CollectionAssert.AreEqual(before, Directory.GetFiles(input).Select(Path.GetFileName).OrderBy(name => name).ToArray(), "a preview saves nothing beside the raw data");
        var summary = ReadTsv(Path.Combine(output, "automatic_alignment_rt_correction_summary.tsv"));
        Assert.IsTrue(summary.All(row => row["Run"] == "Preview" && row["Anchor mode"] == "Hybrid" && row["Analysis batch"] == "1"));
        Assert.AreEqual("s1", summary.Single(row => row["Model source"] == "Reference")["File name"], "the file the library's RT was written for");
        Assert.IsTrue(summary.All(row => row["Reference selection"] == "Automatic (most user anchors, closest to library RT)" && row["User anchors detected"] == "1"));
        var exclusions = ReadTsv(Path.Combine(output, "automatic_alignment_rt_correction_exclusions.tsv"));
        Assert.AreEqual("rejected on review", exclusions.Single()["Anchor name"]);
        Assert.AreEqual("1", exclusions.Single()["Excluded automatic candidates"]);
        var anchors = ReadTsv(Path.Combine(output, "automatic_alignment_rt_correction_anchors.tsv"));
        Assert.IsTrue(anchors.Any(row => row["Anchor source"] == "User" && row["Anchor name"] == "IS1" && row["Used"] == "True"));
        Assert.IsTrue(anchors.Any(row => row["Anchor source"] == "Automatic" && row["Used"] == "True"), "automatic anchors fill the other bins");
        Assert.IsFalse(anchors.Any(row => Math.Abs(Number(row["m/z"]) - 450.4) < 0.01), "the excluded candidate is not an anchor");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void UserOnlyProduction_AlignsOnTheReferenceAxis_GapFillsThere_AndKeepsPeakPickingOnTheOriginalAxis(bool lightMode)
    {
        using var directory = new ScratchDirectory();
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        var standards = new[] { (Mz: 200.1, Rt: 2d), (Mz: 300.2, Rt: 5d), (Mz: 400.3, Rt: 8d) };
        // s2 and s3 run 0.2 and 0.3 min late, so an uncorrected alignment would put the spots
        // 0.17 min after the reference. The compound at m/z 777.7 is picked in s1 and s2; in s3
        // it lies below the minimum peak height, at original 7.3, and only the gap filling finds
        // it. The Blank, injected between s1 and s2, runs 0.1 min late.
        var shifts = new Dictionary<string, double> { ["s1"] = 0d, ["b1"] = 0.1d, ["s2"] = 0.2d, ["s3"] = 0.3d };
        foreach (var (name, shift) in shifts) {
            var compounds = standards.Select(standard => (standard.Mz, standard.Rt + shift, 100000d)).ToList();
            if (name is "s1" or "s2" or "s3") {
                compounds.Add((777.7, 7d + shift, name == "s3" ? 600d : 50000d));
            }
            SyntheticLcmsMzml.Write(Path.Combine(input, name + ".mzML"), compounds);
        }
        var csv = directory.File(
            "analysis_files.csv",
            "file_path,file_name,file_type,class_id,acquisition_type,batch_order,analytical_order,factor\n"
            + $"{Path.Combine(input, "s1.mzML")},s1,Sample,S,DDA,1,1,1\n"
            + $"{Path.Combine(input, "b1.mzML")},b1,Blank,B,DDA,1,2,1\n"
            + $"{Path.Combine(input, "s2.mzML")},s2,Sample,S,DDA,1,3,1\n"
            + $"{Path.Combine(input, "s3.mzML")},s3,Sample,S,DDA,1,4,1\n");
        directory.File(
            "anchors.txt",
            LibraryHeader
            + "IS1\t2\t1\t200.1\t0.01\t1000\tTrue\n"
            + "IS2\t5\t1\t300.2\t0.01\t1000\tTrue\n"
            + "IS3\t8\t1\t400.3\t0.01\t1000\tTrue\n");
        var methodFile = directory.File(
            "method.txt",
            $"""
            Execute automatic RT correction for alignment: True
            Together with alignment: True
            Automatic RT correction anchor mode: UserOnly
            Automatic RT correction anchor library file path: anchors.txt
            Alignment reference file ID: 3
            Minimum peak height: 1000
            Alignment light mode: {lightMode}
            Number of threads: 2
            """);

        var (result, stdout, stderr) = RunLcms(csv, output, methodFile);

        Assert.AreEqual(0, result, stderr + stdout);
        StringAssert.Contains(stdout, "Alignment reference file ID: 0, the automatic alignment RT correction's reference, in place of 3");
        var summary = ReadTsv(Path.Combine(output, "automatic_alignment_rt_correction_summary.tsv")).ToDictionary(row => row["File name"]);
        Assert.IsTrue(summary.Values.All(row => row["Run"] == "Run" && row["Reference selection"].Length > 0 && row["Analysis batch"] == "1"));
        Assert.AreEqual("Reference", summary["s1"]["Model source"]);
        Assert.AreEqual("InterpolatedBlank", summary["b1"]["Model source"]);
        Assert.AreEqual("Interpolated from file IDs 0 and 2 (analysis batch 1).", summary["b1"]["Note"]);

        // Peak picking stays on the original axis.
        var s3Peaks = ReadTsv(Path.Combine(output, "s3.mdpeak"));
        Assert.IsTrue(s3Peaks.Any(row => Math.Abs(Number(row["Precursor m/z"]) - 200.1) < 0.01 && Math.Abs(Number(row["RT (min)"]) - 2.3) < 0.03),
            "s3's own peak at its original RT");

        // The aligned RTs are the reference file's, the gap-filled cell included.
        var qa = ReadTsv(Directory.GetFiles(output, "*.qa.tsv").Single());
        foreach (var standard in standards) {
            var rows = qa.Where(row => Math.Abs(Number(row["MZ"]) - standard.Mz) < 0.01).ToList();
            Assert.AreEqual(4, rows.Count, $"m/z {standard.Mz}: one spot, every file");
            Assert.IsTrue(rows.All(row => Number(row["Height"]) > 10000d), $"m/z {standard.Mz}: detected in every file");
            Assert.IsTrue(rows.All(row => Math.Abs(Number(row["RT"]) - standard.Rt) < 0.03), $"m/z {standard.Mz}: " + string.Join(", ", rows.Select(row => row["RT"])));
        }
        var late = qa.Where(row => Math.Abs(Number(row["MZ"]) - 777.7) < 0.01).ToDictionary(row => row["File"]);
        Assert.IsTrue(new[] { "s1", "s2" }.All(file => Math.Abs(Number(late[file]["RT"]) - 7d) < 0.03));
        // The gap filler extracted at s3's original 7.3 and reports the corrected 7.0. Extracted
        // at 7.0 on s3's own axis it would have found only the peak's tail.
        Assert.AreEqual(7d, Number(late["s3"]["RT"]), 0.03d, "the gap-filled cell is on the corrected axis");
        Assert.IsTrue(Number(late["s3"]["Height"]) > 300d, $"the gap filling found the low peak at its original RT: height {late["s3"]["Height"]}");
    }

    // CONF-58: the light runner floored the gap-fill width at 0.2 min before the correction
    // mapped it back to the file's RTs; the standard path floors it after.
    [TestMethod]
    public void LightAccumulator_GivesTheGapFillerTheWidestDetectedPeakWithoutTheFloor()
    {
        var type = typeof(LcmsAlignmentLightRunner).GetNestedType("LightSpotAccumulator", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LightSpotAccumulator not found");
        var accumulator = Activator.CreateInstance(type, new AlignmentSpotProperty(), 2)!;
        var add = type.GetMethod("AddDetectedPeak")!;
        add.Invoke(accumulator, [Peak(0, 0.08d), AnalysisFileType.Sample, "A"]);
        add.Invoke(accumulator, [Peak(1, 0.1d), AnalysisFileType.Sample, "A"]);

        Assert.AreEqual(0.1d, (double)type.GetProperty("MaximumDetectedPeakWidth")!.GetValue(accumulator)!, 1e-9);
        Assert.AreEqual(0.2d, (double)type.GetProperty("PeakWidth")!.GetValue(accumulator)!, 1e-9, "the spot's width keeps its floor");
    }

    private static AlignmentChromPeakFeature Peak(int fileId, double width)
    {
        return new AlignmentChromPeakFeature {
            FileID = fileId,
            Mass = 100d,
            PeakHeightTop = 1000d,
            ChromXsLeft = new ChromXs(5d - width / 2d, ChromXType.RT, ChromXUnit.Min),
            ChromXsTop = new ChromXs(5d, ChromXType.RT, ChromXUnit.Min),
            ChromXsRight = new ChromXs(5d + width / 2d, ChromXType.RT, ChromXUnit.Min),
        };
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
            .Where(line => line.Length > 0)
            .Select(line => line.Split('\t'))
            .Select(cells => header.Select((name, index) => (name, value: index < cells.Length ? cells[index] : string.Empty)).GroupBy(pair => pair.name).ToDictionary(group => group.Key, group => group.First().value))
            .ToList();
    }

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    private sealed class ScratchDirectory : IDisposable
    {
        public ScratchDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "msdial-autort-production-" + Guid.NewGuid().ToString("N"));
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
