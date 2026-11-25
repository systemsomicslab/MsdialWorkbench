namespace CompMs.Common.StructureFinder.DiagnosticRules
{
    /// <summary>
    /// One MS/MS peak as the evaluator sees it: an m/z and an intensity, nothing else (design 4.10, 6.1 item 1). The
    /// engine drops peaks with a non-finite value, m/z &lt;= 0 or intensity &lt;= 0 and counts them; it never
    /// centroids, merges or filters noise.
    /// </summary>
    public readonly struct DiagnosticPeak
    {
        public DiagnosticPeak(double mz, double intensity) {
            Mz = mz;
            Intensity = intensity;
        }

        public double Mz { get; }

        public double Intensity { get; }
    }
}
