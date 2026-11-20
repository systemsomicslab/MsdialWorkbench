#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The ChemOnt nodes of the shipped motif table against the ontology they were taken from (RuleSet/README.md): every
/// id is a ChemOnt 2.1 term and carries that term's name, and every ancestor of a <c>compatible</c> class below the
/// kingdoms that is not itself <c>compatible</c> is <c>broader</c>. The ontology is not committed, so this runs only
/// when MSDIAL_CHEMONT_OBO names a local copy of <c>ChemOnt_2_1.obo</c>, whose sha256 must be the one the README
/// records; otherwise it is skipped. <see cref="MotifTests"/> checks the form of the nodes on every run.
/// </summary>
[TestClass]
public class ChemOntLocalTests
{
    public const string OboVariable = "MSDIAL_CHEMONT_OBO";

    /// <summary>The sha256 of ChemOnt_2_1.obo that RuleSet/README.md records.</summary>
    public const string OboSha256 = "8616a6ecb96c8aeb204739a4de045cd290eab9a0e164d0ceaa5d888d0751fe22";

    private const string IdTag = "id: ";
    private const string NameTag = "name: ";
    private const string IsATag = "is_a: ";

    /// <summary>The [Term] stanzas of an OBO file: id to name and id to its is_a parents.</summary>
    internal static (Dictionary<string, string> Names, Dictionary<string, List<string>> Parents) ReadObo(IEnumerable<string> lines) {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var parents = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        string? id = null;
        var inTerm = false;
        foreach (var raw in lines) {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("[", StringComparison.Ordinal)) {
                inTerm = line == "[Term]";
                id = null;
                continue;
            }
            if (!inTerm) {
                continue;
            }
            if (line.StartsWith(IdTag, StringComparison.Ordinal)) {
                id = line.Substring(IdTag.Length).Trim();
                parents[id] = new List<string>();
            }
            else if (id != null && line.StartsWith(NameTag, StringComparison.Ordinal)) {
                names[id] = line.Substring(NameTag.Length).Trim();
            }
            else if (id != null && line.StartsWith(IsATag, StringComparison.Ordinal)) {
                parents[id].Add(line.Substring(IsATag.Length).Trim().Split(' ')[0]);
            }
        }
        return (names, parents);
    }

    internal static HashSet<string> Ancestors(string id, Dictionary<string, List<string>> parents) {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>(parents.TryGetValue(id, out var ps) ? ps : new List<string>());
        while (stack.Count > 0) {
            var p = stack.Pop();
            if (seen.Add(p) && parents.TryGetValue(p, out var pp)) {
                foreach (var x in pp) {
                    stack.Push(x);
                }
            }
        }
        return seen;
    }

    /// <summary>The problems of the motif table's ChemOnt nodes against an ontology, in table order.</summary>
    internal static List<string> Problems(IEnumerable<JsonObject> motifs, Dictionary<string, string> names, Dictionary<string, List<string>> parents) {
        // The root has no parent and the kingdoms are its children; the closure stops below them.
        var top = new HashSet<string>(parents.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key), StringComparer.Ordinal);
        top.UnionWith(parents.Where(kv => kv.Value.Any(top.Contains)).Select(kv => kv.Key).ToList());
        var problems = new List<string>();
        foreach (var m in motifs) {
            var motif = ((JsonString)m["motif_id"]).Value;
            var chemont = (JsonObject)m["chemont"];
            List<(string Id, string Name)> Nodes(string list) =>
                ((JsonArray)chemont[list]).Cast<JsonObject>().Select(n => (((JsonString)n["id"]).Value, ((JsonString)n["name"]).Value)).ToList();
            var compatible = Nodes("compatible");
            var broader = Nodes("broader");
            foreach (var (id, name) in compatible.Concat(broader)) {
                if (!names.TryGetValue(id, out var expected)) {
                    problems.Add(motif + ": " + id + " is not a ChemOnt term");
                }
                else if (expected != name) {
                    problems.Add(motif + ": " + id + " is named " + expected + ", not " + name);
                }
            }
            var listed = new HashSet<string>(compatible.Select(n => n.Id).Concat(broader.Select(n => n.Id)), StringComparer.Ordinal);
            var missing = compatible.SelectMany(n => Ancestors(n.Id, parents)).Where(a => !top.Contains(a) && !listed.Contains(a))
                                    .Distinct(StringComparer.Ordinal).OrderBy(a => a, StringComparer.Ordinal);
            problems.AddRange(missing.Select(a => motif + ": ancestor " + a + " of a compatible class is not broader"));
        }
        return problems;
    }

    [TestMethod]
    public void EveryChemOntNodeIsAChemOnt21TermWithItsNameAndTheAncestorsAreClosed() {
        var path = Environment.GetEnvironmentVariable(OboVariable);
        if (string.IsNullOrEmpty(path)) {
            Assert.Inconclusive(OboVariable + " is not set (local check against ChemOnt_2_1.obo only)");
        }
        var bytes = File.ReadAllBytes(path!);
        using (var sha = SHA256.Create()) {
            Assert.AreEqual(OboSha256, string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture))),
                            "not the ChemOnt_2_1.obo the README records");
        }
        var (names, parents) = ReadObo(System.Text.Encoding.UTF8.GetString(bytes).Split('\n'));
        Assert.IsTrue(names.Count > 4000, "too few terms read: " + names.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var motifs = ((JsonArray)StrictJsonReader.Read(DiagnosticRuleSetLoader.ReadEmbedded()[DiagnosticRuleSetLoader.MotifsFile])["motifs"]).Cast<JsonObject>().ToList();
        var problems = Problems(motifs, names, parents);
        Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
    }

    [TestMethod]
    public void TheChemOntOntologyCheckFindsEachDefect() {
        var obo = new[] {
            "format-version: 1.2", "", "[Term]", "id: R:0", "name: Root", "", "[Term]", "id: K:1", "name: Kingdom", "is_a: R:0 ! Root",
            "", "[Term]", "id: S:2", "name: Super", "is_a: K:1 ! Kingdom", "", "[Term]", "id: C:3", "name: Class", "is_a: S:2 ! Super",
            "", "[Typedef]", "id: X:9", "name: not a term",
        };
        var (names, parents) = ReadObo(obo);
        Assert.AreEqual("K:1 R:0 S:2", string.Join(" ", Ancestors("C:3", parents).OrderBy(a => a, StringComparer.Ordinal)));
        Assert.IsFalse(names.ContainsKey("X:9"));
        JsonObject Row(string compatible, string broader) =>
            (JsonObject)JsonEdit.Parse("{\"motif_id\":\"m\",\"chemont\":{\"compatible\":[" + compatible + "],\"broader\":[" + broader + "]}}");
        string Check(string compatible, string broader) => string.Join("\n", Problems(new[] { Row(compatible, broader) }, names, parents));
        Assert.AreEqual("", Check("{\"id\":\"C:3\",\"name\":\"Class\"}", "{\"id\":\"S:2\",\"name\":\"Super\"}"));
        StringAssert.Contains(Check("{\"id\":\"C:3\",\"name\":\"Class\"}", ""), "ancestor S:2");
        StringAssert.Contains(Check("{\"id\":\"C:3\",\"name\":\"Klass\"}", "{\"id\":\"S:2\",\"name\":\"Super\"}"), "is named Class");
        StringAssert.Contains(Check("{\"id\":\"C:4\",\"name\":\"Class\"}", ""), "not a ChemOnt term");
    }
}
