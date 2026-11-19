using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>Why a hypothesis did not fire (design 4.11 <c>failure</c>).</summary>
    internal enum HypothesisFailure
    {
        /// <summary>It fired.</summary>
        None,
        /// <summary>The logic is false even with every not node true.</summary>
        LogicFalse,
        /// <summary>The logic holds with every not node true but a veto blocks it.</summary>
        Veto,
        /// <summary>p2 found no base peak (no usable peak with |m - P_obs| &gt; 1.0).</summary>
        NoBasePeak,
        /// <summary>The exclusive assignment visited more than AssignmentCap leaves.</summary>
        AssignmentOverflow,
    }

    /// <summary>An item with its assigned peak, or an upgrade member with the peak it would match.</summary>
    internal sealed class MatchedItem
    {
        public MatchedItem(TermTargets term, TargetMember member, PeakHit peak, int unitId) {
            Term = term;
            Member = member;
            Peak = peak;
            UnitId = unitId;
        }

        public TermTargets Term { get; }
        public TargetMember Member { get; }
        public PeakHit Peak { get; }
        /// <summary>The evidence unit in canonical order; -1 for an upgrade, which is no evidence.</summary>
        public int UnitId { get; }
    }

    /// <summary>A veto term (veto_present or veto_stronger_than) of one hypothesis: <c>VetoOutcome</c>.</summary>
    internal sealed class VetoState
    {
        public VetoState(TermInfo term, bool fired, double iA, double iBMax, PeakHit? peak) {
            Term = term;
            Fired = fired;
            IA = iA;
            IBMax = iBMax;
            Peak = peak;
        }

        public TermInfo Term { get; }
        /// <summary>The veto condition holds (its not node is false).</summary>
        public bool Fired { get; }
        /// <summary>I_raw of the veto term.</summary>
        public double IA { get; }
        /// <summary>max I_raw over the than-terms; 0 for veto_present, which has none.</summary>
        public double IBMax { get; }
        /// <summary>The first hit with rel = I_raw (member order, then m/z order), or null when nothing hits.</summary>
        public PeakHit? Peak { get; }
    }

    /// <summary>A k_of node of one hypothesis: <c>KOfOutcome</c>.</summary>
    internal sealed class KOfState
    {
        public KOfState(string pointer, long k, long count, bool satisfied) {
            Pointer = pointer;
            K = k;
            Count = count;
            Satisfied = satisfied;
        }

        public string Pointer { get; }
        /// <summary>k_eff = max(1, k + KOffset).</summary>
        public long K { get; }
        public long Count { get; }
        public bool Satisfied { get; }
    }

    /// <summary>An any node of one hypothesis: <c>BranchOutcome</c>, the truth of each child.</summary>
    internal sealed class BranchState
    {
        public BranchState(string pointer, IReadOnlyList<bool> children) {
            Pointer = pointer;
            Children = children;
        }

        public string Pointer { get; }
        public IReadOnlyList<bool> Children { get; }
    }

    /// <summary>The evaluation of one hypothesis of one rule (design 4.6, 4.7).</summary>
    internal sealed class HypothesisEvaluation
    {
        public HypothesisEvaluation(EngineHypothesis hypothesis, IReadOnlyList<TermTargets> targets, bool fired, bool firedIgnoringVetoes,
                                    double score, HypothesisFailure failure, IReadOnlyList<MatchedItem> matched,
                                    IReadOnlyList<VetoState> vetoes, IReadOnlyList<KOfState> kOf, IReadOnlyList<BranchState> branches,
                                    IReadOnlyList<MatchedItem> upgrades) {
            Hypothesis = hypothesis;
            Targets = targets;
            Fired = fired;
            FiredIgnoringVetoes = firedIgnoringVetoes;
            Score = score;
            Failure = failure;
            Matched = matched;
            Vetoes = vetoes;
            KOf = kOf;
            Branches = branches;
            Upgrades = upgrades;
        }

        public EngineHypothesis Hypothesis { get; }
        public IReadOnlyList<TermTargets> Targets { get; }
        public bool Fired { get; }
        /// <summary>The logic with every not node true, under the chosen assignment, which satisfies it whenever some
        /// assignment does (the search's second key; replaces OQ15).</summary>
        public bool FiredIgnoringVetoes { get; }
        /// <summary>fired_ignoring_vetoes and not fired.</summary>
        public bool VetoBlocked => FiredIgnoringVetoes && !Fired;
        public double Score { get; }
        public HypothesisFailure Failure { get; }
        /// <summary>The items with a peak, in canonical item order.</summary>
        public IReadOnlyList<MatchedItem> Matched { get; }
        /// <summary>Every veto term, in term order.</summary>
        public IReadOnlyList<VetoState> Vetoes { get; }
        /// <summary>Every k_of node in pre-order.</summary>
        public IReadOnlyList<KOfState> KOf { get; }
        /// <summary>Every any node in pre-order.</summary>
        public IReadOnlyList<BranchState> Branches { get; }
        /// <summary>Per upgrade member, its first candidate (design 4.6 order, sharing with items allowed; OQ13).</summary>
        public IReadOnlyList<MatchedItem> Upgrades { get; }
    }

    /// <summary>
    /// One rule as an evaluator runs it, built once at construction from the load's compiled rule and the option
    /// snapshot: the hypothesis index, the targets, the logic plan, the rule's convention, tolerances, loss reference,
    /// decoy shift, k offset and assignment cap, and whether the rule still passes both two-ion passes at the effective
    /// KOffset (design 4.7). Immutable after construction; every evaluation allocates its own state.
    /// </summary>
    internal sealed class RuleEngine
    {
        private readonly int[] _groupOfTerm;

        public RuleEngine(Rule rule, RuleInfo info, LoadedTables tables, EvaluationSettings settings) {
            Rule = rule;
            Info = info;
            Tables = tables;
            Index = new HypothesisIndex(info, tables);
            Builder = new TargetBuilder(Index);
            Plan = LogicPlan.Build(rule, info);
            _groupOfTerm = EvidenceItems.GroupOfTerm(info);
            Tolerance = settings.ProductToleranceFor(rule);
            Match = new MatchSettings(rule.Convention, Tolerance, settings.FloorScale);
            PrecursorTolerance = settings.PrecursorToleranceFor(rule);
            LossReference = settings.LossReferenceFor(rule);
            DecoyPda = settings.DecoyShiftPda;
            KOffset = settings.KOffset;
            AssignmentCap = settings.AssignmentCap;
            // At KOffset 0 the passes are exactly the load's, which this rule passed; only an offset can change them.
            VariantSingleIon = info.Measure && KOffset != 0 && TwoIonCheck.TwoIonFailures(info, tables, KOffset).Count > 0;
        }

        public Rule Rule { get; }
        public RuleInfo Info { get; }
        public LoadedTables Tables { get; }
        public HypothesisIndex Index { get; }
        public TargetBuilder Builder { get; }
        public LogicPlan Plan { get; }
        public AppliedProductTolerance Tolerance { get; }
        public MatchSettings Match { get; }
        public double PrecursorTolerance { get; }
        public LossReference LossReference { get; }
        public long DecoyPda { get; }
        public int KOffset { get; }
        public int AssignmentCap { get; }

        /// <summary>A measure:true rule that fails a two-ion pass at the effective KOffset: still evaluated, never
        /// claim-eligible (design 4.7).</summary>
        public bool VariantSingleIon { get; }

        /// <summary>Evaluates one hypothesis under the full logic.</summary>
        public HypothesisEvaluation Evaluate(SpectrumView view, EngineHypothesis hypothesis) => Evaluate(view, hypothesis, -1, -1);

        /// <summary>
        /// Evaluates one hypothesis, optionally with the any node <paramref name="restrictAny"/> (pre-order index)
        /// replaced by its child <paramref name="restrictChild"/>; the assignment is chosen for that logic.
        /// <paramref name="view"/> must be evaluated (<see cref="SpectrumView.Status"/>).
        /// </summary>
        public HypothesisEvaluation Evaluate(SpectrumView view, EngineHypothesis hypothesis, int restrictAny, int restrictChild) {
            var targets = Builder.Build(hypothesis.Series, DecoyPda, LossReference, view.PrecursorMz);
            var nTerms = targets.Count;
            var memberHits = new IReadOnlyList<PeakHit>[nTerms][];
            var iRaw = new double[nTerms];
            var rawPresent = new bool[nTerms];
            var strongest = new PeakHit?[nTerms];
            for (var t = 0; t < nTerms; t++) {
                var term = targets[t];
                memberHits[t] = new IReadOnlyList<PeakHit>[term.Members.Count];
                for (var m = 0; m < term.Members.Count; m++) {
                    var hits = new List<PeakHit>();
                    view.Hits(term, term.Members[m], Match, hits);
                    memberHits[t][m] = hits;
                    foreach (var h in hits) {
                        if (h.Rel > iRaw[t]) {
                            iRaw[t] = h.Rel;
                            strongest[t] = h;
                        }
                        else if (!strongest[t].HasValue) {
                            strongest[t] = h;
                        }
                        rawPresent[t] |= h.Present;
                    }
                }
            }
            var items = EvidenceItems.Build(Info, _groupOfTerm, memberHits);
            var counts = hypothesis.Series.Counts;
            var kValues = TwoIonCheck.KValues(Info, Tables, counts);
            var kEff = new long[kValues.Length];
            for (var i = 0; i < kEff.Length; i++) {
                var k = kValues[i] + KOffset;
                kEff[i] = k < 1 ? 1 : k;
            }
            var noAssignment = Inputs(items, null, iRaw, rawPresent, kEff);
            var vetoes = Vetoes(noAssignment, strongest);
            if (!view.HasReference(Rule.Convention)) {
                return NotEvaluated(hypothesis, targets, HypothesisFailure.NoBasePeak, vetoes);
            }
            // The veto-free logic is the search's second key, so fired_ignoring_vetoes below is read on an assignment
            // that satisfies it whenever any assignment does (design 4.7; see ExclusiveAssignment).
            var assignment = ExclusiveAssignment.Assign(
                items, choice => LogicEvaluator.Evaluate(Plan, Inputs(items, choice, iRaw, rawPresent, kEff), false, restrictAny, restrictChild, null),
                choice => LogicEvaluator.Evaluate(Plan, Inputs(items, choice, iRaw, rawPresent, kEff), true, restrictAny, restrictChild, null),
                AssignmentCap);
            if (assignment.Overflow) {
                return NotEvaluated(hypothesis, targets, HypothesisFailure.AssignmentOverflow, vetoes);
            }
            var chosen = assignment.Choice!;
            var inputs = Inputs(items, chosen, iRaw, rawPresent, kEff);
            var record = new LogicRecord(Plan);
            var fired = LogicEvaluator.Evaluate(Plan, inputs, false, restrictAny, restrictChild, record);
            var firedIgnoringVetoes = LogicEvaluator.Evaluate(Plan, inputs, true, restrictAny, restrictChild, null);
            var matched = new List<MatchedItem>();
            foreach (var item in items.Items) {
                if (chosen[item.Index] >= 0) {
                    var term = targets[item.TermIndex];
                    matched.Add(new MatchedItem(term, term.Members[item.MemberIndex], item.Candidates[chosen[item.Index]], item.UnitId));
                }
            }
            var kOf = new List<KOfState>();
            for (var s = 0; s < Plan.KNodes.Count; s++) {
                kOf.Add(new KOfState(Plan.KNodes[s].Pointer, kEff[s], record.KCount[s], record.KSatisfied[s]));
            }
            var branches = new List<BranchState>();
            for (var a = 0; a < Plan.AnyNodes.Count; a++) {
                branches.Add(new BranchState(Plan.AnyNodes[a].Pointer, new ReadOnlyCollection<bool>(record.AnyChildren[a])));
            }
            var upgrades = new List<MatchedItem>();
            foreach (var t in Info.Terms) {
                if (t.Role != "upgrade") {
                    continue;
                }
                var term = targets[t.Index];
                for (var m = 0; m < term.Members.Count; m++) {
                    var candidates = SpectrumView.Candidates(memberHits[t.Index][m]);
                    if (candidates.Count > 0) {
                        upgrades.Add(new MatchedItem(term, term.Members[m], candidates[0], -1));
                    }
                }
            }
            var failure = fired ? HypothesisFailure.None : firedIgnoringVetoes ? HypothesisFailure.Veto : HypothesisFailure.LogicFalse;
            return new HypothesisEvaluation(hypothesis, targets, fired, firedIgnoringVetoes, ExclusiveAssignment.Score(items, chosen), failure,
                                            new ReadOnlyCollection<MatchedItem>(matched), vetoes, new ReadOnlyCollection<KOfState>(kOf),
                                            new ReadOnlyCollection<BranchState>(branches), new ReadOnlyCollection<MatchedItem>(upgrades));
        }

        /// <summary>
        /// <c>branch_fired</c> (design 4.7, OQ14): for each any node (pre-order) and each of its children, whether the
        /// rule fires with that node replaced by that child, every hypothesis re-evaluated by the same code, its
        /// assignment included. <paramref name="full"/> holds the hypotheses' full-logic evaluations; those that
        /// cannot fire under any restriction (<see cref="MayFireUnderRestriction"/>) are skipped, which changes no
        /// output and saves a capped search per any-node child for each overflowing hypothesis.
        /// </summary>
        public bool[][] BranchFired(SpectrumView view, IReadOnlyList<HypothesisEvaluation> full) {
            var output = new bool[Plan.AnyNodes.Count][];
            for (var a = 0; a < output.Length; a++) {
                output[a] = new bool[Plan.AnyNodes[a].Children.Length];
                for (var c = 0; c < output[a].Length; c++) {
                    foreach (var e in full) {
                        if (MayFireUnderRestriction(e) && Evaluate(view, e.Hypothesis, a, c).Fired) {
                            output[a][c] = true;
                            break;
                        }
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// False when the full evaluation failed in a way no logic restriction can change: <c>no_base_peak</c> depends
        /// on the view and the convention only, and <c>assignment_overflow</c> on the items only, since the search
        /// visits the same leaves under every logic (it never prunes on the logic, which only ranks the leaves).
        /// </summary>
        public static bool MayFireUnderRestriction(HypothesisEvaluation full) =>
            full.Failure != HypothesisFailure.NoBasePeak && full.Failure != HypothesisFailure.AssignmentOverflow;

        private static HypothesisEvaluation NotEvaluated(EngineHypothesis hypothesis, IReadOnlyList<TermTargets> targets, HypothesisFailure failure,
                                                         IReadOnlyList<VetoState> vetoes) {
            // No assignment exists, so nothing is matched and no k_of, branch or upgrade is reported; the vetoes do
            // not depend on an assignment and are.
            var none = new ReadOnlyCollection<MatchedItem>(new MatchedItem[0]);
            return new HypothesisEvaluation(hypothesis, targets, false, false, 0.0, failure, none, vetoes,
                                            new ReadOnlyCollection<KOfState>(new KOfState[0]),
                                            new ReadOnlyCollection<BranchState>(new BranchState[0]), none);
        }

        private IReadOnlyList<VetoState> Vetoes(LogicInputs inputs, PeakHit?[] strongest) {
            var vetoes = new List<VetoState>();
            foreach (var t in Info.Terms) {
                var role = Plan.Roles[t.Index];
                if (role != TermRole.VetoPresent && role != TermRole.VetoStrongerThan) {
                    continue;
                }
                var iBMax = role == TermRole.VetoStrongerThan ? LogicEvaluator.MaxThan(Plan, inputs, t.Index) : 0.0;
                vetoes.Add(new VetoState(t, LogicEvaluator.TermValue(Plan, inputs, t.Index), inputs.IRaw[t.Index], iBMax, strongest[t.Index]));
            }
            return new ReadOnlyCollection<VetoState>(vetoes);
        }

        private static LogicInputs Inputs(EvidenceItems items, int[]? choice, double[] iRaw, bool[] rawPresent, long[] kEff) {
            var assigned = new bool[items.TermCount];
            var units = new List<int>[items.TermCount];
            if (choice != null) {
                foreach (var item in items.Items) {
                    if (choice[item.Index] < 0) {
                        continue;
                    }
                    assigned[item.TermIndex] = true;
                    var list = units[item.TermIndex] ?? (units[item.TermIndex] = new List<int>());
                    if (!list.Contains(item.UnitId)) {
                        list.Add(item.UnitId);
                    }
                }
            }
            var assignedUnits = new int[items.TermCount][];
            for (var t = 0; t < assignedUnits.Length; t++) {
                assignedUnits[t] = units[t]?.ToArray() ?? new int[0];
            }
            return new LogicInputs(assigned, assignedUnits, iRaw, rawPresent, kEff);
        }
    }
}
