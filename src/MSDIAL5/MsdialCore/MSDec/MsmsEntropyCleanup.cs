using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CompMs.MsdialCore.MSDec
{
    /// <summary>What the MS/MS entropy cleanup did to one spectrum.</summary>
    public enum MsmsEntropyCleanupStatus
    {
        /// <summary>The spectrum was at or below the ceiling and is returned as it was.</summary>
        Unchanged,
        /// <summary>The weakest ions were dropped and the spectrum is now at or below the ceiling.</summary>
        Cleaned,
        /// <summary>
        /// No set of the strongest ions reaches the ceiling (the strongest intensity level alone is above it), or
        /// ions protected by the base-peak floor keep it above: the kept ions stay above the ceiling.
        /// </summary>
        AboveCeiling,
        /// <summary>The cleanup would have kept fewer than <see cref="MsmsEntropyCleanup.MinimumPeaks"/> positive ions, so the spectrum is kept raw.</summary>
        KeptRawTooFewPeaks,
    }

    /// <summary>The outcome of the MS/MS entropy cleanup on one spectrum.</summary>
    public sealed class MsmsEntropyCleanupResult
    {
        internal MsmsEntropyCleanupResult(
            List<SpectrumPeak> peaks,
            double entropyBefore,
            double entropyAfter,
            int removed,
            double? intensityThreshold,
            double? basePeak,
            double? largestRemoved,
            MsmsEntropyCleanupStatus status) {
            Peaks = peaks;
            EntropyBefore = entropyBefore;
            EntropyAfter = entropyAfter;
            Removed = removed;
            IntensityThreshold = intensityThreshold;
            BasePeak = basePeak;
            LargestRemoved = largestRemoved;
            Status = status;
        }

        /// <summary>The peaks kept, in their input order; the input itself when nothing was dropped.</summary>
        public List<SpectrumPeak> Peaks { get; }
        public double EntropyBefore { get; }
        public double EntropyAfter { get; }
        /// <summary>The number of positive ions dropped.</summary>
        public int Removed { get; }
        /// <summary>The weakest positive intensity kept, when ions were dropped.</summary>
        public double? IntensityThreshold { get; }
        /// <summary>The strongest intensity, when the spectrum has a positive ion.</summary>
        public double? BasePeak { get; }
        /// <summary>The strongest intensity dropped, when ions were dropped.</summary>
        public double? LargestRemoved { get; }
        public MsmsEntropyCleanupStatus Status { get; }
    }

    /// <summary>The counts of one cleanup pass over a set of spectra, for the log.</summary>
    public sealed class MsmsEntropyCleanupSummary
    {
        public int Spectra { get; internal set; }
        public int Unchanged { get; internal set; }
        public int Cleaned { get; internal set; }
        public int PeaksRemoved { get; internal set; }
        public int AboveCeiling { get; internal set; }
        public int KeptRawTooFewPeaks { get; internal set; }

        public override string ToString() {
            return $"{Spectra} spectra: {Cleaned} cleaned ({PeaksRemoved} peaks removed), {Unchanged} unchanged, "
                + $"{AboveCeiling} above the ceiling, {KeptRawTooFewPeaks} kept raw (fewer than {MsmsEntropyCleanup.MinimumPeaks} ions would remain)";
        }
    }

    /// <summary>
    /// MS/MS peak cleanup by spectral entropy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The spectral entropy (Li et al., Nat. Methods 18, 1524-1531 (2021)) is the Shannon entropy of the positive
    /// intensities normalised to sum 1, with the natural logarithm: S = -sum(p ln p). It is computed on the spectrum
    /// as MS-DIAL stores it, without Li et al.'s preprocessing (merging peaks within 0.05 Da, removing ions under 1%
    /// of the base peak and the precursor region), so their ranges do not calibrate the ceiling.
    /// </para>
    /// <para>
    /// Rule. Dropping the weakest ion never raises S, so the cleanup keeps the strongest ions, one intensity level at
    /// a time, while S stays at or below the ceiling. Ions of equal intensity are kept or dropped together, so the
    /// result does not depend on peak order: it is the largest set of strongest ions, closed under equal intensities,
    /// whose entropy is at most the ceiling. When the strongest level alone is above the ceiling, that level is kept
    /// (<see cref="MsmsEntropyCleanupStatus.AboveCeiling"/>). Entries whose intensity is not positive carry no entropy
    /// and are left as they are. A spectrum at or below the ceiling is returned unchanged.
    /// </para>
    /// <para>
    /// The cut is relative to each spectrum: in a flat spectrum it can drop ions at a sizeable fraction of the base
    /// peak. An optional floor protects every ion at or above a percentage of the base peak; it is 0 (off) by default.
    /// </para>
    /// <para>
    /// The cleanup never empties a spectrum: one that would keep fewer than <see cref="MinimumPeaks"/> positive ions is
    /// kept raw (<see cref="MsmsEntropyCleanupStatus.KeptRawTooFewPeaks"/>).
    /// </para>
    /// <para>
    /// This is the same rule as spectrum_cleanup.py of msdial_spectrum_catalog, which the MS-Emblator exports use.
    /// </para>
    /// </remarks>
    public static class MsmsEntropyCleanup
    {
        public const double DefaultMaxEntropy = 4.5;
        public const int MinimumPeaks = 3;

        /// <summary>Whether <paramref name="maxEntropy"/> is a usable ceiling: finite and positive.</summary>
        public static bool IsValidMaxEntropy(double maxEntropy) {
            return !double.IsNaN(maxEntropy) && !double.IsInfinity(maxEntropy) && maxEntropy > 0d;
        }

        /// <summary>Whether <paramref name="percent"/> is a usable protected floor: from 0 (off) to 100.</summary>
        public static bool IsValidProtectedPercentOfBasePeak(double percent) {
            return percent >= 0d && percent <= 100d;
        }

        /// <summary>S = -sum(p ln p) over the positive intensities normalised to sum 1; 0 for a spectrum without one.</summary>
        public static double SpectralEntropy(IEnumerable<SpectrumPeak> peaks) {
            if (peaks is null) {
                throw new ArgumentNullException(nameof(peaks));
            }
            var intensities = peaks.Select(peak => peak.Intensity).Where(IsPositive).ToArray();
            var total = intensities.Sum();
            if (!(total > 0d)) {
                return 0d;
            }
            var entropy = 0d;
            foreach (var intensity in intensities) {
                var p = intensity / total;
                entropy -= p * Math.Log(p);
            }
            return entropy;
        }

        /// <summary>
        /// Drop the weakest positive ions (equal intensities together) until the spectral entropy is at most
        /// <paramref name="maxEntropy"/>, and keep the spectrum raw when fewer than <see cref="MinimumPeaks"/> positive
        /// ions would remain. The kept peaks stay in their input order; entries whose intensity is not positive are kept.
        /// </summary>
        /// <param name="peaks">The spectrum. It is not modified.</param>
        /// <param name="maxEntropy">The ceiling, finite and positive.</param>
        /// <param name="protectedPercentOfBasePeak">Ions at or above this percentage of the base peak are never dropped; 0 turns the floor off.</param>
        public static MsmsEntropyCleanupResult Clean(List<SpectrumPeak> peaks, double maxEntropy, double protectedPercentOfBasePeak = 0d) {
            var result = CleanByEntropy(peaks, maxEntropy, protectedPercentOfBasePeak);
            if (result.Removed > 0 && result.Peaks.Count(peak => IsPositive(peak.Intensity)) < MinimumPeaks) {
                return new MsmsEntropyCleanupResult(peaks, result.EntropyBefore, result.EntropyBefore, 0, null, result.BasePeak, null, MsmsEntropyCleanupStatus.KeptRawTooFewPeaks);
            }
            return result;
        }

        /// <summary>
        /// The entropy rule alone, without the <see cref="MinimumPeaks"/> floor of <see cref="Clean"/>.
        /// </summary>
        public static MsmsEntropyCleanupResult CleanByEntropy(List<SpectrumPeak> peaks, double maxEntropy, double protectedPercentOfBasePeak = 0d) {
            if (peaks is null) {
                throw new ArgumentNullException(nameof(peaks));
            }
            if (!IsValidMaxEntropy(maxEntropy)) {
                throw new ArgumentOutOfRangeException(nameof(maxEntropy), maxEntropy, "The maximum spectral entropy must be a finite positive number.");
            }
            if (!IsValidProtectedPercentOfBasePeak(protectedPercentOfBasePeak)) {
                throw new ArgumentOutOfRangeException(nameof(protectedPercentOfBasePeak), protectedPercentOfBasePeak, "The protected percentage of the base peak must be from 0 to 100.");
            }

            var before = SpectralEntropy(peaks);
            var intensities = peaks.Select(peak => peak.Intensity).Where(IsPositive).OrderByDescending(intensity => intensity).ToArray();
            double? basePeak = intensities.Length > 0 ? intensities[0] : (double?)null;
            if (before <= maxEntropy) {
                return new MsmsEntropyCleanupResult(peaks, before, before, 0, null, basePeak, null, MsmsEntropyCleanupStatus.Unchanged);
            }

            // Entropy of the k strongest ions: S_k = ln T_k - (sum_{i<=k} I_i ln I_i) / T_k, with T_k their sum.
            double total = 0d, weighted = 0d;
            var threshold = intensities[0];
            var index = 0;
            while (index < intensities.Length) {
                var level = intensities[index];
                double groupTotal = 0d, groupWeighted = 0d;
                while (index < intensities.Length && intensities[index] == level) {
                    groupTotal += level;
                    groupWeighted += level * Math.Log(level);
                    index++;
                }
                double candidateTotal = total + groupTotal, candidateWeighted = weighted + groupWeighted;
                if (Math.Log(candidateTotal) - candidateWeighted / candidateTotal > maxEntropy) {
                    break;
                }
                total = candidateTotal;
                weighted = candidateWeighted;
                threshold = level;
            }
            // The loop decides the status (as spectrum_cleanup.py does), not a recomputed entropy, which can differ in
            // the last bits: the strongest level alone is above the ceiling exactly when nothing was taken.
            var reachedCeiling = total > 0d;
            var protectedLevel = basePeak.Value * protectedPercentOfBasePeak / 100d;
            var floorKeepsMore = protectedPercentOfBasePeak > 0d && protectedLevel < threshold;
            if (floorKeepsMore) {
                threshold = protectedLevel;
            }

            var kept = new List<SpectrumPeak>(peaks.Count);
            var removed = 0;
            double? largestRemoved = null;
            foreach (var peak in peaks) {
                if (!IsPositive(peak.Intensity) || peak.Intensity >= threshold) {
                    kept.Add(peak);
                }
                else {
                    removed++;
                    largestRemoved = Math.Max(largestRemoved ?? peak.Intensity, peak.Intensity);
                }
            }
            if (removed == 0) {
                // The strongest level alone is above the ceiling, or the floor protects every ion.
                return new MsmsEntropyCleanupResult(peaks, before, before, 0, null, basePeak, null, MsmsEntropyCleanupStatus.AboveCeiling);
            }
            var after = SpectralEntropy(kept);
            var status = (floorKeepsMore ? after <= maxEntropy : reachedCeiling) ? MsmsEntropyCleanupStatus.Cleaned : MsmsEntropyCleanupStatus.AboveCeiling;
            var keptThreshold = kept.Where(peak => IsPositive(peak.Intensity)).Min(peak => peak.Intensity);
            return new MsmsEntropyCleanupResult(kept, before, after, removed, keptThreshold, basePeak, largestRemoved, status);
        }

        /// <summary>
        /// Clean the spectrum of every result in place, and recompute the SPLASH of a result that had one and lost ions.
        /// Results without a spectrum are left alone and not counted.
        /// </summary>
        public static MsmsEntropyCleanupSummary Apply(IEnumerable<MSDecResult> results, double maxEntropy, double protectedPercentOfBasePeak = 0d) {
            if (results is null) {
                throw new ArgumentNullException(nameof(results));
            }
            var summary = new MsmsEntropyCleanupSummary();
            foreach (var result in results) {
                if (result?.Spectrum is null || result.Spectrum.Count == 0) {
                    continue;
                }
                var cleaned = Clean(result.Spectrum, maxEntropy, protectedPercentOfBasePeak);
                summary.Spectra++;
                switch (cleaned.Status) {
                    case MsmsEntropyCleanupStatus.Unchanged: summary.Unchanged++; break;
                    case MsmsEntropyCleanupStatus.Cleaned: summary.Cleaned++; break;
                    case MsmsEntropyCleanupStatus.AboveCeiling: summary.AboveCeiling++; break;
                    case MsmsEntropyCleanupStatus.KeptRawTooFewPeaks: summary.KeptRawTooFewPeaks++; break;
                }
                if (cleaned.Removed > 0) {
                    summary.PeaksRemoved += cleaned.Removed;
                    result.Spectrum = cleaned.Peaks;
                    if (!string.IsNullOrEmpty(result.Splash)) {
                        result.Splash = MSDecHandler.CalculateSplash(result.Spectrum);
                    }
                }
            }
            return summary;
        }

        /// <summary>
        /// Whether the project's spectra were cleaned: on, and an LC-MS project, the only mode that runs the cleanup.
        /// </summary>
        public static bool IsAppliedIn(ParameterBase parameter) {
            return parameter?.ChromDecBaseParam is { IsDoMsmsEntropyCleanup: true }
                && parameter.MachineCategory == MachineCategory.LCMS;
        }

        /// <summary>
        /// The cleanup setting as exports record it, for example "max_entropy=4.5; logarithm=natural", or null when the
        /// project's spectra were not cleaned, so that an export without the cleanup is written exactly as before.
        /// </summary>
        public static string? Describe(ParameterBase parameter) {
            if (!IsAppliedIn(parameter)) {
                return null;
            }
            var chromDec = parameter.ChromDecBaseParam;
            var text = "max_entropy=" + chromDec.MsmsMaxSpectralEntropy.ToString(CultureInfo.InvariantCulture) + "; logarithm=natural";
            if (chromDec.MsmsEntropyCleanupProtectedPercentOfBasePeak > 0d) {
                text += "; protected_pct_of_base=" + chromDec.MsmsEntropyCleanupProtectedPercentOfBasePeak.ToString(CultureInfo.InvariantCulture);
            }
            return text;
        }

        private static bool IsPositive(double intensity) => intensity > 0d;
    }
}
