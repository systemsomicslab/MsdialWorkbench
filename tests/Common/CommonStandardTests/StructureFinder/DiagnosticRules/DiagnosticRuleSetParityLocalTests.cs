using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Local parity with the Python reference (<c>rule_format.py sha</c> and <c>validate</c>) on real rule-set
/// directories. Skipped unless MSDIAL_DIAGNOSTIC_RULES_DIR and MSDIAL_DIAGNOSTIC_RULES_OUT are set.
/// </summary>
/// <remarks>
/// <para>MSDIAL_DIAGNOSTIC_RULES_DIR names a rule-set folder, or several separated by ';'. For one folder,
/// MSDIAL_DIAGNOSTIC_RULES_OUT is the JSON file written; for several, it is a folder that receives <c>0.json</c>,
/// <c>1.json</c>, ... in the order given.</para>
/// <para>The load mirrors the reference: by default its non-draft load (the version pin checked), and with
/// MSDIAL_DIAGNOSTIC_RULES_DRAFT=1 its <c>--draft</c> load (staging folders merged, no pin). Each output holds the
/// rule-set sha256, every file sha256, every rule_semantic_sha256 and the ordered error list (code, file, pointer).
/// <c>loaded_as_directory</c> records whether <see cref="DiagnosticRuleSet.LoadDirectory"/> accepts the folder;
/// that load is unpinned.</para>
/// </remarks>
[TestClass]
public class DiagnosticRuleSetParityLocalTests
{
    public const string DirectoryVariable = "MSDIAL_DIAGNOSTIC_RULES_DIR";
    public const string OutputVariable = "MSDIAL_DIAGNOSTIC_RULES_OUT";
    public const string DraftVariable = "MSDIAL_DIAGNOSTIC_RULES_DRAFT";

    private static JsonValue Errors(IEnumerable<RuleLoadError> errors) =>
        new JsonArray(errors.Select(e => (JsonValue)new JsonArray(new[] { JsonValue.Of(e.Code), JsonValue.Of(e.File), JsonValue.Of(e.Pointer) })));

    private static KeyValuePair<string, JsonValue> M(string key, JsonValue value) => new KeyValuePair<string, JsonValue>(key, value);

    [TestMethod]
    public void WriteTheLoadOfALocalRuleSetDirectory() {
        var roots = Environment.GetEnvironmentVariable(DirectoryVariable);
        var output = Environment.GetEnvironmentVariable(OutputVariable);
        if (string.IsNullOrEmpty(roots) || string.IsNullOrEmpty(output)) {
            Assert.Inconclusive(DirectoryVariable + " and " + OutputVariable + " are not set (local parity run only)");
        }
        var draft = Environment.GetEnvironmentVariable(DraftVariable) == "1";
        var list = roots!.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (list.Length > 1) {
            Directory.CreateDirectory(output!);
        }
        for (var i = 0; i < list.Length; i++) {
            var path = list.Length == 1 ? output! : Path.Combine(output!, i.ToString(CultureInfo.InvariantCulture) + ".json");
            File.WriteAllBytes(path, PrettyJson.ToBytes(Describe(list[i], draft)));
        }
    }

    private static JsonObject Describe(string root, bool draft) {
        var files = DiagnosticRuleSetLoader.ReadDirectory(root);
        var report = DiagnosticRuleSetLoader.Validate(files, checkPin: !draft, draft: draft);
        var loaded = true;
        try {
            DiagnosticRuleSet.LoadDirectory(root);
        }
        catch (DiagnosticRuleSetException) {
            loaded = false;
        }
        var semantic = report.Rules.Keys.Select(rid => M(rid, JsonValue.Of(report.SemanticSha256(rid)))).ToList();
        return new JsonObject(new[]
        {
            M("draft", JsonValue.Of(draft)),
            M("errors", Errors(report.Errors)),
            M("file_sha256", new JsonObject(report.FileHashes.Select(kv => M(kv.Key, JsonValue.Of(kv.Value))))),
            M("loaded_as_directory", JsonValue.Of(loaded)),
            M("n_rules", JsonValue.Of(report.RuleCount)),
            M("not_hashed", new JsonArray(report.StagingFiles.Select(p => JsonValue.Of(p)))),
            M("rule_semantic_sha256", new JsonObject(semantic)),
            M("rule_set_version", JsonValue.OfNullable(report.RuleSetVersion)),
            M("ruleset_sha256", JsonValue.OfNullable(report.RuleSetSha256)),
        });
    }
}
