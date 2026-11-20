using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Results
{
    /// <summary>One hypothesis of one rule that passed the precursor predicate, and its evaluation (design 4.11).</summary>
    public sealed class HypothesisOutcome
    {
        /// <summary>The logic is false even with every not node true.</summary>
        public const string FailureLogicFalse = "logic_false";
        /// <summary>The logic holds with every not node true, but a veto blocks it.</summary>
        public const string FailureVeto = "veto";
        /// <summary>The p2 convention found no base peak (no usable peak more than 1.0 Da from the precursor).</summary>
        public const string FailureNoBasePeak = "no_base_peak";
        /// <summary>The exclusive assignment visited more leaves than AssignmentCap.</summary>
        public const string FailureAssignmentOverflow = "assignment_overflow";

        internal HypothesisOutcome(string hypothesisId, string compositionId, string? scaffoldId, IReadOnlyList<KeyValuePair<string, long>> units,
                                   string adduct, string adductSource, string adductTier, string neutralFormula, double hypothesisMz,
                                   double precursorErrorDa, bool fired, bool firedIgnoringVetoes, double score, string? failure,
                                   IReadOnlyList<MatchedTerm> matched, IReadOnlyList<VetoOutcome> vetoes, IReadOnlyList<KOfOutcome> kOf,
                                   IReadOnlyList<BranchOutcome> branches, IReadOnlyList<MatchedTerm> upgrades) {
            HypothesisId = hypothesisId;
            CompositionId = compositionId;
            ScaffoldId = scaffoldId;
            Units = units;
            Adduct = adduct;
            AdductSource = adductSource;
            AdductTier = adductTier;
            NeutralFormula = neutralFormula;
            HypothesisMz = hypothesisMz;
            PrecursorErrorDa = precursorErrorDa;
            Fired = fired;
            FiredIgnoringVetoes = firedIgnoringVetoes;
            Score = score;
            Failure = failure;
            Matched = matched;
            Vetoes = vetoes;
            KOf = kOf;
            Branches = branches;
            Upgrades = upgrades;
        }

        /// <summary><c>&lt;composition_id&gt;|&lt;canonical adduct&gt;</c>.</summary>
        public string HypothesisId { get; }

        /// <summary><c>&lt;scaffold_id|core&gt;|&lt;units&gt;</c>, units in table order, zero counts omitted.</summary>
        public string CompositionId { get; }

        /// <summary>Null when the rule has no scaffold list (the composition id still says core).</summary>
        public string? ScaffoldId { get; }

        /// <summary>The non-zero unit counts, in units-table order.</summary>
        public IReadOnlyList<KeyValuePair<string, long>> Units { get; }

        public string Adduct { get; }

        /// <summary><c>rule</c> (the rule's own list) or <c>common</c> (a common adduct of the polarity).</summary>
        public string AdductSource { get; }

        /// <summary><c>cross_polarity</c>, <c>within_polarity_pair</c>, <c>curated</c> or <c>assumed</c>.</summary>
        public string AdductTier { get; }

        /// <summary>The Hill formula of the rule's own neutral composition.</summary>
        public string NeutralFormula { get; }

        public double HypothesisMz { get; }

        /// <summary>P_obs - <see cref="HypothesisMz"/>.</summary>
        public double PrecursorErrorDa { get; }

        public bool Fired { get; }

        /// <summary>The logic with every not node true, under the assignment chosen for the full logic.</summary>
        public bool FiredIgnoringVetoes { get; }

        /// <summary>The sum over evidence units, in canonical order, of the unit's relative intensity.</summary>
        public double Score { get; }

        /// <summary>Null when the hypothesis fired; otherwise one of the Failure constants.</summary>
        public string? Failure { get; }

        /// <summary>The items with an assigned peak, in canonical item order.</summary>
        public IReadOnlyList<MatchedTerm> Matched { get; }

        /// <summary>Every veto term, in term order.</summary>
        public IReadOnlyList<VetoOutcome> Vetoes { get; }

        /// <summary>Every k_of node, in pre-order.</summary>
        public IReadOnlyList<KOfOutcome> KOf { get; }

        /// <summary>Every any node, in pre-order.</summary>
        public IReadOnlyList<BranchOutcome> Branches { get; }

        /// <summary>Per upgrade member, the peak it would match; reported for the chosen hypothesis only, never
        /// claimed.</summary>
        public IReadOnlyList<MatchedTerm> Upgrades { get; }
    }
}
