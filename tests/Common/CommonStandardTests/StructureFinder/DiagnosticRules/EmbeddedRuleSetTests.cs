#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The rule set embedded in the Common assembly (design 2.2, 3.3): its resources, its version pin, its hashes, its
/// storage form, and the privacy rules for committed rule files.
/// </summary>
[TestClass]
public class EmbeddedRuleSetTests
{
    /// <summary>The shipped set's version and rule-set sha256, as ruleset-history.tsv pins them. A change to any
    /// hashed file needs a new version, a new history row and this pair updated.</summary>
    private const string Version = "0.1.0";
    private const string Sha256 = "a8e02e0aaab56793f39228abe933c5ed957e27fa33b097fb5c492a0259fde5f1";

    /// <summary>The shipped set's size: rules, scaffold lists, motifs and nesting rows.</summary>
    private const int RuleCount = 106;
    private const int ScaffoldListCount = 58;
    private const int MotifCount = 106;
    private const int NestingCount = 28;

    /// <summary>The files every embedded set holds besides its rules and scaffold lists.</summary>
    private static readonly string[] FixedFiles =
    {
        "ruleset-history.tsv",
        "ruleset.json",
        "tables/adducts.json",
        "tables/elements.json",
        "tables/motif_nesting.json",
        "tables/motifs.json",
        "tables/units.json",
    };

    [TestMethod]
    public void TheEmbeddedSetLoadsPinned() {
        var set = DiagnosticRuleSet.LoadEmbedded();
        Assert.AreEqual(Version, set.Identity.Version);
        Assert.AreEqual(Sha256, set.Identity.Sha256);
        Assert.AreEqual(RuleSetSource.Embedded, set.Identity.Source);
        Assert.IsTrue(set.Identity.Pinned);
        Assert.AreEqual(RuleCount, set.Rules.Count);
        Assert.AreEqual(MotifCount, set.Motifs.Count);
        Assert.AreEqual(NestingCount, set.Nesting.Count);
        Assert.AreEqual(ScaffoldListCount, set.ScaffoldLists.Count);
        // No rule of this version has had the author's review (design 8, PR 3).
        Assert.IsFalse(set.Rules.Any(r => r.AuthorReviewed || r.Review != null), "a rule is marked reviewed");
        CollectionAssert.AreEqual(new[] { "Hex", "dHex", "Pen", "HexA" }, set.Units.Take(4).Select(u => u.Name).ToArray());
        Assert.IsTrue(set.Adducts.Any(a => a.Adduct == "[M-H]-" && a.Common));
    }

    [TestMethod]
    public void TheResourcesAreExactlyTheRuleSetFiles() {
        var names = typeof(DiagnosticRuleSet).Assembly.GetManifestResourceNames()
            .Select(n => n.Replace('\\', '/')).Where(n => n.StartsWith(DiagnosticRuleSetLoader.ResourcePrefix, StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal).ToArray();
        // Exactly the fixed files, one rules/<rule_id>.json per loaded rule and one scaffolds/<list_id>.json per
        // loaded scaffold list: no resource is left over and none is missing.
        var set = DiagnosticRuleSet.LoadEmbedded();
        Assert.AreEqual(RuleCount, set.Rules.Count);
        Assert.AreEqual(ScaffoldListCount, set.ScaffoldLists.Count);
        var expected = FixedFiles
            .Concat(set.Rules.Select(r => "rules/" + r.RuleId + ".json"))
            .Concat(set.ScaffoldLists.Select(l => "scaffolds/" + l.ListId + ".json"))
            .Select(n => DiagnosticRuleSetLoader.ResourcePrefix + n)
            .OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.AreEqual(FixedFiles.Length + RuleCount + ScaffoldListCount, expected.Distinct(StringComparer.Ordinal).Count());
        CollectionAssert.AreEqual(expected, names);
    }

    [TestMethod]
    public void TheHistoryPinsTheEmbeddedSha256() {
        var files = DiagnosticRuleSetLoader.ReadEmbedded();
        var history = Encoding.ASCII.GetString(files[DiagnosticRuleSetLoader.HistoryFile]);
        var rows = history.Split('\n').Where(l => l.Length > 0).Select(l => l.Split('\t')).ToList();
        Assert.IsTrue(rows.All(r => r.Length == 3), history);
        Assert.AreEqual(rows.Count, rows.Select(r => r[0]).Distinct(StringComparer.Ordinal).Count(), "a version occurs twice");
        var row = rows.Single(r => r[0] == Version);
        Assert.AreEqual(Sha256, row[1]);
        var hashed = files.Where(kv => kv.Key.EndsWith(".json", StringComparison.Ordinal)).ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.AreEqual(Sha256, DiagnosticRuleSetLoader.RuleSetSha256(hashed));
    }

    [TestMethod]
    public void ResourceOrderDoesNotChangeTheSha256() {
        var files = DiagnosticRuleSetLoader.ReadEmbedded();
        var reversed = files.Reverse().ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.AreEqual(DiagnosticRuleSetLoader.Validate(files, true).RuleSetSha256, DiagnosticRuleSetLoader.Validate(reversed, true).RuleSetSha256);
    }

    [TestMethod]
    public void AnEditedEmbeddedFileWithoutANewHistoryRowIsRefused() {
        var files = DiagnosticRuleSetLoader.ReadEmbedded();
        files["tables/motifs.json"] = Encoding.ASCII.GetBytes("{\"format\":\"msfinder-diagnostic-motifs\",\"format_version\":1,\"motifs\":[],\"x\":1}");
        var e = Assert.ThrowsException<DiagnosticRuleSetException>(() => DiagnosticRuleSet.LoadFiles(files, RuleSetSource.Embedded));
        CollectionAssert.Contains(e.Errors.Select(x => x.Code).ToList(), RuleErrorCodes.RulesetShaMismatch);
        files = DiagnosticRuleSetLoader.ReadEmbedded();
        files["ruleset.json"] = Encoding.ASCII.GetBytes("{\"format\":\"msfinder-diagnostic-ruleset\",\"format_version\":1,\"rule_set_version\":\"9.9.9\"}");
        e = Assert.ThrowsException<DiagnosticRuleSetException>(() => DiagnosticRuleSet.LoadFiles(files, RuleSetSource.Embedded));
        Assert.AreEqual("ruleset_unpinned\truleset.json\t/rule_set_version", string.Join("\n", e.Errors));
    }

    [TestMethod]
    public void EveryEmbeddedJsonFileIsInStorageForm() {
        foreach (var kv in DiagnosticRuleSetLoader.ReadEmbedded().Where(kv => kv.Key.EndsWith(".json", StringComparison.Ordinal))) {
            CollectionAssert.AreEqual(PrettyJson.ToBytes(StrictJsonReader.Read(kv.Value)), kv.Value, kv.Key);
        }
    }

    private static readonly Regex InChIKey = new Regex("[A-Z]{14}-[A-Z]{10}-[A-Z]");
    private static readonly Regex DrivePath = new Regex(@"(^|[^A-Za-z])[A-Za-z]:[\\/]");
    private static readonly Regex UncPath = new Regex(@"\\\\[A-Za-z0-9]");
    private static readonly Regex Email = new Regex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}");

    /// <summary>The first kind of private content in a rule-set file, or null. Every check runs on the raw text and,
    /// for a JSON file, on each decoded key and string. The UNC check runs on decoded strings only, because the raw
    /// JSON spells one backslash (a SMILES stereo bond such as <c>C(=C\OC)</c>) as two.</summary>
    private static string? PrivateContent(string path, byte[] bytes) {
        var raw = Encoding.ASCII.GetString(bytes);
        var isJson = path.EndsWith(".json", StringComparison.Ordinal);
        var decoded = isJson ? DecodedStrings(StrictJsonReader.Read(bytes)).ToList() : new List<string> { raw };
        foreach (var text in decoded.Concat(new[] { raw })) {
            if (InChIKey.IsMatch(text)) {
                return "an InChIKey";
            }
            if (DrivePath.IsMatch(text)) {
                return "a drive path";
            }
            if (Email.IsMatch(text)) {
                return "an e-mail address";
            }
            if (text.IndexOf("local-vs21", StringComparison.OrdinalIgnoreCase) >= 0) {
                return "a local-vs21 name";
            }
        }
        return decoded.Any(t => UncPath.IsMatch(t)) ? "a UNC path" : null;
    }

    private static IEnumerable<string> DecodedStrings(JsonValue value) {
        switch (value) {
            case JsonString s:
                yield return s.Value;
                break;
            case JsonArray a:
                foreach (var item in a) {
                    foreach (var t in DecodedStrings(item)) {
                        yield return t;
                    }
                }
                break;
            case JsonObject o:
                foreach (var m in o.Members) {
                    yield return m.Key;
                    foreach (var t in DecodedStrings(m.Value)) {
                        yield return t;
                    }
                }
                break;
        }
    }

    [TestMethod]
    public void NoEmbeddedFileHoldsPrivateContent() {
        foreach (var kv in DiagnosticRuleSetLoader.ReadEmbedded()) {
            var found = PrivateContent(kv.Key, kv.Value);
            Assert.IsNull(found, kv.Key + ": " + found);
        }
    }

    [TestMethod]
    public void ThePrivateContentCheckFindsEachKind() {
        string? Check(string json) => PrivateContent("rules/x.json", Encoding.ASCII.GetBytes(json));
        Assert.IsNull(Check("{\"smiles\":\"CC/C(=C\\\\OC)/C\"}"));
        Assert.AreEqual("a UNC path", Check("{\"notes\":\"\\\\\\\\host\\\\share\"}"));
        Assert.AreEqual("a UNC path", PrivateContent("ruleset-history.tsv", Encoding.ASCII.GetBytes("\\\\host\\share\n")));
        Assert.AreEqual("a drive path", Check("{\"notes\":\"D:\\\\data\"}"));
        Assert.AreEqual("a drive path", Check("{\"notes\":\"see C:/x\"}"));
        Assert.AreEqual("an InChIKey", Check("{\"notes\":\"AAAAAAAAAAAAAA-BBBBBBBBBB-C\"}"));
        Assert.AreEqual("an e-mail address", Check("{\"notes\":\"a.b@example.org\"}"));
        Assert.AreEqual("a local-vs21 name", Check("{\"Local-VS21\":1}"));
    }

    /// <summary>The source folder, found from this source file's path (independent of where MSTest deploys the
    /// assembly) or, failing that, above the test output; null when neither lies in a checkout.</summary>
    private static string? SourceRuleSetDirectory([CallerFilePath] string sourceFile = "") {
        return FindRuleSetAbove(Path.GetDirectoryName(sourceFile)) ?? FindRuleSetAbove(AppDomain.CurrentDomain.BaseDirectory);
    }

    private static string? FindRuleSetAbove(string? start) {
        if (string.IsNullOrEmpty(start) || !Directory.Exists(start)) {
            return null;
        }
        var dir = new DirectoryInfo(start);
        while (dir != null) {
            var candidate = Path.Combine(dir.FullName, "src", "Common", "CommonStandard", "StructureFinder", "DiagnosticRules", "RuleSet");
            if (Directory.Exists(candidate)) {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }

    [TestMethod]
    public void TheEmbeddedSetEqualsItsSourceDirectory() {
        var root = SourceRuleSetDirectory();
        if (root is null) {
            Assert.Inconclusive("the RuleSet source folder is not above the test output");
        }
        var directory = DiagnosticRuleSet.LoadDirectory(root!);
        Assert.AreEqual(Sha256, directory.Identity.Sha256);
        Assert.IsFalse(directory.Identity.Pinned);
        Assert.AreEqual(RuleSetSource.Directory, directory.Identity.Source);
        var embedded = DiagnosticRuleSet.LoadEmbedded();
        CollectionAssert.AreEqual(embedded.Identity.Files.Select(f => f.Path + " " + f.Sha256).ToList(),
                                  directory.Identity.Files.Select(f => f.Path + " " + f.Sha256).ToList());
        // Byte for byte, so the resources are the committed files and not a converted copy.
        var files = DiagnosticRuleSetLoader.ReadEmbedded();
        foreach (var kv in files) {
            CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(root!, kv.Key.Replace('/', Path.DirectorySeparatorChar))), kv.Value, kv.Key);
        }
    }
}
