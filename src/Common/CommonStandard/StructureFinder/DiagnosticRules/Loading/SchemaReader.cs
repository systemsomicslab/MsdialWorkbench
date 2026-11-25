using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Loading
{
    internal enum FormulaOutcome
    {
        /// <summary>The key is absent (its missing_key is reported by the key check).</summary>
        Missing,
        /// <summary>An error was reported.</summary>
        Error,
        /// <summary>An allowed null.</summary>
        Null,
        Parsed,
    }

    /// <summary>
    /// Closed schemas of the tables and of the rule record (design 3.4, 3.5; load-time check 1 of 3.6). Each method
    /// mirrors the function of the same name in the Python reference (<c>rule_format.py</c>): the same codes at the
    /// same pointers, and the same early returns, which depend on whether an error was reported.
    /// </summary>
    internal static class SchemaReader
    {
        public const int NoteMax = 300;
        public const int MaxLogicDepth = 16;
        public const int MaxLogicNodes = 256;

        private const RegexOptions Options = RegexOptions.CultureInvariant;
        public static readonly Regex DoiPattern = new Regex(@"^10\.[0-9]{4,9}/[!-~]+\z", Options);
        public static readonly Regex ShaPattern = new Regex(@"^[0-9a-f]{64}\z", Options);
        public static readonly Regex IdPattern = new Regex(@"^[a-z0-9_]+\z", Options);
        public static readonly Regex TermIdPattern = new Regex(@"^[A-Za-z0-9_]{1,64}\z", Options);
        public static readonly Regex UnitPattern = new Regex(@"^[A-Za-z][A-Za-z0-9_]{0,31}\z", Options);
        public static readonly Regex RuleIdPattern = new Regex(@"^[a-z0-9_]+\.[a-z0-9_]+\.(pos|neg)\z", Options);
        public static readonly Regex VersionPattern = new Regex(@"^[a-z0-9][a-z0-9._-]*\z", Options);
        public static readonly Regex ChemOntPattern = new Regex(@"^CHEMONTID:[0-9]{7}\z", Options);
        public static readonly Regex SymbolPattern = new Regex(@"^[A-Z][a-z]?\z", Options);
        public static readonly Regex SourceTokenPattern = new Regex(@"^src:(0|[1-9][0-9]{0,5})\z", Options);
        public static readonly Regex DatePattern = new Regex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}\z", Options);

        public static readonly string[] Origins = { "as_published", "tuned_vs20", "tuned_pooled" };
        public static readonly string[] MotifOrigins = { "source_defined", "tuned_vs20", "tuned_pooled" };
        public static readonly string[] UnitClasses = { "sugar", "acyl", "conjugate" };
        public static readonly string[] RequiredUnits = { "Hex", "dHex", "Pen", "HexA" };
        public static readonly string[] Variables = { "n_units", "n_sugars", "n_hex", "n_dhex", "n_pen", "n_hexa", "n_acyl" };
        public static readonly string[] OutOfScopeVariables = { "n_chains" };
        public static readonly string[] Roles = { "required", "counted", "ordering", "veto_present", "veto_stronger_than", "upgrade" };
        public static readonly string[] SupportRoles = { "required", "counted", "ordering" };
        public static readonly string[] VetoRoles = { "veto_present", "veto_stronger_than" };
        public static readonly string[] Kinds = { "product_ion", "loss_from_precursor", "aglycone_ion", "ion_series", "window" };
        public static readonly string[] OutOfScopeKinds = { "chain", "chains", "chain_set" };
        public static readonly string[] InstrumentClasses = { "QTOF-type", "Orbitrap/FT", "other", "unknown" };
        public static readonly string[] CalibrationClasses = { "QTOF-type", "Orbitrap/FT", "other" };

        private static readonly string[] AllKindFields = { "formula", "unit", "scaffold_ions", "form", "charge_carrier", "plus", "minus", "unit_classes", "as", "half_width_da" };
        private static readonly string[] TermKeys = { "term_id", "kind", "role", "term_source", "min_rel_intensity", "floor_source", "than", "ratio", "ratio_source", "note" };
        private static readonly string[] RuleKeys = {
            "format", "format_version", "rule_id", "version", "source", "transcribed_by", "source_checked",
            "author_reviewed", "measure", "review", "parameters_origin", "primary_precondition_mode",
            "calibrated_on", "convention", "product_tolerance", "loss_reference", "veto_excludes_motif", "claim",
            "preconditions", "terms", "complementary", "logic", "notes",
        };
        private static readonly string[] ReviewKeys = {
            "by", "at", "basis", "run_id", "rules_precision_sha256", "review_proposal_sha256",
            "rule_semantic_sha256", "instrument_classes", "product_tolerance", "precursor_tolerance_da",
        };

        /// <summary>Refused keys anywhere (design 3.5) and their codes.</summary>
        public static readonly Dictionary<string, string> RefusedKeys = BuildRefusedKeys();

        private static Dictionary<string, string> BuildRefusedKeys() {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var k in new[] { "element_gate", "oracle_formula", "predicted_formula", "precursor_formula", "assigned_formula", "library_formula", "inchikey" }) {
                d[k] = RuleErrorCodes.FormulaDependentKey;
            }
            foreach (var k in new[] { "mz", "mass", "exact_mass", "monoisotopic_mass", "printed_mz" }) {
                d[k] = RuleErrorCodes.TypedMass;
            }
            foreach (var k in new[] { "chain", "chains", "chain_set", "max_rel_intensity" }) {
                d[k] = RuleErrorCodes.OutOfScopeKind;
            }
            return d;
        }

        public static bool In(string value, string[] set) => Array.IndexOf(set, value) >= 0;

        public static bool InSet(JsonValue? value, string[] set) => value is JsonString s && In(s.Value, set);

        // ------------------------------------------------------------------------------------- generic validators

        public static void CheckKeys(ErrorSink c, JsonObject obj, string ptr, string[] required, string[]? optional = null) {
            foreach (var k in obj.Keys) {
                if (RefusedKeys.ContainsKey(k)) {
                    continue;
                }
                if (!In(k, required) && (optional is null || !In(k, optional))) {
                    c.Err(RuleErrorCodes.UnknownKey, Pointers.Child(ptr, k));
                }
            }
            foreach (var k in required) {
                if (!obj.ContainsKey(k)) {
                    c.Err(RuleErrorCodes.MissingKey, Pointers.Child(ptr, k));
                }
            }
        }

        public static void ScanRefused(ErrorSink c, JsonValue root) {
            var stack = new Stack<KeyValuePair<JsonValue, string>>();
            stack.Push(new KeyValuePair<JsonValue, string>(root, ""));
            while (stack.Count > 0) {
                var top = stack.Pop();
                if (top.Key is JsonObject o) {
                    foreach (var m in o.Members) {
                        var p = Pointers.Child(top.Value, m.Key);
                        if (RefusedKeys.TryGetValue(m.Key, out var code)) {
                            c.Err(code, p);
                        }
                        stack.Push(new KeyValuePair<JsonValue, string>(m.Value, p));
                    }
                }
                else if (top.Key is JsonArray a) {
                    for (var i = 0; i < a.Count; i++) {
                        stack.Push(new KeyValuePair<JsonValue, string>(a[i], Pointers.Child(top.Value, i)));
                    }
                }
            }
        }

        public static bool VObj(ErrorSink c, JsonValue? v, string ptr) {
            if (!(v is JsonObject)) {
                c.Err(RuleErrorCodes.BadType, ptr);
                return false;
            }
            return true;
        }

        public static bool VList(ErrorSink c, JsonValue? v, string ptr, int minLen = 0) {
            if (!(v is JsonArray a)) {
                c.Err(RuleErrorCodes.BadType, ptr);
                return false;
            }
            if (a.Count < minLen) {
                c.Err(RuleErrorCodes.BadValue, ptr);
                return false;
            }
            return true;
        }

        public static bool VStr(ErrorSink c, JsonValue? v, string ptr, Regex? pattern = null, int? maxLen = null,
                                bool nullable = false, bool nonEmpty = true) {
            if (v is null) {
                return false;
            }
            if (v is JsonNull && nullable) {
                return true;
            }
            if (!(v is JsonString s)) {
                c.Err(RuleErrorCodes.BadType, ptr);
                return false;
            }
            if ((nonEmpty && s.Value.Length == 0) || (maxLen.HasValue && s.Value.Length > maxLen.Value)
                || (pattern != null && !pattern.IsMatch(s.Value))) {
                c.Err(RuleErrorCodes.BadValue, ptr);
                return false;
            }
            return true;
        }

        public static bool VEnum(ErrorSink c, JsonValue? v, string ptr, string[] allowed) {
            if (v is null) {
                return false;
            }
            if (!(v is JsonString s)) {
                c.Err(RuleErrorCodes.BadType, ptr);
                return false;
            }
            if (!In(s.Value, allowed)) {
                c.Err(RuleErrorCodes.BadValue, ptr);
                return false;
            }
            return true;
        }

        public static bool VBool(ErrorSink c, JsonValue? v, string ptr) {
            if (v is null) {
                return false;
            }
            if (!(v is JsonBoolean)) {
                c.Err(RuleErrorCodes.BadType, ptr);
                return false;
            }
            return true;
        }

        public static bool VInt(ErrorSink c, JsonValue? v, string ptr, long? minimum = null, string code = RuleErrorCodes.BadValue) {
            if (v is null) {
                return false;
            }
            if (!(v is JsonInteger i)) {
                c.Err(RuleErrorCodes.BadType, ptr);
                return false;
            }
            if (minimum.HasValue && i.Value < minimum.Value) {
                c.Err(code, ptr);
                return false;
            }
            return true;
        }

        public static bool VFloat(ErrorSink c, JsonValue? v, string ptr, bool positive = false, double? maximum = null, bool nullable = false) {
            if (v is null) {
                return false;
            }
            if (v is JsonNull && nullable) {
                return true;
            }
            if (!(v is JsonFloat f)) {
                c.Err(RuleErrorCodes.BadType, ptr);
                return false;
            }
            if ((positive && !(f.Value > 0.0)) || f.Value < 0.0 || (maximum.HasValue && f.Value > maximum.Value)) {
                c.Err(RuleErrorCodes.BadValue, ptr);
                return false;
            }
            return true;
        }

        public static FormulaOutcome VFormula(ErrorSink c, JsonValue? v, string ptr, ElementTable elements, out long[]? comp, bool nullable = false) {
            comp = null;
            if (v is null) {
                return FormulaOutcome.Missing;
            }
            if (v is JsonNull && nullable) {
                return FormulaOutcome.Null;
            }
            if (!(v is JsonString s)) {
                c.Err(RuleErrorCodes.BadType, ptr);
                return FormulaOutcome.Error;
            }
            try {
                comp = Composition.Parse(s.Value, elements);
                return FormulaOutcome.Parsed;
            }
            catch (StrictJsonException e) {
                c.Err(e.Code, ptr);
                return FormulaOutcome.Error;
            }
        }

        public static void VFormatHeader(ErrorSink c, JsonObject obj, string format) {
            var f = obj.Get("format");
            if (f != null && !(f is JsonString fs && fs.Value == format)) {
                c.Err(RuleErrorCodes.BadValue, "/format");
            }
            var v = obj.Get("format_version");
            if (v is null) {
                return;
            }
            if (!(v is JsonInteger i)) {
                c.Err(RuleErrorCodes.BadType, "/format_version");
            }
            else if (i.Value != 1) {
                c.Err(RuleErrorCodes.BadValue, "/format_version");
            }
        }

        public static void VCitation(ErrorSink c, JsonValue? v, string ptr) {
            if (!VObj(c, v, ptr)) {
                return;
            }
            var o = (JsonObject)v!;
            if (o.ContainsKey("doi")) {
                CheckKeys(c, o, ptr, new[] { "doi", "locator" });
                VStr(c, o["doi"], Pointers.Child(ptr, "doi"), pattern: DoiPattern);
            }
            else if (o.ContainsKey("reference")) {
                CheckKeys(c, o, ptr, new[] { "reference", "locator" });
                VStr(c, o["reference"], Pointers.Child(ptr, "reference"), maxLen: NoteMax);
            }
            else {
                c.Err(RuleErrorCodes.MissingKey, Pointers.Child(ptr, "doi"));
                CheckKeys(c, o, ptr, new[] { "locator" });
                return;
            }
            VStr(c, o.Get("locator"), Pointers.Child(ptr, "locator"), maxLen: NoteMax);
        }

        public static void VCitations(ErrorSink c, JsonValue? v, string ptr, int minLen = 0) {
            if (!VList(c, v, ptr, minLen)) {
                return;
            }
            var a = (JsonArray)v!;
            for (var i = 0; i < a.Count; i++) {
                VCitation(c, a[i], Pointers.Child(ptr, i));
            }
        }

        /// <summary>A provenance token -> its origin, or null after an error (or for a missing value).</summary>
        public static string? VToken(ErrorSink c, JsonValue? v, string ptr, int? nSource, bool srcOnly = false) {
            if (v is null) {
                return null;
            }
            if (!(v is JsonString s)) {
                c.Err(RuleErrorCodes.BadType, ptr);
                return null;
            }
            if ((s.Value == "tuned_vs20" || s.Value == "tuned_pooled") && !srcOnly) {
                return s.Value;
            }
            var m = SourceTokenPattern.Match(s.Value);
            if (!m.Success) {
                c.Err(RuleErrorCodes.BadValue, ptr);
                return null;
            }
            if (nSource.HasValue && Composition.ParseDigits(m.Groups[1].Value) >= nSource.Value) {
                c.Err(RuleErrorCodes.UnknownSourceRef, ptr);
                return null;
            }
            return "as_published";
        }

        /// <summary>[min, max] of non-negative ints, or null after an error.</summary>
        public static bool VRangePair(ErrorSink c, JsonValue? v, string ptr) {
            if (!(v is JsonArray a) || a.Count != 2 || !(a[0] is JsonInteger lo) || !(a[1] is JsonInteger hi)) {
                c.Err(RuleErrorCodes.BadType, ptr);
                return false;
            }
            if (lo.Value < 0 || hi.Value < 0) {
                c.Err(RuleErrorCodes.NegativeCount, ptr);
                return false;
            }
            if (lo.Value > hi.Value) {
                c.Err(RuleErrorCodes.BadValue, ptr);
                return false;
            }
            return true;
        }

        public static void VRowOrigin(ErrorSink c, JsonObject row, string ptr, string[] origins) {
            var ok = VEnum(c, row.Get("origin"), Pointers.Child(ptr, "origin"), origins);
            var src = row.Get("source");
            if (src != null) {
                VCitations(c, src, Pointers.Child(ptr, "source"));
            }
            if (ok && ((JsonString)row["origin"]).Value == origins[0] && src is JsonArray a && a.Count == 0) {
                c.Err(RuleErrorCodes.MissingSource, Pointers.Child(ptr, "source"));
            }
        }

        /// <summary>Python's <c>len(set(map(str, values))) != len(values)</c>.</summary>
        public static bool HasRepeats(JsonArray values) {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var x in values) {
                seen.Add(PythonText.Str(x));
            }
            return seen.Count != values.Count;
        }

        // ------------------------------------------------------------------------------------------------ tables

        public static ElementTable? LoadElements(ErrorSink c, JsonObject doc) {
            CheckKeys(c, doc, "", new[] { "format", "format_version", "electron", "elements" });
            var n0 = c.N;
            VFormatHeader(c, doc, "msfinder-diagnostic-elements");
            var electron = doc.Get("electron");
            var electronOk = false;
            if (VFloat(c, electron, "/electron", positive: true)) {
                try {
                    DecimalLexeme.ToPda((JsonFloat)electron!);
                    electronOk = true;
                }
                catch (StrictJsonException e) {
                    c.Err(e.Code, "/electron");
                }
            }
            var masses = new List<KeyValuePair<string, JsonFloat>>();
            var els = doc.Get("elements");
            if (els != null && VObj(c, els, "/elements")) {
                var o = (JsonObject)els;
                if (o.Count == 0) {
                    c.Err(RuleErrorCodes.BadValue, "/elements");
                }
                foreach (var m in o.Members) {
                    var p = Pointers.Child("/elements", m.Key);
                    if (!SymbolPattern.IsMatch(m.Key)) {
                        c.Err(RuleErrorCodes.BadValue, p);
                        continue;
                    }
                    if (VFloat(c, m.Value, p, positive: true)) {
                        try {
                            DecimalLexeme.ToPda((JsonFloat)m.Value);
                            masses.Add(new KeyValuePair<string, JsonFloat>(m.Key, (JsonFloat)m.Value));
                        }
                        catch (StrictJsonException e) {
                            c.Err(e.Code, p);
                        }
                    }
                }
            }
            if (c.N != n0 || !electronOk) {
                return null;
            }
            return new ElementTable(masses, (JsonFloat)electron!);
        }

        public static Model.AdductRow? VAdductRow(ErrorSink c, JsonValue row, string ptr, ElementTable el) {
            if (!VObj(c, row, ptr)) {
                return null;
            }
            var o = (JsonObject)row;
            var n0 = c.N;
            CheckKeys(c, o, ptr, new[] { "adduct", "aliases", "charge", "multiplier", "add", "remove", "common", "origin", "source" });
            var name = o.Get("adduct");
            var nameOk = VStr(c, name, Pointers.Child(ptr, "adduct"), pattern: Composition.AdductPattern);
            var aliases = o.Get("aliases");
            if (aliases != null && VList(c, aliases, Pointers.Child(ptr, "aliases"))) {
                var a = (JsonArray)aliases;
                for (var i = 0; i < a.Count; i++) {
                    VStr(c, a[i], Pointers.Child(ptr, "aliases", i), maxLen: 64);
                }
            }
            var charge = o.Get("charge");
            if (VInt(c, charge, Pointers.Child(ptr, "charge")) && ((JsonInteger)charge!).Value != 1 && ((JsonInteger)charge).Value != -1) {
                c.Err(RuleErrorCodes.BadValue, Pointers.Child(ptr, "charge"));
            }
            VInt(c, o.Get("multiplier"), Pointers.Child(ptr, "multiplier"), minimum: 1);
            VFormula(c, o.Get("add"), Pointers.Child(ptr, "add"), el, out var add, nullable: true);
            VFormula(c, o.Get("remove"), Pointers.Child(ptr, "remove"), el, out var remove, nullable: true);
            VBool(c, o.Get("common"), Pointers.Child(ptr, "common"));
            VRowOrigin(c, o, ptr, Origins);
            if (c.N != n0) {
                return null;
            }
            add ??= new long[el.Count];
            remove ??= new long[el.Count];
            var parsed = nameOk ? Composition.ParseAdductName(((JsonString)name!).Value, el) : null;
            if (parsed is null) {
                c.Err(RuleErrorCodes.AdductNameMismatch, Pointers.Child(ptr, "adduct"));
                return null;
            }
            var chargeValue = ((JsonInteger)o["charge"]).Value;
            if ((parsed.Sign == 1) != (chargeValue == 1) || !parsed.ChargeCount.IsOne) {
                c.Err(RuleErrorCodes.AdductPolarity, Pointers.Child(ptr, "charge"));
                return null;
            }
            var ownNet = new Dictionary<string, long>(StringComparer.Ordinal);
            for (var i = 0; i < el.Count; i++) {
                var n = add[i] - remove[i];
                if (n != 0) {
                    ownNet[el.Symbols[i]] = n;
                }
            }
            var sameNet = ownNet.Count == parsed.Net.Count;
            if (sameNet) {
                foreach (var kv in parsed.Net) {
                    if (!ownNet.TryGetValue(kv.Key, out var n) || kv.Value != n) {
                        sameNet = false;
                        break;
                    }
                }
            }
            if (parsed.Multiplier != ((JsonInteger)o["multiplier"]).Value || !sameNet) {
                c.Err(RuleErrorCodes.AdductNameMismatch, Pointers.Child(ptr, "adduct"));
                return null;
            }
            return new Model.AdductRow(o, el, add, remove);
        }

        public static Model.UnitRow? VUnitRow(ErrorSink c, JsonValue row, string ptr, ElementTable el) {
            if (!VObj(c, row, ptr)) {
                return null;
            }
            var o = (JsonObject)row;
            var n0 = c.N;
            CheckKeys(c, o, ptr, new[] { "name", "class", "residue", "origin", "source" });
            VStr(c, o.Get("name"), Pointers.Child(ptr, "name"), pattern: UnitPattern);
            VEnum(c, o.Get("class"), Pointers.Child(ptr, "class"), UnitClasses);
            VFormula(c, o.Get("residue"), Pointers.Child(ptr, "residue"), el, out var residue);
            VRowOrigin(c, o, ptr, Origins);
            if (c.N != n0) {
                return null;
            }
            return new Model.UnitRow(o, residue!);
        }

        public static JsonObject? VMotifRow(ErrorSink c, JsonValue row, string ptr) {
            if (!VObj(c, row, ptr)) {
                return null;
            }
            var o = (JsonObject)row;
            var n0 = c.N;
            CheckKeys(c, o, ptr, new[] { "motif_id", "smarts", "origin", "source", "chemont" });
            VStr(c, o.Get("motif_id"), Pointers.Child(ptr, "motif_id"), pattern: IdPattern);
            VStr(c, o.Get("smarts"), Pointers.Child(ptr, "smarts"), maxLen: 2000);
            VRowOrigin(c, o, ptr, MotifOrigins);
            var chem = o.Get("chemont");
            if (chem != null && VObj(c, chem, Pointers.Child(ptr, "chemont"))) {
                var co = (JsonObject)chem;
                CheckKeys(c, co, Pointers.Child(ptr, "chemont"), new[] { "compatible", "broader" });
                foreach (var key in new[] { "compatible", "broader" }) {
                    var nodes = co.Get(key);
                    if (nodes is null || !VList(c, nodes, Pointers.Child(ptr, "chemont", key))) {
                        continue;
                    }
                    var a = (JsonArray)nodes;
                    for (var i = 0; i < a.Count; i++) {
                        var p = Pointers.Child(Pointers.Child(ptr, "chemont", key), i);
                        if (VObj(c, a[i], p)) {
                            var node = (JsonObject)a[i];
                            CheckKeys(c, node, p, new[] { "id", "name" });
                            VStr(c, node.Get("id"), Pointers.Child(p, "id"), pattern: ChemOntPattern);
                            VStr(c, node.Get("name"), Pointers.Child(p, "name"), maxLen: NoteMax);
                        }
                    }
                }
            }
            return c.N == n0 ? o : null;
        }

        public static JsonObject? VNestingRow(ErrorSink c, JsonValue row, string ptr) {
            if (!VObj(c, row, ptr)) {
                return null;
            }
            var o = (JsonObject)row;
            var n0 = c.N;
            CheckKeys(c, o, ptr, new[] { "child", "parent", "source" });
            VStr(c, o.Get("child"), Pointers.Child(ptr, "child"), pattern: IdPattern);
            VStr(c, o.Get("parent"), Pointers.Child(ptr, "parent"), pattern: IdPattern);
            if (o.ContainsKey("source")) {
                VCitations(c, o["source"], Pointers.Child(ptr, "source"), minLen: 1);
            }
            return c.N == n0 ? o : null;
        }

        public static Model.ScaffoldList? VScaffoldList(ErrorSink c, JsonObject doc, string stem, ElementTable el) {
            var n0 = c.N;
            CheckKeys(c, doc, "", new[] { "list_id", "entries" });
            var listId = doc.Get("list_id");
            if (VStr(c, listId, "/list_id", pattern: IdPattern) && ((JsonString)listId!).Value != stem) {
                c.Err(RuleErrorCodes.FileNameMismatch, "/list_id");
            }
            var entries = doc.Get("entries");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (entries != null && VList(c, entries, "/entries", minLen: 1)) {
                var a = (JsonArray)entries;
                for (var i = 0; i < a.Count; i++) {
                    var p = Pointers.Child("/entries", i);
                    if (!VObj(c, a[i], p)) {
                        continue;
                    }
                    var e = (JsonObject)a[i];
                    CheckKeys(c, e, p, new[] { "scaffold_id", "name", "formula", "class", "ions", "origin", "source" });
                    var sid = e.Get("scaffold_id");
                    if (VStr(c, sid, Pointers.Child(p, "scaffold_id"), pattern: IdPattern)) {
                        if (!seen.Add(((JsonString)sid!).Value)) {
                            c.Err(RuleErrorCodes.DuplicateId, Pointers.Child(p, "scaffold_id"));
                        }
                    }
                    VStr(c, e.Get("name"), Pointers.Child(p, "name"), maxLen: NoteMax);
                    VFormula(c, e.Get("formula"), Pointers.Child(p, "formula"), el, out _);
                    VStr(c, e.Get("class"), Pointers.Child(p, "class"), pattern: IdPattern);
                    var ions = e.Get("ions");
                    if (ions != null && VObj(c, ions, Pointers.Child(p, "ions"))) {
                        foreach (var key in ((JsonObject)ions).Members) {
                            var pk = Pointers.Child(Pointers.Child(p, "ions"), key.Key);
                            if (!IdPattern.IsMatch(key.Key)) {
                                c.Err(RuleErrorCodes.BadValue, pk);
                                continue;
                            }
                            if (!VObj(c, key.Value, pk)) {
                                continue;
                            }
                            var modes = (JsonObject)key.Value;
                            CheckKeys(c, modes, pk, new[] { "pos", "neg" });
                            foreach (var mode in new[] { "pos", "neg" }) {
                                var lst = modes.Get(mode);
                                if (lst is null || !VList(c, lst, Pointers.Child(pk, mode))) {
                                    continue;
                                }
                                var la = (JsonArray)lst;
                                for (var j = 0; j < la.Count; j++) {
                                    VFormula(c, la[j], Pointers.Child(Pointers.Child(pk, mode), j), el, out _);
                                }
                            }
                        }
                    }
                    VRowOrigin(c, e, p, Origins);
                }
            }
            if (c.N != n0) {
                return null;
            }
            return BuildScaffoldList(doc, el);
        }

        private static Model.ScaffoldList BuildScaffoldList(JsonObject doc, ElementTable el) {
            var entries = new List<Model.ScaffoldEntry>();
            foreach (var x in (JsonArray)doc["entries"]) {
                var e = (JsonObject)x;
                var keys = new List<string>();
                var ions = new Dictionary<string, Model.ScaffoldIons>(StringComparer.Ordinal);
                foreach (var key in ((JsonObject)e["ions"]).Members) {
                    var modes = (JsonObject)key.Value;
                    keys.Add(key.Key);
                    ions[key.Key] = new Model.ScaffoldIons(
                        Model.Validated.Strings((JsonArray)modes["pos"]), Comps((JsonArray)modes["pos"], el),
                        Model.Validated.Strings((JsonArray)modes["neg"]), Comps((JsonArray)modes["neg"], el));
                }
                entries.Add(new Model.ScaffoldEntry(e, Composition.Parse(((JsonString)e["formula"]).Value, el),
                                                    Model.Validated.List(keys), ions));
            }
            return new Model.ScaffoldList(doc, Model.Validated.List(entries));
        }

        private static IReadOnlyList<long[]> Comps(JsonArray formulas, ElementTable el) {
            var list = new List<long[]>();
            foreach (var f in formulas) {
                list.Add(Composition.Parse(((JsonString)f).Value, el));
            }
            return Model.Validated.List(list);
        }

        // ---------------------------------------------------------------------------------- rule schema (phase A)

        private sealed class LogicStats
        {
            public int Nodes;
            public int Depth;
        }

        private static void WalkLogicSchema(ErrorSink c, JsonValue node, string ptr, int? nSource, LogicStats stats, int depth) {
            stats.Nodes++;
            stats.Depth = Math.Max(stats.Depth, depth);
            if (stats.Nodes > MaxLogicNodes || depth > MaxLogicDepth) {
                return;
            }
            if (!(node is JsonObject o) || o.Count != 1) {
                c.Err(RuleErrorCodes.LogicNode, ptr);
                return;
            }
            var key = o.Members[0].Key;
            var val = o.Members[0].Value;
            var p = Pointers.Child(ptr, key);
            switch (key) {
                case "term":
                    if (!(val is JsonString)) {
                        c.Err(RuleErrorCodes.BadType, p);
                    }
                    return;
                case "all":
                case "any": {
                    if (!(val is JsonArray a) || a.Count == 0) {
                        c.Err(RuleErrorCodes.LogicNode, p);
                        return;
                    }
                    for (var i = 0; i < a.Count; i++) {
                        WalkLogicSchema(c, a[i], Pointers.Child(p, i), nSource, stats, depth + 1);
                    }
                    return;
                }
                case "not":
                    WalkLogicSchema(c, val, p, nSource, stats, depth + 1);
                    return;
                case "k_of": {
                    if (!(val is JsonObject kof)) {
                        c.Err(RuleErrorCodes.LogicNode, p);
                        return;
                    }
                    CheckKeys(c, kof, p, new[] { "k", "k_source", "of" });
                    var k = kof.Get("k");
                    if (k is JsonInteger ki) {
                        if (ki.Value < 1) {
                            c.Err(RuleErrorCodes.KRange, Pointers.Child(p, "k"));
                        }
                    }
                    else if (k is JsonObject ko) {
                        var pk = Pointers.Child(p, "k");
                        CheckKeys(c, ko, pk, new[] { "var", "offset", "min" });
                        var variable = ko.Get("var");
                        if (variable != null) {
                            if (!(variable is JsonString vs)) {
                                c.Err(RuleErrorCodes.BadType, Pointers.Child(pk, "var"));
                            }
                            else if (In(vs.Value, OutOfScopeVariables)) {
                                c.Err(RuleErrorCodes.OutOfScopeVariable, Pointers.Child(pk, "var"));
                            }
                            else if (!In(vs.Value, Variables)) {
                                c.Err(RuleErrorCodes.UnknownVariable, Pointers.Child(pk, "var"));
                            }
                        }
                        VInt(c, ko.Get("offset"), Pointers.Child(pk, "offset"));
                        VInt(c, ko.Get("min"), Pointers.Child(pk, "min"), minimum: 1, code: RuleErrorCodes.KRange);
                    }
                    else if (k != null) {
                        c.Err(RuleErrorCodes.BadType, Pointers.Child(p, "k"));
                    }
                    VToken(c, kof.Get("k_source"), Pointers.Child(p, "k_source"), nSource);
                    var of = kof.Get("of");
                    if (of is null) {
                        return;
                    }
                    if (!(of is JsonArray ofa) || ofa.Count == 0) {
                        c.Err(RuleErrorCodes.LogicNode, Pointers.Child(p, "of"));
                        return;
                    }
                    for (var i = 0; i < ofa.Count; i++) {
                        WalkLogicSchema(c, ofa[i], Pointers.Child(p, "of", i), nSource, stats, depth + 1);
                    }
                    return;
                }
                default:
                    c.Err(RuleErrorCodes.LogicNode, ptr);
                    return;
            }
        }

        private static string[] Concat(string[] a, IList<string> b) {
            var r = new string[a.Length + b.Count];
            a.CopyTo(r, 0);
            for (var i = 0; i < b.Count; i++) {
                r[a.Length + i] = b[i];
            }
            return r;
        }

        private static void VTerm(ErrorSink c, JsonValue t, string ptr, int? nSource, string? convention, ElementTable el) {
            if (!VObj(c, t, ptr)) {
                return;
            }
            var o = (JsonObject)t;
            var kind = o.Get("kind");
            var kindOk = false;
            if (kind != null) {
                if (!(kind is JsonString ks)) {
                    c.Err(RuleErrorCodes.BadType, Pointers.Child(ptr, "kind"));
                }
                else if (In(ks.Value, OutOfScopeKinds)) {
                    c.Err(RuleErrorCodes.OutOfScopeKind, Pointers.Child(ptr, "kind"));
                }
                else if (!In(ks.Value, Kinds)) {
                    c.Err(RuleErrorCodes.BadValue, Pointers.Child(ptr, "kind"));
                }
                else {
                    kindOk = true;
                }
            }
            if (!kindOk) {
                CheckKeys(c, o, ptr, TermKeys, AllKindFields);
                return;
            }
            var kindName = ((JsonString)kind!).Value;
            string[] fields;
            if (kindName == "product_ion" || kindName == "loss_from_precursor") {
                var choices = kindName == "product_ion" ? new[] { "formula", "unit", "scaffold_ions" } : new[] { "formula", "unit" };
                var present = new List<string>();
                foreach (var k in choices) {
                    if (o.ContainsKey(k)) {
                        present.Add(k);
                    }
                }
                if (present.Count != 1) {
                    c.Err(RuleErrorCodes.KindFieldChoice, ptr);
                }
                fields = present.Count > 0 ? new[] { present[0] } : new string[0];
                CheckKeys(c, o, ptr, Concat(TermKeys, fields), present.Count > 1 ? present.GetRange(1, present.Count - 1).ToArray() : new string[0]);
            }
            else {
                fields = kindName == "aglycone_ion" ? new[] { "form", "charge_carrier", "plus", "minus" }
                    : kindName == "ion_series" ? new[] { "unit_classes", "charge_carrier", "form", "plus", "minus" }
                    : new[] { "formula", "as", "half_width_da" };
                CheckKeys(c, o, ptr, Concat(TermKeys, fields));
            }
            VStr(c, o.Get("term_id"), Pointers.Child(ptr, "term_id"), pattern: TermIdPattern);
            var role = o.Get("role");
            if (VEnum(c, role, Pointers.Child(ptr, "role"), Roles) && kindName == "window" && !InSet(role, VetoRoles)) {
                c.Err(RuleErrorCodes.RolePlacement, Pointers.Child(ptr, "role"));
            }
            VToken(c, o.Get("term_source"), Pointers.Child(ptr, "term_source"), nSource);
            VToken(c, o.Get("floor_source"), Pointers.Child(ptr, "floor_source"), nSource);
            double? maximum = convention == "p2" ? 1.0 : convention == "msdial" ? 100.0 : (double?)null;
            VFloat(c, o.Get("min_rel_intensity"), Pointers.Child(ptr, "min_rel_intensity"), maximum: maximum);
            var than = o.Get("than");
            if (than != null && !(than is JsonNull) && VList(c, than, Pointers.Child(ptr, "than"), minLen: 1)) {
                var a = (JsonArray)than;
                for (var i = 0; i < a.Count; i++) {
                    VStr(c, a[i], Pointers.Child(ptr, "than", i), pattern: TermIdPattern);
                }
            }
            if (than != null && InSet(role, Roles)) {
                var needsThan = InSet(role, new[] { "ordering", "veto_stronger_than" });
                if (needsThan != !(than is JsonNull)) {
                    c.Err(RuleErrorCodes.BadValue, Pointers.Child(ptr, "than"));
                }
            }
            var ratio = o.Get("ratio");
            if (VFloat(c, ratio, Pointers.Child(ptr, "ratio"), positive: true, nullable: true) && !(ratio is JsonNull) && than is JsonNull) {
                c.Err(RuleErrorCodes.BadValue, Pointers.Child(ptr, "ratio"));
            }
            var rs = o.Get("ratio_source");
            if (rs != null && ratio != null) {
                if ((rs is JsonNull) != (ratio is JsonNull)) {
                    c.Err(RuleErrorCodes.BadValue, Pointers.Child(ptr, "ratio_source"));
                }
                else if (!(rs is JsonNull)) {
                    VToken(c, rs, Pointers.Child(ptr, "ratio_source"), nSource);
                }
            }
            VStr(c, o.Get("note"), Pointers.Child(ptr, "note"), maxLen: NoteMax, nullable: true);
            if (In("formula", fields)) {
                VFormula(c, o.Get("formula"), Pointers.Child(ptr, "formula"), el, out _);
            }
            if (In("unit", fields)) {
                var u = o.Get("unit");
                var pu = Pointers.Child(ptr, "unit");
                if (u != null && VObj(c, u, pu)) {
                    var uo = (JsonObject)u;
                    var sel = new List<string>();
                    foreach (var k in new[] { "class", "name" }) {
                        if (uo.ContainsKey(k)) {
                            sel.Add(k);
                        }
                    }
                    if (sel.Count != 1) {
                        c.Err(RuleErrorCodes.KindFieldChoice, pu);
                    }
                    var first = sel.Count > 0 ? new[] { sel[0] } : new string[0];
                    CheckKeys(c, uo, pu, Concat(first, new[] { "plus", "minus" }), sel.Count > 1 ? sel.GetRange(1, sel.Count - 1).ToArray() : new string[0]);
                    if (sel.Count > 0 && sel[0] == "class") {
                        VEnum(c, uo["class"], Pointers.Child(pu, "class"), UnitClasses);
                    }
                    if (sel.Count > 0 && sel[0] == "name") {
                        VStr(c, uo["name"], Pointers.Child(pu, "name"), pattern: UnitPattern);
                    }
                    VFormula(c, uo.Get("plus"), Pointers.Child(pu, "plus"), el, out _, nullable: true);
                    VFormula(c, uo.Get("minus"), Pointers.Child(pu, "minus"), el, out _, nullable: true);
                }
            }
            if (In("scaffold_ions", fields)) {
                VStr(c, o.Get("scaffold_ions"), Pointers.Child(ptr, "scaffold_ions"), pattern: IdPattern);
            }
            if (In("form", fields)) {
                var formOk = VEnum(c, o.Get("form"), Pointers.Child(ptr, "form"), new[] { "even", "radical" });
                var carrierOk = VEnum(c, o.Get("charge_carrier"), Pointers.Child(ptr, "charge_carrier"), new[] { "proton", "adduct" });
                if (formOk && carrierOk && ((JsonString)o["form"]).Value == "radical" && ((JsonString)o["charge_carrier"]).Value == "adduct") {
                    c.Err(RuleErrorCodes.BadValue, Pointers.Child(ptr, "charge_carrier"));
                }
                VFormula(c, o.Get("plus"), Pointers.Child(ptr, "plus"), el, out _, nullable: true);
                VFormula(c, o.Get("minus"), Pointers.Child(ptr, "minus"), el, out _, nullable: true);
            }
            if (In("unit_classes", fields)) {
                var uc = o.Get("unit_classes");
                if (uc != null && VList(c, uc, Pointers.Child(ptr, "unit_classes"), minLen: 1)) {
                    var a = (JsonArray)uc;
                    for (var i = 0; i < a.Count; i++) {
                        VEnum(c, a[i], Pointers.Child(ptr, "unit_classes", i), UnitClasses);
                    }
                    if (HasRepeats(a)) {
                        c.Err(RuleErrorCodes.BadValue, Pointers.Child(ptr, "unit_classes"));
                    }
                }
            }
            if (In("as", fields)) {
                VEnum(c, o.Get("as"), Pointers.Child(ptr, "as"), new[] { "ion", "loss" });
                VFloat(c, o.Get("half_width_da"), Pointers.Child(ptr, "half_width_da"), positive: true);
            }
        }

        /// <summary>A proleptic Gregorian date with year 1-9999, as Python's date.fromisoformat accepts it.</summary>
        public static bool IsValidDate(string yyyyMmDd) {
            var y = (int)Composition.ParseDigits(yyyyMmDd.Substring(0, 4));
            var m = (int)Composition.ParseDigits(yyyyMmDd.Substring(5, 2));
            var d = (int)Composition.ParseDigits(yyyyMmDd.Substring(8, 2));
            if (y < 1 || m < 1 || m > 12 || d < 1) {
                return false;
            }
            var leap = (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;
            int[] days = { 31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            return d <= days[m - 1];
        }

        private static void VReview(ErrorSink c, JsonValue r, string ptr) {
            if (r is JsonNull || !VObj(c, r, ptr)) {
                return;
            }
            var o = (JsonObject)r;
            CheckKeys(c, o, ptr, ReviewKeys);
            VStr(c, o.Get("by"), Pointers.Child(ptr, "by"), maxLen: NoteMax);
            var at = o.Get("at");
            if (VStr(c, at, Pointers.Child(ptr, "at"), pattern: DatePattern) && !IsValidDate(((JsonString)at!).Value)) {
                c.Err(RuleErrorCodes.BadValue, Pointers.Child(ptr, "at"));
            }
            VEnum(c, o.Get("basis"), Pointers.Child(ptr, "basis"), new[] { "benchmark" });
            VStr(c, o.Get("run_id"), Pointers.Child(ptr, "run_id"), maxLen: NoteMax);
            foreach (var k in new[] { "rules_precision_sha256", "review_proposal_sha256", "rule_semantic_sha256" }) {
                VStr(c, o.Get(k), Pointers.Child(ptr, k), pattern: ShaPattern);
            }
            var ic = o.Get("instrument_classes");
            if (ic != null && VList(c, ic, Pointers.Child(ptr, "instrument_classes"), minLen: 1)) {
                var a = (JsonArray)ic;
                for (var i = 0; i < a.Count; i++) {
                    VEnum(c, a[i], Pointers.Child(ptr, "instrument_classes", i), InstrumentClasses);
                }
                if (HasRepeats(a)) {
                    c.Err(RuleErrorCodes.BadValue, Pointers.Child(ptr, "instrument_classes"));
                }
            }
            var pt = o.Get("product_tolerance");
            var pp = Pointers.Child(ptr, "product_tolerance");
            if (pt != null && VObj(c, pt, pp)) {
                var po = (JsonObject)pt;
                CheckKeys(c, po, pp, new[] { "abs_da", "ppm", "source" });
                // A ppm-only MS-FINDER tolerance is {0, X, msfinder} as the evaluator applies it (design 4.4), so a
                // review measured at it records abs_da 0; every other review tolerance has a positive abs_da.
                var ppmOnly = po.Get("source") is JsonString src && src.Value == "msfinder"
                              && po.Get("ppm") is JsonFloat ppm && ppm.Value > 0.0;
                VFloat(c, po.Get("abs_da"), Pointers.Child(pp, "abs_da"), positive: !ppmOnly);
                VFloat(c, po.Get("ppm"), Pointers.Child(pp, "ppm"));
                VEnum(c, po.Get("source"), Pointers.Child(pp, "source"), new[] { "p2", "msfinder" });
            }
            VFloat(c, o.Get("precursor_tolerance_da"), Pointers.Child(ptr, "precursor_tolerance_da"), positive: true);
        }

        private static void VExamples(ErrorSink c, JsonValue v, string ptr, int minLen) {
            if (!VList(c, v, ptr, minLen)) {
                return;
            }
            var a = (JsonArray)v;
            for (var i = 0; i < a.Count; i++) {
                var p = Pointers.Child(ptr, i);
                if (VObj(c, a[i], p)) {
                    var x = (JsonObject)a[i];
                    CheckKeys(c, x, p, new[] { "pubchem_cid", "smiles" });
                    VInt(c, x.Get("pubchem_cid"), Pointers.Child(p, "pubchem_cid"), minimum: 1);
                    VStr(c, x.Get("smiles"), Pointers.Child(p, "smiles"), maxLen: 2000);
                }
            }
        }

        /// <summary>Phase A: schema, types, enums, patterns and formula grammar (design 3.6 item 1).</summary>
        public static void VRuleSchema(ErrorSink c, JsonObject doc, string stem, ElementTable el) {
            CheckKeys(c, doc, "", RuleKeys);
            VFormatHeader(c, doc, "msfinder-diagnostic-rule");
            var rid = doc.Get("rule_id");
            if (VStr(c, rid, "/rule_id", pattern: RuleIdPattern) && ((JsonString)rid!).Value != stem) {
                c.Err(RuleErrorCodes.FileNameMismatch, "/rule_id");
            }
            VInt(c, doc.Get("version"), "/version", minimum: 1);
            var src = doc.Get("source");
            int? nSource = null;
            if (src != null) {
                VCitations(c, src, "/source", minLen: 1);
                nSource = src is JsonArray sa ? sa.Count : (int?)null;
            }
            VStr(c, doc.Get("transcribed_by"), "/transcribed_by", maxLen: NoteMax);
            foreach (var k in new[] { "source_checked", "author_reviewed", "measure", "veto_excludes_motif" }) {
                VBool(c, doc.Get(k), "/" + k);
            }
            if (doc.ContainsKey("review")) {
                VReview(c, doc["review"], "/review");
            }
            VEnum(c, doc.Get("parameters_origin"), "/parameters_origin", Origins);
            VEnum(c, doc.Get("primary_precondition_mode"), "/primary_precondition_mode", new[] { "adduct_ambiguous", "mz_only" });
            var cal = doc.Get("calibrated_on");
            if (cal != null && VList(c, cal, "/calibrated_on")) {
                var a = (JsonArray)cal;
                for (var i = 0; i < a.Count; i++) {
                    VEnum(c, a[i], Pointers.Child("/calibrated_on", i), CalibrationClasses);
                }
                if (HasRepeats(a)) {
                    c.Err(RuleErrorCodes.BadValue, "/calibrated_on");
                }
            }
            var conventionValue = doc.Get("convention");
            string? convention = VEnum(c, conventionValue, "/convention", new[] { "p2", "msdial" }) ? ((JsonString)conventionValue!).Value : null;
            VEnum(c, doc.Get("product_tolerance"), "/product_tolerance", new[] { "p2", "msfinder" });
            VEnum(c, doc.Get("loss_reference"), "/loss_reference", new[] { "hypothesis", "observed" });
            VStr(c, doc.Get("notes"), "/notes", maxLen: NoteMax, nonEmpty: false);
            var claim = doc.Get("claim");
            if (claim != null && VObj(c, claim, "/claim")) {
                var co = (JsonObject)claim;
                CheckKeys(c, co, "/claim", new[] { "motif_id", "claim_term_ids", "claim_level", "non_claims", "positive_examples", "negative_examples" });
                VStr(c, co.Get("motif_id"), "/claim/motif_id", pattern: IdPattern);
                var ids = co.Get("claim_term_ids");
                if (ids != null && VList(c, ids, "/claim/claim_term_ids")) {
                    var a = (JsonArray)ids;
                    for (var i = 0; i < a.Count; i++) {
                        VStr(c, a[i], Pointers.Child("/claim/claim_term_ids", i), pattern: TermIdPattern);
                    }
                }
                VEnum(c, co.Get("claim_level"), "/claim/claim_level", new[] { "class", "scaffold", "substituent" });
                var nc = co.Get("non_claims");
                if (nc != null && VList(c, nc, "/claim/non_claims")) {
                    var a = (JsonArray)nc;
                    for (var i = 0; i < a.Count; i++) {
                        VStr(c, a[i], Pointers.Child("/claim/non_claims", i), maxLen: NoteMax);
                    }
                }
                if (co.ContainsKey("positive_examples")) {
                    VExamples(c, co["positive_examples"], "/claim/positive_examples", 2);
                }
                if (co.ContainsKey("negative_examples")) {
                    VExamples(c, co["negative_examples"], "/claim/negative_examples", 1);
                }
            }
            var pre = doc.Get("preconditions");
            if (pre != null && VObj(c, pre, "/preconditions")) {
                var po = (JsonObject)pre;
                CheckKeys(c, po, "/preconditions", new[] { "ion_mode", "adducts", "precursor_tolerance_da", "composition", "priors", "competitors" });
                var mode = po.Get("ion_mode");
                if (VEnum(c, mode, "/preconditions/ion_mode", new[] { "pos", "neg" }) && rid is JsonString ridString
                    && RuleIdPattern.IsMatch(ridString.Value)
                    && ridString.Value.Substring(ridString.Value.LastIndexOf('.') + 1) != ((JsonString)mode!).Value) {
                    c.Err(RuleErrorCodes.ModeMismatch, "/preconditions/ion_mode");
                }
                var ad = po.Get("adducts");
                if (ad != null && VList(c, ad, "/preconditions/adducts", minLen: 1)) {
                    var a = (JsonArray)ad;
                    for (var i = 0; i < a.Count; i++) {
                        VStr(c, a[i], Pointers.Child("/preconditions/adducts", i), maxLen: 64);
                    }
                    if (HasRepeats(a)) {
                        c.Err(RuleErrorCodes.BadValue, "/preconditions/adducts");
                    }
                }
                VFloat(c, po.Get("precursor_tolerance_da"), "/preconditions/precursor_tolerance_da", positive: true);
                var comp = po.Get("composition");
                const string pc = "/preconditions/composition";
                if (comp != null && VObj(c, comp, pc)) {
                    var cmp = (JsonObject)comp;
                    CheckKeys(c, cmp, pc, new[] { "core", "scaffold_list", "units", "unit_totals" });
                    var core = VFormula(c, cmp.Get("core"), Pointers.Child(pc, "core"), el, out _, nullable: true);
                    var sl = cmp.Get("scaffold_list");
                    VStr(c, sl, Pointers.Child(pc, "scaffold_list"), pattern: IdPattern, nullable: true);
                    if (core == FormulaOutcome.Null && sl is JsonNull) {
                        c.Err(RuleErrorCodes.BadValue, pc);
                    }
                    var units = cmp.Get("units");
                    if (units != null && VObj(c, units, Pointers.Child(pc, "units"))) {
                        foreach (var m in ((JsonObject)units).Members) {
                            if (!UnitPattern.IsMatch(m.Key)) {
                                c.Err(RuleErrorCodes.BadValue, Pointers.Child(pc, "units", m.Key));
                            }
                            else {
                                VRangePair(c, m.Value, Pointers.Child(pc, "units", m.Key));
                            }
                        }
                    }
                    var totals = cmp.Get("unit_totals");
                    if (totals != null && VObj(c, totals, Pointers.Child(pc, "unit_totals"))) {
                        foreach (var m in ((JsonObject)totals).Members) {
                            if (!In(m.Key, UnitClasses)) {
                                c.Err(RuleErrorCodes.BadValue, Pointers.Child(pc, "unit_totals", m.Key));
                            }
                            else {
                                VRangePair(c, m.Value, Pointers.Child(pc, "unit_totals", m.Key));
                            }
                        }
                    }
                }
                var priors = po.Get("priors");
                if (priors != null && VList(c, priors, "/preconditions/priors")) {
                    var a = (JsonArray)priors;
                    for (var i = 0; i < a.Count; i++) {
                        var p = Pointers.Child("/preconditions/priors", i);
                        if (VObj(c, a[i], p)) {
                            var x = (JsonObject)a[i];
                            CheckKeys(c, x, p, new[] { "source", "note" });
                            VToken(c, x.Get("source"), Pointers.Child(p, "source"), nSource, srcOnly: true);
                            VStr(c, x.Get("note"), Pointers.Child(p, "note"), maxLen: NoteMax);
                        }
                    }
                }
                var comps = po.Get("competitors");
                if (comps != null && VList(c, comps, "/preconditions/competitors")) {
                    var a = (JsonArray)comps;
                    for (var i = 0; i < a.Count; i++) {
                        var p = Pointers.Child("/preconditions/competitors", i);
                        if (VObj(c, a[i], p)) {
                            var x = (JsonObject)a[i];
                            CheckKeys(c, x, p, new[] { "motif_id", "note" });
                            VStr(c, x.Get("motif_id"), Pointers.Child(p, "motif_id"), pattern: IdPattern);
                            VStr(c, x.Get("note"), Pointers.Child(p, "note"), maxLen: NoteMax);
                        }
                    }
                }
            }
            var terms = doc.Get("terms");
            if (terms != null && VList(c, terms, "/terms", minLen: 1)) {
                var a = (JsonArray)terms;
                for (var i = 0; i < a.Count; i++) {
                    VTerm(c, a[i], Pointers.Child("/terms", i), nSource, convention, el);
                }
            }
            var pairs = doc.Get("complementary");
            if (pairs != null && VList(c, pairs, "/complementary")) {
                var a = (JsonArray)pairs;
                for (var i = 0; i < a.Count; i++) {
                    var p = Pointers.Child("/complementary", i);
                    if (!(a[i] is JsonArray pair) || pair.Count != 2) {
                        c.Err(RuleErrorCodes.BadType, p);
                        continue;
                    }
                    for (var j = 0; j < 2; j++) {
                        VStr(c, pair[j], Pointers.Child(p, j), pattern: TermIdPattern);
                    }
                }
            }
            if (doc.ContainsKey("logic")) {
                var stats = new LogicStats();
                WalkLogicSchema(c, doc["logic"], "/logic", nSource, stats, 1);
                if (stats.Nodes > MaxLogicNodes || stats.Depth > MaxLogicDepth) {
                    c.Err(RuleErrorCodes.LogicDepth, "/logic");
                }
            }
        }
    }
}
