#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticQueryFiles;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>
/// The ASCII port of P2 <c>energy_bin</c> (design section 8 PR 5). Expected values are the Python original's, except
/// where the original's Unicode <c>\d</c>, <c>\s</c> or strip would accept a non-ASCII character.
/// </summary>
[TestClass]
public class EnergyBinTests
{
    [DataTestMethod]
    [DataRow("35", "unspecified:20-<40")]
    [DataRow("35 eV", "eV:20-<40")]
    [DataRow("35eV", "eV:20-<40")]
    [DataRow("35 V", "eV:20-<40")]
    [DataRow("35 Ev", "eV:20-<40")]
    [DataRow("NCE=35", "NCE:20-<40")]
    [DataRow("nce: 35", "NCE:20-<40")]
    [DataRow("NCE35", "NCE:20-<40")]
    [DataRow("35%", "NCE:20-<40")]
    [DataRow("35 nce", "NCE:20-<40")]
    [DataRow("NCE=35 eV", "NCE:20-<40")]
    [DataRow("NCE = 45 %", "NCE:>=40")]
    [DataRow("10", "unspecified:<20")]
    [DataRow("19.99", "unspecified:<20")]
    [DataRow("0019", "unspecified:<20")]
    [DataRow("20", "unspecified:20-<40")]
    [DataRow("40", "unspecified:>=40")]
    [DataRow("100", "unspecified:>=40")]
    [DataRow("  \t35 eV\t ", "eV:20-<40")]
    [DataRow("", "unparseable")]
    [DataRow(null, "unparseable")]
    [DataRow("35-45", "unparseable")]
    [DataRow("ramp 20-40", "unparseable")]
    [DataRow("-5", "unparseable")]
    [DataRow("35.", "unparseable")]
    [DataRow(".5", "unparseable")]
    [DataRow("1e2", "unparseable")]
    [DataRow("35 kev", "unparseable")]
    [DataRow("35 eV eV", "unparseable")]
    public void MatchesThePythonOriginal(string? value, string expected) {
        Assert.AreEqual(expected, CollisionEnergyBin.Of(value!));
    }

    /// <summary>Exactly where Python's float() rounds to 20 and to 40 (ties to the even 20 and 40).</summary>
    [DataTestMethod]
    [DataRow("19.9999999999999982236431605997495353221893310546874", "unspecified:<20")]
    [DataRow("19.9999999999999982236431605997495353221893310546875", "unspecified:20-<40")]
    [DataRow("39.999999999999996447286321199499070644378662109374", "unspecified:20-<40")]
    [DataRow("39.999999999999996447286321199499070644378662109375", "unspecified:>=40")]
    [DataRow("19.999999999999999", "unspecified:20-<40")]
    [DataRow("19.99999999999999", "unspecified:<20")]
    public void BoundariesEqualTheBinary64Rounding(string value, string expected) {
        Assert.AreEqual(expected, CollisionEnergyBin.Of(value));
    }

    [TestMethod]
    public void AHugeNumberIsAtLeastFortyWithoutParsing() {
        // Python: float("1" + 400 zeros) is inf; .NET Framework's double.Parse would throw.
        Assert.AreEqual("unspecified:>=40", CollisionEnergyBin.Of("1" + new string('0', 400)));
        Assert.AreEqual("eV:<20", CollisionEnergyBin.Of(new string('0', 400) + "1 eV"));
    }

    /// <summary>Python's <c>\d</c>, <c>\s</c> and strip() accept these (the original bins them); the ASCII port does not.</summary>
    [DataTestMethod]
    [DataRow("\u0663\u0665")]       // Arabic-Indic 35
    [DataRow("\uff13\uff15")]       // full-width 35
    [DataRow("35\u00a0eV")]         // no-break space
    [DataRow("\u00a035")]
    [DataRow("35\u2009eV")]         // thin space
    [DataRow("35\u000beV")]         // vertical tab
    [DataRow("35\r")]
    public void NonAsciiDigitsAndSpacesAreUnparseable(string value) {
        Assert.AreEqual(CollisionEnergyBin.Unparseable, CollisionEnergyBin.Of(value));
    }

    [TestMethod]
    public void TheBinDoesNotDependOnTheCulture() {
        var original = CultureInfo.CurrentCulture;
        try {
            foreach (var name in new[] { "tr-TR", "de-DE", "ar-SA", "th-TH" }) {
                CultureInfo.CurrentCulture = new CultureInfo(name);
                Assert.AreEqual("NCE:20-<40", CollisionEnergyBin.Of("NCE=35"), name);
                Assert.AreEqual("eV:<20", CollisionEnergyBin.Of("12.5 EV"), name);
                Assert.AreEqual(CollisionEnergyBin.Unparseable, CollisionEnergyBin.Of("12,5 eV"), name);
            }
        }
        finally {
            CultureInfo.CurrentCulture = original;
        }
    }
}

/// <summary>The allowlisted multi-record MSP reader of <c>diagrules</c> and its per-record evaluation.</summary>
[TestClass]
public class DiagrulesReaderTests
{
    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = NewTempDirectory();

    [TestCleanup]
    public void Cleanup() => DeleteDirectory(_dir);

    private List<DiagrulesRecord> ReadText(string text, string name = "q.msp") {
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes(text));
        return DiagrulesMspReader.Read(path).ToList();
    }

    private static DiagnosticQueryEvaluation Evaluate(DiagrulesRecord r) =>
        DiagrulesHarness.Evaluate(r, Runtime.Evaluator, Runtime.Settings.InstrumentType, false, false);

    private static string Record(string body) => body.Replace("\n", "\r\n") + "\r\n";

    [TestMethod]
    public void KeepsOnlyTheAllowlistedFieldsAndTheRecordIndex() {
        var text = Record("NAME: first\nprecursormz : 301.5\nPRECURSORTYPE: [M-H]-\nIONMODE: Negative\nINSTRUMENTTYPE: LC-ESI-QTOF\n"
                          + "COLLISIONENERGY: 35 eV\nSPECTRUMTYPE: Centroid\nSMILES: C\nFORMULA: C7H6O\nNum Peaks: 2\n100.1\t50\n150.2 20 \"C7H7O2\"")
                   + " \t\r\n"
                   + Record("PRECURSORMZ: 200\nPRECURSORMZ: 250.25\nCOLLISIONENERGY: NCE 45\nNum Peaks: 1\n90 10")
                   + "\r\n\r\n"
                   + Record("COMMENT: no peaks here");
        var records = ReadText(text);
        Assert.AreEqual(3, records.Count);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, records.Select(r => r.RecordIndex).ToArray());
        Assert.IsTrue(records.All(r => r.FileName == "q.msp"));

        var a = records[0];
        Assert.AreEqual(301.5, a.PrecursorMz);
        Assert.AreEqual("[M-H]-", a.PrecursorType);
        Assert.AreEqual(IonMode.Negative, a.IonMode);
        Assert.AreEqual("LC-ESI-QTOF", a.InstrumentType);
        Assert.AreEqual("35 eV", a.CollisionEnergy);
        Assert.AreEqual("eV:20-<40", a.CollisionEnergyBin);
        Assert.IsFalse(a.IsProfile);
        Assert.AreEqual(2, a.DeclaredPeakCount);
        CollectionAssert.AreEqual(new[] { 100.1, 150.2 }, a.Peaks.Select(p => p.Mz).ToArray());
        Assert.IsTrue(a.Peaks.All(p => string.IsNullOrEmpty(p.Comment)), "a quoted comment is not kept");

        var b = records[1];
        Assert.AreEqual(250.25, b.PrecursorMz, "the last occurrence wins");
        Assert.IsTrue(b.IonModeAbsent);
        Assert.AreEqual(IonMode.Positive, b.IonMode, "absent IONMODE reads as positive, as MS-FINDER");
        Assert.AreEqual("NCE:>=40", b.CollisionEnergyBin);

        var c = records[2];
        Assert.AreEqual(0, c.Peaks.Count);
        Assert.AreEqual(0.0, c.PrecursorMz);
        Assert.AreEqual(CollisionEnergyBin.Unparseable, c.CollisionEnergyBin);
        CollectionAssert.Contains(new[] { DiagnosticRuleResult.StatusNoPrecursor, DiagnosticRuleResult.StatusNoPeaks }, Evaluate(c).Status);
    }

    [TestMethod]
    public void PeaksAreRoundedByPeakAndSortedAsTheReaderSortsThem() {
        var r = ReadText(Record("PRECURSORMZ: 300\nNum Peaks: 3\n200.1234567\t10.00000049\n100.0000005 5\n150 1.5e+2")).Single();
        CollectionAssert.AreEqual(new[] { Math.Round(100.0000005, 6), 150.0, Math.Round(200.1234567, 6) }, r.Peaks.Select(p => p.Mz).ToArray());
        CollectionAssert.AreEqual(new[] { 5.0, 150.0, Math.Round(10.00000049, 6) }, r.Peaks.Select(p => p.Intensity).ToArray());
    }

    [DataTestMethod]
    [DataRow("Num Peaks: 2\n100 50 C7H7O2\n150 20", "peak_block_disagrees")]       // unquoted annotation
    [DataRow("Num Peaks: 2\n100 50 abc\n150 20", "peak_block_disagrees")]          // unquoted text without digits
    [DataRow("Num Peaks: 2\n100 50\n150 20 \"a\" \"b\"", "peak_block_disagrees")]  // two comments
    [DataRow("Num Peaks: 2\n100,5 50\n150 20", "peak_block_disagrees")]           // decimal comma
    [DataRow("Num Peaks: 2\n100 50; 150 20", "peak_block_disagrees")]              // two pairs on a line
    [DataRow("Num Peaks: 2\n-100 50\n150 20", "peak_block_disagrees")]             // sign
    [DataRow("Num Peaks: 2\n100 1e+999\n150 20", "peak_block_disagrees")]          // not finite
    [DataRow("Num Peaks: two\n100 50", "peak_block_disagrees")]                     // marker not plain
    [DataRow("MSTYPE: MS3\nNum Peaks: 1\n100 50", "peak_block_disagrees")]
    [DataRow("Num Peaks: 3\n100 50\n150 20", "peak_count_mismatch")]                // the record ends first
    [DataRow("Num Peaks: 3\n100 50\n\n150 20\n170 10", "peak_block_disagrees")]   // a blank line inside the block
    public void PeakLinesOutsideTheGrammarAreSkippedAsSuspect(string block, string reason) {
        var r = ReadText(Record("PRECURSORMZ: 300\nIONMODE: Positive\nCOLLISIONENERGY: 10\n" + block)).Single();
        Assert.AreEqual(reason, r.PeaksSuspect);
        var e = Evaluate(r);
        Assert.AreEqual("skipped:peaks_suspect:" + reason, e.Status);
        Assert.IsNull(e.Result);
        Assert.AreEqual("unspecified:<20", r.CollisionEnergyBin, "the strata of a skipped record are still reported");
        StringAssert.Contains(DiagrulesHarness.EvidenceLine(r, e), "\"peaks_check\":\"" + reason + "\"");
    }

    [TestMethod]
    public void TheMs1BlockIsSkippedAndTheLastMs2BlockWins() {
        var r = ReadText(Record("PRECURSORMZ: 300\nMSTYPE: MS1\nNum Peaks: 2\n300 100\n301 20\nMSTYPE: MS2\nNum Peaks: 1\n120 10\nNum Peaks: 2\n90 5\n80 7")).Single();
        Assert.IsNull(r.PeaksSuspect);
        Assert.AreEqual(2, r.DeclaredPeakCount);
        CollectionAssert.AreEqual(new[] { 80.0, 90.0 }, r.Peaks.Select(p => p.Mz).ToArray());
    }

    [DataTestMethod]
    [DataRow("Profile", true)]
    [DataRow("P", true)]
    [DataRow("centroid", true)]   // MS-FINDER's reader looks for an upper-case C
    [DataRow("Centroid", false)]
    [DataRow("C", false)]
    [DataRow(null, false)]        // absent: the RawData default, centroid
    public void ProfileRecordsAreSkippedNeverCentroided(string? spectrumType, bool profile) {
        var r = ReadText(Record("PRECURSORMZ: 300\n" + (spectrumType == null ? "" : "SPECTRUMTYPE: " + spectrumType + "\n") + "Num Peaks: 1\n100 50")).Single();
        Assert.AreEqual(profile, r.IsProfile);
        var e = Evaluate(r);
        if (profile) {
            Assert.AreEqual(DiagrulesHarness.SkippedProfile, e.Status);
            Assert.IsNull(e.Result);
            StringAssert.Contains(DiagrulesHarness.EvidenceLine(r, e), "\"peaks_check\":null");
        }
        else {
            Assert.AreNotEqual(DiagrulesHarness.SkippedProfile, e.Status);
        }
    }

    [DataTestMethod]
    [DataRow("Positive", IonMode.Positive)]
    [DataRow("P", IonMode.Positive)]
    [DataRow("Negative", IonMode.Negative)]
    [DataRow("N", IonMode.Negative)]
    public void IonModeIsMappedAsMsFinderMapsIt(string text, IonMode expected) {
        var r = ReadText(Record("IONMODE: " + text)).Single();
        Assert.AreEqual(expected, r.IonMode);
        Assert.IsFalse(r.IonModeAbsent);
    }

    [DataTestMethod]
    [DataRow("IONTYPE: Negative", IonMode.Negative)]
    [DataRow("IONTYPE: Positive", IonMode.Positive)]
    [DataRow("IONMODE: Positive\nIONTYPE: Negative", IonMode.Negative)]
    [DataRow("IONTYPE: Negative\nIONMODE: Positive", IonMode.Positive)]
    public void IonTypeIsReadAsTheIonModeAndTheLaterLineWinsAsInMsFinder(string headers, IonMode expected) {
        var r = ReadText(Record(headers)).Single();
        Assert.AreEqual(expected, r.IonMode);
        Assert.IsFalse(r.IonModeAbsent);
    }

    [DataTestMethod]
    [DataRow("1,234.5")]
    [DataRow("abc")]
    [DataRow("NaN")]
    [DataRow("-5")]
    [DataRow("1e999")]
    public void AnUnusablePrecursorIsZero(string text) {
        Assert.AreEqual(0.0, DiagrulesMspReader.PrecursorMzOf(text));
    }

    [TestMethod]
    public void TheCollisionEnergyNeverReachesTheEvaluator() {
        var f = Fixtures[0];
        var low = ReadText(Msp(f, null).Replace("COLLISIONENERGY: 35", "COLLISIONENERGY: 5 eV"), "low.msp").Single();
        var high = ReadText(Msp(f, null).Replace("COLLISIONENERGY: 35", "COLLISIONENERGY: 37.5 eV"), "high.msp").Single();
        Assert.AreEqual("eV:<20", low.CollisionEnergyBin);
        Assert.AreEqual("eV:20-<40", high.CollisionEnergyBin);
        var a = Evaluate(low);
        var b = Evaluate(high);
        Assert.IsNotNull(a.Result);
        Assert.AreEqual(DiagnosticResultJson.Write(a.Result!), DiagnosticResultJson.Write(b.Result!));
        StringAssert.DoesNotMatch(DiagrulesHarness.EvidenceLine(high, b), new System.Text.RegularExpressions.Regex("37\\.5"));
    }

    [TestMethod]
    public void EvidenceLineLayout() {
        var f = Fixtures.First(x => x.InstrumentType != null);
        var r = ReadText(Msp(f, null), "layout.msp").Single();
        var e = Evaluate(r);
        var expected = "{\"collision_energy_bin\":\"unspecified:20-<40\",\"file_name\":\"layout.msp\",\"input\":{\"instrument_type_source\":\"query\","
                       + "\"peaks_check\":\"ok\",\"peaks_source\":\"harness_msp\"},\"record_index\":0,\"result\":" + DiagnosticResultJson.Write(e.Result!)
                       + ",\"status\":\"evaluated\"}";
        Assert.AreEqual(expected, DiagrulesHarness.EvidenceLine(r, e));
    }

    /// <summary>Every byte of the evidence is the same under every culture (design 6.2).</summary>
    [TestMethod]
    public void EvidenceIsCultureIndependent() {
        var text = string.Concat(Fixtures.Take(40).Select(f => Msp(f, null)))
                   + Record("PRECURSORMZ: 300.5\nCOLLISIONENERGY: NCE=12.5\nNum Peaks: 2\n100.25 50\n150 20 x");
        File.WriteAllBytes(Path.Combine(_dir, "c.msp"), Encoding.ASCII.GetBytes(text));
        string Lines() => string.Join("\n", DiagrulesMspReader.Read(Path.Combine(_dir, "c.msp")).Select(r => DiagrulesHarness.EvidenceLine(r, Evaluate(r))));
        var expected = Lines();
        var original = CultureInfo.CurrentCulture;
        try {
            var odd = (CultureInfo)new CultureInfo("en-US").Clone();
            odd.NumberFormat.NumberDecimalSeparator = ",";
            odd.NumberFormat.NumberGroupSeparator = "\u202f";
            odd.NumberFormat.NegativeSign = "\u2212";
            foreach (var culture in new[] { new CultureInfo("de-DE"), new CultureInfo("fr-FR"), new CultureInfo("tr-TR"), new CultureInfo("cs-CZ"),
                                            new CultureInfo("ar-SA"), new CultureInfo("th-TH"), odd }) {
                CultureInfo.CurrentCulture = culture;
                Assert.AreEqual(expected, Lines(), culture.Name);
            }
        }
        finally {
            CultureInfo.CurrentCulture = original;
        }
    }
}

/// <summary>
/// The same MSP queries through <c>diagrules</c> and through predict's path (MS-FINDER's reader, then the PR 4 runtime)
/// give the same status and the same canonical result (so the same input_sha256).
/// </summary>
[TestClass]
public class DiagrulesPredictParityTests
{
    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = NewTempDirectory();

    [TestCleanup]
    public void Cleanup() => DeleteDirectory(_dir);

    private void AssertParity(IReadOnlyList<string> queries, string label) {
        var multi = Path.Combine(_dir, label + ".msp");
        File.WriteAllBytes(multi, Encoding.ASCII.GetBytes(string.Concat(queries)));
        var records = DiagrulesMspReader.Read(multi).ToList();
        Assert.AreEqual(queries.Count, records.Count, label);
        for (var i = 0; i < queries.Count; i++) {
            var predict = Runtime.Evaluate(WriteAndRead(_dir, label + i + ".msp", queries[i]), false);
            var harness = DiagrulesHarness.Evaluate(records[i], Runtime.Evaluator, Runtime.Settings.InstrumentType, false, false);
            Assert.AreEqual(predict.Status, harness.Status, label + " " + i);
            Assert.AreEqual(predict.Result?.InputSha256, harness.Result?.InputSha256, label + " " + i);
            Assert.AreEqual(predict.Result == null ? null : DiagnosticResultJson.Write(predict.Result),
                            harness.Result == null ? null : DiagnosticResultJson.Write(harness.Result), label + " " + i);
            Assert.AreEqual(predict.Info?.InstrumentTypeSource, harness.Info?.InstrumentTypeSource, label + " " + i);
            Assert.AreEqual(predict.Info?.PeaksSuspect, harness.Info?.PeaksSuspect, label + " " + i);
        }
    }

    [TestMethod]
    public void EveryFixtureWithQuotedComments() {
        AssertParity(Fixtures.Select(f => Msp(f, null)).ToList(), "quoted");
    }

    [TestMethod]
    public void UnquotedFormulaAnnotations() {
        var queries = Fixtures.Take(25).Select(f => {
            var lines = f.Peaks.Select(p => p.Mz + "\t" + p.Intensity).ToList();
            lines.Insert(0, "123.0441 1000 C7H7O2");
            return Msp(f, null, lines);
        }).ToList();
        AssertParity(queries, "unquoted");
    }

    [TestMethod]
    public void UnquotedTextWithoutDigits() {
        // The MSDIAL4 tokeniser keeps "abc"'s peak pending and lets the next pair overwrite it, so one line short it
        // reads past the block; a trailing pair line keeps it from reading past the file's end. Both sides refuse it.
        var queries = Fixtures.Take(25).Select(f => {
            var lines = f.Peaks.Select(p => p.Mz + "\t" + p.Intensity).ToList();
            lines[lines.Count - 1] += " abc";
            return Msp(f, null, lines);
        }).ToList();
        for (var i = 0; i < queries.Count; i++) {
            var predict = Runtime.Evaluate(WriteAndRead(_dir, "abc" + i + ".msp", queries[i] + "1000\t1\r\n"), false);
            var path = Path.Combine(_dir, "abc" + i + ".harness.msp");
            File.WriteAllBytes(path, Encoding.ASCII.GetBytes(queries[i]));
            var harness = DiagrulesHarness.Evaluate(DiagrulesMspReader.Read(path).Single(), Runtime.Evaluator, Runtime.Settings.InstrumentType, false, false);
            Assert.AreEqual("skipped:peaks_suspect:peak_block_disagrees", harness.Status, "abc " + i);
            StringAssert.StartsWith(predict.Status, "skipped:peaks_suspect:", "abc " + i);
        }
    }

    [TestMethod]
    public void SuspectCountsAndSubOneDaltonPeaks() {
        var f = Fixtures[0];
        var plain = f.Peaks.Select(p => p.Mz + "\t" + p.Intensity).ToList();
        var sub = new List<string>(plain) { "0.5\t10" };
        var zero = new List<string>(plain) { "777.7\t0" };
        AssertParity(new[] { Msp(f, null, sub), Msp(f, null, zero) }, "guards");
    }

    // The MS2 block of an Msp() query: the index of its Num Peaks line among the CRLF-split lines.
    private static (List<string> Lines, int NumPeaks) Ms2Block(string query) {
        var lines = query.Split(new[] { "\r\n" }, StringSplitOptions.None).ToList();
        return (lines, lines.FindLastIndex(l => l.StartsWith("Num Peaks:", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void PeakLinesAfterTheDeclaredCountAreIgnoredAsPredictIgnoresThem() {
        var queries = Fixtures.Take(25).Where(f => f.Peaks.Count > 1).Select(f => {
            var (lines, at) = Ms2Block(Msp(f, null));
            lines[at] = "Num Peaks: " + (f.Peaks.Count - 1);
            return string.Join("\r\n", lines);
        }).ToList();
        Assert.IsTrue(queries.Count > 0);
        AssertParity(queries, "extra");
    }

    [TestMethod]
    public void ABlankLineInsideThePeakBlockKeepsTheRecordWhole() {
        var queries = Fixtures.Take(25).Where(f => f.Peaks.Count > 1).Select(f => {
            var (lines, at) = Ms2Block(Msp(f, null));
            lines.Insert(at + 2, "");
            return string.Join("\r\n", lines);
        }).ToList();
        Assert.IsTrue(queries.Count > 0);
        AssertParity(queries, "blank");
        foreach (var r in DiagrulesMspReader.Read(Path.Combine(_dir, "blank.msp"))) {
            Assert.AreEqual(DiagnosticInputInfo.PeakBlockDisagrees, r.PeaksSuspect);
        }
    }
}

/// <summary>A whole <c>diagrules</c> run: files, names, counts, the refusal of existing outputs, and determinism.</summary>
[TestClass]
public class DiagrulesRunTests
{
    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = NewTempDirectory();

    [TestCleanup]
    public void Cleanup() => DeleteDirectory(_dir);

    private static DiagnosticRunEnvironment Environment() {
        long t = 0;
        return new DiagnosticRunEnvironment { BuildIdentity = "test-build", Clock = () => t += 1000, Frequency = 1000000 };
    }

    private string WriteInput(string folder, string name) {
        Directory.CreateDirectory(folder);
        var text = string.Concat(Fixtures.Take(12).Select(f => Msp(f, null)))
                   + "NAME: profile\r\nPRECURSORMZ: 300\r\nSPECTRUMTYPE: Profile\r\nNum Peaks: 1\r\n100 5\r\n\r\n"
                   + "NAME: annotated\r\nPRECURSORMZ: 300\r\nNum Peaks: 1\r\n100 5 C7H7O2\r\n";
        var path = Path.Combine(folder, name);
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes(text));
        return path;
    }

    private (DiagrulesRun Run, string Summary) RunOnce(string input, string output, string stamp) {
        var files = DiagrulesHarness.InputFiles(input);
        var run = DiagrulesRun.Start(Runtime, files, DiagrulesHarness.IsLocalVs21(input, files), output, stamp,
                                     new[] { DiagrulesHarness.BasePass }, Environment(), out var error);
        Assert.IsNotNull(run, error);
        Assert.IsTrue(run!.RunBasePass(out error), error);
        return (run, run.Finish());
    }

    [TestMethod]
    public void WritesTheEvidenceAndTheManifest() {
        var inputDir = Path.Combine(_dir, "in");
        WriteInput(inputDir, "b.msp");
        WriteInput(inputDir, "a.msp");
        File.WriteAllText(Path.Combine(inputDir, "ignored.mat"), "x");
        var output = Path.Combine(_dir, "out");
        Directory.CreateDirectory(output);
        var (run, summary) = RunOnce(inputDir, output, "20261122120000");

        CollectionAssert.AreEqual(new[] { "a.msp", "b.msp" }, run.Inputs.Select(Path.GetFileName).ToArray());
        StringAssert.StartsWith(summary, "Diagnostic rules: ");
        var evidence = File.ReadAllBytes(Path.Combine(output, "Diagrules evidence-base-20261122120000.jsonl"));
        Assert.IsTrue(evidence.All(b => b < 0x80 && b != (byte)'\r'), "ASCII, LF");
        var lines = Encoding.ASCII.GetString(evidence).TrimEnd('\n').Split('\n');
        Assert.AreEqual(28, lines.Length);
        StringAssert.Contains(lines[0], "\"file_name\":\"a.msp\"");
        StringAssert.Contains(lines[27], "\"file_name\":\"b.msp\"");
        StringAssert.Contains(lines[27], "\"record_index\":13");

        var manifest = File.ReadAllText(Path.Combine(output, "Diagrules run-20261122120000.json"), Encoding.ASCII);
        Assert.AreEqual(run.Manifest() + "\n", manifest);
        StringAssert.Contains(manifest, "\"skipped:peaks_suspect:peak_block_disagrees\":2");
        StringAssert.Contains(manifest, "\"skipped:profile\":2");
        StringAssert.Contains(manifest, "\"queries\":28");
        StringAssert.Contains(manifest, "\"local_vs21\":false");
        StringAssert.Contains(manifest, "\"build_identity\":\"test-build\"");
        Assert.IsFalse(manifest.Contains(Path.GetFileName(_dir)), "no paths");
        Assert.AreEqual(28, run.BaseCounts!.Queries);
        Assert.AreEqual(28, run.ReaderCounts!.Records);
    }

    [TestMethod]
    public void AnExistingOutputIsNeverOverwritten() {
        var input = WriteInput(_dir, "q.msp");
        var output = Path.Combine(_dir, "out");
        Directory.CreateDirectory(output);
        var existing = Path.Combine(output, "Diagrules run-20261122120000.json");
        File.WriteAllText(existing, "keep");
        var run = DiagrulesRun.Start(Runtime, new[] { input }, false, output, "20261122120000", new[] { DiagrulesHarness.BasePass }, Environment(), out var error);
        Assert.IsNull(run);
        Assert.AreEqual("error:output_exists:Diagrules run-20261122120000.json", error);
        Assert.AreEqual("keep", File.ReadAllText(existing));
        Assert.AreEqual(1, Directory.GetFiles(output).Length);
    }

    [TestMethod]
    public void Vs21OutputsAreNamedLocal() {
        Assert.IsTrue(DiagrulesHarness.IsLocalVs21(@"lib\synthetic-VS21-pos.msp", new string[0]));
        Assert.IsTrue(DiagrulesHarness.IsLocalVs21(@"lib\vs21\", new[] { @"lib\vs21\pos.msp" }));
        Assert.IsTrue(DiagrulesHarness.IsLocalVs21(@"lib", new[] { @"lib\a.msp", @"lib\Vs21_neg.msp" }));
        // Any path component counts: a VS21 folder marks its files whatever their names.
        Assert.IsTrue(DiagrulesHarness.IsLocalVs21(@"lib\vs21x\fixtures", new[] { @"lib\vs21x\fixtures\a.msp" }));
        Assert.IsTrue(DiagrulesHarness.IsLocalVs21(@"libs\VS21\MSMS-Pos.msp", new[] { @"libs\VS21\MSMS-Pos.msp" }));
        Assert.IsTrue(DiagrulesHarness.IsLocalVs21(@"libs\VS21\pos", new[] { @"libs\VS21\pos\a.msp" }));
        Assert.IsFalse(DiagrulesHarness.IsLocalVs21(Path.Combine(_dir, "fixtures"), new[] { Path.Combine(_dir, "fixtures", "a.msp") }));

        var input = WriteInput(Path.Combine(_dir, "in"), "synthetic-vs21-stand-in.msp");
        var output = Path.Combine(_dir, "out");
        Directory.CreateDirectory(output);
        RunOnce(input, output, "20261122120000");
        var names = Directory.GetFiles(output).Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(new[] { "Diagrules evidence-base-20261122120000.local-vs21.jsonl", "Diagrules run-20261122120000.local-vs21.json" }, names);
        StringAssert.Contains(File.ReadAllText(Path.Combine(output, names[1])), "\"local_vs21\":true");
    }

    [TestMethod]
    public void AnInputRemovedBetweenPassesEndsThePassWithAnErrorLine() {
        var input = WriteInput(Path.Combine(_dir, "in"), "q.msp");
        var output = Path.Combine(_dir, "out");
        Directory.CreateDirectory(output);
        var run = DiagrulesRun.Start(Runtime, new[] { input }, false, output, "20261122120000",
                                     new[] { DiagrulesHarness.BasePass, "second" }, Environment(), out var error);
        Assert.IsNotNull(run, error);
        Assert.IsTrue(run!.RunBasePass(out error), error);
        File.Delete(input);
        Assert.IsFalse(run.RunPass("second", Runtime.Evaluator,
            r => DiagrulesHarness.Evaluate(r, Runtime.Evaluator, Runtime.Settings.InstrumentType, false, false), out _, out error));
        StringAssert.StartsWith(error, "Diagnostic rules: error:");
        StringAssert.EndsWith(error, ":q.msp");
        // The manifest still carries the hash taken at Start, of the bytes the base pass read.
        var summary = run.Finish(out var written);
        Assert.IsTrue(written, summary);
        StringAssert.Contains(File.ReadAllText(Path.Combine(output, "Diagrules run-20261122120000.json")), "\"sha256\":\"");
    }

    [TestMethod]
    public void AManifestThatCannotBeWrittenFailsTheRunAndLeavesNoEmptyFile() {
        var input = WriteInput(Path.Combine(_dir, "in"), "q.msp");
        var output = Path.Combine(_dir, "out");
        Directory.CreateDirectory(output);
        var run = DiagrulesRun.Start(Runtime, new[] { input }, false, output, "20261122120000", new[] { DiagrulesHarness.BasePass }, Environment(), out var error);
        Assert.IsNotNull(run, error);
        Assert.IsTrue(run!.RunBasePass(out error), error);
        var manifest = Path.Combine(output, "Diagrules run-20261122120000.json");
        File.WriteAllText(manifest, "keep");
        var line = run.Finish(out var written);
        Assert.IsFalse(written);
        Assert.AreEqual("Diagnostic rules: error:IOException:Diagrules run-20261122120000.json", line);
        Assert.AreEqual("keep", File.ReadAllText(manifest));
    }

    [TestMethod]
    public void AnUnreadableInputIsRefusedAtStart() {
        var output = Path.Combine(_dir, "out");
        Directory.CreateDirectory(output);
        var run = DiagrulesRun.Start(Runtime, new[] { Path.Combine(_dir, "missing.msp") }, false, output, "20261122120000",
                                     new[] { DiagrulesHarness.BasePass }, Environment(), out var error);
        Assert.IsNull(run);
        Assert.AreEqual("error:FileNotFoundException:missing.msp", error);
        Assert.AreEqual(0, Directory.GetFiles(output).Length);
    }

    [TestMethod]
    public void TwoRunsGiveIdenticalEvidenceBytes() {
        var input = WriteInput(Path.Combine(_dir, "in"), "q.msp");
        var output = Path.Combine(_dir, "out");
        Directory.CreateDirectory(output);
        RunOnce(input, output, "20261122120000");
        RunOnce(input, output, "20261122120001");
        CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(output, "Diagrules evidence-base-20261122120000.jsonl")),
                                  File.ReadAllBytes(Path.Combine(output, "Diagrules evidence-base-20261122120001.jsonl")));
        Assert.AreEqual(File.ReadAllText(Path.Combine(output, "Diagrules run-20261122120000.json")).Replace("20261122120000", "S"),
                        File.ReadAllText(Path.Combine(output, "Diagrules run-20261122120001.json")).Replace("20261122120001", "S"));
    }
}
