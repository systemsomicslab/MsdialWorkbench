using NCDK;
using NCDK.SMARTS;
using NCDK.Smiles;
using System;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Motifs
{
    /// <summary>
    /// Matches candidate structures (SMILES) against the motifs of a rule set (design 5.6), for the ranking term and
    /// the context file of MS-FINDER. It is the only engine code that uses NCDK.
    /// </summary>
    /// <remarks>
    /// Each motif SMARTS is compiled once, at construction, with <c>SmartsPattern.Create</c> and
    /// <c>SetPrepare(false)</c>. A SMILES is parsed once with <see cref="SmilesParser"/> and prepared once
    /// (<see cref="SmartsPattern.Prepare"/>: ring flags and Daylight aromaticity) for every motif it is matched
    /// against. Results are cached by (SMILES, motif). A SMILES that does not parse matches no motif.
    /// <para>Not thread-safe: the cache and the NCDK objects are unguarded. MS-FINDER uses one matcher sequentially
    /// (the export loops and the UI thread).</para>
    /// </remarks>
    public sealed class MotifMatcher
    {
        private readonly Dictionary<string, SmartsPattern> _patterns = new Dictionary<string, SmartsPattern>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, bool>> _cache = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);
        private readonly SmilesParser _parser = new SmilesParser();
        private string? _lastSmiles;
        private IAtomContainer? _lastMolecule;

        /// <summary>Compiles every motif of <paramref name="set"/>.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        /// <exception cref="System.IO.IOException">A motif SMARTS does not parse in NCDK.</exception>
        public MotifMatcher(DiagnosticRuleSet set) {
            if (set is null) {
                throw new ArgumentNullException(nameof(set));
            }
            foreach (var motif in set.Motifs) {
                _patterns[motif.MotifId] = SmartsPattern.Create(motif.Smarts).SetPrepare(false);
            }
        }

        /// <summary>The distinct SMILES parsed so far (parse failures included).</summary>
        public int UniqueSmiles => _cache.Count;

        /// <summary>The (SMILES, motif) lookups answered from the cache so far.</summary>
        public long CacheHits { get; private set; }

        /// <summary>Whether a motif id is one of the set's motifs.</summary>
        public bool HasMotif(string motifId) => motifId != null && _patterns.ContainsKey(motifId);

        /// <summary>
        /// Whether the structure <paramref name="smiles"/> contains the motif <paramref name="motifId"/>. A null,
        /// empty or unparsable SMILES matches nothing.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="motifId"/> is not a motif of the set.</exception>
        public bool Matches(string smiles, string motifId) {
            if (motifId is null || !_patterns.TryGetValue(motifId, out var pattern)) {
                throw new ArgumentException("unknown motif", nameof(motifId));
            }
            if (string.IsNullOrEmpty(smiles)) {
                return false;
            }
            if (!_cache.TryGetValue(smiles, out var byMotif)) {
                byMotif = new Dictionary<string, bool>(StringComparer.Ordinal);
                _cache.Add(smiles, byMotif);
            }
            else if (byMotif.TryGetValue(motifId, out var known)) {
                CacheHits++;
                return known;
            }
            var molecule = Molecule(smiles);
            bool matched;
            if (molecule is null) {
                matched = false;
            }
            else {
                try {
                    matched = pattern.Matches(molecule);
                }
                catch (Exception) {
                    matched = false;
                }
            }
            byMotif.Add(motifId, matched);
            return matched;
        }

        private IAtomContainer? Molecule(string smiles) {
            if (string.Equals(smiles, _lastSmiles, StringComparison.Ordinal)) {
                return _lastMolecule;
            }
            IAtomContainer? molecule;
            try {
                molecule = _parser.ParseSmiles(smiles);
                SmartsPattern.Prepare(molecule);
            }
            catch (Exception) {
                molecule = null;
            }
            _lastSmiles = smiles;
            _lastMolecule = molecule;
            return molecule;
        }
    }
}
