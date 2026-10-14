using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    /// <summary>
    /// One entry of the seed library the staged lipid annotation starts from: a lipid at species level
    /// (e.g. "PC 34:1") and where it elutes. It carries no spectrum and no chain detail; the candidates
    /// and their spectra are generated from it.
    /// </summary>
    public sealed class LipidSeed
    {
        public LipidSeed(string name, ILipid lipid, double? retentionTime = null, AdductIon? adduct = null, double? precursorMz = null, string? formula = null) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Lipid = lipid ?? throw new ArgumentNullException(nameof(lipid));
            RetentionTime = retentionTime;
            Adduct = adduct;
            PrecursorMz = precursorMz;
            Formula = formula;
        }

        /// <summary>The name as written in the library.</summary>
        public string Name { get; }

        public ILipid Lipid { get; }

        /// <summary>Retention time in minutes; null matches any time.</summary>
        public double? RetentionTime { get; }

        /// <summary>The only adduct to consider; null means every adduct the spectrum model has for the class.</summary>
        public AdductIon? Adduct { get; }

        /// <summary>Precursor m/z for <see cref="Adduct"/>; null means it is calculated from the lipid's mass.</summary>
        public double? PrecursorMz { get; }

        /// <summary>Kept for classes whose mass the lipid parser can't give; not used yet.</summary>
        public string? Formula { get; }
    }

    /// <summary>
    /// A seed whose precursor matched a query, with the adduct it matched as.
    /// </summary>
    public sealed class LipidSeedMatch
    {
        public LipidSeedMatch(LipidSeed seed, AdductIon adduct, double precursorMz) {
            Seed = seed;
            Adduct = adduct;
            PrecursorMz = precursorMz;
        }

        public LipidSeed Seed { get; }
        public AdductIon Adduct { get; }
        public double PrecursorMz { get; }
    }

    /// <summary>
    /// Looks seeds up by precursor m/z and retention time. A seed without an adduct is expanded to every
    /// adduct its class has a rule set for in the given spectrum models.
    /// </summary>
    public sealed class LipidSeedLibrary
    {
        private readonly LipidSeedMatch[] _entries; // sorted by PrecursorMz

        public LipidSeedLibrary(IEnumerable<LipidSeed> seeds, IEnumerable<ILipidSpectrumModel> models) {
            if (seeds is null) {
                throw new ArgumentNullException(nameof(seeds));
            }
            var adductsByClass = AdductsByClass(models ?? throw new ArgumentNullException(nameof(models)));
            Seeds = seeds.ToArray();
            _entries = Seeds.SelectMany(seed => Expand(seed, adductsByClass)).OrderBy(e => e.PrecursorMz).ToArray();
        }

        public IReadOnlyList<LipidSeed> Seeds { get; }

        /// <summary>Every (seed, adduct) pair, sorted by precursor m/z.</summary>
        public IReadOnlyList<LipidSeedMatch> Entries => _entries;

        /// <param name="retentionTime">Query retention time in minutes; null skips the RT filter.</param>
        /// <param name="ionMode">Only adducts of this polarity match.</param>
        public IReadOnlyList<LipidSeedMatch> Search(double precursorMz, double mzTolerance, double? retentionTime, double rtTolerance, IonMode ionMode) {
            var results = new List<LipidSeedMatch>();
            for (var i = LowerBound(precursorMz - mzTolerance); i < _entries.Length && _entries[i].PrecursorMz <= precursorMz + mzTolerance; i++) {
                var entry = _entries[i];
                if (entry.Adduct.IonMode != ionMode) {
                    continue;
                }
                if (retentionTime.HasValue && entry.Seed.RetentionTime.HasValue && Math.Abs(entry.Seed.RetentionTime.Value - retentionTime.Value) > rtTolerance) {
                    continue;
                }
                results.Add(entry);
            }
            return results;
        }

        private int LowerBound(double mz) {
            int lo = 0, hi = _entries.Length;
            while (lo < hi) {
                var mid = (lo + hi) / 2;
                if (_entries[mid].PrecursorMz < mz) {
                    lo = mid + 1;
                }
                else {
                    hi = mid;
                }
            }
            return lo;
        }

        private static IEnumerable<LipidSeedMatch> Expand(LipidSeed seed, Dictionary<LbmClass, AdductIon[]> adductsByClass) {
            if (seed.Adduct != null) {
                yield return new LipidSeedMatch(seed, seed.Adduct, seed.PrecursorMz ?? seed.Adduct.ConvertToMz(seed.Lipid.Mass));
                yield break;
            }
            if (!adductsByClass.TryGetValue(seed.Lipid.LipidClass, out var adducts)) {
                yield break;
            }
            foreach (var adduct in adducts) {
                yield return new LipidSeedMatch(seed, adduct, adduct.ConvertToMz(seed.Lipid.Mass));
            }
        }

        private static Dictionary<LbmClass, AdductIon[]> AdductsByClass(IEnumerable<ILipidSpectrumModel> models) {
            return models
                .SelectMany(model => LipidSpectrumModelSelector.TryResolveLbmClass(model.LipidClassName, out var lbm)
                    ? model.RuleSets.Where(r => r.Resolution != LipidSpectrumResolution.None).Select(r => (Class: lbm, Adduct: r.AdductIonName))
                    : Enumerable.Empty<(LbmClass Class, string Adduct)>())
                .GroupBy(p => p.Class)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => p.Adduct).Distinct().Select(AdductIon.GetAdductIon).Where(a => a.FormatCheck).ToArray());
        }
    }
}
