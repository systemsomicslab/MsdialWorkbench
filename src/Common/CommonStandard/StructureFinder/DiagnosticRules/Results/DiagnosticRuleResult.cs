using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Results
{
    /// <summary>
    /// What one evaluation of one spectrum found (design 4.11). <see cref="DiagnosticResultJson"/> writes it as
    /// canonical JSON; the field names below are its keys.
    /// </summary>
    public sealed class DiagnosticRuleResult
    {
        /// <summary>The spectrum was evaluated.</summary>
        public const string StatusEvaluated = "evaluated";
        /// <summary>The precursor m/z is not finite or not positive.</summary>
        public const string StatusNoPrecursor = "no_precursor";
        /// <summary>The ion mode is null.</summary>
        public const string StatusUnsupportedIonMode = "unsupported_ion_mode";
        /// <summary>No peak is usable.</summary>
        public const string StatusNoPeaks = "no_peaks";

        internal DiagnosticRuleResult(string engineSemantics, string identity, RuleSetIdentity ruleSet,
                                      IReadOnlyList<KeyValuePair<string, string>> options, string status, string preconditionMode,
                                      string adductTier, string instrumentClass, string? instrumentType, double precursorMz,
                                      double? baseIntensity, int nPeaksIn, int nPeaksDropped, string inputSha256, int rulesEvaluated,
                                      IReadOnlyList<string> notMeasured, IReadOnlyList<RuleRefusal> refusals, IReadOnlyList<RuleOutcome> outcomes,
                                      IReadOnlyList<string> firedMotifs, IReadOnlyList<string> narrowestMotifs, bool ruleConflict,
                                      bool eligibleRuleConflict) {
            EngineSemantics = engineSemantics;
            Identity = identity;
            RuleSet = ruleSet;
            Options = options;
            Status = status;
            PreconditionMode = preconditionMode;
            AdductTier = adductTier;
            InstrumentClass = instrumentClass;
            InstrumentType = instrumentType;
            PrecursorMz = precursorMz;
            BaseIntensity = baseIntensity;
            NPeaksIn = nPeaksIn;
            NPeaksDropped = nPeaksDropped;
            InputSha256 = inputSha256;
            RulesEvaluated = rulesEvaluated;
            NotMeasured = notMeasured;
            Refusals = refusals;
            Outcomes = outcomes;
            FiredMotifs = firedMotifs;
            NarrowestMotifs = narrowestMotifs;
            RuleConflict = ruleConflict;
            EligibleRuleConflict = eligibleRuleConflict;
        }

        /// <summary><see cref="DiagnosticRuleEvaluator.EngineSemantics"/>.</summary>
        public string EngineSemantics { get; }

        /// <summary><see cref="DiagnosticRuleEvaluator.IdentityString"/>.</summary>
        public string Identity { get; }

        /// <summary>The rule set: JSON writes its version, sha256, source and pinned flag.</summary>
        public RuleSetIdentity RuleSet { get; }

        /// <summary>Every option not at its default, as (snake_case name, text) in ordinal name order (OQ08).</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Options { get; }

        /// <summary>One of the Status constants.</summary>
        public string Status { get; }

        /// <summary><c>adduct_ambiguous</c> (no evidence) or <c>mz_only</c>.</summary>
        public string PreconditionMode { get; }

        /// <summary><c>assumed</c> without evidence, else the evidence tier.</summary>
        public string AdductTier { get; }

        /// <summary><c>QTOF-type</c>, <c>Orbitrap/FT</c>, <c>other</c> or <c>unknown</c>.</summary>
        public string InstrumentClass { get; }

        /// <summary>The instrument type text as given (may be null).</summary>
        public string? InstrumentType { get; }

        /// <summary>The observed precursor m/z as given; JSON writes null when it is not finite.</summary>
        public double PrecursorMz { get; }

        /// <summary>The p2 base intensity (the largest intensity more than 1.0 Da from the precursor), or null when
        /// there is none or the spectrum was not evaluated (OQ07).</summary>
        public double? BaseIntensity { get; }

        public int NPeaksIn { get; }

        /// <summary>Peaks dropped for a non-finite or non-positive m/z or intensity.</summary>
        public int NPeaksDropped { get; }

        /// <summary>The sha256 of the four inputs (design 4.11), peaks in input order.</summary>
        public string InputSha256 { get; }

        /// <summary>The rules that pass the filter and were not refused, whatever the spectrum's ion mode (OQ25).</summary>
        public int RulesEvaluated { get; }

        /// <summary>The ids of the rules outside the filter, in ordinal order.</summary>
        public IReadOnlyList<string> NotMeasured { get; }

        /// <summary>The rules refused at evaluator construction, in ordinal rule_id order.</summary>
        public IReadOnlyList<RuleRefusal> Refusals { get; }

        /// <summary>Ordinal rule_id order; compact results list only outcomes with hypotheses, verbose results every
        /// evaluated rule.</summary>
        public IReadOnlyList<RuleOutcome> Outcomes { get; }

        /// <summary>The motifs of the fired rules, distinct, in ordinal order.</summary>
        public IReadOnlyList<string> FiredMotifs { get; }

        /// <summary>The fired motifs with no fired descendant under motif nesting, in ordinal order (OQ29).</summary>
        public IReadOnlyList<string> NarrowestMotifs { get; }

        /// <summary>Two fired motifs are not nested (neither is an ancestor of the other).</summary>
        public bool RuleConflict { get; }

        /// <summary><see cref="RuleConflict"/> over the claim-eligible fired rules only.</summary>
        public bool EligibleRuleConflict { get; }
    }
}
