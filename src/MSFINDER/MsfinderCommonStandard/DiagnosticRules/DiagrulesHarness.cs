using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// The per-record evaluation and the evidence line of the <c>diagrules</c> benchmark (design section 8 PR 5).
    /// </summary>
    public static class DiagrulesHarness
    {
        /// <summary><c>peaks_source</c> of every harness record.</summary>
        public const string PeaksSource = "harness_msp";

        /// <summary>The status of a profile record (never centroided).</summary>
        public const string SkippedProfile = "skipped:profile";

        /// <summary>The name of the base pass.</summary>
        public const string BasePass = "base";

        /// <summary>
        /// Evaluates one record: a record outside the peak grammar is <c>skipped:peaks_suspect:&lt;reason&gt;</c>, a
        /// profile record <c>skipped:profile</c>, a GC-EI derivative run <c>skipped:ei_derivative</c>; otherwise the PR 4
        /// adapter turns the record into the four inputs (its count and sub-1-Da guards included) and
        /// <paramref name="evaluator"/> evaluates them, with <paramref name="evidence"/> when given. An exception gives
        /// <c>error:&lt;type name&gt;</c>, or is rethrown under <paramref name="strict"/>. The collision energy and the
        /// precursor type never reach the evaluator (except as <paramref name="evidence"/>, which the caller makes).
        /// </summary>
        public static DiagnosticQueryEvaluation Evaluate(DiagrulesRecord record, DiagnosticRuleEvaluator evaluator, string fallbackInstrumentType,
                                                         bool isTmsMeoxDerivative, bool strict, AdductEvidence evidence = null) {
            if (record == null) {
                throw new ArgumentNullException(nameof(record));
            }
            if (evaluator == null) {
                throw new ArgumentNullException(nameof(evaluator));
            }
            if (isTmsMeoxDerivative) {
                return new DiagnosticQueryEvaluation(DiagnosticRuleRuntime.SkippedEiDerivative, null, null);
            }
            if (record.IsProfile && record.PeaksSuspect == null) {
                return new DiagnosticQueryEvaluation(SkippedProfile, null, null);
            }
            DiagnosticInputInfo info = null;
            try {
                // A record outside the grammar has no peaks; the adapter still gives its instrument-type source, as
                // predict reports it for a refused query.
                var raw = record.ToRawData();
                if (record.PeaksSuspect != null) {
                    raw.SpectrumType = DataType.Centroid; // nothing to centroid
                }
                var spectrum = MsfinderDiagnosticAdapter.ToSpectrum(raw, fallbackInstrumentType, out var adapterInfo);
                if (record.PeaksSuspect != null) {
                    info = new DiagnosticInputInfo(PeaksSource, adapterInfo.InstrumentTypeSource, 0, record.PeaksSuspect);
                    return new DiagnosticQueryEvaluation(info.SkipStatus, null, info);
                }
                info = new DiagnosticInputInfo(PeaksSource, adapterInfo.InstrumentTypeSource, adapterInfo.NonPositiveDropped, adapterInfo.PeaksSuspect);
                if (spectrum == null) {
                    return new DiagnosticQueryEvaluation(info.SkipStatus, null, info);
                }
                var result = evidence == null ? evaluator.Evaluate(spectrum) : evaluator.Evaluate(spectrum, evidence);
                return new DiagnosticQueryEvaluation(result.Status, result, info);
            }
            catch (Exception e) when (!strict) {
                return new DiagnosticQueryEvaluation(DiagnosticRuleRuntime.ErrorStatusPrefix + e.GetType().Name, null, info);
            }
        }

        /// <summary>
        /// The evidence line of one record (canonical, keys sorted):
        /// <c>{"collision_energy_bin":..,"file_name":..,"input":{"instrument_type_source":..,"peaks_check":..,"peaks_source":"harness_msp"},"record_index":..,"result":&lt;canonical result&gt;|null,"status":..}</c>.
        /// <c>peaks_check</c> is null when the peaks were not checked (profile, GC-EI derivative).
        /// </summary>
        public static string EvidenceLine(DiagrulesRecord record, DiagnosticQueryEvaluation evaluation) {
            var info = evaluation.Info;
            var sb = new StringBuilder();
            sb.Append("{\"collision_energy_bin\":");
            CanonicalJson.WriteString(sb, record.CollisionEnergyBin);
            sb.Append(",\"file_name\":");
            CanonicalJson.WriteString(sb, record.FileName ?? string.Empty);
            sb.Append(",\"input\":{\"instrument_type_source\":");
            JsonOrNull(sb, info?.InstrumentTypeSource);
            sb.Append(",\"peaks_check\":");
            JsonOrNull(sb, info == null ? null : info.PeaksSuspect ?? "ok");
            sb.Append(",\"peaks_source\":");
            CanonicalJson.WriteString(sb, PeaksSource);
            sb.Append("},\"record_index\":");
            sb.Append(DiagnosticRuleExport.Int(record.RecordIndex));
            sb.Append(",\"result\":");
            sb.Append(evaluation.Result == null ? "null" : DiagnosticResultJson.Write(evaluation.Result));
            sb.Append(",\"status\":");
            CanonicalJson.WriteString(sb, evaluation.Status ?? string.Empty);
            sb.Append('}');
            return sb.ToString();
        }

        private static void JsonOrNull(StringBuilder sb, string value) {
            if (value == null) {
                sb.Append("null");
            }
            else {
                CanonicalJson.WriteString(sb, value);
            }
        }

        /// <summary>The MSP files of <c>-i</c>: the file itself, or the folder's top-level <c>*.msp</c> files in ordinal
        /// file-name order; empty when there are none.</summary>
        public static IReadOnlyList<string> InputFiles(string input) {
            if (string.IsNullOrEmpty(input)) {
                return new string[0];
            }
            if (File.Exists(input)) {
                return new[] { input };
            }
            if (!Directory.Exists(input)) {
                return new string[0];
            }
            return Directory.GetFiles(input)
                .Where(f => DiagrulesMspReader.AsciiLower(Path.GetExtension(f)) == ".msp")
                .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Whether the input is VS21 by its name: "vs21" (ASCII case-insensitive) in the last component of
        /// <paramref name="input"/> or in any input file name. The outputs are then named <c>*.local-vs21.*</c>.</summary>
        public static bool IsLocalVs21(string input, IEnumerable<string> files) {
            bool Named(string path) {
                var name = Path.GetFileName((path ?? string.Empty).TrimEnd('\\', '/'));
                return DiagrulesMspReader.AsciiLower(name).IndexOf("vs21", StringComparison.Ordinal) >= 0;
            }
            return Named(input) || (files ?? Enumerable.Empty<string>()).Any(Named);
        }
    }

    /// <summary>The new files of one <c>diagrules</c> run. With VS21 input, <c>.local-vs21</c> goes before every
    /// extension, so such outputs can be recognised and never committed.</summary>
    public sealed class DiagrulesOutputNames
    {
        public const string EvidencePrefix = "Diagrules evidence-";
        public const string RunPrefix = "Diagrules run-";
        public const string LocalVs21 = ".local-vs21";

        public DiagrulesOutputNames(string stamp, bool localVs21) {
            Stamp = stamp;
            IsLocalVs21 = localVs21;
        }

        public string Stamp { get; }
        public bool IsLocalVs21 { get; }
        private string Suffix => IsLocalVs21 ? LocalVs21 : string.Empty;

        /// <summary>The evidence JSONL of one pass.</summary>
        public string Evidence(string pass) => EvidencePrefix + pass + "-" + Stamp + Suffix + ".jsonl";

        /// <summary>The canonical run manifest.</summary>
        public string Run => RunPrefix + Stamp + Suffix + ".json";
    }

    /// <summary>The record and stratum counts of the reader over every input (from the first pass).</summary>
    public sealed class DiagrulesReaderCounts
    {
        public int Records { get; private set; }
        /// <summary>Records without IONMODE (read as positive, as MS-FINDER reads them).</summary>
        public int IonModeAbsent { get; private set; }
        public SortedDictionary<string, int> CollisionEnergyBins { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);
        /// <summary>Records per input file name, in input order.</summary>
        public List<KeyValuePair<string, int>> RecordsByFile { get; } = new List<KeyValuePair<string, int>>();

        internal void Add(DiagrulesRecord record) {
            Records++;
            if (record.IonModeAbsent) {
                IonModeAbsent++;
            }
            var bin = record.CollisionEnergyBin;
            CollisionEnergyBins.TryGetValue(bin, out var n);
            CollisionEnergyBins[bin] = n + 1;
        }
    }

    /// <summary>
    /// One <c>diagrules</c> run (design section 8 PR 5): passes over every record of the inputs, each written to its
    /// evidence JSONL, and the canonical run manifest. Files are named with the invariant stamp, checked before
    /// anything is written, and created with <see cref="FileMode.CreateNew"/>; ASCII, LF.
    /// </summary>
    public sealed class DiagrulesRun
    {
        private readonly string _folder;
        private readonly DiagnosticRunEnvironment _environment;
        private readonly List<KeyValuePair<string, JsonValue>> _passes = new List<KeyValuePair<string, JsonValue>>();
        private readonly List<string> _passNames;

        private DiagrulesRun(DiagnosticRuleRuntime runtime, IReadOnlyList<string> inputs, DiagrulesOutputNames names, string folder,
                             IReadOnlyList<string> passNames, DiagnosticRunEnvironment environment) {
            Runtime = runtime;
            Inputs = inputs;
            Names = names;
            _folder = folder;
            _passNames = passNames.ToList();
            _environment = environment;
        }

        public DiagnosticRuleRuntime Runtime { get; }
        public IReadOnlyList<string> Inputs { get; }
        public DiagrulesOutputNames Names { get; }
        /// <summary>Filled by the first pass.</summary>
        public DiagrulesReaderCounts ReaderCounts { get; private set; }
        /// <summary>The counts of the base pass (null until it ran).</summary>
        public DiagnosticRunCounts BaseCounts { get; private set; }

        /// <summary>
        /// Starts a run over <paramref name="inputs"/>. Returns null with <c>error:output_exists:&lt;file name&gt;</c>
        /// when any file of <paramref name="passNames"/> or the manifest exists.
        /// </summary>
        public static DiagrulesRun Start(DiagnosticRuleRuntime runtime, IReadOnlyList<string> inputs, bool localVs21, string outputFolder, string stamp,
                                         IReadOnlyList<string> passNames, DiagnosticRunEnvironment environment, out string error) {
            if (runtime == null) {
                throw new ArgumentNullException(nameof(runtime));
            }
            error = null;
            var names = new DiagrulesOutputNames(stamp, localVs21);
            var all = passNames.Select(names.Evidence).Concat(new[] { names.Run });
            foreach (var name in all) {
                if (File.Exists(Path.Combine(outputFolder, name))) {
                    error = DiagnosticRuleExport.OutputExists + ":" + name;
                    return null;
                }
            }
            return new DiagrulesRun(runtime, inputs, names, outputFolder, passNames, environment ?? new DiagnosticRunEnvironment());
        }

        /// <summary>The base pass: the runtime's evaluator with MS-FINDER's settings, no adduct evidence.</summary>
        public bool RunBasePass(out string error) {
            var settings = Runtime.Settings;
            var ok = RunPass(DiagrulesHarness.BasePass, Runtime.Evaluator,
                r => DiagrulesHarness.Evaluate(r, Runtime.Evaluator, settings.InstrumentType, _environment.IsTmsMeoxDerivative, settings.Strict),
                out var counts, out error);
            BaseCounts = counts;
            return ok;
        }

        /// <summary>
        /// One pass: every record of every input, in input and record order, through <paramref name="evaluate"/>,
        /// one evidence line each. <paramref name="evaluator"/> gives the pass's identity and options in the manifest.
        /// Returns false with the error line when a file cannot be created or (Strict) an evaluation throws; the
        /// evidence file is then closed as written so far.
        /// </summary>
        public bool RunPass(string pass, DiagnosticRuleEvaluator evaluator, Func<DiagrulesRecord, DiagnosticQueryEvaluation> evaluate,
                            out DiagnosticRunCounts counts, out string error) {
            if (!_passNames.Contains(pass)) {
                throw new ArgumentException("pass not declared at Start: " + pass, nameof(pass));
            }
            error = null;
            counts = new DiagnosticRunCounts { Refused = evaluator.Refusals.Select(r => r.RuleId).Distinct(StringComparer.Ordinal).Count() };
            var reader = ReaderCounts == null ? new DiagrulesReaderCounts() : null;
            var name = Names.Evidence(pass);
            var path = Path.Combine(_folder, name);
            double evalMs = 0.0;
            StreamWriter writer;
            try {
                writer = Create(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
                error = "Diagnostic rules: error:" + e.GetType().Name + ":" + name;
                return false;
            }
            using (writer) {
                foreach (var input in Inputs) {
                    var fileName = Path.GetFileName(input);
                    var inFile = 0;
                    foreach (var record in DiagrulesMspReader.Read(input)) {
                        reader?.Add(record);
                        inFile++;
                        DiagnosticQueryEvaluation evaluation;
                        var start = _environment.Clock();
                        try {
                            evaluation = evaluate(record);
                        }
                        catch (Exception e) {
                            error = "Diagnostic rules (Strict): error:" + e.GetType().Name + " in " + DiagnosticRuleExport.Cell(fileName)
                                    + "#" + DiagnosticRuleExport.Int(record.RecordIndex);
                            return false;
                        }
                        evalMs += (_environment.Clock() - start) * 1000.0 / _environment.Frequency;
                        counts.Add(evaluation);
                        writer.Write(DiagrulesHarness.EvidenceLine(record, evaluation));
                        writer.Write(DiagnosticRuleExport.NewLine);
                    }
                    reader?.RecordsByFile.Add(new KeyValuePair<string, int>(fileName, inFile));
                }
            }
            if (reader != null) {
                ReaderCounts = reader;
            }
            var options = DiagnosticRuleExport.CanonicalOptions(evaluator);
            _passes.Add(M(pass, Obj(
                M("counts", CountsJson(counts)),
                M("evidence", JsonValue.Of(name)),
                M("identity", JsonValue.Of(evaluator.IdentityString)),
                M("options", options),
                M("options_sha256", JsonValue.Of(DiagnosticRuleExport.Sha256Hex(DiagnosticRuleExport.ToCanonicalText(options)))),
                M("refusals", new JsonArray(evaluator.Refusals.Select(x => (JsonValue)Obj(M("code", JsonValue.Of(x.Code)), M("rule_id", JsonValue.Of(x.RuleId)))).ToList())),
                M("timing", Obj(M("eval_ms", DiagnosticRuleExport.FixedNumber(evalMs, DiagnosticTimingLog.MsDecimals)))))));
            return true;
        }

        /// <summary>Writes the run manifest and returns the console summary line of the base pass (or the error line).</summary>
        public string Finish() {
            var name = Names.Run;
            try {
                using (var run = Create(Path.Combine(_folder, name))) {
                    run.Write(Manifest());
                    run.Write(DiagnosticRuleExport.NewLine);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
                return "Diagnostic rules: error:" + e.GetType().Name + ":" + name;
            }
            return BaseCounts == null ? "Diagnostic rules: no base pass" : DiagnosticRuleExport.SummaryLine(Runtime.RuleSet.Identity, BaseCounts);
        }

        /// <summary>The canonical run manifest: identities, settings, inputs (file names and sha256), reader counts and
        /// strata, every pass with its counts, options and evidence file, and the output names.</summary>
        public string Manifest() {
            var id = Runtime.RuleSet.Identity;
            var settings = Runtime.Settings;
            var reader = ReaderCounts ?? new DiagrulesReaderCounts();
            var records = reader.RecordsByFile.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
            var inputs = new List<JsonValue>();
            foreach (var input in Inputs) {
                var fileName = Path.GetFileName(input);
                records.TryGetValue(fileName, out var n);
                inputs.Add(Obj(M("file_name", JsonValue.Of(fileName)), M("records", JsonValue.Of(n)), M("sha256", JsonValue.Of(FileSha256(input)))));
            }
            var bins = reader.CollisionEnergyBins.Select(b => M(b.Key, JsonValue.Of(b.Value))).ToList();
            var outputs = new List<KeyValuePair<string, JsonValue>>();
            foreach (var pass in _passNames.OrderBy(p => p, StringComparer.Ordinal)) {
                outputs.Add(M("evidence:" + pass, JsonValue.Of(Names.Evidence(pass))));
            }
            outputs.Add(M("run", JsonValue.Of(Names.Run)));
            var manifest = Obj(
                M("build_identity", JsonValue.Of(_environment.BuildIdentity ?? string.Empty)),
                M("command", JsonValue.Of("diagrules")),
                M("engine_semantics", JsonValue.Of(DiagnosticRuleEvaluator.EngineSemantics)),
                M("inputs", new JsonArray(inputs)),
                M("local_vs21", JsonValue.Of(Names.IsLocalVs21)),
                M("outputs", new JsonObject(outputs)),
                M("passes", new JsonObject(_passes.OrderBy(p => p.Key, StringComparer.Ordinal).ToList())),
                M("reader", Obj(
                    M("collision_energy_bins", new JsonObject(bins)),
                    M("ion_mode_absent", JsonValue.Of(reader.IonModeAbsent)),
                    M("peaks_source", JsonValue.Of(DiagrulesHarness.PeaksSource)),
                    M("records", JsonValue.Of(reader.Records)))),
                M("rule_set", Obj(
                    M("files", new JsonArray(id.Files.Select(f => (JsonValue)Obj(M("path", JsonValue.Of(f.Path)), M("sha256", JsonValue.Of(f.Sha256)))).ToList())),
                    M("pinned", JsonValue.Of(id.Pinned)),
                    M("sha256", JsonValue.Of(id.Sha256)),
                    M("source", JsonValue.Of(DiagnosticRuleExport.SourceName(id.Source))),
                    M("version", JsonValue.Of(id.Version)))),
                M("settings", Obj(
                    M("instrument_type", JsonValue.Of(settings.InstrumentType ?? string.Empty)),
                    M("is_tms_meox_derivative", JsonValue.Of(_environment.IsTmsMeoxDerivative)),
                    M("strict", JsonValue.Of(settings.Strict)))),
                M("stamp", JsonValue.Of(Names.Stamp)));
            return DiagnosticRuleExport.ToCanonicalText(manifest);
        }

        private static JsonValue CountsJson(DiagnosticRunCounts counts) => Obj(
            M("conflicts", JsonValue.Of(counts.Conflicts)),
            M("eligible_conflicts", JsonValue.Of(counts.EligibleConflicts)),
            M("errors", JsonValue.Of(counts.Errors)),
            M("evaluated", JsonValue.Of(counts.Evaluated)),
            M("fired", JsonValue.Of(counts.Fired)),
            M("none", JsonValue.Of(counts.None)),
            M("queries", JsonValue.Of(counts.Queries)),
            M("refused", JsonValue.Of(counts.Refused)),
            M("skipped", JsonValue.Of(counts.Skipped)),
            M("skipped_by_reason", new JsonObject(counts.SkippedByReason.Select(s => M(s.Key, JsonValue.Of(s.Value))).ToList())),
            M("veto_blocked", JsonValue.Of(counts.VetoBlocked)));

        private static string FileSha256(string path) {
            byte[] hash;
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path)) {
                hash = sha.ComputeHash(stream);
            }
            const string digits = "0123456789abcdef";
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) {
                sb.Append(digits[b >> 4]);
                sb.Append(digits[b & 15]);
            }
            return sb.ToString();
        }

        private static StreamWriter Create(string path) {
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            return new StreamWriter(stream, Encoding.ASCII) { NewLine = DiagnosticRuleExport.NewLine };
        }

        private static KeyValuePair<string, JsonValue> M(string key, JsonValue value) => new KeyValuePair<string, JsonValue>(key, value);

        private static JsonObject Obj(params KeyValuePair<string, JsonValue>[] members) => new JsonObject(members);
    }
}
