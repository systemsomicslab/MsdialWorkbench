using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export.Tests;

[TestClass]
public class AlignmentLongCSVExporterTests
{
    [TestMethod]
    public void ExportFileMetaEscapesDelimiterAndQuotes()
    {
        var exporter = new AlignmentLongCSVExporter(",");
        var files = new[]
        {
            new AnalysisFileBean
            {
                AnalysisFileName = "sample,\"1\"",
                AnalysisFileClass = "class",
                AnalysisFileType = AnalysisFileType.Sample,
            },
        };
        using var stream = new MemoryStream();

        exporter.ExportFileMeta(stream, files, new FakeFileMetaAccessor());

        Assert.AreEqual("Name,Class\r\n\"sample,\"\"1\"\"\",class\r\n", Encoding.ASCII.GetString(stream.ToArray()));
    }

    [TestMethod]
    public void ExportValueUsesFileIdWhenAlignmentPeakHasOldFileName()
    {
        var exporter = new AlignmentLongCSVExporter();
        var file = new AnalysisFileBean
        {
            AnalysisFileId = 7,
            AnalysisFileName = "renamed.raw",
            AnalysisFileClass = "class",
        };
        var spot = new AlignmentSpotProperty
        {
            MasterAlignmentID = 1,
            AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                new AlignmentChromPeakFeature
                {
                    FileID = 7,
                    FileName = "original.raw",
                },
            },
        };
        using var stream = new MemoryStream();

        exporter.ExportValue(stream, new[] { spot }, new[] { file }, ("Height", new FakeQuantValueAccessor()));

        Assert.AreEqual("ID\tFile\tClass\tHeight\r\n1\trenamed.raw\tclass\tvalue\r\n", Encoding.ASCII.GetString(stream.ToArray()));
    }

    [TestMethod]
    public void ExportValueWithFileMetadataIncludesRunOrderAndBatch()
    {
        var exporter = new AlignmentLongCSVExporter();
        var file = new AnalysisFileBean
        {
            AnalysisFileId = 3,
            AnalysisFileName = "qc.raw",
            AnalysisFileClass = "QC",
            AnalysisFileType = AnalysisFileType.QC,
            AnalysisFileAnalyticalOrder = 17,
            AnalysisBatch = 2,
        };
        var spot = new AlignmentSpotProperty
        {
            MasterAlignmentID = 9,
            AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                new AlignmentChromPeakFeature { FileID = 3, FileName = "qc.raw" },
            },
        };
        using var stream = new MemoryStream();

        exporter.ExportValueWithFileMetadata(
            stream,
            new[] { spot },
            new[] { file },
            new MulticlassFileMetaAccessor(0),
            ("Height", new FakeQuantValueAccessor()));

        Assert.AreEqual(
            "ID\tFile\tClass\tFile type\tInjection order\tBatch ID\tHeight\r\n" +
            "9\tqc.raw\tQC\tQC\t17\t2\tvalue\r\n",
            Encoding.ASCII.GetString(stream.ToArray()));
    }

    /// <summary>
    /// The QA matrix as the Console writes it: a file whose peak an in-silico match annotates is
    /// "In-silico matched" and not "Reference matched" (the author's decision of 2026-10-28); a file
    /// whose peak an acquired spectrum annotates stays "Reference matched".
    /// </summary>
    [TestMethod]
    public void QaMatrixMarksAnInSilicoPeakInSilicoMatchedAndNotReferenceMatched()
    {
        var exporter = new AlignmentLongCSVExporter();
        var files = new[]
        {
            new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "a", AnalysisFileClass = "S", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 1, AnalysisBatch = 1 },
            new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "b", AnalysisFileClass = "S", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 2, AnalysisBatch = 1 },
        };
        var spot = new AlignmentSpotProperty
        {
            MasterAlignmentID = 4,
            AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                Annotated(0, "a", AnnotationEvidenceSource.ReferenceSpectrum),
                Annotated(1, "b", AnnotationEvidenceSource.BySpectrumPredictionTool),
            },
        };
        var parameter = new ParameterBase();
        using var stream = new MemoryStream();

        exporter.ExportValueWithFileMetadata(
            stream,
            new[] { spot },
            files,
            new MulticlassFileMetaAccessor(0),
            ("Reference matched", new LegacyQuantValueAccessor("Reference matched", parameter)),
            ("In-silico matched", new LegacyQuantValueAccessor("In-silico matched", parameter)));

        Assert.AreEqual(
            "ID\tFile\tClass\tFile type\tInjection order\tBatch ID\tReference matched\tIn-silico matched\r\n" +
            "4\ta\tS\tSample\t1\t1\tTRUE\tFALSE\r\n" +
            "4\tb\tS\tSample\t2\t1\tFALSE\tTRUE\r\n",
            Encoding.ASCII.GetString(stream.ToArray()));
    }

    private static AlignmentChromPeakFeature Annotated(int fileId, string fileName, AnnotationEvidenceSource evidence)
    {
        var peak = new AlignmentChromPeakFeature { FileID = fileId, FileName = fileName, Name = "Compound" };
        peak.MatchResults.AddResult(new MsScanMatchResult
        {
            Name = "Compound",
            Source = SourceType.MspDB,
            EvidenceSource = evidence,
            IsReferenceMatched = true,
            IsSpectrumMatch = true,
            TotalScore = 0.8f,
        });
        return peak;
    }

    private sealed class FakeFileMetaAccessor : IFileClassMetaAccessor
    {
        public IReadOnlyList<string> GetHeaders() => new[] { "Name", "Class" };

        public string[] GetContent(AnalysisFileBean file) => new[] { file.AnalysisFileName, file.AnalysisFileClass };

        public string[][] GetContents(IEnumerable<AnalysisFileBean> files)
            => files.Select(GetContent).ToArray();
    }

    private sealed class FakeQuantValueAccessor : IQuantValueAccessor
    {
        public List<string> GetQuantHeaders(IReadOnlyList<AnalysisFileBean> files) => new();

        public List<string> GetStatHeaders() => new();

        public Dictionary<string, string> GetQuantValues(AlignmentSpotProperty spot)
            => spot.AlignedPeakProperties.ToDictionary(peak => peak.FileName, _ => "value");

        public Dictionary<string, string> GetStatsValues(AlignmentSpotProperty spot, StatsValue stat) => new();
    }
}
