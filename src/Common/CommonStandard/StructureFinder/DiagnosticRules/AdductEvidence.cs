using System;

namespace CompMs.Common.StructureFinder.DiagnosticRules
{
    /// <summary>
    /// Evidence for the adduct of a spectrum, beyond the four inputs (design 4.3, 4.10). With it an evaluation runs in
    /// <c>mz_only</c> mode: only this adduct is tried, and only for rules that list it. It records the tier and
    /// carries no formula. <see cref="AdductTier.Assumed"/> is refused: no evidence is expressed by calling
    /// <c>Evaluate(spectrum)</c> without one.
    /// </summary>
    public sealed class AdductEvidence
    {
        /// <param name="canonicalAdduct">The canonical adduct name of tables/adducts.json, for example [M+H]+.</param>
        /// <param name="tier">CrossPolarity, WithinPolarityPair or Curated.</param>
        /// <exception cref="ArgumentNullException"><paramref name="canonicalAdduct"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="tier"/> is Assumed or not a defined tier.</exception>
        public AdductEvidence(string canonicalAdduct, AdductTier tier) {
            if (canonicalAdduct is null) {
                throw new ArgumentNullException(nameof(canonicalAdduct));
            }
            if (tier != AdductTier.CrossPolarity && tier != AdductTier.WithinPolarityPair && tier != AdductTier.Curated) {
                throw new ArgumentException("adduct evidence needs the tier cross_polarity, within_polarity_pair or curated", nameof(tier));
            }
            CanonicalAdduct = canonicalAdduct;
            Tier = tier;
        }

        public string CanonicalAdduct { get; }

        public AdductTier Tier { get; }
    }
}
