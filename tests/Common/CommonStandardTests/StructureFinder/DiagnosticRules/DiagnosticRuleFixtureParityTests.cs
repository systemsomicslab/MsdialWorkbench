#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.DiagnosticRuleFixtures;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The engine parity fixtures (design 7.2, 2.4): <c>engine/semantics.json</c> and <c>engine/loader-errors.json</c>,
/// pinned by <c>manifest.json</c>. Every semantics case replays to its canonical result bytes, under every culture,
/// and its hand-written expectation agrees with those bytes; every loader case replays to its error list, which is
/// the CPython expectation. A missing fixture ends Inconclusive. With MSDIAL_DIAGNOSTIC_FIXTURES_WRITE=&lt;dir&gt; the
/// fixtures are regenerated into &lt;dir&gt; and the regeneration test ends Inconclusive.
/// </summary>
[TestClass]
[DeploymentItem(@"Resources\DiagnosticRuleFixtures", @"Resources\DiagnosticRuleFixtures")]
public class DiagnosticRuleFixtureParityTests
{
    private static byte[] BytesOrInconclusive(string relative) {
        var path = FixturePath(relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) {
            Assert.Inconclusive(relative + " is not deployed");
        }
        return File.ReadAllBytes(path);
    }

    private static JsonObject Fixture(string relative) => StrictJsonReader.Read(BytesOrInconclusive(relative));

    private static IEnumerable<JsonObject> Cases(string relative) => ((JsonArray)Fixture(relative)["cases"]).Cast<JsonObject>();

    private static string Name(JsonObject c) => ((JsonString)c["name"]).Value;

    // ------------------------------------------------------------------------------------------------ files

    [TestMethod]
    public void TheEngineFixturesAreTheGeneratedOnes() {
        var generated = EngineFixtures.Generate();
        var target = Environment.GetEnvironmentVariable(EngineFixtures.WriteVariable);
        if (!string.IsNullOrEmpty(target)) {
            foreach (var (path, bytes) in generated) {
                var full = Path.Combine(target!, path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllBytes(full, bytes);
            }
            Assert.Inconclusive("engine fixtures written to " + target);
        }
        foreach (var (path, bytes) in generated) {
            CollectionAssert.AreEqual(bytes, BytesOrInconclusive(path), path + " differs from its regeneration; record with " + EngineFixtures.WriteVariable);
        }
    }

    [TestMethod]
    public void TheManifestPinsEachEngineFixture() {
        var manifest = Fixture(EngineFixtures.ManifestFile);
        Assert.AreEqual(EngineFixtures.ManifestFormat, ((JsonString)manifest["format"]).Value);
        Assert.AreEqual(1L, ((JsonInteger)manifest["format_version"]).Value);
        Assert.AreEqual(DiagnosticRuleEvaluator.EngineSemantics, ((JsonString)manifest["engine_semantics"]).Value);
        // The engine entries; ShippedRuleFixtureTests checks the shipped-set entries and the rule-set pin.
        var files = (JsonObject)manifest["files"];
        CollectionAssert.AreEqual(new[] { EngineFixtures.LoaderErrorsFile, EngineFixtures.SemanticsFile },
                                  files.Keys.Where(k => k.StartsWith("engine/", StringComparison.Ordinal)).ToArray());
        var source = Encoding.ASCII.GetString(BytesOrInconclusive(EngineFixtures.SourceFile));
        foreach (var m in files.Members.Where(m => m.Key.StartsWith("engine/", StringComparison.Ordinal))) {
            var bytes = BytesOrInconclusive(m.Key);
            var pin = (JsonObject)m.Value;
            var sha = ((JsonString)pin["sha256"]).Value;
            Assert.AreEqual(sha, Sha256Hex.Of(bytes), m.Key);
            StringAssert.Contains(source, sha);
            var doc = StrictJsonReader.Read(bytes);
            Assert.AreEqual(((JsonInteger)pin["cases"]).Value, ((JsonArray)doc["cases"]).Count, m.Key);
            // The WS2 header, in storage form.
            Assert.AreEqual("cases engine_semantics format format_version kind rule_set_sha256 rule_set_version",
                            string.Join(" ", doc.Keys.OrderBy(k => k, StringComparer.Ordinal)), m.Key);
            Assert.AreEqual(EngineFixtures.Format, ((JsonString)doc["format"]).Value);
            Assert.AreEqual(1L, ((JsonInteger)doc["format_version"]).Value);
            Assert.AreEqual(DiagnosticRuleEvaluator.EngineSemantics, ((JsonString)doc["engine_semantics"]).Value);
            Assert.IsInstanceOfType(doc["rule_set_version"], typeof(JsonNull));
            Assert.IsInstanceOfType(doc["rule_set_sha256"], typeof(JsonNull));
            Assert.AreEqual(m.Key == EngineFixtures.SemanticsFile ? EngineFixtures.SemanticsKind : EngineFixtures.LoaderErrorsKind, ((JsonString)doc["kind"]).Value);
            CollectionAssert.AreEqual(bytes, PrettyJson.ToBytes(doc), m.Key + " is not in storage form");
            Assert.AreEqual(((JsonArray)doc["cases"]).Count, ((JsonArray)doc["cases"]).Cast<JsonObject>().Select(Name).Distinct(StringComparer.Ordinal).Count());
        }
        // The CPython-owned vectors stay as the catalog wrote them.
        Assert.AreEqual(VectorSha256, Sha256Hex.Of(VectorBytesOrInconclusive()));
    }

    // ------------------------------------------------------------------------------------------------ semantics

    /// <summary>Replays every semantics case; returns "name\tresult text" lines and adds failures.</summary>
    private static string Replay(IEnumerable<JsonObject> cases, List<string> failures) {
        var sb = new StringBuilder();
        foreach (var c in cases) {
            var name = Name(c);
            var result = EngineFixtures.Run(c);
            var tree = DiagnosticResultJson.ToJson(result);
            var text = CanonicalJson.ToText(tree);
            Assert.AreEqual(DiagnosticResultJson.Write(result), text);
            if (text != ((JsonString)c["result"]).Value) {
                failures.Add(name + ": the result bytes differ\n  expected " + ((JsonString)c["result"]).Value + "\n  got      " + text);
            }
            if (Sha256Hex.Of(Encoding.ASCII.GetBytes(text)) != ((JsonString)c["result_sha256"]).Value) {
                failures.Add(name + ": result_sha256 differs");
            }
            foreach (var f in EngineFixtures.CheckExpectations((JsonObject)c["expect"], tree)) {
                failures.Add(name + ": " + f);
            }
            sb.Append(name).Append('\t').Append(text).Append('\n');
        }
        return sb.ToString();
    }

    [TestMethod]
    public void EverySemanticsCaseReplaysToItsResultAndExpectation() {
        var failures = new List<string>();
        Replay(Cases(EngineFixtures.SemanticsFile), failures);
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    [TestMethod]
    public void TheCatalogueExpectationsAgreeWithTheEvaluator() {
        // The hand-written expectations against a fresh evaluation, without the fixture file.
        var failures = new List<string>();
        foreach (var c in SemanticsFixtureCases.All()) {
            var input = EngineFixtures.CaseInput(c);
            foreach (var f in EngineFixtures.CheckExpectations((JsonObject)input["expect"], DiagnosticResultJson.ToJson(EngineFixtures.Run(input)))) {
                failures.Add(c.Name + ": " + f);
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
        Assert.AreEqual(SemanticsFixtureCases.All().Count, SemanticsFixtureCases.All().Select(c => c.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void TheExpectationCheckerReportsEveryMismatch() {
        var basic = SemanticsFixtureCases.All().First(c => c.Name == "basic_fires");
        var tree = DiagnosticResultJson.ToJson(EngineFixtures.Run(EngineFixtures.CaseInput(basic)));
        const string Rule = "'flavonoid.o_glycoside.neg'";
        Assert.AreEqual(0, EngineFixtures.CheckExpectations(EngineFixtures.Spec(basic.Expect), tree).Count);
        foreach (var wrong in new[]
        {
            "{'status':'no_peaks'}", "{'base_intensity':'1000.0'}", "{'not_measured':['x']}", "{'outcome_ids':[]}", "{'refusals':['x']}",
            "{'no_such_field':1}", "{'outcomes':{'other.rule':{}}}", "{'outcomes':{" + Rule + ":{'fired':false}}}",
            "{'outcomes':{" + Rule + ":{'chosen':null}}}", "{'outcomes':{" + Rule + ":{'matched':['Y0:A@1']}}}",
            "{'outcomes':{" + Rule + ":{'matched':['Y0:A@1','X:C7H3O4@3']}}}", "{'outcomes':{" + Rule + ":{'chosen_score':'1.3'}}}",
            "{'outcomes':{" + Rule + ":{'hypotheses':{'core|Hex1|[M-H]-':'fired'}}}}" /* compact: all is empty */, "{'outcomes':{" + Rule + ":{'k_of':['/logic 1 1 true']}}}",
            "{'outcomes':{" + Rule + ":{'product_tolerance':{'source':'msfinder'}}}}", "{'outcomes':{" + Rule + ":{'vetoes':['w:true']}}}",
        }) {
            Assert.AreEqual(1, EngineFixtures.CheckExpectations(EngineFixtures.Spec(wrong), tree).Count, wrong);
        }
    }

    private static IEnumerable<object[]> Cultures() => CultureIndependenceTests.Cultures();

    [DataTestMethod]
    [DynamicData(nameof(Cultures), DynamicDataSourceType.Method)]
    public void TheSemanticsCasesGiveTheSameBytesUnder(string culture) {
        var cases = Cases(EngineFixtures.SemanticsFile).ToList();
        var failures = new List<string>();
        var invariant = Under(CultureInfo.InvariantCulture, () => Replay(cases, failures));
        var actual = Under(culture == "hostile" ? CultureIndependenceTests.Hostile() : CultureInfo.GetCultureInfo(culture), () => Replay(cases, failures));
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
        Assert.AreEqual(invariant, actual);
    }

    /// <summary>The semantics cases that sit on a ratio comparison on purpose, built from exactly representable
    /// intensities (design 7.2): their ratio violations are expected, every other margin still holds.</summary>
    private static readonly string[] DeliberateRatioEqualities = { "ordering_equal", "veto_stronger_than_equal" };

    [TestMethod]
    public void TheSemanticsInputsKeepTheirMargins() {
        var failures = new List<string>();
        var equalities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in Cases(EngineFixtures.SemanticsFile)) {
            var peaks = (JsonArray)((JsonObject)c["input"])["peaks"];
            foreach (var p in peaks.Cast<JsonArray>().SelectMany(x => x).Cast<JsonString>()) {
                // At most 6 decimals, always with a point: the text a Peak cannot alter.
                var dot = p.Value.IndexOf('.');
                Assert.IsTrue(dot > 0 && p.Value.Length - dot - 1 <= 6, Name(c) + " " + p.Value);
            }
            foreach (var v in EngineFixtures.AuditMargins(c)) {
                if (DeliberateRatioEqualities.Contains(Name(c)) && v.Contains(" ratio: ")) {
                    equalities.Add(Name(c));
                    continue;
                }
                failures.Add(Name(c) + ": " + v);
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
        // The audit sees each deliberate equality, so it would flag one that is not deliberate.
        CollectionAssert.AreEquivalent(DeliberateRatioEqualities, equalities.ToList());
    }

    [TestMethod]
    public void TheMarginAuditFlagsBoundaryInputs() {
        var basic = SemanticsFixtureCases.All().First(c => c.Name == "basic_fires");
        string[] Peak(long micro, long intensity) => new[] { SemanticsFixtureCases.Text(micro), SemanticsFixtureCases.Text(intensity * 1000000L) };
        var y0 = SemanticsFixtureCases.IonMicro(EvaluatorHarness.Y0Ion, -1);
        var x = SemanticsFixtureCases.IonMicro(EvaluatorHarness.XIon, -1);
        List<string> Audit(params string[][] peaks) {
            var c = new SemanticsCase("probe", "probe", basic.Set) { Options = basic.Options, Precursor = basic.Precursor, Peaks = peaks.ToList() };
            return EngineFixtures.AuditMargins(EngineFixtures.CaseInput(c));
        }
        Assert.AreEqual(0, Audit(Peak(300000000L, 1000), Peak(y0, 800), Peak(x, 500)).Count);
        // X 0.005000 Da high sits on the p2 boundary (within the 5e-7 rounding of its target).
        StringAssert.Contains(string.Join("\n", Audit(Peak(300000000L, 1000), Peak(y0, 800), Peak(x + 5000, 500))), "X:C7H3O4 peak 2 tolerance");
        // X at rel 0.05 sits on its floor.
        StringAssert.Contains(string.Join("\n", Audit(Peak(300000000L, 1000), Peak(y0, 800), Peak(x, 50))), "X:C7H3O4 peak 2 floor");
        // A precursor 0.010000 Da high sits on the precursor tolerance; a peak 1.0 Da from it on the precursor region.
        var high = new SemanticsCase("probe", "probe", basic.Set)
        {
            Options = basic.Options, Precursor = SemanticsFixtureCases.Text(431098371L + 10000L), Peaks = basic.Peaks,
        };
        StringAssert.Contains(string.Join("\n", EngineFixtures.AuditMargins(EngineFixtures.CaseInput(high))), "precursor");
        StringAssert.Contains(string.Join("\n", Audit(Peak(300000000L, 1000), Peak(y0, 800), Peak(x, 500), Peak(431098371L - 1000000L, 100))),
                              "peak 3 precursor region");
    }

    [TestMethod]
    public void TheSemanticsCasesCoverEveryKindRoleModeConventionTierVariantStatusAndFailure() {
        var kinds = new SortedSet<string>(StringComparer.Ordinal);
        var roles = new SortedSet<string>(StringComparer.Ordinal);
        var modes = new SortedSet<string>(StringComparer.Ordinal);
        var tiers = new SortedSet<string>(StringComparer.Ordinal);
        var conventions = new SortedSet<string>(StringComparer.Ordinal);
        var variants = new SortedSet<string>(StringComparer.Ordinal);
        var filters = new SortedSet<string>(StringComparer.Ordinal);
        var statuses = new SortedSet<string>(StringComparer.Ordinal);
        var failures = new SortedSet<string>(StringComparer.Ordinal);
        var preconditions = new SortedSet<string>(StringComparer.Ordinal);
        var ineligible = new SortedSet<string>(StringComparer.Ordinal);
        bool refused = false, eligible = false, notMeasured = false, singleIon = false, conflict = false, tie = false;
        var variantKeys = new[] { "precursor_tolerance_da", "product_tolerance_override", "loss_reference_override", "decoy_shift_da", "floor_scale",
                                  "k_offset", "overlap", "assignment_cap" };
        foreach (var c in Cases(EngineFixtures.SemanticsFile)) {
            var options = (JsonObject)c["options"];
            filters.Add(options.TryGetValue("filter", out var f) ? ((JsonString)f).Value : "measured_and_source_checked");
            variants.UnionWith(options.Keys.Where(k => variantKeys.Contains(k)));
            // The result tree of the replay (the replay test asserts that its text is the recorded result).
            var result = DiagnosticResultJson.ToJson(EngineFixtures.Run(c));
            statuses.Add(((JsonString)result["status"]).Value);
            modes.Add(((JsonString)result["precondition_mode"]).Value);
            tiers.Add(((JsonString)result["adduct_tier"]).Value);
            refused |= ((JsonArray)result["refusals"]).Count > 0;
            notMeasured |= ((JsonArray)result["not_measured"]).Count > 0;
            conflict |= ((JsonBoolean)result["rule_conflict"]).Value;
            var terms = new Dictionary<(string, string), JsonObject>();
            foreach (var file in ((JsonObject)c["files"]).Members.Where(m => m.Key.StartsWith("rules/", StringComparison.Ordinal))) {
                var rule = (JsonObject)file.Value;
                foreach (var t in ((JsonArray)rule["terms"]).Cast<JsonObject>()) {
                    terms[(((JsonString)rule["rule_id"]).Value, ((JsonString)t["term_id"]).Value)] = t;
                }
            }
            foreach (var o in ((JsonArray)result["outcomes"]).Cast<JsonObject>()) {
                var ruleId = ((JsonString)o["rule_id"]).Value;
                conventions.Add(((JsonString)o["convention"]).Value);
                eligible |= ((JsonBoolean)o["claim_eligible"]).Value;
                singleIon |= ((JsonBoolean)o["variant_single_ion"]).Value;
                tie |= ((JsonArray)o["tied_hypothesis_ids"]).Count > 1;
                ineligible.UnionWith(((JsonArray)o["claim_ineligible"]).Cast<JsonString>().Select(s => s.Value));
                if (o["precondition_failure"] is JsonString pf) {
                    preconditions.Add(pf.Value);
                }
                var hypotheses = ((JsonArray)o["all"]).Cast<JsonObject>().ToList();
                if (o["chosen"] is JsonObject chosen) {
                    hypotheses.Add(chosen);
                }
                foreach (var h in hypotheses) {
                    if (h["failure"] is JsonString failure) {
                        failures.Add(failure.Value);
                    }
                    foreach (var m in ((JsonArray)h["matched"]).Concat((JsonArray)h["upgrades"]).Cast<JsonObject>()) {
                        kinds.Add(((JsonString)m["kind"]).Value);
                        roles.Add(((JsonString)m["role"]).Value);
                    }
                    foreach (var v in ((JsonArray)h["vetoes"]).Cast<JsonObject>().Where(v => ((JsonBoolean)v["fired"]).Value)) {
                        var term = terms[(ruleId, ((JsonString)v["term_id"]).Value)];
                        kinds.Add(((JsonString)term["kind"]).Value);
                        roles.Add(((JsonString)term["role"]).Value);
                    }
                }
            }
        }
        Assert.AreEqual("aglycone_ion ion_series loss_from_precursor product_ion window", string.Join(" ", kinds));
        Assert.AreEqual("counted ordering required upgrade veto_present veto_stronger_than", string.Join(" ", roles));
        Assert.AreEqual("adduct_ambiguous mz_only", string.Join(" ", modes));
        Assert.AreEqual("msdial p2", string.Join(" ", conventions));
        Assert.AreEqual("assumed cross_polarity curated within_polarity_pair", string.Join(" ", tiers));
        Assert.AreEqual(string.Join(" ", variantKeys.OrderBy(k => k, StringComparer.Ordinal)), string.Join(" ", variants));
        Assert.AreEqual("all author_reviewed measured_and_source_checked", string.Join(" ", filters));
        Assert.AreEqual("evaluated no_peaks no_precursor unsupported_ion_mode", string.Join(" ", statuses));
        Assert.AreEqual("assignment_overflow logic_false no_base_peak veto", string.Join(" ", failures));
        Assert.AreEqual("adduct_not_in_rule ion_mode", string.Join(" ", preconditions));
        Assert.AreEqual("instrument_class not_reviewed precondition_mode tolerance_differs variant_active", string.Join(" ", ineligible));
        Assert.IsTrue(refused && eligible && notMeasured && singleIon && conflict && tie);
    }

    // ------------------------------------------------------------------------------------------------ loader errors

    [TestMethod]
    public void EveryLoaderErrorCaseReplaysToItsErrorList() {
        var codes = new List<string>();
        foreach (var c in Cases(EngineFixtures.LoaderErrorsFile)) {
            var name = Name(c);
            var files = Files((JsonObject)c["files"]);
            var report = DiagnosticRuleSetLoader.Validate(files, ((JsonBoolean)c["check_pin"]).Value);
            Assert.AreEqual(ErrorList((JsonArray)c["errors"]), ErrorList(report.Errors), name);
            var expect = (JsonObject)c["expect"];
            var code = ((JsonString)c["code"]).Value;
            Assert.AreEqual(code, ((JsonString)expect["code"]).Value, name);
            var defect = new RuleLoadError(code, ((JsonString)expect["file"]).Value, ((JsonString)expect["pointer"]).Value);
            Assert.IsTrue(report.Errors.Contains(defect), name + ": " + defect);
            Assert.ThrowsException<DiagnosticRuleSetException>(() => DiagnosticRuleSet.LoadFiles(files, RuleSetSource.Embedded), name);
            codes.Add(code);
        }
        // One case per loader error code.
        CollectionAssert.AreEqual(RuleErrorCodes.All.Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal).ToList(), codes.OrderBy(k => k, StringComparer.Ordinal).ToList());
    }

    [TestMethod]
    public void TheLoaderErrorCasesAreTheCPythonExpectations() {
        var vectors = ((JsonArray)((JsonObject)Vectors()["loader_errors"])["cases"]).Cast<JsonObject>().ToDictionary(Name, StringComparer.Ordinal);
        var baseFiles = (JsonObject)((JsonObject)Vectors()["loader_errors"])["base_files"];
        foreach (var c in Cases(EngineFixtures.LoaderErrorsFile)) {
            var name = Name(c);
            var vector = vectors[name];
            Assert.AreEqual(ErrorList((JsonArray)vector["errors"]), ErrorList((JsonArray)c["errors"]), name);
            Assert.AreEqual(((JsonString)vector["code"]).Value, ((JsonString)c["code"]).Value, name);
            // The file map is the vector's base files with its patch applied, byte for byte.
            var expected = Patched(baseFiles, (JsonObject)vector["patch"]);
            var actual = Files((JsonObject)c["files"]);
            CollectionAssert.AreEqual(expected.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList(), actual.Keys.ToList(), name);
            foreach (var kv in expected) {
                CollectionAssert.AreEqual(kv.Value, actual[kv.Key], name + " " + kv.Key);
            }
        }
    }
}
