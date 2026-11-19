using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    internal enum LogicKind
    {
        Term,
        All,
        Any,
        Not,
        KOf,
    }

    /// <summary>A node of a rule's logic as the engine walks it: the load's compiled node with its RFC 6901 pointer.</summary>
    internal sealed class LogicPlanNode
    {
        public LogicPlanNode(LogicKind kind, string pointer, int termIndex, int slot, int anyIndex, LogicPlanNode[] children) {
            Kind = kind;
            Pointer = pointer;
            TermIndex = termIndex;
            Slot = slot;
            AnyIndex = anyIndex;
            Children = children;
        }

        public LogicKind Kind { get; }

        /// <summary>The pointer of the node object in the rule file (<see cref="LogicNode.Pointer"/>), for example
        /// <c>/logic</c> or <c>/logic/all/1</c>.</summary>
        public string Pointer { get; }

        /// <summary>A term leaf: the term's index in the rule; -1 otherwise.</summary>
        public int TermIndex { get; }

        /// <summary>A k_of node: the index of its k (pre-order over the k_of nodes); -1 otherwise.</summary>
        public int Slot { get; }

        /// <summary>An any node: its index in pre-order over the any nodes; -1 otherwise.</summary>
        public int AnyIndex { get; }

        public LogicPlanNode[] Children { get; }
    }

    /// <summary>The logic of one rule with what its leaves need: each term's role, than-terms and ratio.</summary>
    internal sealed class LogicPlan
    {
        private LogicPlan(LogicPlanNode root, List<LogicPlanNode> anyNodes, LogicPlanNode[] kNodes, TermRole[] roles, int[][] than, double[] ratio) {
            Root = root;
            AnyNodes = new ReadOnlyCollection<LogicPlanNode>(anyNodes);
            KNodes = new ReadOnlyCollection<LogicPlanNode>(kNodes);
            Roles = roles;
            Than = than;
            Ratio = ratio;
        }

        public LogicPlanNode Root { get; }

        /// <summary>The any nodes in pre-order: the order of branch reports.</summary>
        public IReadOnlyList<LogicPlanNode> AnyNodes { get; }

        /// <summary>The k_of nodes by slot (pre-order).</summary>
        public IReadOnlyList<LogicPlanNode> KNodes { get; }

        /// <summary>The role of every term, by term index.</summary>
        public TermRole[] Roles { get; }

        /// <summary>The term indices of every term's <c>than</c> list (empty when it has none).</summary>
        public int[][] Than { get; }

        /// <summary>Every term's ratio, 1.0 when it has none.</summary>
        public double[] Ratio { get; }

        /// <summary>Builds the plan from the load's compiled logic and the public model's pointers; the two trees have
        /// the same shape, children in file order.</summary>
        public static LogicPlan Build(Rule rule, RuleInfo info) {
            var n = info.Terms.Count;
            var roles = new TermRole[n];
            var than = new int[n][];
            var ratio = new double[n];
            for (var i = 0; i < n; i++) {
                var term = rule.Terms[i];
                roles[i] = term.Role;
                ratio[i] = term.Ratio ?? 1.0;
                if (term.Than is null) {
                    than[i] = new int[0];
                }
                else {
                    than[i] = new int[term.Than.Count];
                    for (var j = 0; j < term.Than.Count; j++) {
                        than[i][j] = info.ById[term.Than[j]].Index;
                    }
                }
            }
            var anyNodes = new List<LogicPlanNode>();
            var kNodes = new LogicPlanNode[info.KNodes.Count];
            var root = Compile(info.Logic, rule.Logic, anyNodes, kNodes);
            return new LogicPlan(root, anyNodes, kNodes, roles, than, ratio);
        }

        private static LogicPlanNode Compile(CompiledNode node, LogicNode model, List<LogicPlanNode> anyNodes, LogicPlanNode[] kNodes) {
            LogicKind kind;
            switch (node.Kind) {
                case "term": kind = LogicKind.Term; break;
                case "all": kind = LogicKind.All; break;
                case "any": kind = LogicKind.Any; break;
                case "not": kind = LogicKind.Not; break;
                default: kind = LogicKind.KOf; break;
            }
            var children = new LogicPlanNode[node.Children.Count];
            var anyIndex = -1;
            LogicPlanNode result;
            if (kind == LogicKind.Any) {
                anyIndex = anyNodes.Count;
                anyNodes.Add(null!); // reserved in pre-order, filled below
            }
            for (var i = 0; i < children.Length; i++) {
                children[i] = Compile(node.Children[i], model.Children[i], anyNodes, kNodes);
            }
            result = new LogicPlanNode(kind, model.Pointer, node.TermIndex, kind == LogicKind.KOf ? node.Slot : -1, anyIndex, children);
            if (anyIndex >= 0) {
                anyNodes[anyIndex] = result;
            }
            if (kind == LogicKind.KOf) {
                kNodes[node.Slot] = result;
            }
            return result;
        }
    }

    /// <summary>What the leaves of one hypothesis read under one assignment (design 4.7).</summary>
    internal sealed class LogicInputs
    {
        public LogicInputs(bool[] termAssigned, int[][] assignedUnits, int unitCount, double[] iRaw, bool[] rawPresent, long[] kEff) {
            TermAssigned = termAssigned;
            AssignedUnits = assignedUnits;
            UnitCount = unitCount;
            IRaw = iRaw;
            RawPresent = rawPresent;
            KEff = kEff;
        }

        /// <summary>Per term: at least one of its items has a peak.</summary>
        public bool[] TermAssigned { get; }

        /// <summary>Per term: the evidence units of its items that have a peak.</summary>
        public int[][] AssignedUnits { get; }

        /// <summary>The number of evidence units of the hypothesis: every unit id is below it.</summary>
        public int UnitCount { get; }

        /// <summary>Per term: I_raw, the maximum rel over the hits of all its members, floors ignored (0 when none).</summary>
        public double[] IRaw { get; }

        /// <summary>Per term: raw_present, some hit passes the floor.</summary>
        public bool[] RawPresent { get; }

        /// <summary>Per k slot: k_eff = max(1, k + KOffset) at the hypothesis.</summary>
        public long[] KEff { get; }
    }

    /// <summary>Per-node values of one evaluation, for the branch and k_of reports.</summary>
    internal sealed class LogicRecord
    {
        public LogicRecord(LogicPlan plan) {
            AnyChildren = new bool[plan.AnyNodes.Count][];
            for (var i = 0; i < AnyChildren.Length; i++) {
                AnyChildren[i] = new bool[plan.AnyNodes[i].Children.Length];
            }
            KCount = new long[plan.KNodes.Count];
            KSatisfied = new bool[plan.KNodes.Count];
        }

        /// <summary>Per any node (pre-order): the truth of each child.</summary>
        public bool[][] AnyChildren { get; }

        /// <summary>Per k slot: the count.</summary>
        public long[] KCount { get; }

        /// <summary>Per k slot: count &gt;= k_eff.</summary>
        public bool[] KSatisfied { get; }
    }

    /// <summary>
    /// Evaluates a rule's logic (design 4.7): <c>all</c>, <c>any</c>, <c>not</c>, <c>k_of</c> and <c>term</c> nodes,
    /// interpreted, never compiled to code. Every child of <c>all</c> and <c>any</c> is evaluated (no
    /// short-circuiting), so every branch can be reported. Leaf values by role:
    /// <list type="bullet">
    /// <item><c>required</c>: the term has at least one assigned member;</item>
    /// <item><c>counted</c>: contributes to the parent k_of count (as a leaf elsewhere it would read as required, but
    /// the load only places it under k_of);</item>
    /// <item><c>ordering</c>: A assigned and <c>I_raw(A) &gt; ratio * max_{B in than} I_raw(B)</c>, ratio 1.0 by
    /// default;</item>
    /// <item><c>veto_present</c>: <c>raw_present(A)</c>; <c>veto_stronger_than</c>: <c>raw_present(A)</c> and
    /// <c>I_raw(A) &gt; ratio * max_{B in than} I_raw(B)</c> (both only inside not);</item>
    /// <item><c>upgrade</c> is never in the logic.</item>
    /// </list>
    /// A k_of counts the distinct evidence units among its term children with an assigned peak (a complementary group
    /// once), plus one per true subtree child, and holds iff that count is at least k_eff = max(1, k + KOffset).
    /// </summary>
    internal static class LogicEvaluator
    {
        /// <summary>The value of the logic.</summary>
        /// <param name="plan">The rule's logic.</param>
        /// <param name="inputs">The leaves' inputs.</param>
        /// <param name="ignoreVetoes">Every not node is true (fired_ignoring_vetoes).</param>
        /// <param name="restrictAny">An any node (pre-order index) whose value is that of one child only, or -1.</param>
        /// <param name="restrictChild">The child of <paramref name="restrictAny"/> that stands for the node.</param>
        /// <param name="record">Receives the per-node values, or null.</param>
        public static bool Evaluate(LogicPlan plan, LogicInputs inputs, bool ignoreVetoes, int restrictAny, int restrictChild, LogicRecord? record) =>
            Eval(plan.Root, plan, inputs, ignoreVetoes, restrictAny, restrictChild, record);

        /// <summary>The value of a term leaf.</summary>
        public static bool TermValue(LogicPlan plan, LogicInputs inputs, int term) {
            switch (plan.Roles[term]) {
                case TermRole.Required:
                case TermRole.Counted:
                    return inputs.TermAssigned[term];
                case TermRole.Ordering:
                    return inputs.TermAssigned[term] && inputs.IRaw[term] > plan.Ratio[term] * MaxThan(plan, inputs, term);
                case TermRole.VetoPresent:
                    return inputs.RawPresent[term];
                case TermRole.VetoStrongerThan:
                    return inputs.RawPresent[term] && inputs.IRaw[term] > plan.Ratio[term] * MaxThan(plan, inputs, term);
                default:
                    return false;
            }
        }

        /// <summary>max over the term's than-terms of I_raw; 0 when it has none.</summary>
        public static double MaxThan(LogicPlan plan, LogicInputs inputs, int term) {
            var max = 0.0;
            foreach (var b in plan.Than[term]) {
                if (inputs.IRaw[b] > max) {
                    max = inputs.IRaw[b];
                }
            }
            return max;
        }

        private static bool Eval(LogicPlanNode node, LogicPlan plan, LogicInputs inputs, bool ignoreVetoes, int restrictAny, int restrictChild, LogicRecord? record) {
            switch (node.Kind) {
                case LogicKind.Term:
                    return TermValue(plan, inputs, node.TermIndex);
                case LogicKind.All: {
                    var result = true;
                    foreach (var ch in node.Children) {
                        result &= Eval(ch, plan, inputs, ignoreVetoes, restrictAny, restrictChild, record);
                    }
                    return result;
                }
                case LogicKind.Any: {
                    var result = false;
                    for (var i = 0; i < node.Children.Length; i++) {
                        var v = Eval(node.Children[i], plan, inputs, ignoreVetoes, restrictAny, restrictChild, record);
                        if (record != null) {
                            record.AnyChildren[node.AnyIndex][i] = v;
                        }
                        if (node.AnyIndex == restrictAny) {
                            if (i == restrictChild) {
                                result = v;
                            }
                        }
                        else {
                            result |= v;
                        }
                    }
                    return result;
                }
                case LogicKind.Not: {
                    var inner = Eval(node.Children[0], plan, inputs, ignoreVetoes, restrictAny, restrictChild, record);
                    return ignoreVetoes || !inner;
                }
                default: {
                    // Distinct units by a mark per unit: a list search would make a leaf quadratic in the members.
                    var seen = new bool[inputs.UnitCount];
                    long distinct = 0;
                    long extra = 0;
                    foreach (var ch in node.Children) {
                        if (ch.Kind == LogicKind.Term) {
                            foreach (var u in inputs.AssignedUnits[ch.TermIndex]) {
                                if (!seen[u]) {
                                    seen[u] = true;
                                    distinct++;
                                }
                            }
                        }
                        else if (Eval(ch, plan, inputs, ignoreVetoes, restrictAny, restrictChild, record)) {
                            extra++;
                        }
                    }
                    var count = distinct + extra;
                    var satisfied = count >= inputs.KEff[node.Slot];
                    if (record != null) {
                        record.KCount[node.Slot] = count;
                        record.KSatisfied[node.Slot] = satisfied;
                    }
                    return satisfied;
                }
            }
        }
    }
}
