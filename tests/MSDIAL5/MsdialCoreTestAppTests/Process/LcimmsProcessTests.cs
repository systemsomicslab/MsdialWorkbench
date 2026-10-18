using CompMs.App.MsdialConsole.Process;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The LC-IM-MS alignment export indexes the deconvolution results by MasterAlignmentID for the drift spots as
/// well as the RT spots. The Console loaded one result per RT spot only, so AlignmentCSVExporter.Export threw
/// ArgumentOutOfRangeException on the first drift spot.
/// </summary>
[TestClass]
public sealed class LcimmsProcessTests
{
    private string _scratch = null!;

    [TestInitialize]
    public void Initialize() {
        _scratch = Path.Combine(Path.GetTempPath(), $"msdial-lcimms-dcl-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_scratch);
    }

    [TestCleanup]
    public void Cleanup() {
        if (Directory.Exists(_scratch)) {
            Directory.Delete(_scratch, recursive: true);
        }
    }

    [TestMethod]
    public void EveryRtAndDriftSpotHasItsResultAtItsMasterAlignmentId() {
        var file0 = NewFile(0, "file0", peakCount: 6);
        var file1 = NewFile(1, "file1", peakCount: 6);
        // Numbered as LcimmsAlignmentRefiner numbers them: each RT spot, then its drift spots.
        var spots = new List<AlignmentSpotProperty>
        {
            Spot(0, representative: 0, masterPeakId: 0, Spot(1, representative: 1, masterPeakId: 1), Spot(2, representative: 0, masterPeakId: 2)),
            Spot(3, representative: 1, masterPeakId: 3),
            Spot(4, representative: 1, masterPeakId: 4, Spot(5, representative: 0, masterPeakId: 5)),
        };

        var results = LcimmsProcess.LoadRepresentativeDeconvolutions([file0, file1], spots).ToList();

        Assert.AreEqual(6, results.Count);
        foreach (var spot in spots.SelectMany(s => s.AlignmentDriftSpotFeatures.Prepend(s))) {
            var peak = spot.AlignedPeakProperties[spot.RepresentativeFileID];
            Assert.AreEqual(Marker(peak.FileID, peak.MasterPeakID), results[spot.MasterAlignmentID].PrecursorMz, $"spot {spot.MasterAlignmentID}");
        }
    }

    private AnalysisFileBean NewFile(int id, string name, int peakCount) {
        var file = new AnalysisFileBean {
            AnalysisFileId = id,
            AnalysisFileName = name,
            AnalysisFilePath = Path.Combine(_scratch, name + ".d"),
            DeconvolutionFilePath = Path.Combine(_scratch, name + "_202610181.dcl"),
        };
        var results = Enumerable.Range(0, peakCount).Select(i => new MSDecResult { ScanID = i, PrecursorMz = Marker(id, i), }).ToList();
        MsdecResultsWriter.Write(file.DeconvolutionFilePath, results);
        return file;
    }

    private static AlignmentSpotProperty Spot(int masterAlignmentId, int representative, int masterPeakId, params AlignmentSpotProperty[] driftSpots) {
        return new AlignmentSpotProperty {
            MasterAlignmentID = masterAlignmentId,
            RepresentativeFileID = representative,
            AlignedPeakProperties = Enumerable.Range(0, 2)
                .Select(fileId => new AlignmentChromPeakFeature { FileID = fileId, MasterPeakID = fileId == representative ? masterPeakId : -1, })
                .ToList(),
            AlignmentDriftSpotFeatures = driftSpots.ToList(),
        };
    }

    // Which file and which peak a result came from.
    private static double Marker(int fileId, int peak) => fileId * 1000d + peak;
}
