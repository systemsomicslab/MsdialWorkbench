using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Motifs;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// What MS-FINDER holds while diagnostic-ion rule evaluation is on (design 5.2): the rule set, the evaluator and,
    /// when the ranking weight is above zero, the motif matcher. Create it once per run, only when
    /// <see cref="MsfinderDiagnosticRuleSettings.Evaluation"/> is on; a run never continues "on" without rules.
    /// </summary>
    public sealed class DiagnosticRuleRuntime
    {
        /// <summary>The prefix of every <see cref="Create"/> error.</summary>
        public const string ErrorPrefix = "rule_set_error";

        /// <summary>Row status of a GC-EI query (TMS/MeOX derivatives), outside the LC-MS/MS scope.</summary>
        public const string SkippedEiDerivative = "skipped:ei_derivative";

        /// <summary>Row status prefix of a query whose evaluation threw (not Strict).</summary>
        public const string ErrorStatusPrefix = "error:";

        private DiagnosticRuleRuntime(MsfinderDiagnosticRuleSettings settings, ProductTolerance tolerance,
                                      DiagnosticRuleSet set, DiagnosticRuleEvaluator evaluator, MotifMatcher matcher) {
            Settings = settings;
            MsfinderTolerance = tolerance;
            RuleSet = set;
            Evaluator = evaluator;
            Matcher = matcher;
        }

        /// <summary>A copy of the settings the runtime was created with.</summary>
        public MsfinderDiagnosticRuleSettings Settings { get; }

        /// <summary>The MS-FINDER product tolerance, used by rules that declare <c>msfinder</c>.</summary>
        public ProductTolerance MsfinderTolerance { get; }

        public DiagnosticRuleSet RuleSet { get; }

        public DiagnosticRuleEvaluator Evaluator { get; }

        /// <summary>The motif matcher, or null when the ranking weight is zero (no ranking, no structure parsing).</summary>
        public MotifMatcher Matcher { get; }

        public double RankingWeight => Settings.RankingWeight;

        public DiagnosticRuleRankingScope RankingScope => Settings.RankingScope;

        /// <summary>Whether candidates are ranked: the weight is above zero.</summary>
        public bool RankingOn => Matcher != null;

        /// <summary>
        /// MS-FINDER's product tolerance as the evaluator takes it: <c>Da</c> gives an absolute tolerance and
        /// <c>Ppm</c> a relative one (OQ10).
        /// </summary>
        public static ProductTolerance ToleranceOf(MassToleranceType type, double mass2Tolerance) =>
            type == MassToleranceType.Ppm ? ProductTolerance.FromPpm(mass2Tolerance) : ProductTolerance.Da(mass2Tolerance);

        /// <summary><see cref="ToleranceOf(MassToleranceType, double)"/> of the parameter's MassTolType and
        /// Mass2Tolerance.</summary>
        public static ProductTolerance ToleranceOf(AnalysisParamOfMsfinder param) => ToleranceOf(param.MassTolType, param.Mass2Tolerance);

        /// <summary>
        /// Loads the rule set (the embedded one, or <see cref="MsfinderDiagnosticRuleSettings.RuleSetDirectory"/>
        /// unpinned) and builds the evaluator. Returns null with <paramref name="error"/> set when the settings file had
        /// errors, the set fails to load, an option is invalid, a motif fails to compile, or (Strict) a rule is
        /// refused. The error never contains the directory path.
        /// </summary>
        public static DiagnosticRuleRuntime Create(MsfinderDiagnosticRuleSettings settings, ProductTolerance msfinderTolerance, out string error) {
            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }
            error = null;
            if (settings.Errors.Count > 0) {
                error = ErrorPrefix + ":settings:" + string.Join(";", settings.Errors);
                return null;
            }
            var copy = settings.Clone();
            if (!(copy.RankingWeight >= 0.0) || double.IsInfinity(copy.RankingWeight)) {
                error = ErrorPrefix + ":settings:" + DiagnosticRuleIni.RankingWeightKey;
                return null;
            }

            DiagnosticRuleSet set;
            try {
                set = string.IsNullOrEmpty(copy.RuleSetDirectory)
                    ? DiagnosticRuleSet.LoadEmbedded()
                    : DiagnosticRuleSet.LoadDirectory(copy.RuleSetDirectory);
            }
            catch (DiagnosticRuleSetException e) {
                error = ErrorPrefix + ":load:" + LoadCodes(e.Errors);
                return null;
            }
            catch (Exception e) {
                error = ErrorPrefix + ":load:" + e.GetType().Name;
                return null;
            }

            var options = new DiagnosticEvaluationOptions {
                MsfinderProductTolerance = msfinderTolerance,
                Strict = copy.Strict,
            };
            DiagnosticRuleEvaluator evaluator;
            try {
                evaluator = new DiagnosticRuleEvaluator(set, options);
            }
            catch (DiagnosticRuleRefusedException e) {
                var ids = new List<string>();
                foreach (var r in e.Refusals) {
                    ids.Add(r.RuleId + "=" + r.Code);
                }
                error = ErrorPrefix + ":refused:" + string.Join(";", ids);
                return null;
            }
            catch (ArgumentException e) {
                error = ErrorPrefix + ":options:" + e.GetType().Name;
                return null;
            }

            MotifMatcher matcher = null;
            if (copy.RankingWeight > 0.0) {
                try {
                    matcher = new MotifMatcher(set);
                }
                catch (Exception e) {
                    error = ErrorPrefix + ":motifs:" + e.GetType().Name;
                    return null;
                }
            }
            return new DiagnosticRuleRuntime(copy, msfinderTolerance, set, evaluator, matcher);
        }

        /// <summary>
        /// Evaluates one query: GC-EI derivative queries and queries whose peaks a guard refuses are skipped with
        /// their status, the others are evaluated. An exception during evaluation gives the status
        /// <c>error:&lt;exception type name&gt;</c>, or is rethrown under Strict (OQ08).
        /// </summary>
        public DiagnosticQueryEvaluation Evaluate(RawData raw, bool isTmsMeoxDerivative) {
            if (isTmsMeoxDerivative) {
                return new DiagnosticQueryEvaluation(SkippedEiDerivative, null, null);
            }
            DiagnosticInputInfo info = null;
            try {
                var spectrum = MsfinderDiagnosticAdapter.ToSpectrum(raw, Settings.InstrumentType, out info);
                if (spectrum == null) {
                    return new DiagnosticQueryEvaluation(info.SkipStatus, null, info);
                }
                var result = Evaluator.Evaluate(spectrum);
                return new DiagnosticQueryEvaluation(result.Status, result, info);
            }
            catch (Exception e) when (!Settings.Strict) {
                return new DiagnosticQueryEvaluation(ErrorStatusPrefix + e.GetType().Name, null, info);
            }
        }

        private static string LoadCodes(IReadOnlyList<RuleLoadError> errors) {
            var codes = new List<string>();
            foreach (var e in errors) {
                codes.Add(e.Code + "@" + e.File + "#" + e.Pointer);
                if (codes.Count == 20) {
                    break;
                }
            }
            return string.Join(";", codes);
        }
    }

    /// <summary>The evaluation of one query: its row status, the result when it was evaluated, and the adapter
    /// info when the adapter ran.</summary>
    public sealed class DiagnosticQueryEvaluation
    {
        public DiagnosticQueryEvaluation(string status, DiagnosticRuleResult result, DiagnosticInputInfo info) {
            Status = status;
            Result = result;
            Info = info;
        }

        /// <summary>The result status (<c>evaluated</c>, <c>no_precursor</c>, ...), or a <c>skipped:</c> or
        /// <c>error:</c> status of the glue.</summary>
        public string Status { get; }

        /// <summary>The evaluator result, or null when the query was skipped or failed.</summary>
        public DiagnosticRuleResult Result { get; }

        /// <summary>The adapter info, or null when the adapter did not run (GC-EI derivative).</summary>
        public DiagnosticInputInfo Info { get; }
    }
}
