using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

[TestClass]
public class AlignmentMolecularNetworkExporterTests
{
    [TestMethod]
    [DataRow(MsmsSimilarityCalc.Bonanza)]
    [DataRow(MsmsSimilarityCalc.ModDot)]
    [DataRow(MsmsSimilarityCalc.Cosine)]
    [DataRow(MsmsSimilarityCalc.All)]
    public void Export_UsesAlignmentIdsAndPreservesRepresentativeSpectra(MsmsSimilarityCalc calculation) {
        WithDirectory(folder => {
            var spots = new[] { Spot(7), Spot(19), Spot(35) };
            var spectra = new[] { Spectrum(), Spectrum(), new MSDecResult() };
            var original = spectra[0].Spectrum;
            AlignmentMolecularNetworkExporter.Export(spots, spectra, new MolecularSpectrumNetworkingBaseParameter {
                MsmsSimilarityCalc = calculation,
                MnSpectrumSimilarityCutOff = 50,
                MinimumPeakMatch = 1,
                MnRelativeAbundanceCutOff = 10,
            }, 2, folder);

            var nodeFile = Path.Combine(folder, "node.txt");
            var nodes = File.ReadAllLines(nodeFile);
            CollectionAssert.AreEqual(new[] { "7", "19", "35" }, nodes.Skip(1).Select(line => line.Split('\t')[0]).ToArray());
            Assert.AreEqual("ID\tMetaboliteName\tRt\tMz\tFormula\tOntology\tInChIKey\tSMILES\tPeaks", nodes[0]);
            Assert.AreEqual("50,100;100,80;150,1", nodes[1].Split('\t')[8]);
            Assert.IsTrue(nodes.All(line => line.Split('\t').Length == 9));
            Assert.IsTrue(nodes.Skip(1).All(line => !line.Split('\t')[8].Contains('\r') && !line.Split('\t')[8].Contains('\n')));
            var edges = File.ReadAllLines(Path.Combine(folder, "edge.txt"));
            Assert.AreEqual(2, edges.Length);
            Assert.AreEqual("SourceID\tTargetID\tScore\tMatchPeakCount", edges[0]);
            StringAssert.StartsWith(edges[1], "7\t19\t");
            Assert.AreEqual(4, edges[1].Split('\t').Length);
            Assert.AreEqual("2", edges[1].Split('\t')[3]);
            Assert.AreSame(original, spectra[0].Spectrum);
            Assert.AreEqual(3, spectra[0].Spectrum.Count);
        });
    }

    [TestMethod]
    public void Export_DenseNetworkSpillsCandidatesAndKeepsStableDegreeLimit() {
        WithDirectory(folder => {
            const int count = 365; // 66,430 candidates exceeds the external sort chunk size.
            var spots = Enumerable.Range(0, count).Select(i => Spot(7 + i * 10)).ToArray();
            var spectra = Enumerable.Range(0, count).Select(_ => Spectrum()).ToArray();
            var nestedFolder = Path.Combine(folder, "msn");
            Assert.IsFalse(Directory.Exists(nestedFolder));
            AlignmentMolecularNetworkExporter.Export(spots, spectra, new MolecularSpectrumNetworkingBaseParameter {
                MsmsSimilarityCalc = MsmsSimilarityCalc.Cosine,
                MnSpectrumSimilarityCutOff = 50,
                MinimumPeakMatch = 1,
                MaxEdgeNumberPerNode = 1,
            }, 2, nestedFolder);
            var edges = File.ReadAllLines(Path.Combine(nestedFolder, "edge.txt")).Skip(1).ToArray();
            Assert.AreEqual(0, Directory.GetDirectories(nestedFolder).Length, "Temporary sort directories must be removed.");
            Assert.AreEqual(count / 2, edges.Length);
            for (int i = 0; i < edges.Length; i++) {
                StringAssert.StartsWith(edges[i], $"{spots[i * 2].MasterAlignmentID}\t{spots[i * 2 + 1].MasterAlignmentID}\t");
            }
        });
    }

    [TestMethod]
    public void Export_EmptyAlignmentProducesHeaders() {
        WithDirectory(folder => {
            AlignmentMolecularNetworkExporter.Export(Array.Empty<AlignmentSpotProperty>(), Array.Empty<MSDecResult>(), new MolecularSpectrumNetworkingBaseParameter(), 1, folder);
            Assert.AreEqual(1, File.ReadAllLines(Path.Combine(folder, "node.txt")).Length);
            Assert.AreEqual(1, File.ReadAllLines(Path.Combine(folder, "edge.txt")).Length);
        });
    }

    [TestMethod]
    public void Export_RespectsMinimumPeakMatch() {
        WithDirectory(folder => {
            AlignmentMolecularNetworkExporter.Export(new[] { Spot(7), Spot(19) }, new[] { Spectrum(), Spectrum() },
                new MolecularSpectrumNetworkingBaseParameter { MinimumPeakMatch = 10 }, 2, folder);
            Assert.AreEqual(1, File.ReadAllLines(Path.Combine(folder, "edge.txt")).Length);
            Assert.AreEqual(3, File.ReadAllLines(Path.Combine(folder, "node.txt")).Length);
        });
    }

    [TestMethod]
    public void Export_RejectsMismatchedSpotAndSpectrumCounts() {
        WithDirectory(folder => Assert.ThrowsExactly<ArgumentException>(() =>
            AlignmentMolecularNetworkExporter.Export(new[] { Spot(7) }, Array.Empty<MSDecResult>(), new MolecularSpectrumNetworkingBaseParameter(), 1, folder)));
    }

    [TestMethod]
    public void Run_RequiresSeparateMsnParameterFile() {
        WithDirectory(folder => {
            Directory.CreateDirectory(folder);
            var lcmsParameter = Path.Combine(folder, "lcms.txt");
            File.WriteAllText(lcmsParameter, "");
            var root = new RootCommand();
            MainProcess.SetLcmsMsnCommand(root);
            var input = Path.Combine(folder, "input.mzML");
            File.WriteAllText(input, "");
            var parsed = root.Parse(new[] { "lcms-msn", "-i", input, "-o", folder, "-m", lcmsParameter });
            Assert.IsTrue(parsed.Errors.Count > 0);
            var msnParameter = Path.Combine(folder, "msn.txt");
            File.WriteAllText(msnParameter, "");
            parsed = root.Parse(new[] { "lcms-msn", "-i", input, "-o", folder, "-m", lcmsParameter, "-mn", msnParameter, "--resume" });
            Assert.AreEqual(0, parsed.Errors.Count);
        });
    }

    [TestMethod]
    public void RunWithMolecularNetworking_Net8RejectsWiffBeforeProcessing() {
        WithDirectory(folder => {
            Directory.CreateDirectory(folder);
            var input = Path.Combine(folder, "sample.wiff");
            var method = Path.Combine(folder, "lcms.txt");
            var msnMethod = Path.Combine(folder, "msn.txt");
            var output = Path.Combine(folder, "output");
            File.WriteAllText(input, "");
            File.WriteAllText(method, "");
            File.WriteAllText(msnMethod, "");
            Assert.AreEqual(-1, new LcmsProcess().RunWithMolecularNetworking(input, output, method, msnMethod, false, -1));
            Assert.IsFalse(Directory.Exists(output));
        });
    }

    private static AlignmentSpotProperty Spot(int id) => new AlignmentSpotProperty {
        MasterAlignmentID = id,
        MassCenter = 300,
        HeightAverage = 100,
        Name = "Unknown",
        TimesCenter = new ChromXs(1, ChromXType.RT, ChromXUnit.Min),
    };

    private static MSDecResult Spectrum() => new MSDecResult {
        Spectrum = new List<SpectrumPeak> { new SpectrumPeak(50, 100), new SpectrumPeak(100, 80), new SpectrumPeak(150, 1) },
    };

    private static void WithDirectory(Action<string> test) {
        var folder = Path.Combine(Path.GetTempPath(), "lcms-msn-" + Guid.NewGuid().ToString("N"));
        try { test(folder); }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
