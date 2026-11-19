using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Results
{
    /// <summary>
    /// An any node (design 4.7): its RFC 6901 pointer, for example <c>/logic/all/1</c>, and one boolean per child. In
    /// <see cref="HypothesisOutcome.Branches"/> each boolean is the child's truth under the chosen assignment; in
    /// <see cref="RuleOutcome.BranchFired"/> it says whether the rule fires with the node restricted to that child.
    /// For an any node inside a <c>not</c> (alternative vetoes), the restriction keeps that one veto and drops the
    /// others, so a true child there means "this veto alone does not block": it can be true while the rule is
    /// veto-blocked by another child. Outside a not, a true child implies the rule fires.
    /// </summary>
    public sealed class BranchOutcome
    {
        internal BranchOutcome(string pointer, IReadOnlyList<bool> children) {
            Pointer = pointer;
            Children = children;
        }

        public string Pointer { get; }

        public IReadOnlyList<bool> Children { get; }
    }
}
