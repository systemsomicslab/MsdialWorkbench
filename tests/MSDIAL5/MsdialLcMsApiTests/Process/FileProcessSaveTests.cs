using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialLcMsApi.Process.Tests;

/// <summary>
/// An AIF file deconvoluted at more than one collision energy is saved as one "&lt;name&gt;_&lt;CE x 100&gt;.dcl"
/// per energy. The files are written concurrently, so the order in which they were appended to
/// <see cref="AnalysisFileBean.DeconvolutionFilePathList"/> was the order they finished in. MSDecLoader and the
/// Console's alignment fall back to the first entry, so the saved list has to have one fixed order.
/// </summary>
/// <remarks>
/// Ascending energy is the order the PR keeps; whether the lowest energy should be the fallback is a question
/// for the MS-DIAL author, and these tests pin only that the order does not depend on completion.
/// </remarks>
[TestClass]
public sealed class FileProcessSaveTests
{
    private string _scratch = null!;

    [TestInitialize]
    public void Initialize() {
        _scratch = Path.Combine(Path.GetTempPath(), $"msdial-lcms-save-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_scratch);
    }

    [TestCleanup]
    public void Cleanup() {
        if (Directory.Exists(_scratch)) {
            Directory.Delete(_scratch, recursive: true);
        }
    }

    [TestMethod]
    public async Task MultiEnergyFilesAreListedInAscendingEnergyWhateverOrderTheyFinishIn() {
        double[] given = [40d, 10d, 34.5d, 20d];
        for (var trial = 0; trial < 20; trial++) {
            var file = NewFile($"aif{trial}");
            // Left from an earlier pass over the same file; the save replaces the list.
            file.DeconvolutionFilePathList.Add(Path.Combine(_scratch, $"aif{trial}_stale_9900.dcl"));
            var collections = given.Select(ce => new MSDecResultCollection(Results(), ce)).ToArray();

            await FileProcess.SaveToFileAsync(file, new ChromatogramPeakFeatureCollection([]), collections);

            var expected = given.OrderBy(ce => ce)
                .Select(ce => collections.Single(c => c.CollisionEnergy == ce).GetDeconvolutionFilePathWithCE(file))
                .ToArray();
            CollectionAssert.AreEqual(expected, file.DeconvolutionFilePathList, $"trial {trial}");
            CollectionAssert.AreEqual(
                new[] { "_1000.dcl", "_2000.dcl", "_3450.dcl", "_4000.dcl" },
                file.DeconvolutionFilePathList.Select(p => p.Substring(p.LastIndexOf('_'))).ToArray());
            Assert.IsTrue(file.DeconvolutionFilePathList.All(File.Exists));
            Assert.IsFalse(File.Exists(file.DeconvolutionFilePath), "a multi-energy save writes no unsuffixed .dcl");
        }
    }

    [TestMethod]
    public async Task ASingleDeconvolutionIsSavedUnsuffixedAndLeavesTheListAlone() {
        var file = NewFile("single");
        var collection = new MSDecResultCollection(Results(), 35d);

        await FileProcess.SaveToFileAsync(file, new ChromatogramPeakFeatureCollection([]), [collection]);

        Assert.IsTrue(File.Exists(file.DeconvolutionFilePath));
        Assert.AreEqual(0, file.DeconvolutionFilePathList.Count);
        Assert.IsFalse(File.Exists(collection.GetDeconvolutionFilePathWithCE(file)));
    }

    private AnalysisFileBean NewFile(string name) {
        return new AnalysisFileBean {
            AnalysisFileId = 0,
            AnalysisFileName = name,
            AnalysisFilePath = Path.Combine(_scratch, name + ".mzML"),
            DeconvolutionFilePath = Path.Combine(_scratch, name + "_202610711.dcl"),
            PeakAreaBeanInformationFilePath = Path.Combine(_scratch, name + "_202610711.pai"),
        };
    }

    private static List<MSDecResult> Results() {
        return Enumerable.Range(0, 3).Select(i => new MSDecResult { ScanID = i, }).ToList();
    }
}
