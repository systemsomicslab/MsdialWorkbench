using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;
using System.Globalization;
using System.Numerics;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>
    /// An immutable, checked copy of <see cref="DiagnosticEvaluationOptions"/>, taken once when an evaluator is built
    /// (design 4.10): the options object stays mutable, the evaluator does not. Every property is read once into the
    /// copy, and the copy, not the caller's object, is checked.
    /// </summary>
    internal sealed class EvaluationSettings
    {
        /// <summary>The largest |DecoyShiftDa|: 9007, the largest integer delta with delta * 10^12 &lt; 2^53 pDa.</summary>
        public const long MaxDecoyShiftDa = MassPda.Limit / ElementTable.PdaPerDa;

        private EvaluationSettings(DiagnosticEvaluationOptions o) {
            Filter = o.Filter;
            PrecursorToleranceDa = o.PrecursorToleranceDa;
            ProductToleranceOverride = o.ProductToleranceOverride;
            MsfinderProductTolerance = o.MsfinderProductTolerance;
            LossReferenceOverride = o.LossReferenceOverride;
            DecoyShiftDa = o.DecoyShiftDa;
            FloorScale = o.FloorScale;
            KOffset = o.KOffset;
            Overlap = o.Overlap;
            AssignmentCap = o.AssignmentCap;
            Strict = o.Strict;
            Verbose = o.Verbose;
            Check();
            // Only after the range check: |delta| <= 9007, so the product cannot overflow.
            DecoyShiftPda = checked(DecoyShiftDa * ElementTable.PdaPerDa);
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
        /// tolerance, a FloorScale that is not finite and positive, a variant double option (PrecursorToleranceDa,
        /// ProductToleranceOverride, FloorScale) that its 6-decimal text does not identify (<see cref="SixDecimals"/>),
        /// an AssignmentCap below 1, |DecoyShiftDa| above
        /// <see cref="MaxDecoyShiftDa"/> (so the shift itself, delta * 10^12, stays below 2^53 pDa; this bounds the
        /// shift only and does not keep every shifted term mass inside (0, 2^53) pDa), or an undefined enum value.
        /// </summary>
        public static EvaluationSettings From(DiagnosticEvaluationOptions options) {
            if (options is null) {
                throw new ArgumentNullException(nameof(options));
            }
            return new EvaluationSettings(options);
        }

        /// <summary>Checks the copied values (never the caller's object, which another thread may change).</summary>
        private void Check() {
            if (Filter != RuleFilter.MeasuredAndSourceChecked && Filter != RuleFilter.AuthorReviewed && Filter != RuleFilter.All) {
                throw new ArgumentException("Filter is not a defined RuleFilter", "options");
            }
            if (PrecursorToleranceDa.HasValue && !IsTolerance(PrecursorToleranceDa.Value)) {
                throw new ArgumentException("PrecursorToleranceDa must be finite and not negative", "options");
            }
            if (ProductToleranceOverride.HasValue && !IsTolerance(ProductToleranceOverride.Value)) {
                throw new ArgumentException("ProductToleranceOverride must be finite and not negative", "options");
            }
            if (!IsTolerance(MsfinderProductTolerance)) {
                throw new ArgumentException("MsfinderProductTolerance must be finite and not negative", "options");
            }
            if (LossReferenceOverride.HasValue
                && LossReferenceOverride.Value != Model.LossReference.Hypothesis
                && LossReferenceOverride.Value != Model.LossReference.Observed) {
                throw new ArgumentException("LossReferenceOverride is not a defined LossReference", "options");
            }
            // Compared before any multiplication: |delta| * 10^12 wraps silently in long arithmetic for |delta| of
            // about 9.2e6 and more, and a wrapped product can fall back below 2^53.
            if (Math.Abs((long)DecoyShiftDa) > MaxDecoyShiftDa) {
                throw new ArgumentException("|DecoyShiftDa| must be at most 9007, so that DecoyShiftDa * 10^12 stays below 2^53 pDa", "options");
            }
            if (double.IsNaN(FloorScale) || double.IsInfinity(FloorScale) || FloorScale <= 0.0) {
                throw new ArgumentException("FloorScale must be finite and positive", "options");
            }
            // IdentityString and the result options write these with 6 decimals, so two evaluators that differ
            // only beyond the sixth decimal would share one identity while behaving differently.
            if (PrecursorToleranceDa.HasValue && !SixDecimals(PrecursorToleranceDa.Value)) {
                throw new ArgumentException("PrecursorToleranceDa must have at most 6 decimals", "options");
            }
            if (ProductToleranceOverride.HasValue
                && !(SixDecimals(ProductToleranceOverride.Value.AbsDa) && SixDecimals(ProductToleranceOverride.Value.Ppm))) {
                throw new ArgumentException("ProductToleranceOverride must have at most 6 decimals", "options");
            }
            if (!SixDecimals(FloorScale)) {
                throw new ArgumentException("FloorScale must have at most 6 decimals", "options");
            }
            if (Overlap != OverlapMode.Refuse && Overlap != OverlapMode.Runtime) {
                throw new ArgumentException("Overlap is not a defined OverlapMode", "options");
            }
            if (AssignmentCap < 1) {
                throw new ArgumentException("AssignmentCap must be at least 1", "options");
            }
        }

        /// <summary>
        /// Whether the finite <paramref name="x"/> is identified by <c>ExactDecimal.Format(x, 6)</c>: the text's digit
        /// integer d, read back as <c>(double)d / 1000000.0</c> (correctly rounded, as rule files read decimals), gives
        /// x again. Above |d| = 2^53 every double is accepted: there ulp(x) exceeds 10^-6, so distinct doubles already
        /// have distinct texts. MsfinderProductTolerance is not checked: it is the MS-FINDER user's own setting, not a
        /// variant, and a value read from a float must not make the evaluator unbuildable.
        /// </summary>
        internal static bool SixDecimals(double x) {
            var digits = BigInteger.Parse(ExactDecimal.Format(x, 6).Replace(".", ""), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            if (BigInteger.Abs(digits) > MassPda.Limit) {
                return true;
            }
            return (double)(long)digits / 1000000.0 == x;
        }

        private static bool IsTolerance(double x) => !double.IsNaN(x) && !double.IsInfinity(x) && x >= 0.0;

        private static bool IsTolerance(ProductTolerance t) => IsTolerance(t.AbsDa) && IsTolerance(t.Ppm);
    }
}
