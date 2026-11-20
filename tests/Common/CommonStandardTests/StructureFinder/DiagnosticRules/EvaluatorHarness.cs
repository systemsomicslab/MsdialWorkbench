#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using System.Collections.Generic;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Runs the public <see cref="DiagnosticRuleEvaluator"/> on inline rule sets (design 7.1) and reads its canonical
/// result JSON back with the strict reader.
/// </summary>
internal static class EvaluatorHarness
{
    /// <summary>Apigenin hexoside [M-H]- with the aglycone ion Y0 (C15H9O5-) and the product ion X (C7H3O4-), both
    /// required and both claim terms, so the rule passes both two-ion passes when measured.</summary>
    public const string RuleId = "flavonoid.o_glycoside.neg";

    public const string Y0Ion = "C15H9O5", XIon = "C7H3O4", VetoIon = "C9H7O4";

    public static string Y0 => Term("Y0", "aglycone_ion", "required", Aglycone("even", "proton"));

    public static string X => Term("X", "product_ion", "required", Formula(XIon));

    /// <summary>A window veto around C9H7O4- (179.0350), half width 0.02 Da.</summary>
    public static string Veto => Term("w", "window", "veto_present", Window(VetoIon, "ion", "0.02"));

    /// <summary>The basic rule: all(Y0, X), claim terms [Y0, X].</summary>
    public static InlineRuleSet Basic(string ruleId = RuleId, bool withVeto = false) => new InlineRuleSet().AddBasic(ruleId, withVeto);

    /// <summary>Adds the basic rule to <paramref name="set"/>; a rule id ending in .pos is positive mode (and
    /// claims the same terms, under [M+H]+).</summary>
    public static InlineRuleSet AddBasic(this InlineRuleSet set, string ruleId, bool withVeto = false) {
        var terms = withVeto ? Terms(Y0, X, Veto) : Terms(Y0, X);
        var logic = AllOf(new[] { "Y0", "X" }, withVeto ? new[] { "w" } : null);
        var adducts = ruleId.EndsWith(".pos", System.StringComparison.Ordinal) ? "[\"[M+H]+\"]" : "[\"[M-H]-\"]";
        return set.AddRule(ruleId, terms, logic, CoreHex, adducts, "Y0").SetRule(ruleId, "claim/claim_term_ids", "[\"Y0\",\"X\"]");
    }

    /// <summary>measure and source_checked true: the default filter evaluates the rule.</summary>
    public static InlineRuleSet Measured(this InlineRuleSet set, string ruleId) =>
        set.SetRule(ruleId, "measure", "true").SetRule(ruleId, "source_checked", "true");

    /// <summary>
    /// Makes the rule measured and author-reviewed, with a review whose rule_semantic_sha256 is current: reviewed on
    /// <paramref name="classes"/> at <paramref name="productTolerance"/> and <paramref name="precursorTolerance"/>.
    /// </summary>
    public static InlineRuleSet Reviewed(this InlineRuleSet set, string ruleId, string classes = "[\"QTOF-type\"]",
                                         string productTolerance = "{\"abs_da\":0.005,\"ppm\":10.0,\"source\":\"p2\"}",
                                         string precursorTolerance = "0.01") {
        set.Measured(ruleId).SetRule(ruleId, "author_reviewed", "true");
        set.SetRule(ruleId, "review", Review(new string('0', 64), classes, productTolerance, precursorTolerance));
        var sha = DiagnosticRuleSetLoader.Validate(set.Files(), false).SemanticSha256(ruleId);
        return set.SetRule(ruleId, "review", Review(sha, classes, productTolerance, precursorTolerance));
    }

    private static string Review(string sha, string classes, string productTolerance, string precursorTolerance) =>
        "{\"by\":\"author\",\"at\":\"2026-12-01\",\"basis\":\"benchmark\",\"run_id\":\"p3cs-20261201-abc\","
        + "\"rules_precision_sha256\":\"" + new string('a', 64) + "\",\"review_proposal_sha256\":\"" + new string('b', 64) + "\","
        + "\"rule_semantic_sha256\":\"" + sha + "\",\"instrument_classes\":" + classes + ","
        + "\"product_tolerance\":" + productTolerance + ",\"precursor_tolerance_da\":" + precursorTolerance + "}";

    /// <summary>Options that evaluate every rule (inline rules are not measured by default).</summary>
    public static DiagnosticEvaluationOptions AllRules(bool verbose = false) => new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Verbose = verbose };

    public static DiagnosticRuleEvaluator Evaluator(InlineRuleSet set, DiagnosticEvaluationOptions? options = null) =>
        new DiagnosticRuleEvaluator(set.Load(), options ?? AllRules());

    /// <summary>The basic spectrum: the base peak (m/z 300, 1000), Y0 at 800 and X at 500.</summary>
    public static DiagnosticSpectrum Firing(string instrument = "QTOF") => WithInstrument(SpectrumNeg(Neg(Y0Ion, 800), Neg(XIon, 500)), instrument);

    public static DiagnosticSpectrum WithInstrument(DiagnosticSpectrum s, string instrument) =>
        new DiagnosticSpectrum(s.Peaks, s.PrecursorMz, s.IonMode, instrument);

    public static string ResultText(DiagnosticRuleResult result) => DiagnosticResultJson.Write(result);

    /// <summary>The result JSON read back by the strict reader (it accepts every result document).</summary>
    public static JsonObject Parse(DiagnosticRuleResult result) => StrictJsonReader.Read(DiagnosticResultJson.ToBytes(result));

    /// <summary>The keys of an object in ordinal order, joined by spaces.</summary>
    public static string Keys(JsonValue o) => string.Join(" ", ((JsonObject)o).Keys.OrderBy(k => k, System.StringComparer.Ordinal));

    public static RuleOutcome Outcome(DiagnosticRuleResult result, string ruleId = RuleId) => result.Outcomes.Single(o => o.RuleId == ruleId);

    public static IEnumerable<string> Ids(IEnumerable<HypothesisOutcome> hypotheses) => hypotheses.Select(h => h.HypothesisId);
}
