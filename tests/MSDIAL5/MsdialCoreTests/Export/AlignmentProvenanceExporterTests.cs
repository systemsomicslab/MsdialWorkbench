using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialCoreTests.Export;

[TestClass]
public class AlignmentProvenanceExporterTests
{
    [TestMethod]
    public void ExportWritesStableFeatureLinksAndRawSpectrumIds()
    {
        var spot = new AlignmentSpotProperty {
            MasterAlignmentID = 12,
            AlignmentID = 10,
            ParentAlignmentID = -1,
            RepresentativeFileID = 3,
            AlignedPeakProperties = [
                new AlignmentChromPeakFeature {
                    FileID = 3,
                    FileName = "sample-a",
                    MasterPeakID = 42,
                    PeakID = 40,
                    ParentPeakID = -1,
                    MS1RawSpectrumID = 100,
                    MS1RawSpectrumIdTop = 101,
                    MS2RawSpectrumID = 202,
                    MS2RawSpectrumID2CE = new Dictionary<int, double> { [202] = 20d, [203] = 40d },
                    ChromXsTop = new ChromXs(2.5),
                    PeakHeightTop = 1234d,
                    PeakAreaAboveZero = 5678d,
                    PeakAreaAboveBaseline = 4321d,
                },
            ],
        };

        using var stream = new MemoryStream();
        new AlignmentProvenanceExporter().Export(stream, [spot]);
        var text = Encoding.UTF8.GetString(stream.ToArray());

        StringAssert.Contains(text, "alignment_master_id\talignment_local_id");
        StringAssert.Contains(text, "12\t10\t-1\t3\tsample-a\ttrue\ttrue\t42\t40\t-1\t100\t101\t202\t202;203\t202:20;203:40");
    }

    [TestMethod]
    public void ExportReadsAlignmentLightFileBackedRows()
    {
        var file = new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "sample-light" };
        using var store = AlignmentLightPeakStore.CreateTemp();
        store.Initialize(1, [file]);
        store.WriteSpotPeak(0, new AlignmentChromPeakFeature {
            FileID = 0,
            FileName = "sample-light",
            MasterPeakID = 9,
            PeakID = 9,
            MS1RawSpectrumIdTop = 50,
            MS2RawSpectrumID = 51,
            MS2RawSpectrumID2CE = new Dictionary<int, double> { [51] = 20d },
            ChromXsTop = new ChromXs(1.5),
            Mass = 250.25,
            PeakHeightTop = 500d,
            PeakAreaAboveZero = 1500d,
        });
        var spot = new AlignmentSpotProperty {
            MasterAlignmentID = 0,
            AlignmentID = 0,
            ParentAlignmentID = -1,
            RepresentativeFileID = 0,
        };

        using var stream = new MemoryStream();
        new AlignmentProvenanceExporter().Export(stream, [spot], store);
        var text = Encoding.UTF8.GetString(stream.ToArray());

        StringAssert.Contains(text, "0\t0\t-1\t0\tsample-light\ttrue\ttrue\t9\t9");
        StringAssert.Contains(text, "\t50\t51\t51\t\t1.5\t250.25\t500\t1500\t");
    }
}
