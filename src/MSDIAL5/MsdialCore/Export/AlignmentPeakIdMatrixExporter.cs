using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export;

/// <summary>
/// Exports the compact, stable link from alignment spots to source mdpeak rows.
/// </summary>
public sealed class AlignmentPeakIdMatrixExporter
{
    public void Export(
        Stream stream,
        IEnumerable<AlignmentSpotProperty> spots,
        IReadOnlyList<AnalysisFileBean> files)
    {
        var orderedFiles = files.OrderBy(file => file.AnalysisFileId).ToArray();
        using var writer = CreateWriter(stream);
        WriteHeader(writer, orderedFiles);
        foreach (var spot in Flatten(spots)) {
            var peaks = spot.AlignedPeakProperties.ToDictionary(peak => peak.FileID);
            WriteRow(
                writer,
                spot.MasterAlignmentID,
                orderedFiles.Select(file => peaks.TryGetValue(file.AnalysisFileId, out var peak)
                    ? NormalizePeakId(peak.MasterPeakID)
                    : -1));
        }
    }

    public void Export(
        Stream stream,
        IEnumerable<AlignmentSpotProperty> spots,
        IReadOnlyList<AnalysisFileBean> files,
        AlignmentLightPeakStore peakStore)
    {
        var orderedFiles = files.OrderBy(file => file.AnalysisFileId).ToArray();
        using var writer = CreateWriter(stream);
        WriteHeader(writer, orderedFiles);
        foreach (var spot in Flatten(spots)) {
            var peaks = peakStore.ReadSpotPeaks(spot.MasterAlignmentID).ToDictionary(peak => peak.FileID);
            WriteRow(
                writer,
                spot.MasterAlignmentID,
                orderedFiles.Select(file => peaks.TryGetValue(file.AnalysisFileId, out var peak)
                    ? NormalizePeakId(peak.MasterPeakID)
                    : -1));
        }
    }

    private static StreamWriter CreateWriter(Stream stream)
        => new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);

    private static void WriteHeader(StreamWriter writer, IReadOnlyList<AnalysisFileBean> files)
    {
        writer.Write("alignment_master_id");
        foreach (var file in files) {
            writer.Write('\t');
            writer.Write(Sanitize(file.AnalysisFileName));
        }
        writer.WriteLine();
    }

    private static void WriteRow(StreamWriter writer, int alignmentMasterId, IEnumerable<int> peakIds)
    {
        writer.Write(alignmentMasterId.ToString(CultureInfo.InvariantCulture));
        foreach (var peakId in peakIds) {
            writer.Write('\t');
            writer.Write(peakId.ToString(CultureInfo.InvariantCulture));
        }
        writer.WriteLine();
    }

    private static int NormalizePeakId(int peakId) => peakId >= 0 ? peakId : -1;

    private static IEnumerable<AlignmentSpotProperty> Flatten(IEnumerable<AlignmentSpotProperty> spots)
    {
        foreach (var spot in spots) {
            yield return spot;
            foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? []) {
                yield return driftSpot;
            }
        }
    }

    private static string Sanitize(string value)
        => (value ?? String.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
