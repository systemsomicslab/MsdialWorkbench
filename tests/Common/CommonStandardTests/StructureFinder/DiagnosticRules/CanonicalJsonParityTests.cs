using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.DiagnosticRuleFixtures;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The strict reader, canonical JSON, the hashes and the result-number rules against the CPython vectors of
/// <c>cjson-vectors.json</c> (design 3.1, 3.2, 4.11, 7.2). Every case must agree exactly.
/// </summary>
[TestClass]
[DeploymentItem(@"Resources\DiagnosticRuleFixtures", @"Resources\DiagnosticRuleFixtures")]
public class CanonicalJsonParityTests
{
    [TestMethod]
    public void TheVectorFileIsTheOneSourceMdNames() {
        var bytes = VectorBytesOrInconclusive();
        Assert.AreEqual(VectorSha256, Sha256Hex.Of(bytes));
        var source = File.ReadAllText(FixturePath("SOURCE.md"));
        StringAssert.Contains(source, VectorSha256);
        Assert.AreEqual("msfinder-diagnostic-cjson-vectors", Str(Vectors(), "format"));
        Assert.AreEqual(RuleSemanticHash.EngineSemantics, Str(Vectors(), "engine_semantics"));
    }

    [TestMethod]
    public void TheVectorFileIsInStorageForm() {
        var bytes = VectorBytesOrInconclusive();
        CollectionAssert.AreEqual(bytes, PrettyJson.ToBytes(StrictJsonReader.Read(bytes)));
    }

    [TestMethod]
    public void TheErrorCodesAreTheReferenceCodes() {
        var expected = ((JsonObject)Vectors()["error_codes"]).Members.Select(m => m.Key + "\t" + Str(m.Value)).OrderBy(x => x, StringComparer.Ordinal);
        var actual = RuleErrorCodes.All.Select(kv => kv.Key + "\t" + kv.Value).OrderBy(x => x, StringComparer.Ordinal);
        Assert.AreEqual(string.Join("\n", expected), string.Join("\n", actual));
        Assert.AreEqual(68, RuleErrorCodes.All.Count);
    }

    [TestMethod]
    public void StrictJsonCasesAgree() {
        var failures = new List<string>();
        foreach (JsonObject c in Section("strict_json").Cast<JsonObject>()) {
            var name = Str(c, "name");
            var input = B64(c["input_b64"]);
            string outcome;
            try {
                var doc = StrictJsonReader.Read(input);
                var canonical = CanonicalJson.ToText(doc);
                outcome = "ok " + canonical + " " + Sha256Hex.Of(Encoding.ASCII.GetBytes(canonical)) + " " + DiagnosticRuleSetLoader.FileSha256(input);
            }
            catch (StrictJsonException e) {
                outcome = "error " + e.Code;
            }
            var expected = Str(c, "expect") == "ok"
                ? "ok " + Str(c, "canonical") + " " + Str(c, "sha256") + " " + Str(c, "sha256")
                : "error " + Str(c, "code");
            if (outcome != expected) {
                failures.Add(name + ": expected " + expected + ", got " + outcome);
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
        Assert.AreEqual(117, Section("strict_json").Count);
    }

    [TestMethod]
    public void FloatLexemesConvertWithOneDivision() {
        var failures = new List<string>();
        foreach (JsonObject c in Section("float_lexeme").Cast<JsonObject>()) {
            var lexeme = Str(c, "lexeme");
            var f = JsonFloat.Parse(lexeme);
            var got = IeeeBits.Of(f.Value) + " " + f.Mantissa + " " + f.Scale + " " + f.Canonical;
            var expected = Str(c, "ieee_hex") + " " + Int(c, "mantissa") + " " + Int(c, "scale") + " " + Str(c, "canonical");
            if (got != expected) {
                failures.Add(lexeme + ": expected " + expected + ", got " + got);
            }
            var viaReader = (JsonFloat)StrictJsonReader.Read(Encoding.ASCII.GetBytes("{\"a\":" + lexeme + "}"))["a"];
            Assert.AreEqual(f.Canonical, viaReader.Canonical);
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    [TestMethod]
    public void FixedDecimalsAreExactAndHalfEven() {
        var failures = new List<string>();
        foreach (JsonObject c in Section("fixed_decimal").Cast<JsonObject>()) {
            var x = FromHex(Str(c, "ieee_hex"));
            var d = (int)Int(c, "d");
            var got = ExactDecimal.Format(x, d);
            if (got != Str(c, "text")) {
                failures.Add(Str(c, "ieee_hex") + " d=" + d + ": expected " + Str(c, "text") + ", got " + got);
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    [TestMethod]
    public void ResultDoublesAreNullWhenNotFinite() {
        var failures = new List<string>();
        foreach (JsonObject c in Section("result_double").Cast<JsonObject>()) {
            var x = FromHex(Str(c, "ieee_hex"));
            var got = ExactDecimal.FormatOrNull(x, (int)Int(c, "d"));
            var expected = c["text"] is JsonString s ? s.Value : null;
            if (got != expected) {
                failures.Add(Str(c, "ieee_hex") + ": expected " + (expected ?? "null") + ", got " + (got ?? "null"));
            }
            if (expected is null) {
                Assert.ThrowsException<ArgumentException>(() => ExactDecimal.Format(x, 6));
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    [TestMethod]
    public void StringsAreEscapedAsPythonEnsureAscii() {
        var failures = new List<string>();
        foreach (JsonObject c in Section("string_escape").Cast<JsonObject>()) {
            var text = FromUtf16Hex(Str(c, "text_utf16_hex"));
            var got = CanonicalJson.EscapeString(text);
            if (got != Str(c, "json")) {
                failures.Add(Str(c, "text_utf16_hex") + ": expected " + Str(c, "json") + ", got " + got);
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    [TestMethod]
    public void IeeeBitsCanonicaliseEveryNaN() {
        var failures = new List<string>();
        foreach (JsonObject c in Section("ieee_bits").Cast<JsonObject>()) {
            var got = IeeeBits.Of(FromHex(Str(c, "ieee_hex_in")));
            if (got != Str(c, "bits")) {
                failures.Add(Str(c, "ieee_hex_in") + ": expected " + Str(c, "bits") + ", got " + got);
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    [TestMethod]
    public void FileAndRuleSetHashesAgree() {
        foreach (JsonObject c in Section("file_hashes").Cast<JsonObject>()) {
            var files = Files((JsonObject)c["files"]);
            foreach (var m in ((JsonObject)c["file_sha256"]).Members) {
                Assert.AreEqual(Str(m.Value), DiagnosticRuleSetLoader.FileSha256(files[m.Key]), Str(c, "name") + " " + m.Key);
            }
            Assert.AreEqual(Str(c, "ruleset_sha256"), DiagnosticRuleSetLoader.RuleSetSha256(files), Str(c, "name"));
        }
    }

    [TestMethod]
    public void RuleSemanticHashesAgree() {
        foreach (JsonObject c in Section("rule_semantic").Cast<JsonObject>()) {
            var got = RuleSemanticHash.Compute((JsonObject)c["rule"], c["resolved"], Str(c, "engine_semantics"));
            Assert.AreEqual(Str(c, "sha256"), got, Str(c, "name"));
        }
    }

    [TestMethod]
    public void CleanSetsLoadWithTheReferenceHashesAndResolvedSets() {
        foreach (JsonObject c in Section("ruleset_load").Cast<JsonObject>()) {
            var name = Str(c, "name");
            var files = Files((JsonObject)c["files"]);
            var report = DiagnosticRuleSetLoader.Validate(files, checkPin: true);
            Assert.AreEqual(ErrorList((JsonArray)c["errors"]), ErrorList(report.Errors), name);
            Assert.AreEqual(Str(c, "rule_set_version"), report.RuleSetVersion, name);
            Assert.AreEqual(Str(c, "ruleset_sha256"), report.RuleSetSha256, name);
            var expectedFiles = ((JsonObject)c["file_sha256"]).Members.Select(m => m.Key + " " + Str(m.Value));
            Assert.AreEqual(string.Join("\n", expectedFiles), string.Join("\n", report.FileHashes.Select(kv => kv.Key + " " + kv.Value)), name);
            var rules = (JsonObject)c["rules"];
            CollectionAssert.AreEqual(rules.Keys.ToList(), report.Rules.Keys.ToList(), name);
            foreach (var m in rules.Members) {
                var expected = (JsonObject)m.Value;
                Assert.AreEqual(CanonicalJson.ToText(expected["resolved"]), CanonicalJson.ToText(report.Resolved(m.Key)), name + " " + m.Key);
                Assert.AreEqual(Str(expected, "rule_semantic_sha256"), report.SemanticSha256(m.Key), name + " " + m.Key);
            }
            // The public API gives the same identity, and every rule materialises.
            var set = DiagnosticRuleSet.LoadFiles(files, RuleSetSource.Embedded);
            Assert.AreEqual(Str(c, "ruleset_sha256"), set.Identity.Sha256);
            Assert.IsTrue(set.Identity.Pinned);
            Assert.AreEqual(rules.Count, set.Rules.Count);
            foreach (var rule in set.Rules) {
                Assert.AreEqual(Str((JsonObject)rules[rule.RuleId], "rule_semantic_sha256"), rule.SemanticSha256);
            }
            var inline = DiagnosticRuleSet.LoadFiles(files, RuleSetSource.Inline);
            Assert.IsFalse(inline.Identity.Pinned);
            Assert.AreEqual(set.Identity.Sha256, inline.Identity.Sha256);
        }
    }
}
