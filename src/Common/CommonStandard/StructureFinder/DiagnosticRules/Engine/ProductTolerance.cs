using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>Where the product tolerance of a rule outcome came from (design 4.4).</summary>
    internal enum ToleranceSource
    {
        /// <summary>The rule declares <c>p2</c>: max(0.005 Da, 10 ppm).</summary>
        P2,
        /// <summary>The rule declares <c>msfinder</c>: the evaluator's MsfinderProductTolerance.</summary>
        Msfinder,
        /// <summary>The ProductToleranceOverride option.</summary>
        Override,
    }

    /// <summary>
    /// The product tolerance applied to one rule: {abs_da, ppm, source}. At target m/z t it is
    /// <c>Math.Max(AbsDa, t * Ppm / 1000000.0)</c>, one expression for every case. That is bit-identical to the
    /// design's per-case formulas: p2 is {0.005, 10} (<c>Math.Max(0.005, t * 10.0 / 1000000.0)</c>, that operation
    /// order); an msfinder tolerance of X Da is {X, 0}, giving X; one of X ppm is {0, X}, giving
    /// <c>t * X / 1000000.0</c> for every t &gt;= 0.
    /// </summary>
    internal readonly struct AppliedProductTolerance
    {
        public const double P2AbsDa = 0.005;
        public const double P2Ppm = 10.0;

        public AppliedProductTolerance(double absDa, double ppm, ToleranceSource source) {
            AbsDa = absDa;
            Ppm = ppm;
            Source = source;
        }

        public double AbsDa { get; }
        public double Ppm { get; }
        public ToleranceSource Source { get; }

        /// <summary>The tolerance in Da at target m/z <paramref name="target"/>.</summary>
        public double At(double target) => Math.Max(AbsDa, target * Ppm / 1000000.0);

        /// <summary>The source as results write it: p2, msfinder or override.</summary>
        public string SourceName => Source == ToleranceSource.P2 ? "p2" : Source == ToleranceSource.Msfinder ? "msfinder" : "override";

        /// <summary>The tolerance a rule gets: the override when set, else what the rule declares.</summary>
        public static AppliedProductTolerance For(ProductToleranceKind declared, ProductTolerance msfinder, ProductTolerance? overrideValue) {
            if (overrideValue.HasValue) {
                return new AppliedProductTolerance(overrideValue.Value.AbsDa, overrideValue.Value.Ppm, ToleranceSource.Override);
            }
            return declared == ProductToleranceKind.P2
                ? new AppliedProductTolerance(P2AbsDa, P2Ppm, ToleranceSource.P2)
                : new AppliedProductTolerance(msfinder.AbsDa, msfinder.Ppm, ToleranceSource.Msfinder);
        }
    }
}
