using CompMs.Common.StructureFinder.DiagnosticRules;
using Rfx.Riken.OsakaUniv;
using System;
using System.Linq;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// The GUI's diagnostic-rule runtime (design 5.2): created lazily and cached, keyed by the six settings, their
    /// errors and the product tolerance. A failed creation is cached too, so the GUI reports it once per key.
    /// </summary>
    public sealed class DiagnosticRuleRuntimeCache
    {
        private MsfinderDiagnosticRuleSettings _settings;
        private ProductTolerance _tolerance;

        /// <summary>The cached runtime, or null (evaluation off, nothing created yet, or creation failed).</summary>
        public DiagnosticRuleRuntime Runtime { get; private set; }

        /// <summary>The <c>rule_set_error:...</c> of the cached failed creation, or null.</summary>
        public string Error { get; private set; }

        /// <summary>
        /// The runtime for <paramref name="param"/>: null when evaluation is off (the cache is then cleared) or when
        /// creation failed (<see cref="Error"/> holds the error). <paramref name="created"/> is true when this call
        /// created it (or failed to), false when the cached one was returned.
        /// </summary>
        public DiagnosticRuleRuntime Get(AnalysisParamOfMsfinder param, out bool created) {
            created = false;
            var settings = param?.DiagnosticRules;
            if (settings == null || !settings.Evaluation) {
                Clear();
                return null;
            }
            var tolerance = DiagnosticRuleRuntime.ToleranceOf(param);
            if (_settings != null && Same(_settings, settings) && Same(_tolerance, tolerance)) {
                return Runtime;
            }
            var snapshot = settings.Clone();
            Runtime = DiagnosticRuleRuntime.Create(snapshot, tolerance, out var error);
            Error = Runtime == null ? error : null;
            _settings = snapshot;
            _tolerance = tolerance;
            created = true;
            return Runtime;
        }

        public void Clear() {
            _settings = null;
            _tolerance = default(ProductTolerance);
            Runtime = null;
            Error = null;
        }

        internal static bool Same(MsfinderDiagnosticRuleSettings a, MsfinderDiagnosticRuleSettings b) =>
            a.Evaluation == b.Evaluation
            && a.RankingWeight.Equals(b.RankingWeight)
            && a.RankingScope == b.RankingScope
            && string.Equals(a.RuleSetDirectory, b.RuleSetDirectory, StringComparison.Ordinal)
            && string.Equals(a.InstrumentType, b.InstrumentType, StringComparison.Ordinal)
            && a.Strict == b.Strict
            && a.Errors.SequenceEqual(b.Errors, StringComparer.Ordinal);

        internal static bool Same(ProductTolerance a, ProductTolerance b) => a.AbsDa.Equals(b.AbsDa) && a.Ppm.Equals(b.Ppm);
    }
}
