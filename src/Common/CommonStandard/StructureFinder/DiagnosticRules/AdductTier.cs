namespace CompMs.Common.StructureFinder.DiagnosticRules
{
    /// <summary>
    /// How the adduct of an evaluation is known (design 4.3), ordered CrossPolarity &gt; WithinPolarityPair &gt;
    /// Curated &gt; Assumed. One evaluation has one mode and so one tier. MS-FINDER never passes evidence, so every
    /// MS-FINDER evaluation is <see cref="Assumed"/>.
    /// </summary>
    public enum AdductTier
    {
        /// <summary>The adduct was confirmed by the other polarity.</summary>
        CrossPolarity,
        /// <summary>The adduct was confirmed by a pair of ions within one polarity.</summary>
        WithinPolarityPair,
        /// <summary>The adduct was curated (benchmark only).</summary>
        Curated,
        /// <summary>No evidence: every tried adduct is an assumption (adduct_ambiguous).</summary>
        Assumed,
    }
}
