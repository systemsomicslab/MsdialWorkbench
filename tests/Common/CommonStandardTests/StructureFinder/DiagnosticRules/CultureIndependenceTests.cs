using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.DiagnosticRuleFixtures;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The rule-set format never depends on the thread's culture (design 6.2; MsdialWorkbench #868 and #869 must not
/// return). The whole PR 1 surface (strict reader, canonical JSON, float lexemes, fixed decimals, masses, every
/// loader vector, the clean sets and the embedded set) is run under each culture and must give the same text,
/// byte for byte, as under the invariant culture.
/// </summary>
[TestClass]
[DeploymentItem(@"Resources\DiagnosticRuleFixtures", @"Resources\DiagnosticRuleFixtures")]
public class CultureIndependenceTests
{
    /// <summary>A decimal comma, U+202F digit grouping and a U+2212 minus sign.</summary>
    private static CultureInfo Hostile() {
        var c = (CultureInfo)CultureInfo.GetCultureInfo("fr-FR").Clone();
        c.NumberFormat.NumberDecimalSeparator = ",";
        c.NumberFormat.NumberGroupSeparator = " ";
        c.NumberFormat.NegativeSign = "−";
        c.NumberFormat.PositiveSign = "+";
        return c;
    }

    private static string Digest() {
        var sb = new StringBuilder();
        foreach (JsonObject c in Section("strict_json").Cast<JsonObject>()) {
            try {
                sb.Append(CanonicalJson.ToText(StrictJsonReader.Read(B64(c["input_b64"])))).Append('\n');
            }
            catch (StrictJsonException e) {
                sb.Append(e.Code).Append('\n');
            }
        }
        foreach (JsonObject c in Section("float_lexeme").Cast<JsonObject>()) {
            var f = JsonFloat.Parse(Str(c, "lexeme"));
            sb.Append(f.Canonical).Append(' ').Append(IeeeBits.Of(f.Value)).Append('\n');
        }
        foreach (JsonObject c in Section("fixed_decimal").Cast<JsonObject>()) {
            sb.Append(ExactDecimal.Format(FromHex(Str(c, "ieee_hex")), (int)Int(c, "d"))).Append('\n');
        }
        foreach (JsonObject c in Section("string_escape").Cast<JsonObject>()) {
            sb.Append(CanonicalJson.EscapeString(FromUtf16Hex(Str(c, "text_utf16_hex")))).Append('\n');
        }
        var loader = (JsonObject)Vectors()["loader_errors"];
        foreach (JsonObject c in ((JsonArray)loader["cases"]).Cast<JsonObject>()) {
            var report = DiagnosticRuleSetLoader.Validate(Patched((JsonObject)loader["base_files"], (JsonObject)c["patch"]), true);
            sb.Append(ErrorList(report.Errors)).Append('\n').Append(report.RuleSetSha256).Append('\n');
        }
        foreach (JsonObject c in Section("ruleset_load").Cast<JsonObject>()) {
            var report = DiagnosticRuleSetLoader.Validate(Files((JsonObject)c["files"]), true);
            sb.Append(report.RuleSetSha256).Append('\n');
            foreach (var rid in report.Rules.Keys) {
                sb.Append(rid).Append(' ').Append(report.SemanticSha256(rid)).Append(' ').Append(CanonicalJson.ToText(report.Resolved(rid))).Append('\n');
            }
        }
        var set = DiagnosticRuleSet.LoadEmbedded();
        sb.Append(set.Identity.Sha256).Append('\n');
        foreach (var f in set.Identity.Files) {
            sb.Append(f.Path).Append(' ').Append(f.Sha256).Append('\n');
        }
        foreach (var u in set.Units) {
            var counts = Composition.Parse(u.ResidueFormula, set.Elements);
            sb.Append(u.Name).Append(' ').Append(Composition.Format(counts, set.Elements)).Append(' ')
              .Append(ExactDecimal.Format(ElementTable.ToDa((long)set.Elements.MassPdaExact(counts)), 6)).Append('\n');
        }
        return sb.ToString();
    }

    private static IEnumerable<object[]> Cultures() {
        foreach (var name in new[] { "en-US", "de-DE", "fr-FR", "sv-SE", "tr-TR", "cs-CZ", "ar-SA", "th-TH" }) {
            yield return new object[] { name };
        }
        yield return new object[] { "hostile" };
    }

    [DataTestMethod]
    [DynamicData(nameof(Cultures), DynamicDataSourceType.Method)]
    public void TheFormatGivesTheSameBytesUnder(string culture) {
        var expected = Under(CultureInfo.InvariantCulture, Digest);
        var actual = Under(culture == "hostile" ? Hostile() : CultureInfo.GetCultureInfo(culture), Digest);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TheVectorsPassUnderGermanAndFrenchThreads() {
        // The parity suites themselves, run on a de-DE and an fr-FR thread.
        foreach (var culture in new[] { "de-DE", "fr-FR" }) {
            Under(CultureInfo.GetCultureInfo(culture), () => {
                var canonical = new CanonicalJsonParityTests();
                canonical.StrictJsonCasesAgree();
                canonical.FloatLexemesConvertWithOneDivision();
                canonical.FixedDecimalsAreExactAndHalfEven();
                canonical.ResultDoublesAreNullWhenNotFinite();
                canonical.StringsAreEscapedAsPythonEnsureAscii();
                canonical.FileAndRuleSetHashesAgree();
                canonical.RuleSemanticHashesAgree();
                canonical.CleanSetsLoadWithTheReferenceHashesAndResolvedSets();
                new RuleLoaderValidationTests().EveryLoaderErrorVectorGivesTheReferenceErrorList();
                var masses = new MassTests();
                masses.ElementAndLexemePicoDaltonsAreExact();
                masses.FormulaMassesAreExactAndWithin1e5OfTheOracle();
                masses.IonMassesCarryTheElectronWithItsSign();
                masses.AdductPrecursorsAreExact();
                new EmbeddedRuleSetTests().TheEmbeddedSetLoadsPinned();
                return 0;
            });
        }
    }
}
