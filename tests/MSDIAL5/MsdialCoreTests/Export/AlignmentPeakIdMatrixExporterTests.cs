using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;

namespace CompMs.MsdialCoreTests.Export;

[TestClass]
public class AlignmentPeakIdMatrixExporterTests
{
    [TestMethod]
    public void ExportWritesMasterPeakIdsInAnalysisFileOrder()
    {
        var files = new[] {
            new AnalysisFileBean { AnalysisFileId = 4, AnalysisFileName = "sample-b" },
            new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "sample-a" },
            new AnalysisFileBean { AnalysisFileId = 9, AnalysisFileName = "sample-c" },
        };
        var spot = new AlignmentSpotProperty {
            MasterAlignmentID = 12,
            AlignedPeakProperties = [
                new AlignmentChromPeakFeature { FileID = 4, MasterPeakID = 42 },
                new AlignmentChromPeakFeature { FileID = 1, MasterPeakID = 7 },
                new AlignmentChromPeakFeature { FileID = 9, MasterPeakID = -2 },
            ],
        };

        using var stream = new MemoryStream();
        new AlignmentPeakIdMatrixExporter().Export(stream, [spot], files);
        var text = Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n");

        Assert.AreEqual(
            "alignment_master_id\tsample-a\tsample-b\tsample-c\n12\t7\t42\t-1\n",
            text);
    }

    [TestMethod]
    public void ExportReadsAlignmentLightFileBackedRows()
    {
        var files = new[] {
            new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "sample-a" },
            new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "sample-b" },
        };
        using var store = AlignmentLightPeakStore.CreateTemp();
        store.Initialize(1, files);
        store.WriteSpotPeak(0, new AlignmentChromPeakFeature {
            FileID = 1,
            FileName = "sample-b",
            MasterPeakID = 9,
            PeakID = 9,
        });
        var spot = new AlignmentSpotProperty { MasterAlignmentID = 0 };

        using var stream = new MemoryStream();
        new AlignmentPeakIdMatrixExporter().Export(stream, [spot], files, store);
        var text = Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n");

        Assert.AreEqual(
            "alignment_master_id\tsample-a\tsample-b\n0\t-1\t9\n",
            text);
    }
}
