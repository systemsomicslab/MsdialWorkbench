using CompMs.App.MsdialConsole.Process;
using CompMs.App.MsdialConsole.Process.MoleculerNetworking;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The <c>msn</c> command networks an MS-DIAL peak list (.pai) or alignment result (.arf)
/// with the MS/MS spectra of its .dcl file. These run it on small files written here.
/// </summary>
[TestClass]
public sealed class MolecularNetworkingBinaryInputTests
{
    private string _scratch = null!;

    [TestInitialize]
    public void Initialize() {
        _scratch = Path.Combine(Path.GetTempPath(), $"msdial-msn-binary-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_scratch);
    }

    [TestCleanup]
    public void Cleanup() {
        if (Directory.Exists(_scratch)) {
            Directory.Delete(_scratch, recursive: true);
        }
    }

    // Peaks 0 and 1 share a spectrum, peak 2 does not, and peak 3 shares peak 0's spectrum in negative mode.
    private static readonly (double[] Masses, IonMode IonMode)[] Spectra = [
        ([100d, 150d, 200d, 250d], IonMode.Positive),
        ([100d, 150d, 200d, 250d], IonMode.Positive),
        ([110d, 160d, 210d, 260d], IonMode.Positive),
        ([100d, 150d, 200d, 250d], IonMode.Negative),
    ];

    [TestMethod]
    public void APeakListIsNetworkedOncePerPairUnderTheMasterPeakIds() {
        var (peaks, dcl) = WriteAnalysis();

        // The file on disk, as a user names it.
        var lines = Run(peaks + "2", dcl, "Positive", "Bonanza");

        Assert.AreEqual("SourceID\tTargetID\tScore\tMatchPeakCount", lines[0]);
        Assert.AreEqual(1, lines.Length - 1, string.Join(Environment.NewLine, lines));
        CollectionAssert.AreEqual(new[] { "10", "11" }, lines[1].Split('\t').Take(2).ToArray());
        Assert.AreEqual(4, lines[1].Split('\t').Length);
    }

    [TestMethod]
    public void APeakListIsFoundUnderTheNameAProjectRecords() {
        var (peaks, dcl) = WriteAnalysis();
        Assert.IsFalse(File.Exists(peaks), "the project's .pai name is written to disk as .pai2");

        var lines = Run(peaks, dcl, "Positive", "Bonanza");

        Assert.AreEqual(2, lines.Length);
    }

    [TestMethod]
    public void OnlySpectraOfTheRequestedIonModeAreNetworked() {
        var (peaks, dcl) = WriteAnalysis();

        var lines = Run(peaks, dcl, "negative", "Bonanza");

        Assert.AreEqual(1, lines.Length, "the one negative spectrum has nothing to pair with");
    }

    [TestMethod]
    public void AnUnknownIonModeIsRejected() {
        var (peaks, dcl) = WriteAnalysis();

        Assert.ThrowsException<ArgumentException>(() => Run(peaks, dcl, "Both", "Bonanza"));
    }

    [TestMethod]
    public void AllSimilaritiesWriteAHeaderForEachScoreColumn() {
        var (peaks, dcl) = WriteAnalysis();

        var lines = Run(peaks, dcl, "Positive", "All");

        Assert.AreEqual("SourceID\tTargetID\tBonanzaScore\tMatchPeakCount\tModDotScore\tCosineScore", lines[0]);
        Assert.AreEqual(2, lines.Length);
        Assert.AreEqual(6, lines[1].Split('\t').Length);
    }

    [TestMethod]
    public void AnAlignmentResultIsNetworkedUnderTheMasterAlignmentIds() {
        var dcl = Path.Combine(_scratch, "alignment.dcl");
        MsdecResultsWriter.Write(dcl, Scans());
        var arf = Path.Combine(_scratch, "alignment.arf");
        var spots = Enumerable.Range(0, Spectra.Length).Select(i => new AlignmentSpotProperty {
            MasterAlignmentID = i,
            AlignmentID = i,
            AlignedPeakProperties = new List<AlignmentChromPeakFeature>(),
            AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>(),
        });
        new AlignmentResultContainer { AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(spots) }
            .Save(new AlignmentFileBean { FilePath = arf });

        var lines = Run(arf + "2", dcl, "Positive", "Bonanza");

        Assert.AreEqual(2, lines.Length, string.Join(Environment.NewLine, lines));
        CollectionAssert.AreEqual(new[] { "0", "1" }, lines[1].Split('\t').Take(2).ToArray());
        CollectionAssert.AreEqual(lines, Run(arf, dcl, "Positive", "Bonanza"), "the project's .arf name");
    }

    [TestMethod]
    public void ASpectrumFileWithAnMspInputIsRejected() {
        var msp = Path.Combine(_scratch, "library.msp");
        File.WriteAllText(msp, string.Empty);
        var (_, dcl) = WriteAnalysis();
        var output = Path.Combine(_scratch, "out.pairs");
        var root = new RootCommand("test");
        MainProcess.SetMsnCommand(root);

        var exitCode = root.Parse(["msn", "-i", msp, "-s", dcl, "-o", output, "-m", WriteMethod("Bonanza")]).Invoke();

        Assert.AreNotEqual(0, exitCode);
        Assert.IsFalse(File.Exists(output));
    }

    [TestMethod]
    public void TheOnlyIncludedAnalysisFileIsSelectedWithoutAName() {
        var files = new[] { AnalysisFile("a", included: true), AnalysisFile("b", included: false) };

        var selected = MolecularNetworkingInputLoader.SelectAnalysisFile(files, null, "project.mddata");

        Assert.AreEqual("a", selected.AnalysisFileName);
    }

    [TestMethod]
    public void AnAnalysisFileIsSelectedByItsNameOrItsPeakListName() {
        var files = new[] { AnalysisFile("a", included: true), AnalysisFile("b", included: true) };

        Assert.AreEqual("b", MolecularNetworkingInputLoader.SelectAnalysisFile(files, "B", "project.mddata").AnalysisFileName);
        Assert.AreEqual("a", MolecularNetworkingInputLoader.SelectAnalysisFile(files, "a_peaks", "project.mddata").AnalysisFileName);
    }

    [TestMethod]
    public void SeveralIncludedFilesWithoutANameAreRejectedWithTheCandidates() {
        var files = new[] { AnalysisFile("a", included: true), AnalysisFile("b", included: true) };

        var ex = Assert.ThrowsException<InvalidOperationException>(() => MolecularNetworkingInputLoader.SelectAnalysisFile(files, null, "project.mddata"));

        StringAssert.Contains(ex.Message, "a, b");
    }

    [TestMethod]
    public void AnUnmatchedOrExcludedNameIsReported() {
        var files = new[] { AnalysisFile("a", included: true), AnalysisFile("b", included: false) };

        Assert.ThrowsException<FileNotFoundException>(() => MolecularNetworkingInputLoader.SelectAnalysisFile(files, "b", "project.mddata"));
        Assert.ThrowsException<FileNotFoundException>(() => MolecularNetworkingInputLoader.SelectAnalysisFile(files, "c", "project.mddata"));
    }

    private AnalysisFileBean AnalysisFile(string name, bool included) {
        // The project records the .pai name; the file on disk is .pai2.
        var peaks = Path.Combine(_scratch, name + "_peaks.pai");
        File.WriteAllText(peaks + "2", string.Empty);
        return new AnalysisFileBean {
            AnalysisFileName = name,
            AnalysisFileIncluded = included,
            PeakAreaBeanInformationFilePath = peaks,
        };
    }

    private (string Peaks, string Dcl) WriteAnalysis() {
        var dcl = Path.Combine(_scratch, "sample.dcl");
        MsdecResultsWriter.Write(dcl, Scans());
        var peaks = Path.Combine(_scratch, "sample.pai");
        MsdialPeakSerializer.SaveChromatogramPeakFeatures(peaks, Enumerable.Range(0, Spectra.Length)
            .Select(i => new ChromatogramPeakFeature { PeakID = i, MasterPeakID = 10 + i, })
            .ToList());
        return (peaks, dcl);
    }

    private static List<MSDecResult> Scans() {
        return Spectra.Select((s, i) => new MSDecResult {
            ScanID = i,
            PrecursorMz = 300d,
            IonMode = s.IonMode,
            Spectrum = s.Masses.Select(mass => new SpectrumPeak { Mass = mass, Intensity = 1000d, }).ToList(),
        }).ToList();
    }

    private string WriteMethod(string similarity) {
        var method = Path.Combine(_scratch, $"mn-{similarity}.txt");
        File.WriteAllLines(method, [
            $"MsmsSimilarityCalc: {similarity}",
            "MnMassTolerance: 0.01",
            "MnSpectrumSimilarityCutOff: 50",
            "MinimumPeakMatch: 3",
            "MaxEdgeNumberPerNode: 10",
            "MaxPrecursorDifference: 400",
            "MnRelativeAbundanceCutOff: 0",
            "MnAbsoluteAbundanceCutOff: 0",
        ]);
        return method;
    }

    private string[] Run(string input, string dcl, string ionMode, string similarity) {
        var output = Path.Combine(_scratch, $"out-{Guid.NewGuid():N}.pairs");
        new MoleculerNetworkProcess().Run4Binary(input, dcl, output, WriteMethod(similarity), ionMode);
        return File.ReadAllLines(output);
    }
}
