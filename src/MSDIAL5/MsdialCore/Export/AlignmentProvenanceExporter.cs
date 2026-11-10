using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export;

/// <summary>
/// Exports the optional detailed audit sidecar that explains, per alignment spot and per file, which
/// source peak the aligned value came from.
/// </summary>
/// <remarks>
/// The file's purpose is provenance, so a column must never assert something the run did not establish.
/// Two consequences run through the whole exporter.
///
/// A member with no source peak has no source spectrum either, so every raw-spectrum pointer column is
/// written empty rather than carrying the 0 that the gap filler leaves in those fields. A 0 is a real
/// scan index, and publishing it would point an auditor at a spectrum that has nothing to do with the
/// member.
///
/// The peak-id columns are normalized to -1 for any missing source peak, which is the sentinel the
/// compact <see cref="AlignmentPeakIdMatrixExporter"/> matrix already uses and the one consumers
/// document. The distinction the raw values carried, -2 for gap-filled against -1 for never detected, is
/// preserved by name in <c>peak_origin</c> instead of by magic number, so a reader does not have to know
/// the gap filler's internals to interpret a cell.
/// </remarks>
public sealed class AlignmentProvenanceExporter
{
    private const string OriginDetected = "detected";
    private const string OriginGapFilled = "gap_filled";
    private const string OriginAbsent = "absent";

    /// <summary>The gap filler's marker for a cell it filled, as opposed to one never detected.</summary>
    private const int GapFilledPeakId = -2;

    /// <summary>The value written wherever the run established nothing.</summary>
    private const string NotApplicable = "";

    /// <summary>The peak-id sentinel shared with the compact peak-ID matrix export.</summary>
    private const int MissingPeakId = -1;

    private static readonly string[] Headers = [
        "alignment_master_id",
        "alignment_local_id",
        "parent_alignment_id",
        "file_id",
        "file_name",
        "is_representative",
        "has_source_peak",
        "peak_origin",
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

    /// <summary>
    /// Appended when the alignment moved the per-file RTs onto a reference file's axis: rt_min is then
    /// that aligned RT, and this column is the file's measured RT, the axis of the raw scan IDs.
    /// </summary>
    public const string OriginalRtHeader = "rt_original_min";

    private readonly Func<int, double, double?>? _originalRt;

    public AlignmentProvenanceExporter()
    {
    }

    /// <param name="originalRt">
    /// The measured RT of a file's aligned RT (file ID, aligned RT), when the automatic alignment RT
    /// correction moved the aligned RTs onto a reference file's axis; its values are written in the
    /// appended <see cref="OriginalRtHeader"/> column. Null writes the columns of a run without it.
    /// </param>
    public AlignmentProvenanceExporter(Func<int, double, double?>? originalRt)
    {
        _originalRt = originalRt;
    }

    public void Export(Stream stream, IEnumerable<AlignmentSpotProperty> spots)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
        WriteHeader(writer);
        foreach (var spot in Flatten(spots)) {
            foreach (var peak in spot.AlignedPeakProperties.OrderBy(peak => peak.FileID)) {
                WriteMember(writer, spot, peak);
            }
        }
    }

    public void Export(Stream stream, IEnumerable<AlignmentSpotProperty> spots, AlignmentLightPeakStore peakStore)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
        WriteHeader(writer);
        foreach (var spot in Flatten(spots)) {
            foreach (var peak in peakStore.ReadSpotPeaks(spot.MasterAlignmentID).OrderBy(peak => peak.FileID)) {
                WriteMember(writer, spot, peak);
            }
        }
    }

    private void WriteHeader(StreamWriter writer)
    {
        WriteRow(writer, Headers, _originalRt is null ? null : OriginalRtHeader);
    }

    private string? OriginalRtCell(int fileId, double? alignedRt)
    {
        if (_originalRt is null) {
            return null;
        }
        return alignedRt is double rt && rt >= 0d ? Format(_originalRt(fileId, rt)) : NotApplicable;
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

    private void WriteMember(StreamWriter writer, AlignmentSpotProperty spot, AlignmentChromPeakFeature peak)
    {
        var hasSourcePeak = peak.MasterPeakID >= 0;
        var ms2Ids = hasSourcePeak
            ? peak.MS2RawSpectrumID2CE?.Keys.OrderBy(id => id).ToArray() ?? []
            : [];
        var collisionEnergies = hasSourcePeak && peak.MS2RawSpectrumID2CE is not null
            ? peak.MS2RawSpectrumID2CE
                .OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Key}:{Format(pair.Value)}")
            : Enumerable.Empty<string>();
        WriteRow(writer, [
            spot.MasterAlignmentID.ToString(CultureInfo.InvariantCulture),
            spot.AlignmentID.ToString(CultureInfo.InvariantCulture),
            spot.ParentAlignmentID.ToString(CultureInfo.InvariantCulture),
            peak.FileID.ToString(CultureInfo.InvariantCulture),
            Sanitize(peak.FileName),
            BooleanText(peak.FileID == spot.RepresentativeFileID),
            BooleanText(hasSourcePeak),
            PeakOrigin(peak.MasterPeakID),
            PeakIdText(peak.MasterPeakID),
            PeakIdText(peak.PeakID),
            PeakIdText(peak.ParentPeakID),
            SpectrumIdText(hasSourcePeak, peak.MS1RawSpectrumID),
            SpectrumIdText(hasSourcePeak, peak.MS1RawSpectrumIdTop),
            SpectrumIdText(hasSourcePeak, peak.MS2RawSpectrumID),
            String.Join(";", ms2Ids),
            String.Join(";", collisionEnergies),
            Format(peak.ChromXsTop?.RT.Value),
            // Mass, not ChromXsTop.Mz. On an aligned peak ChromXsTop carries the chromatogram axis and the
            // gap filler resets it outright, so reading Mz here reported a sentinel for every member that
            // did have a source peak and a real value only for the gap-filled ones, which is backwards.
            // Mass is also what the .mdalign MZ column and the light store use.
            Format(peak.Mass),
            Format(peak.PeakHeightTop),
            Format(peak.PeakAreaAboveZero),
            Format(peak.PeakAreaAboveBaseline),
        ], OriginalRtCell(peak.FileID, peak.ChromXsTop?.RT.Value));
    }

    private void WriteMember(StreamWriter writer, AlignmentSpotProperty spot, AlignmentLightPeakRow peak)
    {
        var hasSourcePeak = peak.MasterPeakID >= 0;
        WriteRow(writer, [
            spot.MasterAlignmentID.ToString(CultureInfo.InvariantCulture),
            spot.AlignmentID.ToString(CultureInfo.InvariantCulture),
            spot.ParentAlignmentID.ToString(CultureInfo.InvariantCulture),
            peak.FileID.ToString(CultureInfo.InvariantCulture),
            Sanitize(peak.FileName),
            BooleanText(peak.FileID == spot.RepresentativeFileID),
            BooleanText(hasSourcePeak),
            PeakOrigin(peak.MasterPeakID),
            PeakIdText(peak.MasterPeakID),
            PeakIdText(peak.PeakID),
            // The light store does not persist the parent peak id, the full MS1 id, the ordered MS2 id set
            // or the baseline-corrected area. Those cells are empty in light mode for that reason, not
            // because the run failed to establish them.
            NotApplicable,
            NotApplicable,
            SpectrumIdText(hasSourcePeak, peak.MS1RawSpectrumIdTop),
            SpectrumIdText(hasSourcePeak, peak.MS2RawSpectrumID),
            // Gated on the source peak, like the in-memory overload, so a gap-filled row does not report
            // the 0 the gap filler left behind as if it were an acquired spectrum.
            SpectrumIdText(hasSourcePeak, peak.MS2RawSpectrumID),
            NotApplicable,
            Format(peak.Rt),
            Format(peak.Mass),
            Format(peak.PeakHeightTop),
            Format(peak.PeakAreaAboveZero),
            NotApplicable,
        ], OriginalRtCell(peak.FileID, peak.Rt));
    }

    /// <summary>
    /// Writes one row, refusing a field count that does not match the header; <paramref name="appended"/>,
    /// when not null, is the appended <see cref="OriginalRtHeader"/> cell.
    /// </summary>
    private static void WriteRow(StreamWriter writer, string[] values, string? appended)
    {
        // The header and the two value lists are maintained by hand. Adding a column to one and not the
        // others would otherwise produce a ragged TSV that is wrong in only one of the two Console modes.
        if (values.Length != Headers.Length) {
            throw new InvalidOperationException(
                $"Alignment provenance row has {values.Length} fields but the header declares {Headers.Length}.");
        }
        var line = String.Join("\t", values);
        writer.WriteLine(appended is null ? line : line + "\t" + appended);
    }

    /// <summary>Names why a member has no source peak, so the peak-id sentinel does not have to.</summary>
    private static string PeakOrigin(int masterPeakId)
    {
        if (masterPeakId >= 0) {
            return OriginDetected;
        }
        return masterPeakId == GapFilledPeakId ? OriginGapFilled : OriginAbsent;
    }

    private static string PeakIdText(int peakId)
        => (peakId >= 0 ? peakId : MissingPeakId).ToString(CultureInfo.InvariantCulture);

    private static string SpectrumIdText(bool hasSourcePeak, int spectrumId)
        => hasSourcePeak ? spectrumId.ToString(CultureInfo.InvariantCulture) : NotApplicable;

    private static string Format(double? value)
        => value?.ToString("G17", CultureInfo.InvariantCulture) ?? String.Empty;

    private static string BooleanText(bool value) => value ? "true" : "false";

    private static string Sanitize(string value)
        => (value ?? String.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
