using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Algorithm.Function;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace CompMs.Common.Tests.Algorithm.Function;

[TestClass]
public class MolecularNetworkInstanceTests
{
    private RootObject CreateTestRootObject() {
        return new RootObject
        {
            nodes = new List<Node>
                {
                    new Node { data = new NodeData { id = 1, Name = "Node1", Rt = "1.0", Mz = "100.0", Formula = "H2O", Ontology = "Water", InChiKey = "InChIKey1", Smiles = "O", Size = 1, bordercolor = "black", backgroundcolor = "white", MSMS = new List<List<double>> { new List<double> { 100.0, 1.0 } } } },
                    new Node { data = new NodeData { id = 2, Name = "Node2", Rt = "2.0", Mz = "200.0", Formula = "CO2", Ontology = "Carbon Dioxide", InChiKey = "InChIKey2", Smiles = "O=C=O", Size = 2, bordercolor = "black", backgroundcolor = "white", MSMS = new List<List<double>> { new List<double> { 200.0, 2.0 } } } }
                },
            edges = new List<Edge>
                {
                    new Edge { data = new EdgeData { source = 1, target = 2, score = 0.9, linecolor = "red", comment = "Edge1" }, classes = "class1" }
                }
        };
    }

    [TestMethod]
    public void PruneEdgeByScore_ShouldPruneEdges() {
        var root = CreateTestRootObject();
        var instance = new MolecularNetworkInstance(root);

        var prunedInstance = instance.PruneEdgeByScore(1);

        Assert.AreEqual(1, prunedInstance.Root.edges.Count);
    }

    [TestMethod]
    public void DropIsolatedNodes_ShouldDropIsolatedNodes() {
        var root = CreateTestRootObject();
        var instance = new MolecularNetworkInstance(root);

        var prunedInstance = instance.DropIsolatedNodes();

        Assert.AreEqual(2, prunedInstance.Root.nodes.Count);
    }

    [TestMethod]
    public void DropInvalidMsmsNodes_ShouldDropInvalidNodes() {
        var root = CreateTestRootObject();
        root.nodes[0].data.MsmsMin = -1.0;
        var instance = new MolecularNetworkInstance(root);

        var prunedInstance = instance.DropInvalidMsmsNodes();

        Assert.AreEqual(1, prunedInstance.Root.nodes.Count);
    }

    [TestMethod]
    public void ExportNodeTable_ShouldExportNodeTable() {
        var root = CreateTestRootObject();
        var instance = new MolecularNetworkInstance(root);
        var nodeFile = Path.GetTempFileName();

        try {
            instance.ExportNodeTable(nodeFile, false);

            var lines = File.ReadAllLines(nodeFile);
            Assert.AreEqual(3, lines.Length); // Header + 2 nodes
            Assert.AreEqual(12, lines[0].Split('\t').Length); // Check header columns count
            Assert.AreEqual(12, lines[1].Split('\t').Length); // Check first row columns count
            Assert.AreEqual(12, lines[2].Split('\t').Length); // Check second row columns count
            Assert.IsTrue(lines[1].Contains("Node1"));
            Assert.IsTrue(lines[2].Contains("Node2"));
        }
        finally {
            File.Delete(nodeFile);
        }
    }

    [TestMethod]
    public void ExportEdgeTable_ShouldExportEdgeTable() {
        var root = CreateTestRootObject();
        var instance = new MolecularNetworkInstance(root);
        var edgeFile = Path.GetTempFileName();

        try {
            instance.ExportEdgeTable(edgeFile, false);

            var lines = File.ReadAllLines(edgeFile);
            Assert.AreEqual(2, lines.Length); // Header + 1 edge
            Assert.AreEqual(6, lines[0].Split('\t').Length); // Check header columns count
            Assert.AreEqual(6, lines[1].Split('\t').Length); // Check first row columns count
        }
        finally {
            File.Delete(edgeFile);
        }
    }

    [TestMethod]
    public void ExportCyelement_ShouldExportCyelement() {
        var root = CreateTestRootObject();
        var instance = new MolecularNetworkInstance(root);
        var cyelementFile = Path.GetTempFileName();

        try {
            instance.ExportCyelement(cyelementFile);

            var content = File.ReadAllText(cyelementFile);
            Assert.IsTrue(content.Contains("elements"));
        }
        finally {
            File.Delete(cyelementFile);
        }
    }

    [TestMethod]
    public void ExportNodeEdgeFiles_ShouldExportAllFiles() {
        var root = CreateTestRootObject();
        var instance = new MolecularNetworkInstance(root);
        var folder = Path.GetTempPath();

        var (nodeFilePath, edgeFilePath, cyelementFilePath) = instance.ExportNodeEdgeFiles(folder, false);

        try {
            Assert.IsTrue(File.Exists(nodeFilePath));
            Assert.IsTrue(File.Exists(edgeFilePath));
            Assert.IsTrue(File.Exists(cyelementFilePath));
        }
        finally {
            File.Delete(nodeFilePath);
            File.Delete(edgeFilePath);
            File.Delete(cyelementFilePath);
        }
    }

    [TestMethod]
    public void SaveCytoscapeJs_ShouldSaveCytoscapeJs() {
        var root = CreateTestRootObject();
        var instance = new MolecularNetworkInstance(root);
        var cyjsexportpath = Path.GetTempFileName();

        try {
            instance.SaveCytoscapeJs(cyjsexportpath);

            var content = File.ReadAllText(cyjsexportpath);
            Assert.IsTrue(content.Contains("var dataElements"));
        }
        finally {
            File.Delete(cyjsexportpath);
        }
    }
}
