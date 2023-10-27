using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Extension;
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

        public void ExportNodeTable(string nodeFile) {
            using (StreamWriter sw = new StreamWriter(nodeFile, false, Encoding.ASCII)) {
                sw.WriteLine("ID\tMetaboliteName\tRt\tMz\tFormula\tOntology\tInChIKey\tSMILES\tSize\tBorderColor\tBackgroundColor\tMs2");
                foreach (var nodeObj in Root.nodes) {
                    var node = nodeObj.data;
                    sw.Write(node.id + "\t" + node.Name + "\t" + node.Rt + "\t" + node.Mz + "\t" + node.Formula + "\t" + node.Ontology + "\t" +
                       node.InChiKey + "\t" + node.Smiles + "\t" + node.Size + "\t" + node.bordercolor + "\t" + node.backgroundcolor + "\t");

                    var ms2String = GetMsString(node.MSMS);
                    sw.WriteLine(ms2String);
                }
            }
        }

        public void ExportEdgeTable(string edgeFile) {
            using (StreamWriter sw = new StreamWriter(edgeFile, false, Encoding.ASCII)) {
                sw.WriteLine("SourceID\tTargetID\tScore\tType");
                foreach (var edgeObj in Root.edges) {
                    var edge = edgeObj.data;
                    sw.WriteLine(edge.source + "\t" + edge.target + "\t" + edge.score + "\t" + edgeObj.classes);
                }
            }
        }

        public void ExportCyelement(string cyelementFile) {
            using (StreamWriter sw = new StreamWriter(cyelementFile, false, Encoding.ASCII)) {
                var rootCy = new RootObj4Cytoscape { elements = Root };
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

        public bool SaveCytoscapeJs(string cyjsexportpath) {
            if (Root.nodes.IsEmptyOrNull() || Root.edges.IsEmptyOrNull()) {
                return false;
            }

            var counter = 0;
            var edges = new List<Edge>();
            var nodekeys = new HashSet<int>();
            foreach (var edge in Root.edges.OrderByDescending(n => n.data.score)) {
                if (counter > 3000) break;
                edges.Add(edge);
                nodekeys.Add(edge.data.source);
                nodekeys.Add(edge.data.target);
                counter++;
            }

            var nodes = new List<Node>();
            foreach (var node in Root.nodes.Where(n => n.data.MsmsMin > 0)) {
                if (nodekeys.Contains(node.data.id)) {
                    nodes.Add(node);
                }
            }
            var nRootObj = new RootObject { nodes = nodes, edges = edges };
            var json = JsonConvert.SerializeObject(nRootObj, Formatting.Indented);
            using (StreamWriter sw = new StreamWriter(cyjsexportpath, false, Encoding.ASCII)) {
                sw.WriteLine("var dataElements =\r\n" + json.ToString() + "\r\n;");
            }
            return true;
        }

        private static string GetMsString(List<List<double>> msList) {
            if (msList == null || msList.Count == 0) {
                return string.Empty;
            }
            return string.Join(" ", msList.Select(ms => $"{Math.Round(ms[0], 5)}:{Math.Round(ms[1], 0)}"));
        }
    }
}
