#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using Riken.Metabolomics.StructureFinder.Result;
using System.Collections.Generic;
using System.Linq;
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticQueryFiles;
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticRankingTests;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>The display-only candidate agreement with the diagnostic-ion rules.</summary>
[TestClass]
public class DiagnosticRuleAgreementTests
{
    /// <summary>A fake matcher: SMILES "A+B" has motifs A and B.</summary>
    private static bool Matches(string smiles, string motif) => smiles.Split('+').Contains(motif);

    private static RankingOutcomeView Fired(string motif, bool eligible = true) => new RankingOutcomeView("r." + motif, motif, true, false, false, eligible);

    private static RankingOutcomeView Veto(string motif, bool excludes = true, bool eligible = true) => new RankingOutcomeView("v." + motif, motif, false, true, excludes, eligible);

    private static DiagnosticRankingInput Input(bool eligibleConflict = false, bool conflict = false, params RankingOutcomeView[] views) =>
        new DiagnosticRankingInput(views, eligibleConflict, conflict, true);

    private static List<string> Labels(DiagnosticRankingInput input, DiagnosticRuleRankingScope scope, params FragmenterResult[] candidates) =>
        DiagnosticRuleAgreement.Of(candidates, input, scope, Matches).Select(a => a.Label).ToList();

    [TestMethod]
    public void ReviewedScopeLabelsClaimEligibleFiredAndVetoedMotifs() {
        var input = Input(views: new[] { Fired("A"), Veto("V"), Fired("U", eligible: false) });
        var labels = Labels(input, DiagnosticRuleRankingScope.Reviewed,
            Candidate("A", 1), Candidate("V", 1), Candidate("A+V", 1), Candidate("U", 1), Candidate("x", 1));
        CollectionAssert.AreEqual(new[]
        {
            DiagnosticRuleAgreement.ConsistentEligible, DiagnosticRuleAgreement.VetoEligible, DiagnosticRuleAgreement.Mixed,
            "", // an unreviewed or claim-ineligible rule does not count under Reviewed
            "",
        }, labels);
    }

    [TestMethod]
    public void MeasuredScopeTellsUnreviewedOrIneligibleRulesApart() {
        var input = Input(views: new[] { Fired("A"), Fired("U", eligible: false), Veto("W", eligible: false), Veto("N", excludes: false) });
        var labels = Labels(input, DiagnosticRuleRankingScope.Measured,
            Candidate("A", 1), Candidate("U", 1), Candidate("A+U", 1), Candidate("W", 1), Candidate("N", 1));
        CollectionAssert.AreEqual(new[]
        {
            DiagnosticRuleAgreement.ConsistentEligible, DiagnosticRuleAgreement.ConsistentNotEligible, DiagnosticRuleAgreement.ConsistentEligible,
            DiagnosticRuleAgreement.VetoNotEligible,
            "", // a veto whose rule does not exclude the motif is not a veto of the candidate (as in the ranking)
        }, labels);
    }

    [TestMethod]
    public void ConflictKeepsTheMatchButSaysSo() {
        var reviewed = Labels(Input(eligibleConflict: true, conflict: true, views: Fired("A")), DiagnosticRuleRankingScope.Reviewed, Candidate("A", 1), Candidate("x", 1));
        CollectionAssert.AreEqual(new[] { DiagnosticRuleAgreement.ConsistentEligible + DiagnosticRuleAgreement.ConflictSuffix, "" }, reviewed);
        // only rule_conflict (an ineligible rule disagrees): Reviewed has no conflict, Measured has
        var input = Input(eligibleConflict: false, conflict: true, views: Fired("A"));
        Assert.AreEqual(DiagnosticRuleAgreement.ConsistentEligible, Labels(input, DiagnosticRuleRankingScope.Reviewed, Candidate("A", 1))[0]);
        Assert.AreEqual(DiagnosticRuleAgreement.ConsistentEligible + DiagnosticRuleAgreement.ConflictSuffix, Labels(input, DiagnosticRuleRankingScope.Measured, Candidate("A", 1))[0]);
    }

    [TestMethod]
    public void MotifsAndDisplayText() {
        var a = DiagnosticRuleAgreement.Of(new[] { Candidate("B+A+V", 1), Candidate("x", 1) }, Input(views: new[] { Fired("B"), Fired("A"), Veto("V") }),
            DiagnosticRuleRankingScope.Reviewed, Matches);
        CollectionAssert.AreEqual(new[] { "A", "B" }, a[0].FiredMotifs.ToList());
        CollectionAssert.AreEqual(new[] { "V" }, a[0].VetoMotifs.ToList());
        Assert.AreEqual(DiagnosticRuleAgreement.Mixed + " [A;B;V]", a[0].DisplayText);
        Assert.AreEqual("", a[1].DisplayText);
    }

    [TestMethod]
    public void NoMatcherNoResultOrNoMotifGivesBlankRows() {
        var candidates = new[] { Candidate("A", 1), Candidate("x", 1) };
        foreach (var rows in new[]
        {
            DiagnosticRuleAgreement.Of(candidates, Input(views: Fired("A")), DiagnosticRuleRankingScope.Reviewed, null),
            DiagnosticRuleAgreement.Of(candidates, DiagnosticRankingInput.None, DiagnosticRuleRankingScope.Reviewed, Matches),
            DiagnosticRuleAgreement.Of(candidates, null, DiagnosticRuleRankingScope.Reviewed, Matches),
            DiagnosticRuleAgreement.Of(candidates, Input(views: Fired("A", eligible: false)), DiagnosticRuleRankingScope.Reviewed, Matches),
        }) {
            Assert.AreEqual(2, rows.Count);
            Assert.IsTrue(rows.All(r => r.Label == "" && r.FiredMotifs.Count == 0 && r.VetoMotifs.Count == 0));
        }
    }

    [TestMethod]
    public void AgreementNeverReorders() {
        // the low-score candidate is the only consistent one; the agreement follows the order given at any weight
        var candidates = new[] { Candidate("x", 3.0), Candidate("y", 2.0), Candidate("A", 1.0) };
        var input = Input(views: Fired("A"));
        var zero = MsfinderDiagnosticRanking.Rank(candidates, input, DiagnosticRuleRankingScope.Reviewed, 0.0, Matches);
        var withAgreement = zero.WithAgreements(DiagnosticRuleAgreement.Of(zero.Candidates(), input, DiagnosticRuleRankingScope.Reviewed, Matches));
        Assert.AreEqual(MsfinderDiagnosticRanking.StatusWeightZero, withAgreement.Status);
        CollectionAssert.AreEqual(new[] { "x", "y", "A" }, withAgreement.Ranked.Select(r => r.Candidate.Smiles).ToList());
        CollectionAssert.AreEqual(new[] { "", "", DiagnosticRuleAgreement.ConsistentEligible }, Enumerable.Range(0, 3).Select(i => withAgreement.AgreementAt(i).Label).ToList());
        Assert.IsTrue(withAgreement.Ranked.All(r => r.Term == 0 && r.MatchedMotifs.Count == 0));
        // the same candidate in a different order keeps its own label
        var reversed = DiagnosticRuleAgreement.Of(candidates.Reverse().ToList(), input, DiagnosticRuleRankingScope.Reviewed, Matches);
        CollectionAssert.AreEqual(new[] { DiagnosticRuleAgreement.ConsistentEligible, "", "" }, reversed.Select(r => r.Label).ToList());
        Assert.ThrowsException<System.ArgumentException>(() => zero.WithAgreements(reversed.Take(2).ToList()));
    }

    [TestMethod]
    public void ReviewedRulesMarkTheirPositiveExamplesAtWeightZero() {
        // the default runtime (w = 0, Reviewed scope) over the rule fixtures: a query whose own rule fires
        // claim-eligibly labels that rule's positive example consistent (reviewed, claim-eligible)
        var runtime = Runtime;
        Assert.IsFalse(runtime.RankingOn);
        var dir = NewTempDirectory();
        try {
            var seen = new HashSet<string>();
            foreach (var f in Fixtures.Where(x => x.Positive)) {
                var result = runtime.Evaluate(WriteAndRead(dir, "q.msp", Msp(f, null)), false).Result;
                if (result == null || !result.Outcomes.Any(o => o.RuleId == f.RuleId && o.Fired && o.ClaimEligible)) continue;
                var rule = runtime.RuleSet.Rules.First(r => r.RuleId == f.RuleId);
                var positive = Candidate(rule.Claim.PositiveExamples.First().Smiles, 1.0);
                var agreement = DiagnosticRuleAgreement.Of(new[] { positive }, result, runtime)[0];
                StringAssert.StartsWith(agreement.Label, DiagnosticRuleAgreement.ConsistentEligible, f.Label);
                CollectionAssert.Contains(agreement.FiredMotifs.ToList(), rule.Claim.MotifId, f.Label);
                seen.Add(f.RuleId);
            }
            Assert.IsTrue(seen.Count > 0, "non-vacuous: some reviewed rule fires claim-eligibly on its fixture");
        }
        finally {
            DeleteDirectory(dir);
        }
    }

    [TestMethod]
    public void RuntimeHasAnAgreementMatcherAtWeightZeroOnly() {
        var zero = DiagnosticRuleRuntime.Create(new MsfinderDiagnosticRuleSettings { Evaluation = true }, ProductTolerance.Da(0.01), out var error);
        Assert.IsNotNull(zero, error);
        Assert.IsNull(zero!.Matcher, "w = 0 still builds no ranking matcher");
        Assert.IsFalse(zero.RankingOn);
        Assert.IsNotNull(zero.AgreementMatcher, zero.AgreementError);
        Assert.IsNull(zero.AgreementError);
        Assert.AreSame(zero.AgreementMatcher, zero.AgreementMatcher, "compiled once");

        var weighted = DiagnosticRuleRuntime.Create(new MsfinderDiagnosticRuleSettings { Evaluation = true, RankingWeight = 1.0 }, ProductTolerance.Da(0.01), out error);
        Assert.IsNotNull(weighted, error);
        Assert.AreSame(weighted!.Matcher, weighted.AgreementMatcher, "w > 0 shares the ranking matcher");
    }
}
