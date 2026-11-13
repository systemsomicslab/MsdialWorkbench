using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The <c>msn</c> command on MSP input: which record each row names, the exit codes, the method-file
/// values it refuses, and a run without a console. The MSP files are synthetic and written here.
/// </summary>
[TestClass]
public sealed class MsnCommandTests
{
    private string _scratch = null!;

    [TestInitialize]
    public void Initialize() {
        _scratch = Path.Combine(Path.GetTempPath(), $"msdial-msn-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_scratch);
    }

    [TestCleanup]
    public void Cleanup() {
        if (Directory.Exists(_scratch)) {
            Directory.Delete(_scratch, recursive: true);
        }
    }

    internal sealed record MspRecord(string Name, double Precursor, string IonMode, string? DbId, int? PeakId, (double Mz, double Intensity)[] Peaks, string? InChIKey = null, string? Adduct = null);

    private static readonly (double, double)[] SharedSpectrum = [(100.05, 300d), (150.07, 1000d), (200.1, 500d), (250.12, 50d)];

    internal static string WriteMsp(string path, IEnumerable<MspRecord> records) {
        var builder = new StringBuilder();
        foreach (var record in records) {
            builder.Append("NAME: ").Append(record.Name).Append('\n');
            builder.Append("PRECURSORMZ: ").Append(record.Precursor.ToString(CultureInfo.InvariantCulture)).Append('\n');
            if (record.Adduct is not null) builder.Append("PRECURSORTYPE: ").Append(record.Adduct).Append('\n');
            builder.Append("IONMODE: ").Append(record.IonMode).Append('\n');
            if (record.InChIKey is not null) builder.Append("INCHIKEY: ").Append(record.InChIKey).Append('\n');
            if (record.DbId is not null) builder.Append("DatabaseUniqueIdentifier: ").Append(record.DbId).Append('\n');
            if (record.PeakId is not null) builder.Append("COMMENT: PEAKID=").Append(record.PeakId.Value.ToString(CultureInfo.InvariantCulture)).Append("|ALIGNID=").Append(record.PeakId.Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
            builder.Append("Num Peaks: ").Append(record.Peaks.Length.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var (mz, intensity) in record.Peaks) {
                builder.Append(mz.ToString(CultureInfo.InvariantCulture)).Append('\t').Append(intensity.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
            builder.Append('\n');
        }
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        return path;
    }

    private string WriteMethod(params string[] lines) {
        var method = Path.Combine(_scratch, $"method-{Guid.NewGuid():N}.txt");
        File.WriteAllLines(method, lines);
        return method;
    }

    private string DefaultMethod(string similarity = "Bonanza") => WriteMethod(
        $"MsmsSimilarityCalc: {similarity}",
        "MnMassTolerance: 0.025",
        "MnSpectrumSimilarityCutOff: 0",
        "MinimumPeakMatch: 1",
        "MaxEdgeNumberPerNode: 10",
        "MaxPrecursorDifference: 400",
        "MnRelativeAbundanceCutOff: 0",
        "MnAbsoluteAbundanceCutOff: 0");

    internal static int Invoke(params string[] args) {
        var root = new RootCommand("test");
        MainProcess.SetMsnCommand(root);
        return root.Parse(args).Invoke();
    }

    /// <summary>
    /// Records 0 (no peaks), 1 (negative) and 3 (no peaks) are filtered out before scoring. The rows
    /// name the scored records: their DatabaseUniqueIdentifier, or else their ordinal in the
    /// unfiltered file. The command used to look the IDs up by ordinal in the filtered list, which
    /// named record 5 for record 2 and threw for record 4.
    /// </summary>
    [TestMethod]
    public void RowsNameTheScoredRecordsWhenRecordsBeforeThemAreFilteredOut() {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), [
            new MspRecord("r0", 400d, "Positive", "DB-0", 0, []),
            new MspRecord("r1", 400d, "Negative", "DB-1", 1, SharedSpectrum),
            new MspRecord("r2", 400d, "Positive", "DB-A", 2, SharedSpectrum),
            new MspRecord("r3", 400d, "Positive", null, 3, []),
            new MspRecord("r4", 400d, "Positive", null, 4, SharedSpectrum),
            new MspRecord("r5", 400d, "Positive", "DB-C", 5, SharedSpectrum),
        ]);
        var output = Path.Combine(_scratch, "out.pairs");

        var exitCode = Invoke("msn", "-i", msp, "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive");

        Assert.AreEqual(MsnExitCode.Success, exitCode);
        var lines = File.ReadAllLines(output);
        Assert.AreEqual("SourceID\tTargetID\tScore\tMatchPeakCount", lines[0]);
        CollectionAssert.AreEqual(
            new[] { "DB-A\t4", "DB-A\tDB-C", "4\tDB-C" },
            lines.Skip(1).Select(line => string.Join("\t", line.Split('\t').Take(2))).ToArray());
        Assert.IsFalse(File.Exists(output + ".partial"));
    }

    [TestMethod]
    public void TargetRowsNameTheScoredQueryAndTargetRecords() {
        var query = WriteMsp(Path.Combine(_scratch, "query.msp"), [
            new MspRecord("q0", 400d, "Positive", null, 0, []),
            new MspRecord("q1", 400d, "Positive", null, 1, SharedSpectrum),
        ]);
        var target = WriteMsp(Path.Combine(_scratch, "target.msp"), [
            new MspRecord("t0", 400d, "Negative", "T-0", null, SharedSpectrum),
            new MspRecord("t1", 400d, "Positive", null, null, []),
            new MspRecord("t2", 400d, "Positive", "T-2", null, SharedSpectrum),
            new MspRecord("t3", 400d, "Positive", null, null, SharedSpectrum),
        ]);
        var output = Path.Combine(_scratch, "out.pairs");

        var exitCode = Invoke("msn", "-i", query, "-t", target, "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive");

        Assert.AreEqual(MsnExitCode.Success, exitCode);
        CollectionAssert.AreEqual(
            new[] { "1\tT-2", "1\t3" },
            File.ReadAllLines(output).Skip(1).Select(line => string.Join("\t", line.Split('\t').Take(2))).ToArray());
    }

    [TestMethod]
    public void AnOutputThatCannotBeWrittenFailsWithOneAndLeavesNoOutput() {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), [
            new MspRecord("a", 400d, "Positive", null, 0, SharedSpectrum),
            new MspRecord("b", 400d, "Positive", null, 1, SharedSpectrum),
        ]);
        var output = Path.Combine(_scratch, "out.pairs");

        int exitCode;
        // Another process holding the partial file is an I/O failure the command cannot write past.
        using (new FileStream(output + ".partial", FileMode.Create, FileAccess.ReadWrite, FileShare.None)) {
            exitCode = Invoke("msn", "-i", msp, "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive");
        }

        Assert.AreEqual(MsnExitCode.Failure, exitCode);
        Assert.IsFalse(File.Exists(output));
    }

    [TestMethod]
    public void AnEarlierOutputIsReplacedOnlyByACompleteOne() {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), [
            new MspRecord("a", 400d, "Positive", null, 0, SharedSpectrum),
            new MspRecord("b", 400d, "Positive", null, 1, SharedSpectrum),
        ]);
        var output = Path.Combine(_scratch, "out.pairs");
        File.WriteAllText(output, "earlier run\n");

        using (new FileStream(output + ".partial", FileMode.Create, FileAccess.ReadWrite, FileShare.None)) {
            Assert.AreEqual(MsnExitCode.Failure, Invoke("msn", "-i", msp, "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive"));
        }
        Assert.AreEqual("earlier run\n", File.ReadAllText(output));

        Assert.AreEqual(MsnExitCode.Success, Invoke("msn", "-i", msp, "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive"));
        Assert.AreEqual(2, File.ReadAllLines(output).Length);
        Assert.IsFalse(File.Exists(output + ".partial"));
    }

    [DataTestMethod]
    [DataRow("bonanza")]
    [DataRow("BONANZA")]
    [DataRow("Bonanza")]
    [DataRow("all")]
    public void AKernelNameIsReadInAnyLetterCase(string name) {
        var parameter = ConfigParser.ReadForMoleculerNetworkingParameter(WriteMethod($"MsmsSimilarityCalc: {name}"));

        Assert.AreEqual(name.ToLowerInvariant(), parameter.MsmsSimilarityCalc.ToString().ToLowerInvariant());
    }

    [DataTestMethod]
    [DataRow("Foo")]
    [DataRow("7")]
    [DataRow("1")]
    [DataRow("Bonanza, Cosine")]
    [DataRow("")]
    public void AnUnusableKernelNameStopsTheRunWithTwo(string name) {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), [
            new MspRecord("a", 400d, "Positive", null, 0, SharedSpectrum),
            new MspRecord("b", 400d, "Positive", null, 1, SharedSpectrum),
        ]);
        var output = Path.Combine(_scratch, "out.pairs");

        var exitCode = Invoke("msn", "-i", msp, "-o", output, "-m", WriteMethod($"MsmsSimilarityCalc: {name}"), "--ionmode", "Positive");

        Assert.AreEqual(MsnExitCode.UnusableInput, exitCode);
        Assert.IsFalse(File.Exists(output));
    }

    [TestMethod]
    public void EveryUnusableNetworkingValueIsNamedAndOtherKeysStaySilent() {
        var method = WriteMethod(
            "MsmsSimilarityCalc: Foo",
            "MnMassTolerance: 0,025",
            "Some LC-MS key: whatever",
            "MaxPrecursorDifference: 100");

        var ex = Assert.ThrowsException<MethodFileException>(() => ConfigParser.ReadForMoleculerNetworkingParameter(method));

        CollectionAssert.AreEqual(new[] { "MsmsSimilarityCalc: Foo", "MnMassTolerance: 0,025" }, ex.UnusableLines.ToArray());
    }

    [TestMethod]
    public void NetworkingNumbersAreReadAsTheDoublesTheyAreWrittenAs() {
        var parameter = ConfigParser.ReadForMoleculerNetworkingParameter(WriteMethod(
            "MnMassTolerance: 0.025",
            "MnRelativeAbundanceCutOff: 0.1",
            "MaxPrecursorDifference: 100000"));

        // A float cast made these 0.0250000004 and 0.100000001.
        Assert.AreEqual(0.025, parameter.MnMassTolerance);
        Assert.AreEqual(0.1, parameter.MnRelativeAbundanceCutOff);
        Assert.AreEqual(100000d, parameter.MaxPrecursorDifference);
    }

    [TestMethod]
    public void AMissingTargetFileStopsTheRunWithTwo() {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), [
            new MspRecord("a", 400d, "Positive", null, 0, SharedSpectrum),
            new MspRecord("b", 400d, "Positive", null, 1, SharedSpectrum),
        ]);
        var output = Path.Combine(_scratch, "out.pairs");

        var exitCode = Invoke("msn", "-i", msp, "-t", Path.Combine(_scratch, "missing.msp"), "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive");

        Assert.AreEqual(MsnExitCode.UnusableInput, exitCode, "it used to network the input with itself instead");
        Assert.IsFalse(File.Exists(output));
    }

    [TestMethod]
    public void ATargetFileWithAnInputFolderIsRejectedWithTwo() {
        var folder = Path.Combine(_scratch, "in");
        Directory.CreateDirectory(folder);
        WriteMsp(Path.Combine(folder, "a.msp"), [new MspRecord("a", 400d, "Positive", null, 0, SharedSpectrum)]);
        var target = WriteMsp(Path.Combine(_scratch, "target.msp"), [new MspRecord("t", 400d, "Positive", null, null, SharedSpectrum)]);

        var exitCode = Invoke("msn", "-i", folder, "-t", target, "-o", Path.Combine(_scratch, "out"), "-m", DefaultMethod(), "--ionmode", "Positive");

        Assert.AreEqual(MsnExitCode.UnusableInput, exitCode);
    }

    [TestMethod]
    public void AFolderRunSucceedsWithZeroAndWritesNoPartialFiles() {
        var folder = Path.Combine(_scratch, "in");
        Directory.CreateDirectory(folder);
        WriteMsp(Path.Combine(folder, "a.msp"), [
            new MspRecord("a0", 400d, "Positive", null, 0, SharedSpectrum),
            new MspRecord("a1", 400d, "Positive", null, 1, SharedSpectrum),
        ]);
        WriteMsp(Path.Combine(folder, "b.msp"), [
            new MspRecord("b0", 400d, "Positive", null, 0, SharedSpectrum),
            new MspRecord("b1", 400d, "Positive", null, 1, SharedSpectrum),
        ]);
        var outputFolder = Path.Combine(_scratch, "out");

        var exitCode = Invoke("msn", "-i", folder, "-o", outputFolder, "-m", DefaultMethod(), "--ionmode", "Positive");

        Assert.AreEqual(MsnExitCode.Success, exitCode);
        var written = Directory.GetFiles(outputFolder).Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(new[] { "a_mn_a.pairs", "a_mn_b.pairs", "b_mn_b.pairs" }, written);
    }

    /// <summary>
    /// A Python subprocess runs msn with every standard handle redirected. The progress line used to
    /// move the cursor, which throws IOException when there is no console; it was reached at the
    /// 100th record.
    /// </summary>
    [TestMethod]
    public void ARunWithEveryStandardHandleRedirectedSucceeds() {
        var records = Enumerable.Range(0, 250)
            .Select(i => new MspRecord($"r{i}", 300d + i, "Positive", null, i, [(100.05, 300d), (150.07 + i % 7, 1000d), (200.1, 500d)]))
            .ToList();
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), records);
        var output = Path.Combine(_scratch, "out.pairs");
        var console = Path.Combine(AppContext.BaseDirectory, "MSDIALCUI.exe");
        Assert.IsTrue(File.Exists(console), console);

        var info = new ProcessStartInfo(console) {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in new[] { "msn", "-i", msp, "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive" }) {
            info.ArgumentList.Add(arg);
        }
        using var process = System.Diagnostics.Process.Start(info)!;
        process.StandardInput.Close();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        Assert.IsTrue(process.WaitForExit(120_000), "msn did not finish");

        Assert.AreEqual(MsnExitCode.Success, process.ExitCode, stdout.Result + stderr.Result);
        Assert.IsTrue(File.Exists(output));
        StringAssert.Contains(stderr.Result, "100 / 250");
    }
}
