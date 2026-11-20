#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticQueryFiles;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>The ranking term (design 5.6, 7.1).</summary>
[TestClass]
public class DiagnosticRankingTests
{
    internal static FragmenterResult Candidate(string smiles, double score, string? formula = null, string? inchikey = null, string? ontology = null) {
        var c = new FragmenterResult { TotalScore = score };
        Set(c, "smiles", smiles);
        Set(c, "formula", formula ?? "C6H12O6");
        Set(c, "inchikey", inchikey ?? "key-" + smiles);
        Set(c, "ontology", ontology ?? "Hexoses");
        return c;
    }

    private static void Set(FragmenterResult c, string field, string? value) =>
        typeof(FragmenterResult).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(c, value);

    /// <summary>A fake matcher: SMILES "A+B" has motifs A and B.</summary>
    private static bool Matches(string smiles, string motif) => smiles.Split('+').Contains(motif);

    private static RankingOutcomeView Fired(string motif, bool eligible = true) => new RankingOutcomeView("r." + motif, motif, true, false, false, eligible);

    private static RankingOutcomeView Veto(string motif, bool excludes = true, bool eligible = true) => new RankingOutcomeView("v." + motif, motif, false, true, excludes, eligible);

    private static DiagnosticRankingInput Input(bool eligibleConflict = false, bool conflict = false, params RankingOutcomeView[] views) =>
        new DiagnosticRankingInput(views, eligibleConflict, conflict, true);

    private static string Order(DiagnosticRanking r) => string.Join(",", r.Ranked.Select(c => c.Candidate.Smiles));

    private static DiagnosticRanking Rank(DiagnosticRankingInput input, double w, DiagnosticRuleRankingScope scope = DiagnosticRuleRankingScope.Reviewed, params FragmenterResult[] candidates) =>
        MsfinderDiagnosticRanking.Rank(candidates, input, scope, w, Matches);

    [TestMethod]
    public void WeightZeroKeepsTheLegacyOrderAndMatchesNothing() {
        var calls = 0;
        var candidates = new[] { Candidate("x", 3.0), Candidate("M", 2.9), Candidate("y", 2.9) };
        var r = MsfinderDiagnosticRanking.Rank(candidates, Input(views: Fired("M")), DiagnosticRuleRankingScope.Reviewed, 0.0, (s, m) => { calls++; return true; });
        Assert.AreEqual(MsfinderDiagnosticRanking.StatusWeightZero, r.Status);
        Assert.AreEqual("x,M,y", Order(r));
        Assert.AreEqual(0, calls);
        Assert.IsTrue(r.Ranked.All(c => c.Term == 0));
        // w = 0 needs no matcher (none exists when w = 0)
        Assert.AreEqual("x,M,y", Order(MsfinderDiagnosticRanking.Rank(candidates, Input(views: Fired("M")), DiagnosticRuleRankingScope.Reviewed, 0.0, null!)));
    }

    [TestMethod]
    public void BaseOrderIsTheLegacyStableSort() {
        var merged = new[] { Candidate("a", 1.0), Candidate("b", 2.0), Candidate("c", 1.0) };
        Assert.AreEqual("b,a,c", string.Join(",", MsfinderDiagnosticRanking.BaseOrder(merged).Select(c => c.Smiles)));
    }

    [TestMethod]
    public void PositiveWeightLiftsAcrossAGapSmallerThanW() {
        var input = Input(views: Fired("M"));
        Assert.AreEqual("M,x", Order(Rank(input, 0.25, candidates: new[] { Candidate("x", 3.0), Candidate("M", 2.875) })));
        Assert.AreEqual("x,M", Order(Rank(input, 0.0625, candidates: new[] { Candidate("x", 3.0), Candidate("M", 2.875) })));
        var r = Rank(input, 0.25, candidates: new[] { Candidate("x", 3.0), Candidate("M", 2.875) });
        Assert.AreEqual(MsfinderDiagnosticRanking.StatusApplied, r.Status);
        Assert.AreEqual(1, r.Ranked[0].Term);
        Assert.AreEqual(2, r.Ranked[0].BaseRank);
        Assert.AreEqual(3.125, r.Ranked[0].RankingScore);
        CollectionAssert.AreEqual(new[] { "M" }, r.Ranked[0].MatchedMotifs.ToArray());
    }

    [TestMethod]
    public void TiesKeepTheBaseOrder() {
        var input = Input(views: Fired("M"));
        // 2.5 + 0.5 == 3.0 exactly: the tie keeps x first; equal candidates keep their order.
        Assert.AreEqual("x,M,M+z,y", Order(Rank(input, 0.5, candidates: new[] { Candidate("x", 3.0), Candidate("M", 2.5), Candidate("M+z", 2.5), Candidate("y", 2.0) })));
    }

    [TestMethod]
    public void VetoMotifPushesDown() {
        var input = Input(views: Veto("V"));
        var r = Rank(input, 0.25, candidates: new[] { Candidate("V", 3.0), Candidate("x", 2.875) });
        Assert.AreEqual("x,V", Order(r));
        Assert.AreEqual(-1, r.Ranked[1].Term);
        Assert.AreEqual("V,x", Order(Rank(Input(views: Veto("V", excludes: false)), 0.25, candidates: new[] { Candidate("V", 3.0), Candidate("x", 2.875) })),
                        "a veto_blocked rule without veto_excludes_motif moves nothing");
    }

    [TestMethod]
    public void MatchingBothFiredAndVetoMotifsGivesZero() {
        var r = Rank(Input(views: new[] { Fired("M"), Veto("V") }), 0.25, candidates: new[] { Candidate("x", 3.0), Candidate("M+V", 2.875) });
        Assert.AreEqual("x,M+V", Order(r));
        Assert.AreEqual(0, r.Ranked[1].Term);
        CollectionAssert.AreEqual(new[] { "M", "V" }, r.Ranked[1].MatchedMotifs.ToArray());
    }

    [TestMethod]
    public void ConflictSuppressesTheTerm() {
        var candidates = new[] { Candidate("x", 3.0), Candidate("M", 2.875) };
        var r = Rank(Input(eligibleConflict: true, conflict: true, views: Fired("M")), 0.25, candidates: candidates);
        Assert.AreEqual(MsfinderDiagnosticRanking.StatusConflictSuppressed, r.Status);
        Assert.AreEqual("x,M", Order(r));
        Assert.IsTrue(r.Ranked.All(c => c.Term == 0));
        // Under Measured, the conflict over every fired outcome suppresses (OQ20).
        Assert.AreEqual(MsfinderDiagnosticRanking.StatusConflictSuppressed,
                        Rank(Input(conflict: true, views: Fired("M", eligible: false)), 0.25, DiagnosticRuleRankingScope.Measured, candidates).Status);
        Assert.AreEqual(MsfinderDiagnosticRanking.StatusApplied,
                        Rank(Input(conflict: true, views: Fired("M")), 0.25, DiagnosticRuleRankingScope.Reviewed, candidates).Status);
    }

    [TestMethod]
    public void ScopeDecidesWhichOutcomesCount() {
        var candidates = new[] { Candidate("x", 3.0), Candidate("M", 2.875) };
        var ineligible = Input(views: Fired("M", eligible: false));
        Assert.AreEqual(MsfinderDiagnosticRanking.StatusNoOutcome, Rank(ineligible, 0.25, DiagnosticRuleRankingScope.Reviewed, candidates).Status);
        Assert.AreEqual("x,M", Order(Rank(ineligible, 0.25, DiagnosticRuleRankingScope.Reviewed, candidates)));
        Assert.AreEqual("M,x", Order(Rank(ineligible, 0.25, DiagnosticRuleRankingScope.Measured, candidates)));
    }

    [TestMethod]
    public void NoOutcomeKeepsTheBaseOrder() {
        var candidates = new[] { Candidate("x", 3.0), Candidate("M", 2.875) };
        var r = MsfinderDiagnosticRanking.Rank(candidates, DiagnosticRankingInput.None, DiagnosticRuleRankingScope.Measured, 0.25, Matches);
        Assert.AreEqual(MsfinderDiagnosticRanking.StatusNoOutcome, r.Status);
        Assert.AreEqual("x,M", Order(r));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => MsfinderDiagnosticRanking.Rank(candidates, DiagnosticRankingInput.None, DiagnosticRuleRankingScope.Measured, -0.5, Matches));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => MsfinderDiagnosticRanking.Rank(candidates, DiagnosticRankingInput.None, DiagnosticRuleRankingScope.Measured, double.NaN, Matches));
    }

    /// <summary>
    /// With the shipped set (every rule author_reviewed:false) a fired rule is claim-ineligible (not_reviewed, and
    /// tolerance_differs for an msfinder rule under another tolerance), so under Reviewed nothing moves; under
    /// Measured the rule's own positive example is lifted. Real evaluation and the real NCDK matcher.
    /// </summary>
    [TestMethod]
    public void RealOutcomeMovesNothingUnderReviewedAndLiftsUnderMeasured() {
        var reviewed = CreateRuntime(new MsfinderDiagnosticRuleSettings { Evaluation = true, RankingWeight = 0.5 });
        var measured = CreateRuntime(new MsfinderDiagnosticRuleSettings { Evaluation = true, RankingWeight = 0.5, RankingScope = DiagnosticRuleRankingScope.Measured });
        Assert.IsNotNull(reviewed.Matcher);
        var dir = NewTempDirectory();
        try {
            var tested = 0;
            for (var i = 0; i < Fixtures.Count && tested < 5; i++) {
                var f = Fixtures[i];
                var e = measured.Evaluate(WriteAndRead(dir, "r" + i + ".msp", Msp(f, null)), false);
                if (!AnyFired(e.Result) || e.Result!.RuleConflict) {
                    continue;
                }
                var outcome = e.Result.Outcomes.First(o => o.Fired);
                Assert.IsFalse(outcome.ClaimEligible);
                CollectionAssert.Contains(outcome.ClaimIneligible.ToList(), RuleOutcome.NotReviewed);
                var rule = measured.RuleSet.Rules.First(r => r.RuleId == outcome.RuleId);
                var positive = rule.Claim.PositiveExamples[0].Smiles;
                var candidates = new[] { Candidate("CCCCCCCC", 3.0), Candidate(positive, 2.75) };

                var under = MsfinderDiagnosticRanking.Rank(candidates, e.Result, reviewed);
                Assert.AreEqual(MsfinderDiagnosticRanking.StatusNoOutcome, under.Status, f.Label);
                Assert.AreEqual(candidates[0], under.Ranked[0].Candidate, f.Label);

                var lifted = MsfinderDiagnosticRanking.Rank(candidates, e.Result, measured);
                Assert.AreEqual(MsfinderDiagnosticRanking.StatusApplied, lifted.Status, f.Label);
                Assert.AreEqual(candidates[1], lifted.Ranked[0].Candidate, f.Label);
                Assert.AreEqual(1, lifted.Ranked[0].Term, f.Label);
                tested++;
            }
            Assert.AreEqual(5, tested);
        }
        finally {
            DeleteDirectory(dir);
        }
    }

    /// <summary>A candidate's formula, InChIKey and ontology never change its term (design 6.1 item 6).</summary>
    [TestMethod]
    public void RankingTermIgnoresCandidateFormulaFields() {
        var input = Input(views: new[] { Fired("M"), Veto("V") });
        var smiles = new[] { "x", "M", "V", "M+V", "M+z" };
        var clean = smiles.Select((s, i) => Candidate(s, 3.0 - 0.125 * i)).ToArray();
        var rng = new Random(20261118);
        foreach (var poison in new Func<int, (string?, string?, string?)>[] {
                     i => (null, null, null), i => ("", "", ""), i => ("M", "V", "M"), i => (RandomFormula(rng), Garbage(rng), Garbage(rng)) }) {
            var poisoned = smiles.Select((s, i) => {
                var p = poison(i);
                var c = Candidate(s, 3.0 - 0.125 * i);
                Set(c, "formula", p.Item1);
                Set(c, "inchikey", p.Item2);
                Set(c, "ontology", p.Item3);
                return c;
            }).ToArray();
            var a = Rank(input, 0.25, candidates: clean);
            var b = Rank(input, 0.25, candidates: poisoned);
            CollectionAssert.AreEqual(a.Ranked.Select(c => c.Term).ToArray(), b.Ranked.Select(c => c.Term).ToArray());
            CollectionAssert.AreEqual(a.Ranked.Select(c => c.Candidate.Smiles).ToArray(), b.Ranked.Select(c => c.Candidate.Smiles).ToArray());
            CollectionAssert.AreEqual(a.Ranked.Select(c => c.RankingScore).ToArray(), b.Ranked.Select(c => c.RankingScore).ToArray());
        }
    }
}
