using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>Why a rule has no hypotheses before any precursor is compared (OQ03, OQ04).</summary>
    internal enum PreconditionFailure
    {
        None,
        /// <summary>The rule's ion mode is not the spectrum's: the rule is not tried.</summary>
        IonMode,
        /// <summary>mz_only: the evidence adduct is not in the rule's own list, and there is no fallback.</summary>
        AdductNotInRule,
    }

    /// <summary>One composition of a rule's series (design 4.2), in enumeration order.</summary>
    internal sealed class CompositionRecord
    {
        public CompositionRecord(int entryIndex, string entryId, long[] counts, long neutralPda) {
            EntryIndex = entryIndex;
            EntryId = entryId;
            Counts = counts;
            NeutralPda = neutralPda;
        }

        public int EntryIndex { get; }
        /// <summary>The scaffold id, or "core" when the rule has no scaffold list.</summary>
        public string EntryId { get; }
        /// <summary>Unit counts per units-table row (a copy; never mutated).</summary>
        public long[] Counts { get; }
        public long NeutralPda { get; }
    }

    /// <summary>A hypothesis that passed the precursor predicate, with its ids (design 4.2, 4.3).</summary>
    internal sealed class EngineHypothesis
    {
        public EngineHypothesis(Hypothesis<long> series, int compositionIndex, int adductIndex, string compositionId,
                                string hypothesisId, string? scaffoldId, IReadOnlyList<KeyValuePair<string, long>> units,
                                string neutralFormula, double hypothesisMz, double precursorErrorDa) {
            Series = series;
            CompositionIndex = compositionIndex;
            AdductIndex = adductIndex;
            CompositionId = compositionId;
            HypothesisId = hypothesisId;
            ScaffoldId = scaffoldId;
            Units = units;
            NeutralFormula = neutralFormula;
            HypothesisMz = hypothesisMz;
            PrecursorErrorDa = precursorErrorDa;
        }

        /// <summary>The series hypothesis: compositions and pDa masses, as the loader builds them.</summary>
        public Hypothesis<long> Series { get; }
        public int CompositionIndex { get; }
        /// <summary>The index of the adduct in the rule's tried list.</summary>
        public int AdductIndex { get; }
        /// <summary><c>&lt;scaffold_id|core&gt;|&lt;units&gt;</c>, units in table order with zero counts omitted.</summary>
        public string CompositionId { get; }
        /// <summary><c>&lt;composition_id&gt;|&lt;canonical adduct&gt;</c>.</summary>
        public string HypothesisId { get; }
        /// <summary>Null when the rule has no scaffold list (composition_id still says core) (OQ28).</summary>
        public string? ScaffoldId { get; }
        /// <summary>Non-zero unit counts in table order (OQ28).</summary>
        public IReadOnlyList<KeyValuePair<string, long>> Units { get; }
        /// <summary>The Hill formula of the rule's own neutral composition.</summary>
        public string NeutralFormula { get; }
        public string Adduct => Series.Adduct.Adduct.Adduct;
        /// <summary>"rule" or "common".</summary>
        public string AdductSource => Series.Adduct.SourceName;
        /// <summary>(double)(mult * M + add - remove - z * e) / 1e12.</summary>
        public double HypothesisMz { get; }
        /// <summary>P_obs - hypothesis m/z, the sign of error_da = peak - target (OQ27).</summary>
        public double PrecursorErrorDa { get; }
    }

    /// <summary>The hypotheses of one rule for one spectrum (design 4.3).</summary>
    internal sealed class HypothesisSet
    {
        public HypothesisSet(PreconditionMode mode, AdductTier tier, PreconditionFailure failure, IReadOnlyList<EngineHypothesis> hypotheses) {
            Mode = mode;
            Tier = tier;
            Failure = failure;
            Hypotheses = hypotheses;
        }

        public PreconditionMode Mode { get; }
        public AdductTier Tier { get; }
        public PreconditionFailure Failure { get; }
        /// <summary>The hypotheses that pass the precursor predicate, ordered by hypothesis_id ordinally; their count is
        /// <c>n_hypotheses</c>.</summary>
        public IReadOnlyList<EngineHypothesis> Hypotheses { get; }
    }

    /// <summary>
    /// The compositions of one rule and a precursor lookup over them (design 4.3). The neutral masses are kept as one
    /// sorted <c>long[]</c> per rule, tie-broken by composition index; the neutral mass does not depend on the adduct,
    /// so every adduct of the rule searches the same array with its own window
    /// <c>((P +- (tol + 1e-10)) * 1e12 + z * e - add + remove) / mult</c>, widened by 10^6 pDa. The exact predicate
    /// <c>|P_obs - mz_h| &lt;= tol + 1e-10</c> decides; the window only bounds the search. Built once, then immutable.
    /// </summary>
    internal sealed class HypothesisIndex
    {
        /// <summary>The lookup window is widened by this many pDa on each side (10^-6 Da).</summary>
        public const long WindowMarginPda = 1000000L;

        private readonly CompositionRecord[] _compositions;
        private readonly long[][] _agly;
        private readonly long[] _sortedNeutral;
        private readonly int[] _sortedIndex;
        private readonly long[] _addPda;
        private readonly long[] _removePda;

        public HypothesisIndex(RuleInfo rule, LoadedTables tables) {
            Rule = rule;
            Tables = tables;
            Series = new SeriesBuilder<long, LongPda>(rule, tables);
            var compositions = new List<CompositionRecord>();
            var agly = new long[rule.Scaffold?.Entries.Count ?? 1][];
            Series.ForEachComposition((entryIndex, entryId, entryAgly, counts) => {
                agly[entryIndex] = entryAgly;
                var copy = (long[])counts.Clone();
                var neutral = Series.Mass(Series.NeutralComposition(entryAgly, copy));
                compositions.Add(new CompositionRecord(entryIndex, entryId, copy, neutral));
            });
            _compositions = compositions.ToArray();
            _agly = agly;
            var order = new int[_compositions.Length];
            for (var i = 0; i < order.Length; i++) {
                order[i] = i;
            }
            Array.Sort(order, (a, b) => {
                var c = _compositions[a].NeutralPda.CompareTo(_compositions[b].NeutralPda);
                return c != 0 ? c : a.CompareTo(b);
            });
            _sortedIndex = order;
            _sortedNeutral = new long[order.Length];
            for (var i = 0; i < order.Length; i++) {
                _sortedNeutral[i] = _compositions[order[i]].NeutralPda;
            }
            _addPda = new long[rule.Tried.Count];
            _removePda = new long[rule.Tried.Count];
            for (var a = 0; a < rule.Tried.Count; a++) {
                _addPda[a] = Series.Mass(Series.Vec(rule.Tried[a].Adduct.Add));
                _removePda[a] = Series.Mass(Series.Vec(rule.Tried[a].Adduct.Remove));
            }
            Compositions = new ReadOnlyCollection<CompositionRecord>(_compositions);
        }

        public RuleInfo Rule { get; }
        public LoadedTables Tables { get; }
        public SeriesBuilder<long, LongPda> Series { get; }

        /// <summary>Every composition in enumeration order (design 4.2).</summary>
        public IReadOnlyList<CompositionRecord> Compositions { get; }

        /// <summary>The adducts the rule tries in adduct_ambiguous mode, in try order.</summary>
        public IReadOnlyList<AdductTry> Tried => Rule.Tried;

        /// <summary>mult * M + add - remove - z * e in pDa for a composition under a tried adduct.</summary>
        public long PrecursorPda(int composition, int adduct) {
            var a = Rule.Tried[adduct].Adduct;
            return checked(a.Multiplier * _compositions[composition].NeutralPda + _addPda[adduct] - _removePda[adduct] - a.Charge * Series.Electron);
        }

        /// <summary>The precursor predicate (design 4.3): |P_obs - mz_h| &lt;= tol + 1e-10, mz_h = (double)pDa / 1e12.</summary>
        public bool PrecursorMatches(int composition, int adduct, double precursorMz, double tolerance) =>
            Math.Abs(precursorMz - ElementTable.ToDa(PrecursorPda(composition, adduct))) <= tolerance + SpectrumView.Slack;

        /// <summary>
        /// Appends the compositions whose precursor under tried adduct <paramref name="adduct"/> passes the predicate, in
        /// (neutral pDa, composition index) order. Never throws: a window outside the representable range is clamped,
        /// and one that is empty or not a number finds nothing.
        /// </summary>
        public void Find(int adduct, double precursorMz, double tolerance, List<int> output) {
            var a = Rule.Tried[adduct].Adduct;
            var limit = tolerance + SpectrumView.Slack;
            var shift = (double)(a.Charge * Series.Electron - _addPda[adduct] + _removePda[adduct]);
            var mult = (double)a.Multiplier;
            var lo = ((precursorMz - limit) * 1e12 + shift) / mult - WindowMarginPda;
            var hi = ((precursorMz + limit) * 1e12 + shift) / mult + WindowMarginPda;
            if (!(lo <= hi) || hi < 0.0 || lo >= MassPda.Limit) {
                return;
            }
            var loPda = lo <= 0.0 ? 0L : (long)Math.Floor(lo);
            var hiPda = hi >= MassPda.Limit ? MassPda.Limit : (long)Math.Ceiling(hi);
            int left = 0, right = _sortedNeutral.Length;
            while (left < right) {
                var mid = left + (right - left) / 2;
                if (_sortedNeutral[mid] >= loPda) {
                    right = mid;
                }
                else {
                    left = mid + 1;
                }
            }
            for (var k = left; k < _sortedNeutral.Length && _sortedNeutral[k] <= hiPda; k++) {
                if (PrecursorMatches(_sortedIndex[k], adduct, precursorMz, tolerance)) {
                    output.Add(_sortedIndex[k]);
                }
            }
        }

        /// <summary>The series hypothesis of a composition under a tried adduct.</summary>
        public Hypothesis<long> SeriesHypothesis(int composition, int adduct) {
            var c = _compositions[composition];
            var agly = _agly[c.EntryIndex];
            var comp = Series.NeutralComposition(agly, c.Counts);
            return Series.MakeHypothesis(c.EntryIndex, c.EntryId, c.Counts, Rule.Tried[adduct], agly, comp, c.NeutralPda);
        }

        /// <summary><c>&lt;scaffold_id|core&gt;|&lt;units&gt;</c>.</summary>
        public string CompositionId(int composition) {
            var c = _compositions[composition];
            return c.EntryId + "|" + Series.UnitsKey(c.Counts);
        }

        /// <summary>A hypothesis with its ids, m/z and precursor error.</summary>
        public EngineHypothesis Hypothesis(int composition, int adduct, double precursorMz) {
            var series = SeriesHypothesis(composition, adduct);
            var compositionId = CompositionId(composition);
            var units = new List<KeyValuePair<string, long>>();
            var counts = _compositions[composition].Counts;
            for (var u = 0; u < counts.Length; u++) {
                if (counts[u] != 0) {
                    units.Add(new KeyValuePair<string, long>(Tables.Units[u].Name, counts[u]));
                }
            }
            var mz = ElementTable.ToDa(series.PrecursorPda);
            return new EngineHypothesis(series, composition, adduct, compositionId, compositionId + "|" + series.Adduct.Adduct.Adduct,
                                        Rule.Scaffold != null ? series.EntryId : null, new ReadOnlyCollection<KeyValuePair<string, long>>(units),
                                        Composition.Format(series.Comp, Tables.Elements), mz, precursorMz - mz);
        }

        /// <summary>
        /// The hypotheses of the rule for a spectrum (design 4.3). Without evidence the mode is adduct_ambiguous: every
        /// tried adduct (the rule's list, then the common adducts of the polarity), tier assumed. With evidence it is
        /// mz_only: only the evidence adduct, and only when the rule's own list names it (otherwise no hypotheses and
        /// <see cref="PreconditionFailure.AdductNotInRule"/>, with no fallback), at the evidence tier. A rule of the other
        /// polarity is not tried (<see cref="PreconditionFailure.IonMode"/>, OQ04). The passing hypotheses are ordered by
        /// hypothesis_id ordinally.
        /// </summary>
        public HypothesisSet Select(double precursorMz, DiagnosticIonMode ionMode, AdductEvidence? evidence, double tolerance) {
            var mode = evidence is null ? PreconditionMode.AdductAmbiguous : PreconditionMode.MzOnly;
            var tier = evidence?.Tier ?? AdductTier.Assumed;
            var empty = new ReadOnlyCollection<EngineHypothesis>(new EngineHypothesis[0]);
            var ruleMode = Rule.Charge == 1 ? DiagnosticIonMode.Positive : DiagnosticIonMode.Negative;
            if (ruleMode != ionMode) {
                return new HypothesisSet(mode, tier, PreconditionFailure.IonMode, empty);
            }
            var adducts = new List<int>();
            for (var a = 0; a < Rule.Tried.Count; a++) {
                if (evidence is null || (Rule.Tried[a].FromRule && string.Equals(Rule.Tried[a].Adduct.Adduct, evidence.CanonicalAdduct, StringComparison.Ordinal))) {
                    adducts.Add(a);
                }
            }
            if (adducts.Count == 0) {
                return new HypothesisSet(mode, tier, PreconditionFailure.AdductNotInRule, empty);
            }
            var found = new List<EngineHypothesis>();
            var compositions = new List<int>();
            foreach (var a in adducts) {
                compositions.Clear();
                Find(a, precursorMz, tolerance, compositions);
                foreach (var c in compositions) {
                    found.Add(Hypothesis(c, a, precursorMz));
                }
            }
            found.Sort((x, y) => string.CompareOrdinal(x.HypothesisId, y.HypothesisId));
            return new HypothesisSet(mode, tier, PreconditionFailure.None, new ReadOnlyCollection<EngineHypothesis>(found));
        }
    }
}
