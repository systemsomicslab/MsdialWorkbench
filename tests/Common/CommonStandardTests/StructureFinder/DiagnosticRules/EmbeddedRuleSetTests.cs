using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
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
    /// <summary>The skeleton's version and rule-set sha256, as ruleset-history.tsv pins them. A change to any hashed
    /// file needs a new version, a new history row and this pair updated.</summary>
    private const string Version = "0.0.1-skeleton";
    private const string Sha256 = "7f84e0fe34bd672e375191e282e32e85618ce51079577b71001063ee1250774e";

    [TestMethod]
    public void TheEmbeddedSetLoadsPinned() {
        var set = DiagnosticRuleSet.LoadEmbedded();
        Assert.AreEqual(Version, set.Identity.Version);
        Assert.AreEqual(Sha256, set.Identity.Sha256);
        Assert.AreEqual(RuleSetSource.Embedded, set.Identity.Source);
        Assert.IsTrue(set.Identity.Pinned);
        Assert.AreEqual(0, set.Rules.Count);
        Assert.AreEqual(0, set.Motifs.Count);
        Assert.AreEqual(0, set.Nesting.Count);
        Assert.AreEqual(0, set.ScaffoldLists.Count);
        CollectionAssert.AreEqual(new[] { "Hex", "dHex", "Pen", "HexA" }, set.Units.Take(4).Select(u => u.Name).ToArray());
        Assert.IsTrue(set.Adducts.Any(a => a.Adduct == "[M-H]-" && a.Common));
    }

    [TestMethod]
    public void TheResourcesAreExactlyTheRuleSetFiles() {
        var names = typeof(DiagnosticRuleSet).Assembly.GetManifestResourceNames()
            .Select(n => n.Replace('\\', '/')).Where(n => n.StartsWith(DiagnosticRuleSetLoader.ResourcePrefix, StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(new[]
        {
            "DiagnosticRules/RuleSet/ruleset-history.tsv",
            "DiagnosticRules/RuleSet/ruleset.json",
            "DiagnosticRules/RuleSet/tables/adducts.json",
            "DiagnosticRules/RuleSet/tables/elements.json",
            "DiagnosticRules/RuleSet/tables/motif_nesting.json",
            "DiagnosticRules/RuleSet/tables/motifs.json",
            "DiagnosticRules/RuleSet/tables/units.json",
        }, names);
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

    [TestMethod]
    public void NoEmbeddedFileHoldsPrivateContent() {
        var inchikey = new Regex("[A-Z]{14}-[A-Z]{10}-[A-Z]");
        var drive = new Regex(@"(^|[^A-Za-z])[A-Za-z]:[\\/]");
        var unc = new Regex(@"\\\\[A-Za-z0-9]");
        var email = new Regex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}");
        foreach (var kv in DiagnosticRuleSetLoader.ReadEmbedded()) {
            var text = Encoding.ASCII.GetString(kv.Value);
            Assert.IsFalse(inchikey.IsMatch(text), kv.Key + ": an InChIKey");
            Assert.IsFalse(drive.IsMatch(text), kv.Key + ": a drive path");
            Assert.IsFalse(unc.IsMatch(text), kv.Key + ": a UNC path");
            Assert.IsFalse(email.IsMatch(text), kv.Key + ": an e-mail address");
            Assert.IsFalse(text.IndexOf("local-vs21", StringComparison.OrdinalIgnoreCase) >= 0, kv.Key + ": a local-vs21 name");
        }
    }

    /// <summary>The source folder, when the tests run from a build of this checkout; null otherwise.</summary>
    private static string? SourceRuleSetDirectory() {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
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
