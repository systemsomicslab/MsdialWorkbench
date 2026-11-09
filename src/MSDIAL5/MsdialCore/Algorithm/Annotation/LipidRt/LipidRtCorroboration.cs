using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>The two units after cross-polarity corroboration, and the RT line that mapped them.</summary>
    public sealed class LipidRtCrossPolarityResult
    {
        internal LipidRtCrossPolarityResult(LipidRtRunResult positive, LipidRtRunResult negative, LipidRtCrossPolarityLine line) {
            Positive = positive;
            Negative = negative;
            Line = line;
        }

        public LipidRtRunResult Positive { get; }
        public LipidRtRunResult Negative { get; }
        public LipidRtCrossPolarityLine Line { get; }
    }

    /// <summary>
    /// The corroboration post-step (SPEC step 11): the second adduct, the other polarity, and the policies that depend
    /// on them. A pure function of the units' single-run results; it reads no files and never chains (a promotion is
    /// never the basis of another promotion).
    /// </summary>
    /// <remarks>
    /// (a) A Confirmed(Rt) member whose feature holds at least two distinct adducts is adduct-corroborated.
    /// (b) With one positive and one negative unit of the same sample set and LC: RtNeg = a + b RtPos is fitted by
    /// Theil-Sen over compositions with a single RT cluster in both units; the twins of a candidate are the eligible
    /// candidates of the other unit with the same composition within the tolerance of the mapped RT. A Confirmed(Rt)
    /// with a Confirmed(Rt) twin is polarity-corroborated (score + 1); a NotEvaluable for a promotable reason (Support,
    /// ClassGate, Ambiguous) with a Confirmed(Rt) twin becomes Confirmed(PromotedByPolarity) (score + 0.5). Inconsistent,
    /// tie, artefact, magnitude, margin, deficit, policy, plausibility and gate outcomes are never promoted. A
    /// Confirmed(Rt) with no Confirmed twin and an Inconsistent twin records a polarity conflict.
    /// (c) ClassLevelPolicy Corroborated: a class-level Confirmed without (a) or (b) becomes NotEvaluable(PolicyClass).
    /// The Strict operating point: a Confirmed(Rt) without (a) or (b) becomes NotEvaluable(Uncorroborated).
    /// </remarks>
    public static class LipidRtCorroboration
    {
        /// <summary>
        /// Applies the corroboration step to the given units. Cross polarity runs when the parameter allows it and the
        /// units are exactly one positive and one negative unit; otherwise each unit gets (a) and (c) only.
        /// </summary>
        /// <returns>New results, in the order of <paramref name="units"/>.</returns>
        public static IReadOnlyList<LipidRtRunResult> Apply(IReadOnlyList<LipidRtRunResult> units, LipidRtCheckParameter parameter) {
            return Apply(units, parameter, out _);
        }

        /// <summary>
        /// Cross-polarity corroboration of a positive and a negative unit of the same sample set and LC, from their
        /// single-run results. The returned units replace the inputs; the inputs are not changed.
        /// </summary>
        public static LipidRtCrossPolarityResult CrossPolarity(LipidRtRunResult positive, LipidRtRunResult negative, LipidRtCheckParameter parameter) {
            if (positive is null) throw new ArgumentNullException(nameof(positive));
            if (negative is null) throw new ArgumentNullException(nameof(negative));
            if (positive.Polarity != IonMode.Positive || negative.Polarity != IonMode.Negative) {
                throw new ArgumentException("Cross polarity needs one positive and one negative unit.");
            }
            var results = Apply(new[] { positive, negative }, parameter, out var line);
            return new LipidRtCrossPolarityResult(results[0], results[1], line);
        }

        private static IReadOnlyList<LipidRtRunResult> Apply(IReadOnlyList<LipidRtRunResult> units, LipidRtCheckParameter parameter, out LipidRtCrossPolarityLine line) {
            if (units is null) throw new ArgumentNullException(nameof(units));
            if (parameter is null) throw new ArgumentNullException(nameof(parameter));
            line = null;
            var copies = units.Select(u => u.Results.Select(r => r.CopyForCorroboration()).ToList()).ToList();

            // (a) adduct corroboration
            foreach (var results in copies) {
                foreach (var r in results) {
                    if (r.SingleRunReason == LipidRtReason.ConfirmedRt) {
                        r.CorroboratedByAdduct = r.Evidence.AdductCount >= 2;
                    }
                }
            }

            // (b) cross polarity
            var positive = Enumerable.Range(0, units.Count).Where(i => units[i].Polarity == IonMode.Positive).ToList();
            var negative = Enumerable.Range(0, units.Count).Where(i => units[i].Polarity == IonMode.Negative).ToList();
            if (parameter.CrossPolarity && positive.Count == 1 && negative.Count == 1) {
                int a = positive[0], b = negative[0];
                line = FitLine(units[a], units[b], parameter);
                var promotable = new HashSet<int>(parameter.GetPromotableReasons());
                Corroborate(units[a], copies[a], units[b], line, forward: true, promotable, parameter);
                Corroborate(units[b], copies[b], units[a], line, forward: false, promotable, parameter);
            }

            // (c) policies that depend on corroboration
            foreach (var results in copies) {
                foreach (var r in results) {
                    var corroborated = r.CorroboratedByAdduct || r.CorroboratedByPolarity.Count > 0;
                    if (parameter.ClassLevelPolicy == LipidRtClassLevelPolicy.Corroborated
                        && (r.Reason == LipidRtReason.ConfirmedRt || r.Reason == LipidRtReason.ConfirmedPromotedByPolarity)
                        && r.Candidate.TagLevel == LipidRtTagLevel.Class && !corroborated) {
                        r.Outcome = LipidRtOutcome.NotEvaluable;
                        r.Reason = LipidRtReason.PolicyClass;
                    }
                }
            }
            if (parameter.OperatingPoint == LipidRtOperatingPoint.Strict) {
                foreach (var results in copies) {
                    foreach (var r in results) {
                        if (r.Reason == LipidRtReason.ConfirmedRt && !r.CorroboratedByAdduct && r.CorroboratedByPolarity.Count == 0) {
                            r.Outcome = LipidRtOutcome.NotEvaluable;
                            r.Reason = LipidRtReason.Uncorroborated;
                        }
                    }
                }
            }
            return units.Select((u, i) => new LipidRtRunResult(u.Input, copies[i], u.Gate)).ToList();
        }

        private static void Corroborate(LipidRtRunResult unit, List<LipidRtObjectResult> results, LipidRtRunResult other, LipidRtCrossPolarityLine line,
            bool forward, HashSet<int> promotable, LipidRtCheckParameter parameter) {
            var index = EligibleByComposition(other);
            foreach (var r in results) {
                var s = r.Candidate;
                if (!IsEligible(s)) {
                    continue;
                }
                var mapped = forward ? line.Intercept + line.Slope * s.Rt : (s.Rt - line.Intercept) / line.Slope;
                var twins = index.TryGetValue(s.Composition, out var list)
                    ? list.Where(t => Math.Abs(t.Rt - mapped) <= line.Tolerance).ToList()
                    : new List<LipidRtCandidate>();
                r.Twins = twins.Select(t => new LipidRtTwin(t.Id, t.Adduct, t.Rt, other[t.Id].SingleRunReason)).ToList();
                var confirmedTwins = twins.Where(t => other[t.Id].SingleRunReason == LipidRtReason.ConfirmedRt).ToList();
                if (r.SingleRunReason == LipidRtReason.ConfirmedRt) {
                    if (confirmedTwins.Count > 0) {
                        r.CorroboratedByPolarity = confirmedTwins.Select(t => t.Id).ToList();
                        r.Score = r.Score.HasValue ? r.Score + 1d : null;
                    }
                    else if (twins.Count > 0 && twins.Any(t => other[t.Id].SingleRunReason == LipidRtReason.InconsistentOrder)) {
                        r.PolarityConflict = true;
                        if (parameter.PolarityConflict == LipidRtPolarityConflictPolicy.Demote) {
                            r.Outcome = LipidRtOutcome.NotEvaluable;
                            r.Reason = LipidRtReason.PolarityConflict;
                        }
                    }
                }
                else if (promotable.Contains((int)r.SingleRunReason) && confirmedTwins.Count > 0) {
                    r.Outcome = LipidRtOutcome.Confirmed;
                    r.Reason = LipidRtReason.ConfirmedPromotedByPolarity;
                    r.Score = (r.Score ?? 0d) + 0.5;
                    r.CorroboratedByPolarity = confirmedTwins.Select(t => t.Id).ToList();
                }
            }
        }

        private static bool IsEligible(LipidRtCandidate c) => c.IsEligible && c.Composition != null;

        private static Dictionary<LipidRtComposition, List<LipidRtCandidate>> EligibleByComposition(LipidRtRunResult unit) {
            var index = new Dictionary<LipidRtComposition, List<LipidRtCandidate>>();
            foreach (var c in unit.Input.Candidates) {
                if (!IsEligible(c)) {
                    continue;
                }
                var key = c.Composition;
                if (!index.TryGetValue(key, out var list)) {
                    index[key] = list = new List<LipidRtCandidate>();
                }
                list.Add(c);
            }
            return index;
        }

        /// <summary>Compositions with a single RT cluster (spread at most the collapse window) and their median RT.</summary>
        private static Dictionary<LipidRtComposition, double> UniqueRt(LipidRtRunResult unit, double window) {
            var result = new Dictionary<LipidRtComposition, double>();
            foreach (var pair in EligibleByComposition(unit)) {
                var rts = pair.Value.Select(c => c.Rt).ToList();
                if (rts.Max() - rts.Min() <= window) {
                    result[pair.Key] = LipidRtCheck.Median(rts);
                }
            }
            return result;
        }

        /// <summary>RtNeg ~ a + b RtPos by Theil-Sen over compositions with a single RT cluster in both units.</summary>
        private static LipidRtCrossPolarityLine FitLine(LipidRtRunResult positive, LipidRtRunResult negative, LipidRtCheckParameter parameter) {
            var spanA = positive.Span;
            var spanB = negative.Span;
            var ua = UniqueRt(positive, parameter.CollapseFraction * spanA);
            var ub = UniqueRt(negative, parameter.CollapseFraction * spanB);
            var keys = ua.Keys.Where(ub.ContainsKey).OrderBy(k => k).ToList();
            var x = keys.Select(k => ua[k]).ToArray();
            var y = keys.Select(k => ub[k]).ToArray();
            var floor = parameter.CrossPolarityToleranceFraction * Math.Max(spanA, spanB);
            if (keys.Count < 5) {
                return new LipidRtCrossPolarityLine(0d, 1d, null, keys.Count, floor);
            }
            var slopes = new List<double>();
            for (int i = 0; i < x.Length; i++) {
                for (int j = i + 1; j < x.Length; j++) {
                    if (Math.Abs(x[j] - x[i]) > 0.025 * spanA) {
                        slopes.Add((y[j] - y[i]) / (x[j] - x[i]));
                    }
                }
            }
            var b = slopes.Count >= 10 ? LipidRtCheck.Median(slopes) : 1d;
            var a = LipidRtCheck.Median(y.Select((yi, k) => yi - b * x[k]).ToList());
            var residuals = y.Select((yi, k) => yi - (a + b * x[k])).ToList();
            var center = LipidRtCheck.Median(residuals);
            var sd = 1.4826 * LipidRtCheck.Median(residuals.Select(r => Math.Abs(r - center)).ToList());
            var tolerance = Math.Max(floor, sd != 0 ? parameter.CrossPolarityToleranceSd * sd : 0d);
            return new LipidRtCrossPolarityLine(a, b, sd, keys.Count, tolerance);
        }
    }
}
