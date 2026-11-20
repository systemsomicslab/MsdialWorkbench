#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Inline rule sets for the engine tests (design 7.1): the synthetic tables written here, in C#, and rules built from
/// a template. Nothing is read from a fixture file, so these tests never end Inconclusive. Rules default to
/// <c>measure: false</c>, so the two-ion passes do not constrain a test's logic; the engine components are exercised
/// directly.
/// </summary>
internal sealed class InlineRuleSet
{
    public const string ElementsJson =
        "{\"format\":\"msfinder-diagnostic-elements\",\"format_version\":1,\"electron\":0.000549,\"elements\":{\"C\":12.0,"
        + "\"Cl\":34.96885268,\"H\":1.007825032,\"K\":38.96370668,\"N\":14.003074,\"Na\":22.98976928,\"O\":15.99491462,"
        + "\"P\":30.97376163,\"S\":31.972071}}";

    private const string Src = "\"origin\":\"as_published\",\"source\":[{\"doi\":\"10.1000/synthetic.1\",\"locator\":\"Table 1\"}]";

    private static string Adduct(string name, int charge, string? add, string? remove, bool common, params string[] aliases) =>
        "{\"adduct\":\"" + name + "\",\"aliases\":[" + string.Join(",", aliases.Select(a => "\"" + a + "\"")) + "],\"charge\":"
        + charge.ToString(CultureInfo.InvariantCulture) + ",\"multiplier\":1,\"add\":" + Q(add) + ",\"remove\":" + Q(remove)
        + ",\"common\":" + (common ? "true" : "false") + "," + Src + "}";

    private static string Q(string? s) => s is null ? "null" : "\"" + s + "\"";

    public static readonly string AdductsJson =
        "{\"format\":\"msfinder-diagnostic-adducts\",\"format_version\":1,\"adducts\":["
        + Adduct("[M+H]+", 1, "H", null, true, "M+H") + ","
        + Adduct("[M+Na]+", 1, "Na", null, true) + ","
        + Adduct("[M]+", 1, null, null, false, "[M+]+") + ","
        + Adduct("[M-H]-", -1, null, "H", true) + ","
        + Adduct("[M+HCOO]-", -1, "CHO2", null, true, "[M+FA-H]-") + ","
        + Adduct("[M+Cl]-", -1, "Cl", null, false) + "]}";

    private static string Unit(string name, string cls, string residue) =>
        "{\"name\":\"" + name + "\",\"class\":\"" + cls + "\",\"residue\":\"" + residue + "\"," + Src + "}";

    /// <summary>Units in canonical order. Gal shares Hex's residue, so equal-mass members can be collapsed.</summary>
    public static readonly string UnitsJson =
        "{\"format\":\"msfinder-diagnostic-units\",\"format_version\":1,\"units\":["
        + Unit("Hex", "sugar", "C6H10O5") + "," + Unit("dHex", "sugar", "C6H10O4") + "," + Unit("Pen", "sugar", "C5H8O4") + ","
        + Unit("HexA", "sugar", "C6H8O6") + "," + Unit("Gal", "sugar", "C6H10O5") + "," + Unit("caffeoyl", "acyl", "C9H6O3") + "]}";

    private static string Motif(string id) =>
        "{\"motif_id\":\"" + id + "\",\"smarts\":\"c1ccccc1\",\"origin\":\"source_defined\",\"source\":[{\"doi\":\"10.1000/synthetic.1\",\"locator\":\"Table 1\"}],"
        + "\"chemont\":{\"compatible\":[{\"id\":\"CHEMONTID:0000334\",\"name\":\"Flavonoids\"}],\"broader\":[]}}";

    public static readonly string MotifsJson =
        "{\"format\":\"msfinder-diagnostic-motifs\",\"format_version\":1,\"motifs\":["
        + Motif("flavonoid_o_glycoside") + "," + Motif("flavonoid") + "," + Motif("anthocyanin") + "," + Motif("hydroxycinnamoyl_hexose") + "]}";

    public const string NestingJson =
        "{\"format\":\"msfinder-diagnostic-motif-nesting\",\"format_version\":1,\"nesting\":[{\"child\":\"flavonoid_o_glycoside\","
        + "\"parent\":\"flavonoid\",\"source\":[{\"doi\":\"10.1000/synthetic.1\",\"locator\":\"Table 1\"}]}]}";

    public const string RulesetJson = "{\"format\":\"msfinder-diagnostic-ruleset\",\"format_version\":1,\"rule_set_version\":\"0.0.1-inline\"}";

    private static string Entry(string id, string formula, string rdaPos, string rdaNeg) =>
        "{\"scaffold_id\":\"" + id + "\",\"name\":\"" + id + "\",\"formula\":\"" + formula + "\",\"class\":\"flavone\","
        + "\"ions\":{\"rda\":{\"pos\":[\"" + rdaPos + "\"],\"neg\":[\"" + rdaNeg + "\"]}}," + Src + "}";

    /// <summary>scaffolds/aglycones.json: genistein first, with apigenin's formula, so enumeration order and id order
    /// differ; quercetin has two negative rda ions.</summary>
    public static readonly string AglyconesJson =
        "{\"list_id\":\"aglycones\",\"entries\":[" + Entry("genistein", "C15H10O5", "C7H5O4", "C7H3O4") + ","
        + Entry("apigenin", "C15H10O5", "C7H5O4", "C7H3O4") + ","
        + "{\"scaffold_id\":\"quercetin\",\"name\":\"quercetin\",\"formula\":\"C15H10O7\",\"class\":\"flavonol\","
        + "\"ions\":{\"rda\":{\"pos\":[\"C7H5O4\"],\"neg\":[\"C8H5O4\",\"C7H3O4\"]}}," + Src + "}]}";

    public Dictionary<string, JsonValue> Docs { get; } = new Dictionary<string, JsonValue>(StringComparer.Ordinal);

    public InlineRuleSet() {
        Docs["ruleset.json"] = JsonEdit.Parse(RulesetJson);
        Docs["tables/elements.json"] = JsonEdit.Parse(ElementsJson);
        Docs["tables/adducts.json"] = JsonEdit.Parse(AdductsJson);
        Docs["tables/units.json"] = JsonEdit.Parse(UnitsJson);
        Docs["tables/motifs.json"] = JsonEdit.Parse(MotifsJson);
        Docs["tables/motif_nesting.json"] = JsonEdit.Parse(NestingJson);
        Docs["scaffolds/aglycones.json"] = JsonEdit.Parse(AglyconesJson);
    }

    /// <summary>A term object; <paramref name="fields"/> holds the kind fields.</summary>
    public static string Term(string id, string kind, string role, string fields, string floor = "0.05") =>
        "{\"term_id\":\"" + id + "\",\"kind\":\"" + kind + "\",\"role\":\"" + role + "\",\"term_source\":\"src:0\",\"min_rel_intensity\":" + floor + ","
        + "\"floor_source\":\"src:0\",\"than\":null,\"ratio\":null,\"ratio_source\":null,\"note\":null," + fields + "}";

    public static string Formula(string formula) => "\"formula\":\"" + formula + "\"";

    public static string UnitClass(string cls, string? plus = null, string? minus = null) =>
        "\"unit\":{\"class\":\"" + cls + "\",\"plus\":" + Q(plus) + ",\"minus\":" + Q(minus) + "}";

    public static string UnitName(string name, string? plus = null, string? minus = null) =>
        "\"unit\":{\"name\":\"" + name + "\",\"plus\":" + Q(plus) + ",\"minus\":" + Q(minus) + "}";

    public static string Aglycone(string form, string carrier, string? plus = null, string? minus = null) =>
        "\"form\":\"" + form + "\",\"charge_carrier\":\"" + carrier + "\",\"plus\":" + Q(plus) + ",\"minus\":" + Q(minus);

    public static string Series(string classes, string form = "even", string carrier = "proton", string? plus = null, string? minus = null) =>
        "\"unit_classes\":[" + classes + "]," + Aglycone(form, carrier, plus, minus);

    public static string Window(string formula, string asWhat, string halfWidth) =>
        "\"formula\":\"" + formula + "\",\"as\":\"" + asWhat + "\",\"half_width_da\":" + halfWidth;

    /// <summary>A composition: core (or null), scaffold list (or null), units and unit_totals objects.</summary>
    public static string Composition(string? core, string? scaffoldList, string units, string totals) =>
        "{\"core\":" + Q(core) + ",\"scaffold_list\":" + Q(scaffoldList) + ",\"units\":" + units + ",\"unit_totals\":" + totals + "}";

    /// <summary>{"all":[term, ..., not(veto), ...]}: every support term required, every veto under a not.</summary>
    public static string AllOf(IEnumerable<string> support, IEnumerable<string>? vetoes = null) {
        var items = support.Select(id => "{\"term\":\"" + id + "\"}").ToList();
        items.AddRange((vetoes ?? Enumerable.Empty<string>()).Select(id => "{\"not\":{\"term\":\"" + id + "\"}}"));
        return "{\"all\":[" + string.Join(",", items) + "]}";
    }

    /// <summary>Adds rules/&lt;ruleId&gt;.json from the template.</summary>
    public InlineRuleSet AddRule(string ruleId, string terms, string logic, string composition, string adducts, string claim) {
        var mode = ruleId.EndsWith(".pos", StringComparison.Ordinal) ? "pos" : "neg";
        var doc =
            "{\"format\":\"msfinder-diagnostic-rule\",\"format_version\":1,\"rule_id\":\"" + ruleId + "\",\"version\":1,"
            + "\"source\":[{\"doi\":\"10.1000/synthetic.1\",\"locator\":\"Table 1\"}],\"transcribed_by\":\"synthetic\","
            + "\"source_checked\":false,\"author_reviewed\":false,\"measure\":false,\"review\":null,\"parameters_origin\":\"as_published\","
            + "\"primary_precondition_mode\":\"adduct_ambiguous\",\"calibrated_on\":[\"QTOF-type\"],\"convention\":\"p2\",\"product_tolerance\":\"p2\","
            + "\"loss_reference\":\"hypothesis\",\"veto_excludes_motif\":false,"
            + "\"claim\":{\"motif_id\":\"flavonoid_o_glycoside\",\"claim_term_ids\":[\"" + claim + "\"],\"claim_level\":\"class\",\"non_claims\":[\"synthetic\"],"
            + "\"positive_examples\":[{\"pubchem_cid\":1,\"smiles\":\"C\"},{\"pubchem_cid\":2,\"smiles\":\"CC\"}],\"negative_examples\":[{\"pubchem_cid\":3,\"smiles\":\"CCC\"}]},"
            + "\"preconditions\":{\"ion_mode\":\"" + mode + "\",\"adducts\":" + adducts + ",\"precursor_tolerance_da\":0.01,\"composition\":" + composition
            + ",\"priors\":[],\"competitors\":[]},"
            + "\"terms\":" + terms + ",\"complementary\":[],\"logic\":" + logic + ",\"notes\":\"synthetic\"}";
        Docs["rules/" + ruleId + ".json"] = JsonEdit.Parse(doc);
        return this;
    }

    /// <summary>Sets one field of a document (a '/'-separated path of keys and indices).</summary>
    public InlineRuleSet Set(string file, string path, string json) {
        Docs[file] = JsonEdit.Set(Docs[file], path, JsonEdit.Parse(json));
        return this;
    }

    public InlineRuleSet SetRule(string ruleId, string path, string json) => Set("rules/" + ruleId + ".json", path, json);

    public Dictionary<string, byte[]> Files() => Docs.ToDictionary(kv => kv.Key, kv => PrettyJson.ToBytes(kv.Value), StringComparer.Ordinal);

    /// <summary>Loads the set unpinned (<see cref="RuleSetSource.Inline"/>); throws when the loader refuses it.</summary>
    public DiagnosticRuleSet Load() => DiagnosticRuleSet.LoadFiles(Files(), RuleSetSource.Inline);

    /// <summary>The hypothesis index of one rule of a loaded set.</summary>
    public static HypothesisIndex Index(DiagnosticRuleSet set, string ruleId) {
        for (var i = 0; i < set.Rules.Count; i++) {
            if (set.Rules[i].RuleId == ruleId) {
                return new HypothesisIndex(set.CompiledRules[i], set.Tables);
            }
        }
        throw new ArgumentException("no rule " + ruleId);
    }
}

/// <summary>An independent test-side mass oracle: element masses as pDa literals, Hill-free formula parsing.</summary>
internal static class TestMass
{
    public const long Electron = 549000000L;

    private static readonly Dictionary<string, long> Pda = new Dictionary<string, long>(StringComparer.Ordinal)
    {
        ["C"] = 12000000000000L, ["Cl"] = 34968852680000L, ["H"] = 1007825032000L, ["K"] = 38963706680000L,
        ["N"] = 14003074000000L, ["Na"] = 22989769280000L, ["O"] = 15994914620000L, ["P"] = 30973761630000L,
        ["S"] = 31972071000000L,
    };

    /// <summary>The mass of a formula such as C6H10O5 in pDa.</summary>
    public static long Of(string formula) {
        long total = 0;
        var i = 0;
        while (i < formula.Length) {
            var j = i + 1;
            while (j < formula.Length && char.IsLower(formula[j])) {
                j++;
            }
            var symbol = formula.Substring(i, j - i);
            var k = j;
            while (k < formula.Length && char.IsDigit(formula[k])) {
                k++;
            }
            var count = k == j ? 1 : long.Parse(formula.Substring(j, k - j), CultureInfo.InvariantCulture);
            total += count * Pda[symbol];
            i = k;
        }
        return total;
    }

    /// <summary>The m/z of an ion of charge z: (mass - z * e) / 1e12.</summary>
    public static double Ion(long massPda, int z) => (double)(massPda - z * Electron) / 1e12;

    public static double Da(long pda) => pda / 1e12;
}
