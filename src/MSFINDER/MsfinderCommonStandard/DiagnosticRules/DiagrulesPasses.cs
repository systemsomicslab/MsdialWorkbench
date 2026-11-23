using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;
using System.Collections.Generic;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// One variant pass of the <c>diagrules</c> benchmark (design section 8 PR 5): the base evaluator's options (MS-FINDER's
    /// product tolerance, Strict) with one variant option changed through the evaluator's own options, never by
    /// recomputing fire tables (design 9.1 item 12).
    /// </summary>
    public sealed class DiagrulesVariant
    {
        private readonly Action<DiagnosticEvaluationOptions> _configure;

        internal DiagrulesVariant(string pass, Action<DiagnosticEvaluationOptions> configure) {
            Pass = pass;
            _configure = configure;
        }

        /// <summary>The pass name, used in the evidence file name and the manifest.</summary>
        public string Pass { get; }

        /// <summary>The options of this variant over <paramref name="msfinderTolerance"/> and <paramref name="strict"/>.</summary>
        public DiagnosticEvaluationOptions Options(ProductTolerance msfinderTolerance, bool strict) {
            var options = new DiagnosticEvaluationOptions { MsfinderProductTolerance = msfinderTolerance, Strict = strict };
            _configure(options);
            return options;
        }
    }

    /// <summary>
    /// The variant passes of design section 8 PR 5, in this fixed order: product tolerances 0.003 and 0.01 Da, floor x0.5
    /// and x2, k-1, the loss reference (both overrides, so every rule's other reference is measured: P20), decoys +5, +9
    /// and -7 Da, and precursor tolerance 0.005 Da. Passes that move targets or tolerances use
    /// <see cref="OverlapMode.Runtime"/> (design 4.8), so no rule is refused for an overlap the variant itself causes;
    /// floor and k-1 keep the default <see cref="OverlapMode.Refuse"/>.
    /// </summary>
    public static class DiagrulesVariants
    {
        /// <summary>The name of the <c>mz_only</c> pass with <c>AdductEvidence(curated)</c>.</summary>
        public const string MzOnlyCuratedPass = "mz_only_curated";

        public static IReadOnlyList<DiagrulesVariant> All { get; } = new[] {
            new DiagrulesVariant("tol_product_0.003", o => { o.ProductToleranceOverride = ProductTolerance.Da(0.003); o.Overlap = OverlapMode.Runtime; }),
            new DiagrulesVariant("tol_product_0.01", o => { o.ProductToleranceOverride = ProductTolerance.Da(0.01); o.Overlap = OverlapMode.Runtime; }),
            new DiagrulesVariant("floor_x0.5", o => o.FloorScale = 0.5),
            new DiagrulesVariant("floor_x2", o => o.FloorScale = 2.0),
            new DiagrulesVariant("k_minus_1", o => o.KOffset = -1),
            new DiagrulesVariant("loss_ref_hypothesis", o => { o.LossReferenceOverride = LossReference.Hypothesis; o.Overlap = OverlapMode.Runtime; }),
            new DiagrulesVariant("loss_ref_observed", o => { o.LossReferenceOverride = LossReference.Observed; o.Overlap = OverlapMode.Runtime; }),
            new DiagrulesVariant("decoy_plus_5", o => { o.DecoyShiftDa = 5; o.Overlap = OverlapMode.Runtime; }),
            new DiagrulesVariant("decoy_plus_9", o => { o.DecoyShiftDa = 9; o.Overlap = OverlapMode.Runtime; }),
            new DiagrulesVariant("decoy_minus_7", o => { o.DecoyShiftDa = -7; o.Overlap = OverlapMode.Runtime; }),
            new DiagrulesVariant("tol_precursor_0.005", o => { o.PrecursorToleranceDa = 0.005; o.Overlap = OverlapMode.Runtime; }),
        };

        /// <summary>Every pass of a full run, in run order: base, the variants, then <c>mz_only</c>.</summary>
        public static IReadOnlyList<string> AllPasses() {
            var passes = new List<string> { DiagrulesHarness.BasePass };
            foreach (var v in All) {
                passes.Add(v.Pass);
            }
            passes.Add(MzOnlyCuratedPass);
            return passes;
        }
    }

    /// <summary>
    /// The alias table of tables/adducts.json for the benchmark reader (design 3.4): a PRECURSORTYPE matches an adduct's
    /// canonical name or one of its aliases with ASCII spaces removed on both sides (as P2 does), ordinally. The loader
    /// already refuses a spelling that two rows share after the same stripping.
    /// </summary>
    public sealed class DiagrulesAdductAliases
    {
        private readonly Dictionary<string, string> _canonical = new Dictionary<string, string>(StringComparer.Ordinal);

        public DiagrulesAdductAliases(DiagnosticRuleSet set) {
            if (set == null) {
                throw new ArgumentNullException(nameof(set));
            }
            foreach (var row in set.Adducts) {
                Add(row.Adduct, row.Adduct);
                foreach (var alias in row.Aliases) {
                    Add(alias, row.Adduct);
                }
            }
        }

        private void Add(string spelling, string adduct) {
            var key = Strip(spelling);
            if (!_canonical.ContainsKey(key)) {
                _canonical[key] = adduct;
            }
        }

        private static string Strip(string text) => text.Replace(" ", string.Empty);

        /// <summary>The canonical adduct of <paramref name="precursorType"/>; false (null) when it is absent or has no
        /// alias row.</summary>
        public bool TryMap(string precursorType, out string canonicalAdduct) {
            canonicalAdduct = null;
            return precursorType != null && _canonical.TryGetValue(Strip(precursorType), out canonicalAdduct);
        }
    }
}
