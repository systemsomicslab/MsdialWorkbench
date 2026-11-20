using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Results
{
    /// <summary>One evaluated rule for one spectrum (design 4.9, 4.11).</summary>
    public sealed class RuleOutcome
    {
        /// <summary>claim_ineligible: the rule is not author_reviewed (or has no review).</summary>
        public const string NotReviewed = "not_reviewed";
        /// <summary>claim_ineligible: the spectrum's instrument class is not one the review covers.</summary>
        public const string InstrumentClassNotReviewed = "instrument_class";
        /// <summary>claim_ineligible: the evaluation's mode is not the rule's primary_precondition_mode.</summary>
        public const string PreconditionModeDiffers = "precondition_mode";
        /// <summary>claim_ineligible: the effective product or precursor tolerance is not the reviewed one.</summary>
        public const string ToleranceDiffers = "tolerance_differs";
        /// <summary>claim_ineligible: a variant option is active, or the rule fails a two-ion pass at the KOffset.</summary>
        public const string VariantActive = "variant_active";

        /// <summary>precondition_failure: the rule's ion mode is not the spectrum's, so it was not tried.</summary>
        public const string PreconditionIonMode = "ion_mode";
        /// <summary>precondition_failure: mz_only, and the evidence adduct is not in the rule's own list.</summary>
        public const string PreconditionAdductNotInRule = "adduct_not_in_rule";

        internal RuleOutcome(string ruleId, long version, string motifId, string claimLevel, bool authorReviewed, bool sourceChecked,
                             string parametersOrigin, string convention, string conventionLabel, double productToleranceAbsDa,
                             double productTolerancePpm, string productToleranceSource, double precursorToleranceDa, string lossReference,
                             string? preconditionFailure, int nHypotheses, int nDistinctFormulas, int nHypothesesFired, bool fired,
                             bool vetoBlocked, bool claimEligible, IReadOnlyList<string> claimIneligible, bool variantSingleIon,
                             bool withinCalibration, HypothesisOutcome? chosen, IReadOnlyList<string> tiedHypothesisIds,
                             IReadOnlyList<BranchOutcome> branchFired, IReadOnlyList<HypothesisOutcome> all) {
            RuleId = ruleId;
            Version = version;
            MotifId = motifId;
            ClaimLevel = claimLevel;
            AuthorReviewed = authorReviewed;
            SourceChecked = sourceChecked;
            ParametersOrigin = parametersOrigin;
            Convention = convention;
            ConventionLabel = conventionLabel;
            ProductToleranceAbsDa = productToleranceAbsDa;
            ProductTolerancePpm = productTolerancePpm;
            ProductToleranceSource = productToleranceSource;
            PrecursorToleranceDa = precursorToleranceDa;
            LossReference = lossReference;
            PreconditionFailure = preconditionFailure;
            NHypotheses = nHypotheses;
            NDistinctFormulas = nDistinctFormulas;
            NHypothesesFired = nHypothesesFired;
            Fired = fired;
            VetoBlocked = vetoBlocked;
            ClaimEligible = claimEligible;
            ClaimIneligible = claimIneligible;
            VariantSingleIon = variantSingleIon;
            WithinCalibration = withinCalibration;
            Chosen = chosen;
            TiedHypothesisIds = tiedHypothesisIds;
            BranchFired = branchFired;
            All = all;
        }

        public string RuleId { get; }
        public long Version { get; }
        public string MotifId { get; }
        /// <summary><c>class</c>, <c>scaffold</c> or <c>substituent</c>.</summary>
        public string ClaimLevel { get; }
        public bool AuthorReviewed { get; }
        public bool SourceChecked { get; }
        /// <summary><c>as_published</c>, <c>tuned_vs20</c> or <c>tuned_pooled</c>.</summary>
        public string ParametersOrigin { get; }
        /// <summary><c>p2</c> or <c>msdial</c>.</summary>
        public string Convention { get; }
        /// <summary><c>exact</c> (p2) or <c>approximate</c> (msdial).</summary>
        public string ConventionLabel { get; }
        /// <summary>The effective product tolerance is max(abs_da, t * ppm / 1e6) at target m/z t.</summary>
        public double ProductToleranceAbsDa { get; }
        public double ProductTolerancePpm { get; }
        /// <summary><c>p2</c>, <c>msfinder</c> or <c>override</c>.</summary>
        public string ProductToleranceSource { get; }
        /// <summary>The effective precursor tolerance in Da.</summary>
        public double PrecursorToleranceDa { get; }
        /// <summary>The effective loss reference: <c>hypothesis</c> or <c>observed</c>.</summary>
        public string LossReference { get; }

        /// <summary>
        /// Why the rule has no hypotheses before any precursor comparison (OQ03, OQ04): <c>ion_mode</c>,
        /// <c>adduct_not_in_rule</c>, or null. Compact results list only outcomes with hypotheses, so it shows in
        /// verbose results.
        /// </summary>
        public string? PreconditionFailure { get; }

        /// <summary>The hypotheses that pass the precursor predicate.</summary>
        public int NHypotheses { get; }
        /// <summary>The distinct neutral formulas among them (OQ16).</summary>
        public int NDistinctFormulas { get; }
        public int NHypothesesFired { get; }
        /// <summary>Some hypothesis fired.</summary>
        public bool Fired { get; }
        /// <summary>No hypothesis fired, and some hypothesis fired with every not node true.</summary>
        public bool VetoBlocked { get; }
        public bool ClaimEligible { get; }
        /// <summary>Every failed eligibility condition, in the order not_reviewed, instrument_class,
        /// precondition_mode, tolerance_differs, variant_active.</summary>
        public IReadOnlyList<string> ClaimIneligible { get; }
        /// <summary>A measure:true rule that fails a two-ion pass at the effective KOffset.</summary>
        public bool VariantSingleIon { get; }
        /// <summary>The spectrum's instrument class is in the rule's calibrated_on (report only).</summary>
        public bool WithinCalibration { get; }

        /// <summary>
        /// The fired hypothesis with the highest score, then the lowest hypothesis_id (ordinal). When none fired but
        /// some fired with every not node true (<see cref="VetoBlocked"/>), the best of those, so a blocked rule shows
        /// its hypothesis and the blocking vetoes; otherwise null (OQ05).
        /// </summary>
        public HypothesisOutcome? Chosen { get; }

        /// <summary>The fired hypotheses with exactly the chosen score, the chosen one included, in ordinal order;
        /// empty when the rule did not fire (OQ26).</summary>
        public IReadOnlyList<string> TiedHypothesisIds { get; }

        /// <summary>Per any node, whether the rule fires with the node restricted to each child (design 4.7, OQ14),
        /// for every any node, those inside a not included (see <see cref="BranchOutcome"/>).</summary>
        public IReadOnlyList<BranchOutcome> BranchFired { get; }

        /// <summary>Every hypothesis in hypothesis_id order in verbose results; empty otherwise (OQ24).</summary>
        public IReadOnlyList<HypothesisOutcome> All { get; }
    }
}
