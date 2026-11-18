using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.DiagnosticRuleFixtures;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The static checks of design 3.6 on the cases design 7.1 names, beyond the one-per-code vectors: the two-ion
/// passes, claim-term counts, parameters_origin from each provenance source, review staleness for each resolved
/// dependency (and not for unrelated edits), and counts and masses beyond 64 bits. Each case edits the synthetic
/// base set of the loader vectors and loads it unpinned.
/// </summary>
/// <remarks>
/// The expected lists were checked against the Python reference: with MSDIAL_DIAGNOSTIC_RULES_CASES_OUT set, every
/// case is written there as a rule-set folder with a matching history row and its expected errors, for
/// <c>rule_format.py validate</c>.
/// </remarks>
[TestClass]
[DeploymentItem(@"Resources\DiagnosticRuleFixtures", @"Resources\DiagnosticRuleFixtures")]
public class RuleSetStaticCheckTests
{
    private const string Rule = "rules/flavonoid.o_glycoside.neg.json";
    private const string Units = "tables/units.json";
    private const string Adducts = "tables/adducts.json";
    private const string Motifs = "tables/motifs.json";
    private const string Elements = "tables/elements.json";
    private const string Scaffolds = "scaffolds/aglycones.json";
    private const string Nesting = "tables/motif_nesting.json";

    /// <summary>The loader vectors' base set as editable documents.</summary>
    private sealed class SynSet
    {
        public Dictionary<string, JsonValue> Docs { get; } = new Dictionary<string, JsonValue>(StringComparer.Ordinal);

        public SynSet() {
            var loader = (JsonObject)Vectors()["loader_errors"];
            foreach (var kv in DiagnosticRuleFixtures.Files((JsonObject)loader["base_files"])) {
                if (kv.Key.EndsWith(".json", StringComparison.Ordinal)) {
                    Docs[kv.Key] = StrictJsonReader.Read(kv.Value);
                }
            }
        }

        public SynSet Set(string file, string path, string json) {
            Docs[file] = JsonEdit.Set(Docs[file], path, JsonEdit.Parse(json));
            return this;
        }

        public Dictionary<string, byte[]> Files() => Docs.ToDictionary(kv => kv.Key, kv => PrettyJson.ToBytes(kv.Value), StringComparer.Ordinal);

        /// <summary>Gives the rule a review whose rule_semantic_sha256 is current.</summary>
        public SynSet Reviewed() {
            Set(Rule, "author_reviewed", "true").Set(Rule, "source_checked", "true");
            Set(Rule, "review", Review(new string('0', 64)));
            var sha = DiagnosticRuleSetLoader.Validate(Files(), false).SemanticSha256("flavonoid.o_glycoside.neg");
            return Set(Rule, "review", Review(sha));
        }

        private static string Review(string sha) =>
            "{\"by\":\"author\",\"at\":\"2026-12-01\",\"basis\":\"benchmark\",\"run_id\":\"p3cs-20261201-abc\","
            + "\"rules_precision_sha256\":\"" + new string('a', 64) + "\",\"review_proposal_sha256\":\"" + new string('b', 64) + "\","
            + "\"rule_semantic_sha256\":\"" + sha + "\",\"instrument_classes\":[\"QTOF-type\"],"
            + "\"product_tolerance\":{\"abs_da\":0.005,\"ppm\":10.0,\"source\":\"p2\"},\"precursor_tolerance_da\":0.01}";
    }

    private static string Term(string id, string kind, string role, string fields) =>
        "{\"term_id\":\"" + id + "\",\"kind\":\"" + kind + "\",\"role\":\"" + role + "\",\"term_source\":\"src:0\",\"min_rel_intensity\":0.05,"
        + "\"floor_source\":\"src:0\",\"than\":null,\"ratio\":null,\"ratio_source\":null,\"note\":null," + fields + "}";

    private static readonly string Y0 = Term("Y0", "aglycone_ion", "required", "\"form\":\"even\",\"charge_carrier\":\"proton\",\"plus\":null,\"minus\":null");
    private static readonly string Rda = Term("rda", "product_ion", "required", "\"scaffold_ions\":\"rda\"");
    private static readonly string Water = Term("w", "loss_from_precursor", "required", "\"formula\":\"H2O\"");
    private static readonly string Caffeate = Term("z", "product_ion", "required", "\"formula\":\"C9H7O4\"");

    private static string Check(string name, SynSet s) {
        var files = s.Files();
        var report = DiagnosticRuleSetLoader.Validate(files, checkPin: false);
        var got = ErrorList(report.Errors);
        Dump(name, files, report, got);
        return got;
    }

    private static void Dump(string name, Dictionary<string, byte[]> files, RuleSetLoadReport report, string errors) {
        var root = Environment.GetEnvironmentVariable("MSDIAL_DIAGNOSTIC_RULES_CASES_OUT");
        if (string.IsNullOrEmpty(root)) {
            return;
        }
        var dir = Path.Combine(root!, name);
        foreach (var kv in files) {
            var path = Path.Combine(dir, kv.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, kv.Value);
        }
        if (report.RuleSetSha256 != null && report.RuleSetVersion != null) {
            File.WriteAllBytes(Path.Combine(dir, "ruleset-history.tsv"), Encoding.ASCII.GetBytes(report.RuleSetVersion + "\t" + report.RuleSetSha256 + "\t2026-11-19\n"));
        }
        File.WriteAllBytes(Path.Combine(root!, name + ".expected.txt"), Encoding.ASCII.GetBytes(errors));
    }

    private static string E(params string[] lines) => string.Join("\n", lines);

    private static string At(string code, string file, string pointer) => code + "\t" + file + "\t" + pointer;

    [TestMethod]
    public void TheBaseSetIsClean() => Assert.AreEqual("", Check("base", new SynSet()));

    // ---------------------------------------------------------------------------- two-ion claim (3.6 item 6)

    [TestMethod]
    public void OneDiagnosticIonAndAWaterLossClaimsOnOneIon() {
        // all(X, loss H2O) with claim_term_ids [X]: claim_terms_too_few and claim_single_ion, exactly.
        var s = new SynSet().Set(Rule, "terms", "[" + Y0 + "," + Water + "]")
            .Set(Rule, "logic", "{\"all\":[{\"term\":\"Y0\"},{\"term\":\"w\"}]}")
            .Set(Rule, "claim/claim_term_ids", "[\"Y0\"]");
        Assert.AreEqual(E(At("claim_single_ion", Rule, "/claim/claim_term_ids"), At("claim_terms_too_few", Rule, "/claim/claim_term_ids")),
                        Check("two_ion_x_water", s));
    }

    [TestMethod]
    public void AGenericIonCannotStandInForTheSecondClaimIon() {
        // all(X, loss H2O, any(Y, Z)) with [X, Y]: passes the static count and pass 1, fails pass 2 (Z stands in for Y).
        var s = new SynSet().Set(Rule, "terms", "[" + Y0 + "," + Water + "," + Rda + "," + Caffeate + "]")
            .Set(Rule, "logic", "{\"all\":[{\"term\":\"Y0\"},{\"term\":\"w\"},{\"any\":[{\"term\":\"rda\"},{\"term\":\"z\"}]}]}")
            .Set(Rule, "claim/claim_term_ids", "[\"Y0\",\"rda\"]");
        Assert.AreEqual(At("claim_single_ion", Rule, "/claim/claim_term_ids"), Check("two_ion_any_stand_in", s));
    }

    private static SynSet IonSeriesClaim(int k) =>
        new SynSet().Set(Rule, "terms", "[" + Term("ys", "ion_series", "counted", "\"unit_classes\":[\"sugar\"],\"charge_carrier\":\"proton\",\"form\":\"even\",\"plus\":null,\"minus\":null") + "]")
            .Set(Rule, "logic", "{\"k_of\":{\"k\":" + k + ",\"k_source\":\"src:0\",\"of\":[{\"term\":\"ys\"}]}}")
            .Set(Rule, "claim/claim_term_ids", "[\"ys\"]")
            .Set(Rule, "preconditions/composition/units", "{\"Hex\":[2,3]}")
            .Set(Rule, "preconditions/composition/unit_totals", "{}");

    [TestMethod]
    public void AnIonSeriesCountedWithKOfTwoMayClaimAlone() => Assert.AreEqual("", Check("ion_series_k2", IonSeriesClaim(2)));

    [TestMethod]
    public void AnIonSeriesCountedWithKOfOneMayNot() =>
        Assert.AreEqual(E(At("claim_single_ion", Rule, "/claim/claim_term_ids"), At("claim_terms_too_few", Rule, "/claim/claim_term_ids"),
                          At("single_ion_claim", Rule, "/logic")),
                        Check("ion_series_k1", IonSeriesClaim(1)));

    [TestMethod]
    public void AnUnmeasuredRuleIsNotHeldToTwoIons() {
        var s = new SynSet().Set(Rule, "terms", "[" + Y0 + "," + Water + "]")
            .Set(Rule, "logic", "{\"all\":[{\"term\":\"Y0\"},{\"term\":\"w\"}]}")
            .Set(Rule, "claim/claim_term_ids", "[\"Y0\"]")
            .Set(Rule, "measure", "false");
        Assert.AreEqual("", Check("unmeasured_one_ion", s));
    }

    // ------------------------------------------------------------------------------- parameters_origin (3.5)

    private static IEnumerable<object[]> OriginSources() {
        yield return new object[] { "term_source", Rule, "terms/0/term_source", "\"tuned_vs20\"", "tuned_vs20" };
        yield return new object[] { "floor_source", Rule, "terms/2/floor_source", "\"tuned_pooled\"", "tuned_pooled" };
        yield return new object[] { "k_source", Rule, "logic/all/2/k_of/k_source", "\"tuned_vs20\"", "tuned_vs20" };
        yield return new object[] { "scaffold_entry", Scaffolds, "entries/1/origin", "\"tuned_vs20\"", "tuned_vs20" };
        yield return new object[] { "unit_row", Units, "units/1/origin", "\"tuned_vs20\"", "tuned_vs20" };
        yield return new object[] { "tried_adduct_row", Adducts, "adducts/4/origin", "\"tuned_vs20\"", "tuned_vs20" };
        yield return new object[] { "motif", Motifs, "motifs/0/origin", "\"tuned_vs20\"", "tuned_vs20" };
    }

    [DataTestMethod]
    [DynamicData(nameof(OriginSources), DynamicDataSourceType.Method)]
    public void ParametersOriginIsTheMaximumOverEverySource(string name, string file, string path, string value, string origin) {
        var s = new SynSet().Set(file, path, value);
        Assert.AreEqual(At("origin_inconsistent", Rule, "/parameters_origin"), Check("origin_" + name, s));
        s.Set(Rule, "parameters_origin", "\"" + origin + "\"");
        Assert.AreEqual("", Check("origin_" + name + "_declared", s));
    }

    [TestMethod]
    public void ARatioSourceCountsTowardsTheOrigin() {
        var s = new SynSet().Set(Rule, "terms/3/role", "\"veto_stronger_than\"").Set(Rule, "terms/3/than", "[\"Y0\"]")
            .Set(Rule, "terms/3/ratio", "0.5").Set(Rule, "terms/3/ratio_source", "\"tuned_vs20\"");
        Assert.AreEqual(At("origin_inconsistent", Rule, "/parameters_origin"), Check("origin_ratio_source", s));
    }

    [TestMethod]
    public void RowsTheRuleDoesNotResolveDoNotCount() {
        // A unit the rule does not name, and an adduct it does not try (not common), are not its parameters.
        var s = new SynSet().Set(Units, "units/4/origin", "\"tuned_vs20\"").Set(Adducts, "adducts/5/origin", "\"tuned_vs20\"");
        Assert.AreEqual("", Check("origin_unrelated_rows", s));
    }

    // ---------------------------------------------------------------------------------- review_stale (3.2)

    private static IEnumerable<object[]> ResolvedEdits() {
        yield return new object[] { "residue", Units, "units/0/residue", "\"C6H12O6\"" };
        yield return new object[] { "scaffold_entry_added", Scaffolds, "entries/-",
            "{\"scaffold_id\":\"luteolin\",\"name\":\"luteolin\",\"formula\":\"C15H10O6\",\"class\":\"flavone\",\"ions\":{\"rda\":{\"pos\":[\"C7H5O4\"],\"neg\":[\"C7H3O4\"]}},\"origin\":\"as_published\",\"source\":[{\"doi\":\"10.1000/synthetic.1\",\"locator\":\"Table 1\"}]}" };
        yield return new object[] { "adduct_common_flag", Adducts, "adducts/5/common", "true" };
        yield return new object[] { "motif_smarts", Motifs, "motifs/0/smarts", "\"c1ccccc1O\"" };
        yield return new object[] { "element_mass", Elements, "elements/O", "15.9949146" };
    }

    [DataTestMethod]
    [DynamicData(nameof(ResolvedEdits), DynamicDataSourceType.Method)]
    public void AReviewIsStaleWhenAResolvedDependencyChanges(string name, string file, string path, string value) {
        var s = new SynSet().Reviewed();
        Assert.AreEqual("", Check("review_current", s));
        s.Set(file, path, value);
        Assert.AreEqual(At("review_stale", Rule, "/review/rule_semantic_sha256"), Check("review_stale_" + name, s));
    }

    private static IEnumerable<object[]> UnrelatedEdits() {
        yield return new object[] { "another_unit", Units, "units/-",
            "{\"name\":\"Xyl\",\"class\":\"sugar\",\"residue\":\"C5H8O4\",\"origin\":\"as_published\",\"source\":[{\"doi\":\"10.1000/synthetic.1\",\"locator\":\"Table 1\"}]}" };
        yield return new object[] { "chemont_node", Motifs, "motifs/0/chemont/compatible/0/name", "\"Flavonoid glycosides\"" };
        yield return new object[] { "nesting_row", Nesting, "nesting/-",
            "{\"child\":\"hydroxycinnamoyl_hexose\",\"parent\":\"flavonoid\",\"source\":[{\"doi\":\"10.1000/synthetic.1\",\"locator\":\"Table 1\"}]}" };
        yield return new object[] { "non_common_adduct", Adducts, "adducts/-",
            "{\"adduct\":\"[M+K]+\",\"aliases\":[],\"charge\":1,\"multiplier\":1,\"add\":\"K\",\"remove\":null,\"common\":false,\"origin\":\"as_published\",\"source\":[{\"reference\":\"synthetic adduct table\",\"locator\":\"line 2\"}]}" };
        yield return new object[] { "review_date", Rule, "review/at", "\"2026-12-02\"" };
    }

    [DataTestMethod]
    [DynamicData(nameof(UnrelatedEdits), DynamicDataSourceType.Method)]
    public void AReviewStaysCurrentAfterAnUnrelatedEdit(string name, string file, string path, string value) {
        var s = new SynSet().Reviewed().Set(file, path, value);
        Assert.AreEqual("", Check("review_unrelated_" + name, s));
    }

    [TestMethod]
    public void TheEngineSemanticsIsPartOfTheHash() {
        var report = DiagnosticRuleSetLoader.Validate(new SynSet().Files(), false);
        var rule = report.Rules["flavonoid.o_glycoside.neg"].Doc;
        var resolved = report.Resolved("flavonoid.o_glycoside.neg");
        Assert.AreNotEqual(RuleSemanticHash.Compute(rule, resolved), RuleSemanticHash.Compute(rule, resolved, "msfinder-diagnostic-rules/2"));
        Assert.AreEqual(RuleSemanticHash.Compute(rule, resolved), RuleSemanticHash.Compute(rule.Without("review"), resolved));
    }

    // ------------------------------------------------------------------------- beyond 64 bits (design 4.1)

    [TestMethod]
    public void AHugeUnitCountIsAMassRangeErrorNotAnOverflow() {
        var s = new SynSet().Set(Rule, "preconditions/composition/units", "{\"Hex\":[1000000000000,1000000000000]}")
            .Set(Rule, "preconditions/composition/unit_totals", "{}");
        Assert.AreEqual(At("mass_range", Rule, "/preconditions/composition"), Check("huge_unit_count", s));
    }

    [TestMethod]
    public void AnElementBeyond64BitsIsAMassRangeErrorNotAnOverflow() {
        var s = new SynSet().Set(Elements, "elements/Xx", "999999999.0")
            .Set(Rule, "terms/-", Term("big", "product_ion", "upgrade", "\"formula\":\"Xx\""));
        Assert.AreEqual(At("mass_range", Rule, "/terms/4"), Check("huge_element", s));
    }
}

/// <summary>Edits of immutable JSON trees for tests: a path of keys and indices ("-" appends to an array).</summary>
internal static class JsonEdit
{
    public static JsonValue Parse(string json) => StrictJsonReader.Read(Encoding.ASCII.GetBytes("{\"v\":" + json + "}"))["v"];

    public static JsonValue Set(JsonValue root, string path, JsonValue value) => Set(root, path.Split('/'), 0, value);

    private static JsonValue Set(JsonValue node, string[] path, int i, JsonValue value) {
        var key = path[i];
        var last = i == path.Length - 1;
        switch (node) {
            case JsonObject o: {
                var members = o.Members.ToList();
                var index = members.FindIndex(m => m.Key == key);
                var child = last ? value : Set(o[key], path, i + 1, value);
                if (index >= 0) {
                    members[index] = new KeyValuePair<string, JsonValue>(key, child);
                }
                else {
                    members.Add(new KeyValuePair<string, JsonValue>(key, child));
                }
                return new JsonObject(members);
            }
            case JsonArray a: {
                var items = a.ToList();
                if (key == "-") {
                    items.Add(value);
                }
                else {
                    var index = int.Parse(key, System.Globalization.CultureInfo.InvariantCulture);
                    items[index] = last ? value : Set(items[index], path, i + 1, value);
                }
                return new JsonArray(items);
            }
            default:
                throw new ArgumentException("cannot descend into a scalar at " + key);
        }
    }
}
