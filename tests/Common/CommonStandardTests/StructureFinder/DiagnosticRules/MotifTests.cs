#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCDK.SMARTS;
using NCDK.Smiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The shipped motif tables (design 3.4, 7.1): every SMARTS parses with NCDK, every rule's PubChem positive examples
/// match its claim motif in NCDK and its negative examples do not (P3-A7), each motif no rule claims (competitors and
/// nesting-only motifs) matches a listed positive and misses a listed negative example, every nesting row names two
/// existing, distinct motifs, the nesting graph is acyclic and the positive examples of a child motif match the parent
/// motif in NCDK, and every ChemOnt field is well formed, sorted, and closed over the nesting (a child's compatible
/// classes are compatible or broader for its parent). Read from the raw embedded JSON, so the checks do not depend on
/// the loader's model.
/// <para>
/// Deferred, not done: design 7.1 also has this class run the cross-toolkit fixture (SMILES, motif and the RDKit
/// boolean, generated under D5) and fail on an NCDK/RDKit disagreement not listed with a reason. That fixture is
/// generated with RDKit by the Python port in msdial_spectrum_catalog (D5), which had no RDKit run behind it when the
/// 0.1.0 set was shipped, so it is not committed here and NCDK/RDKit disagreements are not checked yet. When a copy is
/// committed under the fixture folder, this class is to read it.
/// </para>
/// </summary>
[TestClass]
public class MotifTests
{
    /// <summary>
    /// NCDK verdicts on a shipped example that disagree with its role, as "rule_id smiles_role cid: reason". Each is a
    /// finding for the author, never a reason to edit a rule or motif here; the test fails on any disagreement not
    /// listed and on any listed one that has gone away.
    /// </summary>
    private static readonly string[] KnownExampleDisagreements = new string[0];

    private static readonly Regex ChemOntId = new Regex(@"^CHEMONTID:[0-9]{7}$");

    private static JsonObject Doc(string path) => StrictJsonReader.Read(DiagnosticRuleSetLoader.ReadEmbedded()[path]);

    private static List<JsonObject> Motifs() => ((JsonArray)Doc(DiagnosticRuleSetLoader.MotifsFile)["motifs"]).Cast<JsonObject>().ToList();

    private static List<JsonObject> Nesting() => ((JsonArray)Doc(DiagnosticRuleSetLoader.NestingFile)["nesting"]).Cast<JsonObject>().ToList();

    private static string Str(JsonObject o, string key) => ((JsonString)o[key]).Value;

    [TestMethod]
    public void EverySmartsParsesWithNcdk() {
        var failures = new List<string>();
        foreach (var m in Motifs()) {
            try {
                SmartsPattern.Create(Str(m, "smarts"));
            }
            catch (Exception e) {
                failures.Add(Str(m, "motif_id") + ": " + e.Message);
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
        Assert.AreEqual(Motifs().Count, Motifs().Select(m => Str(m, "motif_id")).Distinct(StringComparer.Ordinal).Count(), "a motif id repeats");
    }

    [TestMethod]
    public void EveryRuleExampleMatchesItsMotifInNcdkAsItsRoleSays() {
        var smarts = Motifs().ToDictionary(m => Str(m, "motif_id"), m => Str(m, "smarts"), StringComparer.Ordinal);
        var parser = new SmilesParser();
        var found = new List<string>();
        var checkedExamples = 0;
        foreach (var path in DiagnosticRuleSetLoader.ReadEmbedded().Keys.Where(k => k.StartsWith("rules/", StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal)) {
            var rule = Doc(path);
            var claim = (JsonObject)rule["claim"];
            var motif = Str(claim, "motif_id");
            foreach (var (key, expected) in new[] { ("positive_examples", true), ("negative_examples", false) }) {
                foreach (var example in ((JsonArray)claim[key]).Cast<JsonObject>()) {
                    var cid = ((JsonInteger)example["pubchem_cid"]).Value;
                    var label = Str(rule, "rule_id") + " " + key.Substring(0, key.IndexOf('_')) + " " + cid.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    bool matched;
                    try {
                        // A fresh pattern per molecule: SmartsPattern prepares (rings, Daylight aromaticity) each target.
                        matched = SmartsPattern.Create(smarts[motif]).Matches(parser.ParseSmiles(Str(example, "smiles")));
                    }
                    catch (Exception e) {
                        found.Add(label + ": " + e.GetType().Name);
                        continue;
                    }
                    checkedExamples++;
                    if (matched != expected) {
                        found.Add(label + ": NCDK " + (matched ? "matches" : "does not match") + " motif " + motif);
                    }
                }
            }
        }
        Assert.IsTrue(checkedExamples > 0);
        var known = new HashSet<string>(KnownExampleDisagreements.Select(k => k.Substring(0, k.IndexOf(':'))), StringComparer.Ordinal);
        var unexpected = found.Where(f => !known.Contains(f.Substring(0, f.IndexOf(':')))).ToList();
        var gone = known.Where(k => !found.Any(f => f.StartsWith(k + ":", StringComparison.Ordinal))).ToList();
        Assert.AreEqual(0, unexpected.Count, string.Join("\n", unexpected));
        Assert.AreEqual(0, gone.Count, "listed disagreements that no longer occur:\n" + string.Join("\n", gone));
    }

    [TestMethod]
    public void EveryNestingRowNamesTwoDistinctExistingMotifs() {
        var ids = new HashSet<string>(Motifs().Select(m => Str(m, "motif_id")), StringComparer.Ordinal);
        var rows = Nesting();
        foreach (var r in rows) {
            Assert.IsTrue(ids.Contains(Str(r, "child")), "unknown child " + Str(r, "child"));
            Assert.IsTrue(ids.Contains(Str(r, "parent")), "unknown parent " + Str(r, "parent"));
            Assert.AreNotEqual(Str(r, "child"), Str(r, "parent"));
            Assert.IsTrue(((JsonArray)r["source"]).Count > 0, Str(r, "child") + " < " + Str(r, "parent") + " has no source");
        }
        Assert.AreEqual(rows.Count, rows.Select(r => Str(r, "child") + "\n" + Str(r, "parent")).Distinct(StringComparer.Ordinal).Count(), "a nesting row repeats");
    }

    [TestMethod]
    public void TheNestingGraphIsAcyclic() {
        var parents = Nesting().GroupBy(r => Str(r, "child"), StringComparer.Ordinal)
                               .ToDictionary(g => g.Key, g => g.Select(r => Str(r, "parent")).OrderBy(p => p, StringComparer.Ordinal).ToList(), StringComparer.Ordinal);
        var state = new Dictionary<string, int>(StringComparer.Ordinal); // 1 on the path, 2 done
        string? Cycle(string node, List<string> path) {
            if (state.TryGetValue(node, out var s)) {
                return s == 1 ? string.Join(" < ", path.Skip(path.IndexOf(node)).Concat(new[] { node })) : null;
            }
            state[node] = 1;
            path.Add(node);
            foreach (var p in parents.TryGetValue(node, out var ps) ? ps : new List<string>()) {
                var c = Cycle(p, path);
                if (c != null) {
                    return c;
                }
            }
            path.RemoveAt(path.Count - 1);
            state[node] = 2;
            return null;
        }
        foreach (var child in parents.Keys.OrderBy(k => k, StringComparer.Ordinal)) {
            var cycle = Cycle(child, new List<string>());
            Assert.IsNull(cycle, "nesting cycle: " + cycle);
        }
    }

    /// <summary>
    /// NCDK verdicts that contradict a nesting row, as "child &lt; parent rule_id cid: reason": a positive example of
    /// a rule whose claim motif is the child that the parent motif does not match. Nesting is declared from sources and
    /// never inferred from SMARTS (design 3.4), so each entry is a finding for the author, never a reason to edit a
    /// motif or row here; the test fails on any contradiction not listed and on any listed one that has gone away.
    /// </summary>
    private static readonly string[] KnownNestingContradictions = new string[0];

    [TestMethod]
    public void EveryChildRulesPositiveExampleMatchesTheParentMotifInNcdk() {
        var smarts = Motifs().ToDictionary(m => Str(m, "motif_id"), m => Str(m, "smarts"), StringComparer.Ordinal);
        var rules = DiagnosticRuleSetLoader.ReadEmbedded().Keys.Where(k => k.StartsWith("rules/", StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal)
                                           .Select(Doc).ToList();
        var parser = new SmilesParser();
        var found = new List<string>();
        var checkedExamples = 0;
        foreach (var row in Nesting()) {
            var child = Str(row, "child");
            var parent = Str(row, "parent");
            foreach (var rule in rules.Where(r => Str((JsonObject)r["claim"], "motif_id") == child)) {
                foreach (var example in ((JsonArray)((JsonObject)rule["claim"])["positive_examples"]).Cast<JsonObject>()) {
                    var label = child + " < " + parent + " " + Str(rule, "rule_id") + " "
                                + ((JsonInteger)example["pubchem_cid"]).Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    bool matched;
                    try {
                        matched = SmartsPattern.Create(smarts[parent]).Matches(parser.ParseSmiles(Str(example, "smiles")));
                    }
                    catch (Exception e) {
                        found.Add(label + ": " + e.GetType().Name);
                        continue;
                    }
                    checkedExamples++;
                    if (!matched) {
                        found.Add(label + ": NCDK does not match the parent motif");
                    }
                }
            }
        }
        Assert.IsTrue(checkedExamples > 0);
        var known = new HashSet<string>(KnownNestingContradictions.Select(k => k.Substring(0, k.IndexOf(':'))), StringComparer.Ordinal);
        var unexpected = found.Where(f => !known.Contains(f.Substring(0, f.IndexOf(':')))).ToList();
        var gone = known.Where(k => !found.Any(f => f.StartsWith(k + ":", StringComparison.Ordinal))).ToList();
        Assert.AreEqual(0, unexpected.Count, string.Join("\n", unexpected));
        Assert.AreEqual(0, gone.Count, "listed contradictions that no longer occur:\n" + string.Join("\n", gone));
    }

    /// <summary>
    /// One checked example of a motif that no rule claims, which a rule names only as a competitor
    /// (<c>preconditions.competitors[].motif_id</c>) or which only a nesting row names. The example is a shipped PubChem
    /// example, referenced as "rule_id positive|negative cid" with its SMILES read from that rule, or a hand-written
    /// SMILES marked synthetic with the name of the compound it draws.
    /// </summary>
    private sealed class UnclaimedExample
    {
        public UnclaimedExample(string motifId, bool matches, string source, string? syntheticSmiles) {
            MotifId = motifId;
            Matches = matches;
            Source = source;
            SyntheticSmiles = syntheticSmiles;
        }

        public string MotifId { get; }
        public bool Matches { get; }
        public string Source { get; }
        public string? SyntheticSmiles { get; }
        public string Label => MotifId + " " + (Matches ? "+" : "-") + " " + (SyntheticSmiles is null ? Source : "synthetic " + Source);
    }

    private static UnclaimedExample Shipped(string motifId, bool matches, string example) => new UnclaimedExample(motifId, matches, example, null);

    private static UnclaimedExample Synthetic(string motifId, bool matches, string name, string smiles) => new UnclaimedExample(motifId, matches, name, smiles);

    /// <summary>
    /// At least one example that carries and one that does not carry each motif no rule claims (design 7.1 checks the
    /// claim motifs through the rules' own examples; these motifs have none). The runtime reports competitors as
    /// conflicts, so a competitor SMARTS that matches nothing, or everything, is caught here. Negatives are near
    /// misses where the shipped examples hold one: a positive example of a rule that lists the motif as a competitor.
    /// The verdicts are NCDK's at 0.1.0; a disagreement is a finding for the author, never a reason to edit a motif here.
    /// </summary>
    private static readonly UnclaimedExample[] UnclaimedMotifExamples =
    {
        Synthetic("alkyl_o_hexoside", true, "ethyl glucoside", "CCOC1OC(CO)C(O)C(O)C1O"),
        Shipped("alkyl_o_hexoside", false, "lignan.guaiacyl_hexoside.pos positive 486614"),
        Synthetic("anthraquinone_o_glycoside", true, "anthraquinone 1-O-glucoside", "O=C1c2ccccc2C(=O)c2c(OC3OC(CO)C(O)C(O)C3O)cccc12"),
        Shipped("anthraquinone_o_glycoside", false, "anthocyanin.anthocyanin.pos positive 443648"),
        Synthetic("anthraquinone_o_glycoside", false, "anthraquinone", "O=C1c2ccccc2C(=O)c2ccccc12"),
        Synthetic("anthrone_c_glycoside", true, "aloin", "OC[C@H]1O[C@@H]([C@H](O)[C@@H](O)[C@@H]1O)[C@H]1c2cccc(O)c2C(=O)c2c(O)cc(CO)cc12"),
        Shipped("anthrone_c_glycoside", false, "flavonoid_c_glycoside.mono_c_glycoside.neg positive 5280441"),
        Shipped("aryl_c_glycoside", true, "anthocyanin.anthocyanin.pos negative 5280441"),
        Shipped("aryl_c_glycoside", false, "anthocyanin.anthocyanin.pos positive 443648"),
        Synthetic("aryl_c_glycoside", false, "aloin", "OC[C@H]1O[C@@H]([C@H](O)[C@@H](O)[C@@H]1O)[C@H]1c2cccc(O)c2C(=O)c2c(O)cc(CO)cc12"),
        Synthetic("aryl_o_pyranoside", true, "p-coumaric acid 4-O-glucoside", "OC(=O)/C=C/c1ccc(OC2OC(CO)C(O)C(O)C2O)cc1"),
        Shipped("aryl_o_pyranoside", false, "flavonoid_c_glycoside.di_c_glycoside.neg positive 442664"),
        Synthetic("aryltetralin_lignan", true, "podophyllotoxin", "COc1cc(cc(OC)c1OC)[C@H]1[C@@H]2[C@H](COC2=O)[C@H](O)c2cc3OCOc3cc12"),
        Shipped("aryltetralin_lignan", false, "lignan.dibenzocyclooctadiene.pos positive 43595"),
        Synthetic("c_glycosyl_pyrimidine", true, "pseudouridine", "O=c1[nH]cc([C@@H]2O[C@H](CO)[C@@H](O)[C@H]2O)c(=O)[nH]1"),
        Synthetic("c_glycosyl_pyrimidine", false, "uridine", "O=c1ccn([C@@H]2O[C@H](CO)[C@@H](O)[C@H]2O)c(=O)[nH]1"),
        Shipped("c_glycosyl_pyrimidine", false, "nucleoside_nucleotide.nucleoside.neg positive 60961"),
        Synthetic("c_sulfonic_acid", true, "taurine", "NCCS(=O)(=O)O"),
        Shipped("c_sulfonic_acid", false, "sulfated_metabolite.aryl_sulfate.neg positive 4615423"),
        Synthetic("chalcone_core", true, "chalcone", "O=C(/C=C/c1ccccc1)c1ccccc1"),
        Synthetic("chalcone_core", true, "isoliquiritigenin", "O=C(/C=C/c1ccc(O)cc1)c1ccc(O)cc1O"),
        Synthetic("chalcone_core", false, "naringenin", "O=C1C[C@@H](c2ccc(O)cc2)Oc2cc(O)cc(O)c21"),
        Synthetic("cinchona_quinoline_quinuclidine", true, "quinine", "COc1ccc2nccc([C@@H](O)[C@@H]3C[C@@H]4CCN3C[C@@H]4C=C)c2c1"),
        Shipped("cinchona_quinoline_quinuclidine", false, "alkaloid_monoterpene_indole.indole_c10h10n_only.pos positive 8969"),
        Synthetic("dibenzylbutyrolactone_lignan", true, "matairesinol", "COc1cc(C[C@H]2COC(=O)[C@@H]2Cc2ccc(O)c(OC)c2)ccc1O"),
        Shipped("dibenzylbutyrolactone_lignan", false, "lignan.furofuran.neg positive 73399"),
        Synthetic("dihydrochalcone_core", true, "phloretin", "Oc1ccc(CCC(=O)c2c(O)cc(O)cc2O)cc1"),
        Shipped("dihydrochalcone_core", false, "flavonoid_aglycone.flavan_3_ol.pos positive 9064"),
        Synthetic("flavanone_core", true, "flavanone", "O=C1CC(c2ccccc2)Oc2ccccc12"),
        Synthetic("flavanone_core", true, "naringenin", "O=C1C[C@@H](c2ccc(O)cc2)Oc2cc(O)cc(O)c21"),
        Shipped("flavanone_core", false, "flavonoid_aglycone.dihydroflavonol.pos positive 439533"),
        Synthetic("flavanone_core", false, "isoliquiritigenin", "O=C(/C=C/c1ccc(O)cc1)c1ccc(O)cc1O"),
        Synthetic("gamma_glutamyl_amide", true, "glutathione", "N[C@@H](CCC(=O)N[C@@H](CS)C(=O)NCC(=O)O)C(=O)O"),
        Shipped("gamma_glutamyl_amide", false, "glutathione_conjugate.glutathionyl.neg negative 439498"),
        Shipped("hcaa_cinnamoyl_amide", true, "hydroxycinnamic_acid_amide.aminobutyl_polyamine.pos positive 6439562"),
        Shipped("hcaa_cinnamoyl_amide", false, "hydroxycinnamic_acid_amide.secondary_amide.neg negative 637542"),
        Synthetic("hcaa_cinnamoyl_amide", false, "p-coumaric acid", "OC(=O)/C=C/c1ccc(O)cc1"),
        Shipped("hcaa_cyclodopa_core", true, "hydroxycinnamic_acid_amide.oleracein.neg positive 21574472"),
        Synthetic("hcaa_cyclodopa_core", false, "L-DOPA", "N[C@@H](Cc1ccc(O)c(O)c1)C(=O)O"),
        Synthetic("hydroxycinnamic_acid_o_glycoside", true, "p-coumaric acid 4-O-glucoside", "OC(=O)/C=C/c1ccc(OC2OC(CO)C(O)C(O)C2O)cc1"),
        Shipped("hydroxycinnamic_acid_o_glycoside", false, "acylated_glycoside_ester.caffeoyl_hexose_ester.neg positive 5281761"),
        Synthetic("n_sulfamic_acid", true, "cyclamic acid", "OS(=O)(=O)NC1CCCCC1"),
        Shipped("n_sulfamic_acid", false, "sulfated_metabolite.organosulfate.neg positive 74426"),
        Synthetic("organophosphate_monoester", true, "glycerol 3-phosphate", "OCC(O)COP(=O)(O)O"),
        Synthetic("organophosphate_monoester", false, "diethyl phosphate", "CCOP(=O)(O)OCC"),
        Shipped("organophosphate_monoester", false, "sulfated_metabolite.alkyl_sulfate.neg positive 12594"),
        Synthetic("phenylcoumaran_neolignan", true, "dehydrodiconiferyl alcohol", "COc1cc(C2Oc3c(OC)cc(/C=C/CO)cc3C2CO)ccc1O"),
        Shipped("phenylcoumaran_neolignan", false, "lignan.furofuran.neg positive 73399"),
        Synthetic("phosphate_ester", true, "glycerol 3-phosphate", "OCC(O)COP(=O)(O)O"),
        Shipped("phosphate_ester", false, "glucosinolate.glucosinolate.neg positive 6911854"),
        Synthetic("pyrrolizidine_crotanecine_type", true, "crotanecine", "OCC1=CCN2CC(O)C(O)C12"),
        Shipped("pyrrolizidine_crotanecine_type", false, "alkaloid_pyrrolizidine.retronecine_n_oxide.pos positive 5380876"),
        Synthetic("pyrrolizidine_supinidine_type", true, "supinidine", "OCC1=CCN2CCCC12"),
        Shipped("pyrrolizidine_supinidine_type", false, "alkaloid_pyrrolizidine.platynecine_type.pos positive 5281742"),
        Synthetic("quebrachamine_skeleton", true, "quebrachamine", "CC[C@]12CCCN(C1)CCc1c([nH]c3ccccc13)CC2"),
        Shipped("quebrachamine_skeleton", false, "alkaloid_monoterpene_indole.indole_c10h10n_only.pos positive 8969"),
        Synthetic("quinolizidine_sparteine_type", true, "sparteine", "C1CCN2CC3CC(C2C1)CN1C3CCCC1"),
        Shipped("quinolizidine_sparteine_type", false, "alkaloid_quinolizidine.matrine_type.pos positive 91466"),
        Synthetic("steroid", true, "cholesterol", "CC(C)CCCC(C)C1CCC2C1(CCC3C2CC=C4C3(CCC(C4)O)C)C"),
        Shipped("steroid", false, "triterpenoid_saponin.triterpenoid_saponin.neg positive 122097"),
        Shipped("trimethylammonium", true, "acylcarnitine.acylcarnitine.pos positive 7045767"),
        Synthetic("trimethylammonium", true, "choline", "C[N+](C)(C)CCO"),
        Synthetic("trimethylammonium", false, "N,N-dimethylethanolamine", "CN(C)CCO"),
    };

    private static HashSet<string> ClaimedMotifs() =>
        new HashSet<string>(DiagnosticRuleSetLoader.ReadEmbedded().Keys.Where(k => k.StartsWith("rules/", StringComparison.Ordinal))
                                                  .Select(k => Str((JsonObject)Doc(k)["claim"], "motif_id")), StringComparer.Ordinal);

    /// <summary>The SMILES of an example: the synthetic one, or the shipped example the reference names.</summary>
    private static string ExampleSmiles(UnclaimedExample example) {
        if (example.SyntheticSmiles != null) {
            return example.SyntheticSmiles;
        }
        var parts = example.Source.Split(' ');
        Assert.AreEqual(3, parts.Length, example.Label);
        var path = "rules/" + parts[0] + ".json";
        Assert.IsTrue(DiagnosticRuleSetLoader.ReadEmbedded().ContainsKey(path), example.Label + ": no such rule");
        var list = (JsonArray)((JsonObject)Doc(path)["claim"])[parts[1] + "_examples"];
        var cid = long.Parse(parts[2], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);
        var found = list.Cast<JsonObject>().Where(e => ((JsonInteger)e["pubchem_cid"]).Value == cid).ToList();
        Assert.AreEqual(1, found.Count, example.Label + ": no such example");
        return Str(found[0], "smiles");
    }

    private static bool NcdkMatches(string smarts, string smiles) => SmartsPattern.Create(smarts).Matches(new SmilesParser().ParseSmiles(smiles));

    [TestMethod]
    public void EveryMotifNoRuleClaimsHasAPositiveAndANegativeExample() {
        var claimed = ClaimedMotifs();
        var unclaimed = Motifs().Select(m => Str(m, "motif_id")).Where(id => !claimed.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToList();
        Assert.IsTrue(unclaimed.Count > 0);
        CollectionAssert.AreEqual(unclaimed, UnclaimedMotifExamples.Select(e => e.MotifId).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToList(),
                                  "the examples must cover exactly the motifs no rule claims");
        var missing = unclaimed.Where(id => !(UnclaimedMotifExamples.Any(e => e.MotifId == id && e.Matches) && UnclaimedMotifExamples.Any(e => e.MotifId == id && !e.Matches))).ToList();
        Assert.AreEqual(0, missing.Count, "without a positive and a negative example: " + string.Join(", ", missing));
        Assert.AreEqual(UnclaimedMotifExamples.Length, UnclaimedMotifExamples.Select(e => e.Label).Distinct(StringComparer.Ordinal).Count(), "an example repeats");
    }

    [TestMethod]
    public void EveryUnclaimedMotifExampleMatchesInNcdkAsListed() {
        var smarts = Motifs().ToDictionary(m => Str(m, "motif_id"), m => Str(m, "smarts"), StringComparer.Ordinal);
        var found = new List<string>();
        foreach (var example in UnclaimedMotifExamples) {
            var smiles = ExampleSmiles(example);
            bool matched;
            try {
                matched = NcdkMatches(smarts[example.MotifId], smiles);
            }
            catch (Exception e) {
                found.Add(example.Label + ": " + e.GetType().Name);
                continue;
            }
            if (matched != example.Matches) {
                found.Add(example.Label + ": NCDK " + (matched ? "matches" : "does not match"));
            }
        }
        Assert.AreEqual(0, found.Count, string.Join("\n", found));
    }

    /// <summary>The nesting rows whose child no rule claims, which
    /// <see cref="EveryChildRulesPositiveExampleMatchesTheParentMotifInNcdk"/> has no example for: every positive
    /// example of the child listed above matches the parent in NCDK.</summary>
    [TestMethod]
    public void EveryUnclaimedChildsPositiveExampleMatchesTheParentMotifInNcdk() {
        var smarts = Motifs().ToDictionary(m => Str(m, "motif_id"), m => Str(m, "smarts"), StringComparer.Ordinal);
        var claimed = ClaimedMotifs();
        var found = new List<string>();
        var checkedExamples = 0;
        foreach (var row in Nesting().Where(r => !claimed.Contains(Str(r, "child")))) {
            foreach (var example in UnclaimedMotifExamples.Where(e => e.MotifId == Str(row, "child") && e.Matches)) {
                checkedExamples++;
                if (!NcdkMatches(smarts[Str(row, "parent")], ExampleSmiles(example))) {
                    found.Add(example.Label + ": NCDK does not match the parent motif " + Str(row, "parent"));
                }
            }
        }
        Assert.IsTrue(checkedExamples > 0);
        Assert.AreEqual(0, found.Count, string.Join("\n", found));
    }

    /// <summary>A compound carrying a child motif carries its parent, so each class compatible with the child is
    /// compatible or broader for the parent: the by-name report never calls one compound <c>agree</c> for the child and
    /// <c>differs</c> for its parent.</summary>
    [TestMethod]
    public void EveryChildCompatibleClassIsListedForItsParent() {
        JsonObject ChemOnt(string id) => (JsonObject)Motifs().Single(m => Str(m, "motif_id") == id)["chemont"];
        IEnumerable<string> Ids(JsonObject c, string list) => ((JsonArray)c[list]).Cast<JsonObject>().Select(n => Str(n, "id"));
        var failures = new List<string>();
        foreach (var row in Nesting()) {
            var parent = ChemOnt(Str(row, "parent"));
            var listed = new HashSet<string>(Ids(parent, "compatible").Concat(Ids(parent, "broader")), StringComparer.Ordinal);
            failures.AddRange(Ids(ChemOnt(Str(row, "child")), "compatible").Where(i => !listed.Contains(i))
                                  .Select(i => Str(row, "child") + " < " + Str(row, "parent") + ": " + i));
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    /// <summary>The ChemOnt field of a motif row: exactly <c>compatible</c> and <c>broader</c>, each a list of
    /// <c>{id, name}</c> with an id <c>CHEMONTID:</c> plus seven digits and a trimmed printable ASCII name; no id twice
    /// in one row, one name per id across the table, and each list sorted by id ordinally. Returns the problems.</summary>
    internal static List<string> ChemOntProblems(IEnumerable<JsonObject> motifs) {
        var problems = new List<string>();
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var m in motifs) {
            var id = Str(m, "motif_id");
            if (!(m.TryGetValue("chemont", out var c) && c is JsonObject chemont)) {
                problems.Add(id + ": no chemont object");
                continue;
            }
            if (string.Join(" ", chemont.Keys.OrderBy(k => k, StringComparer.Ordinal)) != "broader compatible") {
                problems.Add(id + ": chemont keys " + string.Join(",", chemont.Keys));
                continue;
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var list in new[] { "compatible", "broader" }) {
                if (!(chemont[list] is JsonArray nodes)) {
                    problems.Add(id + ": chemont/" + list + " is not an array");
                    continue;
                }
                var ids = nodes.OfType<JsonObject>().Select(x => x.TryGetValue("id", out var v) && v is JsonString sv ? sv.Value : "").ToList();
                if (!ids.SequenceEqual(ids.OrderBy(x => x, StringComparer.Ordinal))) {
                    problems.Add(id + ": chemont/" + list + " is not sorted by id");
                }
                foreach (var n in nodes) {
                    if (!(n is JsonObject node) || string.Join(" ", node.Keys.OrderBy(k => k, StringComparer.Ordinal)) != "id name"
                        || !(node["id"] is JsonString nid) || !(node["name"] is JsonString nname)) {
                        problems.Add(id + ": chemont/" + list + " holds a node that is not {id, name}");
                        continue;
                    }
                    if (!ChemOntId.IsMatch(nid.Value)) {
                        problems.Add(id + ": chemont id " + nid.Value);
                    }
                    if (nname.Value.Length == 0 || nname.Value.Trim() != nname.Value || nname.Value.Any(ch => ch < 0x20 || ch > 0x7e)) {
                        problems.Add(id + ": chemont name of " + nid.Value);
                    }
                    if (!seen.Add(nid.Value)) {
                        problems.Add(id + ": chemont id " + nid.Value + " twice");
                    }
                    if (names.TryGetValue(nid.Value, out var other) && other != nname.Value) {
                        problems.Add(id + ": chemont id " + nid.Value + " named both " + other + " and " + nname.Value);
                    }
                    names[nid.Value] = nname.Value;
                }
            }
        }
        return problems;
    }

    [TestMethod]
    public void EveryChemOntFieldIsWellFormed() {
        var problems = ChemOntProblems(Motifs());
        Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
    }

    [TestMethod]
    public void TheChemOntCheckFindsEachDefect() {
        JsonObject Row(string chemont) => (JsonObject)JsonEdit.Parse("{\"motif_id\":\"m\",\"chemont\":" + chemont + "}");
        string Check(params string[] chemont) => string.Join("\n", ChemOntProblems(chemont.Select(Row)));
        Assert.AreEqual("", Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000000\",\"name\":\"Organic compounds\"}],\"broader\":[]}"));
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000002\",\"name\":\"x\"},{\"id\":\"CHEMONTID:0000001\",\"name\":\"y\"}],\"broader\":[]}"), "not sorted by id");
        StringAssert.Contains(Check("{\"compatible\":[]}"), "chemont keys");
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:1\",\"name\":\"x\"}],\"broader\":[]}"), "chemont id CHEMONTID:1");
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\" x\"}],\"broader\":[]}"), "chemont name");
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000001\"}],\"broader\":[]}"), "not {id, name}");
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\"x\"}],\"broader\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\"x\"}]}"), "twice");
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\"x\"}],\"broader\":[]}",
                                    "{\"compatible\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\"y\"}],\"broader\":[]}"), "named both");
    }
}
