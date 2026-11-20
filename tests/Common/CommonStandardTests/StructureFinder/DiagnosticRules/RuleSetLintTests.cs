#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.DiagnosticRuleFixtures;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Lints of the shipped set and its fixtures (design 7.1, 4.8). The storage form and privacy of the embedded rule-set
/// files are <see cref="EmbeddedRuleSetTests"/>; this class applies the same privacy check (no InChIKey, drive or UNC
/// path, e-mail address or local-vs21 name) to every committed fixture, the storage form to the fixtures this
/// repository owns, and lists the support-veto overlaps of the shipped rules, which are reported and not refused.
/// </summary>
[TestClass]
[DeploymentItem(@"Resources\DiagnosticRuleFixtures", @"Resources\DiagnosticRuleFixtures")]
public class RuleSetLintTests
{
    public TestContext TestContext { get; set; } = null!;

    /// <summary>Every fixture file, by path relative to the fixture folder with '/' separators.</summary>
    internal static List<string> FixtureFiles() {
        var root = FixturePath("");
        if (!Directory.Exists(root) || !File.Exists(FixturePath(EngineFixtures.ManifestFile))) {
            Assert.Inconclusive("the fixtures are not deployed");
        }
        return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(f => f.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(f => f, StringComparer.Ordinal).ToList();
    }

    [TestMethod]
    public void TheOwnedFixturesAreInStorageForm() {
        var owned = FixtureFiles().Where(f => f.EndsWith(".json", StringComparison.Ordinal) && f != VectorFile).ToList();
        Assert.IsTrue(owned.Contains(EngineFixtures.ManifestFile) && owned.Contains(ShippedRuleFixtures.MassesFile));
        foreach (var f in owned) {
            var bytes = File.ReadAllBytes(FixturePath(f));
            CollectionAssert.AreEqual(PrettyJson.ToBytes(StrictJsonReader.Read(bytes)), bytes, f + " is not in storage form");
        }
    }

    [TestMethod]
    public void NoFixtureHoldsPrivateContent() {
        var failures = new List<string>();
        foreach (var f in FixtureFiles()) {
            if (f.EndsWith(".gitattributes", StringComparison.Ordinal)) {
                continue;
            }
            var found = EmbeddedRuleSetTests.PrivateContent(f, File.ReadAllBytes(FixturePath(f)));
            // The CPython vectors' string_escape and strict_json sections spell JSON escapes such as \b and \u0000,
            // which the UNC pattern (checked last, so every other kind passed) cannot tell from a host name.
            if (found != null && !(f == VectorFile && found == "a UNC path")) {
                failures.Add(f + ": " + found);
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    /// <summary>
    /// The support-veto overlaps of the shipped rules (design 4.8): for every hypothesis, a support member (required,
    /// counted, ordering) and a veto member whose targets lie within tol(a) + tol(b) (+ the precursor tolerance when
    /// exactly one is a loss referenced to the observed precursor) at the declared p2 tolerances. Computed with the
    /// naive model, one line per (rule, support member key, veto member key) with its smallest distance.
    /// </summary>
    internal static List<string> SupportVetoOverlaps(IEnumerable<NaiveRule> rules) {
        var lines = new List<string>();
        foreach (var rule in rules) {
            var id = rule.RuleId;
            var vetoes = Enumerable.Range(0, rule.Terms.Count).Where(rule.IsVeto).ToList();
            if (vetoes.Count == 0) {
                continue;
            }
            var support = Enumerable.Range(0, rule.Terms.Count).Where(rule.IsSupport).ToList();
            var closest = new SortedDictionary<string, (double Delta, string Hypothesis)>(StringComparer.Ordinal);
            foreach (var h in rule.All) {
                var veto = vetoes.SelectMany(t => rule.Members(t, h, h.Mz, 0).Select(m => (t, m))).ToList();
                foreach (var t in support) {
                    foreach (var a in rule.Members(t, h, h.Mz, 0)) {
                        foreach (var (v, b) in veto) {
                            var observed = rule.LossReference == "observed" && (a.IsLoss ^ b.IsLoss);
                            var limit = NaiveRule.Tolerance(a.Mz) + NaiveRule.Tolerance(b.Mz) + (observed ? rule.PrecursorTolerance + 1e-10 : 0.0);
                            var delta = Math.Abs(a.Mz - b.Mz);
                            if (delta > limit) {
                                continue;
                            }
                            var key = rule.TermId(t) + ":" + a.Key + " ~ " + rule.TermId(v) + ":" + b.Key;
                            if (!closest.TryGetValue(key, out var c) || delta < c.Delta) {
                                closest[key] = (delta, h.Id);
                            }
                        }
                    }
                }
            }
            lines.AddRange(closest.Select(kv => id + " " + kv.Key + " delta " + kv.Value.Delta.ToString("F6", CultureInfo.InvariantCulture)
                                                + " Da at " + kv.Value.Hypothesis));
        }
        return lines;
    }

    [TestMethod]
    public void SupportVetoOverlapsAreListedNotRefused() {
        var overlaps = SupportVetoOverlaps(ShippedRuleFixtures.RuleIds().Select(ShippedRuleFixtures.NaiveModel));
        TestContext.WriteLine("support-veto overlaps of the shipped rules (" + overlaps.Count.ToString(CultureInfo.InvariantCulture) + "):");
        foreach (var line in overlaps) {
            TestContext.WriteLine(line);
        }
        // At the declared tolerances (msfinder at its default 0.01 Da) nothing is refused: the overlap check covers
        // support items only, so an overlap with a veto is listed here and never refused.
        var evaluator = new DiagnosticRuleEvaluator(DiagnosticRuleSet.LoadEmbedded(), new DiagnosticEvaluationOptions { Filter = RuleFilter.All });
        Assert.AreEqual(0, evaluator.Refusals.Count, string.Join("\n", evaluator.Refusals.Select(r => r.RuleId + " " + r.Code + " " + r.HypothesisId)));
        foreach (var line in overlaps) {
            Assert.IsTrue(evaluator.Refusals.All(r => !line.StartsWith(r.RuleId + " ", StringComparison.Ordinal)), line);
        }
    }

    [TestMethod]
    public void TheOverlapListingFindsASupportVetoPairWithinTwoTolerances() {
        // The design 4.8 example: HSO4- 96.9601 and H2PO4- 96.9696 lie 0.0095 Da apart, less than 2 x 0.005.
        var set = new InlineRuleSet().AddRule("sulfate.synthetic.neg",
            EngineHarness.Terms(InlineRuleSet.Term("hso4", "product_ion", "required", InlineRuleSet.Formula("HO4S")),
                                InlineRuleSet.Term("x", "product_ion", "required", InlineRuleSet.Formula("C7H3O4")),
                                InlineRuleSet.Term("v", "product_ion", "veto_present", InlineRuleSet.Formula("H2O4P"))),
            InlineRuleSet.AllOf(new[] { "hso4", "x" }, new[] { "v" }), InlineRuleSet.Composition("C15H10O5", null, "{}", "{}"), "[\"[M-H]-\"]", "hso4");
        var rule = new NaiveRule((JsonObject)set.Docs["rules/sulfate.synthetic.neg.json"], (JsonObject)set.Docs[DiagnosticRuleSetLoader.UnitsFile],
                                 (JsonObject)set.Docs[DiagnosticRuleSetLoader.AdductsFile], null);
        var lines = SupportVetoOverlaps(new[] { rule });
        Assert.AreEqual(1, lines.Count, string.Join("\n", lines));
        StringAssert.StartsWith(lines[0], "sulfate.synthetic.neg hso4:HO4S ~ v:H2O4P delta 0.0095");
    }
}
