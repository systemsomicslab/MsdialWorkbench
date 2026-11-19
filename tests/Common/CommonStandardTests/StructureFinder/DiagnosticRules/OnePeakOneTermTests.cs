#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// One peak, one term (design 4.6, 4.8, 7.1) on caffeoyl-hexose (341.0878): under [M-H]- (C15H17O9-) the loss of
/// C6H10O5 (162.0528) and caffeate C9H7O4- (179.0350) coincide exactly in pDa. With the complementary declaration one
/// peak satisfies both and counts once in k_of and in the score; without it the rule is refused
/// (overlap_undeclared); under OverlapMode.Runtime the search gives the peak to one term and leaves the other
/// unassigned; too many leaves give assignment_overflow. Also the overlap test's tolerances and observed-loss slack.
/// </summary>
[TestClass]
public class OnePeakOneTermTests
{
    private const string RuleId = "hydroxycinnamoyl.caffeoyl_hexose.neg";
    private const string Caffeate = "C9H7O4", Decarboxylated = "C8H7O2", Extra = "C7H3O4";

    /// <summary>Caffeic acid C9H8O4 with one Hex: neutral C15H18O9.</summary>
    private static string CaffeoylHexose => InlineRuleSet.Composition("C9H8O4", null, "{\"Hex\":[1,1]}", "{}");

    private static double Precursor => TestMass.Ion(TestMass.Of("C15H18O9") - TestMass.Of("H"), -1);

    private static DiagnosticSpectrum Spectrum(params (double Mz, double Intensity)[] peaks) =>
        EngineHarness.MakeSpectrum(Precursor, DiagnosticIonMode.Negative, peaks);

    private static string KOfTerms(string role = "counted") => Terms(
        Term("loss_hex", "loss_from_precursor", role, Formula("C6H10O5")),
        Term("caffeate", "product_ion", role, Formula(Caffeate)),
        Term("decarb", "product_ion", role, Formula(Decarboxylated)));

    private static string KOfLogic => KOf("2", TermRef("loss_hex"), TermRef("caffeate"), TermRef("decarb"));

    private static InlineRuleSet KOfRule(bool declared) {
        var set = new InlineRuleSet().AddRule(RuleId, KOfTerms(), KOfLogic, CaffeoylHexose, "[\"[M-H]-\"]", "caffeate");
        return declared ? set.SetRule(RuleId, "complementary", "[[\"loss_hex\",\"caffeate\"]]") : set;
    }

    private static readonly DiagnosticEvaluationOptions Runtime = new DiagnosticEvaluationOptions { Overlap = OverlapMode.Runtime };

    [TestMethod]
    public void TheLossAndTheIonCoincideExactlyInPda() {
        var engine = MakeEngine(KOfRule(true).Load(), RuleId);
        var h = EvaluateOnly(engine, Spectrum()).Hypothesis;
        Assert.AreEqual("core|Hex1|[M-H]-", h.HypothesisId);
        Assert.AreEqual("C15H18O9", h.NeutralFormula);
        Assert.AreEqual(Precursor, h.HypothesisMz);
        Assert.AreEqual("341.087806", Json.ExactDecimal.Format(h.HypothesisMz, 6));
        var targets = engine.Builder.Build(h.Series, 0, Model.LossReference.Hypothesis, double.NaN);
        var loss = targets.Single(t => t.Term.Tid == "loss_hex").Members.Single();
        var ion = targets.Single(t => t.Term.Tid == "caffeate").Members.Single();
        Assert.AreEqual(loss.TargetPda, ion.TargetPda);
        Assert.AreEqual(TestMass.Of("C9H7O4") + TestMass.Electron, ion.TargetPda);
        Assert.AreEqual("162.052823", Json.ExactDecimal.Format(TestMass.Da(loss.MassPda), 6));
        Assert.AreEqual("179.034983", Json.ExactDecimal.Format(ion.TargetMz, 6));
    }

    [TestMethod]
    public void DeclaredOnePeakSatisfiesBothAndCountsOnce() {
        var engine = MakeEngine(KOfRule(true).Load(), RuleId);
        Assert.IsNull(OverlapValidator.Check(engine));
        var e = EvaluateOnly(engine, Spectrum(Neg(Caffeate, 600), Neg(Decarboxylated, 300)));
        Assert.IsTrue(e.Fired);
        Assert.AreEqual("loss_hex:C6H10O5@1 caffeate:C9H7O4@1 decarb:C8H7O2@2", Matched(e));
        Assert.AreEqual("0 0 1", string.Join(" ", e.Matched.Select(m => m.UnitId)));
        Assert.AreEqual(2L, e.KOf[0].Count);
        Assert.AreEqual(0.6 + 0.3, e.Score); // the pair once
        // The shared peak alone is one unit: k = 2 is not reached.
        var alone = EvaluateOnly(engine, Spectrum(Neg(Caffeate, 600)));
        Assert.AreEqual("loss_hex:C6H10O5@1 caffeate:C9H7O4@1", Matched(alone));
        Assert.AreEqual(1L, alone.KOf[0].Count);
        Assert.IsFalse(alone.Fired);
        Assert.AreEqual(0.6, alone.Score);
    }

    [TestMethod]
    public void UndeclaredIsRefused() {
        var set = KOfRule(false).Load();
        var engine = MakeEngine(set, RuleId);
        var refusal = OverlapValidator.Check(engine)!;
        Assert.IsNotNull(refusal);
        Assert.AreEqual(RuleId, refusal.RuleId);
        Assert.AreEqual("overlap_undeclared", refusal.Code);
        Assert.AreEqual(RuleRefusal.OverlapUndeclared, refusal.Code);
        // [M+HCOO]- sorts first ('+' < '-') but has no overlap; the [M-H]- hypothesis does.
        Assert.AreEqual("core|Hex1|[M-H]-", refusal.HypothesisId);
        CollectionAssert.AreEqual(new[] { "loss_hex:C6H10O5", "caffeate:C9H7O4" }, refusal.Items.ToArray());
        Assert.AreEqual(0.0, refusal.DeltaDa);

        var refuse = EvaluationSettings.From(new DiagnosticEvaluationOptions());
        Assert.AreEqual(1, OverlapValidator.Validate(new[] { engine }, refuse).Count);
        var strict = EvaluationSettings.From(new DiagnosticEvaluationOptions { Strict = true });
        var thrown = Assert.ThrowsException<DiagnosticRuleRefusedException>(() => OverlapValidator.Validate(new[] { engine }, strict));
        Assert.AreEqual(RuleId, thrown.Refusals.Single().RuleId);
        StringAssert.Contains(thrown.Message, "overlap_undeclared");
        // Runtime refuses nothing, Strict or not.
        var runtimeStrict = EvaluationSettings.From(new DiagnosticEvaluationOptions { Overlap = OverlapMode.Runtime, Strict = true });
        Assert.AreEqual(0, OverlapValidator.Validate(new[] { MakeEngine(set, RuleId, Runtime) }, runtimeStrict).Count);
    }

    [TestMethod]
    public void RuntimeGivesThePeakToOneTerm() {
        var engine = MakeEngine(KOfRule(false).Load(), RuleId, Runtime);
        var e = EvaluateOnly(engine, Spectrum(Neg(Caffeate, 600), Neg(Decarboxylated, 300)));
        // Both leaves that use the peak tie on (logic, score, assigned): the first, loss_hex, wins; caffeate is
        // unassigned.
        Assert.IsTrue(e.Fired);
        Assert.AreEqual("loss_hex:C6H10O5@1 decarb:C8H7O2@2", Matched(e));
        Assert.AreEqual(2L, e.KOf[0].Count);
        Assert.AreEqual(0.6 + 0.3, e.Score);
    }

    [TestMethod]
    public void TheSearchPrefersLogicThenScoreThenTheFirstLeaf() {
        // all(caffeate, decarb, any(loss_hex, extra)): only giving the peak to caffeate can make the logic true.
        var logic = AllNode(TermRef("caffeate"), TermRef("decarb"), AnyNode(TermRef("loss_hex"), TermRef("extra")));
        var terms = Terms(
            Term("loss_hex", "loss_from_precursor", "required", Formula("C6H10O5")),
            Term("caffeate", "product_ion", "required", Formula(Caffeate)),
            Term("decarb", "product_ion", "required", Formula(Decarboxylated)),
            Term("extra", "product_ion", "required", Formula(Extra)));
        var engine = MakeEngine(new InlineRuleSet().AddRule(RuleId, terms, logic, CaffeoylHexose, "[\"[M-H]-\"]", "caffeate").Load(), RuleId, Runtime);
        var logicWins = EvaluateOnly(engine, Spectrum(Neg(Caffeate, 600), Neg(Decarboxylated, 300), Neg(Extra, 200)));
        Assert.IsTrue(logicWins.Fired);
        Assert.AreEqual("caffeate:C9H7O4@1 decarb:C8H7O2@2 extra:C7H3O4@3", Matched(logicWins));
        // Without extra no leaf is true; the first of the equal-score leaves gives the peak to loss_hex.
        var firstLeaf = EvaluateOnly(engine, Spectrum(Neg(Caffeate, 600), Neg(Decarboxylated, 300)));
        Assert.IsFalse(firstLeaf.Fired);
        Assert.AreEqual("loss_hex:C6H10O5@1 decarb:C8H7O2@2", Matched(firstLeaf));

        // any(loss_hex, caffeate), caffeate with a 0.5 floor: loss_hex can also take a weaker peak 0.003 Da away. The
        // first leaf (loss_hex = the strong peak, caffeate none) scores 0.6; a later one (loss_hex = the weak peak,
        // caffeate = the strong one) scores 0.1 + 0.6 and wins.
        var scored = Terms(
            Term("loss_hex", "loss_from_precursor", "required", Formula("C6H10O5")),
            Term("caffeate", "product_ion", "required", Formula(Caffeate), "0.5"));
        var scoreEngine = MakeEngine(new InlineRuleSet().AddRule(RuleId, scored, AnyNode(TermRef("loss_hex"), TermRef("caffeate")),
                                                             CaffeoylHexose, "[\"[M-H]-\"]", "caffeate").Load(), RuleId, Runtime);
        var best = EvaluateOnly(scoreEngine, Spectrum(Neg(Caffeate, 600), (IonNeg(Caffeate) + 0.003, 100)));
        Assert.AreEqual("loss_hex:C6H10O5@2 caffeate:C9H7O4@1", Matched(best));
        Assert.AreEqual(0.1 + 0.6, best.Score);
    }

    [TestMethod]
    public void AVetoedHypothesisIsAVetoEvenWhenTheFirstEqualLeafFailsTheVetoFreeLogic() {
        // all(caffeate, any(loss_hex, decarb), not(extra)) under Runtime: the caffeate peak is a candidate of loss_hex
        // and caffeate. With the veto peak every leaf is false, and the first two tie on score and assigned count:
        // loss_hex taking the peak (the veto-free logic is false: caffeate has none), then caffeate taking it (the
        // veto-free logic holds). fired_ignoring_vetoes must not depend on that tie (design 4.7).
        var terms = Terms(
            Term("loss_hex", "loss_from_precursor", "required", Formula("C6H10O5")),
            Term("caffeate", "product_ion", "required", Formula(Caffeate)),
            Term("decarb", "product_ion", "required", Formula(Decarboxylated)),
            Term("extra", "product_ion", "veto_present", Formula(Extra)));
        var logic = AllNode(TermRef("caffeate"), AnyNode(TermRef("loss_hex"), TermRef("decarb")), NotNode(TermRef("extra")));
        var set = new InlineRuleSet().AddRule(RuleId, terms, logic, CaffeoylHexose, "[\"[M-H]-\"]", "caffeate").Load();
        var engine = MakeEngine(set, RuleId, Runtime);
        var clean = EvaluateOnly(engine, Spectrum(Neg(Caffeate, 600), Neg(Decarboxylated, 300)));
        Assert.IsTrue(clean.Fired);
        Assert.AreEqual("caffeate:C9H7O4@1 decarb:C8H7O2@2", Matched(clean));

        var vetoed = Spectrum(Neg(Caffeate, 600), Neg(Decarboxylated, 300), Neg(Extra, 100));
        var e = EvaluateOnly(engine, vetoed);
        Assert.IsFalse(e.Fired);
        Assert.IsTrue(e.FiredIgnoringVetoes);
        Assert.IsTrue(e.VetoBlocked);
        Assert.AreEqual(HypothesisFailure.Veto, e.Failure);
        Assert.AreEqual("caffeate:C9H7O4@1 decarb:C8H7O2@2", Matched(e));
        Assert.AreEqual(0.6 + 0.3, e.Score);
        Assert.IsTrue(e.Vetoes.Single().Fired);

        // The rule is veto-blocked, and its chosen hypothesis lists the blocking veto.
        var options = new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Overlap = OverlapMode.Runtime };
        var outcome = new DiagnosticRuleEvaluator(set, options).Evaluate(vetoed).Outcomes.Single();
        Assert.IsFalse(outcome.Fired);
        Assert.IsTrue(outcome.VetoBlocked);
        Assert.AreEqual(HypothesisOutcome.FailureVeto, outcome.Chosen!.Failure);
        Assert.AreEqual("extra", outcome.Chosen.Vetoes.Single(v => v.Fired).TermId);
    }

    [TestMethod]
    public void MoreLeavesThanTheCapGiveAssignmentOverflow() {
        // Two conflicted items with one candidate each: (peak, none), (none, peak), (none, none) = 3 leaves.
        var set = KOfRule(false).Load();
        var spectrum = Spectrum(Neg(Caffeate, 600), Neg(Decarboxylated, 300));
        var over = EvaluateOnly(MakeEngine(set, RuleId, new DiagnosticEvaluationOptions { Overlap = OverlapMode.Runtime, AssignmentCap = 2 }), spectrum);
        Assert.AreEqual(HypothesisFailure.AssignmentOverflow, over.Failure);
        Assert.IsFalse(over.Fired);
        Assert.IsFalse(over.FiredIgnoringVetoes);
        Assert.AreEqual(0, over.Matched.Count);
        Assert.AreEqual(0, over.KOf.Count);
        Assert.AreEqual(0.0, over.Score);
        var atCap = EvaluateOnly(MakeEngine(set, RuleId, new DiagnosticEvaluationOptions { Overlap = OverlapMode.Runtime, AssignmentCap = 3 }), spectrum);
        Assert.AreEqual(HypothesisFailure.None, atCap.Failure);
        Assert.IsTrue(atCap.Fired);
        // With the declaration there is no conflict, so even a cap of 1 never overflows (the fast path).
        var declared = EvaluateOnly(MakeEngine(KOfRule(true).Load(), RuleId, new DiagnosticEvaluationOptions { AssignmentCap = 1 }), spectrum);
        Assert.IsTrue(declared.Fired);
    }

    [TestMethod]
    public void BranchFiredSkipsOnlyHypothesesNoRestrictionCanFire() {
        // all(caffeate, decarb, any(loss_hex, extra)): the search over loss_hex and caffeate visits 3 leaves under every
        // logic, so a cap of 2 overflows the full logic and each restriction alike; branch_fired skips the hypothesis.
        var logic = AllNode(TermRef("caffeate"), TermRef("decarb"), AnyNode(TermRef("loss_hex"), TermRef("extra")));
        var terms = Terms(
            Term("loss_hex", "loss_from_precursor", "required", Formula("C6H10O5")),
            Term("caffeate", "product_ion", "required", Formula(Caffeate)),
            Term("decarb", "product_ion", "required", Formula(Decarboxylated)),
            Term("extra", "product_ion", "required", Formula(Extra)));
        var set = new InlineRuleSet().AddRule(RuleId, terms, logic, CaffeoylHexose, "[\"[M-H]-\"]", "caffeate").Load();
        var spectrum = Spectrum(Neg(Caffeate, 600), Neg(Decarboxylated, 300), Neg(Extra, 200));
        var view = SpectrumView.Create(spectrum);
        var capped = MakeEngine(set, RuleId, new DiagnosticEvaluationOptions { Overlap = OverlapMode.Runtime, AssignmentCap = 2 });
        var full = EvaluateOnly(capped, spectrum);
        Assert.AreEqual(HypothesisFailure.AssignmentOverflow, full.Failure);
        Assert.IsFalse(RuleEngine.MayFireUnderRestriction(full));
        for (var c = 0; c < 2; c++) {
            Assert.AreEqual(HypothesisFailure.AssignmentOverflow, capped.Evaluate(view, full.Hypothesis, 0, c).Failure);
        }
        Assert.AreEqual("False,False", string.Join(",", BranchFired(capped, spectrum)[0]));
        // At a cap of 3 the hypothesis is searched: only the extra branch can fire (loss_hex and caffeate need one peak).
        var searched = MakeEngine(set, RuleId, new DiagnosticEvaluationOptions { Overlap = OverlapMode.Runtime, AssignmentCap = 3 });
        Assert.IsTrue(RuleEngine.MayFireUnderRestriction(EvaluateOnly(searched, spectrum)));
        Assert.AreEqual("False,True", string.Join(",", BranchFired(searched, spectrum)[0]));
    }

    /// <summary>A loss of C6H10O5 (target caffeate, 179.034983) and the ion C8H3O5- (178.998597), 0.036386 Da
    /// apart.</summary>
    private static DiagnosticRuleSet NearPair() =>
        new InlineRuleSet().AddRule(RuleId, Terms(
                Term("loss_hex", "loss_from_precursor", "required", Formula("C6H10O5")),
                Term("near", "product_ion", "required", Formula("C8H3O5"))),
            AllNode(TermRef("loss_hex"), TermRef("near")), CaffeoylHexose, "[\"[M-H]-\"]", "near").Load();

    private static RuleRefusal? Check(DiagnosticRuleSet set, DiagnosticEvaluationOptions options) =>
        OverlapValidator.Check(MakeEngine(set, RuleId, options));

    [TestMethod]
    public void OverlapIsTestedAtTheEffectiveProductTolerance() {
        var set = NearPair();
        Assert.IsNull(Check(set, new DiagnosticEvaluationOptions())); // p2: 0.005 + 0.005 < 0.036
        var wide = Check(set, new DiagnosticEvaluationOptions { ProductToleranceOverride = ProductTolerance.Da(0.02) })!;
        Assert.IsNotNull(wide);
        CollectionAssert.AreEqual(new[] { "near:C8H3O5", "loss_hex:C6H10O5" }, wide.Items.ToArray()); // lower target first
        Assert.AreEqual(IonNeg(Caffeate) - IonNeg("C8H3O5"), wide.DeltaDa);
        Assert.IsNull(Check(set, new DiagnosticEvaluationOptions { ProductToleranceOverride = ProductTolerance.Da(0.018) }));
        // msfinder rules read the evaluator's MsfinderProductTolerance.
        var msfinder = new InlineRuleSet().AddRule(RuleId, Terms(
                Term("loss_hex", "loss_from_precursor", "required", Formula("C6H10O5")),
                Term("near", "product_ion", "required", Formula("C8H3O5"))),
            AllNode(TermRef("loss_hex"), TermRef("near")), CaffeoylHexose, "[\"[M-H]-\"]", "near")
            .SetRule(RuleId, "product_tolerance", "\"msfinder\"").Load();
        Assert.IsNull(Check(msfinder, new DiagnosticEvaluationOptions()));
        Assert.IsNotNull(Check(msfinder, new DiagnosticEvaluationOptions { MsfinderProductTolerance = ProductTolerance.Da(0.02) }));
    }

    [TestMethod]
    public void AnObservedLossAddsThePrecursorToleranceToTheSlack() {
        var set = NearPair();
        var zero = ProductTolerance.Da(0.0);
        Assert.IsNull(Check(set, new DiagnosticEvaluationOptions { ProductToleranceOverride = zero, PrecursorToleranceDa = 0.05 }));
        var observed = Check(set, new DiagnosticEvaluationOptions
        {
            ProductToleranceOverride = zero, PrecursorToleranceDa = 0.05, LossReferenceOverride = Model.LossReference.Observed,
        });
        Assert.IsNotNull(observed);
        Assert.IsNull(Check(set, new DiagnosticEvaluationOptions
        {
            ProductToleranceOverride = zero, PrecursorToleranceDa = 0.03, LossReferenceOverride = Model.LossReference.Observed,
        }));
    }

    [TestMethod]
    public void TheDecoyShiftMovesTheCheckedTargets() {
        // +1 Da: the loss target moves to 178.03, the ion to 180.04; they no longer coincide.
        var set = KOfRule(false).Load();
        Assert.IsNotNull(Check(set, new DiagnosticEvaluationOptions()));
        Assert.IsNull(Check(set, new DiagnosticEvaluationOptions { DecoyShiftDa = 1 }));
    }

    // ------------------------------------------------------------- the search's own bounds (finding: depth and cost)

    /// <summary>A hit on view peak <paramref name="peak"/>, present, rel 1.</summary>
    private static PeakHit Hit(int peak) => new PeakHit(peak, peak, 100.0 + peak, 1.0, 0.0, true, false);

    /// <summary>Items built straight from per-member hits on the caffeoyl rule's terms (loss_hex, caffeate, decarb;
    /// loss_hex and caffeate declared complementary): the targets play no part in the assignment.</summary>
    private static EvidenceItems Items(params int[][][] hitsByTermAndMember) {
        var set = KOfRule(true).Load();
        var info = set.CompiledRules[set.Rules.Select((r, i) => (r, i)).Single(x => x.r.RuleId == RuleId).i];
        var memberHits = new IReadOnlyList<PeakHit>[info.Terms.Count][];
        for (var t = 0; t < memberHits.Length; t++) {
            var members = t < hitsByTermAndMember.Length ? hitsByTermAndMember[t] : new int[0][];
            memberHits[t] = members.Select(peaks => (IReadOnlyList<PeakHit>)peaks.Select(Hit).ToList()).ToArray();
        }
        return EvidenceItems.Build(info, EvidenceItems.GroupOfTerm(info), memberHits);
    }

    [TestMethod]
    public void TheSearchDepthIsNotBoundedByTheThreadStack() {
        // 100 000 members of one term, member m on peaks m and m + 1: every item is conflicted and the first leaf
        // lies 100 000 items deep, whatever the cap. A recursive search overflows a 256 KB stack (a
        // StackOverflowException ends the process); the explicit stack does not. Leaf 1 gives every item its first
        // peak, leaf 2 moves the last item to its second, leaf 3 (the last item unassigned) is over the cap of 2.
        const int N = 100000;
        var items = Items(Enumerable.Range(0, N).Select(m => new[] { m, m + 1 }).ToArray());
        Assert.AreEqual(N, items.Items.Count);
        Assignment? result = null;
        var leafAssigned = new List<int>();
        var thread = new Thread(() => result = ExclusiveAssignment.Assign(items, c => {
            leafAssigned.Add(ExclusiveAssignment.AssignedCount(c));
            return false;
        }, _ => false, 2), 256 * 1024);
        thread.Start();
        thread.Join();
        Assert.IsNotNull(result);
        Assert.IsTrue(result!.Overflow);
        Assert.IsFalse(result.FastPath);
        Assert.AreEqual(3, result.Leaves);
        Assert.AreEqual("100000 100000", string.Join(" ", leafAssigned));
    }

    [TestMethod]
    public void TheSearchVisitsCandidatesInOrderThenNone() {
        // Three members of loss_hex on peaks {0, 1}, {0} and {1}: the leaves of the recursive formulation, in order.
        var items = Items(new[] { new[] { 0, 1 }, new[] { 0 }, new[] { 1 } });
        var leaves = new List<string>();
        var result = ExclusiveAssignment.Assign(items, c => {
            leaves.Add(string.Join(",", c));
            return false;
        }, _ => false, 100);
        Assert.IsFalse(result.Overflow);
        Assert.AreEqual("0,-1,0 0,-1,-1 1,0,-1 1,-1,-1 -1,0,0 -1,0,-1 -1,-1,0 -1,-1,-1", string.Join(" ", leaves));
        Assert.AreEqual(leaves.Count, result.Leaves);
        // Every leaf has the same (false) logic, so the most assigned items win, the first such leaf on ties.
        Assert.AreEqual("0,-1,0", string.Join(",", result.Choice!));
    }

    [TestMethod]
    public void ConflictsAreFoundPerPeakWithoutPairwiseTests() {
        // A peak shared by the two declared terms only (one member each) is no conflict: the fast path.
        var shared = Items(new[] { new[] { 0 } }, new[] { new[] { 0 } });
        Assert.IsTrue(ExclusiveAssignment.Assign(shared, _ => true, _ => true, 10).FastPath);
        // Add a second member of loss_hex on that peak: the two loss_hex members conflict, caffeate's member may share
        // with both and keeps its peak outside the search.
        var twoOfOneTerm = Items(new[] { new[] { 0 }, new[] { 0 } }, new[] { new[] { 0 } });
        var r = ExclusiveAssignment.Assign(twoOfOneTerm, _ => true, _ => true, 100);
        Assert.IsFalse(r.FastPath);
        Assert.AreEqual("0,-1,0", string.Join(",", r.Choice!));
        // decarb is in no group: on a peak with anything else it conflicts, and so does its neighbour.
        var ungrouped = Items(new[] { new[] { 0 } }, new int[0][], new[] { new[] { 0 } });
        var u = ExclusiveAssignment.Assign(ungrouped, _ => true, _ => true, 100);
        Assert.IsFalse(u.FastPath);
        Assert.AreEqual("0,-1", string.Join(",", u.Choice!));
    }
}
