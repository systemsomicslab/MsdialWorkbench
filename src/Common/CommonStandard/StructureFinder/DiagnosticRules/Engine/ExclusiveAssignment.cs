using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>One item (design 4.6): a member of a support term in positive context (required, counted, or the A of
    /// ordering), with its candidates.</summary>
    internal sealed class EvidenceItem
    {
        public EvidenceItem(int index, int termIndex, int memberIndex, int unitId, int groupId, IReadOnlyList<PeakHit> candidates) {
            Index = index;
            TermIndex = termIndex;
            MemberIndex = memberIndex;
            UnitId = unitId;
            GroupId = groupId;
            Candidates = candidates;
        }

        /// <summary>The position in canonical item order: terms in file order, then member order.</summary>
        public int Index { get; }
        public int TermIndex { get; }
        /// <summary>The member's index in <see cref="TermTargets.Members"/>.</summary>
        public int MemberIndex { get; }
        /// <summary>The evidence unit, numbered in canonical order of first appearance (<c>MatchedTerm.unit_id</c>,
        /// OQ12).</summary>
        public int UnitId { get; }
        /// <summary>The complementary group of the term, or -1 when it is in none.</summary>
        public int GroupId { get; }
        /// <summary>Hits that are present and outside the precursor region, by rel descending, |err| ascending, then
        /// input index ascending.</summary>
        public IReadOnlyList<PeakHit> Candidates { get; }
    }

    /// <summary>The items and evidence units of one hypothesis (design 4.6).</summary>
    internal sealed class EvidenceItems
    {
        private EvidenceItems(IReadOnlyList<EvidenceItem> items, int unitCount, int termCount) {
            Items = items;
            UnitCount = unitCount;
            TermCount = termCount;
        }

        /// <summary>The items in canonical order.</summary>
        public IReadOnlyList<EvidenceItem> Items { get; }

        /// <summary>The number of evidence units: a complementary group (the transitive closure of the pairs), or one
        /// member of a term outside every group.</summary>
        public int UnitCount { get; }

        /// <summary>The number of terms of the rule.</summary>
        public int TermCount { get; }

        /// <summary>Two items may share a peak only if they belong to different terms of one complementary group;
        /// members of one term never share, so k counts distinct observed masses.</summary>
        public static bool MayShare(EvidenceItem a, EvidenceItem b) =>
            a.TermIndex != b.TermIndex && a.GroupId >= 0 && a.GroupId == b.GroupId;

        /// <summary>The term index -> complementary group index map of a rule (-1 outside every group).</summary>
        public static int[] GroupOfTerm(RuleInfo rule) {
            var groupOf = new int[rule.Terms.Count];
            for (var i = 0; i < groupOf.Length; i++) {
                groupOf[i] = -1;
            }
            for (var g = 0; g < rule.Groups.Count; g++) {
                foreach (var t in rule.Groups[g]) {
                    groupOf[t] = g;
                }
            }
            return groupOf;
        }

        /// <summary>The items of every support term, in canonical order, with their units and candidates.</summary>
        /// <param name="rule">The rule.</param>
        /// <param name="groupOfTerm">From <see cref="GroupOfTerm(RuleInfo)"/>.</param>
        /// <param name="memberHits">Per term, per member: its hits (<see cref="SpectrumView.Hits"/>).</param>
        public static EvidenceItems Build(RuleInfo rule, int[] groupOfTerm, IReadOnlyList<PeakHit>[][] memberHits) {
            var items = new List<EvidenceItem>();
            var groupUnit = new int[rule.Groups.Count];
            for (var g = 0; g < groupUnit.Length; g++) {
                groupUnit[g] = -1;
            }
            var units = 0;
            foreach (var t in rule.Terms) {
                if (!t.IsSupport) {
                    continue;
                }
                var g = groupOfTerm[t.Index];
                for (var m = 0; m < memberHits[t.Index].Length; m++) {
                    int unit;
                    if (g < 0) {
                        unit = units++;
                    }
                    else {
                        if (groupUnit[g] < 0) {
                            groupUnit[g] = units++;
                        }
                        unit = groupUnit[g];
                    }
                    var candidates = SpectrumView.Candidates(memberHits[t.Index][m]);
                    items.Add(new EvidenceItem(items.Count, t.Index, m, unit, g, new ReadOnlyCollection<PeakHit>(candidates)));
                }
            }
            return new EvidenceItems(new ReadOnlyCollection<EvidenceItem>(items), units, rule.Terms.Count);
        }
    }

    /// <summary>The assignment chosen for one hypothesis.</summary>
    internal sealed class Assignment
    {
        public Assignment(int[]? choice, bool overflow, bool fastPath, int leaves) {
            Choice = choice;
            Overflow = overflow;
            FastPath = fastPath;
            Leaves = leaves;
        }

        /// <summary>Per item: the index of its peak in <see cref="EvidenceItem.Candidates"/>, or -1 for none. Null on
        /// overflow.</summary>
        public int[]? Choice { get; }

        /// <summary>More than AssignmentCap leaves: the hypothesis does not fire (<c>assignment_overflow</c>).</summary>
        public bool Overflow { get; }

        /// <summary>No peak was a candidate of two items that may not share.</summary>
        public bool FastPath { get; }

        /// <summary>The leaves the search visited (0 on the fast path).</summary>
        public int Leaves { get; }
    }

    /// <summary>
    /// One peak, one term (design 4.6). Fast path: if no peak is a candidate of two items that may not share, every
    /// item takes its first candidate, which is exactly optimal (the items are independent and the first candidate has
    /// the highest rel). Otherwise the unconflicted items take their first candidate, and a depth-first search runs
    /// over the conflicted items in canonical order: each tries its candidates in order, then none, skipping a peak
    /// already taken by an item it may not share with. Each leaf is scored by (logic true, logic with every not node
    /// true, score, number of assigned items), compared lexicographically; only a strictly better leaf replaces the
    /// best, so the first leaf wins ties. More than the cap of leaves stops the search with an overflow: a
    /// deterministic cap, not a time-out.
    /// <para>
    /// The second key refines design 4.6 (it replaces OQ15). <c>not</c> holds only vetoes, so the logic is monotone in
    /// its not nodes: wherever the logic is true, the veto-free logic is true too, and the key is only evaluated, and
    /// only separates leaves, when the logic is false. A hypothesis that fires therefore gets exactly the 4.6
    /// assignment. One that does not fire gets an assignment that satisfies the veto-free logic whenever any does, so
    /// <c>fired_ignoring_vetoes</c> (design 4.7) is, like <c>fired</c>, true iff some assignment makes it true, and a
    /// vetoed hypothesis is reported as <c>veto</c>, never as <c>logic_false</c> by a tie between all-false leaves.
    /// </para>
    /// </summary>
    internal static class ExclusiveAssignment
    {
        /// <summary>Chooses the assignment.</summary>
        /// <param name="items">The hypothesis' items.</param>
        /// <param name="logic">The rule's logic under a choice (the objective's first key).</param>
        /// <param name="logicIgnoringVetoes">The same logic with every not node true (the second key, read only where
        /// <paramref name="logic"/> is false).</param>
        /// <param name="cap">AssignmentCap: the most leaves the search may visit.</param>
        public static Assignment Assign(EvidenceItems items, Func<int[], bool> logic, Func<int[], bool> logicIgnoringVetoes, int cap) {
            var n = items.Items.Count;
            var choice = new int[n];
            var conflicted = new bool[n];
            var byPeak = new Dictionary<int, List<int>>();
            for (var i = 0; i < n; i++) {
                choice[i] = items.Items[i].Candidates.Count > 0 ? 0 : -1;
                foreach (var c in items.Items[i].Candidates) {
                    if (!byPeak.TryGetValue(c.ViewIndex, out var list)) {
                        byPeak[c.ViewIndex] = list = new List<int>();
                    }
                    list.Add(i);
                }
            }
            var any = false;
            foreach (var list in byPeak.Values) {
                // Only whether an item is conflicted is recorded, so the dictionary's order does not matter.
                for (var a = 0; a < list.Count; a++) {
                    for (var b = a + 1; b < list.Count; b++) {
                        if (!EvidenceItems.MayShare(items.Items[list[a]], items.Items[list[b]])) {
                            conflicted[list[a]] = true;
                            conflicted[list[b]] = true;
                            any = true;
                        }
                    }
                }
            }
            if (!any) {
                return new Assignment(choice, false, true, 0);
            }
            var order = new List<int>();
            for (var i = 0; i < n; i++) {
                if (conflicted[i]) {
                    order.Add(i);
                    choice[i] = -1;
                }
            }
            var search = new Search(items, logic, logicIgnoringVetoes, cap, choice, order);
            search.Run(0);
            if (search.Overflow) {
                return new Assignment(null, true, false, search.Leaves);
            }
            return new Assignment(search.Best!, false, false, search.Leaves);
        }

        /// <summary>Score = the sum over evidence units, in canonical order, of the unit's rel: a group contributes the
        /// maximum rel among its assigned peaks. Exact doubles in a fixed summation order.</summary>
        public static double Score(EvidenceItems items, int[] choice) {
            var unitRel = new double[items.UnitCount];
            var unitHas = new bool[items.UnitCount];
            for (var i = 0; i < choice.Length; i++) {
                if (choice[i] < 0) {
                    continue;
                }
                var item = items.Items[i];
                var rel = item.Candidates[choice[i]].Rel;
                if (!unitHas[item.UnitId] || rel > unitRel[item.UnitId]) {
                    unitRel[item.UnitId] = rel;
                    unitHas[item.UnitId] = true;
                }
            }
            var score = 0.0;
            for (var u = 0; u < unitRel.Length; u++) {
                if (unitHas[u]) {
                    score += unitRel[u];
                }
            }
            return score;
        }

        /// <summary>The number of items with a peak.</summary>
        public static int AssignedCount(int[] choice) {
            var count = 0;
            foreach (var c in choice) {
                if (c >= 0) {
                    count++;
                }
            }
            return count;
        }

        private sealed class Search
        {
            private readonly EvidenceItems _items;
            private readonly Func<int[], bool> _logic;
            private readonly Func<int[], bool> _logicIgnoringVetoes;
            private readonly int _cap;
            private readonly int[] _choice;
            private readonly List<int> _order;
            /// <summary>Peak view index -> the conflicted items holding it on the current path.</summary>
            private readonly Dictionary<int, List<int>> _held = new Dictionary<int, List<int>>();
            private bool _bestLogic;
            private bool _bestIgnoringVetoes;
            private double _bestScore;
            private int _bestAssigned;

            public Search(EvidenceItems items, Func<int[], bool> logic, Func<int[], bool> logicIgnoringVetoes, int cap, int[] choice, List<int> order) {
                _items = items;
                _logic = logic;
                _logicIgnoringVetoes = logicIgnoringVetoes;
                _cap = cap;
                _choice = choice;
                _order = order;
            }

            public int[]? Best { get; private set; }
            public int Leaves { get; private set; }
            public bool Overflow { get; private set; }

            public void Run(int depth) {
                if (Overflow) {
                    return;
                }
                if (depth == _order.Count) {
                    Leaf();
                    return;
                }
                var i = _order[depth];
                var item = _items.Items[i];
                for (var c = 0; c < item.Candidates.Count && !Overflow; c++) {
                    var peak = item.Candidates[c].ViewIndex;
                    if (!Allowed(item, peak)) {
                        continue;
                    }
                    if (!_held.TryGetValue(peak, out var holders)) {
                        _held[peak] = holders = new List<int>();
                    }
                    holders.Add(i);
                    _choice[i] = c;
                    Run(depth + 1);
                    holders.RemoveAt(holders.Count - 1);
                    _choice[i] = -1;
                }
                if (!Overflow) {
                    Run(depth + 1);
                }
            }

            private bool Allowed(EvidenceItem item, int peak) {
                if (!_held.TryGetValue(peak, out var holders)) {
                    return true;
                }
                foreach (var h in holders) {
                    if (!EvidenceItems.MayShare(item, _items.Items[h])) {
                        return false;
                    }
                }
                return true;
            }

            private void Leaf() {
                Leaves++;
                if (Leaves > _cap) {
                    Overflow = true;
                    return;
                }
                var logic = _logic(_choice);
                // Monotone in the not nodes: true wherever the logic is, so it is only computed where it is not.
                var ignoringVetoes = logic || _logicIgnoringVetoes(_choice);
                var score = Score(_items, _choice);
                var assigned = AssignedCount(_choice);
                var better = Best is null
                    || (logic && !_bestLogic)
                    || (logic == _bestLogic && ignoringVetoes && !_bestIgnoringVetoes)
                    || (logic == _bestLogic && ignoringVetoes == _bestIgnoringVetoes
                        && (score > _bestScore || (score == _bestScore && assigned > _bestAssigned)));
                if (better) {
                    Best = (int[])_choice.Clone();
                    _bestLogic = logic;
                    _bestIgnoringVetoes = ignoringVetoes;
                    _bestScore = score;
                    _bestAssigned = assigned;
                }
            }
        }
    }
}
