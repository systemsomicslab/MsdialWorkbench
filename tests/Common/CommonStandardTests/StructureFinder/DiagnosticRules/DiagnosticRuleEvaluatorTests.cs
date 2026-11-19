#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EvaluatorHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The public evaluator (design 4.10, 4.11): the engine semantics constant, the identity string, the option snapshot,
/// the filter, refusals, and the canonical result JSON (closed key sets, number text, escaping, input_sha256, compact
/// and verbose output).
/// </summary>
[TestClass]
public class DiagnosticRuleEvaluatorTests
{
    [TestMethod]
    public void EngineSemanticsIsOneConstant() {
        Assert.AreEqual("msfinder-diagnostic-rules/1", DiagnosticRuleEvaluator.EngineSemantics);
        Assert.AreEqual(DiagnosticRuleEvaluator.EngineSemantics, RuleSemanticHash.EngineSemantics);
        foreach (var t in new[] { typeof(DiagnosticRuleEvaluator), typeof(RuleSemanticHash) }) {
            Assert.IsTrue(t.GetField("EngineSemantics", BindingFlags.Public | BindingFlags.Static)!.IsLiteral, t.Name);
        }
        var result = Evaluator(Basic()).Evaluate(Firing());
        Assert.AreEqual(DiagnosticRuleEvaluator.EngineSemantics, result.EngineSemantics);
        Assert.AreEqual("\"msfinder-diagnostic-rules/1\"", CanonicalJson.ToText(Parse(result)["engine_semantics"]));
    }

    [TestMethod]
    public void TheIdentityStringListsTheSetAndEveryNonDefaultOption() {
        var set = Basic().Load();
        var head = "msfinder.diagnostic_rules?engine=msfinder-diagnostic-rules/1";
        var tail = "ruleset=0.0.1-inline&ruleset_sha256=" + set.Identity.Sha256;
        Assert.AreEqual(head + "&" + tail, new DiagnosticRuleEvaluator(set, new DiagnosticEvaluationOptions()).IdentityString);
        // Strict and Verbose change no evaluation and are not in the identity, only in the options.
        var plain = new DiagnosticRuleEvaluator(set, new DiagnosticEvaluationOptions { Strict = true, Verbose = true });
        Assert.AreEqual(head + "&" + tail, plain.IdentityString);
        Assert.AreEqual("strict=true verbose=true", string.Join(" ", plain.Evaluate(Firing()).Options.Select(o => o.Key + "=" + o.Value)));

        var every = new DiagnosticEvaluationOptions
        {
            Filter = RuleFilter.All, PrecursorToleranceDa = 0.02, ProductToleranceOverride = ProductTolerance.FromPpm(20.0),
            MsfinderProductTolerance = ProductTolerance.Da(0.05), LossReferenceOverride = Model.LossReference.Observed, DecoyShiftDa = -7,
            FloorScale = 0.5, KOffset = -1, Overlap = OverlapMode.Runtime, AssignmentCap = 100,
        };
        var evaluator = new DiagnosticRuleEvaluator(set, every);
        // Every key sorted ordinally, engine among them.
        Assert.AreEqual("msfinder.diagnostic_rules?assignment_cap=100&decoy_shift_da=-7&engine=msfinder-diagnostic-rules/1"
            + "&filter=all&floor_scale=0.500000&k_offset=-1&loss_reference_override=observed"
            + "&msfinder_product_tolerance.abs_da=0.050000&msfinder_product_tolerance.ppm=0.000000&overlap=runtime"
            + "&precursor_tolerance_da=0.020000&product_tolerance_override.abs_da=0.000000&product_tolerance_override.ppm=20.000000&"
            + tail, evaluator.IdentityString);
        // The result carries the same identity and the options as a typed object.
        var json = Parse(evaluator.Evaluate(Firing()));
        Assert.AreEqual(evaluator.IdentityString, ((JsonString)json["identity"]).Value);
        Assert.AreEqual("{\"assignment_cap\":100,\"decoy_shift_da\":-7,\"filter\":\"all\",\"floor_scale\":0.5,\"k_offset\":-1,"
                        + "\"loss_reference_override\":\"observed\",\"msfinder_product_tolerance.abs_da\":0.05,\"msfinder_product_tolerance.ppm\":0.0,"
                        + "\"overlap\":\"runtime\",\"precursor_tolerance_da\":0.02,\"product_tolerance_override.abs_da\":0.0,"
                        + "\"product_tolerance_override.ppm\":20.0}", CanonicalJson.ToText(json["options"]));
        StringAssert.Contains(ResultText(evaluator.Evaluate(Firing())), "\"options\":{\"assignment_cap\":100,\"decoy_shift_da\":-7,\"filter\":\"all\",\"floor_scale\":0.500000,");
    }

    [TestMethod]
    public void TheOptionsAreSnapshotAtConstruction() {
        var options = AllRules();
        var evaluator = new DiagnosticRuleEvaluator(Basic().Load(), options);
        var before = ResultText(evaluator.Evaluate(Firing()));
        var identity = evaluator.IdentityString;
        options.Filter = RuleFilter.MeasuredAndSourceChecked;
        options.FloorScale = 100.0;
        options.Verbose = true;
        options.KOffset = 5;
        Assert.AreEqual(before, ResultText(evaluator.Evaluate(Firing())));
        Assert.AreEqual(identity, evaluator.IdentityString);
        Assert.IsTrue(Outcome(evaluator.Evaluate(Firing())).Fired);
    }

    [TestMethod]
    public void TheFilterListsTheOtherRulesAsNotMeasured() {
        const string Reviewed = "flavonoid.reviewed.neg", Measured = "flavonoid.measured.neg", Plain = "flavonoid.plain.neg";
        var s = Basic(Plain).AddBasic(Measured).Measured(Measured).AddBasic(Reviewed).Reviewed(Reviewed);
        var set = s.Load();
        string Run(RuleFilter filter) {
            var r = new DiagnosticRuleEvaluator(set, new DiagnosticEvaluationOptions { Filter = filter }).Evaluate(Firing());
            return r.RulesEvaluated + " [" + string.Join(" ", r.NotMeasured) + "] [" + string.Join(" ", r.Outcomes.Select(o => o.RuleId)) + "]";
        }
        Assert.AreEqual("2 [flavonoid.plain.neg] [flavonoid.measured.neg flavonoid.reviewed.neg]", Run(RuleFilter.MeasuredAndSourceChecked));
        Assert.AreEqual("1 [flavonoid.measured.neg flavonoid.plain.neg] [flavonoid.reviewed.neg]", Run(RuleFilter.AuthorReviewed));
        Assert.AreEqual("3 [] [flavonoid.measured.neg flavonoid.plain.neg flavonoid.reviewed.neg]", Run(RuleFilter.All));
        var json = Parse(new DiagnosticRuleEvaluator(set, new DiagnosticEvaluationOptions()).Evaluate(Firing()));
        Assert.AreEqual("[\"flavonoid.plain.neg\"]", CanonicalJson.ToText(json["not_measured"]));
        Assert.AreEqual("{}", CanonicalJson.ToText(json["options"]));
    }

    /// <summary>The caffeoyl-hexose rule whose loss and ion coincide without a complementary declaration.</summary>
    private static InlineRuleSet Undeclared() {
        const string Rid = "hydroxycinnamoyl.caffeoyl_hexose.neg";
        return Basic().AddRule(Rid, Terms(Term("loss_hex", "loss_from_precursor", "required", Formula("C6H10O5")),
                                          Term("caffeate", "product_ion", "required", Formula("C9H7O4"))),
                               AllNode(TermRef("loss_hex"), TermRef("caffeate")), InlineRuleSet.Composition("C9H8O4", null, "{\"Hex\":[1,1]}", "{}"),
                               "[\"[M-H]-\"]", "caffeate");
    }

    [TestMethod]
    public void ARefusedRuleIsListedInEveryResultAndNotEvaluated() {
        var evaluator = Evaluator(Undeclared(), AllRules(verbose: true));
        var refusal = evaluator.Refusals.Single();
        Assert.AreEqual("hydroxycinnamoyl.caffeoyl_hexose.neg", refusal.RuleId);
        var result = evaluator.Evaluate(Firing());
        Assert.AreEqual(1, result.RulesEvaluated);
        Assert.AreSame(evaluator.Refusals, result.Refusals);
        Assert.AreEqual(RuleId, result.Outcomes.Single().RuleId);
        Assert.AreEqual("[{\"code\":\"overlap_undeclared\",\"delta_da\":0.0,\"hypothesis_id\":\"core|Hex1|[M-H]-\",\"items\":[\"loss_hex:C6H10O5\","
                        + "\"caffeate:C9H7O4\"],\"rule_id\":\"hydroxycinnamoyl.caffeoyl_hexose.neg\"}]", CanonicalJson.ToText(Parse(result)["refusals"]));
        StringAssert.Contains(ResultText(result), "\"delta_da\":0.000000");
        // A status that is not evaluated still lists the refusals.
        var none = evaluator.Evaluate(new DiagnosticSpectrum(null!, double.NaN, null, null!));
        Assert.AreEqual(1, none.Refusals.Count);
        Assert.AreEqual(1, none.RulesEvaluated);
        // Runtime refuses nothing and evaluates both.
        var runtime = Evaluator(Undeclared(), new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Overlap = OverlapMode.Runtime });
        Assert.AreEqual(0, runtime.Refusals.Count);
        Assert.AreEqual(2, runtime.Evaluate(Firing()).RulesEvaluated);
    }

    private const string ResultKeys =
        "adduct_tier base_intensity eligible_rule_conflict engine_semantics fired_motifs identity input_sha256 instrument_class "
        + "instrument_type n_peaks_dropped n_peaks_in narrowest_motifs not_measured options outcomes precondition_mode precursor_mz "
        + "refusals rule_conflict rule_set rules_evaluated status";

    private const string OutcomeKeys =
        "all author_reviewed branch_fired chosen claim_eligible claim_ineligible claim_level convention convention_label fired "
        + "loss_reference motif_id n_distinct_formulas n_hypotheses n_hypotheses_fired parameters_origin precondition_failure "
        + "precursor_tolerance_da product_tolerance rule_id source_checked tied_hypothesis_ids variant_single_ion version veto_blocked "
        + "within_calibration";

    private const string HypothesisKeys =
        "adduct adduct_source adduct_tier branches composition_id failure fired fired_ignoring_vetoes hypothesis_id hypothesis_mz k_of "
        + "matched neutral_formula precursor_error_da scaffold_id score units upgrades vetoes";

    private const string MatchedKeys =
        "error_da error_ppm ion_formula kind member_key peak_index peak_mz rel_intensity role target_mz term_id unit_id";

    [TestMethod]
    public void TheResultSchemaIsClosed() {
        // all(Y0, any(X, Z), k_of(1 of X2), not(w)) plus an upgrade U: every kind of record appears.
        var terms = Terms(Y0, X, Term("Z", "product_ion", "required", Formula("C8H5O4")), Term("X2", "product_ion", "counted", Formula("C9H5O4")),
                          Veto, Term("U", "product_ion", "upgrade", Formula("C10H9O4")));
        var logic = AllNode(TermRef("Y0"), AnyNode(TermRef("X"), TermRef("Z")), KOf("1", TermRef("X2")), NotNode(TermRef("w")));
        var set = new InlineRuleSet().AddRule(RuleId, terms, logic, CoreHex, "[\"[M-H]-\"]", "Y0");
        var spectrum = SpectrumNeg(Neg(Y0Ion, 800), Neg(XIon, 500), Neg("C9H5O4", 300), Neg("C10H9O4", 200), Neg(VetoIon, 10));
        var result = Evaluator(set, AllRules(verbose: true)).Evaluate(spectrum);
        var json = Parse(result);
        Assert.AreEqual(ResultKeys, Keys(json));
        Assert.AreEqual("pinned sha256 source version", Keys(json["rule_set"]));
        var outcome = (JsonObject)((JsonArray)json["outcomes"]).Single();
        Assert.AreEqual(OutcomeKeys, Keys(outcome));
        Assert.AreEqual("abs_da ppm source", Keys(outcome["product_tolerance"]));
        var chosen = (JsonObject)outcome["chosen"];
        Assert.AreEqual(HypothesisKeys, Keys(chosen));
        Assert.AreEqual(MatchedKeys, Keys(((JsonArray)chosen["matched"])[0]));
        Assert.AreEqual(MatchedKeys, Keys(((JsonArray)chosen["upgrades"])[0]));
        Assert.AreEqual("fired i_a i_b_max peak_index peak_mz term_id", Keys(((JsonArray)chosen["vetoes"])[0]));
        Assert.AreEqual("count k pointer satisfied", Keys(((JsonArray)chosen["k_of"])[0]));
        Assert.AreEqual("children pointer", Keys(((JsonArray)chosen["branches"])[0]));
        Assert.AreEqual(HypothesisKeys, Keys(((JsonArray)outcome["all"]).Single()));

        // The values of that one hypothesis.
        Assert.AreEqual("{\"/logic/all/1\":[true,false]}", CanonicalJson.ToText(outcome["branch_fired"]));
        Assert.AreEqual("[{\"children\":[true,false],\"pointer\":\"/logic/all/1\"}]", CanonicalJson.ToText(chosen["branches"]));
        Assert.AreEqual("[{\"count\":1,\"k\":1,\"pointer\":\"/logic/all/2\",\"satisfied\":true}]", CanonicalJson.ToText(chosen["k_of"]));
        Assert.AreEqual("{\"Hex\":1}", CanonicalJson.ToText(chosen["units"]));
        Assert.AreEqual("[\"core|Hex1|[M-H]-\"]", CanonicalJson.ToText(outcome["tied_hypothesis_ids"]));
        var o = result.Outcomes.Single();
        Assert.AreEqual("p2 exact 0.005 10 p2 0.01 hypothesis class as_published", string.Join(" ", o.Convention, o.ConventionLabel,
            o.ProductToleranceAbsDa.ToString(CultureInfo.InvariantCulture), o.ProductTolerancePpm.ToString(CultureInfo.InvariantCulture),
            o.ProductToleranceSource, o.PrecursorToleranceDa.ToString(CultureInfo.InvariantCulture), o.LossReference, o.ClaimLevel, o.ParametersOrigin));
        var h = o.Chosen!;
        Assert.AreSame(h, o.All.Single());
        Assert.AreEqual("Y0 X X2 ", string.Concat(h.Matched.Select(m => m.TermId + " ")));
        Assert.AreEqual("0 1 3", string.Join(" ", h.Matched.Select(m => m.UnitId))); // Z (unit 2) has no peak
        var y0 = h.Matched[0];
        Assert.AreEqual("aglycone_ion required C15H9O5 1", string.Join(" ", y0.Kind, y0.Role, y0.IonFormula, y0.PeakIndex));
        Assert.AreEqual(0.8, y0.RelIntensity);
        var u = h.Upgrades.Single();
        Assert.AreEqual("U upgrade 4", u.TermId + " " + u.Role + " " + u.PeakIndex);
        Assert.IsNull(u.UnitId);
        var w = h.Vetoes.Single();
        Assert.IsFalse(w.Fired); // rel 0.01 is below the 0.05 floor
        Assert.AreEqual(0.01, w.IA);
        Assert.AreEqual(5, w.PeakIndex);
        Assert.AreEqual(0.8 + 0.5 + 0.3, h.Score);
    }

    [TestMethod]
    public void NumbersAreExactDecimalText() {
        // Y0 0.0015 Da above its target: error_da and error_ppm keep their decimals, rel and score six.
        var target = IonNeg(Y0Ion);
        var spectrum = SpectrumNeg((target + 0.0015, 800), Neg(XIon, 500));
        var result = Evaluator(Basic()).Evaluate(spectrum);
        var json = ResultText(result);
        var y0 = Outcome(result).Chosen!.Matched[0];
        Assert.AreEqual(target + 0.0015 - target, y0.ErrorDa);
        StringAssert.Contains(json, "\"error_da\":" + ExactDecimal.Format(y0.ErrorDa, 6) + ",\"error_ppm\":" + ExactDecimal.Format(y0.ErrorDa / target * 1000000.0, 3) + ",");
        StringAssert.Contains(json, "\"error_da\":0.001500,\"error_ppm\":5.575,");
        StringAssert.Contains(json, "\"rel_intensity\":0.800000,");
        StringAssert.Contains(json, "\"score\":1.300000,");
        StringAssert.Contains(json, "\"target_mz\":" + ExactDecimal.Format(target, 6) + ",");
        StringAssert.Contains(json, "\"precursor_mz\":" + ExactDecimal.Format(PrecursorNeg, 6) + ",");
        StringAssert.Contains(json, "\"base_intensity\":1000.000000,");
        StringAssert.Contains(json, "\"product_tolerance\":{\"abs_da\":0.005000,\"ppm\":10.000,\"source\":\"p2\"}");
        StringAssert.Contains(json, "\"precursor_tolerance_da\":0.010000,");
        // An exact match has error 0 without a sign, whatever the rounding.
        StringAssert.Contains(json, "\"error_da\":0.000000,\"error_ppm\":0.000,");
        Assert.IsFalse(json.Contains("-0.000"));
        // The bytes are ASCII and the separators compact.
        Assert.IsTrue(DiagnosticResultJson.ToBytes(result).All(b => b < 0x80));
        Assert.IsFalse(json.Contains(": ") || json.Contains(", "));
    }

    [TestMethod]
    public void InstrumentTypeIsEscapedAsPythonEnsureAscii() {
        var text = "Q\nTOF \u00e9\u007f\U0001F600\"\\";
        var result = Evaluator(Basic()).Evaluate(WithInstrument(Firing(), text));
        StringAssert.Contains(ResultText(result), "\"instrument_type\":\"Q\\nTOF \\u00e9\\u007f\\ud83d\\ude00\\\"\\\\\"");
        Assert.AreEqual("QTOF-type", result.InstrumentClass);
        StringAssert.Contains(ResultText(Evaluator(Basic()).Evaluate(WithInstrument(Firing(), null!))), "\"instrument_class\":\"unknown\",\"instrument_type\":null,");
    }

    /// <summary>The design's definition, built here by hand from BitConverter and a culture-free format.</summary>
    private static string ExpectedInputSha(IEnumerable<(double Mz, double I)> peaks, double precursor, string? mode, string? instrument) {
        static string Bits(double x) => double.IsNaN(x) ? "7ff8000000000000" : ((ulong)BitConverter.DoubleToInt64Bits(x)).ToString("x16", CultureInfo.InvariantCulture);
        var text = "{\"instrument_type\":" + (instrument is null ? "null" : CanonicalJson.EscapeString(instrument)) + ",\"ion_mode\":"
                   + (mode is null ? "null" : "\"" + mode + "\"") + ",\"peaks\":["
                   + string.Join(",", peaks.Select(p => "[\"" + Bits(p.Mz) + "\",\"" + Bits(p.I) + "\"]")) + "],\"precursor_mz_bits\":\"" + Bits(precursor) + "\"}";
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(Encoding.ASCII.GetBytes(text)).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
    }

    [TestMethod]
    public void InputSha256HashesTheFourInputsInInputOrder() {
        var evaluator = Evaluator(Basic());
        // Dropped peaks (NaN, -0.0, zero intensity) are still hashed, in input order; NaN is canonical.
        var peaks = new[] { (300.0, 1000.0), (double.NaN, 5.0), (151.0, -0.0), (-0.0, 0.0), (IonNeg(Y0Ion), 800.0), (double.PositiveInfinity, -double.NaN) };
        var spectrum = new DiagnosticSpectrum(peaks.Select(p => new DiagnosticPeak(p.Item1, p.Item2)).ToList(), PrecursorNeg, DiagnosticIonMode.Negative, "QTOF");
        var result = evaluator.Evaluate(spectrum);
        Assert.AreEqual(ExpectedInputSha(peaks, PrecursorNeg, "negative", "QTOF"), result.InputSha256);
        Assert.AreEqual(6, result.NPeaksIn);
        Assert.AreEqual(4, result.NPeaksDropped);
        // The order matters; null mode and instrument are null; a NaN precursor is canonical too.
        var reversed = new DiagnosticSpectrum(spectrum.Peaks.Reverse().ToList(), PrecursorNeg, DiagnosticIonMode.Negative, "QTOF");
        Assert.AreNotEqual(result.InputSha256, evaluator.Evaluate(reversed).InputSha256);
        var bare = new DiagnosticSpectrum(new DiagnosticPeak[0], -double.NaN, null, null!);
        Assert.AreEqual(ExpectedInputSha(new (double, double)[0], double.NaN, null, null), evaluator.Evaluate(bare).InputSha256);
        Assert.AreEqual(ExpectedInputSha(new[] { (1.0, 2.0) }, -0.0, "positive", "x\u00e9"),
                        evaluator.Evaluate(new DiagnosticSpectrum(new[] { new DiagnosticPeak(1.0, 2.0) }, -0.0, DiagnosticIonMode.Positive, "x\u00e9")).InputSha256);
        // An ion mode outside the enum is hashed as null, never folded into "negative".
        var undefined = new DiagnosticSpectrum(spectrum.Peaks, PrecursorNeg, (DiagnosticIonMode)2, "QTOF");
        var undefinedResult = evaluator.Evaluate(undefined);
        Assert.AreEqual("unsupported_ion_mode", undefinedResult.Status);
        Assert.AreEqual(ExpectedInputSha(peaks, PrecursorNeg, null, "QTOF"), undefinedResult.InputSha256);
        Assert.AreNotEqual(result.InputSha256, undefinedResult.InputSha256);
        // A known value, so a port can check its implementation without this code.
        Assert.AreEqual(ExpectedInputSha(new[] { (100.0, 1.0) }, 200.0, "negative", "QTOF"),
                        evaluator.Evaluate(new DiagnosticSpectrum(new[] { new DiagnosticPeak(100.0, 1.0) }, 200.0, DiagnosticIonMode.Negative, "QTOF")).InputSha256);
    }

    [TestMethod]
    public void CompactOutputListsOnlyRulesWithHypotheses() {
        // A positive rule is not tried on a negative spectrum: no outcome in compact output, an outcome with
        // precondition_failure ion_mode in verbose output.
        var s = Basic().AddBasic("flavonoid.o_glycoside.pos");
        var compact = Evaluator(s).Evaluate(Firing());
        Assert.AreEqual(RuleId, compact.Outcomes.Single().RuleId);
        Assert.AreEqual(2, compact.RulesEvaluated);
        Assert.AreEqual("[]", CanonicalJson.ToText(((JsonObject)((JsonArray)Parse(compact)["outcomes"])[0])["all"]));
        var verbose = Evaluator(s, AllRules(verbose: true)).Evaluate(Firing());
        CollectionAssert.AreEqual(new[] { RuleId, "flavonoid.o_glycoside.pos" }, verbose.Outcomes.Select(o => o.RuleId).ToArray());
        var pos = verbose.Outcomes[1];
        Assert.AreEqual(RuleOutcome.PreconditionIonMode, pos.PreconditionFailure);
        Assert.AreEqual(0, pos.NHypotheses);
        Assert.IsNull(pos.Chosen);
        Assert.AreEqual(0, pos.All.Count);
        Assert.IsNull(verbose.Outcomes[0].PreconditionFailure);
        Assert.AreEqual(1, verbose.Outcomes[0].All.Count);
        Assert.AreSame(verbose.Outcomes[0].Chosen, verbose.Outcomes[0].All[0]);
    }

    [TestMethod]
    public void UpgradesAreReportedForTheChosenHypothesisOnly() {
        // genistein and apigenin share a formula: two hypotheses fire with the same score; only the chosen one
        // (apigenin, the lower id) reports the upgrade.
        const string Rid = "flavonoid.series.neg";
        var terms = Terms(Y0, Term("U", "product_ion", "upgrade", Formula("C10H9O4")));
        var set = new InlineRuleSet().AddRule(Rid, terms, AllOf(new[] { "Y0" }),
                                              InlineRuleSet.Composition(null, "aglycones", "{\"Hex\":[1,1]}", "{}"), "[\"[M-H]-\"]", "Y0");
        var o = Outcome(Evaluator(set, AllRules(verbose: true)).Evaluate(SpectrumNeg(Neg(Y0Ion, 800), Neg("C10H9O4", 200))), Rid);
        CollectionAssert.AreEqual(new[] { "apigenin|Hex1|[M-H]-", "genistein|Hex1|[M-H]-" }, Ids(o.All).ToArray());
        Assert.AreSame(o.Chosen, o.All[0]);
        Assert.AreEqual(1, o.All[0].Upgrades.Count);
        Assert.AreEqual(0, o.All[1].Upgrades.Count);
        Assert.IsTrue(o.All[1].Fired);
    }
}
