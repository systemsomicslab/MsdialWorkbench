#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.DiagnosticRuleFixtures;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Provenance of the committed fixtures (design 7.1, directive section 7): every SMILES in a fixture is one of the
/// shipped rules' PubChem examples or is marked synthetic; no fixture holds an InChI or InChIKey (there is no
/// committed (smiles, inchikey, inchi) table for one to belong to); and no fixture name or content names a local VS21
/// output or a VS20/VS21 record. Every fixture is read as text, as decoded JSON strings, and through the rule-set file
/// maps it carries as base64 (the loader-error cases and vectors).
/// </summary>
[TestClass]
[DeploymentItem(@"Resources\DiagnosticRuleFixtures", @"Resources\DiagnosticRuleFixtures")]
public class FixtureProvenanceLintTests
{
    /// <summary>Hand-written synthetic SMILES of the inline test rule sets (<see cref="InlineRuleSet"/>), which only fill
    /// the claim examples a rule file must carry.</summary>
    private static readonly string[] SyntheticSmiles = { "C", "CC", "CCC" };

    private static readonly Regex VsRecord = new Regex("vs2[01]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>The origin value tuned_vs20 (design 3.4) names a tuning, not a record.</summary>
    private const string OriginToken = "tuned_vs20";

    /// <summary>Every JSON document of a fixture: the file and each base64 string that decodes to a strict JSON
    /// object (a rule-set file of a file map), recursively.</summary>
    private static IEnumerable<(string Where, JsonValue Doc)> Documents(string file, byte[] bytes) {
        JsonValue root;
        try {
            root = StrictJsonReader.Read(bytes);
        }
        catch (StrictJsonException) {
            yield break;
        }
        yield return (file, root);
        foreach (var s in EmbeddedRuleSetTests.DecodedStrings(root).Where(s => s.Length >= 8 && s.Length % 4 == 0 && (s.StartsWith("ey", StringComparison.Ordinal) || s.StartsWith("ew", StringComparison.Ordinal)))) {
            byte[] decoded;
            try {
                decoded = Convert.FromBase64String(s);
            }
            catch (FormatException) {
                continue;
            }
            if (decoded.Length == 0 || decoded[0] != (byte)'{') {
                continue;
            }
            foreach (var d in Documents(file + " (base64)", decoded)) {
                yield return d;
            }
        }
    }

    private static IEnumerable<(string File, string Where, JsonValue Doc)> AllDocuments() {
        foreach (var f in RuleSetLintTests.FixtureFiles().Where(f => f.EndsWith(".json", StringComparison.Ordinal))) {
            foreach (var (where, doc) in Documents(f, File.ReadAllBytes(FixturePath(f)))) {
                yield return (f, where, doc);
            }
        }
    }

    /// <summary>The values under every key named <paramref name="key"/> (any case), with whether the object holding it
    /// is marked <c>"synthetic": true</c>.</summary>
    private static IEnumerable<(string Value, bool Synthetic)> Values(JsonValue node, string key) {
        switch (node) {
            case JsonObject o:
                var synthetic = o.TryGetValue("synthetic", out var s) && s is JsonBoolean b && b.Value;
                foreach (var m in o.Members) {
                    if (string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase) && m.Value is JsonString v) {
                        yield return (v.Value, synthetic);
                    }
                    foreach (var x in Values(m.Value, key)) {
                        yield return x;
                    }
                }
                break;
            case JsonArray a:
                foreach (var x in a.SelectMany(i => Values(i, key))) {
                    yield return x;
                }
                break;
        }
    }

    private static HashSet<string> ShippedExampleSmiles() {
        var smiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var kv in DiagnosticRuleSetLoader.ReadEmbedded().Where(kv => kv.Key.StartsWith("rules/", StringComparison.Ordinal))) {
            var claim = (JsonObject)StrictJsonReader.Read(kv.Value)["claim"];
            foreach (var list in new[] { "positive_examples", "negative_examples" }) {
                foreach (var e in ((JsonArray)claim[list]).Cast<JsonObject>()) {
                    smiles.Add(((JsonString)e["smiles"]).Value);
                }
            }
        }
        return smiles;
    }

    [TestMethod]
    public void EverySmilesInAFixtureIsAShippedExampleOrSynthetic() {
        var allowed = ShippedExampleSmiles();
        allowed.UnionWith(SyntheticSmiles);
        var failures = new List<string>();
        var seen = 0;
        foreach (var (_, where, doc) in AllDocuments()) {
            foreach (var (smiles, synthetic) in Values(doc, "smiles")) {
                seen++;
                if (!synthetic && !allowed.Contains(smiles)) {
                    failures.Add(where + ": " + smiles);
                }
            }
        }
        Assert.IsTrue(seen > 0, "no SMILES found: the base64 file maps were not read");
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures.Distinct().Take(20)));
    }

    [TestMethod]
    public void NoFixtureHoldsAnInChIOrInChIKey() {
        var failures = new List<string>();
        foreach (var (_, where, doc) in AllDocuments()) {
            foreach (var key in new[] { "inchi", "inchikey" }) {
                if (Values(doc, key).Any()) {
                    failures.Add(where + ": a " + key + " field");
                }
            }
            if (EmbeddedRuleSetTests.DecodedStrings(doc).Any(s => s.IndexOf("InChI=", StringComparison.OrdinalIgnoreCase) >= 0)) {
                failures.Add(where + ": an InChI string");
            }
        }
        // The InChIKey pattern itself is RuleSetLintTests.NoFixtureHoldsPrivateContent.
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures.Distinct()));
    }

    /// <summary>The first VS20/VS21 name in a text, ignoring the origin token tuned_vs20; null when there is none.</summary>
    internal static string? VsName(string text) {
        var m = VsRecord.Match(text.Replace(OriginToken, ""));
        return m.Success ? m.Value : null;
    }

    [TestMethod]
    public void NoFixtureNameOrContentNamesAVs20OrVs21Record() {
        var failures = new List<string>();
        foreach (var f in RuleSetLintTests.FixtureFiles()) {
            if (VsName(f) != null) {
                failures.Add(f + ": the file name");
            }
            var raw = Encoding.ASCII.GetString(File.ReadAllBytes(FixturePath(f)));
            if (VsName(raw) != null) {
                failures.Add(f + ": its text");
            }
        }
        foreach (var (_, where, doc) in AllDocuments()) {
            if (EmbeddedRuleSetTests.DecodedStrings(doc).Any(s => VsName(s) != null)) {
                failures.Add(where + ": a decoded string");
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures.Distinct()));
    }

    [TestMethod]
    public void TheVsCheckFindsRecordNamesAndSparesTheOriginToken() {
        Assert.IsNull(VsName("{\"origin\":\"tuned_vs20\"}"));
        Assert.AreEqual("VS21", VsName("x.local-VS21.jsonl"));
        Assert.AreEqual("vs20", VsName("record vs20_000123"));
    }
}
