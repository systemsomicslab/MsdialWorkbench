using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export.Tests;

/// <summary>
/// When the automatic alignment RT correction moved the aligned RTs onto a reference file's axis,
/// the alignment table says so in its first header cell and the provenance sidecar appends each
/// member's measured RT; without it both are written as before.
/// </summary>
[TestClass]
public class RetentionTimeAxisExportTests
{
    private static List<AlignmentSpotProperty> Spots() => new()
    {
        new AlignmentSpotProperty
        {
            MasterAlignmentID = 0,
            Name = "Metabolite 1",
            AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                new AlignmentChromPeakFeature { FileName = "File 1", PeakHeightTop = 400, },
                new AlignmentChromPeakFeature { FileName = "File 2", PeakHeightTop = 200, },
            },
        },
    };

    private static List<AnalysisFileBean> Files() => new()
    {
        new AnalysisFileBean { AnalysisFileName = "File 1", AnalysisFileClass = "A", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 1, AnalysisBatch = 1, },
        new AnalysisFileBean { AnalysisFileName = "File 2", AnalysisFileClass = "B", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 2, AnalysisBatch = 1, },
    };

    private static string ExportTable(string? note) {
        var exporter = new AlignmentCSVExporter("\t") { RetentionTimeAxisNote = note };
        using var memory = new MemoryStream();
        exporter.Export(memory, Spots(), new List<MSDecResult> { null! }, Files(), new MulticlassFileMetaAccessor(0), new Meta(), new Quant(), new[] { StatsValue.Average, });
        return Encoding.ASCII.GetString(memory.ToArray());
    }

    [TestMethod]
    public void TheNoteTakesTheFirstEmptyHeaderCellAndNothingElseMoves() {
        var without = ExportTable(null).Split('\n');
        var with = ExportTable("RT axis: aligned RTs are on the RT axis of reference file File 1").Split('\n');

        Assert.AreEqual("\tClass\tA\tB\tNA\tNA", without[0].TrimEnd('\r'));
        Assert.AreEqual("RT axis: aligned RTs are on the RT axis of reference file File 1\tClass\tA\tB\tNA\tNA", with[0].TrimEnd('\r'));
        Assert.AreEqual(without[0].Split('\t').Length, with[0].Split('\t').Length, "the same number of cells");
        CollectionAssert.AreEqual(without.Skip(1).ToArray(), with.Skip(1).ToArray());
    }

    [TestMethod]
    public void TheNoteCannotSplitACell() {
        var with = ExportTable("a\tb").Split('\n');
        Assert.AreEqual("a b\tClass\tA\tB\tNA\tNA", with[0].TrimEnd('\r'));
    }

    private static string[] Provenance(Func<int, double, double?>? originalRt) {
        var spot = new AlignmentSpotProperty
        {
            MasterAlignmentID = 3,
            AlignmentID = 3,
            ParentAlignmentID = -1,
            RepresentativeFileID = 0,
            AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                new AlignmentChromPeakFeature { FileID = 0, FileName = "ref", MasterPeakID = 5, PeakID = 5, ChromXsTop = new ChromXs(8.6), Mass = 200d },
                new AlignmentChromPeakFeature { FileID = 1, FileName = "f", MasterPeakID = 7, PeakID = 7, ChromXsTop = new ChromXs(8.6), Mass = 200d },
                new AlignmentChromPeakFeature { FileID = 2, FileName = "g", MasterPeakID = -1, PeakID = -1, ChromXsTop = new ChromXs(-1d), Mass = 200d },
            },
        };
        using var stream = new MemoryStream();
        (originalRt is null ? new AlignmentProvenanceExporter() : new AlignmentProvenanceExporter(originalRt)).Export(stream, new[] { spot });
        return Encoding.UTF8.GetString(stream.ToArray()).Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Length > 0).ToArray();
    }

    [TestMethod]
    public void ProvenanceAppendsTheMeasuredRtOnlyWhenTheAlignmentCorrectedRts() {
        var plain = Provenance(null);
        var corrected = Provenance((fileId, rt) => fileId == 1 ? rt - 0.3d : rt);

        Assert.IsFalse(plain[0].Contains(AlignmentProvenanceExporter.OriginalRtHeader));
        Assert.AreEqual(plain[0] + "\t" + AlignmentProvenanceExporter.OriginalRtHeader, corrected[0], "appended, nothing renamed");
        Assert.AreEqual(plain[1] + "\t" + plain[1].Split('\t')[16], corrected[1], "the reference file's measured RT is its aligned RT");
        Assert.IsTrue(corrected[2].StartsWith(plain[2] + "\t8."), corrected[2]);
        Assert.AreEqual(8.3d, double.Parse(corrected[2].Split('\t').Last(), System.Globalization.CultureInfo.InvariantCulture), 1e-12);
        Assert.AreEqual(plain[3] + "\t", corrected[3], "no RT, no measured RT");
        Assert.IsTrue(corrected.All(line => line.Split('\t').Length == corrected[0].Split('\t').Length));
    }

    private sealed class Meta : IMetadataAccessor
    {
        public string[] GetHeaders() => new[] { "ID", "Name", };
        public IReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, IMSScanProperty? msdec)
            => new Dictionary<string, string> { ["ID"] = spot.MasterAlignmentID.ToString(), ["Name"] = spot.Name, };
    }

    private sealed class Quant : IQuantValueAccessor
    {
        public List<string> GetQuantHeaders(IReadOnlyList<AnalysisFileBean> files) => files.Select(file => file.AnalysisFileName).ToList();
        public Dictionary<string, string> GetQuantValues(AlignmentSpotProperty spot) => spot.AlignedPeakProperties.ToDictionary(peak => peak.FileName, peak => peak.PeakHeightTop.ToString());
        public List<string> GetStatHeaders() => new List<string> { "A", "B" };
        public Dictionary<string, string> GetStatsValues(AlignmentSpotProperty spot, StatsValue stat) => new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" };
    }
}
