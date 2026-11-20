#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.DiagnosticRuleFixtures;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The shipped rule set's parity fixtures (design 7.2): <c>rules/&lt;rule_id&gt;.json</c>, <c>masses.json</c>,
/// <c>rules/SOURCE.md</c> and <c>manifest.json</c>. They are the generated ones, the manifest pins every fixture and
/// the embedded set, every case replays to its canonical result bytes, its constructive expectation agrees with those
/// bytes, every input keeps the engine's margins, and every mass is within 1e-5 Da of the NCDK oracle. With
/// MSDIAL_DIAGNOSTIC_FIXTURES_WRITE=&lt;dir&gt; the fixtures and the manifest are regenerated into &lt;dir&gt; and the
/// regeneration test ends Inconclusive.
/// </summary>
[TestClass]
[DeploymentItem(@"Resources\DiagnosticRuleFixtures", @"Resources\DiagnosticRuleFixtures")]
public class ShippedRuleFixtureTests
{
    /// <summary>
    /// Constructive expectations the engine does not meet, as "rule_id/case: reason". Each entry is a finding for the
    /// author, never a reason to edit a rule; the test fails on any disagreement not listed here and on any listed one
    /// that has gone away.
    /// </summary>
    private static readonly string[] KnownDisagreements = new string[0];

    private static readonly ConcurrentDictionary<string, DiagnosticRuleResult> Results = new ConcurrentDictionary<string, DiagnosticRuleResult>(StringComparer.Ordinal);

    private static byte[] BytesOrInconclusive(string relative) {
        var path = FixturePath(relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) {
            Assert.Inconclusive(relative + " is not deployed");
        }
        return File.ReadAllBytes(path);
    }

    private static JsonObject Fixture(string relative) => StrictJsonReader.Read(BytesOrInconclusive(relative));

    private static string Name(JsonObject c) => ((JsonString)c["name"]).Value;

    private static IEnumerable<(string RuleId, JsonObject Case)> RuleCases() {
        foreach (var id in ShippedRuleFixtures.RuleIds()) {
            foreach (var c in ((JsonArray)Fixture(ShippedRuleFixtures.RuleFile(id))["cases"]).Cast<JsonObject>()) {
                yield return (id, c);
            }
        }
    }

    /// <summary>The result of a recorded case, evaluated once per test run.</summary>
    private static DiagnosticRuleResult Result(string ruleId, JsonObject c) =>
        Results.GetOrAdd(ruleId + "/" + Name(c), _ => ShippedRuleFixtures.Run(ruleId, c));

    // ------------------------------------------------------------------------------------------------ files

    [TestMethod]
    public void TheShippedFixturesAreTheGeneratedOnes() {
        var generated = ShippedRuleFixtures.Generate();
        var target = Environment.GetEnvironmentVariable(EngineFixtures.WriteVariable);
        if (!string.IsNullOrEmpty(target)) {
            foreach (var (path, bytes) in generated) {
                var full = Path.Combine(target!, path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllBytes(full, bytes);
            }
            Assert.Inconclusive("shipped-set fixtures written to " + target);
        }
        foreach (var (path, bytes) in generated) {
            CollectionAssert.AreEqual(bytes, BytesOrInconclusive(path), path + " differs from its regeneration; record with " + EngineFixtures.WriteVariable);
        }
        // No per-rule fixture is left over from a rule that is no longer shipped.
        var deployed = Directory.GetFiles(FixturePath("rules"), "*.json").Select(f => ShippedRuleFixtures.RulesDirectory + Path.GetFileName(f))
                                .OrderBy(f => f, StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(ShippedRuleFixtures.RuleIds().Select(ShippedRuleFixtures.RuleFile).ToList(), deployed);
    }

    [TestMethod]
    public void TheManifestPinsTheShippedSetAndEveryFixture() {
        var manifest = Fixture(EngineFixtures.ManifestFile);
        var (version, sha) = ShippedRuleFixtures.PinnedRuleSet();
        var embedded = DiagnosticRuleSet.LoadEmbedded();
        Assert.AreEqual(version, embedded.Identity.Version, "manifest.json pins another version; record with " + EngineFixtures.WriteVariable);
        Assert.AreEqual(sha, embedded.Identity.Sha256, "manifest.json pins another rule-set sha256; record with " + EngineFixtures.WriteVariable);
        Assert.AreEqual(ShippedRuleFixtures.EmbeddedIdentity(), (version, sha));
        var files = (JsonObject)manifest["files"];
        var expected = new[] { EngineFixtures.LoaderErrorsFile, EngineFixtures.SemanticsFile, ShippedRuleFixtures.MassesFile }
            .Concat(ShippedRuleFixtures.RuleIds().Select(ShippedRuleFixtures.RuleFile)).OrderBy(f => f, StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(expected, files.Keys.ToList());
        foreach (var m in files.Members.Where(m => !m.Key.StartsWith("engine/", StringComparison.Ordinal))) {
            var bytes = BytesOrInconclusive(m.Key);
            var pin = (JsonObject)m.Value;
            Assert.AreEqual(((JsonString)pin["sha256"]).Value, Sha256Hex.Of(bytes), m.Key);
            var doc = StrictJsonReader.Read(bytes);
            Assert.AreEqual(((JsonInteger)pin["cases"]).Value, ((JsonArray)doc["cases"]).Count, m.Key);
            Assert.AreEqual("cases engine_semantics format format_version kind rule_set_sha256 rule_set_version",
                            string.Join(" ", doc.Keys.OrderBy(k => k, StringComparer.Ordinal)), m.Key);
            Assert.AreEqual(EngineFixtures.Format, ((JsonString)doc["format"]).Value, m.Key);
            Assert.AreEqual(1L, ((JsonInteger)doc["format_version"]).Value, m.Key);
            Assert.AreEqual(DiagnosticRuleEvaluator.EngineSemantics, ((JsonString)doc["engine_semantics"]).Value, m.Key);
            Assert.AreEqual(m.Key == ShippedRuleFixtures.MassesFile ? ShippedRuleFixtures.MassesKind : ShippedRuleFixtures.RuleKind, ((JsonString)doc["kind"]).Value, m.Key);
            // The manifest is the one pin of the rule set's sha256.
            Assert.IsInstanceOfType(doc["rule_set_sha256"], typeof(JsonNull), m.Key);
            Assert.AreEqual(version, ((JsonString)doc["rule_set_version"]).Value, m.Key);
            CollectionAssert.AreEqual(bytes, PrettyJson.ToBytes(doc), m.Key + " is not in storage form");
            var cases = ((JsonArray)doc["cases"]).Cast<JsonObject>().ToList();
            Assert.AreEqual(cases.Count, cases.Select(Name).Distinct(StringComparer.Ordinal).Count(), m.Key + " repeats a case name");
        }
    }

    [TestMethod]
    public void EveryShippedRuleHasAFixtureWithAPositiveCase() {
        var missing = new List<string>();
        foreach (var id in ShippedRuleFixtures.RuleIds()) {
            var cases = ((JsonArray)Fixture(ShippedRuleFixtures.RuleFile(id))["cases"]).Cast<JsonObject>().Select(Name).ToList();
            if (!cases.Contains("positive")) {
                missing.Add(id);
            }
        }
        Assert.AreEqual(0, missing.Count, string.Join("\n", missing));
    }

    private static readonly Regex FiresOnTarget = new Regex(@"^(positive|tolerance_(ppm_)?in_(high|low)|precursor_in_(high|low)|k_of[0-9]+_at_k)$");
    private static readonly Regex DoesNotFire = new Regex(@"^(k_of[0-9]+_at_k_minus_1|decoy_plus_5)$");

    /// <summary>
    /// The outcome each case kind is built for, from its recorded expectation (which another test checks against the
    /// engine): positive, the _in_ edges and k_of at k fire with h* (named by the positive case) chosen; a veto leaves
    /// h* chosen and not fired, or, when its description says another hypothesis takes over, h* neither chosen nor
    /// tied; k_of at k - 1 and the decoy do not fire; wrong_mode fails its precondition; a tie ties; an upgrade case
    /// reports its upgrade. So a regenerated fixture cannot silently lose the coverage its case names promise.
    /// </summary>
    [TestMethod]
    public void EveryCaseHasTheOutcomeItsKindPromises() {
        var failures = new List<string>();
        foreach (var id in ShippedRuleFixtures.RuleIds()) {
            var cases = ((JsonArray)Fixture(ShippedRuleFixtures.RuleFile(id))["cases"]).Cast<JsonObject>().ToList();
            JsonObject? Outcome(JsonObject c) => ((JsonObject)((JsonObject)c["expect"])["outcomes"]).TryGetValue(id, out var o) ? (JsonObject)o : null;
            bool Fired(JsonObject? o) => o != null && ((JsonBoolean)o["fired"]).Value;
            string? Chosen(JsonObject? o) => o?["chosen"] is JsonString s ? s.Value : null;
            List<string> Ids(JsonObject? o, string key) => o?[key] is JsonArray a ? a.Cast<JsonString>().Select(s => s.Value).ToList() : new List<string>();
            var positive = cases.SingleOrDefault(c => Name(c) == "positive");
            if (positive is null) {
                failures.Add(id + ": no positive case");
                continue;
            }
            var description = ((JsonString)positive["description"]).Value;
            var end = description.IndexOf(", precursor at its oracle m/z", StringComparison.Ordinal);
            if (!description.StartsWith("h* = ", StringComparison.Ordinal) || end < 0) {
                failures.Add(id + "/positive: the description does not name h*");
                continue;
            }
            var target = description.Substring(5, end - 5);
            foreach (var c in cases) {
                var name = Name(c);
                var o = Outcome(c);
                string? wrong = null;
                if (FiresOnTarget.IsMatch(name)) {
                    wrong = Fired(o) && Chosen(o) == target ? null : "does not fire with h* chosen";
                }
                else if (DoesNotFire.IsMatch(name)) {
                    wrong = Fired(o) ? "fires" : null;
                }
                else if (name.StartsWith("veto_", StringComparison.Ordinal)) {
                    wrong = ((JsonString)c["description"]).Value.Contains("lets another hypothesis fire")
                        ? Chosen(o) == target || Ids(o, "tied_hypothesis_ids").Contains(target) ? "h* is still chosen or tied" : null
                        : !Fired(o) && Chosen(o) == target ? null : "does not leave h* vetoed and chosen";
                }
                else if (name == "wrong_mode") {
                    wrong = !Fired(o) && o?["precondition_failure"] is JsonString f && f.Value == "ion_mode" ? null : "does not fail on the ion mode";
                }
                else if (name == "tie") {
                    wrong = Fired(o) && Ids(o, "tied_hypothesis_ids").Count >= 2 ? null : "does not tie";
                }
                else if (name.StartsWith("upgrade_", StringComparison.Ordinal)) {
                    var term = name.Substring("upgrade_".Length) + ":";
                    wrong = Ids(o, "upgrades").Any(u => u.StartsWith(term, StringComparison.Ordinal)) ? null : "does not report the upgrade";
                }
                if (wrong != null) {
                    failures.Add(id + "/" + name + ": " + wrong);
                }
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    [TestMethod]
    public void AnOrderingRuleTiesWithItsOrderingTermAtItsOwnIntensity() {
        // alkyl_sulfate.neg is all(hso4, any(...)) with hso4 ordered above so3_radical and loss_so3 (ratio 1): with every
        // peak at one intensity hso4 is not stronger and nothing fires, so the tie keeps hso4 at its placed intensity.
        const string id = "sulfated_metabolite.alkyl_sulfate.neg";
        var built = new DiagnosticFixtureBuilder(ShippedRuleFixtures.NaiveModel(id)).Build();
        var tie = built.SingleOrDefault(c => c.Name == "tie");
        Assert.IsNotNull(tie, "no tie case for " + id);
        var outcome = (JsonObject)((JsonObject)tie!.Expect["outcomes"])[id];
        Assert.IsTrue(((JsonBoolean)outcome["fired"]).Value);
        Assert.IsTrue(((JsonArray)outcome["tied_hypothesis_ids"]).Count >= 2);
    }

    [TestMethod]
    public void TheBuilderMarginFlagsARatioEquality() {
        // The positive spectrum of alkyl_sulfate.neg with every product peak at one intensity puts hso4 exactly on
        // ratio 1.0 times its than-terms: a case there would test the equality by accident, so the builder refuses it.
        const string id = "sulfated_metabolite.alkyl_sulfate.neg";
        var naive = ShippedRuleFixtures.NaiveModel(id);
        var positive = new DiagnosticFixtureBuilder(naive).Build().Single(c => c.Name == "positive");
        var precursorMicro = DiagnosticFixtureBuilder.Micro(double.Parse(((JsonString)positive.Input["precursor_mz"]).Value, CultureInfo.InvariantCulture));
        var peaks = ((JsonArray)positive.Input["peaks"]).Cast<JsonArray>().Select(p => new BuiltPeak(
            DiagnosticFixtureBuilder.Micro(double.Parse(((JsonString)p[0]).Value, CultureInfo.InvariantCulture)),
            (long)Math.Round(double.Parse(((JsonString)p[1]).Value, CultureInfo.InvariantCulture) / 1e6))).ToList();
        Assert.IsNull(naive.MarginViolation(peaks, precursorMicro / 1e6, 0, 5e-6));
        var flat = peaks.Select(p => p.Micro == precursorMicro ? p : new BuiltPeak(p.Micro, 500)).ToList();
        StringAssert.Contains(naive.MarginViolation(flat, precursorMicro / 1e6, 0, 5e-6) ?? "", "on the ratio");
    }

    // ------------------------------------------------------------------------------------------------ replay

    [TestMethod]
    public void EveryRuleCaseReplaysToItsResult() {
        var failures = new List<string>();
        foreach (var (id, c) in RuleCases()) {
            var text = DiagnosticResultJson.Write(Result(id, c));
            if (text != ((JsonString)c["result"]).Value) {
                failures.Add(id + "/" + Name(c) + ": the result bytes differ\n  expected " + ((JsonString)c["result"]).Value + "\n  got      " + text);
            }
            if (Sha256Hex.Of(Encoding.ASCII.GetBytes(text)) != ((JsonString)c["result_sha256"]).Value) {
                failures.Add(id + "/" + Name(c) + ": result_sha256 differs");
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures.Take(20)));
    }

    /// <summary>Every disagreement between a case's constructive expectation and the engine's result, as
    /// "rule_id/case: detail" lines.</summary>
    private static List<string> Disagreements() {
        var found = new List<string>();
        foreach (var (id, c) in RuleCases()) {
            var tree = DiagnosticResultJson.ToJson(Result(id, c));
            foreach (var f in EngineFixtures.CheckExpectations((JsonObject)c["expect"], tree)) {
                found.Add(id + "/" + Name(c) + ": " + f);
            }
        }
        return found;
    }

    [TestMethod]
    public void TheConstructiveExpectationsAgreeWithTheEngine() {
        var found = Disagreements();
        var known = new HashSet<string>(KnownDisagreements.Select(k => k.Substring(0, k.IndexOf(':'))), StringComparer.Ordinal);
        var unexpected = found.Where(f => !known.Contains(f.Substring(0, f.IndexOf(':')))).ToList();
        var gone = known.Where(k => !found.Any(f => f.StartsWith(k + ":", StringComparison.Ordinal))).ToList();
        Assert.AreEqual(0, unexpected.Count, "disagreements:\n" + string.Join("\n", unexpected.Take(1000)));
        Assert.AreEqual(0, gone.Count, "listed disagreements that no longer occur:\n" + string.Join("\n", gone));
    }

    [TestMethod]
    public void TheRecordedExpectationsAreTheNaiveModelsOwn() {
        // The expectation is rebuilt without the engine; the regeneration test compares the files byte for byte, this
        // names the case when only an expectation moved.
        var failures = new List<string>();
        foreach (var id in ShippedRuleFixtures.RuleIds()) {
            var built = new DiagnosticFixtureBuilder(ShippedRuleFixtures.NaiveModel(id)).Build().ToDictionary(c => c.Name, StringComparer.Ordinal);
            foreach (var c in ((JsonArray)Fixture(ShippedRuleFixtures.RuleFile(id))["cases"]).Cast<JsonObject>()) {
                if (!built.TryGetValue(Name(c), out var b)) {
                    failures.Add(id + "/" + Name(c) + ": not built");
                }
                else if (CanonicalJson.ToText(b.Expect) != CanonicalJson.ToText(c["expect"]) || CanonicalJson.ToText(b.Input) != CanonicalJson.ToText(c["input"])) {
                    failures.Add(id + "/" + Name(c) + ": the builder now gives another input or expectation");
                }
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures.Take(20)));
    }

    [TestMethod]
    public void TheRuleCaseInputsKeepTheirMargins() {
        var failures = new List<string>();
        foreach (var (id, c) in RuleCases()) {
            foreach (var p in ((JsonArray)((JsonObject)c["input"])["peaks"]).Cast<JsonArray>().SelectMany(x => x).Cast<JsonString>()) {
                var dot = p.Value.IndexOf('.');
                Assert.IsTrue(dot > 0 && p.Value.Length - dot - 1 <= 6, id + "/" + Name(c) + " " + p.Value);
            }
            foreach (var v in EngineFixtures.AuditMargins(ShippedRuleFixtures.RestrictedSet(id), (JsonObject)c["options"], (JsonObject)c["input"])) {
                failures.Add(id + "/" + Name(c) + ": " + v);
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures.Take(40)));
    }

    // ------------------------------------------------------------------------------------------------ masses

    private static double Decimal(JsonObject row, string key) => EngineFixtures.Decimal(((JsonString)row[key]).Value);

    [TestMethod]
    public void EveryMassIsWithin1e5OfTheOracle() {
        var doc = Fixture(ShippedRuleFixtures.MassesFile);
        var rows = ((JsonArray)doc["cases"]).Cast<JsonObject>().ToList();
        var formulas = rows.Where(r => ((JsonString)r["kind"]).Value == "formula").ToList();
        var precursors = rows.Where(r => ((JsonString)r["kind"]).Value == "precursor").ToList();
        Assert.AreEqual(rows.Count, formulas.Count + precursors.Count);
        // Every formula of the set has a row.
        CollectionAssert.AreEqual(ShippedRuleFixtures.SetFormulas().Keys.ToList(), formulas.Select(r => ((JsonString)r["formula"]).Value).ToList());
        foreach (var r in formulas) {
            var f = ((JsonString)r["formula"]).Value;
            var pda = ((JsonInteger)r["pda"]).Value;
            Assert.AreEqual(TestMass.Of(f), pda, f);
            Assert.AreEqual(ShippedRuleFixtures.PdaText(pda), ((JsonString)r["neutral_mass"]).Value, f);
            Assert.AreEqual(ShippedRuleFixtures.PdaText(pda - TestMass.Electron), ((JsonString)r["cation_mz"]).Value, f);
            Assert.AreEqual(ShippedRuleFixtures.PdaText(pda + TestMass.Electron), ((JsonString)r["anion_mz"]).Value, f);
            var oracle = NcdkOracle.Mass(f);
            Assert.AreEqual(oracle, Decimal(r, "neutral_mass"), 1e-5, f);
            Assert.AreEqual(oracle - NcdkOracle.Electron, Decimal(r, "cation_mz"), 1e-5, f);
            Assert.AreEqual(oracle + NcdkOracle.Electron, Decimal(r, "anion_mz"), 1e-5, f);
        }
        foreach (var group in precursors.GroupBy(r => ((JsonString)r["rule_id"]).Value)) {
            var naive = ShippedRuleFixtures.NaiveModel(group.Key).All.ToDictionary(h => h.Id, StringComparer.Ordinal);
            foreach (var r in group) {
                var id = ((JsonString)r["hypothesis_id"]).Value;
                Assert.IsTrue(naive.TryGetValue(id, out var h), group.Key + " " + id + " is not a hypothesis of the naive enumerator");
                Assert.AreEqual(h!.Pda, ((JsonInteger)r["pda"]).Value, group.Key + " " + id);
                Assert.AreEqual(h.Adduct.Name, ((JsonString)r["adduct"]).Value, group.Key + " " + id);
                Assert.AreEqual(Formula.Hill(h.Neutral), ((JsonString)r["neutral_formula"]).Value, group.Key + " " + id);
                Assert.AreEqual(h.Mz, Decimal(r, "mz"), 1e-5, group.Key + " " + id);
            }
        }
        // Every rule and every tried adduct is sampled.
        foreach (var id in ShippedRuleFixtures.RuleIds()) {
            var sampled = precursors.Where(r => ((JsonString)r["rule_id"]).Value == id).Select(r => ((JsonString)r["adduct"]).Value).Distinct().Count();
            Assert.AreEqual(ShippedRuleFixtures.NaiveModel(id).Tried.Count, sampled, id);
        }
    }

    // ------------------------------------------------------------------------------------------------ the builder

    [TestMethod]
    public void TheOracleAgreesWithTheElementTableWithin1e7PerFormula() {
        // The embedded tables/elements.json, through the engine's own exact pDa arithmetic.
        var el = DiagnosticRuleSet.LoadEmbedded().Elements;
        var worst = 0.0;
        foreach (var f in ShippedRuleFixtures.SetFormulas().Keys) {
            var pda = el.MassPdaExact(Chemistry.Composition.Parse(f, el));
            worst = Math.Max(worst, Math.Abs(NcdkOracle.Mass(f) - (double)pda / 1e12));
        }
        Assert.IsTrue(worst < 1e-7, worst.ToString("R", CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void TheNaiveEnumeratorCountsTheEnginesHypotheses() {
        var set = DiagnosticRuleSet.LoadFiles(DiagnosticRuleSetLoader.ReadEmbedded().Where(kv => kv.Key.EndsWith(".json", StringComparison.Ordinal))
                                                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal), RuleSetSource.Inline);
        for (var r = 0; r < set.Rules.Count; r++) {
            var index = new Engine.HypothesisIndex(set.CompiledRules[r], set.Tables);
            var naive = ShippedRuleFixtures.NaiveModel(set.Rules[r].RuleId);
            Assert.AreEqual(index.Compositions.Count * index.Tried.Count, naive.All.Count, set.Rules[r].RuleId);
            CollectionAssert.AreEqual(index.Tried.Select(t => t.Adduct.Adduct).ToList(), naive.Tried.Select(t => t.Name).ToList(), set.Rules[r].RuleId);
        }
    }
}
