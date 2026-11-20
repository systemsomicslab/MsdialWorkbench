using System.Collections.Generic;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Which diagnostic-ion rule outcomes can move a candidate's rank.
    /// </summary>
    public enum DiagnosticRuleRankingScope
    {
        /// <summary>Only claim-eligible outcomes (rules an author has reviewed). The default.</summary>
        Reviewed,
        /// <summary>Every evaluated outcome; for benchmarking, settable in the settings file only.</summary>
        Measured,
    }

    /// <summary>
    /// MS-FINDER settings of the experimental class-conditional diagnostic-ion rules. Every setting at its default
    /// means the feature is off and MS-FINDER runs its legacy code, with legacy outputs.
    /// </summary>
    public sealed class MsfinderDiagnosticRuleSettings
    {
        /// <summary>Evaluate the rules, show and export the evidence. Default false.</summary>
        public bool Evaluation { get; set; }

        /// <summary>
        /// The single named weight w of the ranking term, on the TotalScore scale; finite, at least 0, at most six
        /// decimals. Default 0. It has an effect only when <see cref="Evaluation"/> is on.
        /// </summary>
        public double RankingWeight { get; set; }

        /// <summary>Which outcomes can move a rank. Default <see cref="DiagnosticRuleRankingScope.Reviewed"/>.</summary>
        public DiagnosticRuleRankingScope RankingScope { get; set; } = DiagnosticRuleRankingScope.Reviewed;

        /// <summary>
        /// A rule-set directory for development or benchmark use. Empty (the default) means the embedded rule set.
        /// The path itself is never written to an output.
        /// </summary>
        public string RuleSetDirectory { get; set; } = string.Empty;

        /// <summary>The instrument type used when a query has no INSTRUMENTTYPE. Default empty.</summary>
        public string InstrumentType { get; set; } = string.Empty;

        /// <summary>Abort on a rule refusal or a per-spectrum exception. Default false.</summary>
        public bool Strict { get; set; }

        /// <summary>
        /// Settings-file lines that named an unknown diagnostic-rule key or held a bad value. The setting concerned
        /// stays at its default; when evaluation is on, the runtime refuses to start. Not part of
        /// <see cref="IsDefault"/> and never written.
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>True when every setting is at its default; <see cref="Errors"/> does not count.</summary>
        public bool IsDefault =>
            !Evaluation && RankingWeight == 0.0 && RankingScope == DiagnosticRuleRankingScope.Reviewed && RuleSetDirectory == ""
            && InstrumentType == "" && !Strict;

        /// <summary>A copy of the six settings and of the errors.</summary>
        public MsfinderDiagnosticRuleSettings Clone() {
            var clone = new MsfinderDiagnosticRuleSettings {
                Evaluation = Evaluation,
                RankingWeight = RankingWeight,
                RankingScope = RankingScope,
                RuleSetDirectory = RuleSetDirectory,
                InstrumentType = InstrumentType,
                Strict = Strict,
            };
            clone.Errors.AddRange(Errors);
            return clone;
        }
    }
}
