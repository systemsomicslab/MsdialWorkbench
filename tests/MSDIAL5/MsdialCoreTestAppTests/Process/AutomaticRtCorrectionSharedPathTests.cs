using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// A production run of the Console with the automatic alignment RT correction, and the GUI's path
/// through the same library code on the project that run saved.
/// </summary>
[TestClass]
public sealed class AutomaticRtCorrectionSharedPathTests
{
    private static readonly (double Mz, double Rt)[] Standards = { (200.1, 2d), (300.2, 5d), (400.3, 8d) };

    [TestMethod]
    public async Task ProductionRunSavesTheMapLabelsTheExportsAndTheGuiPathReproducesTheCorrection()
    {
        using var directory = new ScratchDirectory();
        var input = directory.Folder("raw");
        var output = directory.Folder("output");
        // Three injections, the second migrating 0.1 min later and the third 0.1 min earlier, with a
        // few other compounds so that alignment has something to align besides the anchors.
        var shifts = new Dictionary<string, double> { ["s1"] = 0d, ["s2"] = 0.1d, ["s3"] = -0.1d };
        foreach (var (name, shift) in shifts) {
            var compounds = Standards.Select(standard => (standard.Mz, standard.Rt + shift, 100000d))
                .Concat(new[] { (250.15, 3.5 + shift, 50000d), (350.25, 6.5 + shift, 50000d) })
                .OrderBy(compound => compound.Item1) // a spectrum lists its m/z in increasing order
                .ToList();
            SyntheticLcmsMzml.Write(Path.Combine(input, name + ".mzML"), compounds);
        }
        var library = directory.File(
            "anchors.txt",
            "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F\n"
            + "IS1\t2\t1\t200.1\t0.01\t1000\tTrue\n"
            + "IS2\t5\t1\t300.2\t0.01\t1000\tTrue\n"
            + "IS3\t8\t1\t400.3\t0.01\t1000\tTrue\n");
        var methodFile = directory.File(
            "method.txt",
            """
            Execute automatic RT correction for alignment: True
            Together with alignment: True
            Automatic RT correction anchor mode: UserOnly
            Automatic RT correction anchor library file path: anchors.txt
            Export as mztabM format: True
            Number of threads: 2
            """);

        var (result, stdout, stderr) = RunLcms(input, output, methodFile);
        if (result != 0) {
            // The audit says which anchor was not found where.
            var anchorsTsv = Path.Combine(output, AutomaticAlignmentRetentionTimeCorrectionResult.AnchorsFileName);
            Assert.Fail(stderr + (File.Exists(anchorsTsv) ? File.ReadAllText(anchorsTsv) : "no audit"));
        }

        // The map is saved beside the alignment result, where the project's alignment file is.
        var maps = Directory.GetFiles(input, "*" + AlignmentRetentionTimeCorrectionMapFile.FileSuffix);
        Assert.AreEqual(1, maps.Length, stdout);
        StringAssert.Contains(stdout, "Automatic alignment RT correction map: " + maps[0]);
        var consoleMap = File.ReadAllText(maps[0]);
        var consoleCorrection = AlignmentRetentionTimeCorrectionMapFile.Read(maps[0]);

        // The exports say which axis the aligned RTs are on.
        var mdalign = Directory.GetFiles(output, "*.mdalign").Single();
        StringAssert.StartsWith(File.ReadLines(mdalign).First(), "RT axis: aligned RTs are on the RT axis of reference file s");
        var mztab = Directory.GetFiles(output, "*.mzTab").Single();
        Assert.IsTrue(File.ReadLines(mztab).Any(line => line.StartsWith("MTD\tcustom[") && line.Contains("MS-DIAL retention time axis")));

        // The GUI's path, on the project the Console saved: the same correction, the same audit.
        var storage = await LoadProjectAsync(Directory.GetFiles(output, "*.mdproject").Single());
        var parameter = (MsdialLcmsParameter)storage.Parameter;
        Assert.IsTrue(parameter.AlignmentBaseParam.AutomaticRtCorrection.Execute, "the project keeps its settings");
        Assert.AreEqual(AutomaticRtCorrectionAnchorMode.UserOnly, parameter.AlignmentBaseParam.AutomaticRtCorrection.AnchorMode);
        Assert.AreEqual(consoleCorrection.ReferenceFileId, parameter.AlignmentReferenceFileID, "the Console made the correction's reference the alignment reference");
        parameter.AlignmentReferenceFileID = -99;
        var reporter = new CollectingAutomaticRtCorrectionReporter();
        var inputs = AutomaticAlignmentRtCorrectionWorkflow.PrepareInputs(parameter, reporter);
        Assert.IsNotNull(inputs, string.Join("\n", reporter.Lines.Select(line => line.Line)));
        var alignmentFile = storage.AlignmentFiles.Single();
        File.Delete(maps[0]);
        var gui = AutomaticAlignmentRtCorrectionWorkflow.RunForAlignment(
            storage.AnalysisFiles, parameter, inputs, alignmentFile, new StandardDataProviderFactory(5, false), reporter);

        Assert.IsNotNull(gui, string.Join("\n", reporter.Lines.Select(line => line.Line)));
        Assert.AreEqual(consoleCorrection.ReferenceFileId, parameter.AlignmentReferenceFileID, "the GUI makes the correction's reference the alignment reference");
        Assert.AreEqual(consoleMap, File.ReadAllText(maps[0]), "the GUI writes the same map, byte for byte");
        var auditFolder = AutomaticAlignmentRtCorrectionWorkflow.AuditFolderFor(alignmentFile);
        foreach (var name in new[] { AutomaticAlignmentRetentionTimeCorrectionResult.SummaryFileName, AutomaticAlignmentRetentionTimeCorrectionResult.AnchorsFileName }) {
            Assert.AreEqual(File.ReadAllText(Path.Combine(output, name)), File.ReadAllText(Path.Combine(auditFolder, name)), name);
        }

        // And the axes the curation windows read are the alignment's own maps.
        var axis = AlignmentRtAxis.Load(alignmentFile, storage.AnalysisFiles, correctionRequested: true);
        Assert.IsNotNull(axis.Correction);
        foreach (var file in storage.AnalysisFiles) {
            foreach (var rt in new[] { 1.9d, 2.1d, 4.95d, 6.5d, 8.05d }) {
                Assert.AreEqual(gui.Correction.Correct(file.AnalysisFileId, rt), axis.Correction.Correct(file.AnalysisFileId, rt));
            }
        }
    }

    private static async Task<IMsdialDataStorage<ParameterBase>> LoadProjectAsync(string projectFile)
    {
        var projectDirectory = Path.GetDirectoryName(projectFile)!;
        using var stream = File.Open(projectFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        using IStreamManager streamManager = ZipStreamManager.OpenGet(stream);
        var project = await ProjectDataStorage.LoadAsync(
            streamManager,
            new MsdialIntegrateSerializer(),
            path => new DirectoryTreeStreamManager(path),
            projectDirectory,
            _ => Task.FromResult<string?>(null),
            _ => { });
        streamManager.Complete();
        return project.Storages.Single();
    }

    private static (int Result, string Output, string Error) RunLcms(string input, string output, string methodFile)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        try {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var result = new LcmsProcess().Run(input, output, methodFile, isProjectSaved: true, targetMz: -1f);
            return (result, stdout.ToString(), stderr.ToString());
        }
        finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private sealed class ScratchDirectory : IDisposable
    {
        public ScratchDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "msdial-autort-shared-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Folder(string name)
        {
            var path = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public string File(string name, string content)
        {
            var path = System.IO.Path.Combine(Path, name);
            System.IO.File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path)) {
                try {
                    Directory.Delete(Path, recursive: true);
                }
                catch (IOException) {
                }
            }
        }
    }
}
