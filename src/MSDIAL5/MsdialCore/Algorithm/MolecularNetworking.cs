using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Mathematics.Basic;
using CompMs.MsdialCore.DataObj;
using CompMs.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Extension;
using System;

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

        public static List<Edge> GenerateFeatureLinkedEdges<T>(IReadOnlyList<T> spots, IReadOnlyDictionary<int, IonFeatureCharacter> ionFeatures) where T : IMoleculeProperty, IChromatogramPeak {
            var edges = new List<Edge>();
            if (spots.IsEmptyOrNull()) return edges;
            var id2spot = spots.ToDictionary(spot => spot.ID);
            for (int i = 0; i < spots.Count; ++i) {
                var spot = spots[i];
                var ionlinks = ionFeatures[spot.ID].PeakLinks;
                foreach (var peak in ionlinks) {
                    var linkedID = peak.LinkedPeakID;
                    var linkedProp = peak.Character;
                    if (!id2spot.ContainsKey(linkedID)) {
                        continue;
                    }
                    var linkedSpot = id2spot[linkedID];

                    var sourceID = Math.Min(spot.ID, linkedSpot.ID);
                    var targetID = Math.Max(spot.ID, linkedSpot.ID);

                    var type = "null";
                    var annotation = "null";
                    var linecolor = "black";
                    var score = 1.0;
                    var mzdiff = Math.Round(Math.Abs(id2spot[sourceID].Mass - id2spot[targetID].Mass), 5);

                    if (linkedProp == Common.Enum.PeakLinkFeatureEnum.Adduct) {
                        type = "Adduct annotation";
                        annotation = ionFeatures[sourceID].AdductType.ToString() + "_" + ionFeatures[targetID].AdductType.ToString() +
                            "_dm/z" + mzdiff;
                        linecolor = "grey";
                    }
                    else if (linkedProp == Common.Enum.PeakLinkFeatureEnum.FoundInUpperMsMs) {
                        type = "MS2-based annotation";
                        annotation = "The precursor ion in higher m/z's MS/MS; " + "_dm/z" + mzdiff;
                        linecolor = "blue";
                    }
                    else if (linkedProp == Common.Enum.PeakLinkFeatureEnum.ChromSimilar) {
                        type = "Chromatogram-based annotation";
                        annotation = "Similar chromatogram";
                        linecolor = "green";
                    }
                    else if (linkedProp == Common.Enum.PeakLinkFeatureEnum.Isotope) {
                        //type = "Isotope annotation";
                        //annotation = ionFeatures[sourceID].IsotopeWeightNumber + "_" + ionFeatures[targetID].IsotopeWeightNumber +
                        //    "_dm/z" + mzdiff;
                        //linecolor = "orange";
                    }
                    else if (linkedProp == Common.Enum.PeakLinkFeatureEnum.SameFeature) {
                        type = "Same annotation";
                        annotation = "Same annotation feature";
                        linecolor = "black";
                    }
                    var edge = new Edge {
                        data = new EdgeData {
                            score = score,
                            source = sourceID,
                            target = targetID,
                            linecolor = linecolor,
                            comment = annotation
                        },
                        classes = type,
                    };
                    edges.Add(edge);
                }
            }
            var distinctEdges = edges
                .GroupBy(edge => new { edge.data.source, edge.data.target, edge.data.linecolor })
                .Select(group => group.First())
                .ToList();
            return distinctEdges;
        }

        class LinkTemp {
            public double Score { get; set; }
            public int Source { get; set; }
            public int Target { get; set; }
        }
    }
}
