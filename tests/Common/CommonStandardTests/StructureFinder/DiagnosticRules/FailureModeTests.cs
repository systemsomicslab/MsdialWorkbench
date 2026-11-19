#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EvaluatorHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Failure modes (design 4.4, 4.6, 4.10, 7.1): every spectrum status and its precedence, every hypothesis failure, the
/// Strict refusal exception, AdductEvidence(Assumed) refused, and invalid options refused at construction (OQ20).
/// </summary>
[TestClass]
public class FailureModeTests
{
    private static DiagnosticSpectrum Spectrum(double precursor, DiagnosticIonMode? mode, params DiagnosticPeak[] peaks) =>
        new DiagnosticSpectrum(peaks, precursor, mode, "QTOF");

    private static readonly DiagnosticPeak Usable = new DiagnosticPeak(300.0, 1000.0);

    private static string Status(DiagnosticSpectrum s) => Evaluator(Basic()).Evaluate(s).Status;

    [TestMethod]
    public void EveryStatusInPrecedenceOrder() {
        Assert.AreEqual("evaluated", Status(Spectrum(PrecursorNeg, DiagnosticIonMode.Negative, Usable)));
        foreach (var p in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, 0.0, -0.0, -431.0 }) {
            Assert.AreEqual("no_precursor", Status(Spectrum(p, DiagnosticIonMode.Negative, Usable)));
            // no_precursor before unsupported_ion_mode and no_peaks.
            Assert.AreEqual("no_precursor", Status(Spectrum(p, null)));
        }
        Assert.AreEqual("unsupported_ion_mode", Status(Spectrum(PrecursorNeg, null, Usable)));
        Assert.AreEqual("unsupported_ion_mode", Status(Spectrum(PrecursorNeg, null))); // before no_peaks
        Assert.AreEqual("no_peaks", Status(Spectrum(PrecursorNeg, DiagnosticIonMode.Negative)));
        Assert.AreEqual("no_peaks", Status(new DiagnosticSpectrum(null!, PrecursorNeg, DiagnosticIonMode.Positive, "QTOF")));
        // Every peak unusable: non-finite, non-positive m/z or intensity.
        Assert.AreEqual("no_peaks", Status(Spectrum(PrecursorNeg, DiagnosticIonMode.Negative, new DiagnosticPeak(double.NaN, 1.0),
            new DiagnosticPeak(100.0, 0.0), new DiagnosticPeak(0.0, 5.0), new DiagnosticPeak(-1.0, 5.0), new DiagnosticPeak(100.0, -2.0),
            new DiagnosticPeak(double.PositiveInfinity, 1.0), new DiagnosticPeak(100.0, double.NaN), new DiagnosticPeak(100.0, double.PositiveInfinity))));
    }

    [TestMethod]
    public void ANonEvaluatedResultIsEmptyButComplete() {
        var evaluator = Evaluator(Basic(), AllRules(verbose: true));
        var r = evaluator.Evaluate(Spectrum(double.NaN, DiagnosticIonMode.Negative, Usable, new DiagnosticPeak(0.0, 1.0)));
        Assert.AreEqual(DiagnosticRuleResult.StatusNoPrecursor, r.Status);
        Assert.AreEqual(0, r.Outcomes.Count);
        Assert.AreEqual(0, r.FiredMotifs.Count);
        Assert.IsNull(r.BaseIntensity);
        Assert.AreEqual(1, r.RulesEvaluated);
        Assert.AreEqual(2, r.NPeaksIn);
        Assert.AreEqual(1, r.NPeaksDropped);
        var json = ResultText(r);
        StringAssert.Contains(json, "\"precursor_mz\":null,");
        StringAssert.Contains(json, "\"base_intensity\":null,");
        StringAssert.Contains(json, "\"outcomes\":[],");
        StringAssert.Contains(json, "\"status\":\"no_precursor\"}");
        // Infinite precursors are null too; a negative finite one keeps its value.
        StringAssert.Contains(ResultText(evaluator.Evaluate(Spectrum(double.NegativeInfinity, null))), "\"precursor_mz\":null,");
        StringAssert.Contains(ResultText(evaluator.Evaluate(Spectrum(-431.5, null))), "\"precursor_mz\":-431.500000,");
        var mode = evaluator.Evaluate(Spectrum(PrecursorNeg, null, Usable));
        Assert.AreEqual(DiagnosticRuleResult.StatusUnsupportedIonMode, mode.Status);
        Assert.AreEqual(0, mode.Outcomes.Count);
        Assert.IsNull(mode.BaseIntensity);
    }

    private static HypothesisOutcome Only(DiagnosticRuleResult r, string ruleId = RuleId) => Outcome(r, ruleId).All.Single();

    [TestMethod]
    public void LogicFalse() {
        var h = Only(Evaluator(Basic(), AllRules(verbose: true)).Evaluate(SpectrumNeg(Neg(Y0Ion, 800))));
        Assert.AreEqual(HypothesisOutcome.FailureLogicFalse, h.Failure);
        Assert.IsFalse(h.Fired);
        Assert.IsFalse(h.FiredIgnoringVetoes);
        StringAssert.Contains(ResultText(Evaluator(Basic(), AllRules(verbose: true)).Evaluate(SpectrumNeg(Neg(Y0Ion, 800)))), "\"failure\":\"logic_false\",");
    }

    [TestMethod]
    public void Veto() {
        var h = Only(Evaluator(Basic(withVeto: true), AllRules(verbose: true)).Evaluate(SpectrumNeg(Neg(Y0Ion, 800), Neg(XIon, 500), Neg(VetoIon, 200))));
        Assert.AreEqual(HypothesisOutcome.FailureVeto, h.Failure);
        Assert.IsTrue(h.FiredIgnoringVetoes);
        Assert.IsTrue(h.Vetoes.Single().Fired);
    }

    [TestMethod]
    public void NoBasePeak() {
        // Every usable peak lies within 1.0 Da of the precursor: p2 has no base peak, the hypothesis still counts.
        var spectrum = Spectrum(PrecursorNeg, DiagnosticIonMode.Negative, new DiagnosticPeak(PrecursorNeg, 1000.0), new DiagnosticPeak(PrecursorNeg - 0.9, 50.0));
        var r = Evaluator(Basic(withVeto: true), AllRules(verbose: true)).Evaluate(spectrum);
        Assert.AreEqual("evaluated", r.Status);
        Assert.IsNull(r.BaseIntensity);
        var o = Outcome(r);
        Assert.AreEqual(1, o.NHypotheses);
        Assert.IsFalse(o.Fired);
        var h = o.All.Single();
        Assert.AreEqual(HypothesisOutcome.FailureNoBasePeak, h.Failure);
        Assert.AreEqual(0.0, h.Score);
        Assert.AreEqual(0, h.Matched.Count);
        Assert.AreEqual(0, h.Vetoes.Single().IA);
        Assert.IsNull(h.Vetoes.Single().PeakIndex);
        StringAssert.Contains(ResultText(r), "\"base_intensity\":null,");
        // An msdial rule has its reference (the maximum) and is evaluated.
        var msdial = Evaluator(Basic().SetRule(RuleId, "convention", "\"msdial\""), AllRules(verbose: true)).Evaluate(spectrum);
        Assert.AreEqual(HypothesisOutcome.FailureLogicFalse, Only(msdial).Failure);
        Assert.AreEqual("msdial approximate", Outcome(msdial).Convention + " " + Outcome(msdial).ConventionLabel);
    }

    [TestMethod]
    public void AssignmentOverflow() {
        // caffeoyl hexose without the complementary declaration under Runtime: two conflicted items, three leaves.
        const string Rid = "hydroxycinnamoyl.caffeoyl_hexose.neg";
        var set = new InlineRuleSet().AddRule(Rid, Terms(Term("loss_hex", "loss_from_precursor", "counted", Formula("C6H10O5")),
                                                         Term("caffeate", "product_ion", "counted", Formula("C9H7O4")),
                                                         Term("decarb", "product_ion", "counted", Formula("C8H7O2"))),
                                              KOf("2", TermRef("loss_hex"), TermRef("caffeate"), TermRef("decarb")),
                                              InlineRuleSet.Composition("C9H8O4", null, "{\"Hex\":[1,1]}", "{}"), "[\"[M-H]-\"]", "caffeate");
        var precursor = TestMass.Ion(TestMass.Of("C15H18O9") - TestMass.Of("H"), -1);
        var spectrum = MakeSpectrum(precursor, DiagnosticIonMode.Negative, Neg("C9H7O4", 600), Neg("C8H7O2", 300));
        var over = Outcome(Evaluator(set, new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Overlap = OverlapMode.Runtime, AssignmentCap = 2 })
                           .Evaluate(spectrum), Rid);
        Assert.IsFalse(over.Fired);
        Assert.IsNull(over.Chosen); // neither fired nor veto-blocked
        var verbose = Outcome(Evaluator(set, new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Overlap = OverlapMode.Runtime, AssignmentCap = 2, Verbose = true })
                              .Evaluate(spectrum), Rid);
        Assert.AreEqual(HypothesisOutcome.FailureAssignmentOverflow, verbose.All.Single().Failure);
        Assert.IsTrue(Outcome(Evaluator(set, new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Overlap = OverlapMode.Runtime, AssignmentCap = 3 })
                              .Evaluate(spectrum), Rid).Fired);
    }

    [TestMethod]
    public void StrictTurnsARefusalIntoAnException() {
        const string Rid = "hydroxycinnamoyl.caffeoyl_hexose.neg";
        var set = new InlineRuleSet().AddRule(Rid, Terms(Term("loss_hex", "loss_from_precursor", "required", Formula("C6H10O5")),
                                                         Term("caffeate", "product_ion", "required", Formula("C9H7O4"))),
                                              AllNode(TermRef("loss_hex"), TermRef("caffeate")),
                                              InlineRuleSet.Composition("C9H8O4", null, "{\"Hex\":[1,1]}", "{}"), "[\"[M-H]-\"]", "caffeate").Load();
        var thrown = Assert.ThrowsException<DiagnosticRuleRefusedException>(
            () => new DiagnosticRuleEvaluator(set, new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Strict = true }));
        Assert.AreEqual(Rid, thrown.Refusals.Single().RuleId);
        Assert.AreEqual(RuleRefusal.OverlapUndeclared, thrown.Refusals.Single().Code);
        // Without Strict it is reported; outside the filter it is not checked at all.
        Assert.AreEqual(1, new DiagnosticRuleEvaluator(set, new DiagnosticEvaluationOptions { Filter = RuleFilter.All }).Refusals.Count);
        Assert.AreEqual(0, new DiagnosticRuleEvaluator(set, new DiagnosticEvaluationOptions { Strict = true }).Refusals.Count);
        Assert.AreEqual(0, new DiagnosticRuleEvaluator(set, new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Strict = true, Overlap = OverlapMode.Runtime }).Refusals.Count);
    }

    [TestMethod]
    public void AssumedEvidenceIsRefused() {
        Assert.ThrowsException<ArgumentException>(() => new AdductEvidence("[M-H]-", AdductTier.Assumed));
        Assert.ThrowsException<ArgumentException>(() => new AdductEvidence("[M-H]-", (AdductTier)17));
        Assert.ThrowsException<ArgumentNullException>(() => new AdductEvidence(null!, AdductTier.Curated));
    }

    /// <summary>The invalid options by name. The data rows carry only the name: MSTest serialises dynamic data, which
    /// would lose the get-only fields of <see cref="ProductTolerance"/>.</summary>
    private static Dictionary<string, Func<DiagnosticEvaluationOptions>> Invalid() {
        var cases = new Dictionary<string, Func<DiagnosticEvaluationOptions>>(StringComparer.Ordinal);
        foreach (var x in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0.001 }) {
            var t = x.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            cases["precursor " + t] = () => new DiagnosticEvaluationOptions { PrecursorToleranceDa = x };
            cases["override abs " + t] = () => new DiagnosticEvaluationOptions { ProductToleranceOverride = new ProductTolerance(x, 0.0) };
            cases["override ppm " + t] = () => new DiagnosticEvaluationOptions { ProductToleranceOverride = new ProductTolerance(0.0, x) };
            cases["msfinder abs " + t] = () => new DiagnosticEvaluationOptions { MsfinderProductTolerance = new ProductTolerance(x, 0.0) };
            cases["msfinder ppm " + t] = () => new DiagnosticEvaluationOptions { MsfinderProductTolerance = ProductTolerance.FromPpm(x) };
            cases["floor " + t] = () => new DiagnosticEvaluationOptions { FloorScale = x };
        }
        cases["floor 0"] = () => new DiagnosticEvaluationOptions { FloorScale = 0.0 };
        cases["cap 0"] = () => new DiagnosticEvaluationOptions { AssignmentCap = 0 };
        cases["cap -1"] = () => new DiagnosticEvaluationOptions { AssignmentCap = -1 };
        cases["decoy 9008"] = () => new DiagnosticEvaluationOptions { DecoyShiftDa = 9008 };
        cases["decoy -9008"] = () => new DiagnosticEvaluationOptions { DecoyShiftDa = -9008 };
        cases["decoy min"] = () => new DiagnosticEvaluationOptions { DecoyShiftDa = int.MinValue };
        // delta * 10^12 wraps in long arithmetic to -2407403520 and +9842106368 pDa, both below 2^53.
        cases["decoy wrapping to -0.0024 Da"] = () => new DiagnosticEvaluationOptions { DecoyShiftDa = 1752440687 };
        cases["decoy wrapping to +0.0098 Da"] = () => new DiagnosticEvaluationOptions { DecoyShiftDa = 498062090 };
        cases["decoy max"] = () => new DiagnosticEvaluationOptions { DecoyShiftDa = int.MaxValue };
        cases["filter"] = () => new DiagnosticEvaluationOptions { Filter = (RuleFilter)9 };
        cases["overlap"] = () => new DiagnosticEvaluationOptions { Overlap = (OverlapMode)9 };
        cases["loss reference"] = () => new DiagnosticEvaluationOptions { LossReferenceOverride = (Model.LossReference)9 };
        return cases;
    }

    public static IEnumerable<object[]> InvalidOptions() => Invalid().Keys.Select(k => new object[] { k });

    [DataTestMethod]
    [DynamicData(nameof(InvalidOptions), DynamicDataSourceType.Method)]
    public void InvalidOptionsAreRefusedAtConstruction(string name) {
        var set = Basic().Load();
        Assert.ThrowsException<ArgumentException>(() => new DiagnosticRuleEvaluator(set, Invalid()[name]()), name);
    }

    [TestMethod]
    public void BoundaryOptionsAreAccepted() {
        var set = Basic().Load();
        foreach (var options in new[]
        {
            new DiagnosticEvaluationOptions { PrecursorToleranceDa = 0.0, ProductToleranceOverride = ProductTolerance.Da(0.0), AssignmentCap = 1 },
            new DiagnosticEvaluationOptions { DecoyShiftDa = 9007 }, new DiagnosticEvaluationOptions { DecoyShiftDa = -9007 },
            new DiagnosticEvaluationOptions { FloorScale = double.Epsilon },
        }) {
            options.Filter = RuleFilter.All;
            var r = new DiagnosticRuleEvaluator(set, options).Evaluate(Firing());
            Assert.AreEqual("evaluated", r.Status);
        }
    }

    [TestMethod]
    public void NullArgumentsAreRefused() {
        var set = Basic().Load();
        Assert.ThrowsException<ArgumentNullException>(() => new DiagnosticRuleEvaluator(null!, new DiagnosticEvaluationOptions()));
        Assert.ThrowsException<ArgumentNullException>(() => new DiagnosticRuleEvaluator(set, null!));
        var evaluator = new DiagnosticRuleEvaluator(set, AllRules());
        Assert.ThrowsException<ArgumentNullException>(() => evaluator.Evaluate(null!));
        Assert.ThrowsException<ArgumentNullException>(() => evaluator.Evaluate(null!, new AdductEvidence("[M-H]-", AdductTier.Curated)));
        Assert.ThrowsException<ArgumentNullException>(() => evaluator.Evaluate(Firing(), null!));
    }

    [TestMethod]
    public void AnOverflowingRelativeIntensitySaturates() {
        // A finite peak inside the precursor region can be far stronger than the p2 base: I / base overflows. It is
        // never a candidate, but a veto's I_raw sees it; the quotient saturates at double.MaxValue, so i_a stays finite
        // (design 4.11) and the veto still fires.
        var spectrum = Spectrum(TestMass.Da(TestMass.Of("C21H20O10") - TestMass.Of("H") + TestMass.Electron), DiagnosticIonMode.Negative,
                                new DiagnosticPeak(100.0, double.Epsilon));
        var veto = Term("w", "window", "veto_present", Window("C21H19O10", "ion", "0.02"));
        var set = new InlineRuleSet().AddRule(RuleId, Terms(Y0, veto), AllOf(new[] { "Y0" }, new[] { "w" }), CoreHex, "[\"[M-H]-\"]", "Y0");
        var precursorPeak = new DiagnosticPeak(spectrum.PrecursorMz, 1e300);
        var withPrecursor = new DiagnosticSpectrum(spectrum.Peaks.Concat(new[] { precursorPeak }).ToList(), spectrum.PrecursorMz, spectrum.IonMode, "QTOF");
        var r = Evaluator(set, AllRules(verbose: true)).Evaluate(withPrecursor);
        var h = Outcome(r).All.Single();
        Assert.AreEqual(double.MaxValue, h.Vetoes.Single().IA);
        Assert.IsTrue(h.Vetoes.Single().Fired);
        Assert.IsFalse(ResultText(r).Contains("\"i_a\":null"));

        // veto_stronger_than: both the veto and its than-term see the region peak; i_b_max saturates too.
        var than = TermThan("v", "window", "veto_stronger_than", Window("C21H19O10", "ion", "0.02"), new[] { "t" }, "0.1");
        var thanTerm = Term("t", "window", "veto_present", Window("C21H19O10", "ion", "0.03"));
        var stronger = new InlineRuleSet().AddRule(RuleId, Terms(Y0, than, thanTerm), AllOf(new[] { "Y0" }, new[] { "v", "t" }), CoreHex, "[\"[M-H]-\"]", "Y0");
        var s = Outcome(Evaluator(stronger, AllRules(verbose: true)).Evaluate(withPrecursor)).All.Single();
        foreach (var v in s.Vetoes) {
            Assert.AreEqual(double.MaxValue, v.IA, v.TermId);
        }
        Assert.AreEqual(double.MaxValue, s.Vetoes.Single(v => v.TermId == "v").IBMax);
    }

    [TestMethod]
    public void AnErrorPpmAtAZeroTargetSaturates() {
        // An observed-reference loss whose mass equals P_obs has the target 0.0; a peak at m/z 0.003 hits it (p2:
        // 0.005 Da at t = 0) and error_da / target overflows. error_ppm saturates and stays finite (design 4.11).
        var terms = Terms(
            Term("loss_hex", "loss_from_precursor", "counted", Formula("C6H10O5")),
            Term("caffeate", "product_ion", "counted", Formula("C9H7O4")),
            Term("decarb", "product_ion", "counted", Formula("C8H7O2")));
        var set = new InlineRuleSet().AddRule(RuleId, terms, KOf("2", TermRef("loss_hex"), TermRef("caffeate"), TermRef("decarb")),
                                              InlineRuleSet.Composition("C9H8O4", null, "{\"Hex\":[1,1]}", "{}"), "[\"[M-H]-\"]", "caffeate")
            .SetRule(RuleId, "complementary", "[[\"loss_hex\",\"caffeate\"]]");
        var options = AllRules(verbose: true);
        options.LossReferenceOverride = Model.LossReference.Observed;
        options.PrecursorToleranceDa = 200.0;
        options.Overlap = OverlapMode.Runtime; // the 200 Da observed-loss slack would refuse the rule
        var precursor = TestMass.Da(TestMass.Of("C6H10O5"));
        var r = Evaluator(set, options).Evaluate(Spectrum(precursor, DiagnosticIonMode.Negative, new DiagnosticPeak(0.003, 1000.0)));
        var loss = Outcome(r).All.Single().Matched.Single(m => m.TermId == "loss_hex");
        Assert.AreEqual(0.0, loss.TargetMz);
        Assert.AreEqual(0.003, loss.ErrorDa);
        Assert.AreEqual(double.MaxValue, loss.ErrorPpm);
        Assert.IsFalse(ResultText(r).Contains("\"error_ppm\":null"));
    }
}
