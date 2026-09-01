using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export;

public sealed class AlignmentProvenanceExporter
{
    private static readonly string[] Headers = [
        "alignment_master_id",
        "alignment_local_id",
        "parent_alignment_id",
        "file_id",
        "file_name",
        "is_representative",
        "has_source_peak",
        "source_master_peak_id",
        "source_peak_id",
        "source_parent_peak_id",
        "ms1_raw_spectrum_id",
        "ms1_raw_spectrum_id_top",
        "ms2_raw_spectrum_id",
        "ms2_raw_spectrum_ids",
        "ms2_collision_energies",
        "rt_min",
        "mz",
        "height",
        "area_above_zero",
        "area_above_baseline",
    ];

    public void Export(Stream stream, IEnumerable<AlignmentSpotProperty> spots)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
        writer.WriteLine(String.Join("\t", Headers));
        foreach (var spot in Flatten(spots)) {
            foreach (var peak in spot.AlignedPeakProperties.OrderBy(peak => peak.FileID)) {
                WriteMember(writer, spot, peak);
            }
        }
    }

    public void Export(Stream stream, IEnumerable<AlignmentSpotProperty> spots, AlignmentLightPeakStore peakStore)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
        writer.WriteLine(String.Join("\t", Headers));
        foreach (var spot in Flatten(spots)) {
            foreach (var peak in peakStore.ReadSpotPeaks(spot.MasterAlignmentID).OrderBy(peak => peak.FileID)) {
                WriteMember(writer, spot, peak);
            }
        }
    }

    private static IEnumerable<AlignmentSpotProperty> Flatten(IEnumerable<AlignmentSpotProperty> spots)
    {
        foreach (var spot in spots) {
            yield return spot;
            foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? []) {
                yield return driftSpot;
            }
        }
    }

    private static void WriteMember(StreamWriter writer, AlignmentSpotProperty spot, AlignmentChromPeakFeature peak)
    {
        var ms2Ids = peak.MS2RawSpectrumID2CE?.Keys.OrderBy(id => id).ToArray() ?? [];
        var collisionEnergies = peak.MS2RawSpectrumID2CE?
            .OrderBy(pair => pair.Key)
            .Select(pair => $"{pair.Key}:{Format(pair.Value)}") ?? [];
        var values = new[] {
            spot.MasterAlignmentID.ToString(CultureInfo.InvariantCulture),
            spot.AlignmentID.ToString(CultureInfo.InvariantCulture),
            spot.ParentAlignmentID.ToString(CultureInfo.InvariantCulture),
            peak.FileID.ToString(CultureInfo.InvariantCulture),
            Sanitize(peak.FileName),
            BooleanText(peak.FileID == spot.RepresentativeFileID),
            BooleanText(peak.MasterPeakID >= 0),
            peak.MasterPeakID.ToString(CultureInfo.InvariantCulture),
            peak.PeakID.ToString(CultureInfo.InvariantCulture),
            peak.ParentPeakID.ToString(CultureInfo.InvariantCulture),
            peak.MS1RawSpectrumID.ToString(CultureInfo.InvariantCulture),
            peak.MS1RawSpectrumIdTop.ToString(CultureInfo.InvariantCulture),
            peak.MS2RawSpectrumID.ToString(CultureInfo.InvariantCulture),
            String.Join(";", ms2Ids),
            String.Join(";", collisionEnergies),
            Format(peak.ChromXsTop?.RT.Value),
            Format(peak.ChromXsTop?.Mz.Value),
            Format(peak.PeakHeightTop),
            Format(peak.PeakAreaAboveZero),
            Format(peak.PeakAreaAboveBaseline),
        };
        writer.WriteLine(String.Join("\t", values));
    }

    private static void WriteMember(StreamWriter writer, AlignmentSpotProperty spot, AlignmentLightPeakRow peak)
    {
        var values = new[] {
            spot.MasterAlignmentID.ToString(CultureInfo.InvariantCulture),
            spot.AlignmentID.ToString(CultureInfo.InvariantCulture),
            spot.ParentAlignmentID.ToString(CultureInfo.InvariantCulture),
            peak.FileID.ToString(CultureInfo.InvariantCulture),
            Sanitize(peak.FileName),
            BooleanText(peak.FileID == spot.RepresentativeFileID),
            BooleanText(peak.MasterPeakID >= 0),
            peak.MasterPeakID.ToString(CultureInfo.InvariantCulture),
            peak.PeakID.ToString(CultureInfo.InvariantCulture),
            String.Empty,
            String.Empty,
            peak.MS1RawSpectrumIdTop.ToString(CultureInfo.InvariantCulture),
            peak.MS2RawSpectrumID.ToString(CultureInfo.InvariantCulture),
            peak.MS2RawSpectrumID >= 0 ? peak.MS2RawSpectrumID.ToString(CultureInfo.InvariantCulture) : String.Empty,
            String.Empty,
            Format(peak.Rt),
            Format(peak.Mass),
            Format(peak.PeakHeightTop),
            Format(peak.PeakAreaAboveZero),
            String.Empty,
        };
        writer.WriteLine(String.Join("\t", values));
    }

    private static string Format(double? value)
        => value?.ToString("G17", CultureInfo.InvariantCulture) ?? String.Empty;

    private static string BooleanText(bool value) => value ? "true" : "false";

    private static string Sanitize(string value)
        => (value ?? String.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
