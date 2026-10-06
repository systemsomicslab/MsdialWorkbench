using CompMs.App.MsdialConsole.Process;
using CompMs.Common.DataObj.Result;
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
/// The alignment reads each spot's representative spectrum through <see cref="RepresentativeDeconvolutionReader"/>.
/// Before it, the Console opened <see cref="AnalysisFileBean.DeconvolutionFilePath"/> for every file, which an AIF file
/// with more than one collision energy never writes, so a multi-energy AIF alignment stopped with FileNotFoundException.
/// </summary>
[TestClass]
public sealed class RepresentativeDeconvolutionReaderTests
{
    private const int PeakCount = 3;

    private string _scratch = null!;

    [TestInitialize]
    public void Initialize() {
        _scratch = Path.Combine(Path.GetTempPath(), $"msdial-representative-dcl-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_scratch);
    }

    [TestCleanup]
    public void Cleanup() {
        if (Directory.Exists(_scratch)) {
            Directory.Delete(_scratch, recursive: true);
        }
    }

    [TestMethod]
    public void AMultiEnergyFileIsReadFromTheEnergyOfItsRepresentativeAnnotation() {
        var file = MultiEnergyFile(0, "aif", 10d, 20d, 40d);
        Assert.IsFalse(File.Exists(file.DeconvolutionFilePath), "a multi-energy run writes no unsuffixed .dcl");

        using var reader = new RepresentativeDeconvolutionReader([file]);

        Assert.AreEqual(Marker(20d, 1), reader.Read(Peak(file, 1, representativeCE: 20d)).PrecursorMz);
        Assert.AreEqual(Marker(40d, 2), reader.Read(Peak(file, 2, representativeCE: 40d)).PrecursorMz);
    }

    [TestMethod]
    public void WithoutAnEnergyMatchTheFirstListedEnergyIsRead() {
        // FileProcess lists the energies in ascending order; which energy should serve here is an open question
        // for the MS-DIAL author, and this test pins the current behaviour only.
        var file = MultiEnergyFile(0, "aif", 10d, 20d, 40d);

        using var reader = new RepresentativeDeconvolutionReader([file]);

        Assert.AreEqual(Marker(10d, 0), reader.Read(Peak(file, 0, representativeCE: null)).PrecursorMz, "unannotated");
        Assert.AreEqual(Marker(10d, 2), reader.Read(Peak(file, 2, representativeCE: 30d)).PrecursorMz, "no file at 30 eV");
    }

    [TestMethod]
    public void AStaleUnsuffixedFileIsNotReadWhenEnergyFilesAreListed() {
        var file = MultiEnergyFile(0, "aif", 10d, 20d);
        // Left by an earlier single-deconvolution run on the same raw folder, under the same name, with fewer peaks.
        MsdecResultsWriter.Write(file.DeconvolutionFilePath, new List<MSDecResult> { new() { ScanID = 0, PrecursorMz = -1d, } });

        using var reader = new RepresentativeDeconvolutionReader([file]);

        Assert.AreEqual(Marker(10d, 2), reader.Read(Peak(file, 2, representativeCE: null)).PrecursorMz);
        Assert.AreEqual(Marker(20d, 0), reader.Read(Peak(file, 0, representativeCE: 20d)).PrecursorMz);
    }

    [TestMethod]
    public void ASingleDeconvolutionFileIsReadWhenNoEnergyFilesAreListed() {
        var file = NewFile(3, "dda");
        MsdecResultsWriter.Write(file.DeconvolutionFilePath, Results(-1d));

        using var reader = new RepresentativeDeconvolutionReader([file]);

        Assert.AreEqual(Marker(-1d, 1), reader.Read(Peak(file, 1, representativeCE: 35d)).PrecursorMz);
    }

    [TestMethod]
    public void FilesAreKeyedByAnalysisFileIdNotByPosition() {
        var single = NewFile(7, "dda");
        MsdecResultsWriter.Write(single.DeconvolutionFilePath, Results(-1d));
        var multi = MultiEnergyFile(2, "aif", 15d, 30d);

        using var reader = new RepresentativeDeconvolutionReader([single, multi]);

        Assert.AreEqual(Marker(30d, 1), reader.Read(Peak(multi, 1, representativeCE: 30d)).PrecursorMz);
        Assert.AreEqual(Marker(-1d, 2), reader.Read(Peak(single, 2, representativeCE: null)).PrecursorMz);
    }

    [TestMethod]
    public void AFileWithNoDeconvolutionResultIsReportedByName() {
        var present = NewFile(0, "present");
        MsdecResultsWriter.Write(present.DeconvolutionFilePath, Results(-1d));
        var missing = NewFile(1, "missing");

        var ex = Assert.ThrowsException<FileNotFoundException>(() => new RepresentativeDeconvolutionReader([present, missing]));

        StringAssert.Contains(ex.Message, "missing");
        Assert.AreEqual(missing.DeconvolutionFilePath, ex.FileName);
    }

    private AnalysisFileBean NewFile(int id, string name) {
        return new AnalysisFileBean {
            AnalysisFileId = id,
            AnalysisFileName = name,
            AnalysisFilePath = Path.Combine(_scratch, name + ".mzML"),
            DeconvolutionFilePath = Path.Combine(_scratch, name + "_202610711.dcl"),
        };
    }

    // Writes the per-energy files the way FileProcess does for an AIF file with more than one energy,
    // and lists them in ascending energy.
    private AnalysisFileBean MultiEnergyFile(int id, string name, params double[] energies) {
        var file = NewFile(id, name);
        foreach (var energy in energies.OrderBy(e => e)) {
            var collection = new MSDecResultCollection(Results(energy), energy);
            var path = collection.GetDeconvolutionFilePathWithCE(file);
            MsdecResultsWriter.Write(path, collection.MSDecResults);
            file.DeconvolutionFilePathList.Add(path);
        }
        return file;
    }

    private static List<MSDecResult> Results(double energy) {
        return Enumerable.Range(0, PeakCount)
            .Select(i => new MSDecResult { ScanID = i, PrecursorMz = Marker(energy, i), })
            .ToList();
    }

    // Which file and which peak a spectrum came from.
    private static double Marker(double energy, int peak) => energy * 1000d + peak;

    private static AlignmentChromPeakFeature Peak(AnalysisFileBean file, int masterPeakId, double? representativeCE) {
        var peak = new AlignmentChromPeakFeature {
            FileID = file.AnalysisFileId,
            FileName = file.AnalysisFileName,
            MasterPeakID = masterPeakId,
        };
        if (representativeCE is double ce) {
            peak.MatchResults.AddResult(new MsScanMatchResult {
                Name = "annotated",
                Source = SourceType.MspDB,
                TotalScore = 0.9f,
                CollisionEnergy = ce,
            });
        }
        return peak;
    }
}
