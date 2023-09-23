using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.Algorithm.Function {

    public class LinkNode {
        public double[] Score { get; set; }
        public IMSScanProperty Node { get; set; }
        public int Index { get; set; }
    }

    public sealed class MoleculerNetworkingBase {
        private MoleculerNetworkingBase() { }

        public static void ExportNodesEdgesFiles(string folder, IReadOnlyList<Node> nodes, IReadOnlyList<Edge> edges) {
            var dt = DateTime.Now;
            var nodepath = Path.Combine(folder, $"node-{dt:yyMMddhhmm}.txt");
            var edgepath = Path.Combine(folder, $"edge-{dt:yyMMddhhmm}.txt");

            using (StreamWriter sw = new StreamWriter(nodepath, false, Encoding.ASCII)) {

                sw.WriteLine("ID\tMetaboliteName\tRt\tMz\tFormula\tOntology\tInChIKey\tSMILES\tSize\tBorderColor\tBackgroundColor\tMs2");
                foreach (var nodeObj in nodes) {
                    var node = nodeObj.data;
                    sw.Write(node.id + "\t" + node.Name + "\t" + node.Rt + "\t" + node.Mz + "\t" + node.Formula + "\t" + node.Ontology + "\t" +
                       node.InChiKey + "\t" + node.Smiles + "\t" + node.Size + "\t" + node.bordercolor + "\t" + node.backgroundcolor + "\t");

                    var ms2String = getMsString(node.MSMS);
                    sw.WriteLine(ms2String);
                }
            }

            using (StreamWriter sw = new StreamWriter(edgepath, false, Encoding.ASCII)) {

                sw.WriteLine("SourceID\tTargetID\tScore\tType");
                foreach (var edgeObj in edges) {
                    var edge = edgeObj.data;
                    sw.WriteLine(edge.source + "\t" + edge.target + "\t" + edge.score + "\t" + edgeObj.classes);
                }
            }
        }

        private static string getMsString(List<List<double>> msList) {
            if (msList == null || msList.Count == 0) return string.Empty;

            var specString = string.Empty;

            for (int i = 0; i < msList.Count; i++) {
                var mz = msList[i][0];
                var intensity = msList[i][1];

                if (i == msList.Count - 1)
                    specString += Math.Round(mz, 5).ToString() + ":" + Math.Round(intensity, 0).ToString();
                else
                    specString += Math.Round(mz, 5).ToString() + ":" + Math.Round(intensity, 0).ToString() + " ";
            }

            return specString;
        }

        public static RootObject GetMoleculerNetworkingRootObj<T>(IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans,
            MsmsSimilarityCalc msmsSimilarityCalc, double masstolerance, double absoluteAbsCutoff, double relativeAbsCutoff, double spectrumSimilarityCutoff,
            double minimumPeakMatch, double maxEdgeNumberPerNode, double maxPrecursorDifference, double maxPrecursorDifferenceAsPercent, Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {
            var nodes = GetSimpleNodes(spots, scans);
            var edges = GenerateEdgesBySpectralSimilarity(
                    spots, scans, msmsSimilarityCalc, masstolerance,
                    absoluteAbsCutoff, relativeAbsCutoff, spectrumSimilarityCutoff,
                    minimumPeakMatch, maxEdgeNumberPerNode, maxPrecursorDifference, maxPrecursorDifferenceAsPercent, report);
            return new RootObject() { nodes = nodes, edges = edges };
        }

        public static List<Node> GetSimpleNodes<T>(IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans) where T : IMoleculeProperty, IChromatogramPeak {
            var nodes = new List<Node>();
            var minValue = Math.Log10(spots.Min(n => n.Intensity));
            var maxValue = Math.Log10(spots.Max(n => n.Intensity));
            for (int i = 0; i < spots.Count; i++) {
                var spot = spots[i];
                var node = new Node() {
                    //classes = "fuchsia b_pink hits",
                    data = new NodeData() {
                        id = spot.ID,
                        Name = spot.Name,
                        Rt = spot.ChromXs.RT.Value.ToString(),
                        Mz = spot.Mass.ToString(),
                        Property = "RT " + Math.Round(spot.ChromXs.RT.Value, 3).ToString() + "_m/z " + Math.Round(spot.Mass, 5).ToString(),
                        Formula = spot.Formula.FormulaString,
                        InChiKey = spot.InChIKey,
                        Ontology = spot.Ontology,
                        Smiles = spot.SMILES,
                        Size = (int)((Math.Log10(spot.Intensity) - minValue) / (maxValue - minValue) * 100 + 20),
                        bordercolor = "white"
                    },
                };
                var isCharacterized = !spot.Name.IsEmptyOrNull() && !spot.Name.Contains("Unknown") && !spot.Name.Contains("w/o MS2") && !spot.Name.Contains("RIKEN") ? true : false;
                var backgroundcolor = "rgb(0,0,0)";
                if (isCharacterized && MetaboliteColorCode.metabolite_colorcode.ContainsKey(node.data.Ontology))
                    backgroundcolor = MetaboliteColorCode.metabolite_colorcode[node.data.Ontology];
                node.data.backgroundcolor = backgroundcolor;

                var ms2spec = new List<List<double>>();
                var ms2label = new List<string>();

                foreach (var spec in scans[i].Spectrum) {
                    ms2spec.Add(new List<double>() { spec.Mass, spec.Intensity });
                    ms2label.Add(Math.Round(spec.Mass, 5).ToString());
                }
                node.data.MSMS = ms2spec;
                node.data.MsmsMin = ms2spec.IsEmptyOrNull() ? 0.0 : ms2spec[0][0];
                node.data.MsMsLabel = ms2label;
                nodes.Add(node);
            }
            return nodes;
        }

        public static List<Edge> GenerateEdgesBySpectralSimilarity<T>(IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans,
            MsmsSimilarityCalc msmsSimilarityCalc, double masstolerance, double absoluteAbsCutoff, double relativeAbsCutoff, double spectrumSimilarityCutoff,
            double minimumPeakMatch, double maxEdgeNumberPerNode, double maxPrecursorDifference, double maxPrecursorDifferenceAsPercent, Action<double> report) where T:IMoleculeProperty, IChromatogramPeak {
            foreach (var scan in scans) {
                if (scan.Spectrum.Count > 0)
                    scan.Spectrum = MsScanMatching.GetProcessedSpectrum(scan.Spectrum, scan.PrecursorMz, absoluteAbundanceCutOff: absoluteAbsCutoff, relativeAbundanceCutOff: relativeAbsCutoff);
            }
            var edges = GenerateEdges(spots, scans, masstolerance, minimumPeakMatch, spectrumSimilarityCutoff, 
            maxEdgeNumberPerNode, maxPrecursorDifference, maxPrecursorDifferenceAsPercent, msmsSimilarityCalc == MsmsSimilarityCalc.Bonanza ? true : false, report);

            return edges.Select(n => new Edge() { data = n, classes = "ms_similarity" }).ToList();
        }

        public static List<EdgeData> GenerateEdges<T>(
            IReadOnlyList<T> spots,
            IReadOnlyList<IMSScanProperty> peaks, 
            double massTolerance,
            double minimumPeakMatch, 
            double matchThreshold, 
            double maxEdgeNumPerNode,
            double maxPrecursorDiff,
            double maxPrecursorDiff_Percent,
            bool isBonanza,
            Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {

            var edges = new List<EdgeData>();
            var counter = 0;
            var max = peaks.Count;
            var node2links = new Dictionary<int, List<LinkNode>>();
            Debug.WriteLine(peaks.Count);
            for (int i = 0; i < peaks.Count; i++) {
                if (peaks[i].Spectrum.Count <= 0) continue;
                counter++;
                Debug.WriteLine("{0} / {1}", counter, max);
                report?.Invoke(counter / (double)max);
                for (int j = i + 1; j < peaks.Count; j++) {
                    if (peaks[j].Spectrum.Count <= 0) continue;
                    var prop1 = peaks[i];
                    var prop2 = peaks[j];
                    var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
                    if (massDiff > maxPrecursorDiff) continue;
                   // if (Math.Max(prop1.PrecursorMz, prop2.PrecursorMz) * maxPrecursorDiff_Percent * 0.01 - Math.Min(prop1.PrecursorMz, prop2.PrecursorMz) < 0) continue;
                    double[] scoreitem = new double[2];
                    if (isBonanza) {
                        scoreitem = MsScanMatching.GetBonanzaScore(prop1, prop2, massTolerance);
                    }
                    else {
                        scoreitem = MsScanMatching.GetModifiedDotProductScore(prop1, prop2, massTolerance);
                    }
                    if (scoreitem[1] < minimumPeakMatch) continue;
                    if (scoreitem[0] < matchThreshold * 0.01) continue;

                    if (node2links.ContainsKey(i)) {
                        node2links[i].Add(new LinkNode() { Score = scoreitem, Node = peaks[j], Index = j });
                    }
                    else {
                        node2links[i] = new List<LinkNode>() { new LinkNode() { Score = scoreitem, Node = peaks[j], Index = j } };
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
                    var source_node_id = spots[item.Key].ID;
                    var target_node_id = spots[link.Index].ID;

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
