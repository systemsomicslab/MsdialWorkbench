using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Loading
{
    /// <summary>
    /// Load-time checks 4-7 of design 3.6: series size, counts and masses, complementary members, precursor targets
    /// and the two-ion claim (both passes). Mirrors <c>check_series</c>, <c>two_ion_passes</c> and <c>eval_logic</c>
    /// of the Python reference.
    /// </summary>
    internal static class TwoIonCheck
    {
        public const long MaxCompositions = 100000;

        public static void CheckSeries(ErrorSink c, RuleInfo rule, LoadedTables tables) {
            var n = SeriesCounter.Count(rule, tables);
            const string pc = "/preconditions/composition";
            if (n > MaxCompositions) {
                c.Err(RuleErrorCodes.SeriesTooLarge, pc);
                return;
            }
            if (n.IsZero) {
                c.Err(RuleErrorCodes.SeriesEmpty, pc);
                return;
            }
            var found = new List<KeyValuePair<string, string>>();
            try {
                Run<long, LongPda>(rule, tables, found);
            }
            catch (OverflowException) {
                // A count or mass beyond 64 bits: redo the rule exactly, as the reference's unbounded integers do.
                found.Clear();
                Run<BigInteger, BigPda>(rule, tables, found);
            }
            foreach (var e in found) {
                c.Err(e.Key, e.Value);
            }
        }

        private static void Run<T, TM>(RuleInfo rule, LoadedTables tables, List<KeyValuePair<string, string>> found) where TM : struct, IPdaMath<T> {
            var m = default(TM);
            var limit = m.FromLong(MassPda.Limit);
            var region = m.FromLong(MassPda.PrecursorRegion);
            var series = new SeriesBuilder<T, TM>(rule, tables);
            const string pc = "/preconditions/composition";
            var support = new List<TermInfo>();
            var targetsChecked = new List<TermInfo>();
            foreach (var t in rule.Terms) {
                if (t.IsSupport) {
                    support.Add(t);
                }
                if (t.IsSupport || t.Role == "upgrade") {
                    targetsChecked.Add(t);
                }
            }
            var groupOf = new Dictionary<int, int>();
            for (var gi = 0; gi < rule.Groups.Count; gi++) {
                foreach (var ti in rule.Groups[gi]) {
                    groupOf[ti] = gi;
                }
            }
            var claimSet = new HashSet<int>(rule.ClaimTerms);
            var measure = rule.Measure;
            var flagged = new HashSet<string>(StringComparer.Ordinal);
            void Flag(string code, string ptr) {
                if (flagged.Add(code + "\n" + ptr)) {
                    found.Add(new KeyValuePair<string, string>(code, ptr));
                }
            }
            bool InRange(T x) => m.Compare(x, m.Zero) > 0 && m.Compare(x, limit) < 0;

            var signatureKeys = new HashSet<string>(StringComparer.Ordinal);
            var signatures = new List<KeyValuePair<bool[][], long[]>>();
            series.ForEachHypothesis(h => {
                if (SeriesBuilder<T, TM>.IsNegative(h.PrecursorComp)) {
                    Flag(RuleErrorCodes.NegativeCount, pc);
                    return;
                }
                if (!InRange(h.NeutralPda) || !InRange(h.PrecursorPda)) {
                    Flag(RuleErrorCodes.MassRange, pc);
                    return;
                }
                var members = new Dictionary<int, List<Member<T>>>();
                var bad = false;
                foreach (var t in rule.Terms) {
                    var ms = series.TermMembers(t, h);
                    foreach (var member in ms) {
                        if (SeriesBuilder<T, TM>.IsNegative(member.Comp)) {
                            Flag(RuleErrorCodes.NegativeCount, Pointers.Child("/terms", t.Index));
                            bad = true;
                        }
                        else if (!InRange(member.Mass) || (!member.IsLoss && !InRange(member.Target))) {
                            Flag(RuleErrorCodes.MassRange, Pointers.Child("/terms", t.Index));
                            bad = true;
                        }
                    }
                    members[t.Index] = ms;
                }
                if (bad) {
                    return;
                }
                foreach (var t in targetsChecked) {
                    foreach (var member in members[t.Index]) {
                        if (m.Compare(member.Target, h.PrecursorPda) == 0) {
                            Flag(RuleErrorCodes.SupportTargetIsPrecursor, Pointers.Child("/terms", t.Index));
                            break;
                        }
                    }
                }
                var multi = false;
                foreach (var ti in rule.PairIndexOrder) {
                    if (members[ti].Count > 1) {
                        multi = true;
                        foreach (var i in rule.PairIndex[ti]) {
                            Flag(RuleErrorCodes.ComplementaryMultiMember, Pointers.Child("/complementary", i));
                        }
                    }
                }
                if (multi || !measure) {
                    return;
                }
                var zero = new bool[support.Count][];
                var key = new StringBuilder();
                for (var s = 0; s < support.Count; s++) {
                    var ms = members[support[s].Index];
                    zero[s] = new bool[ms.Count];
                    for (var j = 0; j < ms.Count; j++) {
                        var d = m.Sub(ms[j].Target, h.PrecursorPda);
                        zero[s][j] = m.Compare(d, region) <= 0 && m.Compare(m.Sub(m.Zero, d), region) <= 0;
                        key.Append(zero[s][j] ? '1' : '0');
                    }
                    key.Append(';');
                }
                var kvals = KValues(rule, tables, h.Counts);
                key.Append('|');
                foreach (var k in kvals) {
                    key.Append(k.ToString(CultureInfo.InvariantCulture));
                    key.Append(',');
                }
                if (signatureKeys.Add(key.ToString())) {
                    signatures.Add(new KeyValuePair<bool[][], long[]>(zero, kvals));
                }
            });
            if (!measure) {
                return;
            }
            foreach (var sig in signatures) {
                foreach (var e in Passes(rule, support, sig.Key, sig.Value, groupOf, claimSet)) {
                    Flag(e.Key, e.Value);
                }
            }
        }

        /// <summary>The k of every k_of node at a hypothesis (the reference's hypothesis_vars and k_value).</summary>
        public static long[] KValues(RuleInfo rule, LoadedTables tables, long[] counts) {
            long nUnits = 0, nSugars = 0, nAcyl = 0, nHex = 0, nDhex = 0, nPen = 0, nHexa = 0;
            for (var u = 0; u < counts.Length; u++) {
                var n = counts[u];
                nUnits += n;
                var unit = tables.Units[u];
                if (unit.ClassText == "sugar") {
                    nSugars += n;
                }
                else if (unit.ClassText == "acyl") {
                    nAcyl += n;
                }
                switch (unit.Name) {
                    case "Hex": nHex += n; break;
                    case "dHex": nDhex += n; break;
                    case "Pen": nPen += n; break;
                    case "HexA": nHexa += n; break;
                }
            }
            var kvals = new long[rule.KNodes.Count];
            for (var i = 0; i < kvals.Length; i++) {
                var k = rule.KNodes[i].Value;
                if (k is JsonInteger ki) {
                    kvals[i] = ki.Value;
                    continue;
                }
                var o = (JsonObject)k;
                long value;
                switch (((JsonString)o["var"]).Value) {
                    case "n_units": value = nUnits; break;
                    case "n_sugars": value = nSugars; break;
                    case "n_hex": value = nHex; break;
                    case "n_dhex": value = nDhex; break;
                    case "n_pen": value = nPen; break;
                    case "n_hexa": value = nHexa; break;
                    default: value = nAcyl; break;
                }
                kvals[i] = Math.Max(((JsonInteger)o["min"]).Value, value + ((JsonInteger)o["offset"]).Value);
            }
            return kvals;
        }

        /// <summary>Both passes for one hypothesis signature (design 3.6 item 6).</summary>
        private static List<KeyValuePair<string, string>> Passes(RuleInfo rule, List<TermInfo> support, bool[][] zero,
                                                                 long[] kvals, Dictionary<int, int> groupOf, HashSet<int> claimSet) {
            var unitOf = new int[support.Count][];
            var claimUnits = new SortedSet<int>();
            var nUnits = 0;
            var groupUnit = new Dictionary<int, int>();
            for (var s = 0; s < support.Count; s++) {
                var t = support[s];
                unitOf[s] = new int[zero[s].Length];
                for (var mi = 0; mi < zero[s].Length; mi++) {
                    int uid;
                    if (groupOf.TryGetValue(t.Index, out var gi)) {
                        if (!groupUnit.TryGetValue(gi, out uid)) {
                            uid = nUnits++;
                            groupUnit[gi] = uid;
                        }
                    }
                    else {
                        uid = nUnits++;
                    }
                    unitOf[s][mi] = uid;
                    if (claimSet.Contains(t.Index)) {
                        claimUnits.Add(uid);
                    }
                }
            }

            bool Holds(HashSet<int> present) {
                var map = new Dictionary<int, HashSet<int>>();
                for (var s = 0; s < support.Count; s++) {
                    var set = new HashSet<int>();
                    for (var mi = 0; mi < zero[s].Length; mi++) {
                        if (!zero[s][mi] && present.Contains(unitOf[s][mi])) {
                            set.Add(unitOf[s][mi]);
                        }
                    }
                    map[support[s].Index] = set;
                }
                return Eval(rule.Logic, map, kvals);
            }

            var errors = new List<KeyValuePair<string, string>>();
            var pass1 = Holds(new HashSet<int>());
            for (var u = 0; u < nUnits && !pass1; u++) {
                pass1 = Holds(new HashSet<int> { u });
            }
            if (pass1) {
                errors.Add(new KeyValuePair<string, string>(RuleErrorCodes.SingleIonClaim, "/logic"));
            }
            var baseUnits = new HashSet<int>();
            for (var u = 0; u < nUnits; u++) {
                if (!claimUnits.Contains(u)) {
                    baseUnits.Add(u);
                }
            }
            var pass2 = Holds(baseUnits);
            foreach (var u in claimUnits) {
                if (pass2) {
                    break;
                }
                pass2 = Holds(new HashSet<int>(baseUnits) { u });
            }
            if (pass2) {
                errors.Add(new KeyValuePair<string, string>(RuleErrorCodes.ClaimSingleIon, "/claim/claim_term_ids"));
            }
            return errors;
        }

        /// <summary>The two-ion setting: vetoes absent (every not is true), ordering comparisons satisfied, k at the
        /// hypothesis' value with no offset; a k_of counts evidence units of its term children plus true subtrees.</summary>
        private static bool Eval(CompiledNode node, Dictionary<int, HashSet<int>> present, long[] kvals) {
            switch (node.Kind) {
                case "term":
                    return present.TryGetValue(node.TermIndex, out var termUnits) && termUnits.Count > 0;
                case "all": {
                    var result = true;
                    foreach (var ch in node.Children) {
                        result &= Eval(ch, present, kvals);
                    }
                    return result;
                }
                case "any": {
                    var result = false;
                    foreach (var ch in node.Children) {
                        result |= Eval(ch, present, kvals);
                    }
                    return result;
                }
                case "not":
                    return true;
                default: {
                    var units = new HashSet<int>();
                    var extra = 0L;
                    foreach (var ch in node.Children) {
                        if (ch.Kind == "term") {
                            if (present.TryGetValue(ch.TermIndex, out var s)) {
                                units.UnionWith(s);
                            }
                        }
                        else if (Eval(ch, present, kvals)) {
                            extra++;
                        }
                    }
                    return units.Count + extra >= Math.Max(1L, kvals[node.Slot]);
                }
            }
        }
    }
}
