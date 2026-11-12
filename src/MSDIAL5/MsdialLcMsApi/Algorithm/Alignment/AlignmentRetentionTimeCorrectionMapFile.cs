using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

/// <summary>
/// The maps an automatic alignment RT correction aligned with, kept beside the alignment result so
/// that whoever opens the result later can move any RT between a file's measured axis and the
/// reference file's axis exactly as the alignment did.
/// </summary>
/// <remarks>
/// <para>
/// A tab-separated text file, <c>&lt;alignment file name&gt;.rtcorrection.tsv</c> in the folder of the
/// alignment result. Lines starting with '#' are the header: the format version and the reference
/// file. Then one row per control point of a piecewise-linear model and one row per blend of two
/// models (a Blank interpolated between its neighbours), each with its file, its node in that
/// file's model and the node it belongs to. A file the correction left uncorrected has no row.
/// </para>
/// <para>
/// Every number is written with "G17", which reads back to the same double, and the models are
/// rebuilt through their own constructors, so a model read back corrects and restores every RT to
/// the same double as the one written, and one whose points are not monotonic is refused.
/// </para>
/// </remarks>
public static class AlignmentRetentionTimeCorrectionMapFile {
    public const string FileSuffix = ".rtcorrection.tsv";
    public const int FormatVersion = 1;

    private const string Title = "# MS-DIAL automatic alignment RT correction map";
    private const string VersionKey = "# Format version";
    private const string ReferenceKey = "# Reference file ID";
    private const string ReferenceNameKey = "# Reference file name";
    private const string AxisKey = "# Axes";
    private const string Piecewise = "Piecewise";
    private const string Blended = "Blended";
    private const string LeftSide = "Left";
    private const string RightSide = "Right";

    private static readonly string[] Headers = [
        "File ID", "File name", "Node", "Parent node", "Side", "Model", "Right weight",
        "Original RT (min)", "Corrected RT (min)", "Anchor ID", "m/z", "Quality score",
    ];

    /// <summary>The map file of an alignment result: beside it, named after it.</summary>
    public static string PathFor(AlignmentFileBean alignmentFile) {
        if (alignmentFile is null) throw new ArgumentNullException(nameof(alignmentFile));
        var folder = Path.GetDirectoryName(alignmentFile.FilePath) ?? string.Empty;
        return Path.Combine(folder, alignmentFile.FileName + FileSuffix);
    }

    public static void Write(string path, AlignmentRetentionTimeCorrectionCollection correction, IReadOnlyList<AnalysisFileBean> files) {
        if (correction is null) throw new ArgumentNullException(nameof(correction));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) {
            Directory.CreateDirectory(directory);
        }
        var temporary = path + ".tmp";
        using (var stream = File.Open(temporary, FileMode.Create, FileAccess.Write, FileShare.None)) {
            Write(stream, correction, files);
        }
        if (File.Exists(path)) {
            File.Delete(path);
        }
        File.Move(temporary, path);
    }

    public static void Write(Stream stream, AlignmentRetentionTimeCorrectionCollection correction, IReadOnlyList<AnalysisFileBean> files) {
        if (correction is null) throw new ArgumentNullException(nameof(correction));
        var names = (files ?? Array.Empty<AnalysisFileBean>()).ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileName);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true) { NewLine = "\n" };
        writer.WriteLine(Title);
        writer.WriteLine($"{VersionKey}\t{FormatVersion.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"{ReferenceKey}\t{correction.ReferenceFileId.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"{ReferenceNameKey}\t{Clean(names.TryGetValue(correction.ReferenceFileId, out var referenceName) ? referenceName : string.Empty)}");
        writer.WriteLine($"{AxisKey}\tOriginal RT is each file's measured RT; Corrected RT is the reference file's RT, on which the alignment was made");
        writer.WriteLine(string.Join("\t", Headers));
        foreach (var pair in correction.Models.OrderBy(pair => pair.Key)) {
            var fileName = Clean(names.TryGetValue(pair.Key, out var name) ? name : string.Empty);
            var next = 0;
            WriteNode(writer, pair.Key, fileName, pair.Value, -1, string.Empty, ref next);
        }
    }

    private static void WriteNode(StreamWriter writer, int fileId, string fileName, IAlignmentRetentionTimeCorrectionModel model, int parent, string side, ref int next) {
        var node = next++;
        switch (model) {
            case PiecewiseLinearAlignmentRetentionTimeCorrectionModel piecewise:
                foreach (var point in piecewise.ControlPoints) {
                    WriteRow(writer, fileId, fileName, node, parent, side, Piecewise, string.Empty,
                        Number(point.OriginalRt), Number(point.CorrectedRt), point.AnchorId.ToString(CultureInfo.InvariantCulture), Number(point.Mass), Number(point.QualityScore));
                }
                break;
            case BlendedAlignmentRetentionTimeCorrectionModel blended:
                WriteRow(writer, fileId, fileName, node, parent, side, Blended, Number(blended.RightWeight), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                WriteNode(writer, fileId, fileName, blended.Left, node, LeftSide, ref next);
                WriteNode(writer, fileId, fileName, blended.Right, node, RightSide, ref next);
                break;
            default:
                throw new NotSupportedException($"The RT correction model of file ID {fileId} ({model?.GetType().Name ?? "null"}) cannot be written to the map file.");
        }
    }

    private static void WriteRow(StreamWriter writer, int fileId, string fileName, int node, int parent, string side, string kind, string weight, string original, string corrected, string anchorId, string mass, string quality) {
        writer.WriteLine(string.Join("\t", [
            fileId.ToString(CultureInfo.InvariantCulture), fileName, node.ToString(CultureInfo.InvariantCulture),
            parent.ToString(CultureInfo.InvariantCulture), side, kind, weight, original, corrected, anchorId, mass, quality,
        ]));
    }

    /// <summary>Reads a map file; throws <see cref="InvalidDataException"/> when it cannot be used.</summary>
    public static AlignmentRetentionTimeCorrectionCollection Read(string path) {
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    public static AlignmentRetentionTimeCorrectionCollection Read(Stream stream) {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        var referenceFileId = (int?)null;
        var version = (int?)null;
        string[]? header = null;
        var rows = new List<string[]>();
        string? line;
        var lineNumber = 0;
        while ((line = reader.ReadLine()) is not null) {
            lineNumber++;
            if (line.Length == 0) {
                continue;
            }
            if (line.StartsWith("#", StringComparison.Ordinal)) {
                var cells = line.Split('\t');
                if (cells.Length >= 2 && cells[0] == VersionKey) {
                    version = ParseInt(cells[1], lineNumber);
                }
                else if (cells.Length >= 2 && cells[0] == ReferenceKey) {
                    referenceFileId = ParseInt(cells[1], lineNumber);
                }
                continue;
            }
            if (header is null) {
                header = line.Split('\t');
                if (!header.Take(Headers.Length).SequenceEqual(Headers)) {
                    throw new InvalidDataException("The RT correction map's column header is not the one MS-DIAL writes.");
                }
                continue;
            }
            var row = line.Split('\t');
            if (row.Length < Headers.Length) {
                throw new InvalidDataException($"Line {lineNumber} of the RT correction map has {row.Length} fields, not {Headers.Length}.");
            }
            rows.Add(row);
        }
        if (version is null || header is null || referenceFileId is null) {
            throw new InvalidDataException("The file is not an MS-DIAL RT correction map: its version, reference file or column header is missing.");
        }
        if (version.Value > FormatVersion) {
            throw new InvalidDataException($"The RT correction map has format version {version.Value}; this MS-DIAL reads version {FormatVersion} and earlier.");
        }

        var models = new Dictionary<int, IAlignmentRetentionTimeCorrectionModel>();
        foreach (var file in rows.GroupBy(row => ParseInt(row[0], 0))) {
            var nodes = new Dictionary<int, NodeRows>();
            foreach (var row in file) {
                var node = ParseInt(row[2], 0);
                if (!nodes.TryGetValue(node, out var rowsOfNode)) {
                    rowsOfNode = new NodeRows(ParseInt(row[3], 0), row[4], row[5]);
                    nodes.Add(node, rowsOfNode);
                }
                else if (rowsOfNode.Kind != row[5] || rowsOfNode.Parent != ParseInt(row[3], 0)) {
                    throw new InvalidDataException($"Node {node} of file ID {file.Key} in the RT correction map is written as two different models.");
                }
                rowsOfNode.Rows.Add(row);
            }
            var roots = nodes.Where(pair => pair.Value.Parent < 0).Select(pair => pair.Key).ToList();
            if (roots.Count != 1) {
                throw new InvalidDataException($"File ID {file.Key} in the RT correction map has {roots.Count} root models, not one.");
            }
            try {
                models[file.Key] = BuildNode(roots[0], nodes, new HashSet<int>());
            }
            catch (ArgumentException ex) {
                throw new InvalidDataException($"The RT correction map's model of file ID {file.Key} cannot be used: {ex.Message}", ex);
            }
        }
        return new AlignmentRetentionTimeCorrectionCollection(referenceFileId.Value, models);
    }

    private static IAlignmentRetentionTimeCorrectionModel BuildNode(int node, IReadOnlyDictionary<int, NodeRows> nodes, HashSet<int> visiting) {
        if (!visiting.Add(node)) {
            throw new InvalidDataException("The RT correction map's models refer to each other in a loop.");
        }
        var rows = nodes[node];
        switch (rows.Kind) {
            case Piecewise:
                return new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(rows.Rows.Select(row => new AlignmentRetentionTimeCorrectionControlPoint(
                    ParseInt(row[9], 0), ParseDouble(row[10]), ParseDouble(row[7]), ParseDouble(row[8]), ParseDouble(row[11]))));
            case Blended:
                var children = nodes.Where(pair => pair.Value.Parent == node).ToList();
                var left = children.Where(pair => pair.Value.Side == LeftSide).Select(pair => pair.Key).ToList();
                var right = children.Where(pair => pair.Value.Side == RightSide).Select(pair => pair.Key).ToList();
                if (left.Count != 1 || right.Count != 1 || children.Count != 2) {
                    throw new InvalidDataException("A blended RT correction model in the map does not have exactly one left and one right model.");
                }
                return new BlendedAlignmentRetentionTimeCorrectionModel(
                    BuildNode(left[0], nodes, visiting),
                    BuildNode(right[0], nodes, visiting),
                    ParseDouble(rows.Rows[0][6]));
            default:
                throw new InvalidDataException($"The RT correction map names an unknown model '{rows.Kind}'.");
        }
    }

    private sealed class NodeRows {
        public NodeRows(int parent, string side, string kind) {
            Parent = parent;
            Side = side;
            Kind = kind;
        }

        public int Parent { get; }
        public string Side { get; }
        public string Kind { get; }
        public List<string[]> Rows { get; } = new();
    }

    private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string Clean(string value) => (value ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static int ParseInt(string value, int lineNumber) {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) {
            throw new InvalidDataException(lineNumber > 0
                ? $"Line {lineNumber} of the RT correction map has '{value}' where a whole number belongs."
                : $"The RT correction map has '{value}' where a whole number belongs.");
        }
        return result;
    }

    private static double ParseDouble(string value) {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.IsNaN(result) || double.IsInfinity(result)) {
            throw new InvalidDataException($"The RT correction map has '{value}' where a number belongs.");
        }
        return result;
    }
}
