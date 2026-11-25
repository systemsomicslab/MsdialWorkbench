using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart;

/// <summary>
/// One MS2 spectrum in the MS2 spectrum list of a peak or an alignment spot:
/// one sample's MS2 of an alignment spot, or one MS2 scan of a peak.
/// </summary>
internal sealed class Ms2SpectrumListItem
{
    public Ms2SpectrumListItem(string title, string detail, bool isRepresentative, IReadOnlyList<SpectrumPeak>? spectrum) {
        Title = title ?? string.Empty;
        Detail = detail ?? string.Empty;
        IsRepresentative = isRepresentative;
        Spectrum = new MsSpectrum(spectrum ?? []);
    }

    public string Title { get; }
    public string Detail { get; }
    public bool IsRepresentative { get; }
    public MsSpectrum Spectrum { get; }
    public bool HasSpectrum => Spectrum.HasSpectrum;
}

/// <summary>One sample's MS2 of an alignment spot, as read from that sample's deconvolution result.</summary>
internal sealed class AlignedSampleMs2
{
    public AlignedSampleMs2(int fileId, string fileName, string fileClass, IReadOnlyList<SpectrumPeak>? spectrum) {
        FileId = fileId;
        FileName = fileName ?? string.Empty;
        FileClass = fileClass ?? string.Empty;
        Spectrum = spectrum;
    }

    public int FileId { get; }
    public string FileName { get; }
    public string FileClass { get; }
    public IReadOnlyList<SpectrumPeak>? Spectrum { get; }
}

/// <summary>One MS2 scan acquired over a peak.</summary>
internal sealed class PeakMs2Scan
{
    public PeakMs2Scan(int scanId, double collisionEnergy, double? retentionTime, double? precursorMz, IReadOnlyList<SpectrumPeak>? spectrum) {
        ScanId = scanId;
        CollisionEnergy = collisionEnergy;
        RetentionTime = retentionTime;
        PrecursorMz = precursorMz;
        Spectrum = spectrum;
    }

    public int ScanId { get; }
    public double CollisionEnergy { get; }
    public double? RetentionTime { get; }
    public double? PrecursorMz { get; }
    public IReadOnlyList<SpectrumPeak>? Spectrum { get; }
}

internal static class Ms2SpectrumList
{
    public const double RelativeIntensityMaximum = 100d;

    /// <summary>
    /// Puts the representative spectrum first and keeps the rest in their given order.
    /// Spectra with no peaks are left out unless <paramref name="showEmpty"/> is true.
    /// </summary>
    public static IReadOnlyList<Ms2SpectrumListItem> Arrange(IEnumerable<Ms2SpectrumListItem> items, bool showEmpty) {
        if (items is null) {
            return [];
        }
        var kept = items.Where(item => item is not null && (showEmpty || item.HasSpectrum)).ToList();
        return kept.Where(item => item.IsRepresentative).Concat(kept.Where(item => !item.IsRepresentative)).ToList();
    }

    /// <summary>The m/z range that covers every given spectrum; (0, 1) when none has a peak.</summary>
    public static (double Minimum, double Maximum) GetMassRange(IEnumerable<MsSpectrum?> spectra) {
        var masses = (spectra ?? []).Where(s => s is not null && s.HasSpectrum).SelectMany(s => s!.Spectrum).Select(peak => peak.Mass).ToList();
        if (masses.Count == 0) {
            return (0d, 1d);
        }
        var (min, max) = (masses.Min(), masses.Max());
        return min == max ? (min - .5d, max + .5d) : (min, max);
    }

    /// <summary>
    /// Scales the spectrum so that its base peak is 100. A mirrored spectrum points down (-100 for the base peak),
    /// so the reference can be drawn under the measured spectrum on the same axis.
    /// </summary>
    public static List<SpectrumPeak> ToRelativeSpectrum(MsSpectrum? spectrum, bool mirrored = false) {
        if (spectrum is null || !spectrum.HasSpectrum) {
            return [];
        }
        var max = spectrum.Spectrum.Max(peak => peak.Intensity);
        if (max <= 0d) {
            return [];
        }
        var sign = mirrored ? -1d : 1d;
        return spectrum.Spectrum
            .Select(peak => new SpectrumPeak(peak.Mass, sign * peak.Intensity / max * RelativeIntensityMaximum))
            .ToList();
    }

    public static IReadOnlyList<Ms2SpectrumListItem> FromAlignedSamples(IEnumerable<AlignedSampleMs2> samples, int representativeFileId) {
        return (samples ?? []).Select(sample => {
            var count = sample.Spectrum?.Count ?? 0;
            var detail = string.IsNullOrEmpty(sample.FileClass)
                ? DescribePeakCount(count)
                : $"Class: {sample.FileClass}, {DescribePeakCount(count)}";
            return new Ms2SpectrumListItem(sample.FileName, detail, sample.FileId == representativeFileId, sample.Spectrum);
        }).ToList();
    }

    public static IReadOnlyList<Ms2SpectrumListItem> FromPeakScans(IEnumerable<PeakMs2Scan> scans, int representativeScanId) {
        return (scans ?? []).Select(scan => {
            var parts = new List<string>();
            if (scan.RetentionTime is double rt) {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "RT {0:F3} min", rt));
            }
            if (scan.PrecursorMz is double mz && mz > 0d) {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "Precursor m/z {0:F4}", mz));
            }
            if (scan.CollisionEnergy > 0d) {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "CE {0:G4} eV", scan.CollisionEnergy));
            }
            parts.Add(DescribePeakCount(scan.Spectrum?.Count ?? 0));
            return new Ms2SpectrumListItem($"Scan {scan.ScanId}", string.Join(", ", parts), scan.ScanId == representativeScanId, scan.Spectrum);
        }).ToList();
    }

    public static string Summarize(IReadOnlyCollection<Ms2SpectrumListItem> items, string unit) {
        if (items is null || items.Count == 0) {
            return $"No {unit}.";
        }
        var withMs2 = items.Count(item => item.HasSpectrum);
        return $"{withMs2} of {items.Count} {unit} have an MS2 spectrum.";
    }

    private static string DescribePeakCount(int count) {
        return count switch
        {
            0 => "no MS2",
            1 => "1 peak",
            _ => $"{count} peaks",
        };
    }
}
