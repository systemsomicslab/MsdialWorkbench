#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EvaluatorHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Selection (design 4.9, 7.1): a rule fires iff a hypothesis fires; the chosen hypothesis has the highest score, then
/// the lowest id; tied_hypothesis_ids lists the fired hypotheses with exactly that score; a veto-blocked rule reports
/// its best blocked hypothesis (OQ05); fired_motifs, narrowest_motifs, rule_conflict under the transitive closure of
/// motif nesting, and eligible_rule_conflict over the claim-eligible rules.
/// </summary>
[TestClass]
public class SelectionTests
{
    private const string SeriesRule = "flavonoid.selection.neg";

    /// <summary>The three-scaffold series with the single term Y0 (the aglycone ion: C15H9O5- for genistein and
    /// apigenin, C15H9O7- for quercetin).</summary>
    private static InlineRuleSet SeriesY0() =>
        new InlineRuleSet().AddRule(SeriesRule, Terms(Y0), AllOf(new[] { "Y0" }),
                                    InlineRuleSet.Composition(null, "aglycones", "{\"Hex\":[0,2],\"dHex\":[0,1]}", "{\"sugar\":[1,2]}"),
                                    "[\"[M-H]-\"]", "Y0");

    [TestMethod]
    public void EqualScoresTieAndTheLowestIdIsChosen() {
        // apigenin and genistein hexosides share the precursor and the Y0 ion: both fire with the same score.
        var o = Outcome(Evaluator(SeriesY0(), AllRules(verbose: true)).Evaluate(SpectrumNeg(Neg(Y0Ion, 800))), SeriesRule);
        Assert.IsTrue(o.Fired);
        Assert.AreEqual(2, o.NHypothesesFired);
        Assert.AreEqual("apigenin|Hex1|[M-H]-", o.Chosen!.HypothesisId);
        CollectionAssert.AreEqual(new[] { "apigenin|Hex1|[M-H]-", "genistein|Hex1|[M-H]-" }, o.TiedHypothesisIds.ToArray());
        Assert.AreEqual(o.All[0].Score, o.All[1].Score);
    }

    [TestMethod]
    public void TheHighestScoreWinsOverALowerId() {
        // A 400 Da precursor tolerance admits every composition: quercetin's Y0 is stronger than apigenin's, so a
        // quercetin hypothesis is chosen although apigenin ids sort first; every quercetin hypothesis ties.
        var quercetinY0 = IonNeg("C15H9O7");
        var options = new DiagnosticEvaluationOptions { Filter = RuleFilter.All, Verbose = true, PrecursorToleranceDa = 400.0 };
        var o = Outcome(Evaluator(SeriesY0(), options).Evaluate(SpectrumNeg(Neg(Y0Ion, 300), (quercetinY0, 900))), SeriesRule);
        Assert.AreEqual(24, o.NHypotheses);
        Assert.AreEqual(24, o.NHypothesesFired);
        Assert.AreEqual(8, o.NDistinctFormulas); // four compositions per formula family; genistein and apigenin share theirs
        Assert.AreEqual("quercetin|Hex1,dHex1|[M+HCOO]-", o.Chosen!.HypothesisId); // ',' < '|' and '+' < '-'
        Assert.AreEqual(0.9, o.Chosen.Score);
        CollectionAssert.AreEqual(o.All.Where(h => h.HypothesisId.StartsWith("quercetin|", System.StringComparison.Ordinal)).Select(h => h.HypothesisId).ToArray(),
                                  o.TiedHypothesisIds.ToArray());
        Assert.AreEqual(8, o.TiedHypothesisIds.Count);
        Assert.IsTrue(o.All.Where(h => !h.HypothesisId.StartsWith("quercetin|", System.StringComparison.Ordinal)).All(h => h.Score == 0.3));
    }

    [TestMethod]
    public void AVetoBlockedRuleReportsItsBestBlockedHypothesis() {
        var evaluator = Evaluator(Basic(withVeto: true), AllRules());
        var blocked = Outcome(evaluator.Evaluate(SpectrumNeg(Neg(Y0Ion, 800), Neg(XIon, 500), Neg(VetoIon, 200))));
        Assert.IsFalse(blocked.Fired);
        Assert.IsTrue(blocked.VetoBlocked);
        Assert.AreEqual(0, blocked.NHypothesesFired);
        Assert.AreEqual(0, blocked.TiedHypothesisIds.Count);
        var h = blocked.Chosen!;
        Assert.AreEqual(HypothesisOutcome.FailureVeto, h.Failure);
        Assert.IsFalse(h.Fired);
        Assert.IsTrue(h.FiredIgnoringVetoes);
        var w = h.Vetoes.Single();
        Assert.IsTrue(w.Fired);
        Assert.AreEqual(0.2, w.IA);
        Assert.AreEqual(3, w.PeakIndex);
        Assert.AreEqual(IonNeg(VetoIon), w.PeakMz);
        // Without the veto peak it fires and is not blocked; without X it is neither, and nothing is chosen.
        var fired = Outcome(evaluator.Evaluate(SpectrumNeg(Neg(Y0Ion, 800), Neg(XIon, 500))));
        Assert.IsTrue(fired.Fired);
        Assert.IsFalse(fired.VetoBlocked);
        Assert.IsNull(fired.Chosen!.Failure);
        var none = Outcome(evaluator.Evaluate(SpectrumNeg(Neg(Y0Ion, 800), Neg(VetoIon, 200))));
        Assert.IsFalse(none.Fired);
        Assert.IsFalse(none.VetoBlocked);
        Assert.IsNull(none.Chosen);
        Assert.AreEqual(1, none.NHypotheses);
        Assert.AreEqual("[]", CanonicalJson.ToText(((JsonObject)((JsonArray)Parse(evaluator.Evaluate(SpectrumNeg(Neg(Y0Ion, 800)))) ["outcomes"])[0])["tied_hypothesis_ids"]));
    }

    [TestMethod]
    public void BranchFiredReEvaluatesTheRulePerChild() {
        // all(Y0, any(X, Z)): with X only, the branch through X fires and the one through Z does not.
        var terms = Terms(Y0, X, Term("Z", "product_ion", "required", Formula("C8H5O4")));
        var set = new InlineRuleSet().AddRule(RuleId, terms, AllNode(TermRef("Y0"), AnyNode(TermRef("X"), TermRef("Z"))), CoreHex, "[\"[M-H]-\"]", "Y0");
        var evaluator = Evaluator(set);
        var onlyX = Outcome(evaluator.Evaluate(SpectrumNeg(Neg(Y0Ion, 800), Neg(XIon, 500))));
        Assert.AreEqual("/logic/all/1:True,False", string.Join(" ", onlyX.BranchFired.Select(b => b.Pointer + ":" + string.Join(",", b.Children))));
        var both = Outcome(evaluator.Evaluate(SpectrumNeg(Neg(Y0Ion, 800), Neg(XIon, 500), Neg("C8H5O4", 400))));
        Assert.AreEqual("True,True", string.Join(",", both.BranchFired.Single().Children));
        // Without Y0 no branch fires; the outcome has no fired hypothesis but still reports branch_fired.
        var verbose = Evaluator(set, AllRules(verbose: true));
        var noY0 = Outcome(verbose.Evaluate(SpectrumNeg(Neg(XIon, 500), Neg("C8H5O4", 400))));
        Assert.AreEqual("False,False", string.Join(",", noY0.BranchFired.Single().Children));
    }

    // -------------------------------------------------------------------------------------------- motifs and conflict

    private const string Glycoside = "flavonoid.glycoside.neg", Flavonoid = "flavonoid.any.neg", Anthocyanin = "anthocyanin.any.neg";

    /// <summary>Three copies of the basic rule claiming flavonoid_o_glycoside (a child of flavonoid), flavonoid and
    /// anthocyanin.</summary>
    private static InlineRuleSet Motifs() =>
        Basic(Glycoside).AddBasic(Flavonoid).SetRule(Flavonoid, "claim/motif_id", "\"flavonoid\"")
                        .AddBasic(Anthocyanin).SetRule(Anthocyanin, "claim/motif_id", "\"anthocyanin\"");

    private static string Motifs(DiagnosticRuleResult r) =>
        "fired [" + string.Join(" ", r.FiredMotifs) + "] narrowest [" + string.Join(" ", r.NarrowestMotifs) + "] conflict " + r.RuleConflict
        + " eligible " + r.EligibleRuleConflict;

    [TestMethod]
    public void NestedMotifsDoNotConflict() {
        var set = Motifs().SetRule(Anthocyanin, "claim/motif_id", "\"flavonoid\"");
        Assert.AreEqual("fired [flavonoid flavonoid_o_glycoside] narrowest [flavonoid_o_glycoside] conflict False eligible False",
                        Motifs(Evaluator(set).Evaluate(Firing())));
        // Rules that do not fire contribute nothing.
        Assert.AreEqual("fired [] narrowest [] conflict False eligible False", Motifs(Evaluator(set).Evaluate(SpectrumNeg(Neg(Y0Ion, 800)))));
    }

    [TestMethod]
    public void UnnestedMotifsConflict() {
        Assert.AreEqual("fired [anthocyanin flavonoid flavonoid_o_glycoside] narrowest [anthocyanin flavonoid_o_glycoside] conflict True eligible False",
                        Motifs(Evaluator(Motifs()).Evaluate(Firing())));
        StringAssert.Contains(ResultText(Evaluator(Motifs()).Evaluate(Firing())),
                              "\"fired_motifs\":[\"anthocyanin\",\"flavonoid\",\"flavonoid_o_glycoside\"],");
    }

    [TestMethod]
    public void NestingIsTransitive() {
        // anthocyanin -> flavonoid_o_glycoside -> flavonoid: every pair is nested.
        var set = Motifs().Set("tables/motif_nesting.json", "nesting/-",
            "{\"child\":\"anthocyanin\",\"parent\":\"flavonoid_o_glycoside\",\"source\":[{\"doi\":\"10.1000/synthetic.1\",\"locator\":\"Table 1\"}]}");
        Assert.AreEqual("fired [anthocyanin flavonoid flavonoid_o_glycoside] narrowest [anthocyanin] conflict False eligible False",
                        Motifs(Evaluator(set).Evaluate(Firing())));
        var closure = new MotifNestingClosure(set.Load().Nesting);
        Assert.IsTrue(closure.IsAncestor("flavonoid", "anthocyanin"));
        Assert.IsFalse(closure.IsAncestor("anthocyanin", "flavonoid"));
        Assert.IsTrue(closure.Nested("flavonoid", "anthocyanin"));
        Assert.IsTrue(closure.Nested("hydroxycinnamoyl_hexose", "hydroxycinnamoyl_hexose"));
        Assert.IsFalse(closure.Nested("hydroxycinnamoyl_hexose", "flavonoid"));
    }

    [TestMethod]
    public void EligibleRuleConflictCountsOnlyEligibleRules() {
        // Glycoside and anthocyanin reviewed (eligible), flavonoid not: the eligible pair is not nested.
        var set = Motifs().Reviewed(Glycoside).Reviewed(Anthocyanin);
        var result = Evaluator(set).Evaluate(Firing());
        Assert.IsTrue(Outcome(result, Glycoside).ClaimEligible);
        Assert.IsFalse(Outcome(result, Flavonoid).ClaimEligible);
        Assert.AreEqual("fired [anthocyanin flavonoid flavonoid_o_glycoside] narrowest [anthocyanin flavonoid_o_glycoside] conflict True eligible True",
                        Motifs(result));
        // Glycoside and flavonoid eligible: nested, so no eligible conflict, though the rule conflict remains.
        var nested = Motifs().Reviewed(Glycoside).Reviewed(Flavonoid);
        Assert.AreEqual("fired [anthocyanin flavonoid flavonoid_o_glycoside] narrowest [anthocyanin flavonoid_o_glycoside] conflict True eligible False",
                        Motifs(Evaluator(nested).Evaluate(Firing())));
        // A variant makes every rule ineligible.
        Assert.IsFalse(Evaluator(set, new DiagnosticEvaluationOptions { Filter = RuleFilter.All, FloorScale = 0.5 }).Evaluate(Firing()).EligibleRuleConflict);
    }
}
