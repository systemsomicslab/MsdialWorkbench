#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Every role (design 4.7, 7.1): required present and absent; counted at k-1, k and k+1; veto_present;
/// veto_stronger_than above the ratio (vetoed) and at equality (not vetoed); ordering above (passes) and at equality
/// (fails), with several than-terms; upgrade reported and never in the logic. Inline rules in negative mode on
/// apigenin hexoside [M-H]-; every rel is I / 1000.
/// </summary>
[TestClass]
public class TermRoleTests
{
    private const string RuleId = "flavonoid.roles.neg";

    // Fixed negative product ions, far apart and far from the precursor (m/z 431.10).
    private const string A = "C7H3O4", B = "C8H5O4", C = "C9H7O4", V = "C5H3O3", U = "C10H9O4";

    /// <summary>The rule's claim names its first support term, b when there is no a.</summary>
    private static RuleEngine Engine(string terms, string logic) =>
        EngineHarness.MakeEngine(new InlineRuleSet().AddRule(RuleId, terms, logic, CoreHex, "[\"[M-H]-\"]", terms.Contains("\"term_id\":\"a\"") ? "a" : "b").Load(), RuleId);

    private static string Ion(string id, string role, string formula, string floor = "0.05") => Term(id, "product_ion", role, Formula(formula), floor);

    [TestMethod]
    public void RequiredNeedsAnAssignedMemberAboveTheFloor() {
        var engine = Engine(Terms(Ion("a", "required", A), Ion("b", "required", B)), AllNode(TermRef("a"), TermRef("b")));
        var both = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400)));
        Assert.IsTrue(both.Fired);
        Assert.AreEqual(HypothesisFailure.None, both.Failure);
        Assert.AreEqual("a:C7H3O4@1 b:C8H5O4@2", Matched(both));
        Assert.AreEqual(500.0 / 1000.0 + 400.0 / 1000.0, both.Score);

        var absent = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500)));
        Assert.IsFalse(absent.Fired);
        Assert.IsFalse(absent.FiredIgnoringVetoes);
        Assert.AreEqual(HypothesisFailure.LogicFalse, absent.Failure);
        Assert.AreEqual("a:C7H3O4@1", Matched(absent));

        // rel 0.04 is below the 0.05 floor: a hit, but not present, so not a candidate.
        var weak = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 40)));
        Assert.IsFalse(weak.Fired);
        Assert.AreEqual("a:C7H3O4@1", Matched(weak));
    }

    [TestMethod]
    public void CountedAtKMinusOneKAndKPlusOne() {
        var engine = Engine(Terms(Ion("a", "counted", A), Ion("b", "counted", B), Ion("c", "counted", C)),
                            KOf("2", TermRef("a"), TermRef("b"), TermRef("c")));
        var one = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500)));
        var two = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(C, 300)));
        var three = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400), Neg(C, 300)));
        Assert.IsFalse(one.Fired);
        Assert.IsTrue(two.Fired);
        Assert.IsTrue(three.Fired);
        Assert.AreEqual("/logic|2|1|False", KOfText(one));
        Assert.AreEqual("/logic|2|2|True", KOfText(two));
        Assert.AreEqual("/logic|2|3|True", KOfText(three));
        Assert.AreEqual(0.5 + 0.4 + 0.3, three.Score);
    }

    private static string KOfText(HypothesisEvaluation e) =>
        string.Join(" ", e.KOf.Select(k => k.Pointer + "|" + k.K + "|" + k.Count + "|" + k.Satisfied));

    [TestMethod]
    public void VetoPresentBlocksOnRawPresence() {
        var engine = Engine(Terms(Ion("a", "required", A), Ion("b", "required", B), Ion("v", "veto_present", V)),
                            AllNode(TermRef("a"), TermRef("b"), NotNode(TermRef("v"))));
        var vetoed = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400), Neg(V, 200)));
        Assert.IsFalse(vetoed.Fired);
        Assert.IsTrue(vetoed.FiredIgnoringVetoes);
        Assert.IsTrue(vetoed.VetoBlocked);
        Assert.AreEqual(HypothesisFailure.Veto, vetoed.Failure);
        var veto = vetoed.Vetoes.Single();
        Assert.AreEqual("v", veto.Term.Tid);
        Assert.IsTrue(veto.Fired);
        Assert.AreEqual(0.2, veto.IA);
        Assert.AreEqual(0.0, veto.IBMax);
        Assert.AreEqual(3, veto.Peak!.Value.InputIndex);
        // The matches and score are those of the chosen assignment, veto or not.
        Assert.AreEqual("a:C7H3O4@1 b:C8H5O4@2", Matched(vetoed));

        // Below its floor the veto term is not raw_present; I_raw still reports it (floors ignored).
        var weak = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400), Neg(V, 40)));
        Assert.IsTrue(weak.Fired);
        Assert.IsFalse(weak.VetoBlocked);
        Assert.IsFalse(weak.Vetoes[0].Fired);
        Assert.AreEqual(0.04, weak.Vetoes[0].IA);

        var none = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400)));
        Assert.IsTrue(none.Fired);
        Assert.AreEqual(0.0, none.Vetoes[0].IA);
        Assert.IsNull(none.Vetoes[0].Peak);
    }

    [TestMethod]
    public void VetoStrongerThanBlocksAboveTheRatioAndNotAtEquality() {
        var engine = Engine(Terms(Ion("a", "required", A), Ion("b", "required", B),
                                  TermThan("v", "product_ion", "veto_stronger_than", Formula(V), new[] { "a" }, "0.5")),
                            AllNode(TermRef("a"), TermRef("b"), NotNode(TermRef("v"))));
        // I_raw(v) = 0.3 > 0.5 * I_raw(a) = 0.2: vetoed.
        var above = EvaluateOnly(engine, SpectrumNeg(Neg(A, 400), Neg(B, 500), Neg(V, 300)));
        Assert.IsFalse(above.Fired);
        Assert.IsTrue(above.VetoBlocked);
        Assert.AreEqual(0.3, above.Vetoes[0].IA);
        Assert.AreEqual(0.4, above.Vetoes[0].IBMax);
        Assert.IsTrue(above.Vetoes[0].Fired);
        // I_raw(v) = 0.2 = 0.5 * 0.4 exactly (halving is exact): not stronger, not vetoed.
        Assert.AreEqual(0.2, 0.5 * (400.0 / 1000.0));
        var equal = EvaluateOnly(engine, SpectrumNeg(Neg(A, 400), Neg(B, 500), Neg(V, 200)));
        Assert.IsTrue(equal.Fired);
        Assert.IsFalse(equal.Vetoes[0].Fired);
        // Present but not raw_present (below its floor) never vetoes, whatever the ratio.
        var noA = EvaluateOnly(engine, SpectrumNeg(Neg(B, 500), Neg(V, 40)));
        Assert.IsFalse(noA.Vetoes[0].Fired);
        Assert.AreEqual(0.0, noA.Vetoes[0].IBMax);
    }

    [TestMethod]
    public void OrderingNeedsAAssignedAndStrictlyAboveRatioTimesTheStrongestThan() {
        var engine = Engine(Terms(TermThan("o", "product_ion", "ordering", Formula(A), new[] { "b" }, null), Ion("b", "required", B)),
                            AllNode(TermRef("o"), TermRef("b")));
        Assert.IsTrue(EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400))).Fired);
        var equal = EvaluateOnly(engine, SpectrumNeg(Neg(A, 400), Neg(B, 400)));
        Assert.IsFalse(equal.Fired);
        Assert.AreEqual(HypothesisFailure.LogicFalse, equal.Failure);
        Assert.AreEqual("o:C7H3O4@1 b:C8H5O4@2", Matched(equal)); // A is an item and is assigned; the comparison fails
        Assert.IsFalse(EvaluateOnly(engine, SpectrumNeg(Neg(B, 400))).Fired);
    }

    [TestMethod]
    public void OrderingComparesWithTheMaximumOverEveryThanTerm() {
        var engine = Engine(Terms(TermThan("o", "product_ion", "ordering", Formula(A), new[] { "b", "c" }, "0.5"),
                                  Ion("b", "required", B), Ion("c", "required", C)),
                            AllNode(TermRef("o"), TermRef("b"), TermRef("c")));
        // 0.5 * max(0.4, 0.9) = 0.45 < 0.5: passes; 0.5 * max(0.4, 1.0) = 0.5: fails.
        Assert.IsTrue(EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400), Neg(C, 900))).Fired);
        Assert.IsFalse(EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400), Neg(C, 1000))).Fired);
    }

    [TestMethod]
    public void ThanTermsAreReadRawBelowTheirFloor() {
        // w is a veto with a high floor: at rel 0.9 it is not raw_present (no veto), but I_raw(w) = 0.9 still bounds o.
        var engine = Engine(Terms(TermThan("o", "product_ion", "ordering", Formula(A), new[] { "w" }, null),
                                  Ion("b", "required", B), Ion("w", "veto_present", C, floor: "0.95")),
                            AllNode(TermRef("o"), TermRef("b"), NotNode(TermRef("w"))));
        var below = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400), Neg(C, 900)));
        Assert.IsFalse(below.Vetoes[0].Fired);
        Assert.IsFalse(below.Fired);
        Assert.IsFalse(below.FiredIgnoringVetoes);
        Assert.IsTrue(EvaluateOnly(engine, SpectrumNeg(Neg(A, 950), Neg(B, 400), Neg(C, 900))).Fired);
    }

    [TestMethod]
    public void UpgradeIsReportedAndNeverInTheLogic() {
        var engine = Engine(Terms(Ion("a", "required", A), Ion("b", "required", B), Ion("u", "upgrade", U)),
                            AllNode(TermRef("a"), TermRef("b")));
        var with = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400), Neg(U, 700)));
        var without = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(B, 400)));
        Assert.IsTrue(with.Fired);
        Assert.IsTrue(without.Fired);
        Assert.AreEqual(with.Score, without.Score); // never evidence
        Assert.AreEqual("a:C7H3O4@1 b:C8H5O4@2", Matched(with));
        var upgrade = with.Upgrades.Single();
        Assert.AreEqual("u", upgrade.Term.Term.Tid);
        Assert.AreEqual(3, upgrade.Peak.InputIndex);
        Assert.AreEqual(0.7, upgrade.Peak.Rel);
        Assert.AreEqual(-1, upgrade.UnitId);
        Assert.AreEqual(0, without.Upgrades.Count);
        // An upgrade cannot make a rule fire.
        var onlyUpgrade = EvaluateOnly(engine, SpectrumNeg(Neg(A, 500), Neg(U, 700)));
        Assert.IsFalse(onlyUpgrade.Fired);
        Assert.AreEqual(1, onlyUpgrade.Upgrades.Count);
    }
}
