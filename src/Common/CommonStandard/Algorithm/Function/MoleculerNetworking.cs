using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Algorithm.Function {

    public class LinkNode {
        public double[] Score { get; set; }
        public IMSScanProperty Node { get; set; }
    }

    public class MoleculerNetworking {
        private MoleculerNetworking() { }

        public static List<EdgeData> GenerateEdges(
            IReadOnlyList<IMSScanProperty> quries, 
            double minimumPeakMatch, 
            double matchThreshold, 
            double maxEdgeNumPerNode,
            double maxPrecursorDiff,
            double maxPrecursorDiff_Percent,
            bool isBonanza,
            Action<double> report) {

            var edges = new List<EdgeData>();
            var counter = 0;
            var max = quries.Count * quries.Count;
            var node2links = new Dictionary<int, List<LinkNode>>();
            for (int i = 0; i < quries.Count; i++) {
                for (int j = i + 1; j < quries.Count; j++) {
                    counter++;
                    
                    Console.Write("{0} / {1}", counter, max);
                    Console.SetCursorPosition(0, Console.CursorTop);
                    
                    var prop1 = quries[i];
                    var prop2 = quries[j];
                    var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
                    if (massDiff > maxPrecursorDiff) continue;
                    if (Math.Max(prop1.PrecursorMz, prop2.PrecursorMz) * maxPrecursorDiff_Percent * 0.01 - Math.Min(prop1.PrecursorMz, prop2.PrecursorMz) < 0) continue;
                    double[] scoreitem = new double[2];
                    if (isBonanza) {
                        scoreitem = MsScanMatching.GetBonanzaScore(prop1, prop2);
                    }
                    else {
                        scoreitem = MsScanMatching.GetModifiedDotProductScore(prop1, prop2);
                    }
                    if (scoreitem[1] < minimumPeakMatch) continue;
                    if (scoreitem[0] < matchThreshold) continue;

                    if (node2links.ContainsKey(i)) {
                        node2links[i].Add(new LinkNode() { Score = scoreitem, Node = quries[j] });
                    }
                    else {
                        node2links[i] = new List<LinkNode>() { new LinkNode() { Score = scoreitem, Node = quries[j] } };
                    }
                }
            }

            var cNode2Links = new Dictionary<int, List<LinkNode>>();
            foreach (var item in node2links) {
                var nitem = item.Value.OrderByDescending(n => n.Score[0]).ToList();
                cNode2Links[item.Key] = new List<LinkNode>();
                for (int i = 0; i < nitem.Count; i++) {
                    if (i > maxEdgeNumPerNode - 1) break;
                    cNode2Links[item.Key].Add(nitem[i]);
                }
            }

            foreach (var item in cNode2Links) {
                foreach (var link in item.Value) {
                    var source_node_id = quries[item.Key].ScanID;
                    var target_node_id = link.Node.ScanID;

                    var edge = new EdgeData() {
                        score = link.Score[0], matchpeakcount = link.Score[1], source = source_node_id, target = target_node_id
                    };
                    edges.Add(edge);
                }
            }
            return edges;
        }
    }
}
