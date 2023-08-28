using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Interfaces;
using CompMs.Common.Mathematics.Basic;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm {

    public class NodeTemp {
        public double Score { get; set; }
        public AlignmentSpotProperty Peak { get; set; }
    }
    public sealed class MolecularNetworking {
        private MolecularNetworking() { }

        public static List<Edge> GenerateEdgesByIonValues(IReadOnlyList<AlignmentSpotProperty> spots, double cutoff, double maxEdgeNumPerNode) {
            var edges = new List<EdgeData>();
            var counter = 0;
            var max = spots.Count;
            var node2links = new Dictionary<int, List<NodeTemp>>();

            for (int i = 0; i < spots.Count; i++) {
                var aSpot = spots[i];
                var aArray = aSpot.AlignedPeakProperties.Select(n => n.PeakHeightTop).ToArray();
                counter++;
                for (int j = i + 1; j < spots.Count; j++) {
                    var bSpot = spots[j];
                    var bArray = bSpot.AlignedPeakProperties.Select(n => n.PeakHeightTop).ToArray();
                    var score = BasicMathematics.Coefficient(aArray, bArray);
                    if (score > cutoff * 0.01) {
                        if (node2links.ContainsKey(i)) {
                            node2links[i].Add(new NodeTemp() { Score = score, Peak = bSpot });
                        }
                        else {
                            node2links[i] = new List<NodeTemp>() { new NodeTemp() { Score = score, Peak = bSpot } };
                        }
                    }
                }
            }
            var cNode2Links = new Dictionary<int, List<NodeTemp>>();
            foreach (var item in node2links) {
                var nitem = item.Value.OrderByDescending(n => n.Score).ToList();
                cNode2Links[item.Key] = new List<NodeTemp>();
                for (int i = 0; i < nitem.Count; i++) {
                    if (i > maxEdgeNumPerNode - 1) break;
                    cNode2Links[item.Key].Add(nitem[i]);
                }
            }

            foreach (var item in cNode2Links) {
                foreach (var link in item.Value) {
                    var source_node_id = spots[item.Key].MasterAlignmentID;
                    var target_node_id = link.Peak.MasterAlignmentID;

                    var edge = new EdgeData() {
                        score = link.Score, source = source_node_id, target = target_node_id
                    };
                    edges.Add(edge);
                }
            }

            return edges.Select(n => new Edge() { data = n, classes = "ioncorr_similarity" }).ToList();
        }
    }
}
