#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using NCDK.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The independent mass oracle of the per-rule fixtures (design 7.2): NCDK's BODR major-isotope masses, summed as
/// doubles in symbol order, with the P3 electron 0.000549. Peaks are placed at these m/z values, never at the
/// engine's own pDa arithmetic. The oracle agrees with the embedded element table (tables/elements.json) within 1e-7
/// Da per formula of the shipped set, far inside the 2e-6 Da fixture margin.
/// </summary>
internal static class NcdkOracle
{
    public const double Electron = 0.000549;

    private static readonly ConcurrentDictionary<string, double> Elements = new ConcurrentDictionary<string, double>(StringComparer.Ordinal);

    public static double Element(string symbol) =>
        Elements.GetOrAdd(symbol, s => BODRIsotopeFactory.Instance.GetMajorIsotope(s).ExactMass!.Value);

    public static double Mass(IReadOnlyDictionary<string, long> counts) {
        var mass = 0.0;
        foreach (var kv in counts.Where(kv => kv.Value != 0).OrderBy(kv => kv.Key, StringComparer.Ordinal)) {
            mass += kv.Value * Element(kv.Key);
        }
        return mass;
    }

    public static double Mass(string formula) => Mass(Formula.Parse(formula));

    /// <summary>The m/z of an ion of charge z.</summary>
    public static double Ion(IReadOnlyDictionary<string, long> counts, int z) => Mass(counts) - z * Electron;
}

/// <summary>Element-count dictionaries for the naive model: parse, add, Hill text and the test-side exact pDa.</summary>
internal static class Formula
{
    public static Dictionary<string, long> Parse(string? formula) =>
        formula is null ? new Dictionary<string, long>(StringComparer.Ordinal) : NaiveEnumerator.Parse(formula);

    public static Dictionary<string, long> Add(IReadOnlyDictionary<string, long> a, IReadOnlyDictionary<string, long> b, long times = 1) {
        var r = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var kv in a) {
            r[kv.Key] = kv.Value;
        }
        foreach (var kv in b) {
            r[kv.Key] = (r.TryGetValue(kv.Key, out var n) ? n : 0) + kv.Value * times;
        }
        return r;
    }

    public static Dictionary<string, long> Add(IReadOnlyDictionary<string, long> a, string? formula, long times = 1) =>
        formula is null ? Add(a, new Dictionary<string, long>(), 0) : Add(a, Parse(formula), times);

    public static string Hill(IReadOnlyDictionary<string, long> counts) => NaiveEnumerator.Hill(counts.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal));

    /// <summary>The exact mass in pDa from the test-side literals (<see cref="TestMass"/>): member order and collapse
    /// keys only; m/z values come from <see cref="NcdkOracle"/>.</summary>
    public static long Pda(IReadOnlyDictionary<string, long> counts) {
        long total = 0;
        foreach (var kv in counts.Where(kv => kv.Value != 0)) {
            total += kv.Value * TestMass.Of(kv.Key);
        }
        return total;
    }
}

/// <summary>A unit row of the naive model.</summary>
internal sealed class NaiveUnit
{
    public NaiveUnit(string name, string cls, string residue) {
        Name = name;
        Class = cls;
        Residue = Formula.Parse(residue);
    }

    public string Name { get; }
    public string Class { get; }
    public Dictionary<string, long> Residue { get; }
}

/// <summary>A tried adduct of the naive model: the rule's adducts in rule order, then the common adducts of the
/// polarity not already listed, in table order (design 4.3).</summary>
internal sealed class NaiveAdduct
{
    public NaiveAdduct(string name, int charge, int multiplier, string? add, string? remove, bool fromRule) {
        Name = name;
        Charge = charge;
        Multiplier = multiplier;
        Add = add;
        Remove = remove;
        FromRule = fromRule;
    }

    public string Name { get; }
    public int Charge { get; }
    public int Multiplier { get; }
    public string? Add { get; }
    public string? Remove { get; }
    public bool FromRule { get; }
}

/// <summary>A hypothesis of the naive model: composition, adduct, the oracle m/z and the exact pDa.</summary>
internal sealed class NaiveHypothesis
{
    public NaiveHypothesis(string id, int entry, long[] counts, Dictionary<string, long> agly, Dictionary<string, long> neutral, NaiveAdduct adduct,
                           double mz, long pda) {
        Id = id;
        Entry = entry;
        Counts = counts;
        Agly = agly;
        Neutral = neutral;
        Adduct = adduct;
        Mz = mz;
        Pda = pda;
    }

    public string Id { get; }
    public int Entry { get; }
    public long[] Counts { get; }
    public Dictionary<string, long> Agly { get; }
    public Dictionary<string, long> Neutral { get; }
    public NaiveAdduct Adduct { get; }
    public double Mz { get; }
    public long Pda { get; }
}

/// <summary>A member of a term in one hypothesis of the naive model.</summary>
internal sealed class NaiveMember
{
    public NaiveMember(string key, double mz, long pda, bool isLoss, double reference) {
        Key = key;
        Mz = mz;
        Pda = pda;
        IsLoss = isLoss;
        Reference = reference;
    }

    public string Key { get; }
    public double Mz { get; }
    public long Pda { get; }
    public bool IsLoss { get; }
    public double Reference { get; }
}

/// <summary>A peak of a constructed spectrum: integer micro-Da and an integer intensity, so its text is exact.</summary>
internal readonly struct BuiltPeak
{
    public BuiltPeak(long micro, long intensity) {
        Micro = micro;
        Intensity = intensity;
    }

    public long Micro { get; }
    public long Intensity { get; }
    public double Mz => Micro / 1e6;
}

/// <summary>The naive evaluation of one hypothesis.</summary>
internal sealed class NaiveEvaluation
{
    public NaiveHypothesis Hypothesis = null!;
    public bool Fired;
    public bool FiredIgnoringVetoes;
    public double Score;
    public List<string> Matched = new List<string>();
    /// <summary>Per upgrade member, its first candidate (present, outside the precursor region, in the order of the
    /// support candidates; sharing with support peaks allowed), as term_id:member_key@peak_index.</summary>
    public List<string> Upgrades = new List<string>();
}

/// <summary>The naive outcome of one rule on one spectrum: the constructive expectation.</summary>
internal sealed class NaiveOutcome
{
    public string? PreconditionFailure;
    public int NHypotheses;
    public bool Fired;
    public string? Chosen;
    public List<string>? Matched;
    public List<string>? Upgrades;
    public List<string> Tied = new List<string>();
}

/// <summary>
/// A naive model of one shipped rule, written from the design (sections 4.2 to 4.9) without the engine: it reads the
/// rule and table JSON, enumerates every hypothesis with the oracle masses, builds every term's members and targets,
/// and evaluates a spectrum by brute force (hits, floors, the precursor region, one peak one term with a search over
/// the conflicted items, the logic, the score and the selection). It covers what the shipped set uses: the p2
/// convention and product tolerance, and windows as vetoes.
/// </summary>
internal sealed class NaiveRule
{
    private const double RegionDa = 1.0;
    private const double Slack = 1e-10;

    public NaiveRule(JsonObject rule, JsonObject units, JsonObject adducts, JsonObject? scaffolds) {
        Doc = rule;
        RuleId = Str(rule, "rule_id");
        var pre = (JsonObject)rule["preconditions"];
        Mode = Str(pre, "ion_mode");
        Z = Mode == "pos" ? 1 : -1;
        PrecursorTolerance = ((JsonFloat)pre["precursor_tolerance_da"]).Value;
        LossReference = Str(rule, "loss_reference");
        if (Str(rule, "convention") != "p2" || Str(rule, "product_tolerance") != "p2") {
            throw new NotSupportedException(RuleId + ": the naive model covers the p2 convention and tolerance only");
        }
        Units = ((JsonArray)units["units"]).Cast<JsonObject>().Select(u => new NaiveUnit(Str(u, "name"), Str(u, "class"), Str(u, "residue"))).ToList();
        var rows = ((JsonArray)adducts["adducts"]).Cast<JsonObject>().ToList();
        var listed = ((JsonArray)pre["adducts"]).Cast<JsonString>().Select(s => s.Value).ToList();
        NaiveAdduct Row(JsonObject a, bool fromRule) => new NaiveAdduct(Str(a, "adduct"), (int)Int(a, "charge"), (int)Int(a, "multiplier"),
                                                                         StrOrNull(a, "add"), StrOrNull(a, "remove"), fromRule);
        Tried = listed.Select(n => Row(rows.Single(a => Str(a, "adduct") == n), true)).ToList();
        Tried.AddRange(rows.Where(a => ((JsonBoolean)a["common"]).Value && (int)Int(a, "charge") == Z && !listed.Contains(Str(a, "adduct")))
                           .Select(a => Row(a, false)));
        Terms = ((JsonArray)rule["terms"]).Cast<JsonObject>().ToList();
        TermIndex = Terms.Select((t, i) => (Str(t, "term_id"), i)).ToDictionary(x => x.Item1, x => x.i, StringComparer.Ordinal);
        Group = Enumerable.Repeat(-1, Terms.Count).ToArray();
        var pairs = ((JsonArray)rule["complementary"]).Cast<JsonArray>().Select(p => (TermIndex[((JsonString)p[0]).Value], TermIndex[((JsonString)p[1]).Value])).ToList();
        var parent = Enumerable.Range(0, Terms.Count).ToArray();
        int Find(int x) => parent[x] == x ? x : parent[x] = Find(parent[x]);
        foreach (var (a, b) in pairs) {
            parent[Find(a)] = Find(b);
        }
        foreach (var (a, b) in pairs) {
            Group[a] = Find(a);
            Group[b] = Find(b);
        }
        var composition = (JsonObject)pre["composition"];
        var core = Formula.Parse(StrOrNull(composition, "core"));
        Entries = new List<(string, Dictionary<string, long>, JsonObject?)>();
        if (scaffolds != null) {
            foreach (var e in ((JsonArray)scaffolds["entries"]).Cast<JsonObject>()) {
                Entries.Add((Str(e, "scaffold_id"), Formula.Add(core, StrOrNull(e, "formula")), e));
            }
        }
        else {
            Entries.Add(("core", core, null));
        }
        var ranges = (JsonObject)composition["units"];
        var totals = (JsonObject)composition["unit_totals"];
        var lo = Units.Select(u => ranges.TryGetValue(u.Name, out var r) ? ((JsonInteger)((JsonArray)r)[0]).Value : 0L).ToArray();
        var hi = Units.Select(u => ranges.TryGetValue(u.Name, out var r) ? ((JsonInteger)((JsonArray)r)[1]).Value : 0L).ToArray();
        var vectors = new List<long[]> { new long[0] };
        for (var u = 0; u < Units.Count; u++) {
            var next = new List<long[]>();
            foreach (var v in vectors) {
                for (var c = lo[u]; c <= hi[u]; c++) {
                    next.Add(v.Concat(new[] { c }).ToArray());
                }
            }
            vectors = next;
        }
        vectors = vectors.Where(v => totals.Members.All(t => {
            var sum = Units.Select((u, i) => u.Class == t.Key ? v[i] : 0L).Sum();
            var range = (JsonArray)t.Value;
            return sum >= ((JsonInteger)range[0]).Value && sum <= ((JsonInteger)range[1]).Value;
        })).ToList();
        All = new List<NaiveHypothesis>();
        for (var e = 0; e < Entries.Count; e++) {
            foreach (var v in vectors) {
                var neutral = Entries[e].Agly;
                for (var u = 0; u < Units.Count; u++) {
                    neutral = Formula.Add(neutral, Units[u].Residue, v[u]);
                }
                var key = UnitsKey(v);
                foreach (var a in Tried) {
                    var ion = Formula.Add(Formula.Add(MultiplyCounts(neutral, a.Multiplier), a.Add), a.Remove, -1);
                    var mz = NcdkOracle.Ion(ion, a.Charge);
                    var pda = Formula.Pda(ion) - a.Charge * TestMass.Electron;
                    All.Add(new NaiveHypothesis(Entries[e].Id + "|" + key + "|" + a.Name, e, v, Entries[e].Agly, neutral, a, mz, pda));
                }
            }
        }
        All.Sort((x, y) => string.CompareOrdinal(x.Id, y.Id));
    }

    public JsonObject Doc { get; }
    public string RuleId { get; }
    public string Mode { get; }
    public int Z { get; }
    public double PrecursorTolerance { get; }
    public string LossReference { get; }
    public List<NaiveUnit> Units { get; }
    public List<NaiveAdduct> Tried { get; }
    public List<JsonObject> Terms { get; }
    public Dictionary<string, int> TermIndex { get; }
    /// <summary>The complementary group (a representative term index) of each term, or -1.</summary>
    public int[] Group { get; }
    public List<(string Id, Dictionary<string, long> Agly, JsonObject? Entry)> Entries { get; }
    /// <summary>Every hypothesis, ordered by hypothesis_id ordinally.</summary>
    public List<NaiveHypothesis> All { get; }

    public static string Str(JsonObject o, string key) => ((JsonString)o[key]).Value;

    public static string? StrOrNull(JsonObject o, string key) => o.TryGetValue(key, out var v) && v is JsonString s ? s.Value : null;

    private static long Int(JsonObject o, string key) => ((JsonInteger)o[key]).Value;

    private static Dictionary<string, long> MultiplyCounts(Dictionary<string, long> c, int m) => c.ToDictionary(kv => kv.Key, kv => kv.Value * m, StringComparer.Ordinal);

    public string Role(int t) => Str(Terms[t], "role");

    public string TermId(int t) => Str(Terms[t], "term_id");

    public bool IsSupport(int t) => Role(t) is "required" or "counted" or "ordering";

    public bool IsVeto(int t) => Role(t) is "veto_present" or "veto_stronger_than";

    public string UnitsKey(long[] counts) =>
        string.Join(",", Units.Select((u, i) => counts[i] == 0 ? null : u.Name + counts[i].ToString(CultureInfo.InvariantCulture)).Where(s => s != null));

    /// <summary>Hypotheses whose precursor passes |P - mz| &lt;= tol + 1e-10, by id.</summary>
    public List<NaiveHypothesis> Matching(double precursor) => All.Where(h => Math.Abs(precursor - h.Mz) <= PrecursorTolerance + Slack).ToList();

    // ------------------------------------------------------------------------------------------------ members

    private Dictionary<string, long> Ionise(Dictionary<string, long> b, JsonObject t, NaiveHypothesis h) {
        var hydrogen = Formula.Parse("H");
        Dictionary<string, long> ion;
        if (Str(t, "charge_carrier") == "adduct") {
            ion = Formula.Add(Formula.Add(b, h.Adduct.Add), h.Adduct.Remove, -1);
        }
        else if (Str(t, "form") == "even") {
            ion = Formula.Add(b, hydrogen, h.Adduct.Charge);
        }
        else {
            ion = h.Adduct.Charge == 1 ? Formula.Add(b, hydrogen, 0) : Formula.Add(b, hydrogen, -2);
        }
        return Formula.Add(Formula.Add(ion, StrOrNull(t, "plus")), StrOrNull(t, "minus"), -1);
    }

    /// <summary>The members of term <paramref name="t"/> in <paramref name="h"/> (design 4.5): ordered by target pDa
    /// (the hypothesis as loss reference), then key; equal targets collapsed with keys joined by "+". The m/z follows
    /// the loss reference with the observed precursor <paramref name="precursor"/>; <paramref name="decoy"/> Da is
    /// added to every term mass.</summary>
    public List<NaiveMember> Members(int t, NaiveHypothesis h, double precursor, int decoy) {
        var term = Terms[t];
        var kind = Str(term, "kind");
        var z = h.Adduct.Charge;
        var raw = new List<(Dictionary<string, long> Comp, string Key, bool IsLoss)>();
        if (kind is "product_ion" or "loss_from_precursor" or "window") {
            var isLoss = kind == "loss_from_precursor" || (kind == "window" && Str(term, "as") == "loss");
            if (StrOrNull(term, "formula") is string f) {
                raw.Add((Formula.Parse(f), f, isLoss));
            }
            else if (term.TryGetValue("unit", out var sel) && sel is JsonObject unit) {
                var byClass = unit.ContainsKey("class");
                var value = Str(unit, byClass ? "class" : "name");
                for (var u = 0; u < Units.Count; u++) {
                    if (h.Counts[u] != 0 && (byClass ? Units[u].Class : Units[u].Name) == value) {
                        raw.Add((Formula.Add(Formula.Add(Units[u].Residue, StrOrNull(unit, "plus")), StrOrNull(unit, "minus"), -1), Units[u].Name, isLoss));
                    }
                }
            }
            else if (StrOrNull(term, "scaffold_ions") is string ionKey) {
                var ions = (JsonObject)((JsonObject)Entries[h.Entry].Entry!["ions"])[ionKey];
                foreach (var ion in ((JsonArray)ions[Mode]).Cast<JsonString>()) {
                    var comp = Formula.Parse(ion.Value);
                    raw.Add((comp, Formula.Hill(comp), false));
                }
            }
        }
        else if (kind == "aglycone_ion") {
            raw.Add((Ionise(h.Agly, term, h), "A", false));
        }
        else if (kind == "ion_series") {
            var classes = ((JsonArray)term["unit_classes"]).Cast<JsonString>().Select(s => s.Value).ToList();
            var selected = Enumerable.Range(0, Units.Count).Where(u => h.Counts[u] != 0 && classes.Contains(Units[u].Class)).ToList();
            var subsets = new List<long[]> { new long[Units.Count] };
            foreach (var u in selected) {
                subsets = subsets.SelectMany(s => Enumerable.Range(0, (int)h.Counts[u] + 1).Select(k => {
                    var copy = (long[])s.Clone();
                    copy[u] = k;
                    return copy;
                })).ToList();
            }
            foreach (var s in subsets) {
                if (s.All(k => k == 0) || s.SequenceEqual(h.Counts)) {
                    continue;
                }
                var b = h.Agly;
                for (var u = 0; u < Units.Count; u++) {
                    b = Formula.Add(b, Units[u].Residue, s[u]);
                }
                raw.Add((Ionise(b, term, h), UnitsKey(s), false));
            }
        }
        else {
            throw new NotSupportedException(RuleId + ": kind " + kind);
        }
        var members = new List<(long Pda, string Key, NaiveMember Member)>();
        foreach (var (comp, key, isLoss) in raw) {
            var mass = NcdkOracle.Mass(comp) + decoy;
            var massPda = Formula.Pda(comp) + decoy * 1000000000000L;
            if (isLoss) {
                var reference = LossReference == "hypothesis" ? h.Mz : precursor;
                members.Add((h.Pda - massPda, key, new NaiveMember(key, reference - mass, h.Pda - massPda, true, reference)));
            }
            else {
                members.Add((massPda - z * TestMass.Electron, key, new NaiveMember(key, mass - z * NcdkOracle.Electron, massPda - z * TestMass.Electron, false, double.NaN)));
            }
        }
        var sorted = members.Select((m, i) => (m, i)).OrderBy(x => x.m.Pda).ThenBy(x => x.m.Key, StringComparer.Ordinal).ThenBy(x => x.i).Select(x => x.m.Member).ToList();
        var collapsed = new List<NaiveMember>();
        foreach (var m in sorted) {
            if (collapsed.Count > 0 && collapsed[collapsed.Count - 1].Pda == m.Pda) {
                var prev = collapsed[collapsed.Count - 1];
                collapsed[collapsed.Count - 1] = new NaiveMember(prev.Key + "+" + m.Key, prev.Mz, prev.Pda, prev.IsLoss, prev.Reference);
            }
            else {
                collapsed.Add(m);
            }
        }
        return collapsed;
    }

    public static double Tolerance(double target) => Math.Max(0.005, target * 10.0 / 1000000.0);

    public double HalfWidth(int t) => Terms[t].TryGetValue("half_width_da", out var v) ? ((JsonFloat)v).Value : double.NaN;

    // ------------------------------------------------------------------------------------------------ evaluation

    private sealed class Hit
    {
        public int Peak;
        public double Rel;
        public bool Present;
        public bool InRegion;
        public double Err;
    }

    private sealed class Item
    {
        public int Term;
        public int Member;
        public int Unit;
        public List<Hit> Candidates = new List<Hit>();
    }

    /// <summary>The value of a k_of k at a hypothesis: an integer, or max(min, var + offset).</summary>
    public long KValue(JsonValue k, NaiveHypothesis h) {
        if (k is JsonInteger fixedK) {
            return fixedK.Value;
        }
        var o = (JsonObject)k;
        var name = Str(o, "var");
        long Sum(Func<NaiveUnit, bool> f) => Units.Select((u, i) => f(u) ? h.Counts[i] : 0L).Sum();
        var value = name switch
        {
            "n_units" => Sum(u => true),
            "n_sugars" => Sum(u => u.Class == "sugar"),
            "n_acyl" => Sum(u => u.Class == "acyl"),
            "n_hex" => Sum(u => u.Name == "Hex"),
            "n_dhex" => Sum(u => u.Name == "dHex"),
            "n_pen" => Sum(u => u.Name == "Pen"),
            "n_hexa" => Sum(u => u.Name == "HexA"),
            _ => throw new NotSupportedException("variable " + name),
        };
        return Math.Max(((JsonInteger)o["min"]).Value, value + ((JsonInteger)o["offset"]).Value);
    }

    /// <summary>Evaluates one hypothesis on a spectrum (design 4.4 to 4.7).</summary>
    public NaiveEvaluation Evaluate(NaiveHypothesis h, IReadOnlyList<BuiltPeak> peaks, double precursor, int decoy) {
        var outside = peaks.Where(p => Math.Abs(p.Mz - precursor) > RegionDa).Select(p => (double)p.Intensity).ToList();
        var baseI = outside.Count > 0 ? outside.Max() : double.NaN;
        var members = Enumerable.Range(0, Terms.Count).Select(t => Members(t, h, precursor, decoy)).ToList();
        var hits = new List<List<Hit>>[Terms.Count];
        var iRaw = new double[Terms.Count];
        var rawPresent = new bool[Terms.Count];
        for (var t = 0; t < Terms.Count; t++) {
            var floor = ((JsonFloat)Terms[t]["min_rel_intensity"]).Value;
            hits[t] = new List<List<Hit>>();
            foreach (var m in members[t]) {
                var list = new List<Hit>();
                var tol = Str(Terms[t], "kind") == "window" ? HalfWidth(t) : Tolerance(m.Mz);
                for (var p = 0; p < peaks.Count; p++) {
                    var mz = peaks[p].Mz;
                    var err = mz - m.Mz;
                    if (!(Math.Abs(err) <= tol + Slack)) {
                        continue;
                    }
                    if (m.IsLoss && !(m.Reference - mz >= 1.0)) {
                        continue;
                    }
                    var rel = peaks[p].Intensity / baseI;
                    list.Add(new Hit
                    {
                        Peak = p, Rel = rel, Present = !double.IsNaN(rel) && rel >= floor && rel > 0.0, Err = Math.Abs(err),
                        InRegion = Math.Abs(mz - precursor) <= RegionDa + Slack,
                    });
                }
                hits[t].Add(list);
                foreach (var x in list) {
                    if (!double.IsNaN(x.Rel)) {
                        iRaw[t] = Math.Max(iRaw[t], x.Rel);
                    }
                    rawPresent[t] |= x.Present;
                }
            }
        }
        var eval = new NaiveEvaluation { Hypothesis = h };
        // Items and evidence units in canonical order.
        var items = new List<Item>();
        var unitOfGroup = new Dictionary<int, int>();
        var nUnits = 0;
        for (var t = 0; t < Terms.Count; t++) {
            if (!IsSupport(t)) {
                continue;
            }
            for (var m = 0; m < members[t].Count; m++) {
                int unit;
                if (Group[t] >= 0) {
                    if (!unitOfGroup.TryGetValue(Group[t], out unit)) {
                        unit = unitOfGroup[Group[t]] = nUnits++;
                    }
                }
                else {
                    unit = nUnits++;
                }
                items.Add(new Item
                {
                    Term = t, Member = m, Unit = unit,
                    Candidates = hits[t][m].Where(x => x.Present && !x.InRegion).OrderByDescending(x => x.Rel).ThenBy(x => x.Err).ThenBy(x => x.Peak).ToList(),
                });
            }
        }
        if (double.IsNaN(baseI)) {
            return eval; // no_base_peak: nothing fires
        }
        bool MayShare(Item a, Item b) => a.Term != b.Term && Group[a.Term] >= 0 && Group[a.Term] == Group[b.Term];
        var conflicted = new bool[items.Count];
        for (var i = 0; i < items.Count; i++) {
            for (var j = i + 1; j < items.Count; j++) {
                if (!MayShare(items[i], items[j]) && items[i].Candidates.Any(a => items[j].Candidates.Any(b => a.Peak == b.Peak))) {
                    conflicted[i] = conflicted[j] = true;
                }
            }
        }
        var choice = items.Select(it => it.Candidates.Count > 0 ? 0 : -1).ToArray();
        (bool, bool, double, int) Key(int[] c) => (Logic(Doc["logic"], h, items, c, iRaw, rawPresent, false),
                                                   Logic(Doc["logic"], h, items, c, iRaw, rawPresent, true), Score(items, c), c.Count(x => x >= 0));
        var order = Enumerable.Range(0, items.Count).Where(i => conflicted[i]).ToList();
        if (order.Count > 0) {
            foreach (var i in order) {
                choice[i] = -1;
            }
            int[]? best = null;
            (bool, bool, double, int) bestKey = default;
            void Dfs(int k) {
                if (k == order.Count) {
                    var key = Key(choice);
                    if (best is null || Better(key, bestKey)) {
                        best = (int[])choice.Clone();
                        bestKey = key;
                    }
                    return;
                }
                var i = order[k];
                for (var c = 0; c < items[i].Candidates.Count; c++) {
                    var peak = items[i].Candidates[c].Peak;
                    var taken = false;
                    for (var j = 0; j < items.Count && !taken; j++) {
                        taken = j != i && choice[j] >= 0 && items[j].Candidates[choice[j]].Peak == peak && !MayShare(items[i], items[j]) && (!conflicted[j] || order.IndexOf(j) < k);
                    }
                    if (taken) {
                        continue;
                    }
                    choice[i] = c;
                    Dfs(k + 1);
                }
                choice[i] = -1;
                Dfs(k + 1);
            }
            Dfs(0);
            choice = best!;
        }
        eval.Fired = Logic(Doc["logic"], h, items, choice, iRaw, rawPresent, false);
        eval.FiredIgnoringVetoes = Logic(Doc["logic"], h, items, choice, iRaw, rawPresent, true);
        eval.Score = Score(items, choice);
        for (var i = 0; i < items.Count; i++) {
            if (choice[i] >= 0) {
                eval.Matched.Add(TermId(items[i].Term) + ":" + members[items[i].Term][items[i].Member].Key + "@"
                                 + items[i].Candidates[choice[i]].Peak.ToString(CultureInfo.InvariantCulture));
            }
        }
        for (var t = 0; t < Terms.Count; t++) {
            if (Role(t) != "upgrade") {
                continue;
            }
            for (var m = 0; m < members[t].Count; m++) {
                var first = hits[t][m].Where(x => x.Present && !x.InRegion).OrderByDescending(x => x.Rel).ThenBy(x => x.Err).ThenBy(x => x.Peak).FirstOrDefault();
                if (first != null) {
                    eval.Upgrades.Add(TermId(t) + ":" + members[t][m].Key + "@" + first.Peak.ToString(CultureInfo.InvariantCulture));
                }
            }
        }
        return eval;
    }

    private static bool Better((bool, bool, double, int) a, (bool, bool, double, int) b) {
        if (a.Item1 != b.Item1) {
            return a.Item1;
        }
        if (a.Item2 != b.Item2) {
            return a.Item2;
        }
        if (a.Item3 != b.Item3) {
            return a.Item3 > b.Item3;
        }
        return a.Item4 > b.Item4;
    }

    private static double Score(List<Item> items, int[] choice) {
        var byUnit = new SortedDictionary<int, double>();
        for (var i = 0; i < items.Count; i++) {
            if (choice[i] >= 0) {
                var rel = items[i].Candidates[choice[i]].Rel;
                byUnit[items[i].Unit] = byUnit.TryGetValue(items[i].Unit, out var r) ? Math.Max(r, rel) : rel;
            }
        }
        var score = 0.0;
        foreach (var kv in byUnit) {
            score += kv.Value;
        }
        return score;
    }

    private bool Logic(JsonValue node, NaiveHypothesis h, List<Item> items, int[] choice, double[] iRaw, bool[] rawPresent, bool ignoreVetoes) {
        var o = (JsonObject)node;
        var key = o.Keys.Single();
        switch (key) {
            case "term": {
                var t = TermIndex[((JsonString)o["term"]).Value];
                var assigned = items.Where((it, i) => it.Term == t && choice[i] >= 0).Any();
                double MaxThan() => Terms[t]["than"] is JsonArray than ? than.Cast<JsonString>().Select(s => iRaw[TermIndex[s.Value]]).DefaultIfEmpty(0.0).Max() : 0.0;
                var ratio = Terms[t]["ratio"] is JsonFloat r ? r.Value : 1.0;
                return Role(t) switch
                {
                    "required" or "counted" => assigned,
                    "ordering" => assigned && iRaw[t] > ratio * MaxThan(),
                    "veto_present" => rawPresent[t],
                    "veto_stronger_than" => rawPresent[t] && iRaw[t] > ratio * MaxThan(),
                    var x => throw new NotSupportedException("role " + x + " in logic"),
                };
            }
            case "not":
                return ignoreVetoes || !Logic(o["not"], h, items, choice, iRaw, rawPresent, ignoreVetoes);
            case "all":
                return ((JsonArray)o["all"]).Select(c => Logic(c, h, items, choice, iRaw, rawPresent, ignoreVetoes)).ToList().All(x => x);
            case "any":
                return ((JsonArray)o["any"]).Select(c => Logic(c, h, items, choice, iRaw, rawPresent, ignoreVetoes)).ToList().Any(x => x);
            case "k_of": {
                var kof = (JsonObject)o["k_of"];
                var units = new HashSet<int>();
                var count = 0L;
                foreach (var c in ((JsonArray)kof["of"]).Cast<JsonObject>()) {
                    if (c.ContainsKey("term")) {
                        var t = TermIndex[((JsonString)c["term"]).Value];
                        for (var i = 0; i < items.Count; i++) {
                            if (items[i].Term == t && choice[i] >= 0) {
                                units.Add(items[i].Unit);
                            }
                        }
                    }
                    else if (Logic(c, h, items, choice, iRaw, rawPresent, ignoreVetoes)) {
                        count++;
                    }
                }
                return count + units.Count >= Math.Max(1L, KValue(kof["k"], h));
            }
            default:
                throw new NotSupportedException("logic node " + key);
        }
    }

    /// <summary>The rule's outcome on a spectrum (design 4.3, 4.9): hypotheses, firing, choice and ties.</summary>
    public NaiveOutcome Outcome(IReadOnlyList<BuiltPeak> peaks, double precursor, string ionMode, int decoy) {
        var outcome = new NaiveOutcome();
        if (ionMode != Mode) {
            outcome.PreconditionFailure = "ion_mode";
            return outcome;
        }
        var evaluations = Matching(precursor).Select(h => Evaluate(h, peaks, precursor, decoy)).ToList();
        outcome.NHypotheses = evaluations.Count;
        NaiveEvaluation? chosen = null;
        foreach (var e in evaluations.Where(e => e.Fired)) {
            if (chosen is null || e.Score > chosen.Score) {
                chosen = e;
            }
        }
        outcome.Fired = chosen != null;
        if (chosen is null) {
            foreach (var e in evaluations.Where(e => e.FiredIgnoringVetoes)) {
                if (chosen is null || e.Score > chosen.Score) {
                    chosen = e;
                }
            }
        }
        else {
            outcome.Tied = evaluations.Where(e => e.Fired && e.Score == chosen.Score).Select(e => e.Hypothesis.Id).ToList();
        }
        outcome.Chosen = chosen?.Hypothesis.Id;
        outcome.Matched = chosen?.Matched;
        outcome.Upgrades = chosen?.Upgrades;
        return outcome;
    }

    // ------------------------------------------------------------------------------------------------ margins

    /// <summary>The relative margin of an ordering or veto_stronger_than comparison from its equality.</summary>
    public const double RatioMargin = 1e-6;

    /// <summary>I_raw of every term of <paramref name="h"/> (design 4.7): the largest relative intensity of a peak
    /// within the tolerance of one of its members (and 1 Da below a loss reference), present or not, in the
    /// precursor region or not; 0 when there is none.</summary>
    private double[] RawIntensities(NaiveHypothesis h, IReadOnlyList<BuiltPeak> peaks, double precursor, int decoy) {
        var outside = peaks.Where(p => Math.Abs(p.Mz - precursor) > RegionDa).Select(p => (double)p.Intensity).ToList();
        var iRaw = new double[Terms.Count];
        if (outside.Count == 0) {
            return iRaw;
        }
        var baseI = outside.Max();
        for (var t = 0; t < Terms.Count; t++) {
            foreach (var m in Members(t, h, precursor, decoy)) {
                var tol = Str(Terms[t], "kind") == "window" ? HalfWidth(t) : Tolerance(m.Mz);
                foreach (var p in peaks) {
                    if (Math.Abs(p.Mz - m.Mz) <= tol + Slack && !(m.IsLoss && !(m.Reference - p.Mz >= 1.0))) {
                        iRaw[t] = Math.Max(iRaw[t], p.Intensity / baseI);
                    }
                }
            }
        }
        return iRaw;
    }

    /// <summary>
    /// The builder's own margin check, wider than the engine audit (<see cref="EngineFixtures.AuditMargins"/>):
    /// every hypothesis within 1 Da of the precursor tolerance sits at least <paramref name="margin"/> from it, and
    /// every peak sits at least <paramref name="margin"/> from every tolerance boundary of every member of every
    /// passing hypothesis, from the 1 Da loss constraint and from the precursor region; and in every passing
    /// hypothesis, the I_raw of every ordering or veto_stronger_than term with a peak sits at least
    /// <see cref="RatioMargin"/> (relative) from ratio times the largest I_raw of its than-terms. Returns the first
    /// violation.
    /// </summary>
    public string? MarginViolation(IReadOnlyList<BuiltPeak> peaks, double precursor, int decoy, double margin) {
        foreach (var p in peaks) {
            if (Math.Abs(Math.Abs(p.Mz - precursor) - RegionDa) < margin) {
                return "peak " + p.Micro + " on the precursor region";
            }
        }
        foreach (var h in All) {
            var d = Math.Abs(precursor - h.Mz);
            if (Math.Abs(d - PrecursorTolerance) < margin) {
                return h.Id + " on the precursor tolerance";
            }
            if (d > PrecursorTolerance) {
                continue;
            }
            for (var t = 0; t < Terms.Count; t++) {
                foreach (var m in Members(t, h, precursor, decoy)) {
                    var tol = Str(Terms[t], "kind") == "window" ? HalfWidth(t) : Tolerance(m.Mz);
                    foreach (var p in peaks) {
                        var err = Math.Abs(p.Mz - m.Mz);
                        if (Math.Abs(err - tol) < margin) {
                            return h.Id + " " + TermId(t) + ":" + m.Key + " peak " + p.Micro + " on the tolerance";
                        }
                        if (err <= tol && m.IsLoss && Math.Abs(m.Reference - p.Mz - 1.0) < margin) {
                            return h.Id + " " + TermId(t) + ":" + m.Key + " peak " + p.Micro + " on the loss constraint";
                        }
                    }
                }
            }
            var iRaw = RawIntensities(h, peaks, precursor, decoy);
            for (var t = 0; t < Terms.Count; t++) {
                if (Role(t) is not ("ordering" or "veto_stronger_than") || Terms[t]["than"] is not JsonArray than || iRaw[t] <= 0.0) {
                    continue;
                }
                var bound = (Terms[t]["ratio"] is JsonFloat r ? r.Value : 1.0) * than.Cast<JsonString>().Select(s => iRaw[TermIndex[s.Value]]).DefaultIfEmpty(0.0).Max();
                if (bound > 0.0 && Math.Abs(iRaw[t] - bound) < RatioMargin * bound) {
                    return h.Id + " " + TermId(t) + " on the ratio: I_raw " + iRaw[t].ToString("R", CultureInfo.InvariantCulture) + " against "
                           + bound.ToString("R", CultureInfo.InvariantCulture);
                }
            }
        }
        return null;
    }
}

/// <summary>One constructed case of a per-rule fixture: name, description, options, input and the naive expectation.</summary>
internal sealed class RuleFixtureCase
{
    public RuleFixtureCase(string name, string description, JsonObject options, JsonObject input, JsonObject expect) {
        Name = name;
        Description = description;
        Options = options;
        Input = input;
        Expect = expect;
    }

    public string Name { get; }
    public string Description { get; }
    public JsonObject Options { get; }
    public JsonObject Input { get; }
    public JsonObject Expect { get; }
}

/// <summary>
/// Builds the cases of <c>rules/&lt;rule_id&gt;.json</c> constructively (design 7.2). It picks a target hypothesis
/// h* of the rule's own adducts whose positive spectrum makes it the chosen hypothesis, places one peak at the oracle
/// m/z of every member of every support term of h* (outside the precursor region), adds the precursor peak, and
/// derives: the positive case; each required term dropped (on h*, or on the first other hypothesis that places a peak
/// of the term when h* has none; a peak shared with a complementary partner goes for both, and the description says
/// so); each any node with every alternative dropped; each k_of node at k and k-1 evidence units; each veto
/// triggered (with a member that leaves h* chosen and not fired, when one exists); each ordering reversed; each
/// upgrade term with a peak at a member of h*; a support peak at the product tolerance edge, in and out on both sides,
/// and again on a target at or above m/z 500 when the first is below it; the precursor at its tolerance edge, in and
/// out on both sides; the wrong ion mode; a multi-hypothesis tie where two hypotheses can share a precursor (unless
/// the positive case already is one); and the decoy shift +5 Da. Every m/z is integer micro-Da and every intensity an
/// integer, so the decimal text is exact; every case keeps a 5e-6 Da margin from every boundary and a 1e-6 relative
/// margin from every ratio comparison (<see cref="NaiveRule.MarginViolation"/>), or it is left out and named in
/// <see cref="Skipped"/>. The expectation of each case is the naive model's outcome: fired, the chosen hypothesis,
/// its matched and upgrade term:member@peak lists, the ties and n_hypotheses. Rules with measure:false are built
/// alike.
/// </summary>
internal sealed class DiagnosticFixtureBuilder
{
    public const string Instrument = "QTOF";
    private const long Support = 500;
    private const long Strong = 1000;
    private const long Weak = 250;
    private const long PrecursorIntensity = 2000;
    private const double EdgeDa = 1e-5;
    private const double BuilderMargin = 5e-6;
    private const int MaxTargetTries = 400;
    private const int MaxTiePairs = 1000;

    private readonly NaiveRule _rule;

    public DiagnosticFixtureBuilder(NaiveRule rule) {
        _rule = rule;
    }

    /// <summary>Cases that could not keep their margins, or could not be constructed, with the reason.</summary>
    public List<string> Skipped { get; } = new List<string>();

    public NaiveHypothesis? Target { get; private set; }

    public static long Micro(double mz) => (long)Math.Round(mz * 1e6, MidpointRounding.AwayFromZero);

    private static KeyValuePair<string, JsonValue> M(string key, JsonValue value) => new KeyValuePair<string, JsonValue>(key, value);

    private sealed class Placed
    {
        public int Term;
        public string Key = "";
        public int Unit;
        public long Micro;
        public long Intensity;
    }

    /// <summary>One peak per member of every support term of <paramref name="h"/>, outside the precursor region and
    /// apart from every peak placed before it.</summary>
    private List<Placed> PlaceSupport(NaiveHypothesis h, double precursor) {
        var placed = new List<Placed>();
        var unitOfGroup = new Dictionary<int, int>();
        var nUnits = 0;
        for (var t = 0; t < _rule.Terms.Count; t++) {
            if (!_rule.IsSupport(t)) {
                continue;
            }
            foreach (var m in _rule.Members(t, h, precursor, 0)) {
                int unit;
                if (_rule.Group[t] >= 0) {
                    if (!unitOfGroup.TryGetValue(_rule.Group[t], out unit)) {
                        unit = unitOfGroup[_rule.Group[t]] = nUnits++;
                    }
                }
                else {
                    unit = nUnits++;
                }
                if (Math.Abs(m.Mz - precursor) <= 1.05 || m.Mz < 2.0) {
                    continue;
                }
                var micro = Micro(m.Mz);
                var tol = NaiveRule.Tolerance(m.Mz);
                if (placed.Any(p => Math.Abs(p.Micro - micro) / 1e6 <= 2.0 * Math.Max(tol, NaiveRule.Tolerance(p.Micro / 1e6)) + 1e-4)) {
                    continue;
                }
                placed.Add(new Placed { Term = t, Key = m.Key, Unit = unit, Micro = micro, Intensity = _rule.Role(t) == "ordering" ? Strong : Support });
            }
        }
        return placed;
    }

    private static List<BuiltPeak> Spectrum(IEnumerable<Placed> support, long precursorMicro, IEnumerable<BuiltPeak>? extra = null) {
        var peaks = support.Select(p => new BuiltPeak(p.Micro, p.Intensity)).ToList();
        if (extra != null) {
            peaks.AddRange(extra);
        }
        peaks.Add(new BuiltPeak(precursorMicro, PrecursorIntensity));
        return peaks.OrderBy(p => p.Micro).ThenByDescending(p => p.Intensity).ToList();
    }

    private JsonObject Input(IReadOnlyList<BuiltPeak> peaks, long precursorMicro, string ionMode) => new JsonObject(new[]
    {
        M("adduct_evidence", JsonValue.Null),
        M("instrument_type", JsonValue.Of(Instrument)),
        M("ion_mode", JsonValue.Of(ionMode)),
        M("peaks", new JsonArray(peaks.Select(p => (JsonValue)new JsonArray(new[]
        {
            JsonValue.Of(SemanticsFixtureCases.Text(p.Micro)), JsonValue.Of(SemanticsFixtureCases.Text(p.Intensity * 1000000L)),
        })))),
        M("precursor_mz", JsonValue.Of(SemanticsFixtureCases.Text(precursorMicro))),
    });

    private static JsonValue Strings(IEnumerable<string>? items) => items is null ? JsonValue.Null : new JsonArray(items.Select(JsonValue.Of));

    /// <summary>The expectation spec of <see cref="EngineFixtures.CheckExpectations"/> from the naive outcome. A
    /// compact result lists an outcome only when the rule has a hypothesis (design 4.11); a verbose one always.</summary>
    public JsonObject Expectation(NaiveOutcome o, bool verbose) => !verbose && o.NHypotheses == 0
        ? new JsonObject(new[]
        {
            M("outcome_ids", JsonArray.Empty), M("outcomes", JsonObject.Empty), M("refusals", JsonArray.Empty), M("status", JsonValue.Of("evaluated")),
        })
        : new JsonObject(new[]
    {
        M("outcome_ids", new JsonArray(new[] { JsonValue.Of(_rule.RuleId) })),
        M("outcomes", new JsonObject(new[]
        {
            M(_rule.RuleId, new JsonObject(new[]
            {
                M("chosen", JsonValue.OfNullable(o.Chosen)),
                M("fired", JsonValue.Of(o.Fired)),
                M("matched", Strings(o.Matched)),
                M("n_hypotheses", JsonValue.Of(o.NHypotheses)),
                M("precondition_failure", JsonValue.OfNullable(o.PreconditionFailure)),
                M("tied_hypothesis_ids", Strings(o.Tied)),
                M("upgrades", Strings(o.Upgrades)),
            })),
        })),
        M("refusals", JsonArray.Empty),
        M("status", JsonValue.Of("evaluated")),
    });

    private static JsonObject Options(int decoy, bool verbose) {
        var members = new List<KeyValuePair<string, JsonValue>> { M("filter", JsonValue.Of("all")) };
        if (decoy != 0) {
            members.Add(M("decoy_shift_da", JsonValue.Of(decoy)));
        }
        if (verbose) {
            members.Add(M("verbose", JsonValue.Of(true)));
        }
        return new JsonObject(members);
    }

    private string Where(Placed p) => _rule.TermId(p.Term) + ":" + p.Key;

    /// <summary>Picks h*: the first hypothesis of the rule's own adducts (id order) whose positive spectrum keeps its
    /// margins and makes it the chosen hypothesis; failing that, the first that fires; failing that, the first. Sets
    /// <paramref name="capped"/> when the search stopped at <see cref="MaxTargetTries"/>, and
    /// <paramref name="fallback"/> to the reason when h* is not chosen on its own positive spectrum (empty otherwise).</summary>
    private (NaiveHypothesis H, long PMicro, List<Placed> Support)? PickTarget(out bool capped, out string fallback) {
        (NaiveHypothesis, long, List<Placed>)? firing = null, any = null;
        var tries = 0;
        capped = false;
        fallback = "";
        foreach (var h in _rule.All.Where(h => h.Adduct.FromRule)) {
            if (tries++ >= MaxTargetTries) {
                capped = true;
                break;
            }
            var pMicro = Micro(h.Mz);
            var p = pMicro / 1e6;
            var support = PlaceSupport(h, p);
            if (support.Count == 0) {
                continue;
            }
            var peaks = Spectrum(support, pMicro);
            if (_rule.MarginViolation(peaks, p, 0, BuilderMargin) != null) {
                continue;
            }
            any ??= (h, pMicro, support);
            var o = _rule.Outcome(peaks, p, _rule.Mode, 0);
            if (o.Fired) {
                firing ??= (h, pMicro, support);
                if (o.Chosen == h.Id) {
                    return (h, pMicro, support);
                }
            }
        }
        var searched = capped ? "among the first " + MaxTargetTries.ToString(CultureInfo.InvariantCulture) + " of the rule's adducts (search capped)" : "of the rule's adducts";
        if (firing != null) {
            fallback = "h*: no hypothesis " + searched + " is the chosen one on its own positive spectrum; h* = " + firing.Value.Item1.Id
                       + " fires there, but another hypothesis is chosen";
            return firing;
        }
        if (any != null) {
            fallback = "h*: no hypothesis " + searched + " fires on its own positive spectrum; h* = " + any.Value.Item1.Id
                       + ", and its positive case does not fire";
        }
        return any;
    }

    /// <summary>The placed peaks of term <paramref name="t"/> in <paramref name="support"/>: the peaks placed for
    /// it, or, when every member of the term coincides with a peak placed for another term (a complementary partner,
    /// say), the placed peaks within the product tolerance of one of its members.</summary>
    private List<Placed> TermPeaks(int t, NaiveHypothesis h, double precursor, List<Placed> support) {
        var own = support.Where(s => s.Term == t).ToList();
        if (own.Count > 0) {
            return own;
        }
        var members = _rule.Members(t, h, precursor, 0);
        return support.Where(s => members.Any(m => Math.Abs(s.Micro / 1e6 - m.Mz) <= NaiveRule.Tolerance(m.Mz))).ToList();
    }

    /// <summary>The other support terms with a member within the product tolerance of a peak of
    /// <paramref name="removed"/>, by term order.</summary>
    private List<string> AlsoMatched(IEnumerable<int> terms, NaiveHypothesis h, double precursor, List<Placed> removed) {
        var skip = new HashSet<int>(terms);
        return Enumerable.Range(0, _rule.Terms.Count)
            .Where(u => _rule.IsSupport(u) && !skip.Contains(u)
                        && _rule.Members(u, h, precursor, 0).Any(m => removed.Any(s => Math.Abs(s.Micro / 1e6 - m.Mz) <= NaiveRule.Tolerance(m.Mz))))
            .Select(_rule.TermId).ToList();
    }

    /// <summary>
    /// The positive spectrum a case about term <paramref name="t"/> is built on: h*'s when a peak of the term is placed
    /// there; otherwise the first other hypothesis of the rule's own adducts (id order) whose positive spectrum places
    /// one, keeps its margins and makes it the chosen hypothesis, failing that the first that fires. Null, with the
    /// reason, when there is none.
    /// </summary>
    private (NaiveHypothesis H, long PMicro, List<Placed> Support, List<Placed> Peaks)? Basis(int t, NaiveHypothesis h, long pMicro, List<Placed> support, out string reason) {
        reason = "";
        var peaks = TermPeaks(t, h, pMicro / 1e6, support);
        if (peaks.Count > 0) {
            return (h, pMicro, support, peaks);
        }
        (NaiveHypothesis, long, List<Placed>, List<Placed>)? firing = null;
        var tries = 0;
        foreach (var other in _rule.All.Where(x => x.Adduct.FromRule && x.Id != h.Id)) {
            if (tries++ >= MaxTargetTries) {
                reason = "h* has no peak of the term, and no hypothesis among the first " + MaxTargetTries.ToString(CultureInfo.InvariantCulture)
                         + " others of the rule's adducts places one and fires (search capped)";
                return firing;
            }
            var oMicro = Micro(other.Mz);
            var op = oMicro / 1e6;
            var oSupport = PlaceSupport(other, op);
            var oPeaks = TermPeaks(t, other, op, oSupport);
            if (oPeaks.Count == 0) {
                continue;
            }
            var spectrum = Spectrum(oSupport, oMicro);
            if (_rule.MarginViolation(spectrum, op, 0, BuilderMargin) != null) {
                continue;
            }
            var o = _rule.Outcome(spectrum, op, _rule.Mode, 0);
            if (o.Fired) {
                firing ??= (other, oMicro, oSupport, oPeaks);
                if (o.Chosen == other.Id) {
                    return (other, oMicro, oSupport, oPeaks);
                }
            }
        }
        reason = "h* has no peak of the term, and no other hypothesis of the rule's adducts places one and fires";
        return firing;
    }

    private static string On(NaiveHypothesis basis, NaiveHypothesis h) =>
        basis.Id == h.Id ? h.Id : basis.Id + " (h* " + h.Id + " has no peak of the term)";

    /// <summary>The description suffix naming the other support terms the removed peaks also match.</summary>
    private string Also(IEnumerable<int> terms, NaiveHypothesis h, long pMicro, List<Placed> removed) {
        var also = AlsoMatched(terms, h, pMicro / 1e6, removed);
        return also.Count == 0 ? "" : "; the removed peaks also match " + string.Join(", ", also) + ", which lose them too";
    }

    public List<RuleFixtureCase> Build() {
        var cases = new List<RuleFixtureCase>();
        var picked = PickTarget(out var targetCapped, out var fallback);
        if (picked is null) {
            Skipped.Add(targetCapped
                ? "no hypothesis among the first " + MaxTargetTries.ToString(CultureInfo.InvariantCulture) + " of the rule's adducts gives a spectrum with its margins (search capped)"
                : "no hypothesis of the rule's adducts gives a spectrum with its margins");
            return cases;
        }
        if (fallback.Length > 0) {
            Skipped.Add(fallback);
        }
        var (h, pMicro, support) = picked.Value;
        Target = h;
        var p = pMicro / 1e6;
        void Add(string name, string description, List<BuiltPeak> peaks, long precursorMicro, string mode = "", int decoy = 0, bool verbose = false,
                 Func<NaiveOutcome, string>? suffix = null) {
            mode = mode.Length == 0 ? _rule.Mode : mode;
            var precursor = precursorMicro / 1e6;
            var violation = _rule.MarginViolation(peaks, precursor, decoy, BuilderMargin);
            if (violation != null) {
                Skipped.Add(name + ": " + violation);
                return;
            }
            var outcome = _rule.Outcome(peaks, precursor, mode, decoy);
            cases.Add(new RuleFixtureCase(name, description + (suffix is null ? "" : suffix(outcome)), Options(decoy, verbose), Input(peaks, precursorMicro, mode),
                                          Expectation(outcome, verbose)));
        }
        var positive = Spectrum(support, pMicro);
        var hDescription = "h* = " + h.Id + ", precursor at its oracle m/z";
        Add("positive", hDescription + "; one peak per support member: " + string.Join(", ", support.Select(Where)), positive, pMicro);

        // Each required term dropped, on h* or, when h* has no peak of the term, on another hypothesis that has one.
        foreach (var t in Enumerable.Range(0, _rule.Terms.Count).Where(t => _rule.Role(t) == "required")) {
            var basis = Basis(t, h, pMicro, support, out var reason);
            if (basis is null) {
                Skipped.Add("drop_" + _rule.TermId(t) + ": " + reason);
                continue;
            }
            var (bh, bMicro, bSupport, bPeaks) = basis.Value;
            Add("drop_" + _rule.TermId(t), "the positive spectrum of " + On(bh, h) + " without the peaks of required term " + _rule.TermId(t)
                                           + Also(new[] { t }, bh, bMicro, bPeaks),
                Spectrum(bSupport.Where(s => !bPeaks.Contains(s)), bMicro), bMicro,
                suffix: o => !o.Fired ? "; the rule does not fire"
                    : o.Chosen == bh.Id ? "; " + bh.Id + " still fires, because the logic has an alternative to the term"
                    : "; another hypothesis fires and is chosen");
        }

        // Each any node with every alternative dropped: the peaks of every support term under it.
        var anyNodes = new List<JsonArray>();
        void CollectAny(JsonValue node) {
            foreach (var m in ((JsonObject)node).Members) {
                if (m.Key == "any") {
                    anyNodes.Add((JsonArray)m.Value);
                }
                if (m.Key is "all" or "any") {
                    foreach (var c in (JsonArray)m.Value) {
                        CollectAny(c);
                    }
                }
                else if (m.Key == "k_of") {
                    foreach (var c in (JsonArray)((JsonObject)m.Value)["of"]) {
                        CollectAny(c);
                    }
                }
                else if (m.Key == "not") {
                    CollectAny(m.Value);
                }
            }
        }
        CollectAny(_rule.Doc["logic"]);
        for (var n = 0; n < anyNodes.Count; n++) {
            var under = new List<int>();
            void Terms(JsonValue node) {
                foreach (var m in ((JsonObject)node).Members) {
                    if (m.Key == "term") {
                        var t = _rule.TermIndex[((JsonString)m.Value).Value];
                        if (_rule.IsSupport(t) && !under.Contains(t)) {
                            under.Add(t);
                        }
                    }
                    else if (m.Key is "all" or "any") {
                        foreach (var c in (JsonArray)m.Value) {
                            Terms(c);
                        }
                    }
                    else if (m.Key == "k_of") {
                        foreach (var c in (JsonArray)((JsonObject)m.Value)["of"]) {
                            Terms(c);
                        }
                    }
                }
            }
            foreach (var c in anyNodes[n]) {
                Terms(c);
            }
            var name = "drop_any" + n.ToString(CultureInfo.InvariantCulture);
            var removed = under.SelectMany(t => TermPeaks(t, h, p, support)).Distinct().ToList();
            if (removed.Count == 0) {
                Skipped.Add(name + ": no peak of a term under any node " + n.ToString(CultureInfo.InvariantCulture) + " is placed at " + h.Id);
                continue;
            }
            Add(name, "the positive spectrum of " + h.Id + " without the peaks of every support term under any node " + n.ToString(CultureInfo.InvariantCulture)
                      + " (pre-order): " + string.Join(", ", under.Select(_rule.TermId)) + Also(under, h, pMicro, removed),
                Spectrum(support.Where(s => !removed.Contains(s)), pMicro), pMicro);
        }

        // Each k_of node at k and k-1 evidence units.
        var kNodes = new List<JsonObject>();
        void Collect(JsonValue node) {
            var o = (JsonObject)node;
            foreach (var m in o.Members) {
                if (m.Key == "k_of") {
                    kNodes.Add((JsonObject)m.Value);
                    foreach (var c in (JsonArray)((JsonObject)m.Value)["of"]) {
                        Collect(c);
                    }
                }
                else if (m.Key is "all" or "any") {
                    foreach (var c in (JsonArray)m.Value) {
                        Collect(c);
                    }
                }
                else if (m.Key == "not") {
                    Collect(m.Value);
                }
            }
        }
        Collect(_rule.Doc["logic"]);
        for (var n = 0; n < kNodes.Count; n++) {
            var of = ((JsonArray)kNodes[n]["of"]).Cast<JsonObject>().ToList();
            var termChildren = of.Where(c => c.ContainsKey("term")).Select(c => _rule.TermIndex[((JsonString)c["term"]).Value]).ToList();
            var subtrees = of.Count - termChildren.Count;
            var k = Math.Max(1L, _rule.KValue(kNodes[n]["k"], h));
            var units = support.Where(s => termChildren.Contains(s.Term)).Select(s => s.Unit).Distinct().ToList();
            var need = k - subtrees;
            if (need < 1 || need > units.Count) {
                Skipped.Add("k_of" + n.ToString(CultureInfo.InvariantCulture) + ": k " + k.ToString(CultureInfo.InvariantCulture) + " needs "
                            + need.ToString(CultureInfo.InvariantCulture) + " of " + units.Count.ToString(CultureInfo.InvariantCulture) + " placed units");
                continue;
            }
            foreach (var keep in new[] { need, need - 1 }) {
                var dropped = new HashSet<int>(units.Skip((int)keep));
                var name = "k_of" + n.ToString(CultureInfo.InvariantCulture) + (keep == need ? "_at_k" : "_at_k_minus_1");
                Add(name, "the positive spectrum of " + h.Id + " with " + (keep + subtrees).ToString(CultureInfo.InvariantCulture) + " of the k = "
                          + k.ToString(CultureInfo.InvariantCulture) + " evidence units of k_of node " + n.ToString(CultureInfo.InvariantCulture)
                          + " (pre-order) present", Spectrum(support.Where(s => !dropped.Contains(s.Unit)), pMicro), pMicro);
            }
        }

        // Each veto triggered, at the strongest intensity. The member is the first (away from the precursor first)
        // whose case leaves h* chosen and not fired, so the case shows h* vetoed and no other hypothesis taking over.
        foreach (var t in Enumerable.Range(0, _rule.Terms.Count).Where(_rule.IsVeto)) {
            var members = _rule.Members(t, h, p, 0);
            var ordered = members.Where(x => Math.Abs(x.Mz - p) > 1.05).Concat(members.Where(x => Math.Abs(x.Mz - p) <= 1.05)).ToList();
            if (ordered.Count == 0) {
                Skipped.Add("veto_" + _rule.TermId(t) + ": no member at " + h.Id);
                continue;
            }
            List<BuiltPeak> VetoSpectrum(NaiveMember x) {
                var micro = Micro(x.Mz);
                return Spectrum(support.Where(s => s.Micro != micro), pMicro, new[] { new BuiltPeak(micro, Strong) });
            }
            var m = ordered.FirstOrDefault(x => {
                var peaks = VetoSpectrum(x);
                if (_rule.MarginViolation(peaks, p, 0, BuilderMargin) != null) {
                    return false;
                }
                var o = _rule.Outcome(peaks, p, _rule.Mode, 0);
                return !o.Fired && o.Chosen == h.Id;
            });
            var alone = m != null;
            m ??= ordered[0];
            Add("veto_" + _rule.TermId(t), "the positive spectrum of " + h.Id + " plus veto " + _rule.TermId(t) + ":" + m.Key + " at intensity "
                                           + Strong.ToString(CultureInfo.InvariantCulture)
                                           + (alone ? "" : "; every member of the veto lets another hypothesis fire, so h* is not the chosen one"),
                VetoSpectrum(m), pMicro);
        }

        // Each ordering reversed: A weaker than the than-terms, on h* or, when h* has no peak of the term, on another
        // hypothesis that has one.
        foreach (var t in Enumerable.Range(0, _rule.Terms.Count).Where(t => _rule.Role(t) == "ordering")) {
            var basis = Basis(t, h, pMicro, support, out var reason);
            if (basis is null) {
                Skipped.Add("ordering_reversed_" + _rule.TermId(t) + ": " + reason);
                continue;
            }
            var (bh, bMicro, bSupport, bPeaks) = basis.Value;
            var reversed = bSupport.Select(s => bPeaks.Contains(s) ? new Placed { Term = s.Term, Key = s.Key, Unit = s.Unit, Micro = s.Micro, Intensity = Weak } : s);
            Add("ordering_reversed_" + _rule.TermId(t), "the positive spectrum of " + On(bh, h) + " with ordering term " + _rule.TermId(t) + " at intensity "
                + Weak.ToString(CultureInfo.InvariantCulture) + " below its than-terms", Spectrum(reversed, bMicro), bMicro);
        }

        // Each upgrade term: the positive spectrum of h* plus one peak at the first member of the term (by target)
        // that lies outside the precursor region and apart from every placed peak, so that the chosen hypothesis
        // reports it. An upgrade is never in the logic; the expectation records what the extra peak does elsewhere.
        foreach (var t in Enumerable.Range(0, _rule.Terms.Count).Where(t => _rule.Role(t) == "upgrade")) {
            var name = "upgrade_" + _rule.TermId(t);
            var members = _rule.Members(t, h, p, 0);
            if (members.Count == 0) {
                Skipped.Add(name + ": no member at " + h.Id);
                continue;
            }
            var free = members.FirstOrDefault(x => Math.Abs(x.Mz - p) > 1.05 && x.Mz >= 2.0
                && !support.Any(s => Math.Abs(s.Micro / 1e6 - x.Mz) <= 2.0 * Math.Max(NaiveRule.Tolerance(x.Mz), NaiveRule.Tolerance(s.Micro / 1e6)) + 1e-4));
            if (free is null) {
                Skipped.Add(name + ": every member at " + h.Id + " lies in the precursor region or on a placed support peak");
                continue;
            }
            Add(name, "the positive spectrum of " + h.Id + " plus upgrade " + _rule.TermId(t) + ":" + free.Key + " at intensity "
                      + Support.ToString(CultureInfo.InvariantCulture),
                Spectrum(support, pMicro, new[] { new BuiltPeak(Micro(free.Mz), Support) }), pMicro);
        }

        // The product tolerance edge of the first placed required (or other support) peak.
        // When that target is below m/z 500 (the 0.005 Da branch), the same four cases again with the placed support
        // peak of the highest target at or above m/z 500 (the 10 ppm branch), named tolerance_ppm_*.
        double TargetMz(Placed s) => _rule.Members(s.Term, h, p, 0).Single(m => m.Key == s.Key).Mz;
        var probe = support.OrderBy(s => _rule.Role(s.Term) == "required" ? 0 : 1).ThenBy(s => support.IndexOf(s)).First();
        var probes = new List<(string Prefix, Placed Probe)> { ("tolerance_", probe) };
        if (TargetMz(probe) < 500.0) {
            var ppm = support.Where(s => TargetMz(s) >= 500.0).OrderByDescending(TargetMz).ThenBy(s => support.IndexOf(s)).FirstOrDefault();
            if (ppm != null) {
                probes.Add(("tolerance_ppm_", ppm));
            }
        }
        foreach (var (prefix, pr) in probes) {
            var target = TargetMz(pr);
            var tol = NaiveRule.Tolerance(target);
            foreach (var (edge, offset) in new[] { ("in_high", tol - EdgeDa), ("out_high", tol + EdgeDa), ("in_low", -(tol - EdgeDa)), ("out_low", -(tol + EdgeDa)) }) {
                var moved = Micro(target + offset);
                var shifted = support.Select(s => s == pr ? new Placed { Term = s.Term, Key = s.Key, Unit = s.Unit, Micro = moved, Intensity = s.Intensity } : s);
                Add(prefix + edge, "the positive spectrum of " + h.Id + " with " + Where(pr) + " moved "
                                   + (offset > 0 ? "+" : "-") + "(tol " + (Math.Abs(offset) < tol ? "- " : "+ ") + "1e-5) Da, tol = max(0.005 Da, 10 ppm)",
                    Spectrum(shifted, pMicro), pMicro);
            }
        }

        // The precursor tolerance edge; the support peaks follow an observed loss reference.
        var ptol = _rule.PrecursorTolerance;
        foreach (var (name, offset) in new[] { ("precursor_in_high", ptol - EdgeDa), ("precursor_out_high", ptol + EdgeDa),
                                               ("precursor_in_low", -(ptol - EdgeDa)), ("precursor_out_low", -(ptol + EdgeDa)) }) {
            var moved = Micro(h.Mz + offset);
            var peaks = _rule.LossReference == "observed" ? Spectrum(PlaceSupport(h, moved / 1e6), moved) : Spectrum(support, moved);
            Add(name, "the positive spectrum of " + h.Id + " with the precursor " + (offset > 0 ? "+" : "-") + "(precursor_tolerance_da "
                      + (Math.Abs(offset) < ptol ? "- " : "+ ") + "1e-5) Da from its m/z", peaks, moved);
        }

        Add("wrong_mode", "the positive spectrum of " + h.Id + " in the other ion mode, verbose so that the outcome with no hypothesis is listed",
            positive, pMicro, _rule.Mode == "pos" ? "neg" : "pos", verbose: true);

        // A multi-hypothesis tie: two hypotheses that share a precursor, with the union of their positive peaks. When
        // the positive case already is a tie and the first tie found has its input, the tie case is left out.
        var positiveOutcome = _rule.Outcome(positive, p, _rule.Mode, 0);
        var positiveTies = positiveOutcome.Fired && positiveOutcome.Tied.Count >= 2;
        var tie = FindTie(out var tieCapped, out var nPairs);
        if (positiveTies && (tie is null || (tie.Value.PMicro == pMicro && tie.Value.Peaks.Select(x => (x.Micro, x.Intensity)).SequenceEqual(positive.Select(x => (x.Micro, x.Intensity)))))) {
            Skipped.Add("tie: the positive case already is a tie of " + positiveOutcome.Tied.Count.ToString(CultureInfo.InvariantCulture) + " hypotheses"
                        + (tie is null ? "" : ", with the input of the first tie found"));
        }
        else if (tie is null) {
            Skipped.Add(tieCapped
                ? "tie: no tie among the first " + MaxTiePairs.ToString(CultureInfo.InvariantCulture) + " pairs of hypotheses that share a precursor (search capped)"
                : nPairs == 0
                    ? "tie: no two hypotheses share a precursor (within precursor_tolerance_da - 2e-5 Da)"
                    : "tie: none of the " + nPairs.ToString(CultureInfo.InvariantCulture) + " pairs of hypotheses that share a precursor ties on the union of their peaks");
        }
        else {
            Add("tie", "the union of the positive spectra of " + tie.Value.A.Id + " and " + tie.Value.B.Id + " at the precursor of the first",
                tie.Value.Peaks, tie.Value.PMicro);
        }

        Add("decoy_plus_5", "the positive spectrum of " + h.Id + " under the decoy shift +5 Da", positive, pMicro, decoy: 5);
        return cases;
    }

    /// <summary>The first pair of hypotheses that share a precursor and tie on the union of their positive peaks,
    /// each placed peak at its own intensity (an ordering term stronger than its than-terms, as in the positive
    /// case); <paramref name="capped"/> is set when the search stopped at <see cref="MaxTiePairs"/> pairs, and
    /// <paramref name="pairs"/> counts the pairs tried.</summary>
    private (NaiveHypothesis A, NaiveHypothesis B, List<BuiltPeak> Peaks, long PMicro)? FindTie(out bool capped, out int pairs) {
        var byMz = _rule.All.OrderBy(x => x.Mz).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
        pairs = 0;
        capped = false;
        foreach (var a in _rule.All.Where(x => x.Adduct.FromRule)) {
            var pMicro = Micro(a.Mz);
            var p = pMicro / 1e6;
            foreach (var b in byMz.Where(x => x.Id != a.Id && Math.Abs(x.Mz - p) <= _rule.PrecursorTolerance - 2e-5)) {
                if (pairs++ >= MaxTiePairs) {
                    capped = true;
                    return null;
                }
                var placed = PlaceSupport(a, p);
                foreach (var s in PlaceSupport(b, p)) {
                    if (!placed.Any(x => Math.Abs(x.Micro - s.Micro) / 1e6 <= 2.0 * NaiveRule.Tolerance(s.Micro / 1e6) + 1e-4)) {
                        placed.Add(s);
                    }
                }
                if (placed.Count == 0) {
                    continue;
                }
                var peaks = Spectrum(placed, pMicro);
                if (_rule.MarginViolation(peaks, p, 0, BuilderMargin) != null) {
                    continue;
                }
                var o = _rule.Outcome(peaks, p, _rule.Mode, 0);
                if (o.Fired && o.Tied.Count >= 2) {
                    return (a, b, peaks, pMicro);
                }
            }
        }
        return null;
    }
}
