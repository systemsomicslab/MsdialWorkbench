#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticQueryFiles;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>
/// The diagnostic-rule output files (design 5.5, 6.2, 6.3; R16, R19): column order, ASCII, culture independence,
/// context-only columns, no overwrite, and committed goldens that net48 and net8.0 must both reproduce byte for byte.
/// Queries are written at test time from the linked per-rule fixtures (OQ03); the clock and build identity are fixed.
/// </summary>
[TestClass]
public class DiagnosticExportTests
{
    private const string Stamp = "20261118000000";
    private const long LegacyDate = 4103;
    private const string FseaMarker = "FseaMarkerTerm";
    private const string OntologyMarker = "OntologyMarker";

    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = NewTempDirectory();

    [TestCleanup]
    public void Cleanup() => DeleteDirectory(_dir);

    private static DiagnosticRunEnvironment FixedEnvironment() => new DiagnosticRunEnvironment { BuildIdentity = "test-build", Clock = () => 0, Frequency = 1000 };

    private static readonly Lazy<DiagnosticRuleRuntime> _ranking = new Lazy<DiagnosticRuleRuntime>(() => CreateRuntime(new MsfinderDiagnosticRuleSettings
    {
        Evaluation = true,
        RankingWeight = 1.5,
        RankingScope = DiagnosticRuleRankingScope.Measured,
    }));

    /// <summary>The golden queries: the first four fixtures that fire, the first three that do not, and one without
    /// peaks.</summary>
    private static IReadOnlyList<(FixtureSpectrum Fixture, bool Empty)> Queries() {
        var fired = Fixtures.Where(f => AnyFired(Runtime.Evaluate(ToRaw(f), false).Result)).Take(4);
        var silent = Fixtures.Where(f => !AnyFired(Runtime.Evaluate(ToRaw(f), false).Result)).Take(3);
        return fired.Concat(silent).Select(f => (f, false)).Concat(new[] { (Fixtures[0], true) }).ToList();
    }

    private static string? _scratch;

    private static RawData ToRaw(FixtureSpectrum f) {
        _scratch ??= NewTempDirectory();
        return WriteAndRead(_scratch, "probe.msp", Msp(f, null));
    }

    private sealed class Outputs
    {
        public Dictionary<string, byte[]> Files { get; } = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        public string Structure { get; set; } = "";
        public string Summary { get; set; } = "";
        public DiagnosticRuleRun? Run { get; set; }
    }

    /// <summary>Runs the queries as the console export loop does and returns every new file by its golden name. The
    /// queries are read under the invariant culture (MS-FINDER's reader depends on it); the export runs under
    /// <paramref name="culture"/> when given.</summary>
    private Outputs Produce(DiagnosticRuleRuntime runtime, string folder, CultureInfo? culture = null) {
        Directory.CreateDirectory(folder);
        var queries = Queries().Select((q, k) => (q.Fixture, q.Empty, Name: "q" + k + ".msp")).ToList();
        var raws = queries.Select(q => WriteAndRead(folder, q.Name, q.Empty ? Msp(q.Fixture, null, new string[0]) : Msp(q.Fixture, null))).ToList();
        var saved = Thread.CurrentThread.CurrentCulture;
        if (culture != null) {
            Thread.CurrentThread.CurrentCulture = culture;
        }
        try {
            return Export(runtime, folder, queries.Select(q => (q.Fixture, q.Empty, q.Name)).ToList(), raws);
        }
        finally {
            Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    private Outputs Export(DiagnosticRuleRuntime runtime, string folder, List<(FixtureSpectrum Fixture, bool Empty, string Name)> queries, List<RawData> raws) {
        var run = DiagnosticRuleRun.Start(runtime, folder, Stamp, LegacyDate, FixedEnvironment(), out var error);
        Assert.IsNotNull(run, error);
        Assert.IsTrue(run!.OpenWriters(out error), error);
        var structure = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
        structure.WriteLine(DiagnosticRuleExport.StructureHeader);
        var i = 0;
        foreach (var (f, empty, name) in queries) {
            var raw = raws[i++];
            var query = run.Evaluate(name, name, raw);
            if (!empty) {
                var rule = runtime.RuleSet.Rules.First(r => r.RuleId == f.RuleId);
                var formula = new FormulaResult { Formula = new Formula { FormulaString = "C6H6O" }, ChemicalOntologyDescriptions = new List<string> { FseaMarker + i } };
                var candidates = new List<FragmenterResult>
                {
                    C("neg", 2.0, rule.Claim.NegativeExamples.First().Smiles, OntologyMarker + "B", "C6H6O", null, null),
                    C("pos", 1.0, rule.Claim.PositiveExamples.First().Smiles, OntologyMarker + "A", "C6H6O", null, null),
                    C("zero", 0.0, "CCO", OntologyMarker + "Z", "C2H6O", null, null),
                };
                var ranking = run.Rank(query, new List<FormulaResult> { formula }, candidates);
                if (ranking != null) {
                    DiagnosticRuleExport.WriteStructureRows(structure, raw, ranking, runtime.RuleSet.Identity);
                }
            }
            run.Complete(query);
        }
        var outputs = new Outputs { Summary = run.Finish(), Run = run, Structure = structure.ToString().Replace(folder, "<dir>") };
        foreach (var name in run.Names.All) {
            outputs.Files[name.Replace(Stamp, "stamp")] = File.ReadAllBytes(Path.Combine(folder, name));
        }
        return outputs;
    }

    /// <summary>A structure candidate (the reader sets these fields; the properties are read-only).</summary>
    private static FragmenterResult C(string title, double score, string smiles, string ontology, string? formula, string? inchikey, string? resources) {
        var c = new FragmenterResult { TotalScore = score };
        foreach (var (field, value) in new[] { ("title", title), ("smiles", smiles), ("ontology", ontology), ("formula", formula), ("inchikey", inchikey), ("resources", resources) }) {
            typeof(FragmenterResult).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(c, value);
        }
        return c;
    }

    private static string GoldenPath(string name) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Export", "Golden", name);

    [TestMethod]
    public void OutputsEqualTheCommittedGoldens() {
        var outputs = Produce(_ranking.Value, Path.Combine(_dir, "golden"));
        var actual = outputs.Files.ToDictionary(p => p.Key, p => p.Value);
        actual["Structure result rows.txt"] = Encoding.ASCII.GetBytes(outputs.Structure);
        actual["Console summary.txt"] = Encoding.ASCII.GetBytes(outputs.Summary + "\n");
        var missing = new List<string>();
        foreach (var pair in actual) {
            var path = GoldenPath(pair.Key);
            if (!File.Exists(path)) {
                var dump = Path.Combine(Path.GetTempPath(), "MsfinderExportGolden");
                Directory.CreateDirectory(dump);
                File.WriteAllBytes(Path.Combine(dump, pair.Key), pair.Value);
                missing.Add(pair.Key);
                continue;
            }
            var expected = File.ReadAllBytes(path);
            Assert.AreEqual(Encoding.ASCII.GetString(expected), Encoding.ASCII.GetString(pair.Value), pair.Key);
            CollectionAssert.AreEqual(expected, pair.Value, pair.Key);
        }
        Assert.AreEqual(0, missing.Count, "golden missing (actual written to the temp folder): " + string.Join(", ", missing));
        Assert.IsTrue(outputs.Run!.Counts.Fired >= 4, "non-vacuous: " + outputs.Summary);
        StringAssert.Contains(outputs.Structure, "\tapplied\t", "the ranking term was applied");
    }

    [TestMethod]
    public void EveryByteIsAscii() {
        var outputs = Produce(_ranking.Value, Path.Combine(_dir, "ascii"));
        foreach (var pair in outputs.Files) {
            Assert.IsTrue(pair.Value.All(b => b < 0x80), pair.Key);
            Assert.IsFalse(pair.Value.Contains((byte)'\r'), pair.Key + ": LF line breaks only");
        }
        var raw = ToRaw(Fixtures[0]);
        raw.Name = "a\tb\u00e9\r\nc";
        var rows = DiagnosticRuleExport.ResultRows("f\u00fc.msp", raw, Runtime.Evaluate(raw, false), Runtime.RuleSet);
        Assert.IsTrue(rows.All(r => r.All(c => c >= ' ' && c <= '~' || c == '\t')));
        Assert.AreEqual("f?.msp", rows[0].Split('\t')[0]);
        Assert.AreEqual("a b?  c", rows[0].Split('\t')[1]);
    }

    [TestMethod]
    public void OutputsDoNotDependOnTheProcessCulture() {
        var invariant = Produce(_ranking.Value, Path.Combine(_dir, "inv"));
        var comma = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        comma.NumberFormat.NumberDecimalSeparator = ",";
        comma.NumberFormat.NumberGroupSeparator = " ";
        comma.NumberFormat.NegativeSign = "−";
        foreach (var culture in new[] { new CultureInfo("de-DE"), new CultureInfo("fr-FR"), new CultureInfo("ar-SA"), new CultureInfo("tr-TR"), comma }) {
            var name = culture.Name.Length == 0 ? "comma" : culture.Name;
            var other = Produce(_ranking.Value, Path.Combine(_dir, name), culture);
            foreach (var pair in invariant.Files) {
                CollectionAssert.AreEqual(pair.Value, other.Files[pair.Key], name + " " + pair.Key);
            }
            Assert.AreEqual(invariant.Summary, other.Summary, name);
            // Only the appended structure columns are culture-free; the legacy ones keep their legacy formatting.
            var appended = invariant.Structure.Split('\n').Select(l => string.Join("\t", l.Split('\t').Skip(14))).ToList();
            var otherAppended = other.Structure.Split('\n').Select(l => string.Join("\t", l.Split('\t').Skip(14))).ToList();
            CollectionAssert.AreEqual(appended, otherAppended, name + " structure appended columns");
        }
    }

    [TestMethod]
    public void ColumnsAreInTheDesignOrder() {
        Assert.AreEqual(
            "File name\tTitle\tInput sha256\tPrecursor mz\tIon mode\tInstrument type\tInstrument class\tInstrument type source\tPeaks source\t"
            + "Peaks check\tPeaks dropped by adapter\tRule set version\tRule set sha256\tRule set source\tEngine semantics\tStatus\tOutcome\tRule id\t"
            + "Rule version\tMotif id\tAuthor reviewed\tClaim eligible\tClaim ineligible\tPrecondition mode\tAdduct\tAdduct source\tAdduct tier\t"
            + "Hypothesis id\tHypothesis formula\tHypothesis mz\tPrecursor error (Da)\tN hypotheses\tN fired hypotheses\tTied hypotheses\tScore\t"
            + "Matched terms\tVeto terms\tBranches fired\tProduct tolerance\tPrecursor tolerance\tRule conflict",
            DiagnosticRuleExport.ResultHeader);
        Assert.AreEqual(
            "File name\tInput sha256\tRule id\tMotif id\tQuery precursor type\tFSEA top term\tFSEA agreement\tBase top ClassyFire\t"
            + "Base top agreement\tRanked top ClassyFire\tRanked top agreement\tHypothesis formula equals top formula\tCandidates matching motif\t"
            + "Rule weight\tRule scope",
            DiagnosticRuleExport.ContextHeader);
        Assert.AreEqual(DiagnosticRuleExport.LegacyStructureHeader + "\tRanking score\tBase rank\tDiagnostic rule term\tMatched fired motifs\t"
                        + "Matched vetoed motifs\tDiagnostic rule status\tRule set version\tRule set sha256\t"
                        + "Diagnostic rule agreement\tAgreement fired motifs\tAgreement vetoed motifs", DiagnosticRuleExport.StructureHeader);
        Assert.AreEqual(string.Join("\t", DiagnosticTimingLog.Columns),
            "File name\tMS2 peaks\tRules evaluated\tHypotheses tested\tOutcomes\tCandidates\tUnique SMILES\tCache hits\tEval ms\tMatch ms\t"
            + "Formula step ms\tStructure step ms");

        var outputs = Produce(_ranking.Value, Path.Combine(_dir, "cols"));
        void Check(string key, int columns) {
            var lines = Encoding.ASCII.GetString(outputs.Files[key]).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.IsTrue(lines.Length > 1, key);
            foreach (var line in lines) {
                Assert.AreEqual(columns, line.Split('\t').Length, key + ": " + line);
            }
        }
        Check("Diagnostic rule result-stamp.txt", DiagnosticRuleExport.ResultColumns.Length);
        Check("Diagnostic rule context-stamp.txt", DiagnosticRuleExport.ContextColumns.Length);
        Check("Diagnostic rule timing-stamp.tsv", DiagnosticTimingLog.Columns.Length);
        foreach (var line in outputs.Structure.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
            Assert.AreEqual(14 + DiagnosticRuleExport.StructureAppendedColumns.Length, line.Split('\t').Length, line);
        }
    }

    [TestMethod]
    public void FormulaAndCandidateFieldsAreOnlyInTheContextFile() {
        var outputs = Produce(_ranking.Value, Path.Combine(_dir, "ctx"));
        var contextOnly = DiagnosticRuleExport.ContextColumns.Except(new[] { "File name", "Input sha256", "Rule id", "Motif id" }).ToList();
        foreach (var column in contextOnly) {
            CollectionAssert.DoesNotContain(DiagnosticRuleExport.ResultColumns, column);
        }
        foreach (var key in new[] { "Diagnostic rule result-stamp.txt", "Diagnostic rule evidence-stamp.jsonl" }) {
            var text = Encoding.ASCII.GetString(outputs.Files[key]);
            Assert.IsFalse(text.Contains(FseaMarker), key);
            Assert.IsFalse(text.Contains(OntologyMarker), key);
            Assert.IsFalse(text.Contains("C6H6O"), key);
        }
        var context = Encoding.ASCII.GetString(outputs.Files["Diagnostic rule context-stamp.txt"]);
        StringAssert.Contains(context, FseaMarker);
        StringAssert.Contains(context, OntologyMarker);
    }

    [TestMethod]
    public void ContextHasNoMotifCountsWhenTheWeightIsZero() {
        var outputs = Produce(Runtime, Path.Combine(_dir, "w0"));
        var rows = outputs.Structure.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(DiagnosticRuleExport.StructureHeader, rows[0]);
        Assert.IsTrue(rows.Length > 1, "w = 0 writes the base-order rows with the agreement columns");
        var lines = Encoding.ASCII.GetString(outputs.Files["Diagnostic rule context-stamp.txt"]).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var column = Array.IndexOf(DiagnosticRuleExport.ContextColumns, "Candidates matching motif");
        Assert.IsTrue(lines.Length > 1);
        Assert.IsTrue(lines.Skip(1).All(l => l.Split('\t')[column] == ""), "OQ07");
    }

    [TestMethod]
    public void AnExistingFileIsNeverOverwritten() {
        foreach (var name in new DiagnosticOutputNames(Stamp).All) {
            var folder = Path.Combine(_dir, "exists" + Array.IndexOf(new DiagnosticOutputNames(Stamp).All.ToArray(), name));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name), "keep");
            var run = DiagnosticRuleRun.Start(Runtime, folder, Stamp, LegacyDate, FixedEnvironment(), out var error);
            Assert.IsNull(run, name);
            Assert.AreEqual(DiagnosticRuleExport.OutputExists + ":" + name, error);
            Assert.AreEqual("keep", File.ReadAllText(Path.Combine(folder, name)));
            Assert.AreEqual(1, Directory.GetFiles(folder).Length, "nothing else written");
        }

        // A file that appears after the pre-check: CreateNew refuses it and the files this call created are removed.
        var late = Path.Combine(_dir, "late");
        Directory.CreateDirectory(late);
        var started = DiagnosticRuleRun.Start(Runtime, late, Stamp, LegacyDate, FixedEnvironment(), out var startError);
        Assert.IsNotNull(started, startError);
        var context = Path.Combine(late, started!.Names.Context);
        File.WriteAllText(context, "keep");
        Assert.IsFalse(started.OpenWriters(out var openError));
        Assert.AreEqual(DiagnosticRuleExport.OutputExists + ":" + started.Names.Context, openError);
        Assert.AreEqual("keep", File.ReadAllText(context));
        Assert.AreEqual(1, Directory.GetFiles(late).Length);
    }

    /// <summary>A run file that appears after the pre-check is kept, and Finish reports it instead of throwing.</summary>
    [TestMethod]
    public void FinishReportsARunFileThatAppearedLate() {
        var late = Path.Combine(_dir, "late-run");
        Directory.CreateDirectory(late);
        var started = DiagnosticRuleRun.Start(Runtime, late, Stamp, LegacyDate, FixedEnvironment(), out var startError);
        Assert.IsNotNull(started, startError);
        Assert.IsTrue(started!.OpenWriters(out var openError), openError);
        var run = Path.Combine(late, started.Names.Run);
        File.WriteAllText(run, "keep");
        Assert.AreEqual("Diagnostic rules: " + DiagnosticRuleExport.OutputExists + ":" + started.Names.Run, started.Finish());
        Assert.AreEqual("keep", File.ReadAllText(run));
    }

    [TestMethod]
    public void StampIsInvariant() {
        var time = new DateTime(2026, 11, 8, 7, 5, 3);
        foreach (var name in new[] { "", "ar-SA", "th-TH", "fa-IR" }) {
            var saved = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(name);
            try {
                Assert.AreEqual("20261108070503", DiagnosticRuleExport.Stamp(time), name);
            }
            finally {
                Thread.CurrentThread.CurrentCulture = saved;
            }
        }
    }

    [TestMethod]
    public void WeightZeroKeepsTheBaseOrderAndShowsTheAgreement() {
        var runtime = CreateRuntime(new MsfinderDiagnosticRuleSettings { Evaluation = true, RankingScope = DiagnosticRuleRankingScope.Measured });
        Assert.IsFalse(runtime.RankingOn);
        var outputs = Produce(runtime, Path.Combine(_dir, "w0agree"));
        var header = DiagnosticRuleExport.StructureHeader.Split('\t');
        int Col(string name) => Array.IndexOf(header, name);
        var rows = outputs.Structure.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(l => l.Split('\t')).ToList();
        Assert.AreEqual(2 * 7, rows.Count, "seven queries with candidates, TotalScore <= 0 filtered");
        // the base order: "neg" (score 2) before "pos" (score 1) in every query, even where "pos" agrees with a fired rule
        CollectionAssert.AreEqual(Enumerable.Range(0, 7).SelectMany(_ => new[] { "neg", "pos" }).ToList(), rows.Select(r => r[Col("Structure rank")]).ToList());
        Assert.IsTrue(rows.All(r => r[Col("Diagnostic rule status")] == MsfinderDiagnosticRanking.StatusWeightZero && r[Col("Diagnostic rule term")] == "0"
                                    && r[Col("Matched fired motifs")] == "" && r[Col("Matched vetoed motifs")] == ""));
        var agreeing = rows.Where(r => r[Col("Diagnostic rule agreement")].StartsWith("consistent (", StringComparison.Ordinal)).ToList();
        Assert.IsTrue(agreeing.Count >= 4, "non-vacuous: the fired queries' positive examples agree");
        Assert.IsTrue(agreeing.All(r => r[Col("Agreement fired motifs")] != ""));
        Assert.IsTrue(rows.Where(r => r[Col("Diagnostic rule agreement")] == "").All(r => r[Col("Agreement fired motifs")] == "" && r[Col("Agreement vetoed motifs")] == ""));
    }

    [TestMethod]
    public void StructureRowsKeepTheLegacyColumnsAndFilter() {
        var raw = ToRaw(Fixtures[0]);
        var candidates = new List<FragmenterResult>
        {
            C("a", 3.25, "CCO", "x", "C2H6O", "k", "r"),
            C("b", 0.0, "CC", "y", null, null, null),
            C("c", -1.0, "C", "z", null, null, null),
        };
        var ranking = MsfinderDiagnosticRanking.Rank(candidates, null, _ranking.Value);
        var sw = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
        DiagnosticRuleExport.WriteStructureRows(sw, raw, ranking, _ranking.Value.RuleSet.Identity);
        var lines = sw.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(1, lines.Length, "TotalScore <= 0 is filtered on the base score");
        var c = candidates[0];
        var legacy = raw.RawdataFilePath + "\t" + Path.GetFileNameWithoutExtension(raw.RawdataFilePath) + "\t" + raw.Name + "\t" + raw.Ms1PeakNumber + "\t"
                     + raw.Ms2PeakNumber + "\t" + raw.PrecursorMz + "\t" + raw.PrecursorType + "\t" + c.Title + "\t" + c.TotalScore + "\t" + c.Resources + "\t"
                     + c.Formula + "\t" + c.Ontology + "\t" + c.Inchikey + "\t" + c.Smiles;
        var id = _ranking.Value.RuleSet.Identity;
        Assert.AreEqual(legacy + "\t3.250000\t1\t0\t\t\tno_outcome\t" + id.Version + "\t" + id.Sha256 + "\t\t\t", lines[0]);
    }

    [TestMethod]
    public void SummaryLineHasTheDesignForm() {
        var outputs = Produce(Runtime, Path.Combine(_dir, "summary"));
        var c = outputs.Run!.Counts;
        var id = Runtime.RuleSet.Identity;
        Assert.AreEqual("Diagnostic rules: " + id.Version + " (" + id.Sha256.Substring(0, 12) + ", embedded) evaluated " + c.Evaluated + ", fired " + c.Fired
                        + ", veto_blocked " + c.VetoBlocked + ", none " + c.None + ", skipped " + c.Skipped + ", refused " + c.Refused + ", errors " + c.Errors,
                        outputs.Summary);
        Assert.AreEqual(c.Evaluated, c.Fired + c.VetoBlocked + c.None);
        Assert.AreEqual(c.Queries, c.Evaluated + c.Skipped + c.Errors);
        Assert.AreEqual(1, c.Skipped, "the query without peaks");
    }
}
