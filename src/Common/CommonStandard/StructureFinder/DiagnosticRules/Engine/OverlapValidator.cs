using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>
    /// The overlap refusal (design 4.8), run once per rule at evaluator construction at the effective tolerances. For
    /// every composition x tried adduct, the support items (members of required, counted and ordering terms; never
    /// vetoes or upgrades) are sorted by target and neighbours are swept. Items a and b overlap when
    /// <c>|t_a - t_b| &lt;= tol(t_a) + tol(t_b) + s</c>, with <c>s = tol_prec + 1e-10</c> when exactly one of them is
    /// a loss referenced to the observed precursor (its target moves with P_obs) and 0 otherwise. An overlap is allowed
    /// only between different terms of one complementary group. Targets are the hypothesis-referenced ones, with the
    /// decoy shift. The test is the design's expression, without the 1e-10 slack of the inclusive p2 hit, so two
    /// targets within 2e-10 Da beyond tol(t_a) + tol(t_b) pass here yet could share a peak; the exclusive assignment
    /// (design 4.6) resolves that sliver at evaluation time.
    /// </summary>
    internal static class OverlapValidator
    {
        private sealed class Item
        {
            public Item(int index, double target, double tolerance, bool observedLoss, int termIndex, int groupId, string label) {
                Index = index;
                Target = target;
                Tolerance = tolerance;
                ObservedLoss = observedLoss;
                TermIndex = termIndex;
                GroupId = groupId;
                Label = label;
            }

            public int Index { get; }
            public double Target { get; }
            public double Tolerance { get; }
            public bool ObservedLoss { get; }
            public int TermIndex { get; }
            public int GroupId { get; }
            public string Label { get; }
        }

        /// <summary>
        /// The refusals of <paramref name="engines"/> (in their order) under <paramref name="settings"/>:
        /// <see cref="OverlapMode.Refuse"/> checks every rule, <see cref="OverlapMode.Runtime"/> refuses nothing (the
        /// exclusive assignment resolves the conflicts). With Strict, any refusal throws
        /// <see cref="DiagnosticRuleRefusedException"/>.
        /// </summary>
        public static IReadOnlyList<RuleRefusal> Validate(IEnumerable<RuleEngine> engines, EvaluationSettings settings) {
            var refusals = new List<RuleRefusal>();
            if (settings.Overlap == OverlapMode.Refuse) {
                foreach (var engine in engines) {
                    var r = Check(engine);
                    if (r != null) {
                        refusals.Add(r);
                    }
                }
            }
            var result = new ReadOnlyCollection<RuleRefusal>(refusals);
            if (settings.Strict && refusals.Count > 0) {
                throw new DiagnosticRuleRefusedException(result);
            }
            return result;
        }

        /// <summary>The refusal of one rule, or null. The first hypothesis in ordinal hypothesis_id order that has an
        /// undeclared overlap is reported, with its first such pair in sweep order.</summary>
        public static RuleRefusal? Check(RuleEngine engine) {
            var index = engine.Index;
            var hypotheses = new List<KeyValuePair<string, int[]>>();
            for (var c = 0; c < index.Compositions.Count; c++) {
                var compositionId = index.CompositionId(c);
                for (var a = 0; a < index.Tried.Count; a++) {
                    hypotheses.Add(new KeyValuePair<string, int[]>(compositionId + "|" + index.Tried[a].Adduct.Adduct, new[] { c, a }));
                }
            }
            hypotheses.Sort((x, y) => string.CompareOrdinal(x.Key, y.Key));
            var groupOfTerm = EvidenceItems.GroupOfTerm(engine.Info);
            var observed = engine.LossReference == LossReference.Observed;
            var sObserved = engine.PrecursorTolerance + SpectrumView.Slack;
            var items = new List<Item>();
            foreach (var h in hypotheses) {
                var series = index.SeriesHypothesis(h.Value[0], h.Value[1]);
                var targets = engine.Builder.Build(series, engine.DecoyPda, LossReference.Hypothesis, double.NaN);
                items.Clear();
                foreach (var term in targets) {
                    if (!term.Term.IsSupport) {
                        continue;
                    }
                    foreach (var m in term.Members) {
                        items.Add(new Item(items.Count, m.TargetMz, engine.Tolerance.At(m.TargetMz), observed && m.IsLoss, term.Term.Index,
                                           groupOfTerm[term.Term.Index], term.Term.Tid + ":" + m.Key));
                    }
                }
                var found = Sweep(items, sObserved);
                if (found != null) {
                    return new RuleRefusal(engine.Rule.RuleId, RuleRefusal.OverlapUndeclared, h.Key,
                                           new ReadOnlyCollection<string>(new[] { found.Value.Key.Label, found.Value.Value.Label }),
                                           found.Value.Value.Target - found.Value.Key.Target);
                }
            }
            return null;
        }

        private static KeyValuePair<Item, Item>? Sweep(List<Item> items, double sObserved) {
            if (items.Count < 2) {
                return null;
            }
            var sorted = items.ToArray();
            Array.Sort(sorted, (x, y) => {
                var c = x.Target.CompareTo(y.Target);
                return c != 0 ? c : x.Index.CompareTo(y.Index);
            });
            // tol(t) = max(abs, t * ppm / 1e6) is monotone in t, and so are the rounded differences and sums, so once
            // t_j - t_i exceeds tol_i + tol(t_max) + s_max no later j can overlap item i.
            var tolMax = sorted[sorted.Length - 1].Tolerance;
            for (var i = 0; i < sorted.Length; i++) {
                var a = sorted[i];
                var bound = a.Tolerance + tolMax + sObserved;
                for (var j = i + 1; j < sorted.Length; j++) {
                    var b = sorted[j];
                    var delta = b.Target - a.Target;
                    if (delta > bound) {
                        break;
                    }
                    var s = a.ObservedLoss != b.ObservedLoss ? sObserved : 0.0;
                    if (delta <= a.Tolerance + b.Tolerance + s && !MayOverlap(a, b)) {
                        return new KeyValuePair<Item, Item>(a, b);
                    }
                }
            }
            return null;
        }

        private static bool MayOverlap(Item a, Item b) => a.TermIndex != b.TermIndex && a.GroupId >= 0 && a.GroupId == b.GroupId;
    }
}
