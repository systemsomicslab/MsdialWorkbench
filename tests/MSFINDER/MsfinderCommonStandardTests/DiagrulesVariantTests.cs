#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticQueryFiles;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>
/// The variant passes of design section 8 PR 5: each changes only its own options, decoys never fire on the real term
/// masses, and a run with every pass is deterministic, also with records and peak lines reordered.
/// </summary>
[TestClass]
public class DiagrulesVariantTests
{
    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = NewTempDirectory();

    [TestCleanup]
    public void Cleanup() => DeleteDirectory(_dir);

    private static DiagrulesVariant Variant(string pass) => DiagrulesVariants.All.Single(v => v.Pass == pass);

    private static HashSet<string> Pairs(DiagnosticRuleEvaluator e) =>
        new HashSet<string>(e.IdentityString.Substring(e.IdentityString.IndexOf('?') + 1).Split('&'), StringComparer.Ordinal);

    private static DiagnosticRuleEvaluator AllRules(DiagnosticEvaluationOptions options) {
        options.Filter = RuleFilter.All;
        return new DiagnosticRuleEvaluator(Runtime.RuleSet, options);
    }

    internal static DiagrulesRecord Parse(string msp) => DiagrulesMspReader.Parse("q.msp", 0, msp.Split(new[] { "\r\n" }, StringSplitOptions.None));

    private static bool Fires(DiagnosticRuleEvaluator e, DiagrulesRecord r, string ruleId) =>
        DiagrulesHarness.Evaluate(r, e, null, false, true).Result!.Outcomes.Any(o => o.RuleId == ruleId && o.Fired);

    [TestMethod]
    public void ThePassesAreTheDesignsInItsOrder() {
        CollectionAssert.AreEqual(new[] {
            "base", "tol_product_0.003", "tol_product_0.01", "floor_x0.5", "floor_x2", "k_minus_1", "loss_ref_hypothesis",
            "loss_ref_observed", "decoy_plus_5", "decoy_plus_9", "decoy_minus_7", "tol_precursor_0.005", "mz_only_curated",
        }, DiagrulesVariants.AllPasses().ToArray());
    }

    [DataTestMethod]
    [DataRow("tol_product_0.003", "overlap,product_tolerance_override.abs_da,product_tolerance_override.ppm")]
    [DataRow("tol_product_0.01", "overlap,product_tolerance_override.abs_da,product_tolerance_override.ppm")]
    [DataRow("floor_x0.5", "floor_scale")]
    [DataRow("floor_x2", "floor_scale")]
    [DataRow("k_minus_1", "k_offset")]
    [DataRow("loss_ref_hypothesis", "loss_reference_override,overlap")]
    [DataRow("loss_ref_observed", "loss_reference_override,overlap")]
    [DataRow("decoy_plus_5", "decoy_shift_da,overlap")]
    [DataRow("decoy_plus_9", "decoy_shift_da,overlap")]
    [DataRow("decoy_minus_7", "decoy_shift_da,overlap")]
    [DataRow("tol_precursor_0.005", "overlap,precursor_tolerance_da")]
    public void EachVariantChangesOnlyItsOwnOptions(string pass, string keys) {
        var tolerance = Runtime.MsfinderTolerance;
        var baseline = Pairs(new DiagnosticRuleEvaluator(Runtime.RuleSet, new DiagnosticEvaluationOptions { MsfinderProductTolerance = tolerance }));
        Assert.AreEqual(Runtime.Evaluator.IdentityString, new DiagnosticRuleEvaluator(Runtime.RuleSet, new DiagnosticEvaluationOptions { MsfinderProductTolerance = tolerance }).IdentityString);
        var options = Variant(pass).Options(tolerance, false);
        var variant = Pairs(new DiagnosticRuleEvaluator(Runtime.RuleSet, options));
        Assert.IsTrue(baseline.IsSubsetOf(variant), pass + ": a base option changed");
        var added = variant.Except(baseline).Select(p => p.Substring(0, p.IndexOf('='))).OrderBy(k => k, StringComparer.Ordinal).ToArray();
        Assert.AreEqual(keys, string.Join(",", added), pass);
        Assert.AreEqual(RuleFilter.MeasuredAndSourceChecked, options.Filter);
        Assert.AreEqual(DiagnosticEvaluationOptions.DefaultAssignmentCap, options.AssignmentCap);
        Assert.IsFalse(options.Verbose);
        Assert.IsTrue(Variant(pass).Options(tolerance, true).Strict, "Strict is passed through");
    }

    [TestMethod]
    public void VariantValuesAreTheDesigns() {
        var t = Runtime.MsfinderTolerance;
        DiagnosticEvaluationOptions O(string pass) => Variant(pass).Options(t, false);
        Assert.AreEqual(0.003, O("tol_product_0.003").ProductToleranceOverride!.Value.AbsDa);
        Assert.AreEqual(0.0, O("tol_product_0.003").ProductToleranceOverride!.Value.Ppm);
        Assert.AreEqual(0.01, O("tol_product_0.01").ProductToleranceOverride!.Value.AbsDa);
        Assert.AreEqual(0.5, O("floor_x0.5").FloorScale);
        Assert.AreEqual(2.0, O("floor_x2").FloorScale);
        Assert.AreEqual(-1, O("k_minus_1").KOffset);
        Assert.AreEqual(LossReference.Hypothesis, O("loss_ref_hypothesis").LossReferenceOverride);
        Assert.AreEqual(LossReference.Observed, O("loss_ref_observed").LossReferenceOverride);
        Assert.AreEqual(5, O("decoy_plus_5").DecoyShiftDa);
        Assert.AreEqual(9, O("decoy_plus_9").DecoyShiftDa);
        Assert.AreEqual(-7, O("decoy_minus_7").DecoyShiftDa);
        Assert.AreEqual(0.005, O("tol_precursor_0.005").PrecursorToleranceDa);
        Assert.AreEqual(OverlapMode.Refuse, O("floor_x2").Overlap);
        Assert.AreEqual(OverlapMode.Refuse, O("k_minus_1").Overlap);
    }

    [DataTestMethod]
    [DataRow("decoy_plus_5")]
    [DataRow("decoy_plus_9")]
    [DataRow("decoy_minus_7")]
    public void DecoysNeverFireOnTheRealTermMasses(string pass) {
        var real = AllRules(new DiagnosticEvaluationOptions { MsfinderProductTolerance = Runtime.MsfinderTolerance, Overlap = OverlapMode.Runtime });
        var decoy = AllRules(Variant(pass).Options(Runtime.MsfinderTolerance, false));
        var positives = Fixtures.Where(f => f.CaseName == "positive").ToList();
        Assert.IsTrue(positives.Count >= 100);
        foreach (var f in positives) {
            var record = Parse(Msp(f, null));
            Assert.IsTrue(Fires(real, record, f.RuleId), f.Label + " fires on the real masses");
            Assert.IsFalse(Fires(decoy, record, f.RuleId), f.Label + " " + pass);
            Assert.AreEqual(DiagrulesHarness.Evaluate(record, real, null, false, true).Result!.InputSha256,
                            DiagrulesHarness.Evaluate(record, decoy, null, false, true).Result!.InputSha256, "the same four inputs");
        }
    }

    [TestMethod]
    public void KMinusOneFiresTheKMinusOneCases() {
        var kMinus1 = AllRules(Variant("k_minus_1").Options(Runtime.MsfinderTolerance, false));
        var plain = AllRules(new DiagnosticEvaluationOptions { MsfinderProductTolerance = Runtime.MsfinderTolerance });
        var cases = Fixtures.Where(f => f.CaseName == "k_of0_at_k_minus_1").ToList();
        Assert.IsTrue(cases.Count > 0);
        var fired = 0;
        foreach (var f in cases) {
            var record = Parse(Msp(f, null));
            Assert.IsFalse(Fires(plain, record, f.RuleId), f.Label);
            // The case has k - 1 of node 0's k units. k_eff = max(1, k - 1), so only a node with k >= 2 is satisfied:
            // a k = 1 node is never vacuously true (design 4.7).
            var text = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FixtureDirectory, f.RuleId + ".json"));
            var k = Regex.Matches(text, "with ([0-9]+) of the k = ([0-9]+) evidence units of k_of node 0").Cast<Match>()
                .Select(m => (Have: int.Parse(m.Groups[1].Value), K: int.Parse(m.Groups[2].Value))).Single(m => m.Have == m.K - 1).K;
            var fires = Fires(kMinus1, record, f.RuleId);
            Assert.AreEqual(k >= 2, fires, f.Label + " k = " + k);
            fired += fires ? 1 : 0;
        }
        Assert.IsTrue(fired > 0);
        foreach (var f in Fixtures.Where(x => x.CaseName == "positive")) {
            Assert.IsTrue(Fires(kMinus1, Parse(Msp(f, null)), f.RuleId), f.Label + ": k-1 never removes a fire");
        }
    }

    private string WriteInput(string folder, string name, bool reverseRecords = false, bool reversePeaks = false) {
        Directory.CreateDirectory(folder);
        var picked = Fixtures.Where((f, i) => i % 9 == 0).Take(24).ToList();
        if (reverseRecords) {
            picked.Reverse();
        }
        var text = string.Concat(picked.Select(f => {
            var lines = f.Peaks.Select(p => p.Mz + "\t" + p.Intensity).ToList();
            if (reversePeaks) {
                lines.Reverse();
            }
            return Msp(f, null, lines) + "\r\n";
        }));
        var path = Path.Combine(folder, name);
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes(text));
        return path;
    }

    private static DiagnosticRunEnvironment Environment() {
        long t = 0;
        return new DiagnosticRunEnvironment { BuildIdentity = "test-build", Clock = () => t += 1000, Frequency = 1000000 };
    }

    private static DiagrulesRun RunAll(string input, string output, string stamp) {
        Directory.CreateDirectory(output);
        var files = DiagrulesHarness.InputFiles(input);
        var run = DiagrulesRun.Start(Runtime, files, DiagrulesHarness.IsLocalVs21(input, files), output, stamp,
                                     DiagrulesVariants.AllPasses(), Environment(), out var error);
        Assert.IsNotNull(run, error);
        Assert.IsTrue(run!.RunAllPasses(out error), error);
        StringAssert.StartsWith(run.Finish(), "Diagnostic rules: ");
        return run;
    }

    private static string[] Lines(string path) => File.ReadAllText(path, Encoding.ASCII).TrimEnd('\n').Split('\n');

    [TestMethod]
    public void EveryPassWritesOneLinePerRecordInTheSameOrder() {
        var output = Path.Combine(_dir, "out");
        var run = RunAll(WriteInput(Path.Combine(_dir, "in"), "q.msp"), output, "20261122120000");
        var passes = DiagrulesVariants.AllPasses();
        Assert.AreEqual(passes.Count + 1, Directory.GetFiles(output).Length);
        CollectionAssert.AreEqual(passes.ToArray(), run.PassCounts.Select(p => p.Key).ToArray());
        string Key(string line) => Regex.Match(line, "\"record_index\":[0-9]+").Value;
        var baseKeys = Lines(Path.Combine(output, run.Names.Evidence("base"))).Select(Key).ToArray();
        Assert.AreEqual(24, baseKeys.Length);
        foreach (var pass in passes) {
            CollectionAssert.AreEqual(baseKeys, Lines(Path.Combine(output, run.Names.Evidence(pass))).Select(Key).ToArray(), pass);
            Assert.AreEqual(24, run.PassCounts.Single(p => p.Key == pass).Value.Queries, pass);
        }
        var manifest = File.ReadAllText(Path.Combine(output, run.Names.Run), Encoding.ASCII);
        Assert.AreEqual(run.Manifest() + "\n", manifest);
        foreach (var pass in passes) {
            StringAssert.Contains(manifest, "\"evidence\":\"" + run.Names.Evidence(pass) + "\"");
            StringAssert.Contains(manifest, "\"evidence:" + pass + "\":");
        }
        StringAssert.Contains(manifest, "\"decoy_shift_da\":-7");
        StringAssert.Contains(manifest, "\"tier\":\"curated\"");
        StringAssert.Contains(manifest, "\"precursor_type\":{\"mapped\":24,\"unmapped\":0}");
        foreach (var pass in passes.Where(p => p != "base" && p != DiagrulesVariants.MzOnlyCuratedPass)) {
            Assert.IsFalse(Lines(Path.Combine(output, run.Names.Evidence(pass))).Any(l => l.Contains("\"claim_eligible\":true")), pass + " is never claim-eligible");
        }
    }

    [TestMethod]
    public void TheOtherLossReferenceRulesAreListedPerPass() {
        var run = RunAll(WriteInput(Path.Combine(_dir, "in"), "q.msp"), Path.Combine(_dir, "out"), "20261122120000");
        var manifest = run.Manifest();
        string Listed(LossReference declared) => "\"other_loss_reference_rule_ids\":[" + string.Join(",", Runtime.RuleSet.Rules
            .Where(r => r.LossReference == declared).Select(r => r.RuleId).OrderBy(x => x, StringComparer.Ordinal).Select(x => "\"" + x + "\"")) + "]";
        Assert.IsTrue(Runtime.RuleSet.Rules.Any(r => r.LossReference == LossReference.Observed));
        StringAssert.Contains(manifest, Listed(LossReference.Observed), "the hypothesis pass flips the observed rules");
        StringAssert.Contains(manifest, Listed(LossReference.Hypothesis), "the observed pass flips the hypothesis rules");
    }

    [TestMethod]
    public void AllPassesAreDeterministicAndIndependentOfRecordAndPeakOrder() {
        var forward = RunAll(WriteInput(Path.Combine(_dir, "a"), "q.msp"), Path.Combine(_dir, "o1"), "20261122120000");
        var again = RunAll(WriteInput(Path.Combine(_dir, "b"), "q.msp"), Path.Combine(_dir, "o2"), "20261122120000");
        RunAll(WriteInput(Path.Combine(_dir, "c"), "q.msp", reversePeaks: true), Path.Combine(_dir, "o3"), "20261122120000");
        RunAll(WriteInput(Path.Combine(_dir, "d"), "q.msp", reverseRecords: true), Path.Combine(_dir, "o4"), "20261122120000");
        string Strip(string line) => Regex.Replace(line, "\"record_index\":[0-9]+", "\"record_index\":0");
        foreach (var pass in DiagrulesVariants.AllPasses()) {
            var name = forward.Names.Evidence(pass);
            var bytes = File.ReadAllBytes(Path.Combine(_dir, "o1", name));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(Path.Combine(_dir, "o2", name)), pass + " repeat");
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(Path.Combine(_dir, "o3", name)), pass + " peak order");
            var reversed = Lines(Path.Combine(_dir, "o4", name)).Select(Strip).Reverse().ToArray();
            CollectionAssert.AreEqual(Lines(Path.Combine(_dir, "o1", name)).Select(Strip).ToArray(), reversed, pass + " record order");
        }
        Assert.AreEqual(File.ReadAllText(Path.Combine(_dir, "o1", forward.Names.Run)), File.ReadAllText(Path.Combine(_dir, "o2", again.Names.Run)));
    }

    [TestMethod]
    public void Vs21OutputsOfEveryPassAreNamedLocal() {
        var output = Path.Combine(_dir, "out");
        RunAll(WriteInput(Path.Combine(_dir, "in"), "synthetic-vs21-stand-in.msp"), output, "20261122120000");
        var names = Directory.GetFiles(output).Select(n => Path.GetFileName(n)!).ToList();
        Assert.AreEqual(DiagrulesVariants.AllPasses().Count + 1, names.Count);
        Assert.IsTrue(names.All(n => n.EndsWith(".local-vs21.jsonl", StringComparison.Ordinal) || n.EndsWith(".local-vs21.json", StringComparison.Ordinal)),
                      string.Join("|", names));
    }

    [TestMethod]
    public void AnExistingVariantOutputStopsTheRunBeforeAnythingIsWritten() {
        var input = WriteInput(Path.Combine(_dir, "in"), "q.msp");
        var output = Path.Combine(_dir, "out");
        Directory.CreateDirectory(output);
        var names = new DiagrulesOutputNames("20261122120000", false);
        File.WriteAllText(Path.Combine(output, names.Evidence("decoy_minus_7")), "keep");
        var run = DiagrulesRun.Start(Runtime, new[] { input }, false, output, "20261122120000", DiagrulesVariants.AllPasses(), Environment(), out var error);
        Assert.IsNull(run);
        Assert.AreEqual("error:output_exists:" + names.Evidence("decoy_minus_7"), error);
        Assert.AreEqual(1, Directory.GetFiles(output).Length);
    }
}

/// <summary>
/// AdductAliasTests (design 7.1, PR 5): PRECURSORTYPE is mapped through the alias table with spaces stripped; a type
/// without an alias row is <c>adduct_unmapped</c> in the <c>mz_only</c> pass and still evaluated in the base pass; an
/// adduct outside the rule gives <c>adduct_not_in_rule</c>.
/// </summary>
[TestClass]
public class AdductAliasTests
{
    private static readonly DiagrulesAdductAliases Aliases = new DiagrulesAdductAliases(Runtime.RuleSet);

    private static DiagrulesRecord Record(FixtureSpectrum f, string? precursorType) {
        var lines = Msp(f, null).Split(new[] { "\r\n" }, StringSplitOptions.None)
            .Where(l => !l.StartsWith("PRECURSORTYPE:", StringComparison.Ordinal))
            .ToList();
        if (precursorType != null) {
            lines.Insert(1, "PRECURSORTYPE: " + precursorType);
        }
        return DiagrulesMspReader.Parse("q.msp", 0, lines);
    }

    private static DiagnosticRuleEvaluator AllRules() =>
        new DiagnosticRuleEvaluator(Runtime.RuleSet, new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Verbose = true, MsfinderProductTolerance = Runtime.MsfinderTolerance });

    [TestMethod]
    public void EverySpellingMapsWithSpacesStripped() {
        var n = 0;
        foreach (var row in Runtime.RuleSet.Adducts) {
            foreach (var spelling in new[] { row.Adduct }.Concat(row.Aliases)) {
                Assert.IsTrue(Aliases.TryMap(spelling, out var a) && a == row.Adduct, spelling);
                Assert.IsTrue(Aliases.TryMap(" " + spelling.Replace("+", " + ").Replace("-", " - ") + " ", out a) && a == row.Adduct, spelling);
                n++;
            }
        }
        Assert.IsTrue(n > Runtime.RuleSet.Adducts.Count, "the table has aliases");
        Assert.IsFalse(Aliases.TryMap(null, out _));
        Assert.IsFalse(Aliases.TryMap("", out _));
        Assert.IsFalse(Aliases.TryMap("[M+Xx]+", out _));
        Assert.IsFalse(Aliases.TryMap("[M+H]+\t", out _), "only spaces are stripped");
    }

    [TestMethod]
    public void AnUnmappedTypeIsEvaluatedInTheBasePassButNotInMzOnly() {
        var f = Fixtures.First(x => x.CaseName == "positive");
        foreach (var type in new[] { "[M+Xx]+", null }) {
            var record = Record(f, type);
            Assert.AreEqual(type, record.PrecursorType);
            Assert.AreEqual(DiagnosticRuleResult.StatusEvaluated, DiagrulesHarness.Evaluate(record, Runtime.Evaluator, null, false, true).Status);
            var mzOnly = DiagrulesHarness.EvaluateMzOnly(record, Runtime.Evaluator, Aliases, null, false, true);
            Assert.AreEqual(DiagrulesHarness.SkippedAdductUnmapped, mzOnly.Status);
            Assert.IsNull(mzOnly.Result);
            Assert.IsNull(mzOnly.Info!.PeaksSuspect);
        }
    }

    [TestMethod]
    public void ReaderSkipsComeBeforeTheAdductCheck() {
        var profile = DiagrulesMspReader.Parse("q.msp", 0, new[] { "NAME: p", "PRECURSORMZ: 300", "PRECURSORTYPE: [M+Xx]+", "SPECTRUMTYPE: Profile", "Num Peaks: 1", "100 5" });
        Assert.AreEqual(DiagrulesHarness.SkippedProfile, DiagrulesHarness.EvaluateMzOnly(profile, Runtime.Evaluator, Aliases, null, false, true).Status);
        Assert.AreEqual(DiagnosticRuleRuntime.SkippedEiDerivative,
                        DiagrulesHarness.EvaluateMzOnly(Record(Fixtures[0], "[M+Xx]+"), Runtime.Evaluator, Aliases, null, true, true).Status);
    }

    [TestMethod]
    public void AMappedTypeIsCuratedMzOnlyEvidence() {
        var f = Fixtures.First(x => x.CaseName == "positive" && !x.Positive);
        var rule = Runtime.RuleSet.Rules.Single(r => r.RuleId == f.RuleId);
        var row = Runtime.RuleSet.Adducts.First(a => a.Adduct == rule.Preconditions.Adducts[0]);
        var type = row.Aliases.Count > 0 ? row.Aliases[0] : row.Adduct;
        var json = DiagnosticResultJson.Write(DiagrulesHarness.EvaluateMzOnly(Record(f, type), AllRules(), Aliases, null, false, true).Result!);
        StringAssert.Contains(json, "\"adduct_tier\":\"curated\"");
        StringAssert.Contains(json, "mz_only");
    }

    [TestMethod]
    public void AnAdductOutsideTheRuleIsNotInRule() {
        var f = Fixtures.First(x => x.CaseName == "positive" && x.Positive);
        var rule = Runtime.RuleSet.Rules.Single(r => r.RuleId == f.RuleId);
        var outside = Runtime.RuleSet.Adducts.First(a => a.Charge > 0 && !rule.Preconditions.Adducts.Contains(a.Adduct));
        var result = DiagrulesHarness.EvaluateMzOnly(Record(f, outside.Adduct), AllRules(), Aliases, null, false, true).Result!;
        var outcome = result.Outcomes.Single(o => o.RuleId == f.RuleId);
        Assert.AreEqual("adduct_not_in_rule", outcome.PreconditionFailure);
        Assert.AreEqual(0, outcome.NHypotheses);
        Assert.IsFalse(outcome.Fired);
    }
}
