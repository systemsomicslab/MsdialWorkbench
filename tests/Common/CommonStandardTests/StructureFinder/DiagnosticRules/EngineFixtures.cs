#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The engine parity fixtures owned by MsdialWorkbench (design 7.2): <c>engine/semantics.json</c> (hand-derived inline
/// mini rule sets with expectations and the C# canonical result bytes), <c>engine/loader-errors.json</c> (one defect
/// per loader error code as a self-contained file map), <c>manifest.json</c> and <c>engine/SOURCE.md</c>. This class
/// builds them, replays them, checks the hand-written expectations against the result JSON and audits the input
/// margins.
/// </summary>
internal static class EngineFixtures
{
    public const string Format = "msfinder-diagnostic-rule-fixtures";
    public const string ManifestFormat = "msfinder-diagnostic-rule-fixture-manifest";
    public const string SemanticsFile = "engine/semantics.json";
    public const string LoaderErrorsFile = "engine/loader-errors.json";
    public const string ManifestFile = "manifest.json";
    public const string SourceFile = "engine/SOURCE.md";
    public const string SemanticsKind = "semantics";
    public const string LoaderErrorsKind = "loader_errors";

    /// <summary>Record mode: a directory that receives freshly generated fixtures (the test then ends Inconclusive).</summary>
    public const string WriteVariable = "MSDIAL_DIAGNOSTIC_FIXTURES_WRITE";

    /// <summary>Peaks sit at least this far (Da) from every tolerance, window, precursor-region and loss boundary.</summary>
    public const double MarginDa = 2e-6;

    /// <summary>Relative intensities sit at least this far (relative) from every floor.</summary>
    public const double FloorMargin = 1e-6;

    private static KeyValuePair<string, JsonValue> M(string key, JsonValue value) => new KeyValuePair<string, JsonValue>(key, value);

    private static string Str(JsonObject o, string key) => ((JsonString)o[key]).Value;

    private static string? StrOrNull(JsonObject o, string key) => o[key] is JsonString s ? s.Value : null;

    /// <summary>A spec written with single quotes for double quotes.</summary>
    public static JsonObject Spec(string text) => (JsonObject)JsonEdit.Parse(text.Replace('\'', '"'));

    /// <summary>Decimal text through the strict lexeme conversion (one correctly rounded division).</summary>
    public static double Decimal(string text) => JsonFloat.Parse(text).Value;

    // ------------------------------------------------------------------------------------------------ semantics

    /// <summary>The case object without its result: name, description, files, options, input, expect.</summary>
    public static JsonObject CaseInput(SemanticsCase c) {
        // A scaffold list no rule of the case uses is left out, to keep each case small.
        var rules = string.Join("\n", c.Set.Docs.Where(kv => kv.Key.StartsWith("rules/", StringComparison.Ordinal)).Select(kv => CanonicalJson.ToText(kv.Value)));
        var files = c.Set.Docs.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                     .Where(kv => !kv.Key.StartsWith("scaffolds/", StringComparison.Ordinal)
                                  || rules.Contains("\"scaffold_list\":\"" + kv.Key.Substring("scaffolds/".Length, kv.Key.Length - "scaffolds/".Length - ".json".Length) + "\""))
                     .Select(kv => M(kv.Key, kv.Value));
        var peaks = c.Peaks.Select(p => (JsonValue)new JsonArray(new[] { JsonValue.Of(p[0]), JsonValue.Of(p[1]) }));
        var evidence = c.EvidenceAdduct is null
            ? JsonValue.Null
            : new JsonObject(new[] { M("adduct", JsonValue.Of(c.EvidenceAdduct)), M("tier", JsonValue.Of(c.EvidenceTier!)) });
        var input = new JsonObject(new[]
        {
            M("adduct_evidence", evidence),
            M("instrument_type", JsonValue.OfNullable(c.Instrument)),
            M("ion_mode", JsonValue.OfNullable(c.IonMode)),
            M("peaks", new JsonArray(peaks)),
            M("precursor_mz", JsonValue.OfNullable(c.Precursor)),
        });
        return new JsonObject(new[]
        {
            M("description", JsonValue.Of(c.Description)),
            M("expect", Spec(c.Expect)),
            M("files", new JsonObject(files)),
            M("input", input),
            M("name", JsonValue.Of(c.Name)),
            M("options", Spec(c.Options)),
        });
    }

    /// <summary>The file map of a case: each JSON object reserialised in storage form (PrettyJson) (OQ18).</summary>
    public static Dictionary<string, byte[]> CaseFiles(JsonObject caseObject) =>
        ((JsonObject)caseObject["files"]).Members.ToDictionary(m => m.Key, m => PrettyJson.ToBytes(m.Value), StringComparer.Ordinal);

    /// <summary>
    /// The options of a case from its spec: <c>filter</c> (measured_and_source_checked | author_reviewed | all),
    /// <c>precursor_tolerance_da</c> and <c>floor_scale</c> (decimal text), <c>product_tolerance_override</c> and
    /// <c>msfinder_product_tolerance</c> ({abs_da, ppm} decimal text), <c>loss_reference_override</c> (hypothesis |
    /// observed), <c>overlap</c> (refuse | runtime), the integers <c>decoy_shift_da</c>, <c>k_offset</c> and
    /// <c>assignment_cap</c>, and the booleans <c>strict</c> and <c>verbose</c>. A missing key is the default.
    /// </summary>
    public static DiagnosticEvaluationOptions Options(JsonObject spec) {
        var o = new DiagnosticEvaluationOptions();
        foreach (var m in spec.Members) {
            switch (m.Key) {
                case "filter":
                    o.Filter = Text(m.Value) switch
                    {
                        "measured_and_source_checked" => RuleFilter.MeasuredAndSourceChecked,
                        "author_reviewed" => RuleFilter.AuthorReviewed,
                        "all" => RuleFilter.All,
                        var x => throw new ArgumentException("filter " + x),
                    };
                    break;
                case "precursor_tolerance_da":
                    o.PrecursorToleranceDa = Decimal(Text(m.Value));
                    break;
                case "product_tolerance_override":
                    o.ProductToleranceOverride = Tolerance((JsonObject)m.Value);
                    break;
                case "msfinder_product_tolerance":
                    o.MsfinderProductTolerance = Tolerance((JsonObject)m.Value);
                    break;
                case "loss_reference_override":
                    o.LossReferenceOverride = Text(m.Value) switch
                    {
                        "hypothesis" => LossReference.Hypothesis,
                        "observed" => LossReference.Observed,
                        var x => throw new ArgumentException("loss reference " + x),
                    };
                    break;
                case "decoy_shift_da":
                    o.DecoyShiftDa = checked((int)((JsonInteger)m.Value).Value);
                    break;
                case "floor_scale":
                    o.FloorScale = Decimal(Text(m.Value));
                    break;
                case "k_offset":
                    o.KOffset = checked((int)((JsonInteger)m.Value).Value);
                    break;
                case "overlap":
                    o.Overlap = Text(m.Value) switch
                    {
                        "refuse" => OverlapMode.Refuse,
                        "runtime" => OverlapMode.Runtime,
                        var x => throw new ArgumentException("overlap " + x),
                    };
                    break;
                case "assignment_cap":
                    o.AssignmentCap = checked((int)((JsonInteger)m.Value).Value);
                    break;
                case "strict":
                    o.Strict = ((JsonBoolean)m.Value).Value;
                    break;
                case "verbose":
                    o.Verbose = ((JsonBoolean)m.Value).Value;
                    break;
                default:
                    throw new ArgumentException("unknown option " + m.Key);
            }
        }
        return o;
    }

    private static string Text(JsonValue v) => ((JsonString)v).Value;

    private static ProductTolerance Tolerance(JsonObject o) => new ProductTolerance(Decimal(Str(o, "abs_da")), Decimal(Str(o, "ppm")));

    /// <summary>The spectrum of a case input: decimal-text peaks, the precursor (null is NaN), the ion mode (pos, neg or
    /// null) and the instrument type.</summary>
    public static DiagnosticSpectrum Spectrum(JsonObject input) {
        var peaks = ((JsonArray)input["peaks"]).Select(p => {
            var pair = (JsonArray)p;
            return new DiagnosticPeak(Decimal(((JsonString)pair[0]).Value), Decimal(((JsonString)pair[1]).Value));
        }).ToList();
        var precursor = StrOrNull(input, "precursor_mz") is string p ? Decimal(p) : double.NaN;
        DiagnosticIonMode? mode = StrOrNull(input, "ion_mode") switch
        {
            null => null,
            "pos" => DiagnosticIonMode.Positive,
            "neg" => DiagnosticIonMode.Negative,
            var x => throw new ArgumentException("ion mode " + x),
        };
        return new DiagnosticSpectrum(peaks, precursor, mode, StrOrNull(input, "instrument_type")!);
    }

    public static AdductEvidence? Evidence(JsonObject input) {
        if (!(input["adduct_evidence"] is JsonObject e)) {
            return null;
        }
        var tier = Str(e, "tier") switch
        {
            "cross_polarity" => AdductTier.CrossPolarity,
            "within_polarity_pair" => AdductTier.WithinPolarityPair,
            "curated" => AdductTier.Curated,
            var x => throw new ArgumentException("tier " + x),
        };
        return new AdductEvidence(Str(e, "adduct"), tier);
    }

    /// <summary>Loads the case's rule set (inline, unpinned), builds the evaluator and evaluates the input.</summary>
    public static DiagnosticRuleResult Run(JsonObject caseObject) {
        var set = DiagnosticRuleSet.LoadFiles(CaseFiles(caseObject), RuleSetSource.Inline);
        var evaluator = new DiagnosticRuleEvaluator(set, Options((JsonObject)caseObject["options"]));
        var input = (JsonObject)caseObject["input"];
        var spectrum = Spectrum(input);
        var evidence = Evidence(input);
        return evidence is null ? evaluator.Evaluate(spectrum) : evaluator.Evaluate(spectrum, evidence);
    }

    /// <summary>A recorded case: the input plus the canonical result text and its sha256.</summary>
    public static JsonObject Record(SemanticsCase c) {
        var input = CaseInput(c);
        var text = DiagnosticResultJson.Write(Run(input));
        var members = input.Members.ToList();
        members.Add(M("result", JsonValue.Of(text)));
        members.Add(M("result_sha256", JsonValue.Of(Sha256Hex.Of(Encoding.ASCII.GetBytes(text)))));
        return new JsonObject(members);
    }

    private static JsonObject Document(string kind, IEnumerable<JsonValue> cases) => new JsonObject(new[]
    {
        M("cases", new JsonArray(cases)),
        M("engine_semantics", JsonValue.Of(DiagnosticRuleEvaluator.EngineSemantics)),
        M("format", JsonValue.Of(Format)),
        M("format_version", JsonValue.Of(1)),
        M("kind", JsonValue.Of(kind)),
        M("rule_set_sha256", JsonValue.Null),
        M("rule_set_version", JsonValue.Null),
    });

    public static JsonObject SemanticsDocument(IEnumerable<SemanticsCase> cases) => Document(SemanticsKind, cases.Select(c => (JsonValue)Record(c)));

    // ------------------------------------------------------------------------------------------------ expectations

    /// <summary>
    /// Checks a hand-written expectation against the result JSON tree (the tree whose canonical text is the result
    /// bytes). Every key is optional. A plain key is compared with the result field of the same name: objects by the
    /// expected keys only, arrays item by item, and a string against a number by its text. Special keys:
    /// <list type="bullet">
    /// <item><c>outcome_ids</c>: the rule ids of the outcomes, in order;</item>
    /// <item><c>refusals</c>: <c>rule_id:code:hypothesis_id:item,item</c> per refusal;</item>
    /// <item><c>outcomes</c>: rule id to an outcome expectation, whose special keys are <c>chosen</c> (the chosen
    /// hypothesis_id or null), <c>chosen_&lt;field&gt;</c> (a field of the chosen hypothesis), <c>matched</c> and
    /// <c>upgrades</c> (<c>term_id:member_key@peak_index</c> of the chosen hypothesis), <c>vetoes</c>
    /// (<c>term_id:fired</c>), <c>k_of</c> (<c>pointer k count satisfied</c>), <c>hypotheses</c> (hypothesis_id to its
    /// failure, or <c>fired</c>; from <c>all</c>, so verbose), and <c>all_matched</c> and <c>all_k_of</c> (hypothesis_id to
    /// the list of that hypothesis).</item>
    /// </list>
    /// </summary>
    public static List<string> CheckExpectations(JsonObject expect, JsonObject result) {
        var failures = new List<string>();
        foreach (var m in expect.Members) {
            switch (m.Key) {
                case "outcome_ids":
                    Compare("outcome_ids", m.Value, new JsonArray(Outcomes(result).Select(o => o["rule_id"])), failures);
                    break;
                case "refusals":
                    Compare("refusals", m.Value, new JsonArray(((JsonArray)result["refusals"]).Cast<JsonObject>().Select(r => JsonValue.Of(
                        Str(r, "rule_id") + ":" + Str(r, "code") + ":" + Str(r, "hypothesis_id") + ":"
                        + string.Join(",", ((JsonArray)r["items"]).Select(i => ((JsonString)i).Value))))), failures);
                    break;
                case "outcomes":
                    foreach (var o in ((JsonObject)m.Value).Members) {
                        var actual = Outcomes(result).FirstOrDefault(x => Str(x, "rule_id") == o.Key);
                        if (actual is null) {
                            failures.Add("outcomes/" + o.Key + ": no outcome");
                            continue;
                        }
                        CheckOutcome("outcomes/" + o.Key, (JsonObject)o.Value, actual, failures);
                    }
                    break;
                default:
                    Field(m.Key, m.Value, result, failures);
                    break;
            }
        }
        return failures;
    }

    private static IEnumerable<JsonObject> Outcomes(JsonObject result) => ((JsonArray)result["outcomes"]).Cast<JsonObject>();

    private static void CheckOutcome(string where, JsonObject expect, JsonObject outcome, List<string> failures) {
        var chosen = outcome["chosen"] as JsonObject;
        var all = ((JsonArray)outcome["all"]).Cast<JsonObject>().ToList();
        foreach (var m in expect.Members) {
            var at = where + "/" + m.Key;
            switch (m.Key) {
                case "chosen":
                    Compare(at, m.Value, chosen is null ? JsonValue.Null : chosen["hypothesis_id"], failures);
                    break;
                case "matched":
                case "upgrades":
                    Compare(at, m.Value, chosen is null ? JsonValue.Null : Items((JsonArray)chosen[m.Key]), failures);
                    break;
                case "vetoes":
                    Compare(at, m.Value, chosen is null ? JsonValue.Null : new JsonArray(((JsonArray)chosen["vetoes"]).Cast<JsonObject>()
                        .Select(v => JsonValue.Of(Str(v, "term_id") + ":" + (((JsonBoolean)v["fired"]).Value ? "true" : "false")))), failures);
                    break;
                case "k_of":
                    Compare(at, m.Value, chosen is null ? JsonValue.Null : KOfList(chosen), failures);
                    break;
                case "hypotheses":
                    Compare(at, m.Value, new JsonObject(all.Select(h => M(Str(h, "hypothesis_id"), JsonValue.Of(
                        StrOrNull(h, "failure") ?? (((JsonBoolean)h["fired"]).Value ? "fired" : "none"))))), failures, exactKeys: true);
                    break;
                case "all_matched":
                    Compare(at, m.Value, new JsonObject(all.Select(h => M(Str(h, "hypothesis_id"), Items((JsonArray)h["matched"])))), failures, exactKeys: true);
                    break;
                case "all_k_of":
                    Compare(at, m.Value, new JsonObject(all.Select(h => M(Str(h, "hypothesis_id"), KOfList(h)))), failures, exactKeys: true);
                    break;
                default:
                    if (m.Key.StartsWith("chosen_", StringComparison.Ordinal)) {
                        if (chosen is null) {
                            failures.Add(at + ": no chosen hypothesis");
                        }
                        else {
                            Field(m.Key.Substring("chosen_".Length), m.Value, chosen, failures, at);
                        }
                    }
                    else {
                        Field(m.Key, m.Value, outcome, failures, at);
                    }
                    break;
            }
        }
    }

    private static JsonArray Items(JsonArray matched) => new JsonArray(matched.Cast<JsonObject>().Select(t => JsonValue.Of(
        Str(t, "term_id") + ":" + Str(t, "member_key") + "@" + ((JsonInteger)t["peak_index"]).Value.ToString(CultureInfo.InvariantCulture))));

    private static JsonArray KOfList(JsonObject hypothesis) => new JsonArray(((JsonArray)hypothesis["k_of"]).Cast<JsonObject>().Select(k => JsonValue.Of(
        Str(k, "pointer") + " " + ((JsonInteger)k["k"]).Value.ToString(CultureInfo.InvariantCulture) + " "
        + ((JsonInteger)k["count"]).Value.ToString(CultureInfo.InvariantCulture) + " " + (((JsonBoolean)k["satisfied"]).Value ? "true" : "false"))));

    private static void Field(string key, JsonValue expected, JsonObject actual, List<string> failures, string? where = null) {
        var at = where ?? key;
        if (!actual.TryGetValue(key, out var value)) {
            failures.Add(at + ": no such field");
            return;
        }
        Compare(at, expected, value, failures);
    }

    /// <summary>Objects match on the expected keys (all keys when <paramref name="exactKeys"/>), arrays item by item, a
    /// string matches a number by its text, everything else by canonical text.</summary>
    private static void Compare(string at, JsonValue expected, JsonValue actual, List<string> failures, bool exactKeys = false) {
        if (expected is JsonObject eo && actual is JsonObject ao) {
            if (exactKeys && !eo.Keys.OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(ao.Keys.OrderBy(k => k, StringComparer.Ordinal), StringComparer.Ordinal)) {
                failures.Add(at + ": keys " + string.Join(",", eo.Keys) + " != " + string.Join(",", ao.Keys));
                return;
            }
            foreach (var m in eo.Members) {
                if (!ao.TryGetValue(m.Key, out var v)) {
                    failures.Add(at + "/" + m.Key + ": missing");
                    continue;
                }
                Compare(at + "/" + m.Key, m.Value, v, failures);
            }
            return;
        }
        if (expected is JsonArray ea && actual is JsonArray aa) {
            if (ea.Count != aa.Count) {
                failures.Add(at + ": expected " + CanonicalJson.ToText(ea) + ", got " + CanonicalJson.ToText(aa));
                return;
            }
            for (var i = 0; i < ea.Count; i++) {
                Compare(at + "/" + i.ToString(CultureInfo.InvariantCulture), ea[i], aa[i], failures);
            }
            return;
        }
        if (expected is JsonString es && actual is JsonNumberText an) {
            if (es.Value != an.Text) {
                failures.Add(at + ": expected " + es.Value + ", got " + an.Text);
            }
            return;
        }
        var e = CanonicalJson.ToText(expected);
        var a = CanonicalJson.ToText(actual);
        if (e != a) {
            failures.Add(at + ": expected " + e + ", got " + a);
        }
    }

    // ------------------------------------------------------------------------------------------------ margins

    /// <summary>
    /// The input margins of a case (design 7.2): for every rule of the set and every hypothesis whose precursor passes,
    /// every peak sits at least <see cref="MarginDa"/> from each target's tolerance (or window) boundary, from the
    /// 1.0 Da loss constraint and from the precursor region, and every hit's relative intensity at least
    /// <see cref="FloorMargin"/> (relative) from the term's floor. Every composition sits at least MarginDa from the
    /// precursor tolerance boundary. Returns the violations.
    /// </summary>
    public static List<string> AuditMargins(JsonObject caseObject) =>
        AuditMargins(DiagnosticRuleSet.LoadFiles(CaseFiles(caseObject), RuleSetSource.Inline), (JsonObject)caseObject["options"], (JsonObject)caseObject["input"]);

    /// <summary>The margins of <paramref name="input"/> under <paramref name="options"/> for every rule of
    /// <paramref name="set"/> (see <see cref="AuditMargins(JsonObject)"/>).</summary>
    public static List<string> AuditMargins(DiagnosticRuleSet set, JsonObject options, JsonObject input) {
        var violations = new List<string>();
        var settings = EvaluationSettings.From(Options(options));
        var spectrum = Spectrum(input);
        var view = SpectrumView.Create(spectrum);
        if (view.Status != SpectrumStatus.Evaluated) {
            return violations;
        }
        var p = spectrum.PrecursorMz;
        var kept = spectrum.Peaks.Select((pk, i) => (pk, i)).Where(x => x.pk.Mz > 0.0 && x.pk.Intensity > 0.0).ToList();
        var outside = kept.Where(x => Math.Abs(x.pk.Mz - p) > SpectrumView.PrecursorRegionDa).Select(x => x.pk.Intensity).ToList();
        var baseI = outside.Count > 0 ? outside.Max() : double.NaN;
        var maxI = kept.Max(x => x.pk.Intensity);
        foreach (var x in kept) {
            Near("peak " + x.i + " precursor region", Math.Abs(x.pk.Mz - p), SpectrumView.PrecursorRegionDa, violations);
        }
        for (var r = 0; r < set.Rules.Count; r++) {
            var rule = set.Rules[r];
            var engine = new RuleEngine(rule, set.CompiledRules[r], set.Tables, settings);
            var index = engine.Index;
            for (var c = 0; c < index.Compositions.Count; c++) {
                for (var a = 0; a < index.Tried.Count; a++) {
                    var h = index.Hypothesis(c, a, p);
                    var d = Math.Abs(p - h.HypothesisMz);
                    if (d > engine.PrecursorTolerance + 1.0) {
                        continue;
                    }
                    Near(rule.RuleId + " " + h.HypothesisId + " precursor", d, engine.PrecursorTolerance, violations);
                    if (d > engine.PrecursorTolerance) {
                        continue;
                    }
                    foreach (var term in engine.Builder.Build(h.Series, engine.DecoyPda, engine.LossReference, p)) {
                        foreach (var member in term.Members) {
                            var t = member.TargetMz;
                            var tol = term.IsWindow ? term.HalfWidthDa : engine.Tolerance.At(t);
                            foreach (var x in kept) {
                                var where = rule.RuleId + " " + h.HypothesisId + " " + term.Term.Tid + ":" + member.Key + " peak " + x.i;
                                var err = Math.Abs(x.pk.Mz - t);
                                Near(where + " tolerance", err, tol, violations);
                                if (err > tol) {
                                    continue;
                                }
                                if (member.IsLoss) {
                                    Near(where + " loss 1 Da", member.LossReferenceMz - x.pk.Mz, SpectrumView.MinLossDa, violations);
                                }
                                var rel = rule.Convention == RuleConvention.P2 ? x.pk.Intensity / baseI : x.pk.Intensity / maxI * 100.0;
                                var floor = term.Floor * settings.FloorScale;
                                if (!double.IsNaN(rel) && Math.Abs(rel - floor) < FloorMargin * floor) {
                                    violations.Add(where + " floor: rel " + rel.ToString("R", CultureInfo.InvariantCulture) + " at floor "
                                                   + floor.ToString("R", CultureInfo.InvariantCulture));
                                }
                            }
                        }
                    }
                }
            }
        }
        return violations;
    }

    private static void Near(string where, double distance, double boundary, List<string> violations) {
        if (Math.Abs(distance - boundary) < MarginDa) {
            violations.Add(where + ": " + distance.ToString("R", CultureInfo.InvariantCulture) + " within " + MarginDa.ToString("R", CultureInfo.InvariantCulture)
                           + " of " + boundary.ToString("R", CultureInfo.InvariantCulture));
        }
    }

    // ------------------------------------------------------------------------------------------------ loader errors

    /// <summary>
    /// <c>engine/loader-errors.json</c> (OQ02): one case per loader error code, each a self-contained file map (the
    /// CPython vector's base files with its patch applied, base64) with the C# loader's error list and the defect's
    /// <c>{code, file, pointer}</c>. The C# list must equal the CPython expectation of the same vector.
    /// </summary>
    public static JsonObject LoaderErrorsDocument() {
        var vectors = (JsonObject)DiagnosticRuleFixtures.Vectors()["loader_errors"];
        var baseFiles = (JsonObject)vectors["base_files"];
        var cases = new List<JsonValue>();
        foreach (var c in ((JsonArray)vectors["cases"]).Cast<JsonObject>()) {
            if (!(c["code"] is JsonString code)) {
                continue; // the clean control holds no defect
            }
            var files = DiagnosticRuleFixtures.Patched(baseFiles, (JsonObject)c["patch"]);
            var errors = DiagnosticRuleSetLoader.Validate(files, checkPin: true).Errors;
            var expected = DiagnosticRuleFixtures.ErrorList((JsonArray)c["errors"]);
            if (DiagnosticRuleFixtures.ErrorList(errors) != expected) {
                throw new InvalidOperationException(Str(c, "name") + ": the C# loader disagrees with the CPython vector");
            }
            var defect = errors.First(e => e.Code == code.Value);
            cases.Add(new JsonObject(new[]
            {
                M("check_pin", JsonValue.Of(true)),
                M("code", JsonValue.Of(code.Value)),
                M("errors", ErrorArray(errors)),
                M("expect", new JsonObject(new[] { M("code", JsonValue.Of(defect.Code)), M("file", JsonValue.Of(defect.File)), M("pointer", JsonValue.Of(defect.Pointer)) })),
                M("files", new JsonObject(files.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => M(kv.Key, JsonValue.Of(Convert.ToBase64String(kv.Value)))))),
                M("name", JsonValue.Of(Str(c, "name"))),
            }));
        }
        return Document(LoaderErrorsKind, cases);
    }

    public static JsonArray ErrorArray(IEnumerable<RuleLoadError> errors) =>
        new JsonArray(errors.Select(e => (JsonValue)new JsonArray(new[] { JsonValue.Of(e.Code), JsonValue.Of(e.File), JsonValue.Of(e.Pointer) })));

    // ------------------------------------------------------------------------------------------------ manifest

    /// <summary>
    /// <c>manifest.json</c>: each fixture's raw sha256 and case count, and the shipped rule set (version and rule-set
    /// sha256) that the per-rule fixtures and <c>masses.json</c> were generated from. It is the one place a test reads
    /// the shipped set's sha256 from (<see cref="ShippedRuleFixtures.PinnedRuleSet"/>).
    /// </summary>
    public static JsonObject Manifest(IEnumerable<(string Path, byte[] Bytes)> files, string ruleSetVersion, string ruleSetSha256) => new JsonObject(new[]
    {
        M("engine_semantics", JsonValue.Of(DiagnosticRuleEvaluator.EngineSemantics)),
        M("files", new JsonObject(files.OrderBy(f => f.Path, StringComparer.Ordinal).Select(f => M(f.Path, new JsonObject(new[]
        {
            M("cases", JsonValue.Of(((JsonArray)StrictJsonReader.Read(f.Bytes)["cases"]).Count)),
            M("sha256", JsonValue.Of(Sha256Hex.Of(f.Bytes))),
        }))))),
        M("format", JsonValue.Of(ManifestFormat)),
        M("format_version", JsonValue.Of(1)),
        M("rule_set", new JsonObject(new[] { M("sha256", JsonValue.Of(ruleSetSha256)), M("version", JsonValue.Of(ruleSetVersion)) })),
    });

    /// <summary>The engine fixtures' SOURCE.md, naming each file's sha256 and case count.</summary>
    public static string SourceMd(byte[] semantics, int semanticsCases, byte[] loaderErrors, int loaderCases) {
        const string NL = "\n";
        return "# Engine parity fixtures" + NL + NL
            + "Owned by MsdialWorkbench (the C# engine is the oracle); the Python port copies this folder from a pinned commit." + NL
            + "Generated by `DiagnosticRuleFixtureParityTests` from the hand-derived cases in" + NL
            + "`tests/Common/CommonStandardTests/StructureFinder/DiagnosticRules/SemanticsFixtureCases.cs` and from the" + NL
            + "`loader_errors` vectors of `../cjson-vectors.json`. Every input is synthetic." + NL + NL
            + "- `semantics.json` (" + semanticsCases.ToString(CultureInfo.InvariantCulture) + " cases): sha256 `" + Sha256Hex.Of(semantics) + "`" + NL
            + "- `loader-errors.json` (" + loaderCases.ToString(CultureInfo.InvariantCulture) + " cases): sha256 `" + Sha256Hex.Of(loaderErrors) + "`" + NL
            + "- engine semantics: `" + DiagnosticRuleEvaluator.EngineSemantics + "`" + NL + NL
            + "Both files are `{format, format_version, engine_semantics, rule_set_version, rule_set_sha256, kind, cases}` written" + NL
            + "with indent 1, `ensure_ascii`, LF and a trailing newline. `rule_set_version` and `rule_set_sha256` are null: each" + NL
            + "case carries its own rule set. `../manifest.json` pins each file's sha256 and case count." + NL + NL
            + "## semantics.json" + NL + NL
            + "Each case holds:" + NL + NL
            + "- `files`: every rule-set file as a JSON object. The bytes loaded are its storage form (sorted keys, indent 1," + NL
            + "  `ensure_ascii`, LF and a trailing newline), loaded unpinned as an inline set. There is no `ruleset-history.tsv`," + NL
            + "  so a pinned load reports only `ruleset_unpinned`. A scaffold list no rule uses is left out." + NL
            + "- `options`: only the non-default ones, with decimal text for doubles." + NL
            + "- `input`: `peaks` as `[m/z, intensity]` decimal text with at most 6 decimals, written from integers;" + NL
            + "  `precursor_mz` as text, or null for NaN; `ion_mode` `pos`, `neg` or null; `instrument_type`; `adduct_evidence`" + NL
            + "  null or `{adduct, tier}`." + NL
            + "- `expect`: the hand-written expectation; `result`: the canonical result text; `result_sha256`." + NL + NL
            + "Decimal text converts to a double by one correctly rounded division" + NL
            + "(Python `float(text)`). Peaks sit at least 2e-6 Da from every tolerance boundary and at least 1e-6 (relative)" + NL
            + "from every floor. The deliberate equalities are ratio comparisons of identical doubles: `ordering_equal` (0.4" + NL
            + "against 0.4) and `veto_stronger_than_equal` (0.2 against 0.5 * 0.4, where halving is exact)." + NL + NL
            + "## loader-errors.json" + NL + NL
            + "One case per loader error code: `files` maps each rule-set path to its base64 bytes, `check_pin` is true (the" + NL
            + "version pin is checked against `ruleset-history.tsv`), `errors` is the full ordered list `[code, file, pointer]`" + NL
            + "and `expect` is the defect's `{code, file, pointer}`. Each list equals the CPython expectation of the same vector." + NL + NL
            + "## Regenerate" + NL + NL
            + "Set `" + WriteVariable + "=<dir>` and run the CommonStandardTests filter `DiagnosticRuleFixtureParityTests`;" + NL
            + "the files are written under `<dir>` (this layout) and the test ends Inconclusive. Copy them here unchanged." + NL;
    }

    /// <summary>Every generated engine file, by path relative to the fixture folder. <c>manifest.json</c>, which also
    /// pins the per-rule fixtures and the shipped rule set, is generated by <see cref="ShippedRuleFixtures.Generate"/>.</summary>
    public static List<(string Path, byte[] Bytes)> Generate() {
        var semantics = PrettyJson.ToBytes(SemanticsDocument(SemanticsFixtureCases.All()));
        var loader = PrettyJson.ToBytes(LoaderErrorsDocument());
        var nSemantics = ((JsonArray)StrictJsonReader.Read(semantics)["cases"]).Count;
        var nLoader = ((JsonArray)StrictJsonReader.Read(loader)["cases"]).Count;
        var source = Encoding.ASCII.GetBytes(SourceMd(semantics, nSemantics, loader, nLoader));
        return new List<(string, byte[])> { (SemanticsFile, semantics), (LoaderErrorsFile, loader), (SourceFile, source) };
    }
}
