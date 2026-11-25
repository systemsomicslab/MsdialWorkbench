namespace CompMs.Common.StructureFinder.DiagnosticRules
{
    /// <summary>The ion mode of a spectrum. A spectrum without one (MS-FINDER's <c>Both</c>) is not evaluated
    /// (status <c>unsupported_ion_mode</c>, design 4.4).</summary>
    public enum DiagnosticIonMode
    {
        Positive,
        Negative,
    }
}
