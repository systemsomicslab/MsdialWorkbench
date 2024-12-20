using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.Common.Algorithm.Function
{

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
        public MolecularNetworkInstance GetMolecularNetworkInstance<T>(IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans, MolecularNetworkingQuery query, Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {
            List<PeakScanPair<T>> peakScans = spots.Zip(scans, (spot, scan) => new PeakScanPair<T>(spot, scan)).ToList();
            var nodes = GetSimpleNodes(peakScans);
            RefineScans(scans, query);
            var edges = GenerateEdgesBySpectralSimilarity(peakScans, query, report);
            return new MolecularNetworkInstance(new RootObject { nodes = nodes, edges = edges });
        }

        public MolecularNetworkInstance GetMoleculerNetworkInstanceForTargetSpot<T>(T targetSpot, IMSScanProperty targetScan, IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans, MolecularNetworkingQuery query, Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {
            List<PeakScanPair<T>> peakScans = spots.Zip(scans, (spot, scan) => new PeakScanPair<T>(spot, scan)).ToList();
            var nodes = GetSimpleNodes(peakScans);
            if (scans.All(s => s.ScanID != targetScan.ScanID)) {
                RefineScans(new[] { targetScan }, query);
                if (targetScan.Spectrum.IsEmptyOrNull()) {
                    return new MolecularNetworkInstance(new RootObject { nodes = new List<Node>(0), edges = new List<Edge>(0), });
                }
            }
            RefineScans(scans, query);
            var edges = GenerateEdgesBySpectralSimilarity(new PeakScanPair<T>(targetSpot, targetScan), peakScans, query, report);
            return new MolecularNetworkInstance(new RootObject { nodes = nodes, edges = edges });
        }

        private static List<Node> GetSimpleNodes<T>(List<PeakScanPair<T>> peakScans) where T : IMoleculeProperty, IChromatogramPeak {
            if (peakScans.IsEmptyOrNull()) {
                return new List<Node>(0);
            }
            var minValue = Math.Log10(peakScans.Min(n => n.Peak.Intensity));
            var maxValue = Math.Log10(peakScans.Max(n => n.Peak.Intensity));
            return peakScans.Select(peakScan => GetSimpleNode(peakScan, minValue, maxValue)).ToList();
        }

        private static Node GetSimpleNode<T>(PeakScanPair<T> peakScanPair, double minValue, double maxValue) where T : IMoleculeProperty, IChromatogramPeak {
            var spot = peakScanPair.Peak;
            IMSScanProperty scan = peakScanPair.Scan;
            return new Node
            {
                data = new NodeData
                {
                    id = spot.ID,
                    Name = spot.Name,
                    Rt = spot.ChromXs.RT.Value.ToString(),
                    Mz = spot.Mass.ToString(),
                    Method = "MSMS",
                    Property = $"RT {Math.Round(spot.ChromXs.RT.Value, 3)}_m/z {Math.Round(spot.Mass, 5)}",
                    Formula = spot.Formula?.FormulaString??string.Empty,
                    InChiKey = spot.InChIKey,
                    Ontology = spot.Ontology,
                    Smiles = spot.SMILES,
                    Size = (int)((Math.Log10(spot.Intensity) - minValue) / (maxValue - minValue) * 100 + 20),
                    bordercolor = "white",
                    backgroundcolor = GetOntologyColor(spot),
                    MSMS = scan.Spectrum.Select(spec => new List<double> { spec.Mass, spec.Intensity }).ToList(),
                    MsmsMin = scan.Spectrum.FirstOrDefault()?.Mass ?? 0d,
                    MsMsLabel = scan.Spectrum.Select(spec => Math.Round(spec.Mass, 5).ToString()).ToList(),
                },
            };
        }

        private static string GetOntologyColor<T>(T spot) where T : IMoleculeProperty, IChromatogramPeak {
            var isCharacterized = !spot.Name.IsEmptyOrNull() && !spot.Name.Contains("Unknown") && !spot.Name.Contains("w/o MS2") && !spot.Name.Contains("RIKEN");
            if (isCharacterized && MetaboliteColorCode.metabolite_colorcode.TryGetValue(spot.Ontology, out var backgroundcolor)) {
                return backgroundcolor;
            }
            return "rgb(0,0,0)";
        }

        private static void RefineScans(IEnumerable<IMSScanProperty> scans, MolecularNetworkingQuery query) {
            foreach (var scan in scans) {
                if (scan.Spectrum.Count > 0) {
                    scan.Spectrum = MsScanMatching.GetProcessedSpectrum(scan.Spectrum, scan.PrecursorMz, absoluteAbundanceCutOff: query.AbsoluteAbundanceCutOff, relativeAbundanceCutOff: query.RelativeAbundanceCutOff);
                }
            }
        }

        private static List<Edge> GenerateEdgesBySpectralSimilarity<T>(List<PeakScanPair<T>> peakScans, MolecularNetworkingQuery query, Action<double> report) where T:IMoleculeProperty, IChromatogramPeak {
            var edges = GenerateEdges(peakScans, peakScans, query, report);
            var counts = new Dictionary<int, int>();
            foreach (var edge in edges) {
                counts[edge.source] = counts[edge.target] = 0;
            }
            var filteredEdges = new List<EdgeData>();
            foreach (var edge in edges.OrderByDescending(edge => edge.score)) {
                if (counts[edge.source] < query.MaxEdgeNumberPerNode && counts[edge.target] < query.MaxEdgeNumberPerNode) {
                    ++counts[edge.source];
                    ++counts[edge.target];
                    filteredEdges.Add(edge);
                }
            }
            return filteredEdges.Select(edge => new Edge { data = edge, classes = "ms_similarity" }).ToList();
        }

        private static List<Edge> GenerateEdgesBySpectralSimilarity<T>(PeakScanPair<T> targetPeakScan, List<PeakScanPair<T>> peakScans, MolecularNetworkingQuery query, Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {
            if (targetPeakScan.Scan.Spectrum.IsEmptyOrNull()) {
                return new List<Edge>(0);
            }
            var edges = GenerateEdges(new List<PeakScanPair<T>> { targetPeakScan }, peakScans, query, report);
            return edges.Select(edge => new Edge() { data = edge, classes = "ms_similarity" }).ToList();
        }

        

        private static List<EdgeData> GenerateEdges<T>(List<PeakScanPair<T>> srcPeakScans, List<PeakScanPair<T>> dstPeakScans, MolecularNetworkingQuery query, Action<double> report) where T : IMoleculeProperty, IChromatogramPeak {
            var counter = 0;
            var max = srcPeakScans.Count * dstPeakScans.Count;
            var edges = new List<EdgeData>();
            var checkedPeaks = new HashSet<int>[new[] { srcPeakScans, dstPeakScans }.SelectMany(pss => pss, (_, ps) => ps.Peak.ID).DefaultIfEmpty().Max() + 1];
            for (int i = 0; i < srcPeakScans.Count; i++) {
                var srcPeakScan = srcPeakScans[i];

                if (srcPeakScan.Scan.Spectrum.Count <= 0) {
                    counter += dstPeakScans.Count;
                    report?.Invoke(counter / (double)max);
                    continue;
                }
                var srcCheckedPeaks = checkedPeaks[srcPeakScan.Peak.ID] ?? (checkedPeaks[srcPeakScan.Peak.ID] = new HashSet<int>());

                for (int j = 0; j < dstPeakScans.Count; j++) {
                    PeakScanPair<T> dstPeakScan = dstPeakScans[j];
                    counter++;
                    report?.Invoke(counter / (double)max);
                    if (dstPeakScan.Scan.Spectrum.Count <= 0) continue;

                    var dstCheckedPeaks = checkedPeaks[dstPeakScan.Peak.ID] ?? (checkedPeaks[dstPeakScan.Peak.ID] = new HashSet<int>());
                    if (srcPeakScan.Peak.ID == dstPeakScan.Peak.ID || srcCheckedPeaks.Contains(dstPeakScan.Peak.ID)) {
                        continue;
                    }
                    srcCheckedPeaks.Add(dstPeakScan.Peak.ID);
                    dstCheckedPeaks.Add(srcPeakScan.Peak.ID);

                    double[] scoreitem = CalculateEdgeScore(srcPeakScan.Scan, dstPeakScan.Scan, query);
                    if (scoreitem is null) continue;
                    edges.Add(new EdgeData
                    {
                        score = Math.Round(scoreitem[0], 3),
                        matchpeakcount = scoreitem[1],
                        source = srcPeakScan.Peak.ID,
                        target = dstPeakScan.Peak.ID,
                        linecolor = "red",
                    });
                }
            }
            return edges;
        }

        private static double[] CalculateEdgeScore(IMSScanProperty prop1, IMSScanProperty prop2, MolecularNetworkingQuery query) {
            var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
            if (massDiff > query.MaxPrecursorDifference) return null;
            // if (Math.Max(prop1.PrecursorMz, prop2.PrecursorMz) * maxPrecursorDiff_Percent * 0.01 - Math.Min(prop1.PrecursorMz, prop2.PrecursorMz) < 0) continue;
            double[] scoreitem = new double[2];
            switch (query.MsmsSimilarityCalc) {
                case MsmsSimilarityCalc.Bonanza:
                    scoreitem = MsScanMatching.GetBonanzaScore(prop1, prop2, query.MassTolerance);
                    break;
                case MsmsSimilarityCalc.ModDot:
                    scoreitem = MsScanMatching.GetModifiedDotProductScore(prop1, prop2, query.MassTolerance);
                    break;
            }
            if (scoreitem[1] < query.MinimumPeakMatch) return null; 
            if (scoreitem[0] < query.SpectrumSimilarityCutOff * 0.01) return null;

            return scoreitem;
        }

        public static EdgeData GetEdge(
           IMoleculeMsProperty peak1,
           IMoleculeMsProperty peak2,
           double massTolerance,
           double minimumPeakMatch,
           double matchThreshold,
           double maxEdgeNumPerNode,
           double maxPrecursorDiff,
           double maxPrecursorDiff_Percent,
           MsmsSimilarityCalc msmsSimilarityCalc) {
           if (peak1.Spectrum.Count <= 0 || peak2.Spectrum.Count <= 0) return null;
            var massDiff = Math.Abs(peak1.PrecursorMz - peak2.PrecursorMz);
            if (massDiff > maxPrecursorDiff) return null; 

            var scoreitem = GetMsnScoreItems(peak1, peak2, massTolerance, msmsSimilarityCalc);
            if (scoreitem == null) return null;
            if (scoreitem[1] < minimumPeakMatch) return null;
            if (scoreitem[0] < matchThreshold * 0.01) return null;

            var edge = new EdgeData() {
                score = scoreitem[0], matchpeakcount = scoreitem[1], source = peak1.ScanID, target = peak2.ScanID,
                scores = scoreitem
            };
            return edge;
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
           MsmsSimilarityCalc msmsSimilarityCalc,
           Action<double> report) {

            var edges = new List<EdgeData>();
            var counter = 0;
            var max = peaks1.Count;
            var node2links = new Dictionary<int, List<LinkNode>>();
            for (int i = 0; i < peaks1.Count; i++) {
                if (peaks1[i].Spectrum.Count <= 0) continue;
                counter++;
                report?.Invoke(counter / (double)max);
                for (int j = 0; j < peaks2.Count; j++) {
                    if (peaks2[j].Spectrum.Count <= 0) continue;
                    var prop1 = peaks1[i];
                    var prop2 = peaks2[j];
                    var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
                    if (massDiff > maxPrecursorDiff) continue;

                    var scoreitem = new List<double>();
                    scoreitem = GetMsnScoreItems(prop1, prop2, massTolerance, msmsSimilarityCalc);
                    if (scoreitem == null) continue;
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
                        score = link.Score[0], matchpeakcount = link.Score[1], source = source_node_id, target = target_node_id,
                        scores = link.Score
                    };
                    edges.Add(edge);
                }
            }
            return edges;
        }

        public static void ExportAllEdges(
          string outputfile,
          string inputA, string inputB,
          IReadOnlyList<IMoleculeMsProperty> peaks1,
          IReadOnlyList<IMoleculeMsProperty> peaks2,
          double massTolerance,
          double minimumPeakMatch,
          double matchThreshold,
          double maxEdgeNumPerNode,
          double maxPrecursorDiff,
          double maxPrecursorDiff_Percent,
          MsmsSimilarityCalc msmsSimilarityCalc,
          Action<double> report) {
            var counter = 0;
            var max = peaks1.Count;
            using (var sw = new StreamWriter(outputfile, false)) {
                if (msmsSimilarityCalc == MsmsSimilarityCalc.All) {
                    sw.WriteLine("SourceID: {0}\tTargetID: {1}\tBonanzaScore\tMatchPeakCount\tModDotScore\tCosineScore", inputA, inputB);
                }
                else {
                    sw.WriteLine("SourceID: {0}\tTargetID: {1}\tSimilarityScore\tMatchPeakCount", inputA, inputB);
                }

                var isABMatched = inputA == inputB;
                if (isABMatched) {
                    for (int i = 0; i < peaks1.Count; i++) {
                        if (peaks1[i].Spectrum.Count <= 0) continue;
                        counter++;
                        report?.Invoke(counter / (double)max);
                        for (int j = i + 1; j < peaks1.Count; j++) {
                            if (peaks1[j].Spectrum.Count <= 0) continue;
                            var prop1 = peaks1[i];
                            var prop2 = peaks1[j];
                            var edge = GetEdge(prop1, prop2, massTolerance, minimumPeakMatch, matchThreshold, maxEdgeNumPerNode, maxPrecursorDiff, maxPrecursorDiff_Percent, msmsSimilarityCalc);
                            if (edge != null)
                                sw.WriteLine(edge.source + "\t" + edge.target + "\t" + String.Join("\t", edge.scores));
                        }
                    }
                }
                else {
                    for (int i = 0; i < peaks1.Count; i++) {
                        if (peaks1[i].Spectrum.Count <= 0) continue;
                        counter++;
                        report?.Invoke(counter / (double)max);
                        for (int j = 0; j < peaks2.Count; j++) {
                            if (peaks2[j].Spectrum.Count <= 0) continue;
                            var prop1 = peaks1[i];
                            var prop2 = peaks2[j];
                            var edge = GetEdge(prop1, prop2, massTolerance, minimumPeakMatch, matchThreshold, maxEdgeNumPerNode, maxPrecursorDiff, maxPrecursorDiff_Percent, msmsSimilarityCalc);
                            if (edge != null)
                                sw.WriteLine(edge.source + "\t" + edge.target + "\t" + String.Join("\t", edge.scores));
                        }
                    }
                }
            }
        }

        private static List<double> GetMsnScoreItems(IMoleculeMsProperty prop1, IMoleculeMsProperty prop2, double massTolerance, MsmsSimilarityCalc msmsSimilarityCalc) {
            if (msmsSimilarityCalc == MsmsSimilarityCalc.Bonanza) {
                return MsScanMatching.GetBonanzaScore(prop1, prop2, massTolerance).ToList();
            }
            else if (msmsSimilarityCalc == MsmsSimilarityCalc.ModDot) {
                return MsScanMatching.GetModifiedDotProductScore(prop1, prop2, massTolerance).ToList();
            }
            else if (msmsSimilarityCalc == MsmsSimilarityCalc.Cosine) {
                return MsScanMatching.GetCosineScore(prop1, prop2, massTolerance).ToList();
            }
            else if (msmsSimilarityCalc == MsmsSimilarityCalc.All) {
                return MsScanMatching.GetBonanzaModifiedDotCosineScores(prop1, prop2, massTolerance).ToList();
            }
            return null;
        }

        class PeakScanPair<T> {
            public PeakScanPair(T peak, IMSScanProperty scan) {
                Peak = peak;
                Scan = scan;
            }

            public T Peak { get; }
            public IMSScanProperty Scan { get; }
        }

        class LinkNode {
            public List<double> Score { get; set; } = new List<double>();
            public IMSScanProperty Node { get; set; }
            public int Index { get; set; }
        }
    }
}
