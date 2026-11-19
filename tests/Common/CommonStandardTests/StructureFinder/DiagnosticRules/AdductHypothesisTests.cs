#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EvaluatorHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Adduct tiers and claim eligibility through the public evaluator (design 4.3, 4.9, 7.1): no evidence is
/// adduct_ambiguous with every hypothesis assumed, trying the rule's adducts then the common ones; evidence is mz_only
/// at its tier; an adduct outside the rule is adduct_not_in_rule with no fallback; eligibility needs the review, the
/// instrument class, the primary mode, the reviewed tolerances and no variant option; the [M]+ flavylium precursor.
/// </summary>
[TestClass]
public class AdductHypothesisTests
{
    /// <summary>[M+HCOO]- of C21H20O10: a common adduct of the negative mode that the rule does not list.</summary>
    private static double Formate => TestMass.Da(TestMass.Of("C21H20O10") + TestMass.Of("CHO2") + TestMass.Electron);

    private static DiagnosticSpectrum At(double precursor) => MakeSpectrum(precursor, DiagnosticIonMode.Negative, Neg(Y0Ion, 800), Neg(XIon, 500));

    [TestMethod]
    public void WithoutEvidenceTheModeIsAdductAmbiguousAndEveryHypothesisAssumed() {
        var evaluator = Evaluator(Basic(), AllRules(verbose: true));
        var deprotonated = evaluator.Evaluate(Firing());
        Assert.AreEqual("adduct_ambiguous", deprotonated.PreconditionMode);
        Assert.AreEqual("assumed", deprotonated.AdductTier);
        var h = Outcome(deprotonated).Chosen!;
        Assert.AreEqual("[M-H]- rule assumed", h.Adduct + " " + h.AdductSource + " " + h.AdductTier);
        // The formate precursor is found through the common adduct, after the rule's own.
        var formate = evaluator.Evaluate(At(Formate));
        var f = Outcome(formate).All.Single();
        Assert.AreEqual("core|Hex1|[M+HCOO]- [M+HCOO]- common assumed", f.HypothesisId + " " + f.Adduct + " " + f.AdductSource + " " + f.AdductTier);
        Assert.IsTrue(f.Fired);
        StringAssert.Contains(ResultText(formate), "\"adduct_source\":\"common\",\"adduct_tier\":\"assumed\",");
    }

    [TestMethod]
    public void EvidenceIsMzOnlyAtItsTier() {
        var evaluator = Evaluator(Basic());
        foreach (var (tier, name) in new[] { (AdductTier.CrossPolarity, "cross_polarity"), (AdductTier.WithinPolarityPair, "within_polarity_pair"), (AdductTier.Curated, "curated") }) {
            var result = evaluator.Evaluate(Firing(), new AdductEvidence("[M-H]-", tier));
            Assert.AreEqual("mz_only", result.PreconditionMode);
            Assert.AreEqual(name, result.AdductTier);
            var h = Outcome(result).Chosen!;
            Assert.AreEqual(name, h.AdductTier);
            Assert.AreEqual("rule", h.AdductSource);
            StringAssert.Contains(ResultText(result), "\"adduct_tier\":\"" + name + "\",");
        }
        // Only the evidence adduct is tried: the formate precursor finds nothing under [M-H]-.
        Assert.AreEqual(0, evaluator.Evaluate(At(Formate), new AdductEvidence("[M-H]-", AdductTier.Curated)).Outcomes.Count);
    }

    [TestMethod]
    public void AnAdductOutsideTheRuleHasNoHypothesesAndNoFallback() {
        var verbose = Evaluator(Basic(), AllRules(verbose: true));
        foreach (var adduct in new[] { "[M+HCOO]-", "[M+Cl]-", "[M+X]-" }) {
            var result = verbose.Evaluate(At(Formate), new AdductEvidence(adduct, AdductTier.WithinPolarityPair));
            var o = Outcome(result);
            Assert.AreEqual(RuleOutcome.PreconditionAdductNotInRule, o.PreconditionFailure, adduct);
            Assert.AreEqual(0, o.NHypotheses);
            Assert.IsFalse(o.Fired);
            StringAssert.Contains(ResultText(result), "\"precondition_failure\":\"adduct_not_in_rule\",");
        }
        // Compact output has no outcome for it.
        Assert.AreEqual(0, Evaluator(Basic()).Evaluate(At(Formate), new AdductEvidence("[M+HCOO]-", AdductTier.Curated)).Outcomes.Count);
    }

    [TestMethod]
    public void AFlavyliumCationIsItsOwnPrecursor() {
        // Cyanidin 3-O-glucoside as the composition C21H21O11 under [M]+: the precursor is M - e = 449.107837.
        const string Rid = "anthocyanin.flavylium.pos";
        var set = new InlineRuleSet().AddRule(Rid, Terms(Term("agl", "product_ion", "required", Formula("C15H11O6"))), AllOf(new[] { "agl" }),
                                              InlineRuleSet.Composition("C21H21O11", null, "{}", "{}"), "[\"[M]+\"]", "agl");
        var aglycone = TestMass.Ion(TestMass.Of("C15H11O6"), 1);
        var spectrum = MakeSpectrum(449.107837, DiagnosticIonMode.Positive, (aglycone, 900));
        var result = Evaluator(set).Evaluate(spectrum, new AdductEvidence("[M]+", AdductTier.Curated));
        var h = Outcome(result, Rid).Chosen!;
        Assert.AreEqual("core||[M]+", h.HypothesisId);
        Assert.AreEqual("C21H21O11", h.NeutralFormula);
        Assert.AreEqual("449.107837", ExactDecimal.Format(h.HypothesisMz, 6));
        Assert.IsTrue(h.Fired);
        StringAssert.Contains(ResultText(result), "\"hypothesis_mz\":449.107837,");
        Assert.AreEqual(aglycone, h.Matched.Single().TargetMz);
    }

    // ------------------------------------------------------------------------------------------------ eligibility

    private static RuleOutcome Eligibility(InlineRuleSet set, DiagnosticEvaluationOptions? options = null, AdductEvidence? evidence = null,
                                           string instrument = "QTOF") {
        var evaluator = new DiagnosticRuleEvaluator(set.Load(), options ?? new DiagnosticEvaluationOptions());
        var spectrum = Firing(instrument);
        return Outcome(evidence is null ? evaluator.Evaluate(spectrum) : evaluator.Evaluate(spectrum, evidence));
    }

    private static string Reasons(RuleOutcome o) => (o.ClaimEligible ? "eligible" : "ineligible") + " [" + string.Join(" ", o.ClaimIneligible) + "]";

    [TestMethod]
    public void AReviewedRuleIsEligibleOnlyUnderItsReview() {
        var reviewed = Basic().Reviewed(RuleId);
        Assert.AreEqual("eligible []", Reasons(Eligibility(reviewed)));
        // The evaluation's mode must be the primary one (adduct_ambiguous here).
        Assert.AreEqual("ineligible [precondition_mode]", Reasons(Eligibility(reviewed, evidence: new AdductEvidence("[M-H]-", AdductTier.CrossPolarity))));
        // The instrument class must be one the review covers.
        Assert.AreEqual("ineligible [instrument_class]", Reasons(Eligibility(reviewed, instrument: "Q Exactive")));
        Assert.AreEqual("ineligible [instrument_class]", Reasons(Eligibility(reviewed, instrument: "")));
        // A rule reviewed for mz_only is eligible only with evidence.
        var mzOnly = Basic().SetRule(RuleId, "primary_precondition_mode", "\"mz_only\"").Reviewed(RuleId);
        Assert.AreEqual("ineligible [precondition_mode]", Reasons(Eligibility(mzOnly)));
        Assert.AreEqual("eligible []", Reasons(Eligibility(mzOnly, evidence: new AdductEvidence("[M-H]-", AdductTier.Curated))));
        // Several failures are all listed, in order.
        Assert.AreEqual("ineligible [instrument_class precondition_mode variant_active]",
                        Reasons(Eligibility(mzOnly, new DiagnosticEvaluationOptions { KOffset = 1 }, instrument: "Orbitrap")));
    }

    [TestMethod]
    public void AnUnreviewedRuleIsNotReviewedAndOtherwiseChecked() {
        // No review: instrument_class and tolerance_differs have nothing to compare with (OQ06); mode and variants do.
        Assert.AreEqual("ineligible [not_reviewed]", Reasons(Eligibility(Basic(), AllRules())));
        Assert.AreEqual("ineligible [not_reviewed precondition_mode variant_active]",
                        Reasons(Eligibility(Basic(), new DiagnosticEvaluationOptions { Filter = RuleFilter.All, FloorScale = 2.0 },
                                            new AdductEvidence("[M-H]-", AdductTier.Curated), "Orbitrap")));
    }

    [TestMethod]
    public void TheEffectiveTolerancesMustBeTheReviewedOnes() {
        const string MsfinderReview = "{\"abs_da\":0.01,\"ppm\":0.0,\"source\":\"msfinder\"}";
        var msfinder = Basic().SetRule(RuleId, "product_tolerance", "\"msfinder\"").Reviewed(RuleId, productTolerance: MsfinderReview);
        Assert.AreEqual("eligible []", Reasons(Eligibility(msfinder)));
        var o = Eligibility(msfinder);
        Assert.AreEqual("0.01 0 msfinder", o.ProductToleranceAbsDa.ToString(System.Globalization.CultureInfo.InvariantCulture) + " "
                        + o.ProductTolerancePpm.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + o.ProductToleranceSource);
        // MS-FINDER's Ms2Tolerance at 0.05 Da: not a variant, but not the reviewed tolerance.
        Assert.AreEqual("ineligible [tolerance_differs]",
                        Reasons(Eligibility(msfinder, new DiagnosticEvaluationOptions { MsfinderProductTolerance = ProductTolerance.Da(0.05) })));
        Assert.AreEqual("ineligible [tolerance_differs]",
                        Reasons(Eligibility(msfinder, new DiagnosticEvaluationOptions { MsfinderProductTolerance = ProductTolerance.FromPpm(10.0) })));
        // A p2 rule ignores MsfinderProductTolerance; reviewed under msfinder it is tolerance_differs.
        var p2 = Basic().Reviewed(RuleId);
        Assert.AreEqual("eligible []", Reasons(Eligibility(p2, new DiagnosticEvaluationOptions { MsfinderProductTolerance = ProductTolerance.Da(0.05) })));
        Assert.AreEqual("ineligible [tolerance_differs]", Reasons(Eligibility(Basic().Reviewed(RuleId, productTolerance: MsfinderReview))));
        // The precursor tolerance too, and an override is also a variant even at the reviewed value.
        Assert.AreEqual("ineligible [tolerance_differs]", Reasons(Eligibility(Basic().Reviewed(RuleId, precursorTolerance: "0.02"))));
        Assert.AreEqual("ineligible [variant_active]", Reasons(Eligibility(p2, new DiagnosticEvaluationOptions { PrecursorToleranceDa = 0.01 })));
        Assert.AreEqual("ineligible [tolerance_differs variant_active]",
                        Reasons(Eligibility(p2, new DiagnosticEvaluationOptions { PrecursorToleranceDa = 0.02 })));
        Assert.AreEqual("ineligible [tolerance_differs variant_active]",
                        Reasons(Eligibility(p2, new DiagnosticEvaluationOptions { ProductToleranceOverride = new ProductTolerance(0.005, 10.0) })));
    }

    /// <summary>Every variant option at a non-default value. The data rows carry only the name: MSTest serialises
    /// dynamic data, which would lose the get-only fields of <see cref="ProductTolerance"/>.</summary>
    private static readonly Dictionary<string, System.Func<DiagnosticEvaluationOptions>> VariantOptions =
        new Dictionary<string, System.Func<DiagnosticEvaluationOptions>>(System.StringComparer.Ordinal)
        {
            ["precursor_tolerance_da"] = () => new DiagnosticEvaluationOptions { PrecursorToleranceDa = 0.01 },
            ["product_tolerance_override"] = () => new DiagnosticEvaluationOptions { ProductToleranceOverride = ProductTolerance.Da(0.005) },
            ["loss_reference_override"] = () => new DiagnosticEvaluationOptions { LossReferenceOverride = Model.LossReference.Hypothesis },
            ["decoy_shift_da"] = () => new DiagnosticEvaluationOptions { DecoyShiftDa = 5 },
            ["floor_scale"] = () => new DiagnosticEvaluationOptions { FloorScale = 0.5 },
            ["k_offset"] = () => new DiagnosticEvaluationOptions { KOffset = 1 },
            ["overlap"] = () => new DiagnosticEvaluationOptions { Overlap = OverlapMode.Runtime },
            ["assignment_cap"] = () => new DiagnosticEvaluationOptions { AssignmentCap = 4097 },
        };

    public static IEnumerable<object[]> Variants() => VariantOptions.Keys.Select(k => new object[] { k });

    [DataTestMethod]
    [DynamicData(nameof(Variants), DynamicDataSourceType.Method)]
    public void EveryVariantOptionMakesTheRuleIneligible(string name) {
        var o = Eligibility(Basic().Reviewed(RuleId), VariantOptions[name]());
        // An override's source is "override", which no review records, so it is also tolerance_differs.
        Assert.AreEqual(name == "product_tolerance_override" ? "ineligible [tolerance_differs variant_active]" : "ineligible [variant_active]",
                        Reasons(o), name);
        Assert.AreEqual(8, VariantOptions.Count);
    }

    [TestMethod]
    public void TheOtherOptionsAreNoVariant() {
        var reviewed = Basic().Reviewed(RuleId);
        foreach (var options in new[]
        {
            new DiagnosticEvaluationOptions { Filter = RuleFilter.All }, new DiagnosticEvaluationOptions { Filter = RuleFilter.AuthorReviewed },
            new DiagnosticEvaluationOptions { Strict = true }, new DiagnosticEvaluationOptions { Verbose = true },
            new DiagnosticEvaluationOptions { MsfinderProductTolerance = ProductTolerance.Da(0.05) },
        }) {
            Assert.AreEqual("eligible []", Reasons(Eligibility(reviewed, options)));
        }
    }

    [TestMethod]
    public void AFailingTwoIonPassAtTheKOffsetIsVariantSingleIon() {
        // k_of(2 of c0, c1, c2) with every term a claim term: fine at KOffset 0 and +1, single-ion at -1.
        string Ion(string id, string formula) => Term(id, "product_ion", "counted", Formula(formula));
        var set = new InlineRuleSet().AddRule(RuleId, Terms(Ion("c0", "C7H3O4"), Ion("c1", "C8H5O4"), Ion("c2", "C9H7O4")),
                                              KOf("2", TermRef("c0"), TermRef("c1"), TermRef("c2")), CoreHex, "[\"[M-H]-\"]", "c0")
            .SetRule(RuleId, "claim/claim_term_ids", "[\"c0\",\"c1\",\"c2\"]").Measured(RuleId);
        var spectrum = SpectrumNeg(Neg("C9H7O4", 500));
        var plain = Outcome(new DiagnosticRuleEvaluator(set.Load(), new DiagnosticEvaluationOptions { Verbose = true }).Evaluate(spectrum));
        Assert.IsFalse(plain.VariantSingleIon);
        Assert.IsFalse(plain.Fired);
        var minus = Outcome(new DiagnosticRuleEvaluator(set.Load(), new DiagnosticEvaluationOptions { KOffset = -1 }).Evaluate(spectrum));
        Assert.IsTrue(minus.VariantSingleIon);
        Assert.IsTrue(minus.Fired); // still evaluated: the variant is a measurement
        Assert.AreEqual("ineligible [not_reviewed variant_active]", Reasons(minus));
        StringAssert.Contains(ResultText(new DiagnosticRuleEvaluator(set.Load(), new DiagnosticEvaluationOptions { KOffset = -1 }).Evaluate(spectrum)),
                              "\"variant_single_ion\":true,");
        Assert.IsFalse(Outcome(new DiagnosticRuleEvaluator(set.Load(), new DiagnosticEvaluationOptions { KOffset = 1 }).Evaluate(SpectrumNeg(Neg("C9H7O4", 500), Neg("C7H3O4", 400), Neg("C8H5O4", 300)))).VariantSingleIon);
    }

    [TestMethod]
    public void WithinCalibrationIsTheInstrumentClassInCalibratedOn() {
        // calibrated_on is [QTOF-type] in every inline rule.
        Assert.IsTrue(Eligibility(Basic(), AllRules()).WithinCalibration);
        Assert.IsFalse(Eligibility(Basic(), AllRules(), instrument: "Orbitrap Fusion").WithinCalibration);
        var both = Basic().SetRule(RuleId, "calibrated_on", "[\"Orbitrap/FT\",\"QTOF-type\"]");
        Assert.IsTrue(Eligibility(both, AllRules(), instrument: "LTQ-Orbitrap").WithinCalibration);
        Assert.IsFalse(Eligibility(both, AllRules(), instrument: "ion trap").WithinCalibration);
        // Report only: it changes no eligibility.
        Assert.AreEqual("eligible []", Reasons(Eligibility(Basic().SetRule(RuleId, "calibrated_on", "[\"Orbitrap/FT\"]").Reviewed(RuleId))));
    }
}
