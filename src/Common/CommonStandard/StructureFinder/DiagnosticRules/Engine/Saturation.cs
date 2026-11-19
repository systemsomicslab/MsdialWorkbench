namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>
    /// Keeps the engine's computed doubles finite (design 4.11: only <c>precursor_mz</c> may be non-finite). Two
    /// quotients can overflow on finite inputs: a p2 rel <c>I / base</c> of a precursor-region peak, which the base
    /// excludes (so I_raw, <c>i_a</c> and <c>i_b_max</c> could be infinite), and <c>error_ppm</c>
    /// <c>error_da / target * 1000000.0</c> at a target of 0 or one far smaller than the error. Each is the design's
    /// expression, saturated to +-double.MaxValue: bit-identical wherever the expression is finite, and finite where it
    /// would overflow. A port applies the same clamp (Python: <c>max(-sys.float_info.max, min(sys.float_info.max, x))</c>).
    /// </summary>
    internal static class Saturation
    {
        /// <summary>x clamped to [-double.MaxValue, double.MaxValue]; a finite x is returned unchanged.</summary>
        public static double Clamp(double x) => x > double.MaxValue ? double.MaxValue : x < -double.MaxValue ? -double.MaxValue : x;
    }
}
