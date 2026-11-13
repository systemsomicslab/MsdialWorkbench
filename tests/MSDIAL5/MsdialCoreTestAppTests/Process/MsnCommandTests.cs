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
    [DataRow("modifiedcosine")]
    [DataRow("ModDotLegacy")]
    public void AKernelNameIsReadInAnyLetterCase(string name) {
        var parameter = ConfigParser.ReadForMoleculerNetworkingParameter(WriteMethod($"MsmsSimilarityCalc: {name}"));

        Assert.AreEqual(name.ToLowerInvariant(), parameter.MsmsSimilarityCalc.ToString().ToLowerInvariant());
    }

    [TestMethod]
    public void TheOldNameModDotIsReadAsModDotLegacyWithANotice() {
        var error = new StringWriter();
        var original = Console.Error;
        Console.SetError(error);
        try {
            var parameter = ConfigParser.ReadForMoleculerNetworkingParameter(WriteMethod("MsmsSimilarityCalc: ModDot"));

            Assert.AreEqual(MsmsSimilarityCalc.ModDotLegacy, parameter.MsmsSimilarityCalc);
        }
        finally {
            Console.SetError(original);
        }
        StringAssert.Contains(error.ToString(), "ModDot is read as ModDotLegacy");
    }

    [TestMethod]
    public void ThePrecursorWindowAndIntensityPowerAreRead() {
        var parameter = ConfigParser.ReadForMoleculerNetworkingParameter(WriteMethod("MnPrecursorWindow: 0", "MnIntensityPower: 0.5"));

        Assert.AreEqual(0d, parameter.MnPrecursorWindow);
        Assert.AreEqual(0.5, parameter.MnIntensityPower);
        Assert.AreEqual(17d, ConfigParser.ReadForMoleculerNetworkingParameter(WriteMethod("MsmsSimilarityCalc: Bonanza")).MnPrecursorWindow, "the default");
        Assert.ThrowsException<MethodFileException>(() => ConfigParser.ReadForMoleculerNetworkingParameter(WriteMethod("MnPrecursorWindow: -1")));
        Assert.ThrowsException<MethodFileException>(() => ConfigParser.ReadForMoleculerNetworkingParameter(WriteMethod("MnIntensityPower: 0")));
    }

    // ---- v2 ----

    private static List<MspRecord> V2Records() => [
        new MspRecord("hit A", 400d, "Positive", "DB-A", 10, SharedSpectrum, "AAAAAAAAAAAAAA-BBBBBBBBBB-N", "[M+H]+"),
        new MspRecord("no peaks", 400d, "Positive", "DB-X", 11, []),
        new MspRecord("Unknown", 414d, "Positive", null, 12, [(100.05, 300d), (164.07, 1000d), (200.1, 500d)]),
        new MspRecord("other mode", 400d, "Negative", null, 13, SharedSpectrum),
        new MspRecord("hit\tC", 400.01, "Positive", "DB-C", 14, [(100.05, 300d), (150.07, 900d)], "CCCCCCCCCCCCCC-DDDDDDDDDD-N", "[M+Na]+"),
        new MspRecord("no precursor", 0d, "Positive", null, 15, SharedSpectrum),
    ];

    private (int ExitCode, string[] Lines) RunV2(string msp, string? target, params string[] options) {
        var output = Path.Combine(_scratch, $"v2-{Guid.NewGuid():N}.tsv");
        var args = new List<string> { "msn", "-i", msp, "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive", "--format", "v2" };
        if (target is not null) args.AddRange(["-t", target]);
        args.AddRange(options);
        var exitCode = Invoke(args.ToArray());
        return (exitCode, File.Exists(output) ? File.ReadAllText(output).Split('\n').Where(line => line.Length > 0).ToArray() : []);
    }

    [TestMethod]
    public void V2WritesMetadataThenOneRowPerPairWithBothRecordsIdentified() {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), V2Records());

        var (exitCode, lines) = RunV2(msp, null, "--all-pairs", "--kernels", "ModifiedCosine,ModDotLegacy,Bonanza");

        Assert.AreEqual(MsnExitCode.Success, exitCode);
        var meta = lines.TakeWhile(line => line.StartsWith("#")).ToList();
        CollectionAssert.Contains(meta, "#msn_format=2");
        CollectionAssert.Contains(meta, "#mode=self");
        CollectionAssert.Contains(meta, "#target_file=self");
        CollectionAssert.Contains(meta, "#all_pairs=true");
        CollectionAssert.Contains(meta, "#excluded_query=no_peaks=1,wrong_ionmode=1,precursor_le_0=1");
        Assert.IsTrue(meta.Any(line => line.StartsWith("#query_file=query.msp sha256=", StringComparison.Ordinal)));
        Assert.IsFalse(meta.Any(line => line.Contains(_scratch)), "no absolute path in the header");
        var primary = meta.Single(line => line.StartsWith("#primary=", StringComparison.Ordinal));
        StringAssert.StartsWith(primary, "#primary=gnps.modified_cosine?assign=greedy&ipow=1&mzpow=0&prep=mn&prep.above=0.025&");

        var header = lines[meta.Count].Split('\t');
        CollectionAssert.AreEqual(new[] { "q_ordinal", "q_peak_id", "q_db_id", "q_name", "q_inchikey", "q_precursor_mz", "q_adduct" }, header.Take(7).ToArray());
        Assert.AreEqual("precursor_delta", header[14]);
        Assert.AreEqual(15 + 2 * 3, header.Length);
        StringAssert.StartsWith(header[15], "score[gnps.modified_cosine?");
        StringAssert.StartsWith(header[17], "score[msdial.moddot.legacy?");
        StringAssert.StartsWith(header[19], "score[msdial.bonanza.v1?");

        var rows = lines.Skip(meta.Count + 1).Select(line => line.Split('\t')).ToList();
        // Records 0, 2 and 4 are usable: C(3,2) pairs in (q, t) ordinal order.
        CollectionAssert.AreEqual(new[] { "0|2", "0|4", "2|4" }, rows.Select(r => r[0] + "|" + r[7]).ToArray());
        CollectionAssert.AreEqual(new[] { "0", "10", "DB-A", "hit A", "AAAAAAAAAAAAAA-BBBBBBBBBB-N" }, rows[0].Take(5).ToArray());
        Assert.AreEqual("hit C", rows[1][10], "a tab in a name becomes a space");
        Assert.AreEqual("[M+Na]+", rows[1][13]);
        Assert.IsTrue(rows.All(r => r.Length == header.Length));
    }

    [TestMethod]
    public void RedactionBlanksNamesInChIKeysAndDatabaseIdsButKeepsOrdinalsAndPeakIds() {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), V2Records());

        var (exitCode, lines) = RunV2(msp, null, "--all-pairs", "--redact-identity", "both");

        Assert.AreEqual(MsnExitCode.Success, exitCode);
        Assert.IsFalse(lines.Any(line => line.Contains("hit") || line.Contains("DB-") || line.Contains("AAAAAAAAAAAAAA")), string.Join("\n", lines));
        var rows = lines.SkipWhile(line => line.StartsWith("#")).Skip(1).Select(line => line.Split('\t')).ToList();
        Assert.AreEqual(3, rows.Count);
        Assert.IsTrue(rows.All(r => r[2] == "" && r[3] == "" && r[4] == "" && r[9] == "" && r[10] == "" && r[11] == ""));
        CollectionAssert.AreEqual(new[] { "10", "10", "12" }, rows.Select(r => r[1]).ToArray());
    }

    [TestMethod]
    public void QueryRedactionAgainstATargetKeepsTheTargetIdentities() {
        var query = WriteMsp(Path.Combine(_scratch, "query.msp"), V2Records());
        var target = WriteMsp(Path.Combine(_scratch, "target.msp"), [new MspRecord("public", 400d, "Positive", "PUB-1", null, SharedSpectrum, "PPPPPPPPPPPPPP-QQQQQQQQQQ-N")]);

        var (exitCode, lines) = RunV2(query, target, "--all-pairs", "--redact-identity", "query");

        Assert.AreEqual(MsnExitCode.Success, exitCode);
        var rows = lines.SkipWhile(line => line.StartsWith("#")).Skip(1).Select(line => line.Split('\t')).ToList();
        Assert.AreEqual(3, rows.Count, "three usable queries against one target");
        Assert.IsTrue(rows.All(r => r[2] == "" && r[3] == "" && r[4] == ""));
        Assert.IsTrue(rows.All(r => r[9] == "PUB-1" && r[10] == "public"));
        Assert.IsTrue(lines.Any(line => line.StartsWith("#target_file=target.msp sha256=", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void WithoutAllPairsTheCutoffAndMinimumMatchOfThePrimaryKernelApply() {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), V2Records());
        var method = WriteMethod("MsmsSimilarityCalc: Bonanza", "MinimumPeakMatch: 3", "MnSpectrumSimilarityCutOff: 0", "MaxPrecursorDifference: 400", "MnRelativeAbundanceCutOff: 0");
        var output = Path.Combine(_scratch, "cut.tsv");

        Assert.AreEqual(MsnExitCode.Success, Invoke("msn", "-i", msp, "-o", output, "-m", method, "--ionmode", "Positive", "--format", "v2"));

        var rows = File.ReadAllText(output).Split('\n').Where(line => line.Length > 0 && !line.StartsWith("#")).Skip(1).ToList();
        // Only records 0 and 2 share three peaks (the third through the precursor shift).
        Assert.AreEqual(1, rows.Count, string.Join("\n", rows));
    }

    [TestMethod]
    public void TheOutputIsByteIdenticalForAnyThreadCount() {
        var records = Enumerable.Range(0, 120)
            .Select(i => new MspRecord($"r{i}", 300d + i % 37, "Positive", null, i, [(100.05, 300d), (150.07 + i % 7, 1000d), (200.1 + i % 3, 500d), (250d + i % 11, 200d)]))
            .ToList();
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), records);
        var one = Path.Combine(_scratch, "one.tsv");
        var eight = Path.Combine(_scratch, "eight.tsv");

        Assert.AreEqual(MsnExitCode.Success, Invoke("msn", "-i", msp, "-o", one, "-m", DefaultMethod(), "--ionmode", "Positive", "--format", "v2", "--all-pairs", "--kernels", "All,ModifiedCosine", "--threads", "1"));
        Assert.AreEqual(MsnExitCode.Success, Invoke("msn", "-i", msp, "-o", eight, "-m", DefaultMethod(), "--ionmode", "Positive", "--format", "v2", "--all-pairs", "--kernels", "All,ModifiedCosine", "--threads", "8"));

        CollectionAssert.AreEqual(File.ReadAllBytes(one), File.ReadAllBytes(eight));
        Assert.AreEqual(120 * 119 / 2, File.ReadAllText(one).Split('\n').Count(line => line.Length > 0 && !line.StartsWith("#")) - 1);
    }

    [DataTestMethod]
    [DataRow("--format", "v1", "--all-pairs")]
    [DataRow("--format", "v1", "--kernels", "Bonanza")]
    [DataRow("--format", "v1", "--redact-identity", "both")]
    [DataRow("--format", "v3")]
    [DataRow("--format", "v2", "--kernels", "Cosine")]
    [DataRow("--format", "v2", "--kernels", "Bonanza,Foo")]
    [DataRow("--format", "v2", "--redact-identity", "names")]
    [DataRow("--format", "v2", "--threads", "0")]
    public void OptionsTheRunCannotHonourStopItWithTwo(params string[] options) {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), V2Records());
        var output = Path.Combine(_scratch, "out.tsv");

        var exitCode = Invoke(new[] { "msn", "-i", msp, "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive" }.Concat(options).ToArray());

        Assert.AreEqual(MsnExitCode.UnusableInput, exitCode, string.Join(" ", options));
        Assert.IsFalse(File.Exists(output));
    }

    [TestMethod]
    public void AMethodFileCosineIsRefusedInV2AndStillRunsInV1() {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), V2Records());
        var method = WriteMethod("MsmsSimilarityCalc: Cosine", "MinimumPeakMatch: 1", "MnSpectrumSimilarityCutOff: 0");

        Assert.AreEqual(MsnExitCode.UnusableInput, Invoke("msn", "-i", msp, "-o", Path.Combine(_scratch, "v2.tsv"), "-m", method, "--ionmode", "Positive", "--format", "v2"));
        Assert.AreEqual(MsnExitCode.Success, Invoke("msn", "-i", msp, "-o", Path.Combine(_scratch, "v1.tsv"), "-m", method, "--ionmode", "Positive"));
    }

    [TestMethod]
    public void V1WithModifiedCosineUsesItsOwnPreprocessing() {
        // The precursor-adjacent peaks are removed by the 17 Da window, so these two records share
        // only 100.05: one match, below a MinimumPeakMatch of 2.
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), [
            new MspRecord("a", 400d, "Positive", null, 0, [(100.05, 300d), (399d, 1000d)]),
            new MspRecord("b", 400d, "Positive", null, 1, [(100.05, 300d), (399d, 1000d)]),
        ]);
        var output = Path.Combine(_scratch, "v1.pairs");

        Assert.AreEqual(MsnExitCode.Success, Invoke("msn", "-i", msp, "-o", output, "-m",
            WriteMethod("MsmsSimilarityCalc: ModifiedCosine", "MinimumPeakMatch: 2", "MnSpectrumSimilarityCutOff: 0"), "--ionmode", "Positive"));
        Assert.AreEqual(1, File.ReadAllLines(output).Length, "header only");

        Assert.AreEqual(MsnExitCode.Success, Invoke("msn", "-i", msp, "-o", output, "-m",
            WriteMethod("MsmsSimilarityCalc: ModifiedCosine", "MnPrecursorWindow: 0", "MinimumPeakMatch: 2", "MnSpectrumSimilarityCutOff: 0"), "--ionmode", "Positive"));
        Assert.AreEqual(2, File.ReadAllLines(output).Length, "with the window off both peaks match");
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
    public void ASpectrumFileWithAnMspInputIsRejectedWithTwo() {
        var msp = WriteMsp(Path.Combine(_scratch, "query.msp"), [new MspRecord("a", 400d, "Positive", null, 0, SharedSpectrum)]);
        var dcl = Path.Combine(_scratch, "sample.dcl");
        File.WriteAllText(dcl, string.Empty);
        var output = Path.Combine(_scratch, "out.pairs");

        Assert.AreEqual(MsnExitCode.UnusableInput, Invoke("msn", "-i", msp, "-s", dcl, "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive"));
        Assert.IsFalse(File.Exists(output));
    }

    [TestMethod]
    public void APeakListWithoutItsSpectrumFileIsRejectedWithTwo() {
        var peaks = Path.Combine(_scratch, "sample.pai2");
        File.WriteAllText(peaks, string.Empty);
        var output = Path.Combine(_scratch, "out.pairs");

        Assert.AreEqual(MsnExitCode.UnusableInput, Invoke("msn", "-i", peaks, "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive"));
        Assert.AreEqual(MsnExitCode.UnusableInput, Invoke("msn", "-i", peaks, "-s", Path.Combine(_scratch, "missing.dcl"), "-o", output, "-m", DefaultMethod(), "--ionmode", "Positive"));
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
