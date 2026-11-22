using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>What the ranking term and the ChemOnt agreement need of one rule outcome.</summary>
    public sealed class RankingOutcomeView
    {
        public RankingOutcomeView(string ruleId, string motifId, bool fired, bool vetoBlocked, bool vetoExcludesMotif, bool claimEligible) {
            RuleId = ruleId;
            MotifId = motifId;
            Fired = fired;
            VetoBlocked = vetoBlocked;
            VetoExcludesMotif = vetoExcludesMotif;
            ClaimEligible = claimEligible;
        }

        public string RuleId { get; }
        public string MotifId { get; }
        public bool Fired { get; }
        public bool VetoBlocked { get; }
        /// <summary>The rule's <c>veto_excludes_motif</c>.</summary>
        public bool VetoExcludesMotif { get; }
        public bool ClaimEligible { get; }

        /// <summary>Whether the outcome counts under the scope: Reviewed takes claim-eligible outcomes only, Measured
        /// every evaluated outcome.</summary>
        public bool EligibleUnder(DiagnosticRuleRankingScope scope) => scope == DiagnosticRuleRankingScope.Measured || ClaimEligible;

        /// <summary>The views of a result's outcomes, in result order; <c>veto_excludes_motif</c> comes from the rule
        /// set. A null result has none.</summary>
        public static IReadOnlyList<RankingOutcomeView> Of(DiagnosticRuleResult result, DiagnosticRuleSet set) {
            var views = new List<RankingOutcomeView>();
            if (result == null) {
                return views;
            }
            var excludes = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var rule in set.Rules) {
                excludes[rule.RuleId] = rule.VetoExcludesMotif;
            }
            foreach (var o in result.Outcomes) {
                excludes.TryGetValue(o.RuleId, out var vetoExcludes);
                views.Add(new RankingOutcomeView(o.RuleId, o.MotifId, o.Fired, o.VetoBlocked, vetoExcludes, o.ClaimEligible));
            }
            return views;
        }
    }

    /// <summary>The rule evidence of one query as the ranking term takes it (design 5.6).</summary>
    public sealed class DiagnosticRankingInput
    {
        public DiagnosticRankingInput(IReadOnlyList<RankingOutcomeView> outcomes, bool eligibleRuleConflict, bool ruleConflict, bool evaluated) {
            Outcomes = outcomes ?? new List<RankingOutcomeView>();
            EligibleRuleConflict = eligibleRuleConflict;
            RuleConflict = ruleConflict;
            Evaluated = evaluated;
        }

        public IReadOnlyList<RankingOutcomeView> Outcomes { get; }
        public bool EligibleRuleConflict { get; }
        public bool RuleConflict { get; }
        /// <summary>Whether the query was evaluated (not skipped, failed or left out).</summary>
        public bool Evaluated { get; }

        /// <summary>The input of a query that has no result (skipped, failed or not evaluated).</summary>
        public static DiagnosticRankingInput None { get; } = new DiagnosticRankingInput(null, false, false, false);

        /// <summary>The input of an evaluator result; a null result gives <see cref="None"/>.</summary>
        public static DiagnosticRankingInput Of(DiagnosticRuleResult result, DiagnosticRuleSet set) =>
            result == null ? None : new DiagnosticRankingInput(RankingOutcomeView.Of(result, set), result.EligibleRuleConflict, result.RuleConflict, true);

        /// <summary>
        /// The conflict that suppresses the term under the scope: <c>eligible_rule_conflict</c> under Reviewed, and
        /// <c>rule_conflict</c> (the conflict over every fired outcome) under Measured, where every outcome is
        /// eligible (OQ20).
        /// </summary>
        public bool ConflictUnder(DiagnosticRuleRankingScope scope) => scope == DiagnosticRuleRankingScope.Measured ? RuleConflict : EligibleRuleConflict;

        /// <summary>F: the motifs of the eligible fired outcomes, distinct, in ordinal order.</summary>
        public IReadOnlyList<string> FiredMotifs(DiagnosticRuleRankingScope scope) =>
            Distinct(Outcomes.Where(o => o.EligibleUnder(scope) && o.Fired).Select(o => o.MotifId));

        /// <summary>V: the motifs of the eligible <c>veto_blocked</c> outcomes whose rule has
        /// <c>veto_excludes_motif</c>, distinct, in ordinal order.</summary>
        public IReadOnlyList<string> VetoMotifs(DiagnosticRuleRankingScope scope) =>
            Distinct(Outcomes.Where(o => o.EligibleUnder(scope) && o.VetoBlocked && o.VetoExcludesMotif).Select(o => o.MotifId));

        private static List<string> Distinct(IEnumerable<string> ids) {
            var list = ids.Where(id => id != null).Distinct(StringComparer.Ordinal).ToList();
            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }

    /// <summary>One candidate after ranking.</summary>
    public sealed class RankedCandidate
    {
        public RankedCandidate(FragmenterResult candidate, int baseRank, int term, double rankingScore, IReadOnlyList<string> matchedMotifs) {
            Candidate = candidate;
            BaseRank = baseRank;
            Term = term;
            RankingScore = rankingScore;
            MatchedMotifs = matchedMotifs;
        }

        public FragmenterResult Candidate { get; }
        /// <summary>The 1-based rank in the base (legacy) order.</summary>
        public int BaseRank { get; }
        /// <summary>t: +1, -1 or 0.</summary>
        public int Term { get; }
        /// <summary>TotalScore + w * t, the sort key.</summary>
        public double RankingScore { get; }
        /// <summary>The motifs of F and V the candidate matches, in ordinal order.</summary>
        public IReadOnlyList<string> MatchedMotifs { get; }
    }

    /// <summary>The candidates of one query in ranked order, and how the term was applied.</summary>
    public sealed class DiagnosticRanking
    {
        public DiagnosticRanking(string status, IReadOnlyList<RankedCandidate> ranked, IReadOnlyList<string> firedMotifs, IReadOnlyList<string> vetoMotifs) {
            Status = status;
            Ranked = ranked;
            FiredMotifs = firedMotifs;
            VetoMotifs = vetoMotifs;
        }

        /// <summary>One of the <c>MsfinderDiagnosticRanking.Status*</c> values.</summary>
        public string Status { get; }
        public IReadOnlyList<RankedCandidate> Ranked { get; }
        public IReadOnlyList<string> FiredMotifs { get; }
        public IReadOnlyList<string> VetoMotifs { get; }

        /// <summary>The candidates in ranked order.</summary>
        public List<FragmenterResult> Candidates() => Ranked.Select(r => r.Candidate).ToList();
    }

    /// <summary>
    /// The ranking term (design 5.6): per candidate, F = eligible fired motifs it matches, V = motifs of eligible
    /// <c>veto_blocked</c> rules with <c>veto_excludes_motif</c> it matches; t = +1 if F is not empty and V is, -1 if
    /// V is not empty and F is, else 0. The ranked order is <c>baseOrder.OrderByDescending(TotalScore + w * t)</c>,
    /// which is stable, so w = 0 or no outcome keeps the base order. Only <see cref="FragmenterResult.Smiles"/> and
    /// <see cref="FragmenterResult.TotalScore"/> of a candidate are read.
    /// </summary>
    public static class MsfinderDiagnosticRanking
    {
        /// <summary>The term was computed for every candidate.</summary>
        public const string StatusApplied = "applied";
        /// <summary>The conflict of the scope (see <see cref="DiagnosticRankingInput.ConflictUnder"/>) set every t to 0.</summary>
        public const string StatusConflictSuppressed = "conflict_suppressed";
        /// <summary>The query has no result, or no motif in F or V: every t is 0.</summary>
        public const string StatusNoOutcome = "no_outcome";
        /// <summary>w = 0: the base order, no term computed.</summary>
        public const string StatusWeightZero = "weight_zero";

        /// <summary>The legacy order: <c>OrderByDescending(n =&gt; n.TotalScore)</c> over the merge order.</summary>
        public static List<FragmenterResult> BaseOrder(IEnumerable<FragmenterResult> merged) => merged.OrderByDescending(n => n.TotalScore).ToList();

        /// <summary>Ranks with the runtime's weight, scope and matcher.</summary>
        public static DiagnosticRanking Rank(IReadOnlyList<FragmenterResult> baseOrder, DiagnosticRuleResult result, DiagnosticRuleRuntime runtime) {
            var input = DiagnosticRankingInput.Of(result, runtime.RuleSet);
            var matcher = runtime.Matcher;
            Func<string, string, bool> matches = null;
            if (matcher != null) {
                matches = matcher.Matches;
            }
            return Rank(baseOrder, input, runtime.RankingScope, runtime.RankingWeight, matches);
        }

        /// <summary>
        /// Ranks <paramref name="baseOrder"/> (already in the legacy order) by TotalScore + w * t.
        /// </summary>
        /// <param name="matches">(SMILES, motif id) to whether the structure has the motif; not called when w = 0.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="weight"/> is negative or not finite.</exception>
        public static DiagnosticRanking Rank(IReadOnlyList<FragmenterResult> baseOrder, DiagnosticRankingInput input, DiagnosticRuleRankingScope scope,
                                             double weight, Func<string, string, bool> matches) {
            if (baseOrder == null) {
                throw new ArgumentNullException(nameof(baseOrder));
            }
            if (!(weight >= 0.0) || double.IsInfinity(weight)) {
                throw new ArgumentOutOfRangeException(nameof(weight));
            }
            input = input ?? DiagnosticRankingInput.None;
            var fired = input.FiredMotifs(scope);
            var vetoes = input.VetoMotifs(scope);
            var none = new string[0];

            string status;
            if (weight == 0.0) {
                status = StatusWeightZero;
            }
            else if (!input.Evaluated || fired.Count == 0 && vetoes.Count == 0) {
                status = StatusNoOutcome;
            }
            else if (input.ConflictUnder(scope)) {
                status = StatusConflictSuppressed;
            }
            else {
                status = StatusApplied;
            }
            if (status != StatusWeightZero && status != StatusNoOutcome && matches == null) {
                throw new ArgumentNullException(nameof(matches));
            }

            var rows = new List<RankedCandidate>(baseOrder.Count);
            for (var i = 0; i < baseOrder.Count; i++) {
                var candidate = baseOrder[i];
                var term = 0;
                IReadOnlyList<string> matched = none;
                if (status == StatusApplied || status == StatusConflictSuppressed) {
                    var smiles = candidate.Smiles;
                    var f = fired.Where(m => matches(smiles, m)).ToList();
                    var v = vetoes.Where(m => matches(smiles, m)).ToList();
                    if (status == StatusApplied) {
                        term = f.Count > 0 && v.Count == 0 ? 1 : v.Count > 0 && f.Count == 0 ? -1 : 0;
                    }
                    var all = f.Concat(v).Distinct(StringComparer.Ordinal).ToList();
                    all.Sort(StringComparer.Ordinal);
                    matched = all;
                }
                rows.Add(new RankedCandidate(candidate, i + 1, term, candidate.TotalScore + weight * term, matched));
            }
            var ranked = rows.OrderByDescending(r => r.Candidate.TotalScore + weight * r.Term).ToList();
            return new DiagnosticRanking(status, ranked, fired, vetoes);
        }
    }
}
