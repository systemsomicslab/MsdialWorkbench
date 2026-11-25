using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>One outcome's ChemOnt relations (design 5.7), report only.</summary>
    public sealed class ChemOntComparison
    {
        public ChemOntComparison(string ruleId, string motifId, string topFormula, string baseRank1, string rankedRank1) {
            RuleId = ruleId;
            MotifId = motifId;
            TopFormula = topFormula;
            BaseRank1 = baseRank1;
            RankedRank1 = rankedRank1;
        }

        public string RuleId { get; }
        public string MotifId { get; }
        /// <summary>(a) against the top formula's FSEA term.</summary>
        public string TopFormula { get; }
        /// <summary>(b) against the base rank-1 candidate's ClassyFire class (the independent comparison).</summary>
        public string BaseRank1 { get; }
        /// <summary>(c) against the rule-adjusted rank-1 candidate's class (partly circular).</summary>
        public string RankedRank1 { get; }
    }

    /// <summary>
    /// Compares a fired motif with a ChemOnt class name (design 5.7): <c>agree</c> when the name is one of the
    /// motif's <c>chemont.compatible</c> nodes, <c>broader</c> when it is one of its <c>chemont.broader</c> nodes,
    /// <c>differs</c> otherwise, and <c>not_comparable</c> when there is no name or the motif has no mapping. By name
    /// only (ordinal, after trimming), because MS-FINDER keeps no ChemOnt id on read-back. Nothing is overwritten.
    /// </summary>
    public static class ChemOntAgreement
    {
        public const string Agree = "agree";
        public const string Broader = "broader";
        public const string Differs = "differs";
        public const string NotComparable = "not_comparable";

        /// <summary>The relation of <paramref name="motif"/> to the class name <paramref name="term"/>.</summary>
        public static string Relation(MotifRow motif, string term) {
            if (motif == null || string.IsNullOrWhiteSpace(term) || motif.Compatible.Count == 0 && motif.Broader.Count == 0) {
                return NotComparable;
            }
            var name = term.Trim();
            foreach (var node in motif.Compatible) {
                if (string.Equals(node.Name, name, StringComparison.Ordinal)) {
                    return Agree;
                }
            }
            foreach (var node in motif.Broader) {
                if (string.Equals(node.Name, name, StringComparison.Ordinal)) {
                    return Broader;
                }
            }
            return Differs;
        }

        /// <summary>The relation of the set's motif <paramref name="motifId"/>; an unknown motif is not comparable.</summary>
        public static string Relation(DiagnosticRuleSet set, string motifId, string term) => Relation(Motif(set, motifId), term);

        /// <summary>The motif row of <paramref name="motifId"/>, or null.</summary>
        public static MotifRow Motif(DiagnosticRuleSet set, string motifId) {
            foreach (var m in set.Motifs) {
                if (string.Equals(m.MotifId, motifId, StringComparison.Ordinal)) {
                    return m;
                }
            }
            return null;
        }

        /// <summary>
        /// One comparison per outcome of <paramref name="result"/>, in result order. Outcomes that did not fire or
        /// are not eligible under <paramref name="scope"/> are <c>not_comparable</c> in all three (OQ06).
        /// </summary>
        /// <param name="topFormulaTerm">(a) <c>ChemOntDescriptions[0]</c> of the top formula, or null.</param>
        /// <param name="baseRank1Ontology">(b) <c>Ontology</c> of the base rank-1 candidate, or null.</param>
        /// <param name="rankedRank1Ontology">(c) <c>Ontology</c> of the ranked rank-1 candidate, or null.</param>
        public static IReadOnlyList<ChemOntComparison> Compare(DiagnosticRuleResult result, DiagnosticRuleSet set, DiagnosticRuleRankingScope scope,
                                                              string topFormulaTerm, string baseRank1Ontology, string rankedRank1Ontology) {
            var rows = new List<ChemOntComparison>();
            foreach (var view in RankingOutcomeView.Of(result, set)) {
                if (!view.Fired || !view.EligibleUnder(scope)) {
                    rows.Add(new ChemOntComparison(view.RuleId, view.MotifId, NotComparable, NotComparable, NotComparable));
                    continue;
                }
                var motif = Motif(set, view.MotifId);
                rows.Add(new ChemOntComparison(view.RuleId, view.MotifId, Relation(motif, topFormulaTerm),
                                               Relation(motif, baseRank1Ontology), Relation(motif, rankedRank1Ontology)));
            }
            return rows;
        }
    }
}
