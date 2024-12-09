using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Parser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.Algorithm.Function
{
    public sealed class MolecularNetworkInstance {
        public MolecularNetworkInstance(RootObject root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));       
        }

        public RootObject Root { get; }

        public bool IsNodeEmpty => Root.nodes.Count == 0;
        public bool IsEdgeEmpty => Root.edges.Count == 0;

        public MolecularNetworkInstance PruneEdgeByScore(int maxNumberOfEdge) {
            if (Root.edges.Count <= maxNumberOfEdge) {
                return this;
            }
            return new MolecularNetworkInstance(
                new RootObject {
                    nodes = Root.nodes,
                    edges = Root.edges.GroupBy(e => e.classes)
                        .SelectMany(group => group.OrderByDescending(edge => edge.data.score).Take(maxNumberOfEdge))
                        .ToList()
                });
        }

        public MolecularNetworkInstance DropIsolatedNodes() {
            var nodeIDs = new HashSet<int>(Root.edges.SelectMany(edge => new[] { edge.data.source, edge.data.target }));
            var nodes = Root.nodes.Where(node => nodeIDs.Contains(node.data.id)).ToList();
            return new MolecularNetworkInstance(new RootObject { nodes = nodes, edges = Root.edges, });
        }

        public MolecularNetworkInstance DropInvalidMsmsNodes() {
            var invalidNodeIDs = new HashSet<int>(Root.nodes.Where(node => node.data.MsmsMin < 0d).Select(node => node.data.id));
            if (invalidNodeIDs.Count == 0) {
                return this;
            }
            var edges = Root.edges.Where(edge => !invalidNodeIDs.Contains(edge.data.source) && !invalidNodeIDs.Contains(edge.data.target)).ToList();
            var nodes = Root.nodes.Where(node => !invalidNodeIDs.Contains(node.data.id)).ToList();
            return new MolecularNetworkInstance(new RootObject { edges = edges, nodes = nodes, });
        }

        public void ExportNodeTable(string nodeFile) {
            using var writer = new CsvWriter(nodeFile) { Delimiter = '\t', };
            writer.WriteRow(["ID", "MetaboliteName", "Rt", "Mz", "Formula", "Ontology", "InChIKey", "SMILES", "Size", "BorderColor", "BackgroundColor", "Ms2"]);
            foreach (var nodeObj in Root.nodes) {
                var node = nodeObj.data;
                writer.WriteRow([
                    node.id.ToString(),
                    node.Name,
                    node.Rt,
                    node.Mz,
                    node.Formula,
                    node.Ontology,
                    node.InChiKey,
                    node.Smiles,
                    node.Size.ToString(),
                    node.bordercolor,
                    node.backgroundcolor,
                    GetMsString(node.MSMS)
                ]);
            }
        }

        public void ExportEdgeTable(string edgeFile) {
            using (StreamWriter sw = new StreamWriter(edgeFile, false, Encoding.ASCII)) {
                sw.WriteLine("SourceID\tTargetID\tScore\tType\tColor\tComment");
                foreach (var edgeObj in Root.edges) {
                    var edge = edgeObj.data;
                    sw.WriteLine(edge.source + "\t" + edge.target + "\t" + edge.score + "\t" + edgeObj.classes + "\t" + edge.linecolor + "\t" + edge.comment);
                }
            }
        }

        public void ExportCyelement(string cyelementFile) {
            using (StreamWriter sw = new StreamWriter(cyelementFile, false, Encoding.ASCII)) {
                var rootCy = new { elements = Root };
                var json = JsonConvert.SerializeObject(rootCy, Formatting.Indented);
                sw.WriteLine(json.ToString());
            }
        }

        public void ExportNodeEdgeFiles(string folder) {
            var dt = DateTime.Now;
            ExportNodeTable(Path.Combine(folder, $"node-{dt:yyMMddhhmm}.txt"));
            ExportEdgeTable(Path.Combine(folder, $"edge-{dt:yyMMddhhmm}.txt"));
            ExportCyelement(Path.Combine(folder, $"cyelements-{dt:yyMMddhhmm}.js"));
        }

        public void SaveCytoscapeJs(string cyjsexportpath) {
            var root = new RootObject { nodes = Root.nodes, edges = Root.edges.OrderByDescending(edge => edge.data.score).ToList(), };
            var json = JsonConvert.SerializeObject(root, Formatting.Indented);
            using (StreamWriter sw = new StreamWriter(cyjsexportpath, false, Encoding.ASCII)) {
                sw.WriteLine("var dataElements =\r\n" + json.ToString() + "\r\n;");
            }
        }

        private static string GetMsString(List<List<double>> msList) {
            if (msList == null || msList.Count == 0) {
                return string.Empty;
            }
            return string.Join(" ", msList.Select(ms => $"{Math.Round(ms[0], 5)}:{Math.Round(ms[1], 0)}"));
        }
    }
}
