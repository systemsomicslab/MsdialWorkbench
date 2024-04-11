using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Mathematics.Basic;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm {

    public static class MolecularNetworking {
        public static List<Edge> GenerateEdgesByIonValues(IReadOnlyList<AlignmentSpotProperty> spots, double cutoff, double maxEdgeNumPerNode) {
            var links = new List<LinkTemp>();
            for (int i = 0; i < spots.Count; i++) {
                var aSpot = spots[i];
                var aArray = aSpot.AlignedPeakProperties.Select(n => n.PeakHeightTop).ToArray();
                for (int j = i + 1; j < spots.Count; j++) {
                    var bSpot = spots[j];
                    var bArray = bSpot.AlignedPeakProperties.Select(n => n.PeakHeightTop).ToArray();
                    var score = BasicMathematics.Coefficient(aArray, bArray);
                    if (score > cutoff * 0.01) {
                        links.Add(new LinkTemp() { Score = score, Source = aSpot.MasterAlignmentID, Target = bSpot.MasterAlignmentID, });
                    }
                }
            }

            var sets = new Dictionary<int, int>();
            foreach (var link in links) {
                sets[link.Source] = sets[link.Target] = 0;
            }
            var edges = new List<Edge>();
            foreach (var link in links.OrderByDescending(e => e.Score)) {
                if (sets[link.Source] < maxEdgeNumPerNode && sets[link.Target] < maxEdgeNumPerNode) {
                    ++sets[link.Source];
                    ++sets[link.Target];

                    var edge = new Edge {
                        data = new EdgeData {
                            score = System.Math.Round(link.Score, 3),
                            source = link.Source,
                            target = link.Target,
                            linecolor = "white"
                        },
                        classes = "ioncorr_similarity",
                    };
                    edges.Add(edge);
                }
            }
            return edges;
        }

        class LinkTemp {
            public double Score { get; set; }
            public int Source { get; set; }
            public int Target { get; set; }
        }
    }
}
