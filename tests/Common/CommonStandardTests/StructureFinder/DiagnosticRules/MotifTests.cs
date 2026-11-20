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
/// match its claim motif in NCDK and its negative examples do not (P3-A7), every nesting row names two existing,
/// distinct motifs and the nesting graph is acyclic, and every ChemOnt field is well formed. Read from the raw
/// embedded JSON, so the checks do not depend on the loader's model.
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

    /// <summary>The ChemOnt field of a motif row: exactly <c>compatible</c> and <c>broader</c>, each a list of
    /// <c>{id, name}</c> with an id <c>CHEMONTID:</c> plus seven digits and a trimmed printable ASCII name; no id twice
    /// in one row, and one name per id across the table. Returns the problems.</summary>
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
        Assert.AreEqual("", Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\"Organic compounds\"}],\"broader\":[]}"));
        StringAssert.Contains(Check("{\"compatible\":[]}"), "chemont keys");
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:1\",\"name\":\"x\"}],\"broader\":[]}"), "chemont id CHEMONTID:1");
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\" x\"}],\"broader\":[]}"), "chemont name");
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000001\"}],\"broader\":[]}"), "not {id, name}");
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\"x\"}],\"broader\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\"x\"}]}"), "twice");
        StringAssert.Contains(Check("{\"compatible\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\"x\"}],\"broader\":[]}",
                                    "{\"compatible\":[{\"id\":\"CHEMONTID:0000001\",\"name\":\"y\"}],\"broader\":[]}"), "named both");
    }
}
