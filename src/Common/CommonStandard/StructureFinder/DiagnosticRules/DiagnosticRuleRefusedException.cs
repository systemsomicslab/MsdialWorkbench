using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using System;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules
{
    /// <summary>
    /// Thrown when an evaluator is built with <see cref="DiagnosticEvaluationOptions.Strict"/> and refuses at least
    /// one rule (design 4.8). Without Strict the refusals are reported and the other rules are evaluated.
    /// </summary>
    public sealed class DiagnosticRuleRefusedException : Exception
    {
        public DiagnosticRuleRefusedException(IReadOnlyList<RuleRefusal> refusals)
            : base(Describe(refusals)) {
            Refusals = refusals;
        }

        /// <summary>Every refusal, in ordinal rule_id order.</summary>
        public IReadOnlyList<RuleRefusal> Refusals { get; }

        private static string Describe(IReadOnlyList<RuleRefusal> refusals) {
            var lines = new List<string> { "The diagnostic rule evaluator refused rules:" };
            for (var i = 0; i < refusals.Count && i < 20; i++) {
                var r = refusals[i];
                lines.Add(r.RuleId + " " + r.Code + " " + r.HypothesisId + " " + string.Join(" ", r.Items));
            }
            if (refusals.Count > 20) {
                lines.Add("...");
            }
            return string.Join("\n", lines);
        }
    }
}
