using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Local parity with the Python reference on a real rule-set directory. Skipped unless MSDIAL_DIAGNOSTIC_RULES_DIR
/// names a rule-set folder; it then loads that folder as a directory set and writes the errors, the rule-set
/// sha256, every file sha256 and every rule_semantic_sha256 to the JSON file MSDIAL_DIAGNOSTIC_RULES_OUT names, for
/// comparison with <c>rule_format.py sha</c> and <c>validate</c>.
/// </summary>
/// <remarks>
/// <c>errors</c> is the directory load, which is unpinned; <c>errors_pinned</c> is the same files with the version pin
/// of the embedded path, which is what the reference's non-draft load checks.
/// </remarks>
[TestClass]
public class DiagnosticRuleSetParityLocalTests
{
    public const string DirectoryVariable = "MSDIAL_DIAGNOSTIC_RULES_DIR";
    public const string OutputVariable = "MSDIAL_DIAGNOSTIC_RULES_OUT";

    private static JsonValue Errors(IEnumerable<RuleLoadError> errors) =>
        new JsonArray(errors.Select(e => (JsonValue)new JsonArray(new[] { JsonValue.Of(e.Code), JsonValue.Of(e.File), JsonValue.Of(e.Pointer) })));

    private static KeyValuePair<string, JsonValue> M(string key, JsonValue value) => new KeyValuePair<string, JsonValue>(key, value);

    [TestMethod]
    public void WriteTheLoadOfALocalRuleSetDirectory() {
        var root = Environment.GetEnvironmentVariable(DirectoryVariable);
        if (string.IsNullOrEmpty(root)) {
            Assert.Inconclusive(DirectoryVariable + " is not set (local parity run only)");
        }
        var output = Environment.GetEnvironmentVariable(OutputVariable);
        if (string.IsNullOrEmpty(output)) {
            Assert.Inconclusive(OutputVariable + " is not set");
        }
        var files = DiagnosticRuleSetLoader.ReadDirectory(root!);
        var report = DiagnosticRuleSetLoader.Validate(files, checkPin: false);
        var pinned = DiagnosticRuleSetLoader.Validate(files, checkPin: true);
        var loaded = true;
        try {
            DiagnosticRuleSet.LoadDirectory(root!);
        }
        catch (DiagnosticRuleSetException) {
            loaded = false;
        }
        var semantic = report.Rules.Keys.Select(rid => M(rid, JsonValue.Of(report.SemanticSha256(rid)))).ToList();
        var doc = new JsonObject(new[]
        {
            M("errors", Errors(report.Errors)),
            M("errors_pinned", Errors(pinned.Errors)),
            M("file_sha256", new JsonObject(report.FileHashes.Select(kv => M(kv.Key, JsonValue.Of(kv.Value))))),
            M("loaded", JsonValue.Of(loaded)),
            M("n_rules", JsonValue.Of(report.RuleCount)),
            M("rule_semantic_sha256", new JsonObject(semantic)),
            M("rule_set_version", JsonValue.OfNullable(report.RuleSetVersion)),
            M("ruleset_sha256", JsonValue.OfNullable(report.RuleSetSha256)),
            M("source", JsonValue.Of("directory")),
        });
        File.WriteAllBytes(output!, PrettyJson.ToBytes(doc));
        Assert.AreEqual(loaded, report.Errors.Count == 0);
    }
}
