using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>How one structure candidate agrees with the diagnostic-ion rules of its query. Display only: it never
    /// changes the order of the candidates.</summary>
    public sealed class CandidateRuleAgreement
    {
        public CandidateRuleAgreement(string label, IReadOnlyList<string> firedMotifs, IReadOnlyList<string> vetoMotifs) {
            Label = label ?? string.Empty;
            FiredMotifs = firedMotifs ?? DiagnosticRuleAgreement.NoMotifs;
            VetoMotifs = vetoMotifs ?? DiagnosticRuleAgreement.NoMotifs;
        }

        /// <summary>One of the <c>DiagnosticRuleAgreement.*</c> labels, possibly with
        /// <see cref="DiagnosticRuleAgreement.ConflictSuffix"/>; empty when the candidate matches no motif.</summary>
        public string Label { get; }
        /// <summary>The eligible fired motifs the candidate has, in ordinal order.</summary>
        public IReadOnlyList<string> FiredMotifs { get; }
        /// <summary>The eligible vetoed motifs (<c>veto_excludes_motif</c>) the candidate has, in ordinal order.</summary>
        public IReadOnlyList<string> VetoMotifs { get; }

        /// <summary>The label followed by the matched motifs in brackets, for a single grid cell.</summary>
        public string DisplayText {
            get {
                var motifs = FiredMotifs.Concat(VetoMotifs).Distinct(StringComparer.Ordinal).ToList();
                return motifs.Count == 0 ? Label : Label + " [" + string.Join(";", motifs) + "]";
            }
        }

        public static CandidateRuleAgreement None { get; } = new CandidateRuleAgreement(string.Empty, null, null);
    }

    /// <summary>
    /// The per-candidate agreement with the fired and vetoed diagnostic-ion rules of a query, with the same eligible
    /// motifs (F and V of <see cref="DiagnosticRankingInput"/>), scope and motif matcher as
    /// <see cref="MsfinderDiagnosticRanking"/>, but for display only: it is computed whatever the ranking weight and
    /// never reorders. Under the Reviewed scope only claim-eligible outcomes count, and claim eligibility already
    /// requires an author-reviewed rule, the instrument class and the tolerance; under the Measured scope an outcome
    /// that is not claim-eligible counts too, and the label says so.
    /// </summary>
    public static class DiagnosticRuleAgreement
    {
        /// <summary>The candidate has a fired motif of a claim-eligible (author-reviewed) rule and no vetoed one.</summary>
        public const string ConsistentEligible = "consistent (reviewed, claim-eligible)";
        /// <summary>As <see cref="ConsistentEligible"/>, but every matching fired rule is unreviewed or not claim-eligible
        /// (Measured scope only).</summary>
        public const string ConsistentNotEligible = "consistent (unreviewed or not claim-eligible)";
        /// <summary>The candidate has a vetoed motif of a claim-eligible rule and no fired one.</summary>
        public const string VetoEligible = "veto (reviewed, claim-eligible)";
        /// <summary>As <see cref="VetoEligible"/>, but every matching vetoed rule is unreviewed or not claim-eligible.</summary>
        public const string VetoNotEligible = "veto (unreviewed or not claim-eligible)";
        /// <summary>The candidate has both a fired and a vetoed motif.</summary>
        public const string Mixed = "mixed (consistent and veto)";
        /// <summary>Appended to a non-empty label when the fired rules of the query conflict under the scope.</summary>
        public const string ConflictSuffix = "; rules conflict";

        internal static readonly IReadOnlyList<string> NoMotifs = new string[0];

        /// <summary>The agreement of each candidate with the runtime's scope and agreement matcher, in the order given;
        /// every agreement is <see cref="CandidateRuleAgreement.None"/> when the matcher is unavailable.</summary>
        public static IReadOnlyList<CandidateRuleAgreement> Of(IReadOnlyList<FragmenterResult> candidates, CompMs.Common.StructureFinder.DiagnosticRules.Results.DiagnosticRuleResult result,
                                                               DiagnosticRuleRuntime runtime) {
            if (runtime == null) {
                throw new ArgumentNullException(nameof(runtime));
            }
            var matcher = runtime.AgreementMatcher;
            Func<string, string, bool> matches = null;
            if (matcher != null) {
                matches = matcher.Matches;
            }
            return Of(candidates, DiagnosticRankingInput.Of(result, runtime.RuleSet), runtime.RankingScope, matches);
        }

        /// <summary>The agreement of each candidate, in the order given (never reordered).</summary>
        /// <param name="matches">(SMILES, motif id) to whether the structure has the motif; null gives
        /// <see cref="CandidateRuleAgreement.None"/> for every candidate.</param>
        public static IReadOnlyList<CandidateRuleAgreement> Of(IReadOnlyList<FragmenterResult> candidates, DiagnosticRankingInput input,
                                                               DiagnosticRuleRankingScope scope, Func<string, string, bool> matches) {
            if (candidates == null) {
                throw new ArgumentNullException(nameof(candidates));
            }
            input = input ?? DiagnosticRankingInput.None;
            var rows = new List<CandidateRuleAgreement>(candidates.Count);
            var fired = input.FiredMotifs(scope);
            var vetoes = input.VetoMotifs(scope);
            if (matches == null || !input.Evaluated || fired.Count == 0 && vetoes.Count == 0) {
                for (var i = 0; i < candidates.Count; i++) {
                    rows.Add(CandidateRuleAgreement.None);
                }
                return rows;
            }
            var eligibleFired = new HashSet<string>(input.Outcomes.Where(o => o.ClaimEligible && o.Fired).Select(o => o.MotifId).Where(m => m != null), StringComparer.Ordinal);
            var eligibleVetoes = new HashSet<string>(input.Outcomes.Where(o => o.ClaimEligible && o.VetoBlocked && o.VetoExcludesMotif).Select(o => o.MotifId).Where(m => m != null), StringComparer.Ordinal);
            var conflict = input.ConflictUnder(scope);
            foreach (var candidate in candidates) {
                var smiles = candidate?.Smiles;
                var f = fired.Where(m => matches(smiles, m)).ToList();
                var v = vetoes.Where(m => matches(smiles, m)).ToList();
                string label;
                if (f.Count > 0 && v.Count > 0) {
                    label = Mixed;
                }
                else if (f.Count > 0) {
                    label = f.Any(eligibleFired.Contains) ? ConsistentEligible : ConsistentNotEligible;
                }
                else if (v.Count > 0) {
                    label = v.Any(eligibleVetoes.Contains) ? VetoEligible : VetoNotEligible;
                }
                else {
                    rows.Add(CandidateRuleAgreement.None);
                    continue;
                }
                if (conflict) {
                    label += ConflictSuffix;
                }
                rows.Add(new CandidateRuleAgreement(label, f, v));
            }
            return rows;
        }
    }
}
