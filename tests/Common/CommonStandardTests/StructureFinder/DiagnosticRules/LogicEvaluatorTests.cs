#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// k-of-n with a variable k and the logic reports (design 4.7, 7.1): max(min, var + offset) for every variable;
/// KOffset with the clamp k_eff &gt;= 1 and variant_single_ion (the two-ion passes rerun at the offset); k above the
/// number of units is false; an unknown variable or node key is refused at load; branch reports and branch_fired;
/// veto_blocked; no short-circuiting.
/// </summary>
[TestClass]
public class LogicEvaluatorTests
{
    private const string RuleId = "flavonoid.logic.neg";

    private static readonly string[] Ions = { "C7H3O4", "C8H5O4", "C9H7O4", "C6H5O3", "C5H3O3", "C10H9O4", "C4H3O2" };

    private static string Ion(string id, string role, string formula) => Term(id, "product_ion", role, Formula(formula));

    private static string Counted(int n, string prefix = "c") =>
        Terms(Enumerable.Range(0, n).Select(i => Ion(prefix + i.ToString(CultureInfo.InvariantCulture), "counted", Ions[i])).ToArray());

    private static string[] Refs(int n, string prefix = "c") =>
        Enumerable.Range(0, n).Select(i => TermRef(prefix + i.ToString(CultureInfo.InvariantCulture))).ToArray();

    private static InlineRuleSet Set(string terms, string logic, string? composition = null) =>
        new InlineRuleSet().AddRule(RuleId, terms, logic, composition ?? CoreHex, "[\"[M-H]-\"]",
                                    terms.Contains("\"term_id\":\"c0\"") ? "c0" : "a");

    private static string KText(HypothesisEvaluation e) => string.Join(",", e.KOf.Select(k => k.K.ToString(CultureInfo.InvariantCulture)));

    [TestMethod]
    public void KIsMaxOfMinAndVariablePlusOffsetForEveryVariable() {
        // n_units 6, n_sugars 5 (Hex2 dHex1 Pen1 HexA1), n_hex 2, n_dhex 1, n_pen 1, n_hexa 1, n_acyl 1 (caffeoyl).
        var composition = InlineRuleSet.Composition("C15H10O5", null, "{\"Hex\":[2,2],\"dHex\":[1,1],\"Pen\":[1,1],\"HexA\":[1,1],\"caffeoyl\":[1,1]}", "{}");
        var ks = new[]
        {
            KVar("n_units", -1, 1), // 5
            KVar("n_sugars", 0, 1), // 5
            KVar("n_hex", 1, 1), // 3
            KVar("n_dhex", 0, 2), // min wins: 2
            KVar("n_pen", 2, 1), // 3
            KVar("n_hexa", -3, 1), // min wins: 1
            KVar("n_acyl", 0, 1), // 1
        };
        var terms = Terms(Enumerable.Range(0, 7).Select(i => Ion("c" + i.ToString(CultureInfo.InvariantCulture), "counted", Ions[i])).ToArray());
        var logic = AnyNode(Enumerable.Range(0, 7).Select(i => KOf(ks[i], TermRef("c" + i.ToString(CultureInfo.InvariantCulture)))).ToArray());
        var set = Set(terms, logic, composition).Load();
        foreach (var (offset, expected) in new[] { (0, "5,5,3,2,3,1,1"), (-1, "4,4,2,1,2,1,1"), (2, "7,7,5,4,5,3,3") }) {
            var engine = MakeEngine(set, RuleId, new DiagnosticEvaluationOptions { KOffset = offset });
            var h = engine.Index.Hypothesis(0, 0, double.NaN);
            var e = EvaluateOnly(engine, MakeSpectrum(h.HypothesisMz, DiagnosticIonMode.Negative, Neg(Ions[6], 500)));
            Assert.AreEqual("core|Hex2,dHex1,Pen1,HexA1,caffeoyl1|[M-H]-", e.Hypothesis.HypothesisId);
            Assert.AreEqual(expected, KText(e), "KOffset " + offset);
            Assert.AreEqual("/logic/any/0,/logic/any/1,/logic/any/2,/logic/any/3,/logic/any/4,/logic/any/5,/logic/any/6",
                            string.Join(",", e.KOf.Select(k => k.Pointer)));
            // Only the n_acyl node can hold with one ion, and only while its k_eff is 1.
            Assert.AreEqual(offset <= 0, e.Fired, "KOffset " + offset);
        }
    }

    [TestMethod]
    public void KOffsetIsClampedSoAKOfIsNeverVacuouslyTrue() {
        var set = Set(Counted(2), KOf("1", Refs(2))).Load();
        var engine = MakeEngine(set, RuleId, new DiagnosticEvaluationOptions { KOffset = -1 });
        var none = EvaluateOnly(engine, SpectrumNeg());
        Assert.AreEqual(1L, none.KOf[0].K); // max(1, 1 - 1)
        Assert.AreEqual(0L, none.KOf[0].Count);
        Assert.IsFalse(none.Fired);
        Assert.IsTrue(EvaluateOnly(engine, SpectrumNeg(Neg(Ions[1], 500))).Fired);

        var three = MakeEngine(Set(Counted(3), KOf("3", Refs(3))).Load(), RuleId, new DiagnosticEvaluationOptions { KOffset = -1 });
        var two = EvaluateOnly(three, SpectrumNeg(Neg(Ions[0], 500), Neg(Ions[2], 400)));
        Assert.AreEqual(2L, two.KOf[0].K);
        Assert.IsTrue(two.Fired);
    }

    [TestMethod]
    public void KAboveTheNumberOfUnitsIsFalse() {
        var engine = MakeEngine(Set(Counted(3), KOf("4", Refs(3))).Load(), RuleId);
        var e = EvaluateOnly(engine, SpectrumNeg(Neg(Ions[0], 500), Neg(Ions[1], 400), Neg(Ions[2], 300)));
        Assert.AreEqual(3L, e.KOf[0].Count);
        Assert.AreEqual(4L, e.KOf[0].K);
        Assert.IsFalse(e.KOf[0].Satisfied);
        Assert.IsFalse(e.Fired);
    }

    /// <summary>A measure:true rule with k_of(2 of c0, c1, c2), every term a claim term: it passes both two-ion passes
    /// at load, fails both at KOffset -1.</summary>
    private static DiagnosticRuleSet MeasuredSet() =>
        Set(Counted(3), KOf("2", Refs(3)))
            .SetRule(RuleId, "measure", "true")
            .SetRule(RuleId, "claim/claim_term_ids", "[\"c0\",\"c1\",\"c2\"]")
            .Load();

    [TestMethod]
    public void TwoIonPassesRerunAtTheKOffset() {
        var set = MeasuredSet();
        var info = set.CompiledRules.Single();
        Assert.AreEqual(0, TwoIonCheck.TwoIonFailures(info, set.Tables, 0).Count);
        Assert.AreEqual(0, TwoIonCheck.TwoIonFailures(info, set.Tables, 1).Count);
        CollectionAssert.AreEqual(new[] { RuleErrorCodes.SingleIonClaim, RuleErrorCodes.ClaimSingleIon },
                                  TwoIonCheck.TwoIonFailures(info, set.Tables, -1));
        // A measure:false rule is never checked.
        var unmeasured = Set(Counted(3), KOf("1", Refs(3))).Load();
        Assert.AreEqual(0, TwoIonCheck.TwoIonFailures(unmeasured.CompiledRules.Single(), unmeasured.Tables, -1).Count);
        Assert.IsFalse(MakeEngine(unmeasured, RuleId, new DiagnosticEvaluationOptions { KOffset = -1 }).VariantSingleIon);
    }

    [TestMethod]
    public void AFailingRuleIsMarkedVariantSingleIonAndStillEvaluated() {
        var set = MeasuredSet();
        Assert.IsFalse(MakeEngine(set, RuleId).VariantSingleIon);
        Assert.IsFalse(MakeEngine(set, RuleId, new DiagnosticEvaluationOptions { KOffset = 1 }).VariantSingleIon);
        var k1 = MakeEngine(set, RuleId, new DiagnosticEvaluationOptions { KOffset = -1 });
        Assert.IsTrue(k1.VariantSingleIon);
        // Evaluated as a measurement: one ion fires it under k - 1.
        Assert.IsTrue(EvaluateOnly(k1, SpectrumNeg(Neg(Ions[2], 500))).Fired);
        Assert.IsFalse(EvaluateOnly(MakeEngine(set, RuleId), SpectrumNeg(Neg(Ions[2], 500))).Fired);
    }

    [TestMethod]
    public void AnUnknownVariableOrNodeKeyIsRefusedAtLoad() {
        var variable = Assert.ThrowsException<DiagnosticRuleSetException>(() => Set(Counted(2), KOf(KVar("n_rings", 0, 1), Refs(2))).Load());
        Assert.IsTrue(variable.Errors.Any(e => e.Code == RuleErrorCodes.UnknownVariable && e.Pointer == "/logic/k_of/k/var"),
                      string.Join("\n", variable.Errors));
        var node = Assert.ThrowsException<DiagnosticRuleSetException>(() =>
            Set(Counted(2), "{\"xor\":[" + string.Join(",", Refs(2)) + "]}").Load());
        Assert.IsTrue(node.Errors.Any(e => e.Code == RuleErrorCodes.LogicNode), string.Join("\n", node.Errors));
    }

    private static RuleEngine BranchEngine() =>
        MakeEngine(Set(Terms(Ion("a", "required", Ions[0]), Ion("b", "required", Ions[1]), Ion("c", "required", Ions[2])),
                   AllNode(TermRef("a"), AnyNode(TermRef("b"), TermRef("c")))).Load(), RuleId);

    private static string Branches(HypothesisEvaluation e) =>
        string.Join(" ", e.Branches.Select(b => b.Pointer + "=" + string.Join(",", b.Children)));

    private static string Fired(bool[][] f) => string.Join(" ", f.Select(x => string.Join(",", x)));

    [TestMethod]
    public void BranchesReportEveryChildAndBranchFiredReevaluatesTheRule() {
        var engine = BranchEngine();
        var onlyB = SpectrumNeg(Neg(Ions[0], 500), Neg(Ions[1], 400));
        var e = EvaluateOnly(engine, onlyB);
        Assert.IsTrue(e.Fired);
        Assert.AreEqual("/logic/all/1=True,False", Branches(e));
        Assert.AreEqual("True,False", Fired(BranchFired(engine, onlyB)));

        var both = SpectrumNeg(Neg(Ions[0], 500), Neg(Ions[1], 400), Neg(Ions[2], 300));
        Assert.AreEqual("/logic/all/1=True,True", Branches(EvaluateOnly(engine, both)));
        Assert.AreEqual("True,True", Fired(BranchFired(engine, both)));

        // No short-circuiting: with a absent the all node is false, yet the any node's children are still reported;
        // branch_fired is the rule firing with the any node restricted, so it is false for both children.
        var noA = SpectrumNeg(Neg(Ions[1], 400));
        var e2 = EvaluateOnly(engine, noA);
        Assert.IsFalse(e2.Fired);
        Assert.AreEqual("/logic/all/1=True,False", Branches(e2));
        Assert.AreEqual("False,False", Fired(BranchFired(engine, noA)));
    }

    [TestMethod]
    public void BranchFiredIsFalseWithoutHypotheses() {
        var engine = BranchEngine();
        var view = SpectrumView.Create(MakeSpectrum(PrecursorNeg + 3.0, DiagnosticIonMode.Negative, Neg(Ions[0], 500), Neg(Ions[1], 400)));
        var hypotheses = Hypotheses(engine, view);
        Assert.AreEqual(0, hypotheses.Count);
        Assert.AreEqual("False,False", Fired(engine.BranchFired(view, hypotheses)));
    }

    [TestMethod]
    public void VetoBlockedIsFiredIgnoringVetoesAndNotFired() {
        // A veto subtree may hold an any node; it is reported like every other any node.
        var engine = MakeEngine(Set(Terms(Ion("a", "required", Ions[0]), Ion("b", "required", Ions[1]),
                                      Ion("v", "veto_present", Ions[4]), Ion("w", "veto_present", Ions[5])),
                                AllNode(TermRef("a"), TermRef("b"), NotNode(AnyNode(TermRef("v"), TermRef("w"))))).Load(), RuleId);
        var blocked = EvaluateOnly(engine, SpectrumNeg(Neg(Ions[0], 500), Neg(Ions[1], 400), Neg(Ions[5], 200)));
        Assert.IsFalse(blocked.Fired);
        Assert.IsTrue(blocked.FiredIgnoringVetoes);
        Assert.IsTrue(blocked.VetoBlocked);
        Assert.AreEqual(HypothesisFailure.Veto, blocked.Failure);
        Assert.AreEqual("v:False w:True", string.Join(" ", blocked.Vetoes.Select(v => v.Term.Tid + ":" + v.Fired)));
        Assert.AreEqual("/logic/all/2/not=False,True", Branches(blocked));
        Assert.AreEqual("False,False", Fired(BranchFired(engine, SpectrumNeg(Neg(Ions[0], 500), Neg(Ions[1], 400), Neg(Ions[4], 200), Neg(Ions[5], 200)))));
        // Restricting the veto's any node to v lifts the block from w.
        Assert.AreEqual("True,False", Fired(BranchFired(engine, SpectrumNeg(Neg(Ions[0], 500), Neg(Ions[1], 400), Neg(Ions[5], 200)))));

        // Not blocked when the logic fails without the vetoes too.
        var noB = EvaluateOnly(engine, SpectrumNeg(Neg(Ions[0], 500), Neg(Ions[5], 200)));
        Assert.IsFalse(noB.FiredIgnoringVetoes);
        Assert.IsFalse(noB.VetoBlocked);
        Assert.AreEqual(HypothesisFailure.LogicFalse, noB.Failure);
    }

    [TestMethod]
    public void WithoutABasePeakNothingFires() {
        var engine = BranchEngine();
        // Every usable peak lies within 1.0 Da of P_obs: p2 has no base peak.
        var e = EvaluateOnly(engine, new DiagnosticSpectrum(new[] { new DiagnosticPeak(PrecursorNeg - 0.5, 1000.0) }, PrecursorNeg,
                                                             DiagnosticIonMode.Negative, "QTOF"));
        Assert.IsFalse(e.Fired);
        Assert.IsFalse(e.FiredIgnoringVetoes);
        Assert.AreEqual(HypothesisFailure.NoBasePeak, e.Failure);
        Assert.AreEqual(0, e.Matched.Count);
        Assert.AreEqual(0.0, e.Score);
    }
}
