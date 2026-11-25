using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Results
{
    /// <summary>
    /// A rule the evaluator refused at construction and does not evaluate (design 4.8). The only code is
    /// <c>overlap_undeclared</c>: two support items of one hypothesis can match the same peak at the effective
    /// tolerances without being different terms of one complementary group.
    /// </summary>
    public sealed class RuleRefusal
    {
        /// <summary>The code of an undeclared overlap.</summary>
        public const string OverlapUndeclared = "overlap_undeclared";

        public RuleRefusal(string ruleId, string code, string hypothesisId, IReadOnlyList<string> items, double deltaDa) {
            RuleId = ruleId;
            Code = code;
            HypothesisId = hypothesisId;
            Items = items;
            DeltaDa = deltaDa;
        }

        public string RuleId { get; }

        public string Code { get; }

        /// <summary>The first hypothesis (ordinal hypothesis_id order) in which the overlap occurs.</summary>
        public string HypothesisId { get; }

        /// <summary>The two overlapping items as <c>term_id:member_key</c>, the lower target first.</summary>
        public IReadOnlyList<string> Items { get; }

        /// <summary>The distance between the two targets in Da (not negative).</summary>
        public double DeltaDa { get; }
    }
}
