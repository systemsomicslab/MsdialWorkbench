using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Loading
{
    /// <summary>A term of a rule whose schema passed (the reference's Term dataclass).</summary>
    internal sealed class TermInfo
    {
        public TermInfo(int index, JsonObject obj) {
            Index = index;
            Obj = obj;
            Tid = ((JsonString)obj["term_id"]).Value;
            Kind = ((JsonString)obj["kind"]).Value;
            Role = ((JsonString)obj["role"]).Value;
        }

        public int Index { get; }
        public string Tid { get; }
        public string Kind { get; }
        public string Role { get; }
        public JsonObject Obj { get; }
        public long[]? Formula { get; set; }
        /// <summary>"class" or "name", and the selected value.</summary>
        public string? UnitSelector { get; set; }
        public string? UnitValue { get; set; }
        public long[]? Plus { get; set; }
        public long[]? Minus { get; set; }
        public string? ScaffoldKey { get; set; }

        public bool IsSupport => SchemaReader.In(Role, SchemaReader.SupportRoles);
    }

    /// <summary>A logic node after role placement: term (with the term index, or -1 when unknown), all, any, not or k_of.</summary>
    internal sealed class CompiledNode
    {
        public CompiledNode(string kind, int termIndex, int slot, List<CompiledNode> children) {
            Kind = kind;
            TermIndex = termIndex;
            Slot = slot;
            Children = children;
        }

        public string Kind { get; }
        public int TermIndex { get; }
        /// <summary>k_of: the index of its k in <see cref="RuleInfo.KNodes"/>.</summary>
        public int Slot { get; }
        public List<CompiledNode> Children { get; }
    }

    /// <summary>A rule whose schema and references passed: enough to enumerate hypotheses and targets.</summary>
    internal sealed class RuleInfo
    {
        public RuleInfo(string ruleId, JsonObject doc, string file, string mode, int charge, List<TermInfo> terms,
                        Dictionary<string, TermInfo> byId, ScaffoldList? scaffold, long[] core, long[][] unitRanges,
                        List<KeyValuePair<string, long[]>> totals, List<AdductTry> tried, CompiledNode logic,
                        List<KeyValuePair<string, JsonValue>> kNodes, int[] claimTerms, List<HashSet<int>> groups,
                        Dictionary<int, List<int>> pairIndex, List<int> pairIndexOrder) {
            RuleId = ruleId;
            Doc = doc;
            File = file;
            Mode = mode;
            Charge = charge;
            Terms = terms;
            ById = byId;
            Scaffold = scaffold;
            Core = core;
            UnitRanges = unitRanges;
            Totals = totals;
            Tried = tried;
            Logic = logic;
            KNodes = kNodes;
            ClaimTerms = claimTerms;
            Groups = groups;
            PairIndex = pairIndex;
            PairIndexOrder = pairIndexOrder;
        }

        public string RuleId { get; }
        public JsonObject Doc { get; }
        public string File { get; }
        /// <summary>"pos" or "neg".</summary>
        public string Mode { get; }
        public int Charge { get; }
        public List<TermInfo> Terms { get; }
        public Dictionary<string, TermInfo> ById { get; }
        public ScaffoldList? Scaffold { get; }
        public long[] Core { get; }
        /// <summary>[lo, hi] per units-table row (0, 0 for units the rule does not name).</summary>
        public long[][] UnitRanges { get; }
        /// <summary>unit_totals in file order: class name, [lo, hi].</summary>
        public List<KeyValuePair<string, long[]>> Totals { get; }
        public List<AdductTry> Tried { get; }
        public CompiledNode Logic { get; }
        /// <summary>The k of every k_of node, in pre-order: its pointer and its k value.</summary>
        public List<KeyValuePair<string, JsonValue>> KNodes { get; }
        /// <summary>Distinct claim term indices, sorted.</summary>
        public int[] ClaimTerms { get; }
        public List<HashSet<int>> Groups { get; }
        /// <summary>Term index -> indices of the complementary pairs it is in.</summary>
        public Dictionary<int, List<int>> PairIndex { get; }
        public List<int> PairIndexOrder { get; }
        public bool Measure => ((JsonBoolean)Doc["measure"]).Value;
    }

    /// <summary>Phase B of the load-time checks (design 3.6 items 2, 3 and 5): references, role placement, claim terms
    /// and complementary pairs. Mirrors <c>check_rule_refs</c> and <c>place_logic</c> of the Python reference.</summary>
    internal static class RuleValidator
    {
        public static RuleInfo? CheckRuleRefs(ErrorSink c, JsonObject doc, string file, LoadedTables tables) {
            var n0 = c.N;
            var el = tables.Elements;
            var pre = (JsonObject)doc["preconditions"];
            var comp = (JsonObject)pre["composition"];
            var mode = ((JsonString)pre["ion_mode"]).Value;
            var charge = mode == "pos" ? 1 : -1;
            var terms = new List<TermInfo>();
            var byId = new Dictionary<string, TermInfo>(StringComparer.Ordinal);
            var termArray = (JsonArray)doc["terms"];
            for (var i = 0; i < termArray.Count; i++) {
                var ptr = Pointers.Child("/terms", i);
                var t = (JsonObject)termArray[i];
                var term = new TermInfo(i, t);
                if (byId.ContainsKey(term.Tid)) {
                    c.Err(RuleErrorCodes.DuplicateTerm, Pointers.Child(ptr, "term_id"));
                }
                else {
                    byId[term.Tid] = term;
                }
                terms.Add(term);
                if (t.ContainsKey("formula")) {
                    term.Formula = Composition.Parse(((JsonString)t["formula"]).Value, el);
                }
                if (t.ContainsKey("plus") && !(t["plus"] is JsonNull)) {
                    term.Plus = Composition.Parse(((JsonString)t["plus"]).Value, el);
                }
                if (t.ContainsKey("minus") && !(t["minus"] is JsonNull)) {
                    term.Minus = Composition.Parse(((JsonString)t["minus"]).Value, el);
                }
                if (t.ContainsKey("unit")) {
                    var u = (JsonObject)t["unit"];
                    var sel = u.ContainsKey("class") ? "class" : "name";
                    term.UnitSelector = sel;
                    term.UnitValue = ((JsonString)u[sel]).Value;
                    term.Plus = u["plus"] is JsonString plus ? Composition.Parse(plus.Value, el) : null;
                    term.Minus = u["minus"] is JsonString minus ? Composition.Parse(minus.Value, el) : null;
                    if (sel == "name" && !tables.UnitByName.ContainsKey(term.UnitValue)) {
                        c.Err(RuleErrorCodes.UnknownUnit, Pointers.Child(ptr, "unit", "name"));
                    }
                }
                if (t.ContainsKey("scaffold_ions")) {
                    term.ScaffoldKey = ((JsonString)t["scaffold_ions"]).Value;
                }
                if ((term.Kind == "aglycone_ion" || term.Kind == "ion_series") && el.HydrogenIndex < 0) {
                    c.Err(RuleErrorCodes.UnknownElement, Pointers.Child(ptr, "form")); // Y0 forms add or remove hydrogen
                }
                if (t["than"] is JsonArray than) {
                    for (var j = 0; j < than.Count; j++) {
                        if (((JsonString)than[j]).Value == term.Tid) {
                            c.Err(RuleErrorCodes.BadValue, Pointers.Child(ptr, "than", j));
                        }
                    }
                }
            }
            foreach (var t in terms) {
                if (t.Obj["than"] is JsonArray than) {
                    for (var j = 0; j < than.Count; j++) {
                        if (!byId.ContainsKey(((JsonString)than[j]).Value)) {
                            c.Err(RuleErrorCodes.UnknownTerm, Pointers.Child(Pointers.Child("/terms", t.Index), "than", j));
                        }
                    }
                }
            }
            // composition
            const string pc = "/preconditions/composition";
            var core = comp["core"] is JsonString coreText ? Composition.Parse(coreText.Value, el) : new long[el.Count];
            ScaffoldList? scaffold = null;
            if (comp["scaffold_list"] is JsonString listId) {
                if (!tables.Scaffolds.TryGetValue(listId.Value, out scaffold)) {
                    scaffold = null;
                    c.Err(RuleErrorCodes.UnknownScaffoldList, Pointers.Child(pc, "scaffold_list"));
                }
            }
            var units = (JsonObject)comp["units"];
            foreach (var name in units.Keys) {
                if (!tables.UnitByName.ContainsKey(name)) {
                    c.Err(RuleErrorCodes.UnknownUnit, Pointers.Child(pc, "units", name));
                }
            }
            foreach (var t in terms) {
                if (t.ScaffoldKey is null) {
                    continue;
                }
                var p = Pointers.Child(Pointers.Child("/terms", t.Index), "scaffold_ions");
                if (comp["scaffold_list"] is JsonNull) {
                    c.Err(RuleErrorCodes.UnknownScaffoldList, p);
                }
                else if (scaffold != null) {
                    foreach (var e in scaffold.Entries) {
                        if (!e.Ions.TryGetValue(t.ScaffoldKey, out var ions)
                            || ions.Comps(mode == "pos" ? RuleIonMode.Positive : RuleIonMode.Negative).Count == 0) {
                            c.Err(RuleErrorCodes.UnknownIonKey, p);
                            break;
                        }
                    }
                }
            }
            // adducts tried in adduct_ambiguous order: the rule's list, then common adducts of the polarity (design 4.3)
            var tried = new List<AdductTry>();
            var adducts = (JsonArray)pre["adducts"];
            var listed = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < adducts.Count; i++) {
                var name = ((JsonString)adducts[i]).Value;
                listed.Add(name);
                if (!tables.AdductByName.TryGetValue(name, out var a)) {
                    c.Err(RuleErrorCodes.UnknownAdduct, Pointers.Child("/preconditions/adducts", i));
                }
                else if (a.Charge != charge) {
                    c.Err(RuleErrorCodes.AdductPolarity, Pointers.Child("/preconditions/adducts", i));
                }
                else {
                    tried.Add(new AdductTry(a, true));
                }
            }
            foreach (var a in tables.Adducts) {
                if (a.Common && a.Charge == charge && !listed.Contains(a.Adduct)) {
                    tried.Add(new AdductTry(a, false));
                }
            }
            // motifs
            var claim = (JsonObject)doc["claim"];
            if (!tables.Motifs.ContainsKey(((JsonString)claim["motif_id"]).Value)) {
                c.Err(RuleErrorCodes.UnknownMotif, "/claim/motif_id");
            }
            var competitors = (JsonArray)pre["competitors"];
            for (var i = 0; i < competitors.Count; i++) {
                if (!tables.Motifs.ContainsKey(((JsonString)((JsonObject)competitors[i])["motif_id"]).Value)) {
                    c.Err(RuleErrorCodes.UnknownMotif, Pointers.Child(Pointers.Child("/preconditions/competitors", i), "motif_id"));
                }
            }
            // logic placement
            var used = new HashSet<int>();
            var kNodes = new List<KeyValuePair<string, JsonValue>>();
            var logic = PlaceLogic(c, (JsonObject)doc["logic"], "/logic", byId, false, false, used, kNodes);
            foreach (var t in terms) {
                if (t.Role != "upgrade" && !used.Contains(t.Index) && byId.TryGetValue(t.Tid, out var first) && ReferenceEquals(first, t)) {
                    c.Err(RuleErrorCodes.UnusedTerm, Pointers.Child("/terms", t.Index));
                }
            }
            // claim terms
            var claimIdx = new List<int>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var ids = (JsonArray)claim["claim_term_ids"];
            for (var i = 0; i < ids.Count; i++) {
                var p = Pointers.Child("/claim/claim_term_ids", i);
                var tid = ((JsonString)ids[i]).Value;
                if (!byId.TryGetValue(tid, out var t)) {
                    c.Err(RuleErrorCodes.UnknownTerm, p);
                }
                else if (seen.Contains(tid) || !t.IsSupport) {
                    c.Err(RuleErrorCodes.ClaimTermRole, p);
                }
                else {
                    claimIdx.Add(t.Index);
                }
                seen.Add(tid);
            }
            // complementary pairs, groups by transitive closure
            var parent = new Dictionary<int, int>();
            int Find(int x) {
                while (parent.TryGetValue(x, out var px) && px != x) {
                    x = px;
                }
                return x;
            }
            var pairIndex = new Dictionary<int, List<int>>();
            var pairIndexOrder = new List<int>();
            var seenPairs = new HashSet<string>(StringComparer.Ordinal);
            var pairs = (JsonArray)doc["complementary"];
            for (var i = 0; i < pairs.Count; i++) {
                var p = Pointers.Child("/complementary", i);
                var pair = (JsonArray)pairs[i];
                var a = ((JsonString)pair[0]).Value;
                var b = ((JsonString)pair[1]).Value;
                byId.TryGetValue(a, out var ta);
                byId.TryGetValue(b, out var tb);
                if (ta is null) {
                    c.Err(RuleErrorCodes.UnknownTerm, Pointers.Child(p, 0));
                }
                if (tb is null) {
                    c.Err(RuleErrorCodes.UnknownTerm, Pointers.Child(p, 1));
                }
                if (ta is null || tb is null) {
                    continue;
                }
                var key = string.CompareOrdinal(a, b) <= 0 ? a + "\n" + b : b + "\n" + a;
                if (a == b || ta.Kind == tb.Kind || !ta.IsSupport || !tb.IsSupport || seenPairs.Contains(key)) {
                    c.Err(RuleErrorCodes.ComplementaryPair, p);
                    continue;
                }
                seenPairs.Add(key);
                foreach (var ti in new[] { ta.Index, tb.Index }) {
                    if (!pairIndex.TryGetValue(ti, out var list)) {
                        pairIndex[ti] = list = new List<int>();
                        pairIndexOrder.Add(ti);
                    }
                    list.Add(i);
                }
                parent[Find(ta.Index)] = Find(tb.Index);
            }
            var groupByRoot = new Dictionary<int, HashSet<int>>();
            var groups = new List<HashSet<int>>();
            foreach (var x in pairIndexOrder) {
                var root = Find(x);
                if (!groupByRoot.TryGetValue(root, out var g)) {
                    groupByRoot[root] = g = new HashSet<int>();
                    groups.Add(g);
                }
                g.Add(x);
            }
            if (c.N != n0) {
                return null;
            }
            // claim_terms_too_few (design 3.5): >= 2 terms, or one multi-member term counted under k >= 2 everywhere.
            // It does not stop the two-ion passes: the design's example expects it together with claim_single_ion.
            if (((JsonBoolean)doc["measure"]).Value) {
                var ok = claimIdx.Count >= 2;
                if (claimIdx.Count == 1) {
                    var t = terms[claimIdx[0]];
                    var parents = new Dictionary<int, List<int>>();
                    KOfParents(logic, -1, parents);
                    parents.TryGetValue(t.Index, out var slots);
                    ok = t.Role == "counted" && MultiMemberCapable(t) && slots != null && slots.Count > 0;
                    if (ok) {
                        foreach (var s in slots!) {
                            if (!KAtLeastTwo(kNodes[s].Value)) {
                                ok = false;
                            }
                        }
                    }
                }
                if (!ok) {
                    c.Err(RuleErrorCodes.ClaimTermsTooFew, "/claim/claim_term_ids");
                }
            }
            var unitRanges = new long[tables.Units.Count][];
            for (var i = 0; i < tables.Units.Count; i++) {
                unitRanges[i] = units.TryGetValue(tables.Units[i].Name, out var range)
                    ? new[] { ((JsonInteger)((JsonArray)range)[0]).Value, ((JsonInteger)((JsonArray)range)[1]).Value }
                    : new long[] { 0, 0 };
            }
            var totals = new List<KeyValuePair<string, long[]>>();
            foreach (var m in ((JsonObject)comp["unit_totals"]).Members) {
                var range = (JsonArray)m.Value;
                totals.Add(new KeyValuePair<string, long[]>(m.Key, new[] { ((JsonInteger)range[0]).Value, ((JsonInteger)range[1]).Value }));
            }
            var claimTerms = new SortedSet<int>(claimIdx);
            var claimArray = new int[claimTerms.Count];
            claimTerms.CopyTo(claimArray);
            return new RuleInfo(((JsonString)doc["rule_id"]).Value, doc, file, mode, charge, terms, byId, scaffold, core,
                                unitRanges, totals, tried, logic, kNodes, claimArray, groups, pairIndex, pairIndexOrder);
        }

        private static bool KAtLeastTwo(JsonValue k) =>
            (k is JsonInteger i && i.Value >= 2) || (k is JsonObject o && ((JsonInteger)o["min"]).Value >= 2);

        private static bool MultiMemberCapable(TermInfo t) =>
            t.Kind == "ion_series" || t.ScaffoldKey != null || t.UnitSelector == "class";

        private static void KOfParents(CompiledNode node, int parentSlot, Dictionary<int, List<int>> output) {
            switch (node.Kind) {
                case "term":
                    if (parentSlot >= 0 && node.TermIndex >= 0) {
                        if (!output.TryGetValue(node.TermIndex, out var list)) {
                            output[node.TermIndex] = list = new List<int>();
                        }
                        list.Add(parentSlot);
                    }
                    return;
                case "k_of":
                    foreach (var ch in node.Children) {
                        KOfParents(ch, node.Slot, output);
                    }
                    return;
                default:
                    foreach (var ch in node.Children) {
                        KOfParents(ch, -1, output);
                    }
                    return;
            }
        }

        /// <summary>Phase B role placement (design 3.6 item 3): returns the compiled node.</summary>
        private static CompiledNode PlaceLogic(ErrorSink c, JsonObject node, string ptr, Dictionary<string, TermInfo> terms,
                                               bool insideNot, bool parentKOf, HashSet<int> used,
                                               List<KeyValuePair<string, JsonValue>> outK) {
            var key = node.Members[0].Key;
            var val = node.Members[0].Value;
            var p = Pointers.Child(ptr, key);
            if (key == "term") {
                if (!terms.TryGetValue(((JsonString)val).Value, out var t)) {
                    c.Err(RuleErrorCodes.UnknownTerm, p);
                    return new CompiledNode("term", -1, -1, new List<CompiledNode>());
                }
                used.Add(t.Index);
                var role = t.Role;
                var isVeto = SchemaReader.In(role, SchemaReader.VetoRoles);
                var bad = role == "upgrade"
                          || (role == "counted") != parentKOf
                          || isVeto != insideNot
                          || ((role == "required" || role == "ordering") && insideNot);
                if (bad) {
                    c.Err(RuleErrorCodes.RolePlacement, p);
                }
                return new CompiledNode("term", t.Index, -1, new List<CompiledNode>());
            }
            if (key == "all" || key == "any") {
                var children = new List<CompiledNode>();
                var items = (JsonArray)val;
                for (var i = 0; i < items.Count; i++) {
                    children.Add(PlaceLogic(c, (JsonObject)items[i], Pointers.Child(p, i), terms, insideNot, false, used, outK));
                }
                return new CompiledNode(key, -1, -1, children);
            }
            if (key == "not") {
                if (insideNot) {
                    c.Err(RuleErrorCodes.RolePlacement, p);
                }
                return new CompiledNode("not", -1, -1, new List<CompiledNode> { PlaceLogic(c, (JsonObject)val, p, terms, true, false, used, outK) });
            }
            // k_of
            if (insideNot) {
                c.Err(RuleErrorCodes.RolePlacement, p);
            }
            var kof = (JsonObject)val;
            var slot = outK.Count;
            outK.Add(new KeyValuePair<string, JsonValue>(p, kof["k"]));
            var kids = new List<CompiledNode>();
            var of = (JsonArray)kof["of"];
            for (var i = 0; i < of.Count; i++) {
                kids.Add(PlaceLogic(c, (JsonObject)of[i], Pointers.Child(p, "of", i), terms, insideNot, true, used, outK));
            }
            return new CompiledNode("k_of", -1, slot, kids);
        }
    }
}
