using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Loading
{
    /// <summary>The validated tables of a rule set (the reference's Tables).</summary>
    internal sealed class LoadedTables
    {
        public LoadedTables(ElementTable elements) {
            Elements = elements;
        }

        public ElementTable Elements { get; }
        public List<AdductRow> Adducts { get; } = new List<AdductRow>();
        public Dictionary<string, AdductRow> AdductByName { get; } = new Dictionary<string, AdductRow>(StringComparer.Ordinal);
        public List<UnitRow> Units { get; } = new List<UnitRow>();
        public Dictionary<string, UnitRow> UnitByName { get; } = new Dictionary<string, UnitRow>(StringComparer.Ordinal);
        /// <summary>Motif rows by id (file order kept in <see cref="MotifOrder"/>).</summary>
        public Dictionary<string, JsonObject> Motifs { get; } = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        public List<string> MotifOrder { get; } = new List<string>();
        public List<JsonObject> Nesting { get; } = new List<JsonObject>();
        public Dictionary<string, ScaffoldList> Scaffolds { get; } = new Dictionary<string, ScaffoldList>(StringComparer.Ordinal);
    }

    /// <summary>Everything a load found, whether or not it succeeded (the reference's LoadResult).</summary>
    internal sealed class RuleSetLoadReport
    {
        public List<RuleLoadError> Errors { get; set; } = new List<RuleLoadError>();
        /// <summary>Per-file canonical sha256 of every hashed file, by path; empty when a hashed file failed to read.</summary>
        public SortedDictionary<string, string> FileHashes { get; } = new SortedDictionary<string, string>(CanonicalJson.CodePointOrder);
        public string? RuleSetSha256 { get; set; }
        public string? RuleSetVersion { get; set; }
        public int RuleCount { get; set; }
        /// <summary>Draft staging files that were read (merged, never hashed), in path order.</summary>
        public List<string> StagingFiles { get; } = new List<string>();
        public bool PinChecked { get; set; }
        public LoadedTables? Tables { get; set; }
        /// <summary>Rules whose schema and references passed, by rule id.</summary>
        public SortedDictionary<string, RuleInfo> Rules { get; } = new SortedDictionary<string, RuleInfo>(CanonicalJson.CodePointOrder);

        public JsonObject Resolved(string ruleId) => RuleSemanticHash.Resolved(Rules[ruleId], Tables!);

        public string SemanticSha256(string ruleId) => RuleSemanticHash.Compute(Rules[ruleId].Doc, Resolved(ruleId));
    }

    /// <summary>
    /// Loads and validates a rule set given as files (design 3.6): every load-time check, all or nothing, each error
    /// as (code, file, pointer) sorted by (file, pointer, code). Mirrors <c>load_files(files, draft=False)</c> of
    /// the Python reference.
    /// </summary>
    internal static class DiagnosticRuleSetLoader
    {
        public const string HistoryFile = "ruleset-history.tsv";
        public const string RulesetFile = "ruleset.json";
        public const string ElementsFile = "tables/elements.json";
        public const string AdductsFile = "tables/adducts.json";
        public const string UnitsFile = "tables/units.json";
        public const string MotifsFile = "tables/motifs.json";
        public const string NestingFile = "tables/motif_nesting.json";

        /// <summary>The resource name prefix of the embedded set (csproj LogicalName).</summary>
        public const string ResourcePrefix = "DiagnosticRules/RuleSet/";

        private static readonly string[][] TableWrappers = {
            new[] { AdductsFile, "msfinder-diagnostic-adducts", "adducts" },
            new[] { UnitsFile, "msfinder-diagnostic-units", "units" },
            new[] { MotifsFile, "msfinder-diagnostic-motifs", "motifs" },
            new[] { NestingFile, "msfinder-diagnostic-motif-nesting", "nesting" },
        };

        /// <summary>Draft staging folders, merged after the table rows and never hashed (draft only).</summary>
        private static readonly string[] StagingDirs = { "_motifs", "_nesting", "_requests" };

        /// <summary>Folders a draft ignores.</summary>
        private static readonly string[] DraftIgnoredDirs = { "_audit" };

        private static readonly Regex HistoryRow = new Regex(
            @"^([a-z0-9][a-z0-9._-]*)\t([0-9a-f]{64})\t([0-9]{4}-[0-9]{2}-[0-9]{2})\z", RegexOptions.CultureInvariant);

        // ------------------------------------------------------------------------------------------------ hashes

        /// <summary>sha256 of the canonical form of a file (design 3.2); throws <see cref="StrictJsonException"/>.</summary>
        public static string FileSha256(byte[] bytes) => Sha256Hex.OfCanonical(StrictJsonReader.Read(bytes));

        /// <summary>The rule-set sha256 over {path: file sha256} (design 3.2).</summary>
        public static string RuleSetSha256(IReadOnlyDictionary<string, byte[]> files) {
            var hashes = new SortedDictionary<string, string>(CanonicalJson.CodePointOrder);
            foreach (var kv in files) {
                hashes[kv.Key] = FileSha256(kv.Value);
            }
            return RuleSetSha256FromHashes(hashes);
        }

        public static string RuleSetSha256FromHashes(IEnumerable<KeyValuePair<string, string>> fileHashes) {
            var members = new List<KeyValuePair<string, JsonValue>>();
            foreach (var kv in fileHashes) {
                members.Add(new KeyValuePair<string, JsonValue>(kv.Key, JsonValue.Of(kv.Value)));
            }
            var m = new JsonObject(new[]
            {
                new KeyValuePair<string, JsonValue>("files", new JsonObject(members)),
                new KeyValuePair<string, JsonValue>("format", JsonValue.Of("msfinder-diagnostic-ruleset-hash")),
                new KeyValuePair<string, JsonValue>("format_version", JsonValue.Of(1)),
            });
            return Sha256Hex.OfCanonical(m);
        }

        // ------------------------------------------------------------------------------------------------ layout

        private static bool IsTableFile(string rel) {
            foreach (var w in TableWrappers) {
                if (w[0] == rel) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>"hashed", "staging", "unexpected" or "ignore" (the reference's classify).</summary>
        internal static string Classify(string rel, bool draft) {
            var parts = rel.Split('/');
            var top = parts[0];
            var isJson = rel.EndsWith(".json", StringComparison.Ordinal);
            if (top.StartsWith("_", StringComparison.Ordinal) && parts.Length > 1) {
                if (draft && Array.IndexOf(DraftIgnoredDirs, top) >= 0) {
                    return "ignore";
                }
                if (draft && Array.IndexOf(StagingDirs, top) >= 0 && parts.Length == 2 && isJson) {
                    return "staging";
                }
                return isJson ? "unexpected" : "ignore";
            }
            if (!isJson) {
                return "ignore";
            }
            if (rel == RulesetFile || rel == ElementsFile || IsTableFile(rel)) {
                return "hashed";
            }
            if (parts.Length == 2 && (top == "rules" || top == "scaffolds")) {
                return "hashed";
            }
            return "unexpected";
        }

        // -------------------------------------------------------------------------------------------- the loader

        /// <summary>
        /// Validates a rule set given as {path relative to RuleSet/ with '/': bytes}. With <paramref name="checkPin"/>
        /// (the embedded set) the version must have a matching ruleset-history.tsv row. A <paramref name="draft"/>
        /// (the reference's <c>--draft</c>, a catalog working copy) also merges the staging folders _motifs, _nesting
        /// and _requests after the table rows, ignores _audit, does not require the motif and nesting tables and
        /// never checks the pin.
        /// </summary>
        public static RuleSetLoadReport Validate(IReadOnlyDictionary<string, byte[]> files, bool checkPin, bool draft = false) {
            var errors = new List<RuleLoadError>();
            var res = new RuleSetLoadReport();
            var docs = new SortedDictionary<string, JsonObject>(CanonicalJson.CodePointOrder);
            var hashed = new SortedDictionary<string, byte[]?>(CanonicalJson.CodePointOrder);
            var staging = new SortedDictionary<string, JsonValue>(CanonicalJson.CodePointOrder);
            var paths = new List<string>(files.Keys);
            paths.Sort(CanonicalJson.CodePointOrder);
            foreach (var rel in paths) {
                var kind = Classify(rel, draft);
                if (kind == "ignore") {
                    continue;
                }
                if (kind == "unexpected") {
                    errors.Add(new RuleLoadError(RuleErrorCodes.UnexpectedFile, rel, ""));
                    continue;
                }
                var b = files[rel];
                JsonValue doc;
                try {
                    doc = kind == "hashed" ? StrictJsonReader.Read(b) : StrictJsonReader.ReadStaging(b);
                }
                catch (StrictJsonException e) {
                    errors.Add(new RuleLoadError(e.Code, rel, ""));
                    if (kind == "hashed") {
                        hashed[rel] = null;
                    }
                    continue;
                }
                SchemaReader.ScanRefused(new ErrorSink(errors, rel), doc);
                if (kind == "hashed") {
                    hashed[rel] = b;
                    docs[rel] = (JsonObject)doc;
                }
                else {
                    staging[rel] = doc;
                }
            }
            res.StagingFiles.AddRange(staging.Keys);
            var allRead = true;
            foreach (var kv in hashed) {
                allRead &= kv.Value != null;
            }
            if (allRead) {
                foreach (var kv in docs) {
                    res.FileHashes[kv.Key] = Sha256Hex.OfCanonical(kv.Value);
                }
                res.RuleSetSha256 = RuleSetSha256FromHashes(res.FileHashes);
            }
            var required = draft
                ? new[] { RulesetFile, ElementsFile, AdductsFile, UnitsFile }
                : new[] { RulesetFile, ElementsFile, AdductsFile, UnitsFile, MotifsFile, NestingFile };
            foreach (var rel in required) {
                if (!files.ContainsKey(rel)) {
                    errors.Add(new RuleLoadError(RuleErrorCodes.MissingFile, rel, ""));
                }
            }
            if (docs.TryGetValue(RulesetFile, out var rulesetDoc)) {
                var c = new ErrorSink(errors, RulesetFile);
                SchemaReader.CheckKeys(c, rulesetDoc, "", new[] { "format", "format_version", "rule_set_version" });
                SchemaReader.VFormatHeader(c, rulesetDoc, "msfinder-diagnostic-ruleset");
                var ver = rulesetDoc.Get("rule_set_version");
                if (SchemaReader.VStr(c, ver, "/rule_set_version", pattern: SchemaReader.VersionPattern)) {
                    res.RuleSetVersion = ((JsonString)ver!).Value;
                }
            }
            if (!docs.TryGetValue(ElementsFile, out var elementsDoc)) {
                res.Errors = RuleLoadError.Sort(errors);
                return res;
            }
            var el = SchemaReader.LoadElements(new ErrorSink(errors, ElementsFile), elementsDoc);
            if (el is null) {
                res.Errors = RuleLoadError.Sort(errors);
                return res;
            }
            var tables = new LoadedTables(el);
            res.Tables = tables;
            var rows = new Dictionary<string, List<Tuple<string, string, JsonValue>>>(StringComparer.Ordinal);
            foreach (var w in TableWrappers) {
                rows[w[2]] = new List<Tuple<string, string, JsonValue>>();
                if (docs.TryGetValue(w[0], out var doc)) {
                    var c = new ErrorSink(errors, w[0]);
                    SchemaReader.CheckKeys(c, doc, "", new[] { "format", "format_version", w[2] });
                    SchemaReader.VFormatHeader(c, doc, w[1]);
                    var list = doc.Get(w[2]);
                    if (list is null) {
                        continue;
                    }
                    if (!(list is JsonArray a)) {
                        c.Err(RuleErrorCodes.BadType, "/" + w[2]);
                        continue;
                    }
                    for (var i = 0; i < a.Count; i++) {
                        rows[w[2]].Add(Tuple.Create(w[0], Pointers.Child("/" + w[2], i), a[i]));
                    }
                }
            }
            // Draft staging rows follow the table rows, in path order.
            foreach (var kv in staging) {
                var c = new ErrorSink(errors, kv.Key);
                var top = kv.Key.Split('/')[0];
                if (top == "_motifs" || top == "_nesting") {
                    if (!(kv.Value is JsonArray list)) {
                        c.Err(RuleErrorCodes.BadType, "");
                        continue;
                    }
                    var key = top == "_motifs" ? "motifs" : "nesting";
                    for (var i = 0; i < list.Count; i++) {
                        rows[key].Add(Tuple.Create(kv.Key, Pointers.Child("", i), list[i]));
                    }
                }
                else {
                    if (!(kv.Value is JsonObject request)) {
                        c.Err(RuleErrorCodes.BadType, "");
                        continue;
                    }
                    SchemaReader.CheckKeys(c, request, "", new[] { "units", "adducts" });
                    foreach (var key in new[] { "units", "adducts" }) {
                        var value = request.Get(key);
                        if (value is null) {
                            continue;
                        }
                        if (!(value is JsonArray list)) {
                            c.Err(RuleErrorCodes.BadType, "/" + key);
                            continue;
                        }
                        for (var i = 0; i < list.Count; i++) {
                            rows[key].Add(Tuple.Create(kv.Key, Pointers.Child("/" + key, i), list[i]));
                        }
                    }
                }
            }
            var aliasSeen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows["adducts"]) {
                var c = new ErrorSink(errors, row.Item1);
                var a = SchemaReader.VAdductRow(c, row.Item3, row.Item2, el);
                if (a is null) {
                    continue;
                }
                var spellings = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(a.Adduct, Pointers.Child(row.Item2, "adduct")) };
                for (var i = 0; i < a.Aliases.Count; i++) {
                    spellings.Add(new KeyValuePair<string, string>(a.Aliases[i], Pointers.Child(row.Item2, "aliases", i)));
                }
                var dup = false;
                foreach (var s in spellings) {
                    if (!aliasSeen.Add(s.Key.Replace(" ", ""))) {
                        c.Err(RuleErrorCodes.DuplicateId, s.Value);
                        dup = true;
                    }
                }
                if (!dup) {
                    tables.Adducts.Add(a);
                    tables.AdductByName[a.Adduct] = a;
                }
            }
            foreach (var row in rows["units"]) {
                var c = new ErrorSink(errors, row.Item1);
                var u = SchemaReader.VUnitRow(c, row.Item3, row.Item2, el);
                if (u is null) {
                    continue;
                }
                if (tables.UnitByName.ContainsKey(u.Name)) {
                    c.Err(RuleErrorCodes.DuplicateId, Pointers.Child(row.Item2, "name"));
                    continue;
                }
                tables.Units.Add(u);
                tables.UnitByName[u.Name] = u;
            }
            if (docs.ContainsKey(UnitsFile)) {
                foreach (var name in SchemaReader.RequiredUnits) {
                    if (!tables.UnitByName.ContainsKey(name)) {
                        errors.Add(new RuleLoadError(RuleErrorCodes.MissingRequiredUnit, UnitsFile, "/units"));
                        break;
                    }
                }
            }
            foreach (var row in rows["motifs"]) {
                var c = new ErrorSink(errors, row.Item1);
                var m = SchemaReader.VMotifRow(c, row.Item3, row.Item2);
                if (m is null) {
                    continue;
                }
                var id = ((JsonString)m["motif_id"]).Value;
                if (tables.Motifs.ContainsKey(id)) {
                    c.Err(RuleErrorCodes.DuplicateId, Pointers.Child(row.Item2, "motif_id"));
                    continue;
                }
                tables.Motifs[id] = m;
                tables.MotifOrder.Add(id);
            }
            var pairs = new HashSet<string>(StringComparer.Ordinal);
            var edges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var row in rows["nesting"]) {
                var c = new ErrorSink(errors, row.Item1);
                var n = SchemaReader.VNestingRow(c, row.Item3, row.Item2);
                if (n is null) {
                    continue;
                }
                var child = ((JsonString)n["child"]).Value;
                var parent = ((JsonString)n["parent"]).Value;
                var bad = false;
                foreach (var k in new[] { "child", "parent" }) {
                    if (!tables.Motifs.ContainsKey(((JsonString)n[k]).Value)) {
                        c.Err(RuleErrorCodes.UnknownMotif, Pointers.Child(row.Item2, k));
                        bad = true;
                    }
                }
                if (bad) {
                    continue;
                }
                if (child == parent) {
                    c.Err(RuleErrorCodes.NestingCycle, row.Item2);
                    continue;
                }
                if (!pairs.Add(child + "\n" + parent)) {
                    c.Err(RuleErrorCodes.DuplicateId, row.Item2);
                    continue;
                }
                if (!edges.TryGetValue(child, out var parents)) {
                    edges[child] = parents = new HashSet<string>(StringComparer.Ordinal);
                }
                parents.Add(parent);
                if (Reaches(edges, parent, child)) {
                    c.Err(RuleErrorCodes.NestingCycle, row.Item2);
                    parents.Remove(parent);
                    continue;
                }
                tables.Nesting.Add(n);
            }
            foreach (var kv in docs) {
                if (kv.Key.StartsWith("scaffolds/", StringComparison.Ordinal)) {
                    var stem = kv.Key.Substring("scaffolds/".Length, kv.Key.Length - "scaffolds/".Length - ".json".Length);
                    var sl = SchemaReader.VScaffoldList(new ErrorSink(errors, kv.Key), kv.Value, stem, el);
                    if (sl != null) {
                        tables.Scaffolds[sl.ListId] = sl;
                    }
                }
            }
            foreach (var kv in docs) {
                var rel = kv.Key;
                if (!rel.StartsWith("rules/", StringComparison.Ordinal)) {
                    continue;
                }
                var stem = rel.Substring("rules/".Length, rel.Length - "rules/".Length - ".json".Length);
                res.RuleCount++;
                var c = new ErrorSink(errors, rel);
                var n0 = c.N;
                SchemaReader.VRuleSchema(c, kv.Value, stem, el);
                if (c.N != n0 || errors.Exists(e => e.File == rel)) {
                    continue;
                }
                var rule = RuleValidator.CheckRuleRefs(c, kv.Value, rel, tables);
                if (rule is null) {
                    continue;
                }
                res.Rules[rule.RuleId] = rule;
                TwoIonCheck.CheckSeries(c, rule, tables);
                ProvenanceDeriver.CheckProvenanceAndReview(c, rule, tables);
            }
            if (checkPin && !draft && res.RuleSetVersion != null && res.RuleSetSha256 != null) {
                res.PinChecked = true;
                files.TryGetValue(HistoryFile, out var history);
                CheckPin(errors, history, res.RuleSetVersion, res.RuleSetSha256);
            }
            res.Errors = RuleLoadError.Sort(errors);
            return res;
        }

        private static bool Reaches(Dictionary<string, HashSet<string>> edges, string start, string goal) {
            var stack = new Stack<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            stack.Push(start);
            while (stack.Count > 0) {
                var x = stack.Pop();
                if (x == goal) {
                    return true;
                }
                if (!seen.Add(x)) {
                    continue;
                }
                if (edges.TryGetValue(x, out var next)) {
                    foreach (var y in next) {
                        stack.Push(y);
                    }
                }
            }
            return false;
        }

        /// <summary>The version pin (design 3.3): ruleset-history.tsv holds version, sha256 and date rows, append-only.</summary>
        private static void CheckPin(List<RuleLoadError> errors, byte[]? history, string version, string sha) {
            var rows = new Dictionary<string, string>(StringComparer.Ordinal);
            if (history != null) {
                var sb = new StringBuilder(history.Length);
                foreach (var b in history) {
                    sb.Append(b < 0x80 ? (char)b : '�'); // ascii, errors="replace"
                }
                foreach (var raw in sb.ToString().Split('\n')) {
                    var line = raw.TrimEnd('\r');
                    if (line.Length == 0) {
                        continue;
                    }
                    var m = HistoryRow.Match(line);
                    if (!m.Success || !SchemaReader.IsValidDate(m.Groups[3].Value) || rows.ContainsKey(m.Groups[1].Value)) {
                        errors.Add(new RuleLoadError(RuleErrorCodes.HistoryFormat, HistoryFile, ""));
                        continue;
                    }
                    rows[m.Groups[1].Value] = m.Groups[2].Value;
                }
            }
            if (!rows.TryGetValue(version, out var pinned)) {
                errors.Add(new RuleLoadError(RuleErrorCodes.RulesetUnpinned, RulesetFile, "/rule_set_version"));
            }
            else if (pinned != sha) {
                errors.Add(new RuleLoadError(RuleErrorCodes.RulesetShaMismatch, RulesetFile, "/rule_set_version"));
            }
        }

        // ------------------------------------------------------------------------------------------------ sources

        /// <summary>Every file under a directory, by path relative to it with '/'. JSON files and the history file are
        /// read; other files map to an empty array (they are classified as ignored or unexpected, never read).</summary>
        public static Dictionary<string, byte[]> ReadDirectory(string root) {
            var full = Path.GetFullPath(root);
            if (!Directory.Exists(full)) {
                throw new DirectoryNotFoundException("not a directory: " + root);
            }
            var prefix = full[full.Length - 1] == Path.DirectorySeparatorChar ? full : full + new string(Path.DirectorySeparatorChar, 1);
            var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var path in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories)) {
                var rel = path.Substring(prefix.Length).Replace('\\', '/');
                var keep = rel.EndsWith(".json", StringComparison.Ordinal) || rel == HistoryFile;
                files[rel] = keep && !rel.StartsWith("_audit/", StringComparison.Ordinal) ? File.ReadAllBytes(path) : new byte[0];
            }
            return files;
        }

        /// <summary>The embedded rule set: resources named <see cref="ResourcePrefix"/> + path ('\' mapped to '/').
        /// Names are sorted ordinally; the enumeration order of the manifest is never relied on.</summary>
        public static Dictionary<string, byte[]> ReadEmbedded() {
            var assembly = typeof(DiagnosticRuleSetLoader).Assembly;
            var names = new List<string>(assembly.GetManifestResourceNames());
            names.Sort(StringComparer.Ordinal);
            var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var name in names) {
                var normalised = name.Replace('\\', '/');
                if (!normalised.StartsWith(ResourcePrefix, StringComparison.Ordinal)) {
                    continue;
                }
                using (var stream = assembly.GetManifestResourceStream(name)) {
                    if (stream is null) {
                        continue;
                    }
                    using (var copy = new MemoryStream()) {
                        stream.CopyTo(copy);
                        files[normalised.Substring(ResourcePrefix.Length)] = copy.ToArray();
                    }
                }
            }
            return files;
        }
    }
}
