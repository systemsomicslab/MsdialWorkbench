using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Results
{
    /// <summary>
    /// The canonical result JSON (design 4.11): <see cref="CanonicalJson"/> (ordinal-sorted keys, "," and ":"
    /// separators, ASCII with Python ensure_ascii escaping) of the result's fields. Doubles are
    /// <see cref="ExactDecimal.Format"/> text, exact and half-even: m/z and Da with 6 decimals, ppm with 3, relative
    /// intensities, scores and intensities with 6. A non-finite double is written as null; only
    /// <c>precursor_mz</c> can be one (status <c>no_precursor</c>). Integers use the invariant culture.
    /// </summary>
    public static class DiagnosticResultJson
    {
        /// <summary>Decimals of m/z and Da values.</summary>
        public const int MzDecimals = 6;
        /// <summary>Decimals of ppm values.</summary>
        public const int PpmDecimals = 3;
        /// <summary>Decimals of relative intensities, scores and intensities.</summary>
        public const int IntensityDecimals = 6;

        /// <summary>The canonical JSON text of a result.</summary>
        public static string Write(DiagnosticRuleResult result) => CanonicalJson.ToText(ToJson(result));

        /// <summary>The canonical JSON bytes (ASCII) of a result.</summary>
        public static byte[] ToBytes(DiagnosticRuleResult result) => Encoding.ASCII.GetBytes(Write(result));

        /// <summary>The result as a JSON value tree.</summary>
        internal static JsonObject ToJson(DiagnosticRuleResult r) {
            if (r is null) {
                throw new ArgumentNullException(nameof(r));
            }
            var options = new List<KeyValuePair<string, JsonValue>>();
            foreach (var o in r.Options) {
                options.Add(M(o.Key, OptionValue(o.Key, o.Value)));
            }
            var outcomes = new List<JsonValue>();
            foreach (var o in r.Outcomes) {
                outcomes.Add(Outcome(o));
            }
            var refusals = new List<JsonValue>();
            foreach (var x in r.Refusals) {
                refusals.Add(Refusal(x));
            }
            return Obj(
                M("adduct_tier", Str(r.AdductTier)),
                M("base_intensity", r.BaseIntensity.HasValue ? Num(r.BaseIntensity.Value, IntensityDecimals) : JsonValue.Null),
                M("eligible_rule_conflict", JsonValue.Of(r.EligibleRuleConflict)),
                M("engine_semantics", Str(r.EngineSemantics)),
                M("fired_motifs", Strings(r.FiredMotifs)),
                M("identity", Str(r.Identity)),
                M("input_sha256", Str(r.InputSha256)),
                M("instrument_class", Str(r.InstrumentClass)),
                M("instrument_type", JsonValue.OfNullable(r.InstrumentType)),
                M("n_peaks_dropped", JsonValue.Of(r.NPeaksDropped)),
                M("n_peaks_in", JsonValue.Of(r.NPeaksIn)),
                M("narrowest_motifs", Strings(r.NarrowestMotifs)),
                M("not_measured", Strings(r.NotMeasured)),
                M("options", new JsonObject(options)),
                M("outcomes", new JsonArray(outcomes)),
                M("precondition_mode", Str(r.PreconditionMode)),
                M("precursor_mz", Num(r.PrecursorMz, MzDecimals)),
                M("refusals", new JsonArray(refusals)),
                M("rule_conflict", JsonValue.Of(r.RuleConflict)),
                M("rule_set", Obj(
                    M("pinned", JsonValue.Of(r.RuleSet.Pinned)),
                    M("sha256", Str(r.RuleSet.Sha256)),
                    M("source", Str(SourceName(r.RuleSet.Source))),
                    M("version", Str(r.RuleSet.Version)))),
                M("rules_evaluated", JsonValue.Of(r.RulesEvaluated)),
                M("status", Str(r.Status)));
        }

        /// <summary><c>embedded</c>, <c>directory</c> or <c>inline</c>.</summary>
        internal static string SourceName(RuleSetSource source) {
            switch (source) {
                case RuleSetSource.Embedded: return "embedded";
                case RuleSetSource.Directory: return "directory";
                default: return "inline";
            }
        }

        /// <summary>
        /// An option of the <c>options</c> object, typed by its key (OQ08): the enum options (<c>filter</c>,
        /// <c>overlap</c>, <c>loss_reference_override</c>) as strings, <c>strict</c> and <c>verbose</c> as booleans, the
        /// integer options (<c>decoy_shift_da</c>, <c>k_offset</c>, <c>assignment_cap</c>) as integers, and the
        /// doubles as their <see cref="ExactDecimal"/> text with 6 decimals, the same text as the identity string.
        /// </summary>
        private static JsonValue OptionValue(string key, string text) {
            switch (key) {
                case "filter":
                case "overlap":
                case "loss_reference_override":
                    return Str(text);
                case "strict":
                case "verbose":
                    return JsonValue.Of(string.Equals(text, "true", StringComparison.Ordinal));
                default:
                    // Integers are invariant digits and doubles ExactDecimal text, both valid JSON numbers as written.
                    return new JsonNumberText(text);
            }
        }

        private static JsonValue Refusal(RuleRefusal r) => Obj(
            M("code", Str(r.Code)),
            M("delta_da", Num(r.DeltaDa, MzDecimals)),
            M("hypothesis_id", Str(r.HypothesisId)),
            M("items", Strings(r.Items)),
            M("rule_id", Str(r.RuleId)));

        private static JsonValue Outcome(RuleOutcome o) {
            var branchFired = new List<KeyValuePair<string, JsonValue>>();
            foreach (var b in o.BranchFired) {
                branchFired.Add(M(b.Pointer, Bools(b.Children)));
            }
            var all = new List<JsonValue>();
            foreach (var h in o.All) {
                all.Add(Hypothesis(h));
            }
            return Obj(
                M("all", new JsonArray(all)),
                M("author_reviewed", JsonValue.Of(o.AuthorReviewed)),
                M("branch_fired", new JsonObject(branchFired)),
                M("chosen", o.Chosen is null ? JsonValue.Null : Hypothesis(o.Chosen)),
                M("claim_eligible", JsonValue.Of(o.ClaimEligible)),
                M("claim_ineligible", Strings(o.ClaimIneligible)),
                M("claim_level", Str(o.ClaimLevel)),
                M("convention", Str(o.Convention)),
                M("convention_label", Str(o.ConventionLabel)),
                M("fired", JsonValue.Of(o.Fired)),
                M("loss_reference", Str(o.LossReference)),
                M("motif_id", Str(o.MotifId)),
                M("n_distinct_formulas", JsonValue.Of(o.NDistinctFormulas)),
                M("n_hypotheses", JsonValue.Of(o.NHypotheses)),
                M("n_hypotheses_fired", JsonValue.Of(o.NHypothesesFired)),
                M("parameters_origin", Str(o.ParametersOrigin)),
                M("precondition_failure", JsonValue.OfNullable(o.PreconditionFailure)),
                M("precursor_tolerance_da", Num(o.PrecursorToleranceDa, MzDecimals)),
                M("product_tolerance", Obj(
                    M("abs_da", Num(o.ProductToleranceAbsDa, MzDecimals)),
                    M("ppm", Num(o.ProductTolerancePpm, PpmDecimals)),
                    M("source", Str(o.ProductToleranceSource)))),
                M("rule_id", Str(o.RuleId)),
                M("source_checked", JsonValue.Of(o.SourceChecked)),
                M("tied_hypothesis_ids", Strings(o.TiedHypothesisIds)),
                M("variant_single_ion", JsonValue.Of(o.VariantSingleIon)),
                M("version", JsonValue.Of(o.Version)),
                M("veto_blocked", JsonValue.Of(o.VetoBlocked)),
                M("within_calibration", JsonValue.Of(o.WithinCalibration)));
        }

        private static JsonValue Hypothesis(HypothesisOutcome h) {
            var units = new List<KeyValuePair<string, JsonValue>>();
            foreach (var u in h.Units) {
                units.Add(M(u.Key, JsonValue.Of(u.Value)));
            }
            var vetoes = new List<JsonValue>();
            foreach (var v in h.Vetoes) {
                vetoes.Add(Obj(
                    M("fired", JsonValue.Of(v.Fired)),
                    M("i_a", Num(v.IA, IntensityDecimals)),
                    M("i_b_max", Num(v.IBMax, IntensityDecimals)),
                    M("peak_index", v.PeakIndex.HasValue ? JsonValue.Of(v.PeakIndex.Value) : JsonValue.Null),
                    M("peak_mz", v.PeakMz.HasValue ? Num(v.PeakMz.Value, MzDecimals) : JsonValue.Null),
                    M("term_id", Str(v.TermId))));
            }
            var kOf = new List<JsonValue>();
            foreach (var k in h.KOf) {
                kOf.Add(Obj(
                    M("count", JsonValue.Of(k.Count)),
                    M("k", JsonValue.Of(k.K)),
                    M("pointer", Str(k.Pointer)),
                    M("satisfied", JsonValue.Of(k.Satisfied))));
            }
            var branches = new List<JsonValue>();
            foreach (var b in h.Branches) {
                branches.Add(Obj(M("children", Bools(b.Children)), M("pointer", Str(b.Pointer))));
            }
            return Obj(
                M("adduct", Str(h.Adduct)),
                M("adduct_source", Str(h.AdductSource)),
                M("adduct_tier", Str(h.AdductTier)),
                M("branches", new JsonArray(branches)),
                M("composition_id", Str(h.CompositionId)),
                M("failure", JsonValue.OfNullable(h.Failure)),
                M("fired", JsonValue.Of(h.Fired)),
                M("fired_ignoring_vetoes", JsonValue.Of(h.FiredIgnoringVetoes)),
                M("hypothesis_id", Str(h.HypothesisId)),
                M("hypothesis_mz", Num(h.HypothesisMz, MzDecimals)),
                M("k_of", new JsonArray(kOf)),
                M("matched", Matched(h.Matched)),
                M("neutral_formula", Str(h.NeutralFormula)),
                M("precursor_error_da", Num(h.PrecursorErrorDa, MzDecimals)),
                M("scaffold_id", JsonValue.OfNullable(h.ScaffoldId)),
                M("score", Num(h.Score, IntensityDecimals)),
                M("units", new JsonObject(units)),
                M("upgrades", Matched(h.Upgrades)),
                M("vetoes", new JsonArray(vetoes)));
        }

        private static JsonArray Matched(IReadOnlyList<MatchedTerm> terms) {
            var list = new List<JsonValue>();
            foreach (var m in terms) {
                list.Add(Obj(
                    M("error_da", Num(m.ErrorDa, MzDecimals)),
                    M("error_ppm", Num(m.ErrorPpm, PpmDecimals)),
                    M("ion_formula", Str(m.IonFormula)),
                    M("kind", Str(m.Kind)),
                    M("member_key", Str(m.MemberKey)),
                    M("peak_index", JsonValue.Of(m.PeakIndex)),
                    M("peak_mz", Num(m.PeakMz, MzDecimals)),
                    M("rel_intensity", Num(m.RelIntensity, IntensityDecimals)),
                    M("role", Str(m.Role)),
                    M("target_mz", Num(m.TargetMz, MzDecimals)),
                    M("term_id", Str(m.TermId)),
                    M("unit_id", m.UnitId.HasValue ? JsonValue.Of(m.UnitId.Value) : JsonValue.Null)));
            }
            return new JsonArray(list);
        }

        /// <summary>A double as exact fixed-point text, or null when it is not finite.</summary>
        private static JsonValue Num(double value, int decimals) {
            var text = ExactDecimal.FormatOrNull(value, decimals);
            return text is null ? JsonValue.Null : new JsonNumberText(text);
        }

        private static JsonValue Str(string value) => JsonValue.Of(value);

        private static JsonArray Strings(IReadOnlyList<string> values) {
            var list = new List<JsonValue>(values.Count);
            foreach (var v in values) {
                list.Add(JsonValue.Of(v));
            }
            return new JsonArray(list);
        }

        private static JsonArray Bools(IReadOnlyList<bool> values) {
            var list = new List<JsonValue>(values.Count);
            foreach (var v in values) {
                list.Add(JsonValue.Of(v));
            }
            return new JsonArray(list);
        }

        private static KeyValuePair<string, JsonValue> M(string key, JsonValue value) => new KeyValuePair<string, JsonValue>(key, value);

        private static JsonObject Obj(params KeyValuePair<string, JsonValue>[] members) => new JsonObject(members);
    }
}
