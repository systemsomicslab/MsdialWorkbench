using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// Pairs each MS1 feature with the ion at its m/z in the product-ion scan the peak picking chose for it, so
/// that the product-ion scans' m/z can be measured against the survey's.
/// </summary>
/// <remarks>
/// A SWATH window, an AIF scan and a DDA product-ion scan all pass part of the precursor through the collision
/// cell unfragmented. With the MS1 survey as the reference, its m/z in the product-ion scan gives the error of
/// that scan at that m/z, from the sample itself and in every file.
/// <para>
/// The search window is wide (<see cref="DefaultSearchPpm"/>), so that an error of tens of ppm is still found,
/// and the most intense ion in it is taken. In a crowded scan that ion is often another one, so the pairs are
/// candidates: <see cref="MassErrorEstimate"/> finds the error as their mode.
/// </para>
/// <para>
/// A feature the peak picking marked as an M+1 or heavier isotope of another is skipped: its monoisotopic
/// feature is paired instead. The spectra must be sorted by m/z, as RawDataHandler returns them.
/// </para>
/// <para>
/// In profile data the most intense point is the apex of a profile peak, as MS-DIAL's local-maximum centroiding
/// takes it for the deconvolution: each error is then rounded to the sampling interval, which widens the peak of
/// errors but does not move its median.
/// </para>
/// </remarks>
public sealed class PrecursorCalibrantCollector
{
    /// <summary>Half width of the search window around the MS1 m/z, in ppm.</summary>
    public const double DefaultSearchPpm = 200d;

    /// <summary>
    /// How far the M+1 isotope in the product-ion scan may lie from where the monoisotopic pair puts it, in ppm.
    /// </summary>
    public const double DefaultIsotopeConsistencyPpm = 10d;

    private const double C13Spacing = 1.0033548378d;

    public PrecursorCalibrantCollector(double ms1Tolerance, double searchPpm = DefaultSearchPpm, double isotopeConsistencyPpm = DefaultIsotopeConsistencyPpm) {
        if (!(ms1Tolerance > 0d)) {
            throw new ArgumentOutOfRangeException(nameof(ms1Tolerance), ms1Tolerance, "The MS1 tolerance must be positive.");
        }
        if (!(searchPpm > 0d)) {
            throw new ArgumentOutOfRangeException(nameof(searchPpm), searchPpm, "The search window must be positive.");
        }
        if (!(isotopeConsistencyPpm > 0d)) {
            throw new ArgumentOutOfRangeException(nameof(isotopeConsistencyPpm), isotopeConsistencyPpm, "The isotope consistency window must be positive.");
        }
        Ms1Tolerance = ms1Tolerance;
        SearchPpm = searchPpm;
        IsotopeConsistencyPpm = isotopeConsistencyPpm;
    }

    /// <summary>Tolerance, in Da, for finding the feature and its M+1 isotope in the MS1 peak-top scan.</summary>
    public double Ms1Tolerance { get; }

    public double SearchPpm { get; }

    public double IsotopeConsistencyPpm { get; }

    /// <summary>
    /// One candidate for each feature that has a product-ion scan with an ion within <see cref="SearchPpm"/> of
    /// its MS1 m/z, in the order of <paramref name="features"/>.
    /// </summary>
    /// <param name="spectra">All spectra of the file, indexed as <see cref="ChromatogramPeakFeature.MS1RawSpectrumIdTop"/>
    /// and <see cref="ChromatogramPeakFeature.MS2RawSpectrumID"/> index them.</param>
    public List<Ms2PrecursorCalibrant> Collect(IReadOnlyList<RawSpectrum> spectra, IEnumerable<ChromatogramPeakFeature> features) {
        if (spectra is null) {
            throw new ArgumentNullException(nameof(spectra));
        }
        if (features is null) {
            throw new ArgumentNullException(nameof(features));
        }

        var result = new List<Ms2PrecursorCalibrant>();
        var totalIntensities = new Dictionary<int, double>();
        foreach (var feature in features) {
            if (feature is null || feature.PeakCharacter is { IsotopeWeightNumber: > 0 }) {
                continue;
            }
            var ms2 = SpectrumAt(spectra, feature.MS2RawSpectrumID);
            if (ms2 is null || ms2.MsLevel < 2 || ms2.Spectrum is not { Length: > 0 } product) {
                continue;
            }

            var featureMz = feature.PeakFeature.Mass;
            var ms1Mz = featureMz;
            var ms1Intensity = feature.PeakFeature.PeakHeightTop;
            var ms1 = SpectrumAt(spectra, feature.MS1RawSpectrumIdTop);
            RawPeakElement[]? survey = null;
            if (ms1 is { MsLevel: <= 1, Spectrum: { Length: > 0 } peaks }) {
                survey = peaks;
                var top = MostIntense(peaks, featureMz - Ms1Tolerance, featureMz + Ms1Tolerance);
                if (top >= 0) {
                    ms1Mz = peaks[top].Mz;
                    ms1Intensity = peaks[top].Intensity;
                }
            }
            if (!(ms1Mz > 0d)) {
                continue;
            }

            var halfWidth = ms1Mz * SearchPpm * 1e-6;
            var found = MostIntense(product, ms1Mz - halfWidth, ms1Mz + halfWidth);
            if (found < 0) {
                continue;
            }
            var ms2Mz = product[found].Mz;

            var isotopeConfirmed = false;
            if (survey is not null) {
                var charge = Math.Max(1, Math.Abs(feature.PeakCharacter?.Charge ?? 1));
                var spacing = C13Spacing / charge;
                var isotope = MostIntense(survey, ms1Mz + spacing - Ms1Tolerance, ms1Mz + spacing + Ms1Tolerance);
                if (isotope >= 0) {
                    var expected = survey[isotope].Mz * (ms2Mz / ms1Mz);
                    var tolerance = expected * IsotopeConsistencyPpm * 1e-6;
                    isotopeConfirmed = MostIntense(product, expected - tolerance, expected + tolerance) >= 0;
                }
            }

            if (!totalIntensities.TryGetValue(feature.MS2RawSpectrumID, out var totalIntensity)) {
                totalIntensity = 0d;
                foreach (var peak in product) {
                    totalIntensity += peak.Intensity;
                }
                totalIntensities[feature.MS2RawSpectrumID] = totalIntensity;
            }

            var (lower, upper) = IsolationWindow(ms2.Precursor);
            result.Add(new Ms2PrecursorCalibrant(
                feature.PeakID,
                ms2.ScanStartTime,
                ms1Mz,
                ms1Intensity,
                ms2Mz,
                product[found].Intensity,
                feature.MS2RawSpectrumID,
                ms2.ExperimentID,
                lower,
                upper,
                ms2.CollisionEnergy,
                totalIntensity,
                isotopeConfirmed));
        }
        return result;
    }

    private static RawSpectrum? SpectrumAt(IReadOnlyList<RawSpectrum> spectra, int index) {
        return index >= 0 && index < spectra.Count ? spectra[index] : null;
    }

    /// <summary>The isolation window of a product-ion scan, or NaN bounds when the scan has no precursor record.</summary>
    internal static (double Lower, double Upper) IsolationWindow(RawPrecursorIon? precursor) {
        if (precursor is null) {
            return (double.NaN, double.NaN);
        }
        var target = precursor.IsolationTargetMz > 0d ? precursor.IsolationTargetMz : precursor.SelectedIonMz;
        return (target - precursor.IsolationWindowLowerOffset, target + precursor.IsolationWindowUpperOffset);
    }

    /// <summary>Index of the most intense peak with lower &lt;= m/z &lt;= upper, or -1 when there is none.</summary>
    internal static int MostIntense(RawPeakElement[] peaks, double lower, double upper) {
        var begin = 0;
        var end = peaks.Length;
        while (begin < end) {
            var middle = begin + (end - begin) / 2;
            if (peaks[middle].Mz < lower) {
                begin = middle + 1;
            }
            else {
                end = middle;
            }
        }
        var best = -1;
        var bestIntensity = double.NegativeInfinity;
        for (var i = begin; i < peaks.Length && peaks[i].Mz <= upper; i++) {
            if (peaks[i].Intensity > bestIntensity) {
                best = i;
                bestIntensity = peaks[i].Intensity;
            }
        }
        return best;
    }
}
