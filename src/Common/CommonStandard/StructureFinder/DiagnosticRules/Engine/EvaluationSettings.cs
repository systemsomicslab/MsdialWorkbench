using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>
    /// An immutable, checked copy of <see cref="DiagnosticEvaluationOptions"/>, taken once when an evaluator is built
    /// (design 4.10): the options object stays mutable, the evaluator does not.
    /// </summary>
    internal sealed class EvaluationSettings
    {
        private EvaluationSettings(DiagnosticEvaluationOptions o) {
            Filter = o.Filter;
            PrecursorToleranceDa = o.PrecursorToleranceDa;
            ProductToleranceOverride = o.ProductToleranceOverride;
            MsfinderProductTolerance = o.MsfinderProductTolerance;
            LossReferenceOverride = o.LossReferenceOverride;
            DecoyShiftDa = o.DecoyShiftDa;
            DecoyShiftPda = o.DecoyShiftDa * ElementTable.PdaPerDa;
            FloorScale = o.FloorScale;
            KOffset = o.KOffset;
            Overlap = o.Overlap;
            AssignmentCap = o.AssignmentCap;
            Strict = o.Strict;
            Verbose = o.Verbose;
        }

        public RuleFilter Filter { get; }
        public double? PrecursorToleranceDa { get; }
        public ProductTolerance? ProductToleranceOverride { get; }
        public ProductTolerance MsfinderProductTolerance { get; }
        public LossReference? LossReferenceOverride { get; }
        public int DecoyShiftDa { get; }
        /// <summary>The decoy shift in pDa (delta * 10^12).</summary>
        public long DecoyShiftPda { get; }
        public double FloorScale { get; }
        public int KOffset { get; }
        public OverlapMode Overlap { get; }
        public int AssignmentCap { get; }
        public bool Strict { get; }
        public bool Verbose { get; }

        /// <summary>
        /// True when any variant option (design 4.9) is not at its default: PrecursorToleranceDa,
        /// ProductToleranceOverride or LossReferenceOverride set, DecoyShiftDa != 0, FloorScale != 1, KOffset != 0,
        /// Overlap != Refuse or AssignmentCap != 4096. MsfinderProductTolerance is not a variant: it reaches claim
        /// eligibility only through the effective tolerance.
        /// </summary>
        public bool VariantActive =>
            PrecursorToleranceDa.HasValue || ProductToleranceOverride.HasValue || LossReferenceOverride.HasValue
            || DecoyShiftDa != 0 || FloorScale != 1.0 || KOffset != 0 || Overlap != OverlapMode.Refuse
            || AssignmentCap != DiagnosticEvaluationOptions.DefaultAssignmentCap;

        /// <summary>The precursor tolerance in Da applied to <paramref name="rule"/>.</summary>
        public double PrecursorToleranceFor(Rule rule) => PrecursorToleranceDa ?? rule.Preconditions.PrecursorToleranceDa;

        /// <summary>The product tolerance applied to <paramref name="rule"/>.</summary>
        public AppliedProductTolerance ProductToleranceFor(Rule rule) =>
            AppliedProductTolerance.For(rule.ProductTolerance, MsfinderProductTolerance, ProductToleranceOverride);

        /// <summary>The loss reference applied to <paramref name="rule"/>.</summary>
        public LossReference LossReferenceFor(Rule rule) => LossReferenceOverride ?? rule.LossReference;

        /// <summary>
        /// Copies and checks the options. Throws <see cref="ArgumentException"/> for a non-finite or negative
        /// tolerance, a FloorScale that is not finite and positive, an AssignmentCap below 1, a decoy shift that
        /// cannot keep masses inside (0, 2^53) pDa (|delta| * 10^12 &gt;= 2^53), or an undefined enum value.
        /// </summary>
        public static EvaluationSettings From(DiagnosticEvaluationOptions options) {
            if (options is null) {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.Filter != RuleFilter.MeasuredAndSourceChecked && options.Filter != RuleFilter.AuthorReviewed && options.Filter != RuleFilter.All) {
                throw new ArgumentException("Filter is not a defined RuleFilter", nameof(options));
            }
            if (options.PrecursorToleranceDa.HasValue && !IsTolerance(options.PrecursorToleranceDa.Value)) {
                throw new ArgumentException("PrecursorToleranceDa must be finite and not negative", nameof(options));
            }
            if (options.ProductToleranceOverride.HasValue && !IsTolerance(options.ProductToleranceOverride.Value)) {
                throw new ArgumentException("ProductToleranceOverride must be finite and not negative", nameof(options));
            }
            if (!IsTolerance(options.MsfinderProductTolerance)) {
                throw new ArgumentException("MsfinderProductTolerance must be finite and not negative", nameof(options));
            }
            if (options.LossReferenceOverride.HasValue
                && options.LossReferenceOverride.Value != Model.LossReference.Hypothesis
                && options.LossReferenceOverride.Value != Model.LossReference.Observed) {
                throw new ArgumentException("LossReferenceOverride is not a defined LossReference", nameof(options));
            }
            if (Math.Abs((long)options.DecoyShiftDa) * ElementTable.PdaPerDa >= MassPda.Limit) {
                throw new ArgumentException("DecoyShiftDa moves masses outside (0, 2^53) pDa", nameof(options));
            }
            if (double.IsNaN(options.FloorScale) || double.IsInfinity(options.FloorScale) || options.FloorScale <= 0.0) {
                throw new ArgumentException("FloorScale must be finite and positive", nameof(options));
            }
            if (options.Overlap != OverlapMode.Refuse && options.Overlap != OverlapMode.Runtime) {
                throw new ArgumentException("Overlap is not a defined OverlapMode", nameof(options));
            }
            if (options.AssignmentCap < 1) {
                throw new ArgumentException("AssignmentCap must be at least 1", nameof(options));
            }
            return new EvaluationSettings(options);
        }

        private static bool IsTolerance(double x) => !double.IsNaN(x) && !double.IsInfinity(x) && x >= 0.0;

        private static bool IsTolerance(ProductTolerance t) => IsTolerance(t.AbsDa) && IsTolerance(t.Ppm);
    }
}
