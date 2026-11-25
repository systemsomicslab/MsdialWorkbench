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
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticQueryFiles;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>
/// The library side of the GUI (design 5.2, 5.3, 5.5 item 8; R22-R25): the runtime cache, the tab's evidence view,
/// the GUI structure writers and the GUI export run without a timing file (OQ09). The WPF code itself is
/// build-verified only (OQ15).
/// </summary>
[TestClass]
public class DiagnosticGuiSupportTests
{
    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = NewTempDirectory();

    [TestCleanup]
    public void Cleanup() => DeleteDirectory(_dir);

    private static readonly Lazy<DiagnosticRuleRuntime> _ranking = new Lazy<DiagnosticRuleRuntime>(() => CreateRuntime(new MsfinderDiagnosticRuleSettings
    {
        Evaluation = true,
        RankingWeight = 1.5,
        RankingScope = DiagnosticRuleRankingScope.Measured,
    }));

    private static AnalysisParamOfMsfinder Param(bool on, double weight = 0.0, double tolerance = 0.01) {
        var param = new AnalysisParamOfMsfinder { MassTolType = MassToleranceType.Da, Mass2Tolerance = tolerance };
        param.DiagnosticRules.Evaluation = on;
        param.DiagnosticRules.RankingWeight = weight;
        return param;
    }

    private RawData Raw(FixtureSpectrum f) => WriteAndRead(_dir, "probe.msp", Msp(f, null));

    private static FragmenterResult C(string title, double score, string smiles) {
        var c = new FragmenterResult { TotalScore = score };
        foreach (var (field, value) in new[] { ("title", title), ("smiles", smiles), ("ontology", "o-" + title), ("formula", "C2H6O"), ("inchikey", "k-" + title), ("resources", "r") }) {
            typeof(FragmenterResult).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(c, value);
        }
        return c;
    }

    [TestMethod]
    public void CacheIsEmptyWhileEvaluationIsOff() {
        var cache = new DiagnosticRuleRuntimeCache();
        Assert.IsNull(cache.Get(Param(false, 1.0), out var created));
        Assert.IsFalse(created);
        Assert.IsNull(cache.Get(null!, out created));
        Assert.IsNull(cache.Error);
    }

    [TestMethod]
    public void CacheIsKeyedBySettingsAndTolerance() {
        var cache = new DiagnosticRuleRuntimeCache();
        var param = Param(true);
        var first = cache.Get(param, out var created);
        Assert.IsNotNull(first, cache.Error);
        Assert.IsTrue(created);
        Assert.AreSame(first, cache.Get(param, out created));
        Assert.IsFalse(created);

        param.DiagnosticRules.RankingWeight = 0.5;
        var weighted = cache.Get(param, out created);
        Assert.IsTrue(created);
        Assert.AreNotSame(first, weighted);
        Assert.IsTrue(weighted!.RankingOn);

        param.Mass2Tolerance = 0.02;
        var wider = cache.Get(param, out created);
        Assert.IsTrue(created);
        Assert.AreEqual(0.02, wider!.MsfinderTolerance.AbsDa);

        param.MassTolType = MassToleranceType.Ppm;
        var ppm = cache.Get(param, out created)!.MsfinderTolerance;
        Assert.AreEqual(0.02, ppm.Ppm);
        Assert.AreEqual(0.0, ppm.AbsDa);
        Assert.IsTrue(created);

        // The runtime keeps a snapshot: editing the parameter afterwards does not change it.
        var cached = cache.Runtime!;
        param.DiagnosticRules.RankingWeight = 2.0;
        Assert.AreEqual(0.5, cached.RankingWeight);

        param.DiagnosticRules.Evaluation = false;
        Assert.IsNull(cache.Get(param, out _));
        Assert.IsNull(cache.Runtime);
    }

    [TestMethod]
    public void CacheKeepsAFailureOncePerKey() {
        var cache = new DiagnosticRuleRuntimeCache();
        var param = Param(true);
        var missing = Path.Combine(_dir, "no-such-rule-set");
        param.DiagnosticRules.RuleSetDirectory = missing;
        Assert.IsNull(cache.Get(param, out var created));
        Assert.IsTrue(created);
        StringAssert.StartsWith(cache.Error, DiagnosticRuleRuntime.ErrorPrefix + ":");
        Assert.IsFalse(cache.Error!.Contains(missing), "the error never names the directory");
        var error = cache.Error;
        Assert.IsNull(cache.Get(param, out created));
        Assert.IsFalse(created, "the failure is reported once per key");
        Assert.AreEqual(error, cache.Error);

        param.DiagnosticRules.RuleSetDirectory = "";
        Assert.IsNotNull(cache.Get(param, out created));
        Assert.IsTrue(created);
        Assert.IsNull(cache.Error);
    }

    [TestMethod]
    public void EvidenceViewListsOutcomesTermsAndRefusals() {
        var runtime = Runtime;
        var fixture = Fixtures.First(f => AnyFired(runtime.Evaluate(Raw(f), false).Result));
        var evaluation = runtime.Evaluate(Raw(fixture), false);
        var r = evaluation.Result!;
        var view = DiagnosticEvidenceView.Of(evaluation, runtime);
        var id = runtime.RuleSet.Identity;

        Assert.IsNull(view.Error);
        Assert.AreEqual(id.Version, view.Version);
        Assert.AreEqual(id.Sha256, view.Sha256);
        Assert.AreEqual("embedded", view.Source);
        Assert.AreEqual(evaluation.Status, view.Status);
        Assert.AreEqual(r.InstrumentClass, view.InstrumentClass);
        Assert.AreEqual(r.Outcomes.Count + r.Refusals.Count, view.Outcomes.Count);
        CollectionAssert.AreEqual(r.Outcomes.Select(DiagnosticRuleExport.OutcomeName).ToList(), view.Outcomes.Take(r.Outcomes.Count).Select(o => o.Outcome).ToList());
        Assert.IsTrue(view.Outcomes.Skip(r.Outcomes.Count).All(o => o.Outcome == "refused"));

        var reported = r.Outcomes.Select(DiagnosticRuleExport.Reported).Where(h => h != null).ToList();
        var matched = reported.SelectMany(h => h!.Matched).ToList();
        Assert.AreEqual(matched.Count + reported.Sum(h => h!.Vetoes.Count(v => v.Fired)), view.Terms.Count);
        Assert.IsTrue(matched.Count > 0);
        var first = view.Terms.First(t => t.Role != "veto");
        Assert.AreEqual(DiagnosticRuleExport.Num(matched[0].PeakMz, DiagnosticRuleExport.MzDecimals), first.ObservedMz);
        Assert.AreEqual(DiagnosticRuleExport.Num(matched[0].TargetMz, DiagnosticRuleExport.MzDecimals), first.TheoreticalMz);
        Assert.AreEqual(DiagnosticRuleExport.Num(matched[0].ErrorDa * 1000.0, DiagnosticRuleExport.MdaDecimals), first.ErrorMDa);
        Assert.AreEqual(matched[0].TermId + ":" + matched[0].MemberKey, first.Term);

        var chemOnt = view.ChemOnt(null);
        Assert.AreEqual(RankingOutcomeView.Of(r, runtime.RuleSet).Count, chemOnt.Count);
    }

    [TestMethod]
    public void EvidenceViewShowsTheScopeBannerAndTheError() {
        var runtime = _ranking.Value;
        var evaluation = runtime.Evaluate(Raw(Fixtures[0]), false);
        var view = DiagnosticEvidenceView.Of(evaluation, runtime);
        Assert.IsTrue(view.Banners.Any(b => b.StartsWith("Measured scope", StringComparison.Ordinal)));
        Assert.AreEqual("measured", view.Scope);
        Assert.AreEqual("1.500000", view.Weight);

        var failed = DiagnosticEvidenceView.Failed("rule_set_error:load:x");
        Assert.AreEqual("rule_set_error:load:x", failed.Error);
        Assert.AreEqual(0, failed.Outcomes.Count);
        Assert.AreEqual(0, failed.ChemOnt(DiagnosticQueryContext.Empty).Count);
    }

    [TestMethod]
    public void GuiStructureRowsAreCappedAndUnfiltered() {
        var raw = Raw(Fixtures[0]);
        var candidates = new List<FragmenterResult> { C("a", 3.25, "CCO"), C("b", 0.0, "CC"), C("c", -1.0, "C") };
        var runtime = _ranking.Value;
        var ranking = MsfinderDiagnosticRanking.Rank(candidates, null, runtime);
        var id = runtime.RuleSet.Identity;
        var sw = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
        DiagnosticRuleExport.WriteGuiStructureRows(sw, raw, ranking, id, 2);
        var lines = sw.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(2, lines.Length, "the export number caps the rows; TotalScore <= 0 is not filtered");
        var c = candidates[1];
        var legacy = raw.RawdataFilePath + "\t" + Path.GetFileNameWithoutExtension(raw.RawdataFilePath) + "\t" + raw.Name + "\t" + raw.Ms1PeakNumber + "\t"
                     + raw.Ms2PeakNumber + "\t" + raw.PrecursorMz + "\t" + raw.PrecursorType + "\t" + c.Title + "\t" + c.TotalScore + "\t" + c.Resources + "\t"
                     + c.Formula + "\t" + c.Ontology + "\t" + c.Inchikey + "\t" + c.Smiles;
        Assert.AreEqual(legacy + "\t0.000000\t2\t0\t\t\tno_outcome\t" + id.Version + "\t" + id.Sha256 + "\t\t\t", lines[1]);
        Assert.AreEqual(DiagnosticRuleExport.GuiLegacyStructureHeader + "\t" + string.Join("\t", DiagnosticRuleExport.StructureAppendedColumns), DiagnosticRuleExport.GuiStructureHeader);
        Assert.AreEqual(DiagnosticRuleExport.GuiLegacyStructureHeader.Split('\t').Length, legacy.Split('\t').Length);
    }

    [TestMethod]
    public void EachFileRowsCarryTheNewRank() {
        var candidates = new List<FragmenterResult> { C("a", 3.25, "CCO"), C("b", 0.0, "CC") };
        var runtime = _ranking.Value;
        var ranking = MsfinderDiagnosticRanking.Rank(candidates, null, runtime);
        var id = runtime.RuleSet.Identity;
        var sw = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
        DiagnosticRuleExport.WriteEachFileStructureRows(sw, ranking, id);
        var lines = sw.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(2, lines.Length);
        var c = candidates[0];
        var legacy = "1\t" + c.Title + "\t" + c.TotalScore + "\t" + c.RetentionTime + "\t" + c.RetentionIndex + "\t" + c.Resources + "\t" + c.Formula + "\t"
                     + c.Ontology + "\t" + c.Inchikey + "\t" + c.Smiles;
        Assert.AreEqual(legacy + "\t3.250000\t1\t0\t\t\tno_outcome\t" + id.Version + "\t" + id.Sha256 + "\t\t\t", lines[0]);
        StringAssert.StartsWith(lines[1], "2\tb\t");
        Assert.AreEqual(DiagnosticRuleExport.EachFileLegacyStructureHeader.Split('\t').Length, legacy.Split('\t').Length);
        StringAssert.StartsWith(DiagnosticRuleExport.EachFileStructureHeader, DiagnosticRuleExport.EachFileLegacyStructureHeader + "\tRanking score\t");
    }

    [TestMethod]
    public void GuiRunWritesNoTimingFile() {
        var runtime = Runtime;
        var environment = new DiagnosticRunEnvironment { BuildIdentity = "test-build", Clock = () => 0, Frequency = 1000 };
        var run = DiagnosticRuleRun.Start(runtime, _dir, "20261120000000", 4103, environment, out var error);
        Assert.IsNotNull(run, error);
        run!.WritesTiming = false;
        Assert.IsTrue(run.OpenWriters(out error), error);
        var raw = Raw(Fixtures[0]);
        var query = run.Evaluate("probe", "probe.msp", raw);
        run.Rank(query, new List<FormulaResult>(), new List<FragmenterResult>());
        run.Complete(query);
        run.Finish();
        foreach (var name in new[] { run.Names.Result, run.Names.Evidence, run.Names.Context, run.Names.Run }) {
            Assert.IsTrue(File.Exists(Path.Combine(_dir, name)), name);
        }
        Assert.IsFalse(File.Exists(Path.Combine(_dir, run.Names.Timing)));
        var manifest = File.ReadAllText(Path.Combine(_dir, run.Names.Run));
        StringAssert.Contains(manifest, "\"run\":\"" + run.Names.Run + "\",\"timing\":null}");
    }
}
