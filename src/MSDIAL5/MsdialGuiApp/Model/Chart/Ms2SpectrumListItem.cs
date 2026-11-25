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
    public Ms2SpectrumListItem(string title, string detail, bool isRepresentative, IReadOnlyList<SpectrumPeak>? spectrum, string? collisionEnergy = null) {
        Title = title ?? string.Empty;
        Detail = detail ?? string.Empty;
        IsRepresentative = isRepresentative;
        Spectrum = new MsSpectrum(spectrum ?? []);
        CollisionEnergy = collisionEnergy;
    }

    public string Title { get; }
    public string Detail { get; }
    public bool IsRepresentative { get; }
    public MsSpectrum Spectrum { get; }
    public bool HasSpectrum => Spectrum.HasSpectrum;

    /// <summary>The collision energy, shown on its own next to the title: "CE 35 eV", "CE n/a", or null where there is no MS2.</summary>
    public string? CollisionEnergy { get; }
}

/// <summary>One sample's MS2 of an alignment spot, as read from that sample's deconvolution result.</summary>
internal sealed class AlignedSampleMs2
{
    public AlignedSampleMs2(int fileId, string fileName, string fileClass, IReadOnlyList<SpectrumPeak>? spectrum, double? collisionEnergy = null) {
        FileId = fileId;
        FileName = fileName ?? string.Empty;
        FileClass = fileClass ?? string.Empty;
        Spectrum = spectrum;
        CollisionEnergy = collisionEnergy;
    }

    public int FileId { get; }
    public string FileName { get; }
    public string FileClass { get; }
    public IReadOnlyList<SpectrumPeak>? Spectrum { get; }

    /// <summary>The collision energy of the sample's representative MS2 scan, as read from the raw data; null when unknown.</summary>
    public double? CollisionEnergy { get; }
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

/// <summary>How the vertical axis of a spectrum is drawn: the same choices as in the main MS/MS view.</summary>
internal enum Ms2SpectrumAxisType
{
    Relative,
    Absolute,
    Log10,
    Sqrt,
}

internal static class Ms2SpectrumList
{
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

    /// <summary>The lowest and highest intensity of the spectrum; (0, 1) when it has no peak.</summary>
    public static (double Minimum, double Maximum) GetIntensityRange(MsSpectrum? spectrum) {
        if (spectrum is null || !spectrum.HasSpectrum) {
            return (0d, 1d);
        }
        return (spectrum.Spectrum.Min(peak => peak.Intensity), spectrum.Spectrum.Max(peak => peak.Intensity));
    }

    /// <summary>
    /// Reads an axis type from the label the main MS/MS view gives it ("Relative", "Absolute", "Log10" or "Sqrt").
    /// </summary>
    public static bool TryParseAxisType(string? label, out Ms2SpectrumAxisType type) {
        type = Ms2SpectrumAxisType.Relative;
        return !string.IsNullOrWhiteSpace(label)
            && Enum.TryParse(label!.Trim(), ignoreCase: true, out type)
            && Enum.IsDefined(typeof(Ms2SpectrumAxisType), type);
    }

    /// <summary>The vertical axis title, worded as in the main MS/MS view.</summary>
    public static string GetAxisTitle(Ms2SpectrumAxisType type) {
        return type switch
        {
            Ms2SpectrumAxisType.Absolute => "Absolute abundance",
            Ms2SpectrumAxisType.Log10 => "Relative abundance (log10)",
            Ms2SpectrumAxisType.Sqrt => "Relative abundance (^1/2)",
            _ => "Relative abundance",
        };
    }

    public static IReadOnlyList<Ms2SpectrumListItem> FromAlignedSamples(IEnumerable<AlignedSampleMs2> samples, int representativeFileId) {
        return (samples ?? []).Select(sample => {
            var count = sample.Spectrum?.Count ?? 0;
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(sample.FileClass)) {
                parts.Add($"Class: {sample.FileClass}");
            }
            parts.Add(DescribePeakCount(count));
            var ce = count > 0 ? DescribeCollisionEnergy(sample.CollisionEnergy) : null;
            return new Ms2SpectrumListItem(sample.FileName, string.Join(", ", parts), sample.FileId == representativeFileId, sample.Spectrum, ce);
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
            parts.Add(DescribePeakCount(scan.Spectrum?.Count ?? 0));
            return new Ms2SpectrumListItem($"Scan {scan.ScanId}", string.Join(", ", parts), scan.ScanId == representativeScanId, scan.Spectrum, DescribeCollisionEnergy(scan.CollisionEnergy));
        }).ToList();
    }

    public static string Summarize(IReadOnlyCollection<Ms2SpectrumListItem> items, string unit) {
        if (items is null || items.Count == 0) {
            return $"No {unit}.";
        }
        var withMs2 = items.Count(item => item.HasSpectrum);
        return $"{withMs2} of {items.Count} {unit} have an MS2 spectrum.";
    }

    /// <summary>
    /// The collision energy of a sample's representative MS2 scan, from the scan-to-energy map that peak picking reads
    /// from the raw data. Falls back to the first scan of the sample when the representative one is not in the map.
    /// </summary>
    public static double? GetCollisionEnergy(int representativeScanId, IReadOnlyDictionary<int, double>? scanToCollisionEnergy) {
        if (scanToCollisionEnergy is null || scanToCollisionEnergy.Count == 0) {
            return null;
        }
        if (scanToCollisionEnergy.TryGetValue(representativeScanId, out var ce)) {
            return ce;
        }
        return scanToCollisionEnergy.OrderBy(pair => pair.Key).First().Value;
    }

    public const string UnknownCollisionEnergy = "CE n/a";

    /// <summary>"CE 35 eV", or "CE n/a" when the raw data did not record it.</summary>
    private static string DescribeCollisionEnergy(double? collisionEnergy) {
        return collisionEnergy is double ce && ce > 0d
            ? string.Format(CultureInfo.InvariantCulture, "CE {0:G4} eV", ce)
            : UnknownCollisionEnergy;
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
