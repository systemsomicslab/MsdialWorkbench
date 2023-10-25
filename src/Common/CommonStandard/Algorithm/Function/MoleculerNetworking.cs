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
using Newtonsoft.Json;

namespace CompMs.Common.Algorithm.Function {

    public class LinkNode {
        public double[] Score { get; set; }
        public IMSScanProperty Node { get; set; }
        public int Index { get; set; }
    }

    //public class CyNode
    //{
    //    public CyNodeData data { get; set; }
    //    public string selected { get; set; }

    //    public CyNode() {
    //        data = new CyNodeData();
    //        selected = "false";
    //    }
    //}

    //public class CyNodeData {
    //    public string id { get; set; }
    //    public string Name { get; set; }
    //    public string Rt { get; set; }
    //    public string Mz { get; set; }
    //    public string Formula { get; set; }
    //    public string Ontology { get; set; }
    //    public string InChiKey { get; set; }
    //    public string Smiles { get; set; }
    //    public int Size { get; set; }
    //    public string bordercolor { get; set; }
    //    public string backgroundcolor { get; set; }
    //}

    //public class CyEdgeData {
    //    public string source { get; set; }
    //    public string target { get; set; }
    //    public double score { get; set; }
    //    public string type { get; set; }
    //}

    //public class CyEdge {
    //    public CyEdgeData data { get; set; }
    //    public string selected { get; set; }
    //    public CyEdge() {
    //        data = new CyEdgeData();
    //        selected = "false";
    //    }
    //}

    //public class RootObject {
    //    public List<CyNode> nodes { get; set; }
    //    public List<CyEdge> edges { get; set; }
    //}

    //internal class RootObject2
    //{
    //    public RootObject elements { get; set; }
    //}

    public sealed class MolecularNetworkingQuery {
        public MsmsSimilarityCalc MsmsSimilarityCalc { get; set; }
        public double MassTolerance { get; set; }
        public double AbsoluteAbundanceCutOff { get; set; }
        public double RelativeAbundanceCutOff { get; set; }
        public double SpectrumSimilarityCutOff { get; set; }
        public double MinimumPeakMatch { get; set; }
        public double MaxEdgeNumberPerNode { get; set; }
        public double MaxPrecursorDifference { get; set; }
        public double MaxPrecursorDifferenceAsPercent { get; set; }
    }

    public sealed class MoleculerNetworkingBase {
        public static void ExportNodesEdgesFiles(string folder, RootObject rootObj) {

            var nodes = rootObj.nodes;
            var edges = rootObj.edges;

            var dt = DateTime.Now;
            var nodepath = Path.Combine(folder, $"node-{dt:yyMMddhhmm}.txt");
            var edgepath = Path.Combine(folder, $"edge-{dt:yyMMddhhmm}.txt");
            var cypath = Path.Combine(folder, $"cyelements-{dt:yyMMddhhmm}.js");
        

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

            var rootCy = new RootObj4Cytoscape() { elements = rootObj };
            using (StreamWriter sw = new StreamWriter(cypath, false, Encoding.ASCII)) {
                var json = JsonConvert.SerializeObject(rootCy, Formatting.Indented);
                sw.WriteLine(json.ToString());
            }
        }

        public static void SendToCytoscapeJs(RootObject rootObj) {
            if (rootObj.nodes.IsEmptyOrNull() || rootObj.edges.IsEmptyOrNull()) return;
            var curDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var cytoDir = Path.Combine(curDir, "CytoscapeLocalBrowser");
            var url = Path.Combine(cytoDir, "MsdialCytoscapeViewer.html");
            var cyjsexportpath = Path.Combine(Path.Combine(cytoDir, "data"), "elements.js");

            var counter = 0;
            var edges = new List<Edge>();
            var nodekeys = new List<int>();
            foreach (var edge in rootObj.edges.OrderByDescending(n => n.data.score)) {
                if (counter > 3000) break;
                edges.Add(edge);

                if (!nodekeys.Contains(edge.data.source))
                    nodekeys.Add(edge.data.source);
                if (!nodekeys.Contains(edge.data.target))
                    nodekeys.Add(edge.data.target);

                counter++;
            }

            var nodes = new List<Node>();
            foreach (var node in rootObj.nodes.Where(n => n.data.MsmsMin > 0)) {
                if (nodekeys.Contains(node.data.id))
                    nodes.Add(node);
            }
            var nRootObj = new RootObject() { nodes = nodes, edges = edges };

            var json = JsonConvert.SerializeObject(nRootObj, Formatting.Indented);
            using (StreamWriter sw = new StreamWriter(cyjsexportpath, false, Encoding.ASCII)) {
                sw.WriteLine("var dataElements =\r\n" + json.ToString() + "\r\n;");
            }
            System.Diagnostics.Process.Start(url);
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

        public RootObject GetMoleculerNetworkingRootObjZZZ<T>(IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans, MsmsSimilarityCalc msmsSimilarityCalc, double masstolerance, double absoluteAbsCutoff, double relativeAbsCutoff, double spectrumSimilarityCutoff, double minimumPeakMatch, double maxEdgeNumberPerNode, double maxPrecursorDifference, double maxPrecursorDifferenceAsPercent, Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {
            var nodes = GetSimpleNodes(spots, scans);
            var edges = GenerateEdgesBySpectralSimilarity(
                    spots, scans, msmsSimilarityCalc, masstolerance,
                    absoluteAbsCutoff, relativeAbsCutoff, spectrumSimilarityCutoff,
                    minimumPeakMatch, maxEdgeNumberPerNode, maxPrecursorDifference, maxPrecursorDifferenceAsPercent, report);
            return new RootObject() { nodes = nodes, edges = edges };
        }

        public RootObject GetMoleculerNetworkingRootObjZZZ<T>(IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans, MolecularNetworkingQuery query, Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {
            var nodes = GetSimpleNodes(spots, scans);
            var edges = GenerateEdgesBySpectralSimilarity(
                    spots, scans, query.MsmsSimilarityCalc, query.MassTolerance,
                    query.AbsoluteAbundanceCutOff, query.RelativeAbundanceCutOff, query.SpectrumSimilarityCutOff,
                    query.MinimumPeakMatch, query.MaxEdgeNumberPerNode, query.MaxPrecursorDifference, query.MaxPrecursorDifferenceAsPercent, report);
            return new RootObject() { nodes = nodes, edges = edges };
        }

        public static RootObject GetMoleculerNetworkingRootObj<T>(IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans,
            MsmsSimilarityCalc msmsSimilarityCalc, double masstolerance, double absoluteAbsCutoff, double relativeAbsCutoff, double spectrumSimilarityCutoff,
            double minimumPeakMatch, double maxEdgeNumberPerNode, double maxPrecursorDifference, double maxPrecursorDifferenceAsPercent, Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {
            var network = new MoleculerNetworkingBase();
            return network.GetMoleculerNetworkingRootObjZZZ(spots, scans, msmsSimilarityCalc, masstolerance, absoluteAbsCutoff, relativeAbsCutoff, spectrumSimilarityCutoff, minimumPeakMatch, maxEdgeNumberPerNode, maxPrecursorDifference, maxPrecursorDifferenceAsPercent, report);
        }

        public static RootObject GetMoleculerNetworkingRootObjForTargetSpot<T>(
            T targetSpot, IMSScanProperty targetScan, IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans,
            MsmsSimilarityCalc msmsSimilarityCalc, double masstolerance, double absoluteAbsCutoff, double relativeAbsCutoff, double spectrumSimilarityCutoff,
            double minimumPeakMatch, double maxEdgeNumberPerNode, double maxPrecursorDifference, double maxPrecursorDifferenceAsPercent, Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {

            var edges = GenerateEdgesBySpectralSimilarity(
                targetSpot, targetScan,
                spots, scans, msmsSimilarityCalc, masstolerance,
                absoluteAbsCutoff, relativeAbsCutoff, spectrumSimilarityCutoff,
                minimumPeakMatch, maxEdgeNumberPerNode, maxPrecursorDifference, maxPrecursorDifferenceAsPercent, report);

            var idlist = new List<int>();
            foreach (var edge in edges) {
                var sID = edge.data.source;
                var tID = edge.data.target;
                if (!idlist.Contains(sID)) idlist.Add(sID);
                if (!idlist.Contains(tID)) idlist.Add(tID);
            }
            var nSpots = new List<T>();
            var nScans = new List<IMSScanProperty>();

            for (int i = 0; i < spots.Count; i++) {
                if (idlist.Contains(spots[i].ID)) {
                    nSpots.Add(spots[i]);
                    nScans.Add(scans[i]);
                }
            }

            var nodes = GetSimpleNodes(nSpots, nScans);
            return new RootObject() { nodes = nodes, edges = edges };
        }

        private static List<Node> GetSimpleNodes<T>(IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans) where T : IMoleculeProperty, IChromatogramPeak {
            var nodes = new List<Node>();
            if (spots.IsEmptyOrNull()) return nodes;
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
                        Method = "MSMS",
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

        private static List<Edge> GenerateEdgesBySpectralSimilarity<T>(IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans,
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

        private static List<Edge> GenerateEdgesBySpectralSimilarity<T>(
            T targetSpot, IMSScanProperty targetScan, IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans,
            MsmsSimilarityCalc msmsSimilarityCalc, double masstolerance, double absoluteAbsCutoff, double relativeAbsCutoff, double spectrumSimilarityCutoff,
            double minimumPeakMatch, double maxEdgeNumberPerNode, double maxPrecursorDifference, double maxPrecursorDifferenceAsPercent, Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {
            if (targetScan.Spectrum.IsEmptyOrNull()) return new List<Edge>();
            targetScan.Spectrum = MsScanMatching.GetProcessedSpectrum(targetScan.Spectrum, targetScan.PrecursorMz, absoluteAbundanceCutOff: absoluteAbsCutoff, relativeAbundanceCutOff: relativeAbsCutoff);
            if (targetScan.Spectrum.IsEmptyOrNull()) return new List<Edge>();

            foreach (var scan in scans) {
                if (scan.Spectrum.Count > 0)
                    scan.Spectrum = MsScanMatching.GetProcessedSpectrum(scan.Spectrum, scan.PrecursorMz, absoluteAbundanceCutOff: absoluteAbsCutoff, relativeAbundanceCutOff: relativeAbsCutoff);
            }

            var edges = GenerateEdges(targetSpot, targetScan, spots, scans, masstolerance, minimumPeakMatch, spectrumSimilarityCutoff,
            maxEdgeNumberPerNode, maxPrecursorDifference, maxPrecursorDifferenceAsPercent, msmsSimilarityCalc == MsmsSimilarityCalc.Bonanza ? true : false, report);

            return edges.Select(n => new Edge() { data = n, classes = "ms_similarity" }).ToList();
        }

        private static List<EdgeData> GenerateEdges<T>(
            T targetSpot,
            IMSScanProperty targetScan,
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
            var links = new List<LinkNode>();
            Debug.WriteLine(peaks.Count);
            for (int i = 0; i < peaks.Count; i++) {
                if (peaks[i].Spectrum.Count <= 0) continue;
                if (peaks[i].ScanID == targetScan.ScanID) continue;
                counter++;
                Debug.WriteLine("{0} / {1}", counter, max);
                report?.Invoke(counter / (double)max);
                double[] scoreitem = GetLinkNode(targetScan, peaks[i], maxPrecursorDiff, massTolerance, minimumPeakMatch, matchThreshold, isBonanza);
                if (scoreitem == null) continue;
                links.Add(new LinkNode() { Score = scoreitem, Node = peaks[i], Index = i });
            }

            foreach (var link in links) {
                var source_node_id = targetSpot.ID;
                var target_node_id = spots[link.Index].ID;

                var edge = new EdgeData() {
                    score = link.Score[0], matchpeakcount = link.Score[1], source = source_node_id, target = target_node_id
                };
                edges.Add(edge);
            }
            return edges;
        }

        private static List<EdgeData> GenerateEdges<T>(
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

                node2links[i] = new List<LinkNode>();
                for (int j = i + 1; j < peaks.Count; j++) {
                    if (peaks[j].Spectrum.Count <= 0) continue;
                    double[] scoreitem = GetLinkNode(peaks[i], peaks[j], maxPrecursorDiff, massTolerance, minimumPeakMatch, matchThreshold, isBonanza);
                    if (scoreitem == null) continue;
                    node2links[i].Add(new LinkNode() { Score = scoreitem, Node = peaks[j], Index = j });
                }
            }

            var cNode2Links = new Dictionary<int, List<LinkNode>>();
            foreach (var item in node2links) {
                if (item.Value.IsEmptyOrNull()) continue;
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

        private static double[] GetLinkNode(IMSScanProperty prop1, IMSScanProperty prop2, double maxPrecursorDiff, double massTolerance, double minimumPeakMatch, double matchThreshold, bool isBonanza) {
            var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
            if (massDiff > maxPrecursorDiff) return null;
            // if (Math.Max(prop1.PrecursorMz, prop2.PrecursorMz) * maxPrecursorDiff_Percent * 0.01 - Math.Min(prop1.PrecursorMz, prop2.PrecursorMz) < 0) continue;
            double[] scoreitem = new double[2];
            if (isBonanza) {
                scoreitem = MsScanMatching.GetBonanzaScore(prop1, prop2, massTolerance);
            }
            else {
                scoreitem = MsScanMatching.GetModifiedDotProductScore(prop1, prop2, massTolerance);
            }
            if (scoreitem[1] < minimumPeakMatch) return null; 
            if (scoreitem[0] < matchThreshold * 0.01) return null;

            return scoreitem;
        }

        public static List<EdgeData> GenerateEdges(
           IReadOnlyList<IMoleculeMsProperty> peaks1,
           IReadOnlyList<IMoleculeMsProperty> peaks2,
           double massTolerance,
           double minimumPeakMatch,
           double matchThreshold,
           double maxEdgeNumPerNode,
           double maxPrecursorDiff,
           double maxPrecursorDiff_Percent,
           bool isBonanza,
           Action<double> report) {

            var edges = new List<EdgeData>();
            var counter = 0;
            var max = peaks1.Count;
            var node2links = new Dictionary<int, List<LinkNode>>();
            Console.WriteLine("Query1 {0}, Query2 {1}, Total {2}", peaks1.Count, peaks2.Count, peaks1.Count * peaks2.Count);
            for (int i = 0; i < peaks1.Count; i++) {
                if (peaks1[i].Spectrum.Count <= 0) continue;
                counter++;
                report?.Invoke(counter / (double)max);
                if (counter % 100 == 0) {
                    Console.Write("{0} / {1}", counter, max);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }

                for (int j = 0; j < peaks2.Count; j++) {
                    if (peaks2[j].Spectrum.Count <= 0) continue;
                    var prop1 = peaks1[i];
                    var prop2 = peaks2[j];
                    var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
                    if (massDiff > maxPrecursorDiff) continue;
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
                        node2links[i].Add(new LinkNode() { Score = scoreitem, Node = peaks2[j], Index = j });
                    }
                    else {
                        node2links[i] = new List<LinkNode>() { new LinkNode() { Score = scoreitem, Node = peaks2[j], Index = j } };
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
                    var source_node_id = peaks1[item.Key].ScanID;
                    var target_node_id = peaks2[link.Index].ScanID;

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
