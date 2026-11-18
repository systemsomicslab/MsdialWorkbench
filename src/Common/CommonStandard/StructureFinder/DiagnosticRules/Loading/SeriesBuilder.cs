using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Loading
{
    /// <summary>One hypothesis: a composition (scaffold entry and unit counts) under one tried adduct (design 4.2, 4.3).</summary>
    internal sealed class Hypothesis<T>
    {
        public Hypothesis(int entryIndex, string entryId, long[] counts, AdductTry adduct, T[] agly, T[] comp,
                          T neutralPda, T precursorPda, T[] precursorComp) {
            EntryIndex = entryIndex;
            EntryId = entryId;
            Counts = counts;
            Adduct = adduct;
            Agly = agly;
            Comp = comp;
            NeutralPda = neutralPda;
            PrecursorPda = precursorPda;
            PrecursorComp = precursorComp;
        }

        public int EntryIndex { get; }
        /// <summary>The scaffold id, or "core" when the rule has no scaffold list.</summary>
        public string EntryId { get; }
        /// <summary>Unit counts per units-table row.</summary>
        public long[] Counts { get; }
        public AdductTry Adduct { get; }
        /// <summary>core + scaffold.</summary>
        public T[] Agly { get; }
        /// <summary>The neutral composition: core + scaffold + sum of count * residue.</summary>
        public T[] Comp { get; }
        public T NeutralPda { get; }
        /// <summary>mult * M + add - remove - z * e.</summary>
        public T PrecursorPda { get; }
        /// <summary>mult * composition + add - remove (checked for negative counts).</summary>
        public T[] PrecursorComp { get; }
    }

    /// <summary>A member of a term in one hypothesis: its target, key, composition and mass (design 4.5).</summary>
    internal readonly struct Member<T>
    {
        public Member(T target, string key, T[] comp, T mass, bool isLoss) {
            Target = target;
            Key = key;
            Comp = comp;
            Mass = mass;
            IsLoss = isLoss;
        }

        public T Target { get; }
        /// <summary>Equal-mass members collapse into one, their keys joined by "+".</summary>
        public string Key { get; }
        public T[] Comp { get; }
        public T Mass { get; }
        public bool IsLoss { get; }
    }

    /// <summary>
    /// Composition series, hypotheses and term members of one rule (design 4.2-4.5), in the arithmetic
    /// <typeparamref name="TM"/>. Mirrors <c>iter_compositions</c>, <c>iter_hypotheses</c>, <c>ionise</c> and
    /// <c>term_members</c> of the Python reference.
    /// </summary>
    internal sealed class SeriesBuilder<T, TM> where TM : struct, IPdaMath<T>
    {
        private static readonly TM M = default;

        private readonly RuleInfo _rule;
        private readonly LoadedTables _tables;
        private readonly T[] _pda;
        private readonly T _electron;
        private readonly T[][] _residues;
        private readonly UnitClass[] _unitClass;
        private readonly int _nElements;

        public SeriesBuilder(RuleInfo rule, LoadedTables tables) {
            _rule = rule;
            _tables = tables;
            var el = tables.Elements;
            _nElements = el.Count;
            _pda = new T[el.Count];
            for (var i = 0; i < el.Count; i++) {
                _pda[i] = M.FromBig(el.PdaExact(i));
            }
            _electron = M.FromBig(el.ElectronPdaExact);
            _residues = new T[tables.Units.Count][];
            _unitClass = new UnitClass[tables.Units.Count];
            for (var u = 0; u < tables.Units.Count; u++) {
                _residues[u] = Vec(tables.Units[u].Residue);
                _unitClass[u] = tables.Units[u].Class;
            }
        }

        public T[] Vec(long[] counts) {
            var v = new T[counts.Length];
            for (var i = 0; i < counts.Length; i++) {
                v[i] = M.FromLong(counts[i]);
            }
            return v;
        }

        /// <summary>a + k * b.</summary>
        public T[] AddVec(T[] a, T[] b, long k = 1) {
            var r = new T[a.Length];
            var kk = M.FromLong(k);
            for (var i = 0; i < a.Length; i++) {
                r[i] = M.Add(a[i], M.Mul(kk, b[i]));
            }
            return r;
        }

        public T[] AddVec(T[] a, long[] b, long k = 1) => AddVec(a, Vec(b), k);

        public T Mass(T[] comp) {
            var total = M.Zero;
            for (var i = 0; i < comp.Length; i++) {
                total = M.Add(total, M.Mul(comp[i], _pda[i]));
            }
            return total;
        }

        public static bool IsNegative(T[] comp) {
            foreach (var x in comp) {
                if (M.Compare(x, M.Zero) < 0) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Unit counts in table order with zeros omitted, for example "Hex1,dHex1".</summary>
        public string UnitsKey(long[] counts) {
            var sb = new StringBuilder();
            for (var u = 0; u < counts.Length; u++) {
                if (counts[u] == 0) {
                    continue;
                }
                if (sb.Length > 0) {
                    sb.Append(',');
                }
                sb.Append(_tables.Units[u].Name);
                sb.Append(counts[u].ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Compositions in design 4.2 order: scaffold entries in list order (or one "core" entry), then unit vectors
        /// as an odometer over units-table order (the first unit outermost, counts ascending), skipping vectors
        /// outside unit_totals. A class sum above its maximum prunes the rest of that branch, which never
        /// changes the order.
        /// </summary>
        public void ForEachComposition(Action<int, string, T[], long[]> visit) {
            var entries = new List<KeyValuePair<string, T[]>>();
            var core = Vec(_rule.Core);
            if (_rule.Scaffold != null) {
                foreach (var e in _rule.Scaffold.Entries) {
                    entries.Add(new KeyValuePair<string, T[]>(e.ScaffoldId, AddVec(core, e.Formula)));
                }
            }
            else {
                entries.Add(new KeyValuePair<string, T[]>("core", core));
            }
            var nUnits = _tables.Units.Count;
            var totalsLo = new long?[3];
            var totalsHi = new long?[3];
            foreach (var t in _rule.Totals) {
                var cls = (int)RuleNames.ParseUnitClass(t.Key);
                totalsLo[cls] = t.Value[0];
                totalsHi[cls] = t.Value[1];
            }
            for (var e = 0; e < entries.Count; e++) {
                var counts = new long[nUnits];
                var sums = new long[3];
                Odometer(0, counts, sums, totalsLo, totalsHi, () => visit(e, entries[e].Key, entries[e].Value, counts));
            }
        }

        private void Odometer(int u, long[] counts, long[] sums, long?[] lo, long?[] hi, Action leaf) {
            if (u == counts.Length) {
                for (var cls = 0; cls < 3; cls++) {
                    if (lo[cls].HasValue && (sums[cls] < lo[cls]!.Value || sums[cls] > hi[cls]!.Value)) {
                        return;
                    }
                }
                leaf();
                return;
            }
            var range = _rule.UnitRanges[u];
            var c = (int)_unitClass[u];
            for (var n = range[0]; n <= range[1]; n++) {
                counts[u] = n;
                sums[c] += n;
                var over = hi[c].HasValue && sums[c] > hi[c]!.Value;
                if (!over) {
                    Odometer(u + 1, counts, sums, lo, hi, leaf);
                }
                sums[c] -= n;
                if (over) {
                    break;
                }
            }
            counts[u] = 0;
        }

        /// <summary>Every hypothesis: each composition under each tried adduct, in that order.</summary>
        public void ForEachHypothesis(Action<Hypothesis<T>> visit) {
            ForEachComposition((index, entryId, agly, counts) => {
                var comp = agly;
                for (var u = 0; u < counts.Length; u++) {
                    if (counts[u] != 0) {
                        comp = AddVec(comp, _residues[u], counts[u]);
                    }
                }
                var neutral = Mass(comp);
                foreach (var a in _rule.Tried) {
                    var add = Vec(a.Adduct.Add);
                    var remove = Vec(a.Adduct.Remove);
                    var mult = M.FromLong(a.Adduct.Multiplier);
                    var scaled = new T[comp.Length];
                    for (var i = 0; i < comp.Length; i++) {
                        scaled[i] = M.Mul(comp[i], mult);
                    }
                    var preComp = AddVec(AddVec(scaled, add), remove, -1);
                    var precursor = M.Sub(M.Sub(M.Add(M.Mul(mult, neutral), Mass(add)), Mass(remove)),
                                          M.Mul(M.FromLong(a.Adduct.Charge), _electron));
                    visit(new Hypothesis<T>(index, entryId, counts, a, agly, comp, neutral, precursor, preComp));
                }
            });
        }

        /// <summary>The aglycone-based ion (design 4.5): even or radical, proton or adduct carrier, then plus and minus.</summary>
        private T[] Ionise(T[] agly, TermInfo t, Hypothesis<T> h) {
            var form = ((JsonString)t.Obj["form"]).Value;
            var carrier = ((JsonString)t.Obj["charge_carrier"]).Value;
            var z = h.Adduct.Adduct.Charge;
            T[] ion;
            if (carrier == "adduct") {
                ion = AddVec(AddVec(agly, h.Adduct.Adduct.Add), h.Adduct.Adduct.Remove, -1);
            }
            else if (form == "even") {
                ion = AddVec(agly, Hydrogen(), z);
            }
            else {
                ion = z == 1 ? (T[])agly.Clone() : AddVec(agly, Hydrogen(), -2);
            }
            if (t.Plus != null) {
                ion = AddVec(ion, t.Plus);
            }
            if (t.Minus != null) {
                ion = AddVec(ion, t.Minus, -1);
            }
            return ion;
        }

        private long[] Hydrogen() {
            var h = new long[_nElements];
            h[_tables.Elements.HydrogenIndex] = 1;
            return h;
        }

        private struct RawMember
        {
            public T[] Comp;
            public string Key;
            public bool IsLoss;
        }

        /// <summary>
        /// The members of a term in a hypothesis, sorted by target then key (ordinal), members of equal target
        /// collapsed with their keys joined by "+". A window yields its centre.
        /// </summary>
        public List<Member<T>> TermMembers(TermInfo t, Hypothesis<T> h) {
            var z = h.Adduct.Adduct.Charge;
            var raw = new List<RawMember>();
            if (t.Kind == "product_ion" || t.Kind == "loss_from_precursor" || t.Kind == "window") {
                var isLoss = t.Kind == "loss_from_precursor" || (t.Kind == "window" && ((JsonString)t.Obj["as"]).Value == "loss");
                if (t.Formula != null) {
                    raw.Add(new RawMember { Comp = Vec(t.Formula), Key = ((JsonString)t.Obj["formula"]).Value, IsLoss = isLoss });
                }
                else if (t.UnitSelector != null) {
                    for (var u = 0; u < _tables.Units.Count; u++) {
                        var unit = _tables.Units[u];
                        var selected = t.UnitSelector == "class" ? unit.ClassText : unit.Name;
                        if (h.Counts[u] != 0 && selected == t.UnitValue) {
                            var comp = _residues[u];
                            if (t.Plus != null) {
                                comp = AddVec(comp, t.Plus);
                            }
                            if (t.Minus != null) {
                                comp = AddVec(comp, t.Minus, -1);
                            }
                            raw.Add(new RawMember { Comp = comp, Key = unit.Name, IsLoss = isLoss });
                        }
                    }
                }
                else if (t.ScaffoldKey != null) {
                    var entry = _rule.Scaffold!.Entries[h.EntryIndex];
                    var mode = _rule.Mode == "pos" ? RuleIonMode.Positive : RuleIonMode.Negative;
                    foreach (var comp in entry.Ions[t.ScaffoldKey].Comps(mode)) {
                        raw.Add(new RawMember { Comp = Vec(comp), Key = Composition.Format(comp, _tables.Elements), IsLoss = false });
                    }
                }
            }
            else if (t.Kind == "aglycone_ion") {
                raw.Add(new RawMember { Comp = Ionise(h.Agly, t, h), Key = "A", IsLoss = false });
            }
            else if (t.Kind == "ion_series") {
                var classes = new HashSet<string>(StringComparer.Ordinal);
                foreach (var x in (JsonArray)t.Obj["unit_classes"]) {
                    classes.Add(((JsonString)x).Value);
                }
                var selected = new List<int>();
                for (var u = 0; u < _tables.Units.Count; u++) {
                    if (h.Counts[u] != 0 && classes.Contains(_tables.Units[u].ClassText)) {
                        selected.Add(u);
                    }
                }
                var sub = new long[selected.Count];
                SubsetOdometer(0, sub, () => {
                    var any = false;
                    foreach (var k in sub) {
                        any |= k != 0;
                    }
                    if (!any) {
                        return;
                    }
                    var sCounts = new long[h.Counts.Length];
                    for (var i = 0; i < selected.Count; i++) {
                        sCounts[selected[i]] = sub[i];
                    }
                    if (sCounts.SequenceEqual(h.Counts)) {
                        return;
                    }
                    var b = h.Agly;
                    for (var u = 0; u < sCounts.Length; u++) {
                        if (sCounts[u] != 0) {
                            b = AddVec(b, _residues[u], sCounts[u]);
                        }
                    }
                    raw.Add(new RawMember { Comp = Ionise(b, t, h), Key = UnitsKey(sCounts), IsLoss = false });
                }, selected, h.Counts);
            }
            var output = new List<Member<T>>(raw.Count);
            foreach (var m in raw) {
                var mass = Mass(m.Comp);
                var target = m.IsLoss ? M.Sub(h.PrecursorPda, mass) : M.Sub(mass, M.Mul(M.FromLong(z), _electron));
                output.Add(new Member<T>(target, m.Key, m.Comp, mass, m.IsLoss));
            }
            // Stable sort by (target, key), as Python's sorted.
            var sorted = output.Select((m, i) => new KeyValuePair<int, Member<T>>(i, m)).ToList();
            sorted.Sort((x, y) => {
                var c = M.Compare(x.Value.Target, y.Value.Target);
                if (c != 0) {
                    return c;
                }
                c = CanonicalJson.CodePointOrder.Compare(x.Value.Key, y.Value.Key);
                return c != 0 ? c : x.Key.CompareTo(y.Key);
            });
            var collapsed = new List<Member<T>>();
            foreach (var kv in sorted) {
                var m = kv.Value;
                if (collapsed.Count > 0 && M.Compare(collapsed[collapsed.Count - 1].Target, m.Target) == 0) {
                    var prev = collapsed[collapsed.Count - 1];
                    collapsed[collapsed.Count - 1] = new Member<T>(prev.Target, prev.Key + "+" + m.Key, prev.Comp, prev.Mass, prev.IsLoss);
                }
                else {
                    collapsed.Add(m);
                }
            }
            return collapsed;
        }

        private static void SubsetOdometer(int i, long[] sub, Action leaf, List<int> selected, long[] counts) {
            if (i == sub.Length) {
                leaf();
                return;
            }
            var n = counts[selected[i]];
            for (long k = 0; k <= n; k++) {
                sub[i] = k;
                SubsetOdometer(i + 1, sub, leaf, selected, counts);
            }
            sub[i] = 0;
        }
    }

    /// <summary>Counts compositions without enumerating them (the reference's <c>count_series</c>).</summary>
    internal static class SeriesCounter
    {
        public static BigInteger Count(RuleInfo rule, LoadedTables tables) {
            var byClass = new List<KeyValuePair<string, List<long[]>>>();
            for (var u = 0; u < tables.Units.Count; u++) {
                var cls = tables.Units[u].ClassText;
                var group = byClass.FirstOrDefault(kv => kv.Key == cls).Value;
                if (group is null) {
                    group = new List<long[]>();
                    byClass.Add(new KeyValuePair<string, List<long[]>>(cls, group));
                }
                group.Add(rule.UnitRanges[u]);
            }
            var total = BigInteger.One;
            foreach (var kv in byClass) {
                long[]? totals = null;
                foreach (var t in rule.Totals) {
                    if (t.Key == kv.Key) {
                        totals = t.Value;
                    }
                }
                if (totals is null) {
                    // Every sum lies in [0, max]: the count is the product of the range sizes.
                    foreach (var r in kv.Value) {
                        total *= new BigInteger(r[1]) - r[0] + 1;
                    }
                    continue;
                }
                // Sums above the class maximum never count, so they are dropped as they arise.
                var ways = new Dictionary<long, BigInteger> { [0] = BigInteger.One };
                foreach (var r in kv.Value) {
                    var next = new Dictionary<long, BigInteger>();
                    foreach (var w in ways) {
                        for (var n = r[0]; n <= r[1] && w.Key + n <= totals[1]; n++) {
                            next.TryGetValue(w.Key + n, out var current);
                            next[w.Key + n] = current + w.Value;
                        }
                    }
                    ways = next;
                }
                var sum = BigInteger.Zero;
                foreach (var w in ways) {
                    if (w.Key >= totals[0] && w.Key <= totals[1]) {
                        sum += w.Value;
                    }
                }
                total *= sum;
            }
            foreach (var t in rule.Totals) {
                if (!byClass.Any(kv => kv.Key == t.Key) && t.Value[0] > 0) {
                    return BigInteger.Zero;
                }
            }
            var nEntries = rule.Scaffold != null ? rule.Scaffold.Entries.Count : 1;
            return total * nEntries;
        }
    }
}
