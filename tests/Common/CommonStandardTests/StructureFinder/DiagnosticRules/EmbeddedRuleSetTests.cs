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
[DeploymentItem(@"Resources\DiagnosticRuleFixtures", @"Resources\DiagnosticRuleFixtures")]
public class EmbeddedRuleSetTests
{
    /// <summary>The shipped set's version and rule-set sha256 as the fixture manifest pins them, which must also be
    /// the ruleset-history.tsv row. A change to any hashed file needs a new version, a new history row and one
    /// fixture record run (MSDIAL_DIAGNOSTIC_FIXTURES_WRITE), which rewrites the manifest: it is the one pin in the
    /// tests.</summary>
    private static string Version => ShippedRuleFixtures.PinnedRuleSet().Version;

    private static string Sha256 => ShippedRuleFixtures.PinnedRuleSet().Sha256;

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

    /// <summary>
    /// The released rows of ruleset-history.tsv (design 3.3: append-only, so a label never names two contents). The
    /// history must start with exactly these lines, in this order. A row is added here in the same change that adds it
    /// to the history, since merging that change releases it; until the merge both may still be edited together, and
    /// afterwards the row is never edited or removed. The manifest pin above follows the embedded files, so this list
    /// is what keeps a released row from being rewritten.
    /// </summary>
    private static readonly string[] ReleasedHistoryRows =
    {
        "0.0.1-skeleton\t7f84e0fe34bd672e375191e282e32e85618ce51079577b71001063ee1250774e\t2026-11-19",
        "0.1.0\taf4a7ebf2732b30b9610c7e410ba34f50f176b3fc0155d5a4f9a28fb413ab2eb\t2026-11-20",
    };

    [TestMethod]
    public void TheHistoryStartsWithEveryReleasedRowUnchanged() {
        var history = Encoding.ASCII.GetString(DiagnosticRuleSetLoader.ReadEmbedded()[DiagnosticRuleSetLoader.HistoryFile]);
        var lines = history.Split('\n');
        Assert.IsTrue(history.EndsWith("\n", StringComparison.Ordinal), "ruleset-history.tsv does not end with a newline");
        Assert.IsTrue(lines.Length - 1 >= ReleasedHistoryRows.Length, "a released history row is missing");
        CollectionAssert.AreEqual(ReleasedHistoryRows, lines.Take(ReleasedHistoryRows.Length).ToArray(), "a released history row was edited, removed or moved");
        // Later rows name versions no released row names.
        var released = new HashSet<string>(ReleasedHistoryRows.Select(r => r.Split('\t')[0]), StringComparer.Ordinal);
        Assert.IsFalse(lines.Skip(ReleasedHistoryRows.Length).Where(l => l.Length > 0).Any(l => released.Contains(l.Split('\t')[0])), "a released version is pinned again");
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
    /// <summary>An absolute POSIX or MSYS path: a slash at the start of a word followed by a one-letter drive
    /// (/c/..., the Git Bash form) or a usual home or mount root.</summary>
    private static readonly Regex PosixPath = new Regex(@"(^|[\s""'`(=,;<>\[])/([A-Za-z]|home|Users|mnt|root|tmp|var|opt|media)/[A-Za-z0-9_.~-]");
    private static readonly Regex Email = new Regex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}");

    /// <summary>The first kind of private content in a rule-set file, or null. Every check runs on the raw text and,
    /// for a JSON file, on each decoded key and string. The UNC check runs on decoded strings only, because the raw
    /// JSON spells one backslash (a SMILES stereo bond such as <c>C(=C\OC)</c>) as two.</summary>
    internal static string? PrivateContent(string path, byte[] bytes) {
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
            if (text.IndexOf('@') >= 0 && Email.IsMatch(text)) {
                return "an e-mail address";
            }
            if (text.IndexOf("local-vs21", StringComparison.OrdinalIgnoreCase) >= 0) {
                return "a local-vs21 name";
            }
            if (PosixPath.IsMatch(text)) {
                return "a POSIX path";
            }
            if (FixtureProvenanceLintTests.VsName(text) != null) {
                return "a VS20/VS21 name";
            }
        }
        return decoded.Any(t => UncPath.IsMatch(t)) ? "a UNC path" : null;
    }

    internal static IEnumerable<string> DecodedStrings(JsonValue value) {
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
        Assert.AreEqual("a POSIX path", Check("{\"notes\":\"see /f/data/x.json\"}"));
        Assert.AreEqual("a POSIX path", Check("{\"notes\":\"/home/someone/x\"}"));
        Assert.AreEqual("a POSIX path", PrivateContent("README.md", Encoding.ASCII.GetBytes("under `/mnt/c/x`\n")));
        Assert.AreEqual("a VS20/VS21 name", Check("{\"notes\":\"tuned on VS21 record 1234\"}"));
        // Not private: the origin token, a relative path, a URL path and a SMILES stereo bond.
        Assert.IsNull(Check("{\"origin\":\"tuned_vs20\",\"notes\":\"see rules/x.json\",\"smiles\":\"C/C=C/C\"}"));
        Assert.IsNull(PrivateContent("README.md", Encoding.ASCII.GetBytes("download path `/system/downloads/1_0/chemont/x.zip`, https://example.org/a/b\n")));
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

    /// <summary>The committed files of the source folder that are not embedded (its README.md) get the same privacy
    /// check as the embedded ones; .gitattributes holds only git attributes.</summary>
    [TestMethod]
    public void NoSourceFileOfTheRuleSetHoldsPrivateContent() {
        var root = SourceRuleSetDirectory();
        if (root is null) {
            Assert.Inconclusive("the RuleSet source folder is not above the test output");
        }
        var files = Directory.GetFiles(root!, "*", SearchOption.AllDirectories).Where(f => Path.GetFileName(f) != ".gitattributes")
                             .OrderBy(f => f, StringComparer.Ordinal).ToList();
        Assert.IsTrue(files.Any(f => Path.GetFileName(f) == "README.md"), "README.md is not in the source folder");
        foreach (var f in files) {
            var relative = f.Substring(root!.Length).TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/');
            var found = PrivateContent(relative, File.ReadAllBytes(f));
            Assert.IsNull(found, relative + ": " + found);
        }
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
