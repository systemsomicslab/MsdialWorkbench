using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.DiagnosticRuleFixtures;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// One synthetic file set per error code (the <c>loader_errors</c> vectors, design 3.6 and 7.1): the C# loader must
/// give the reference's exact error list, codes, files and RFC 6901 pointers, in the same order.
/// </summary>
/// <remarks>
/// The vectors were made with the reference's pinned load (<c>load_files(files, draft=False)</c>), which checks the
/// version against ruleset-history.tsv; that is the embedded path here. A directory set is unpinned, so it never
/// reports history_format, ruleset_unpinned or ruleset_sha_mismatch.
/// </remarks>
[TestClass]
[DeploymentItem(@"Resources\DiagnosticRuleFixtures", @"Resources\DiagnosticRuleFixtures")]
public class RuleLoaderValidationTests
{
    private static JsonObject LoaderVectors() => (JsonObject)Vectors()["loader_errors"];

    public static IEnumerable<KeyValuePair<string, Dictionary<string, byte[]>>> CaseFiles() {
        var loader = LoaderVectors();
        var baseFiles = (JsonObject)loader["base_files"];
        foreach (JsonObject c in ((JsonArray)loader["cases"]).Cast<JsonObject>()) {
            yield return new KeyValuePair<string, Dictionary<string, byte[]>>(Str(c, "name"), Patched(baseFiles, (JsonObject)c["patch"]));
        }
    }

    [TestMethod]
    public void EveryLoaderErrorVectorGivesTheReferenceErrorList() {
        var loader = LoaderVectors();
        var baseFiles = (JsonObject)loader["base_files"];
        var failures = new List<string>();
        var covered = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonObject c in ((JsonArray)loader["cases"]).Cast<JsonObject>()) {
            var name = Str(c, "name");
            var files = Patched(baseFiles, (JsonObject)c["patch"]);
            var report = DiagnosticRuleSetLoader.Validate(files, checkPin: true);
            var expected = ErrorList((JsonArray)c["errors"]);
            var got = ErrorList(report.Errors);
            if (expected != got) {
                failures.Add(name + ":\n  expected:\n    " + expected.Replace("\n", "\n    ") + "\n  got:\n    " + got.Replace("\n", "\n    "));
            }
            if (c["code"] is JsonString code) {
                covered.Add(code.Value);
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
        var missing = RuleErrorCodes.All.Select(kv => kv.Key).Where(k => !covered.Contains(k)).ToList();
        Assert.AreEqual(0, missing.Count, "codes without a vector: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void TheCleanBaseSetLoadsThroughThePublicApi() {
        var baseFiles = Files((JsonObject)LoaderVectors()["base_files"]);
        var set = DiagnosticRuleSet.LoadFiles(baseFiles, RuleSetSource.Embedded);
        Assert.AreEqual("0.0.1-test", set.Identity.Version);
        Assert.IsTrue(set.Identity.Pinned);
        Assert.AreEqual(1, set.Rules.Count);
        var rule = set.Rules[0];
        Assert.AreEqual("flavonoid.o_glycoside.neg", rule.RuleId);
        CollectionAssert.AreEqual(new[] { "[M-H]-", "[M+HCOO]-" }, rule.AdductsTried.Select(a => a.Adduct.Adduct).ToArray());
        CollectionAssert.AreEqual(new[] { true, false }, rule.AdductsTried.Select(a => a.FromRule).ToArray());
        Assert.AreEqual(Model.LogicNodeKind.All, rule.Logic.Kind);
        Assert.AreEqual(4, rule.Terms.Count);
    }

    [TestMethod]
    public void EveryErrorRefusesTheWholeSet() {
        foreach (var c in CaseFiles()) {
            if (c.Key == "clean") {
                continue;
            }
            var e = Assert.ThrowsException<DiagnosticRuleSetException>(() => DiagnosticRuleSet.LoadFiles(c.Value, RuleSetSource.Embedded), c.Key);
            Assert.IsTrue(e.Errors.Count > 0, c.Key);
        }
    }

    [TestMethod]
    public void ADirectorySetIsNeverRefusedForItsPin() {
        var loader = LoaderVectors();
        var baseFiles = (JsonObject)loader["base_files"];
        foreach (JsonObject c in ((JsonArray)loader["cases"]).Cast<JsonObject>()) {
            var code = c["code"] is JsonString s ? s.Value : null;
            if (code != RuleErrorCodes.HistoryFormat && code != RuleErrorCodes.RulesetUnpinned && code != RuleErrorCodes.RulesetShaMismatch) {
                continue;
            }
            var report = DiagnosticRuleSetLoader.Validate(Patched(baseFiles, (JsonObject)c["patch"]), checkPin: false);
            Assert.AreEqual(0, report.Errors.Count, Str(c, "name") + ": " + ErrorList(report.Errors));
            var set = DiagnosticRuleSet.LoadFiles(Patched(baseFiles, (JsonObject)c["patch"]), RuleSetSource.Directory);
            Assert.IsFalse(set.Identity.Pinned);
        }
    }
}
