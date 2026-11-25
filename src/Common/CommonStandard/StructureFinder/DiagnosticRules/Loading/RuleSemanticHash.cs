using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Loading
{
    /// <summary>
    /// <c>rule_semantic_sha256</c> (design 3.2): the sha256 of the canonical JSON of the rule without its review
    /// fields, every table row, scaffold list, motif row and element mass the rule resolves, and the engine
    /// semantics. A review is stale when any of them changes.
    /// </summary>
    public static class RuleSemanticHash
    {
        /// <summary>The engine semantics identity; bumped on any semantic change, which stales every review. It is
        /// <see cref="DiagnosticRuleEvaluator.EngineSemantics"/>, the single definition.</summary>
        public const string EngineSemantics = DiagnosticRuleEvaluator.EngineSemantics;

        public const string Format = "msfinder-diagnostic-rule-semantics";

        /// <summary>Python's <c>rule_semantic_sha256(rule, resolved, engine_semantics)</c>.</summary>
        public static string Compute(JsonObject rule, JsonValue resolved, string engineSemantics = EngineSemantics) {
            var r = rule.Without("author_reviewed", "review");
            var m = new JsonObject(new[]
            {
                Member("engine_semantics", JsonValue.Of(engineSemantics)),
                Member("format", JsonValue.Of(Format)),
                Member("format_version", JsonValue.Of(1)),
                Member("resolved", resolved),
                Member("rule", r),
            });
            return Sha256Hex.OfCanonical(m);
        }

        /// <summary>The <c>resolved</c> argument for a loaded rule (the reference's <c>rule_resolved</c>).</summary>
        internal static JsonObject Resolved(RuleInfo rule, LoadedTables tables) {
            var doc = rule.Doc;
            var comp = (JsonObject)((JsonObject)doc["preconditions"])["composition"];
            var adducts = new List<JsonValue>();
            foreach (var a in rule.Tried) {
                adducts.Add(a.Adduct.Row);
            }
            var unitNames = (JsonObject)comp["units"];
            var units = new List<JsonValue>();
            foreach (var u in tables.Units) {
                if (unitNames.ContainsKey(u.Name)) {
                    units.Add(u.Row);
                }
            }
            tables.Motifs.TryGetValue(((JsonString)((JsonObject)doc["claim"])["motif_id"]).Value, out var motif);
            var symbols = new SortedSet<string>(CanonicalJson.CodePointOrder);
            void Add(JsonValue? f) {
                if (f is JsonString s) {
                    foreach (var symbol in Composition.Symbols(s.Value)) {
                        symbols.Add(symbol);
                    }
                }
            }
            Add(comp["core"]);
            if (rule.Scaffold != null) {
                foreach (var e in (JsonArray)rule.Scaffold.Document["entries"]) {
                    var entry = (JsonObject)e;
                    Add(entry["formula"]);
                    foreach (var modes in ((JsonObject)entry["ions"]).Members) {
                        foreach (var lst in ((JsonObject)modes.Value).Members) {
                            foreach (var f in (JsonArray)lst.Value) {
                                Add(f);
                            }
                        }
                    }
                }
            }
            foreach (var u in units) {
                Add(((JsonObject)u)["residue"]);
            }
            foreach (var x in (JsonArray)doc["terms"]) {
                var t = (JsonObject)x;
                foreach (var k in new[] { "formula", "plus", "minus" }) {
                    Add(t.Get(k));
                }
                if (t.ContainsKey("unit")) {
                    var unit = (JsonObject)t["unit"];
                    Add(unit["plus"]);
                    Add(unit["minus"]);
                }
            }
            foreach (var a in adducts) {
                Add(((JsonObject)a)["add"]);
                Add(((JsonObject)a)["remove"]);
            }
            var el = tables.Elements;
            var masses = new List<KeyValuePair<string, JsonValue>>();
            foreach (var s in symbols) {
                el.TryGetIndex(s, out var index);
                masses.Add(Member(s, el.Mass(index)));
            }
            return new JsonObject(new[]
            {
                Member("adducts", new JsonArray(adducts)),
                Member("units", new JsonArray(units)),
                Member("scaffold_list", rule.Scaffold != null ? rule.Scaffold.Document : JsonValue.Null),
                Member("motif", motif != null ? motif.Without("chemont") : JsonValue.Null),
                Member("elements", new JsonObject(new[]
                {
                    Member("electron", el.Electron),
                    Member("masses", new JsonObject(masses)),
                })),
            });
        }

        internal static KeyValuePair<string, JsonValue> Member(string key, JsonValue value) => new KeyValuePair<string, JsonValue>(key, value);
    }

    /// <summary>Phase D (design 3.5, 3.6 items 8 and 9): parameters_origin and the review block.</summary>
    internal static class ProvenanceDeriver
    {
        private static int Rank(string origin) => Array.IndexOf(SchemaReader.Origins, origin);

        public static void CheckProvenanceAndReview(ErrorSink c, RuleInfo rule, LoadedTables tables) {
            var doc = rule.Doc;
            var origins = new List<string>();
            void Tok(JsonValue v) {
                if (v is JsonString s) {
                    origins.Add(s.Value.StartsWith("src:", StringComparison.Ordinal) ? "as_published" : s.Value);
                }
            }
            foreach (var x in (JsonArray)doc["terms"]) {
                var t = (JsonObject)x;
                Tok(t["term_source"]);
                Tok(t["floor_source"]);
                Tok(t["ratio_source"]);
            }
            var stack = new Stack<JsonObject>();
            stack.Push((JsonObject)doc["logic"]);
            while (stack.Count > 0) {
                var node = stack.Pop();
                var key = node.Members[0].Key;
                var val = node.Members[0].Value;
                if (key == "all" || key == "any") {
                    foreach (var ch in (JsonArray)val) {
                        stack.Push((JsonObject)ch);
                    }
                }
                else if (key == "not") {
                    stack.Push((JsonObject)val);
                }
                else if (key == "k_of") {
                    Tok(((JsonObject)val)["k_source"]);
                    foreach (var ch in (JsonArray)((JsonObject)val)["of"]) {
                        stack.Push((JsonObject)ch);
                    }
                }
            }
            if (rule.Scaffold != null) {
                foreach (var e in rule.Scaffold.Entries) {
                    origins.Add(e.OriginText);
                }
            }
            var unitNames = (JsonObject)((JsonObject)((JsonObject)doc["preconditions"])["composition"])["units"];
            foreach (var u in tables.Units) {
                if (unitNames.ContainsKey(u.Name)) {
                    origins.Add(u.OriginText);
                }
            }
            foreach (var a in rule.Tried) {
                origins.Add(a.Adduct.OriginText);
            }
            if (tables.Motifs.TryGetValue(((JsonString)((JsonObject)doc["claim"])["motif_id"]).Value, out var motif)) {
                var origin = ((JsonString)motif["origin"]).Value;
                origins.Add(origin == "source_defined" ? "as_published" : origin);
            }
            var derived = "as_published";
            foreach (var o in origins) {
                if (Rank(o) > Rank(derived)) {
                    derived = o;
                }
            }
            if (((JsonString)doc["parameters_origin"]).Value != derived) {
                c.Err(RuleErrorCodes.OriginInconsistent, "/parameters_origin");
            }
            var review = doc["review"];
            var authorReviewed = ((JsonBoolean)doc["author_reviewed"]).Value;
            if (authorReviewed != !(review is JsonNull)) {
                c.Err(RuleErrorCodes.ReviewMissing, "/review");
            }
            if (authorReviewed && !(((JsonBoolean)doc["source_checked"]).Value && ((JsonBoolean)doc["measure"]).Value)) {
                c.Err(RuleErrorCodes.ReviewNotAllowed, "/author_reviewed");
            }
            if (review is JsonObject r) {
                var stated = ((JsonString)r["rule_semantic_sha256"]).Value;
                if (stated != RuleSemanticHash.Compute(doc, RuleSemanticHash.Resolved(rule, tables))) {
                    c.Err(RuleErrorCodes.ReviewStale, "/review/rule_semantic_sha256");
                }
            }
        }
    }
}
