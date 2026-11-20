using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>
    /// The transitive closure of tables/motif_nesting.json (design 4.9, OQ29): two motifs are nested when they are
    /// equal or one is an ancestor of the other. <c>rule_conflict</c> is true when two fired motifs are not nested, and
    /// <c>narrowest_motifs</c> are the fired motifs with no fired descendant. The sets are used for lookups only; every
    /// output order comes from the caller's ordinal-sorted motif list. Built once, then immutable.
    /// </summary>
    internal sealed class MotifNestingClosure
    {
        private readonly Dictionary<string, HashSet<string>> _ancestors = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        public MotifNestingClosure(IEnumerable<MotifNesting> rows) {
            var parents = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var row in rows) {
                if (!parents.TryGetValue(row.Child, out var list)) {
                    parents.Add(row.Child, list = new List<string>());
                }
                list.Add(row.Parent);
            }
            foreach (var child in parents.Keys) {
                // A depth-first walk up the parent links; the visited set also guards against a cycle.
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var stack = new Stack<string>(parents[child]);
                while (stack.Count > 0) {
                    var p = stack.Pop();
                    if (!seen.Add(p)) {
                        continue;
                    }
                    if (parents.TryGetValue(p, out var up)) {
                        foreach (var q in up) {
                            stack.Push(q);
                        }
                    }
                }
                seen.Remove(child);
                _ancestors.Add(child, seen);
            }
        }

        /// <summary>Whether <paramref name="ancestor"/> is a proper ancestor of <paramref name="motif"/>.</summary>
        public bool IsAncestor(string ancestor, string motif) =>
            _ancestors.TryGetValue(motif, out var set) && set.Contains(ancestor);

        /// <summary>Equal, or one is an ancestor of the other.</summary>
        public bool Nested(string a, string b) => string.Equals(a, b, StringComparison.Ordinal) || IsAncestor(a, b) || IsAncestor(b, a);

        /// <summary>Some two of <paramref name="motifs"/> are not nested.</summary>
        public bool Conflict(IReadOnlyList<string> motifs) {
            for (var i = 0; i < motifs.Count; i++) {
                for (var j = i + 1; j < motifs.Count; j++) {
                    if (!Nested(motifs[i], motifs[j])) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>The motifs of <paramref name="motifs"/> that are no other one's ancestor, in the given order.</summary>
        public List<string> Narrowest(IReadOnlyList<string> motifs) {
            var output = new List<string>();
            foreach (var m in motifs) {
                var hasDescendant = false;
                foreach (var other in motifs) {
                    if (IsAncestor(m, other)) {
                        hasDescendant = true;
                        break;
                    }
                }
                if (!hasDescendant) {
                    output.Add(m);
                }
            }
            return output;
        }
    }
}
