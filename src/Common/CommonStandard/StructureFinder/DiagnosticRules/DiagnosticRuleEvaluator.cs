using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace CompMs.Common.StructureFinder.DiagnosticRules
{
    /// <summary>
    /// Evaluates the rules of a <see cref="DiagnosticRuleSet"/> on spectra (design 4). It is immutable, pure and
    /// thread-safe: the options are copied and checked once, every rule's hypothesis index, targets and logic are built
    /// at construction, there is no lazy cache, and each evaluation allocates its own state. There is no clock,
    /// randomness, parallelism or process state inside; rules are evaluated in ordinal rule_id order.
    /// </summary>
    public sealed class DiagnosticRuleEvaluator
    {
        /// <summary>The engine semantics identity, bumped on any semantic change; <c>RuleSemanticHash</c> uses this
        /// constant, so a change stales every review.</summary>
        public const string EngineSemantics = "msfinder-diagnostic-rules/1";

        /// <summary>The prefix of <see cref="IdentityString"/>.</summary>
        public const string IdentityPrefix = "msfinder.diagnostic_rules?";

        private readonly DiagnosticRuleSet _set;
        private readonly EvaluationSettings _settings;
        private readonly RuleEngine[] _engines;
        private readonly MotifNestingClosure _nesting;
        private readonly IReadOnlyList<string> _notMeasured;
        private readonly IReadOnlyList<KeyValuePair<string, string>> _options;

        /// <summary>
        /// Builds an evaluator. The rules outside <see cref="DiagnosticEvaluationOptions.Filter"/> are listed as
        /// <c>not_measured</c>; the others are checked for undeclared overlaps (design 4.8) and the refused ones are
        /// listed in <see cref="Refusals"/> and not evaluated.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> or <paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentException">An option is invalid: a non-finite or negative tolerance, a FloorScale
        /// that is not finite and positive, an AssignmentCap below 1, |DecoyShiftDa| above 9007 (the shift, not every
        /// shifted mass, is bounded below 2^53 pDa), or an undefined enum value (OQ20).</exception>
        /// <exception cref="DiagnosticRuleRefusedException"><see cref="DiagnosticEvaluationOptions.Strict"/> is set
        /// and a rule is refused (OQ21).</exception>
        public DiagnosticRuleEvaluator(DiagnosticRuleSet set, DiagnosticEvaluationOptions options) {
            _set = set ?? throw new ArgumentNullException(nameof(set));
            _settings = EvaluationSettings.From(options);
            var order = new int[set.Rules.Count];
            for (var i = 0; i < order.Length; i++) {
                order[i] = i;
            }
            Array.Sort(order, (a, b) => string.CompareOrdinal(set.Rules[a].RuleId, set.Rules[b].RuleId));
            var filtered = new List<RuleEngine>();
            var notMeasured = new List<string>();
            foreach (var i in order) {
                var rule = set.Rules[i];
                if (Passes(rule, _settings.Filter)) {
                    filtered.Add(new RuleEngine(rule, set.CompiledRules[i], set.Tables, _settings));
                }
                else {
                    notMeasured.Add(rule.RuleId);
                }
            }
            Refusals = OverlapValidator.Validate(filtered, _settings);
            var refused = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in Refusals) {
                refused.Add(r.RuleId);
            }
            var engines = new List<RuleEngine>();
            foreach (var e in filtered) {
                if (!refused.Contains(e.Rule.RuleId)) {
                    engines.Add(e);
                }
            }
            _engines = engines.ToArray();
            _notMeasured = new ReadOnlyCollection<string>(notMeasured);
            _nesting = new MotifNestingClosure(set.Nesting);
            _options = OptionList(_settings);
            IdentityString = Identity(set.Identity, _options);
        }

        /// <summary>The rules refused at construction (design 4.8), in ordinal rule_id order.</summary>
        public IReadOnlyList<RuleRefusal> Refusals { get; }

        /// <summary>
        /// <c>msfinder.diagnostic_rules?k=v&amp;...</c> with the keys sorted ordinally: <c>engine</c>,
        /// <c>ruleset</c> (the version), <c>ruleset_sha256</c> and every option not at its default (OQ08). Doubles are
        /// <see cref="ExactDecimal"/> text with 6 decimals and integers invariant. <c>strict</c> and <c>verbose</c>
        /// change no evaluation and are left out.
        /// </summary>
        public string IdentityString { get; }

        /// <summary>Evaluates a spectrum without adduct evidence: <c>adduct_ambiguous</c>, tier <c>assumed</c>.
        /// Never throws for a spectrum; its status says when it could not be evaluated.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="spectrum"/> is null.</exception>
        public DiagnosticRuleResult Evaluate(DiagnosticSpectrum spectrum) {
            if (spectrum is null) {
                throw new ArgumentNullException(nameof(spectrum));
            }
            return Run(spectrum, null);
        }

        /// <summary>Evaluates a spectrum with adduct evidence: <c>mz_only</c> at the evidence tier, trying only the
        /// evidence adduct and only for rules that list it. Never throws for a spectrum.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="spectrum"/> or <paramref name="evidence"/> is null.</exception>
        public DiagnosticRuleResult Evaluate(DiagnosticSpectrum spectrum, AdductEvidence evidence) {
            if (spectrum is null) {
                throw new ArgumentNullException(nameof(spectrum));
            }
            if (evidence is null) {
                throw new ArgumentNullException(nameof(evidence));
            }
            return Run(spectrum, evidence);
        }

        private DiagnosticRuleResult Run(DiagnosticSpectrum spectrum, AdductEvidence? evidence) {
            var view = SpectrumView.Create(spectrum);
            var mode = evidence is null ? PreconditionMode.AdductAmbiguous : PreconditionMode.MzOnly;
            var tier = TierName(evidence?.Tier ?? AdductTier.Assumed);
            var instrumentClass = InstrumentClassifier.Classify(spectrum.InstrumentType);
            var outcomes = new List<RuleOutcome>();
            var fired = new SortedSet<string>(StringComparer.Ordinal);
            var eligible = new SortedSet<string>(StringComparer.Ordinal);
            if (view.Status == SpectrumStatus.Evaluated) {
                foreach (var engine in _engines) {
                    var outcome = EvaluateRule(engine, view, evidence, mode, tier, instrumentClass);
                    if (outcome.Fired) {
                        fired.Add(outcome.MotifId);
                        if (outcome.ClaimEligible) {
                            eligible.Add(outcome.MotifId);
                        }
                    }
                    if (_settings.Verbose || outcome.NHypotheses > 0) {
                        outcomes.Add(outcome);
                    }
                }
            }
            var firedMotifs = new List<string>(fired);
            var eligibleMotifs = new List<string>(eligible);
            return new DiagnosticRuleResult(
                EngineSemantics, IdentityString, _set.Identity, _options, view.StatusName, ModeName(mode), tier, instrumentClass,
                spectrum.InstrumentType, spectrum.PrecursorMz, view.HasBase ? view.BaseIntensity : (double?)null, view.NPeaksIn,
                view.NPeaksDropped, InputSha256(spectrum), _engines.Length, _notMeasured, Refusals,
                new ReadOnlyCollection<RuleOutcome>(outcomes), new ReadOnlyCollection<string>(firedMotifs),
                new ReadOnlyCollection<string>(_nesting.Narrowest(firedMotifs)), _nesting.Conflict(firedMotifs), _nesting.Conflict(eligibleMotifs));
        }

        /// <summary>One rule (design 4.9): its hypotheses, their evaluation, the chosen hypothesis, ties, branches and
        /// claim eligibility.</summary>
        private RuleOutcome EvaluateRule(RuleEngine engine, SpectrumView view, AdductEvidence? evidence, PreconditionMode mode, string tier,
                                         string instrumentClass) {
            var rule = engine.Rule;
            var set = engine.Index.Select(view.PrecursorMz, view.IonMode!.Value, evidence, engine.PrecursorTolerance);
            var evaluations = new List<HypothesisEvaluation>(set.Hypotheses.Count);
            foreach (var h in set.Hypotheses) {
                evaluations.Add(engine.Evaluate(view, h));
            }
            // Hypotheses are in ordinal hypothesis_id order, so the first one with the highest score (exact double)
            // is the lowest id among the best.
            var chosen = -1;
            var nFired = 0;
            for (var i = 0; i < evaluations.Count; i++) {
                if (evaluations[i].Fired) {
                    nFired++;
                    if (chosen < 0 || evaluations[i].Score > evaluations[chosen].Score) {
                        chosen = i;
                    }
                }
            }
            var ruleFired = nFired > 0;
            var vetoBlocked = false;
            if (!ruleFired) {
                // OQ05: a veto-blocked rule reports its best hypothesis that fired with every not node true.
                for (var i = 0; i < evaluations.Count; i++) {
                    if (evaluations[i].FiredIgnoringVetoes && (chosen < 0 || evaluations[i].Score > evaluations[chosen].Score)) {
                        chosen = i;
                    }
                }
                vetoBlocked = chosen >= 0;
            }
            var tied = new List<string>();
            if (ruleFired) {
                foreach (var e in evaluations) {
                    if (e.Fired && e.Score == evaluations[chosen].Score) {
                        tied.Add(e.Hypothesis.HypothesisId);
                    }
                }
            }
            var formulas = new HashSet<string>(StringComparer.Ordinal);
            foreach (var h in set.Hypotheses) {
                formulas.Add(h.NeutralFormula);
            }
            var branchFired = new List<BranchOutcome>();
            var fires = engine.BranchFired(view, set.Hypotheses);
            for (var a = 0; a < fires.Length; a++) {
                branchFired.Add(new BranchOutcome(engine.Plan.AnyNodes[a].Pointer, new ReadOnlyCollection<bool>(fires[a])));
            }
            HypothesisOutcome? chosenOutcome = null;
            var all = new List<HypothesisOutcome>();
            for (var i = 0; i < evaluations.Count; i++) {
                if (i == chosen || _settings.Verbose) {
                    var outcome = Outcome(engine, evaluations[i], tier, i == chosen);
                    if (i == chosen) {
                        chosenOutcome = outcome;
                    }
                    if (_settings.Verbose) {
                        all.Add(outcome);
                    }
                }
            }
            var ineligible = ClaimIneligible(engine, mode, instrumentClass);
            var tolerance = engine.Tolerance;
            return new RuleOutcome(
                rule.RuleId, rule.Version, rule.Claim.MotifId, ClaimLevelName(rule.Claim.ClaimLevel), rule.AuthorReviewed, rule.SourceChecked,
                OriginName(rule.ParametersOrigin), rule.Convention == RuleConvention.P2 ? "p2" : "msdial",
                rule.Convention == RuleConvention.P2 ? "exact" : "approximate", tolerance.AbsDa, tolerance.Ppm, tolerance.SourceName,
                engine.PrecursorTolerance, engine.LossReference == LossReference.Hypothesis ? "hypothesis" : "observed",
                PreconditionFailureName(set.Failure), set.Hypotheses.Count, formulas.Count, nFired, ruleFired, vetoBlocked,
                ineligible.Count == 0, ineligible, engine.VariantSingleIon, Contains(rule.CalibratedOn, instrumentClass), chosenOutcome,
                new ReadOnlyCollection<string>(tied), new ReadOnlyCollection<BranchOutcome>(branchFired),
                new ReadOnlyCollection<HypothesisOutcome>(all));
        }

        /// <summary>
        /// The failed claim-eligibility conditions (design 4.9), in order: not_reviewed (not author_reviewed, or no
        /// review), instrument_class (not in review.instrument_classes), precondition_mode (not the rule's
        /// primary_precondition_mode), tolerance_differs (the effective product tolerance {abs_da, ppm, source} or
        /// precursor tolerance is not the reviewed one, exact double equality) and variant_active (a variant option, or
        /// variant_single_ion). Without a review, instrument_class and tolerance_differs have nothing to compare with
        /// and are not listed (OQ06).
        /// </summary>
        private IReadOnlyList<string> ClaimIneligible(RuleEngine engine, PreconditionMode mode, string instrumentClass) {
            var rule = engine.Rule;
            var review = rule.Review;
            var output = new List<string>();
            if (!rule.AuthorReviewed || review is null) {
                output.Add(RuleOutcome.NotReviewed);
            }
            if (review != null && !Contains(review.InstrumentClasses, instrumentClass)) {
                output.Add(RuleOutcome.InstrumentClassNotReviewed);
            }
            if (mode != rule.PrimaryPreconditionMode) {
                output.Add(RuleOutcome.PreconditionModeDiffers);
            }
            if (review != null && !ReviewedTolerances(engine, review)) {
                output.Add(RuleOutcome.ToleranceDiffers);
            }
            if (_settings.VariantActive || engine.VariantSingleIon) {
                output.Add(RuleOutcome.VariantActive);
            }
            return new ReadOnlyCollection<string>(output);
        }

        private static bool ReviewedTolerances(RuleEngine engine, Review review) {
            var t = engine.Tolerance;
            var reviewed = review.ProductTolerance;
            var sameSource = t.Source == ToleranceSource.P2 ? reviewed.Source == ProductToleranceKind.P2
                           : t.Source == ToleranceSource.Msfinder && reviewed.Source == ProductToleranceKind.Msfinder;
            return sameSource && t.AbsDa == reviewed.AbsDa && t.Ppm == reviewed.Ppm && engine.PrecursorTolerance == review.PrecursorToleranceDa;
        }

        private HypothesisOutcome Outcome(RuleEngine engine, HypothesisEvaluation e, string tier, bool chosen) {
            var h = e.Hypothesis;
            var matched = new List<MatchedTerm>();
            foreach (var m in e.Matched) {
                matched.Add(Matched(engine, m, m.UnitId));
            }
            var upgrades = new List<MatchedTerm>();
            if (chosen) {
                // OQ13: upgrades are reported for the chosen hypothesis only.
                foreach (var m in e.Upgrades) {
                    upgrades.Add(Matched(engine, m, null));
                }
            }
            var vetoes = new List<VetoOutcome>();
            foreach (var v in e.Vetoes) {
                vetoes.Add(new VetoOutcome(v.Term.Tid, v.Fired, v.IA, v.IBMax, v.Peak?.InputIndex, v.Peak?.Mz));
            }
            var kOf = new List<KOfOutcome>();
            foreach (var k in e.KOf) {
                kOf.Add(new KOfOutcome(k.Pointer, k.K, k.Count, k.Satisfied));
            }
            var branches = new List<BranchOutcome>();
            foreach (var b in e.Branches) {
                branches.Add(new BranchOutcome(b.Pointer, b.Children));
            }
            return new HypothesisOutcome(
                h.HypothesisId, h.CompositionId, h.ScaffoldId, h.Units, h.Adduct, h.AdductSource, tier, h.NeutralFormula, h.HypothesisMz,
                h.PrecursorErrorDa, e.Fired, e.FiredIgnoringVetoes, e.Score, FailureName(e.Failure),
                new ReadOnlyCollection<MatchedTerm>(matched), new ReadOnlyCollection<VetoOutcome>(vetoes), new ReadOnlyCollection<KOfOutcome>(kOf),
                new ReadOnlyCollection<BranchOutcome>(branches), new ReadOnlyCollection<MatchedTerm>(upgrades));
        }

        private static MatchedTerm Matched(RuleEngine engine, MatchedItem m, int? unitId) {
            var target = m.Member.TargetMz;
            var error = m.Peak.ErrorDa;
            return new MatchedTerm(m.Term.Term.Tid, m.Member.Key, m.Term.Term.Kind, m.Term.Term.Role, engine.Builder.IonFormula(m.Member), target,
                                   m.Peak.InputIndex, m.Peak.Mz, error, error / target * 1000000.0, m.Peak.Rel, unitId);
        }

        /// <summary>
        /// <c>input_sha256</c> (design 4.11): the sha256 of the canonical bytes of <c>{"instrument_type": str|null,
        /// "ion_mode": "positive"|"negative"|null, "peaks": [[mz_bits, intensity_bits], ...], "precursor_mz_bits": ...}</c>,
        /// with the peaks in input order (before the engine drops any) and every double as its big-endian IEEE-754
        /// pattern in 16 lowercase hex digits, NaN canonicalised to 7ff8000000000000 and -0.0 kept.
        /// </summary>
        internal static string InputSha256(DiagnosticSpectrum spectrum) {
            var peaks = new List<JsonValue>(spectrum.Peaks.Count);
            foreach (var p in spectrum.Peaks) {
                peaks.Add(new JsonArray(new[] { JsonValue.Of(IeeeBits.Of(p.Mz)), JsonValue.Of(IeeeBits.Of(p.Intensity)) }));
            }
            var mode = spectrum.IonMode.HasValue
                ? JsonValue.Of(spectrum.IonMode.Value == DiagnosticIonMode.Positive ? "positive" : "negative")
                : JsonValue.Null;
            var doc = new JsonObject(new[]
            {
                new KeyValuePair<string, JsonValue>("instrument_type", JsonValue.OfNullable(spectrum.InstrumentType)),
                new KeyValuePair<string, JsonValue>("ion_mode", mode),
                new KeyValuePair<string, JsonValue>("peaks", new JsonArray(peaks)),
                new KeyValuePair<string, JsonValue>("precursor_mz_bits", JsonValue.Of(IeeeBits.Of(spectrum.PrecursorMz))),
            });
            return Sha256Hex.OfCanonical(doc);
        }

        private static bool Passes(Rule rule, RuleFilter filter) {
            switch (filter) {
                case RuleFilter.MeasuredAndSourceChecked: return rule.Measure && rule.SourceChecked;
                case RuleFilter.AuthorReviewed: return rule.AuthorReviewed;
                default: return true;
            }
        }

        /// <summary>
        /// Every option not at its default as (snake_case name, text), in ordinal name order (OQ08). Enum values are
        /// written as rule files write them, doubles as <see cref="ExactDecimal"/> text with 6 decimals, integers and
        /// booleans invariant. A product tolerance is flattened into <c>.abs_da</c> and <c>.ppm</c>.
        /// </summary>
        private static IReadOnlyList<KeyValuePair<string, string>> OptionList(EvaluationSettings s) {
            var list = new List<KeyValuePair<string, string>>();
            void Add(string key, string text) => list.Add(new KeyValuePair<string, string>(key, text));
            if (s.Filter != RuleFilter.MeasuredAndSourceChecked) {
                Add("filter", s.Filter == RuleFilter.AuthorReviewed ? "author_reviewed" : "all");
            }
            if (s.PrecursorToleranceDa.HasValue) {
                Add("precursor_tolerance_da", D(s.PrecursorToleranceDa.Value));
            }
            if (s.ProductToleranceOverride.HasValue) {
                Add("product_tolerance_override.abs_da", D(s.ProductToleranceOverride.Value.AbsDa));
                Add("product_tolerance_override.ppm", D(s.ProductToleranceOverride.Value.Ppm));
            }
            var msfinder = s.MsfinderProductTolerance;
            var msfinderDefault = new DiagnosticEvaluationOptions().MsfinderProductTolerance;
            if (msfinder.AbsDa != msfinderDefault.AbsDa || msfinder.Ppm != msfinderDefault.Ppm) {
                Add("msfinder_product_tolerance.abs_da", D(msfinder.AbsDa));
                Add("msfinder_product_tolerance.ppm", D(msfinder.Ppm));
            }
            if (s.LossReferenceOverride.HasValue) {
                Add("loss_reference_override", s.LossReferenceOverride.Value == LossReference.Hypothesis ? "hypothesis" : "observed");
            }
            if (s.DecoyShiftDa != 0) {
                Add("decoy_shift_da", s.DecoyShiftDa.ToString(CultureInfo.InvariantCulture));
            }
            if (s.FloorScale != 1.0) {
                Add("floor_scale", D(s.FloorScale));
            }
            if (s.KOffset != 0) {
                Add("k_offset", s.KOffset.ToString(CultureInfo.InvariantCulture));
            }
            if (s.Overlap != OverlapMode.Refuse) {
                Add("overlap", "runtime");
            }
            if (s.AssignmentCap != DiagnosticEvaluationOptions.DefaultAssignmentCap) {
                Add("assignment_cap", s.AssignmentCap.ToString(CultureInfo.InvariantCulture));
            }
            if (s.Strict) {
                Add("strict", "true");
            }
            if (s.Verbose) {
                Add("verbose", "true");
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            return new ReadOnlyCollection<KeyValuePair<string, string>>(list);
        }

        private static string Identity(RuleSetIdentity set, IReadOnlyList<KeyValuePair<string, string>> options) {
            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("engine", EngineSemantics),
                new KeyValuePair<string, string>("ruleset", set.Version),
                new KeyValuePair<string, string>("ruleset_sha256", set.Sha256),
            };
            foreach (var o in options) {
                if (!string.Equals(o.Key, "strict", StringComparison.Ordinal) && !string.Equals(o.Key, "verbose", StringComparison.Ordinal)) {
                    pairs.Add(o);
                }
            }
            pairs.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            var parts = new List<string>(pairs.Count);
            foreach (var p in pairs) {
                parts.Add(p.Key + "=" + p.Value);
            }
            return IdentityPrefix + string.Join("&", parts);
        }

        private static string D(double x) => ExactDecimal.Format(x, 6);

        private static bool Contains(IReadOnlyList<string> list, string value) {
            foreach (var x in list) {
                if (string.Equals(x, value, StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }

        internal static string TierName(AdductTier tier) {
            switch (tier) {
                case AdductTier.CrossPolarity: return "cross_polarity";
                case AdductTier.WithinPolarityPair: return "within_polarity_pair";
                case AdductTier.Curated: return "curated";
                default: return "assumed";
            }
        }

        private static string ModeName(PreconditionMode mode) => mode == PreconditionMode.MzOnly ? "mz_only" : "adduct_ambiguous";

        private static string ClaimLevelName(ClaimLevel level) {
            switch (level) {
                case ClaimLevel.Class: return "class";
                case ClaimLevel.Scaffold: return "scaffold";
                default: return "substituent";
            }
        }

        private static string OriginName(ParametersOrigin origin) {
            switch (origin) {
                case ParametersOrigin.AsPublished: return "as_published";
                case ParametersOrigin.TunedVs20: return "tuned_vs20";
                default: return "tuned_pooled";
            }
        }

        private static string? PreconditionFailureName(PreconditionFailure failure) {
            switch (failure) {
                case PreconditionFailure.IonMode: return RuleOutcome.PreconditionIonMode;
                case PreconditionFailure.AdductNotInRule: return RuleOutcome.PreconditionAdductNotInRule;
                default: return null;
            }
        }

        private static string? FailureName(HypothesisFailure failure) {
            switch (failure) {
                case HypothesisFailure.LogicFalse: return HypothesisOutcome.FailureLogicFalse;
                case HypothesisFailure.Veto: return HypothesisOutcome.FailureVeto;
                case HypothesisFailure.NoBasePeak: return HypothesisOutcome.FailureNoBasePeak;
                case HypothesisFailure.AssignmentOverflow: return HypothesisOutcome.FailureAssignmentOverflow;
                default: return null;
            }
        }
    }
}
