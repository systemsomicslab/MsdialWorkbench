#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
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
using System.Numerics;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The fixtures of the shipped rule set (design 7.2), owned by MsdialWorkbench: <c>rules/&lt;rule_id&gt;.json</c> (the
/// constructive cases of <see cref="DiagnosticFixtureBuilder"/> with the C# canonical result bytes),
/// <c>masses.json</c> (every formula of the set and sampled hypothesis precursors, from the C# pDa arithmetic),
/// <c>rules/SOURCE.md</c> and <c>manifest.json</c>, which pins every fixture and the shipped set's version and
/// rule-set sha256. Each per-rule case runs on the shipped set restricted to that rule: every table, the rule's own
/// file and the scaffold lists it names, loaded as an inline set.
/// </summary>
internal static class ShippedRuleFixtures
{
    public const string RulesDirectory = "rules/";
    public const string MassesFile = "masses.json";
    public const string SourceFile = "rules/SOURCE.md";
    public const string RuleKind = "rule";
    public const string MassesKind = "masses";

    private static readonly Lazy<Dictionary<string, byte[]>> Embedded = new Lazy<Dictionary<string, byte[]>>(DiagnosticRuleSetLoader.ReadEmbedded);
    private static readonly ConcurrentDictionary<string, DiagnosticRuleSet> Sets = new ConcurrentDictionary<string, DiagnosticRuleSet>(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, NaiveRule> Naive = new ConcurrentDictionary<string, NaiveRule>(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, DiagnosticRuleEvaluator> Evaluators = new ConcurrentDictionary<string, DiagnosticRuleEvaluator>(StringComparer.Ordinal);

    private static KeyValuePair<string, JsonValue> M(string key, JsonValue value) => new KeyValuePair<string, JsonValue>(key, value);

    private static JsonObject Doc(string path) => StrictJsonReader.Read(Embedded.Value[path]);

    /// <summary>Every shipped rule id, ordinally.</summary>
    public static List<string> RuleIds() => Embedded.Value.Keys
        .Where(k => k.StartsWith(RulesDirectory, StringComparison.Ordinal))
        .Select(k => k.Substring(RulesDirectory.Length, k.Length - RulesDirectory.Length - ".json".Length))
        .OrderBy(k => k, StringComparer.Ordinal).ToList();

    public static string RuleFile(string ruleId) => RulesDirectory + ruleId + ".json";

    private static string? ScaffoldList(string ruleId) =>
        NaiveRule.StrOrNull((JsonObject)((JsonObject)Doc(RuleFile(ruleId))["preconditions"])["composition"], "scaffold_list");

    /// <summary>The shipped set restricted to one rule: every JSON file but the other rules and the scaffold lists the
    /// rule does not name (ruleset-history.tsv is not hashed and is left out).</summary>
    public static Dictionary<string, byte[]> RestrictedFiles(string ruleId) {
        var list = ScaffoldList(ruleId);
        return Embedded.Value.Where(kv => kv.Key.EndsWith(".json", StringComparison.Ordinal)
                                          && (kv.Key.StartsWith(RulesDirectory, StringComparison.Ordinal) ? kv.Key == RuleFile(ruleId)
                                              : !kv.Key.StartsWith("scaffolds/", StringComparison.Ordinal) || kv.Key == "scaffolds/" + list + ".json"))
                             .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
    }

    public static DiagnosticRuleSet RestrictedSet(string ruleId) =>
        Sets.GetOrAdd(ruleId, id => DiagnosticRuleSet.LoadFiles(RestrictedFiles(id), RuleSetSource.Inline));

    public static NaiveRule NaiveModel(string ruleId) => Naive.GetOrAdd(ruleId, id => {
        var list = ScaffoldList(id);
        return new NaiveRule(Doc(RuleFile(id)), Doc(DiagnosticRuleSetLoader.UnitsFile), Doc(DiagnosticRuleSetLoader.AdductsFile),
                             list is null ? null : Doc("scaffolds/" + list + ".json"));
    });

    /// <summary>The embedded set's version and rule-set sha256, read without the pin.</summary>
    public static (string Version, string Sha256) EmbeddedIdentity() {
        var hashed = Embedded.Value.Where(kv => kv.Key.EndsWith(".json", StringComparison.Ordinal)).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        return (((JsonString)Doc(DiagnosticRuleSetLoader.RulesetFile)["rule_set_version"]).Value, DiagnosticRuleSetLoader.RuleSetSha256(hashed));
    }

    /// <summary>The shipped set's version and sha256 as <c>manifest.json</c> pins them; Inconclusive when the fixtures
    /// are not deployed.</summary>
    public static (string Version, string Sha256) PinnedRuleSet() {
        var path = DiagnosticRuleFixtures.FixturePath(EngineFixtures.ManifestFile);
        if (!File.Exists(path)) {
            Assert.Inconclusive("manifest.json is not deployed");
        }
        var pin = (JsonObject)StrictJsonReader.Read(File.ReadAllBytes(path))["rule_set"];
        return (((JsonString)pin["version"]).Value, ((JsonString)pin["sha256"]).Value);
    }

    // ------------------------------------------------------------------------------------------------ rule cases

    /// <summary>Evaluates a per-rule case on the restricted set; evaluators are shared per rule and option spec.</summary>
    public static DiagnosticRuleResult Run(string ruleId, JsonObject caseObject) {
        var options = (JsonObject)caseObject["options"];
        var evaluator = Evaluators.GetOrAdd(ruleId + "\n" + CanonicalJson.ToText(options),
                                            _ => new DiagnosticRuleEvaluator(RestrictedSet(ruleId), EngineFixtures.Options(options)));
        var input = (JsonObject)caseObject["input"];
        var evidence = EngineFixtures.Evidence(input);
        var spectrum = EngineFixtures.Spectrum(input);
        return evidence is null ? evaluator.Evaluate(spectrum) : evaluator.Evaluate(spectrum, evidence);
    }

    public static JsonObject CaseObject(RuleFixtureCase c) => new JsonObject(new[]
    {
        M("description", JsonValue.Of(c.Description)),
        M("expect", c.Expect),
        M("input", c.Input),
        M("name", JsonValue.Of(c.Name)),
        M("options", c.Options),
    });

    public static JsonObject Record(string ruleId, RuleFixtureCase c) {
        var input = CaseObject(c);
        var text = DiagnosticResultJson.Write(Run(ruleId, input));
        var members = input.Members.ToList();
        members.Add(M("result", JsonValue.Of(text)));
        members.Add(M("result_sha256", JsonValue.Of(Sha256Hex.Of(Encoding.ASCII.GetBytes(text)))));
        return new JsonObject(members);
    }

    private static JsonObject Document(string kind, string version, IEnumerable<JsonValue> cases) => new JsonObject(new[]
    {
        M("cases", new JsonArray(cases)),
        M("engine_semantics", JsonValue.Of(DiagnosticRuleEvaluator.EngineSemantics)),
        M("format", JsonValue.Of(EngineFixtures.Format)),
        M("format_version", JsonValue.Of(1)),
        M("kind", JsonValue.Of(kind)),
        M("rule_set_sha256", JsonValue.Null),
        M("rule_set_version", JsonValue.Of(version)),
    });

    /// <summary>The fixture of one rule and the cases its builder left out (with the reason).</summary>
    public static (JsonObject Document, List<string> Skipped) RuleDocument(string ruleId, string version) {
        var builder = new DiagnosticFixtureBuilder(NaiveModel(ruleId));
        var cases = builder.Build();
        return (Document(RuleKind, version, cases.Select(c => (JsonValue)Record(ruleId, c))), builder.Skipped);
    }

    // ------------------------------------------------------------------------------------------------ masses

    /// <summary>An exact decimal of an integer number of pDa: 12 fraction digits with trailing zeros trimmed.</summary>
    public static string PdaText(BigInteger pda) {
        var negative = pda.Sign < 0;
        var a = BigInteger.Abs(pda);
        var frac = (a % ElementTable.PdaPerDa).ToString(CultureInfo.InvariantCulture).PadLeft(12, '0').TrimEnd('0');
        return (negative ? "-" : "") + (a / ElementTable.PdaPerDa).ToString(CultureInfo.InvariantCulture) + "." + (frac.Length == 0 ? "0" : frac);
    }

    /// <summary>Every formula of the shipped set with the files that hold it: term formulas, plus and minus, unit
    /// plus and minus and the composition core of each rule; each scaffold entry's formula and ion formulas; each unit
    /// residue; each adduct's add and remove.</summary>
    public static SortedDictionary<string, SortedSet<string>> SetFormulas() {
        var found = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        void Add(string file, JsonValue? v) {
            if (v is JsonString s) {
                if (!found.TryGetValue(s.Value, out var files)) {
                    found[s.Value] = files = new SortedSet<string>(StringComparer.Ordinal);
                }
                files.Add(file);
            }
        }
        JsonValue? Get(JsonObject o, string key) => o.TryGetValue(key, out var v) ? v : null;
        foreach (var path in Embedded.Value.Keys.Where(k => k.EndsWith(".json", StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal)) {
            var doc = Doc(path);
            if (path.StartsWith(RulesDirectory, StringComparison.Ordinal)) {
                foreach (var t in ((JsonArray)doc["terms"]).Cast<JsonObject>()) {
                    Add(path, Get(t, "formula"));
                    Add(path, Get(t, "plus"));
                    Add(path, Get(t, "minus"));
                    if (Get(t, "unit") is JsonObject unit) {
                        Add(path, Get(unit, "plus"));
                        Add(path, Get(unit, "minus"));
                    }
                }
                Add(path, ((JsonObject)((JsonObject)doc["preconditions"])["composition"])["core"]);
            }
            else if (path.StartsWith("scaffolds/", StringComparison.Ordinal)) {
                foreach (var e in ((JsonArray)doc["entries"]).Cast<JsonObject>()) {
                    Add(path, Get(e, "formula"));
                    foreach (var ions in ((JsonObject)e["ions"]).Members.Select(m => (JsonObject)m.Value)) {
                        foreach (var mode in new[] { "pos", "neg" }) {
                            foreach (var ion in (JsonArray)ions[mode]) {
                                Add(path, ion);
                            }
                        }
                    }
                }
            }
            else if (path == DiagnosticRuleSetLoader.UnitsFile) {
                foreach (var u in ((JsonArray)doc["units"]).Cast<JsonObject>()) {
                    Add(path, u["residue"]);
                }
            }
            else if (path == DiagnosticRuleSetLoader.AdductsFile) {
                foreach (var a in ((JsonArray)doc["adducts"]).Cast<JsonObject>()) {
                    Add(path, a["add"]);
                    Add(path, a["remove"]);
                }
            }
        }
        return found;
    }

    /// <summary>
    /// <c>masses.json</c>: a <c>formula</c> row per formula of the set (pDa, neutral mass, cation and anion m/z) and a
    /// <c>precursor</c> row per sampled hypothesis (for every rule and every tried adduct, the first and the last
    /// composition in enumeration order), all from the C# pDa arithmetic, as exact decimals of the pDa integers.
    /// </summary>
    public static JsonObject MassesDocument(string version) {
        var set = DiagnosticRuleSet.LoadFiles(Embedded.Value.Where(kv => kv.Key.EndsWith(".json", StringComparison.Ordinal))
                                                            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal), RuleSetSource.Inline);
        var el = set.Elements;
        var rows = new List<JsonValue>();
        foreach (var kv in SetFormulas()) {
            var pda = el.MassPdaExact(Composition.Parse(kv.Key, el));
            rows.Add(new JsonObject(new[]
            {
                M("anion_mz", JsonValue.Of(PdaText(pda + el.ElectronPdaExact))),
                M("cation_mz", JsonValue.Of(PdaText(pda - el.ElectronPdaExact))),
                M("files", new JsonArray(kv.Value.Select(JsonValue.Of))),
                M("formula", JsonValue.Of(kv.Key)),
                M("kind", JsonValue.Of("formula")),
                M("name", JsonValue.Of("formula:" + kv.Key)),
                M("neutral_mass", JsonValue.Of(PdaText(pda))),
                M("pda", JsonValue.Of((long)pda)),
            }));
        }
        for (var r = 0; r < set.Rules.Count; r++) {
            var index = new HypothesisIndex(set.CompiledRules[r], set.Tables);
            var compositions = new[] { 0, index.Compositions.Count - 1 }.Distinct().ToList();
            for (var a = 0; a < index.Tried.Count; a++) {
                foreach (var c in compositions) {
                    var pda = index.PrecursorPda(c, a);
                    var h = index.Hypothesis(c, a, ElementTable.ToDa(pda));
                    rows.Add(new JsonObject(new[]
                    {
                        M("adduct", JsonValue.Of(h.Adduct)),
                        M("hypothesis_id", JsonValue.Of(h.HypothesisId)),
                        M("kind", JsonValue.Of("precursor")),
                        M("mz", JsonValue.Of(PdaText(pda))),
                        M("name", JsonValue.Of("precursor:" + set.Rules[r].RuleId + ":" + h.HypothesisId)),
                        M("neutral_formula", JsonValue.Of(h.NeutralFormula)),
                        M("pda", JsonValue.Of(pda)),
                        M("rule_id", JsonValue.Of(set.Rules[r].RuleId)),
                    }));
                }
            }
        }
        return Document(MassesKind, version, rows);
    }

    // ------------------------------------------------------------------------------------------------ generation

    public static string SourceMd(int nRules, int nCases, int nMasses, IReadOnlyList<string> skipped) {
        const string NL = "\n";
        var sb = new StringBuilder();
        sb.Append("# Shipped rule-set fixtures").Append(NL).Append(NL)
          .Append("Owned by MsdialWorkbench (the C# engine is the oracle); the Python port copies this folder from a pinned commit.").Append(NL)
          .Append("Generated by `ShippedRuleFixtureTests` (CommonStandardTests) from the embedded rule set; every input is synthetic.").Append(NL)
          .Append("`../manifest.json` pins each file's sha256 and case count and, under `rule_set`, the version and rule-set").Append(NL)
          .Append("sha256 of the shipped set they were generated from. The files carry `rule_set_sha256: null` so that the manifest").Append(NL)
          .Append("is the one pin.").Append(NL).Append(NL)
          .Append("- `rules/<rule_id>.json`: ").Append(nRules.ToString(CultureInfo.InvariantCulture)).Append(" files, ")
          .Append(nCases.ToString(CultureInfo.InvariantCulture)).Append(" cases").Append(NL)
          .Append("- `../masses.json`: ").Append(nMasses.ToString(CultureInfo.InvariantCulture)).Append(" rows").Append(NL)
          .Append("- engine semantics: `").Append(DiagnosticRuleEvaluator.EngineSemantics).Append("`").Append(NL).Append(NL)
          .Append("Both are `{format, format_version, engine_semantics, rule_set_version, rule_set_sha256, kind, cases}` written with").Append(NL)
          .Append("indent 1, `ensure_ascii`, LF and a trailing newline.").Append(NL).Append(NL)
          .Append("## rules/<rule_id>.json").Append(NL).Append(NL)
          .Append("Each case runs on the shipped set restricted to that rule: every `tables/*.json`, `ruleset.json`,").Append(NL)
          .Append("`rules/<rule_id>.json` and the `scaffolds/<list_id>.json` the rule names, loaded unpinned as an inline set").Append(NL)
          .Append("(`ruleset-history.tsv` is not hashed and is left out). A case holds `name`, `description`, `options` (only the").Append(NL)
          .Append("non-default ones: every case has `filter: all`, and `wrong_mode` also `verbose: true` so that its outcome with no").Append(NL)
          .Append("hypothesis is listed), `input` (as in `../engine/semantics.json`), `expect`, `result` (the canonical result").Append(NL)
          .Append("text) and `result_sha256`.").Append(NL).Append(NL)
          .Append("The cases are built constructively by `DiagnosticFixtureBuilder`, independently of the engine: it enumerates").Append(NL)
          .Append("the rule's hypotheses with NCDK's BODR major-isotope masses (electron 0.000549), picks a hypothesis h* of the").Append(NL)
          .Append("rule's own adducts whose positive spectrum makes it the chosen hypothesis, places one peak at the oracle m/z of").Append(NL)
          .Append("every member of every support term of h* (outside the precursor region) plus the precursor peak, and derives:").Append(NL)
          .Append("`positive`; `drop_<term>` for each required term (on h*, or, when h* has no peak of the term, on the first").Append(NL)
          .Append("other hypothesis of the rule's adducts that places one and fires, named in the description; a peak that a").Append(NL)
          .Append("complementary partner shares goes for both terms, and the description names the other); `drop_any<n>` for").Append(NL)
          .Append("each `any` node (pre-order) with the peaks of every support term under it removed; `k_of<n>_at_k` and").Append(NL)
          .Append("`k_of<n>_at_k_minus_1` for each `k_of` node (pre-order); `veto_<term>` for each veto (with a member that").Append(NL)
          .Append("leaves h* chosen and not fired when one exists); `ordering_reversed_<term>`; `tolerance_{in,out}_{high,low}`").Append(NL)
          .Append("(the first required support peak at the product tolerance -/+ 1e-5 Da) and, when that target is below m/z").Append(NL)
          .Append("500, `tolerance_ppm_{in,out}_{high,low}` on the highest support target at or above m/z 500 (the 10 ppm").Append(NL)
          .Append("branch); `precursor_{in,out}_{high,low}` (the precursor at its tolerance -/+ 1e-5 Da); `wrong_mode`; `tie`").Append(NL)
          .Append("(two hypotheses that share a precursor, with the union of their peaks); `decoy_plus_5`. Rules with").Append(NL)
          .Append("`measure: false` are built alike. `expect` is the builder's naive").Append(NL)
          .Append("evaluation (fired, chosen hypothesis, matched `term_id:member_key@peak_index`, ties, `n_hypotheses`,").Append(NL)
          .Append("`precondition_failure`), checked against `result` by the test; `result` is the parity target.").Append(NL).Append(NL)
          .Append("Peaks are integer micro-Da and integer intensities written as decimal text, and sit at least 5e-6 Da from every").Append(NL)
          .Append("tolerance boundary, the precursor region and the loss constraint in the builder's model and at least 2e-6 Da in").Append(NL)
          .Append("the engine's (the test audits both).").Append(NL).Append(NL)
          .Append("## ../masses.json").Append(NL).Append(NL)
          .Append("C# output. A `formula` row per formula of the shipped set (term formulas, plus and minus, unit plus and minus,").Append(NL)
          .Append("composition cores, scaffold formulas and ion formulas, unit residues, adduct add and remove) with its integer").Append(NL)
          .Append("`pda`, `neutral_mass`, `cation_mz` (- e) and `anion_mz` (+ e) as exact decimals of pDa, and the files that").Append(NL)
          .Append("hold it. A `precursor` row per sampled hypothesis: for every rule and tried adduct, the first and the last").Append(NL)
          .Append("composition in enumeration order. The test checks every value against NCDK within 1e-5 Da; the catalog checks").Append(NL)
          .Append("them against RDKit.").Append(NL).Append(NL)
          .Append("## Cases left out").Append(NL).Append(NL)
          .Append("A case the builder cannot construct for a rule, or that cannot keep its margins, is left out:").Append(NL).Append(NL);
        foreach (var s in skipped) {
            sb.Append("- ").Append(s).Append(NL);
        }
        if (skipped.Count == 0) {
            sb.Append("- none").Append(NL);
        }
        sb.Append(NL).Append("## Not here yet").Append(NL).Append(NL)
          .Append("The cross-toolkit motif fixture of design 7.1 (SMILES, motif and the RDKit boolean, from the shipped rules'").Append(NL)
          .Append("PubChem examples plus marked synthetic SMILES) is generated with RDKit by the Python port and is not committed").Append(NL)
          .Append("with this set, so NCDK/RDKit motif disagreements are not checked yet; `MotifTests` checks NCDK only.").Append(NL);
        sb.Append(NL).Append("## Regenerate").Append(NL).Append(NL)
          .Append("Set `").Append(EngineFixtures.WriteVariable).Append("=<dir>` and run the CommonStandardTests filter `DiagnosticRules`; every").Append(NL)
          .Append("fixture of this folder's parent is written under `<dir>` and the regeneration tests end Inconclusive. Copy the").Append(NL)
          .Append("files here unchanged. A change to the shipped set (a new version and history row) needs only this run.").Append(NL);
        return sb.ToString();
    }

    /// <summary>
    /// Every shipped-set fixture by path relative to the fixture folder: the per-rule files, <c>masses.json</c>,
    /// <c>rules/SOURCE.md</c> and <c>manifest.json</c> (which also pins the engine fixtures of
    /// <see cref="EngineFixtures.Generate"/>).
    /// </summary>
    public static List<(string Path, byte[] Bytes)> Generate() {
        var (version, sha) = EmbeddedIdentity();
        var output = new List<(string Path, byte[] Bytes)>();
        var skipped = new List<string>();
        var nCases = 0;
        foreach (var id in RuleIds()) {
            var (doc, left) = RuleDocument(id, version);
            nCases += ((JsonArray)doc["cases"]).Count;
            output.Add((RuleFile(id), PrettyJson.ToBytes(doc)));
            skipped.AddRange(left.Select(s => id + " " + s));
        }
        var masses = MassesDocument(version);
        output.Add((MassesFile, PrettyJson.ToBytes(masses)));
        output.Add((SourceFile, Encoding.ASCII.GetBytes(SourceMd(RuleIds().Count, nCases, ((JsonArray)masses["cases"]).Count, skipped))));
        var pinned = EngineFixtures.Generate().Where(f => f.Path.EndsWith(".json", StringComparison.Ordinal))
                                              .Concat(output.Where(f => f.Path.EndsWith(".json", StringComparison.Ordinal)));
        output.Add((EngineFixtures.ManifestFile, PrettyJson.ToBytes(EngineFixtures.Manifest(pinned, version, sha))));
        return output;
    }
}
