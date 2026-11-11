using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export;

public sealed class AlignmentCSVExporter
{
    private const string DEFAULT_SEPARATOR = "\t";

    public AlignmentCSVExporter(string separator = DEFAULT_SEPARATOR) {
        Separator = separator;
    }

    public string Separator { get; }

    /// <summary>
    /// When set, written into the first header line's first cell, which is otherwise empty: which RT
    /// axis the RTs of the table are on, for an alignment the automatic RT correction moved onto a
    /// reference file's axis. Nothing else moves, so a reader that finds the "Class" cell or skips
    /// the header lines reads the table as before. Null, the default, writes nothing.
    /// </summary>
    public string? RetentionTimeAxisNote { get; set; }

    public void Export(
        Stream stream,
        IReadOnlyList<AlignmentSpotProperty> spots,
        IReadOnlyList<MSDecResult> msdecResults,
        IReadOnlyList<AnalysisFileBean> files,
        IFileClassMetaAccessor fileMetaAccessor,
        IMetadataAccessor metaAccessor,
        IQuantValueAccessor quantAccessor,
        IReadOnlyList<StatsValue> stats) {

        using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true);

        // Header
        var metaHeaders = metaAccessor.GetHeaders();
        var quantHeaders = quantAccessor.GetQuantHeaders(files);
        var classHeaders = quantAccessor.GetStatHeaders();
        WriteHeader(sw, files, fileMetaAccessor, metaHeaders, quantHeaders, classHeaders, stats);

        // Content
        foreach (var spot in spots) {
            WriteContent(sw, spot, msdecResults[spot.MasterAlignmentID], metaHeaders, quantHeaders, classHeaders, metaAccessor, quantAccessor, stats);

            foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>()) {
                WriteContent(sw, driftSpot, msdecResults[driftSpot.MasterAlignmentID], metaHeaders, quantHeaders, classHeaders, metaAccessor, quantAccessor, stats);
            }
        }
    }

    private void WriteHeader(
        StreamWriter sw,
        IReadOnlyList<AnalysisFileBean> files,
        IFileClassMetaAccessor fileMetaAccessor,
        IReadOnlyList<string> metaHeaders,
        IReadOnlyList<string> quantHeaders,
        IReadOnlyList<string> classHeaders,
        IReadOnlyList<StatsValue> stats) {

        var marginString = RepeatString("", metaHeaders.Count - 1, Separator);
        var naStrig = RepeatString("NA", classHeaders.Count * stats.Count, Separator);

        var header = fileMetaAccessor.GetHeaders();
        var contents = fileMetaAccessor.GetContents(files);

        for (int i = 0; i < header.Count; i++) {
            IEnumerable<string> statsFeilds = i == header.Count - 1
                ? stats.SelectMany(stat => Enumerable.Repeat(stat.ToString(), classHeaders.Count))
                : [naStrig];
            var margin = i == 0 && metaHeaders.Count > 1 && !string.IsNullOrEmpty(RetentionTimeAxisNote)
                ? RetentionTimeAxisNote!.Replace(Separator, " ") + marginString
                : marginString;
            sw.WriteLine(
                string.Join(Separator,
                [
                    margin,
                    header[i],
                    ..contents.Select(c => c[i]),
                    ..statsFeilds,
                ]).TrimEnd());
        }

        sw.WriteLine(
            JoinContents(
                metaHeaders,
                quantHeaders,
                stats.SelectMany(_ => classHeaders)
            ).TrimEnd());
    }

    private void WriteContent(
        StreamWriter sw,
        AlignmentSpotProperty spot,
        MSDecResult msdec,
        IReadOnlyList<string> metaHeaders,
        IReadOnlyList<string> quantHeaders,
        IReadOnlyList<string> classHeaders,
        IMetadataAccessor metaAccessor,
        IQuantValueAccessor quantAccessor,
        IReadOnlyList<StatsValue> stats) {
        var metadata = metaAccessor.GetContent(spot, msdec);
        var quantValues = quantAccessor.GetQuantValues(spot);
        var statValues = stats.Select(stat => quantAccessor.GetStatsValues(spot, stat))
            .SelectMany(dict => classHeaders.Where(dict.ContainsKey).Select(clss => dict[clss]));
        sw.WriteLine(
            JoinContents(
                metaHeaders.Select(header => metadata[header]),
                quantHeaders.Select(header => quantValues[header]),
                statValues));
    }

    private static string RepeatString(string rep, int numColumn, string separator) {
        return string.Join(separator, Enumerable.Repeat(rep, numColumn));
    }

    private string JoinContents(params IEnumerable<string>[] contentss) {
        var contents = contentss.SelectMany(cs => cs.Select(WrapField));
        return string.Join(Separator, contents);
    }

    private string WrapField(string field) {
        if (field.Contains(Separator)) {
            return $"\"{field}\"";
        }
        return field;
    }
}
