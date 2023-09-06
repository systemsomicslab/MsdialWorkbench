using CompMs.App.MsdialConsole.EadSpectraAnalysis;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.MolecularNetwork
{
    internal class MoleculerInput
    {
        private MoleculerInput() { }

        public static void Which() {
            Console.WriteLine("VS Raw ->0   VS Ref ->1");
            string type = Console.ReadLine();
            Console.WriteLine("input minimumPeakMatch");
            string match = Console.ReadLine();
            Console.WriteLine("input scoreThreshold(0-1)");
            string threshold = Console.ReadLine();
            Console.WriteLine("input maxEdgeperNode");
            string node = Console.ReadLine();

            var matchs = double.Parse(match);
            var thresholds = double.Parse(threshold);
            var nodes = double.Parse(node);

            if (type == "0") {
                Console.WriteLine("input file");
                string input = Console.ReadLine();
                Console.WriteLine("input output directory");
                string output = Console.ReadLine();
                Run(input, output, matchs, thresholds, nodes);
            }

            if (type == "1") {
                Console.WriteLine("input raw data file");
                string input = Console.ReadLine();
                Console.WriteLine("input library file");
                string reffile = Console.ReadLine();
                Console.WriteLine("input output directory");
                string output = Console.ReadLine();
                refRun(input, reffile, output, matchs, thresholds, nodes);
            }
        }

        public static void Run(string input, string outputdir, double matchs, double thresholds, double nodes) {
            if (!Directory.Exists(outputdir)) {
                Directory.CreateDirectory(outputdir);
            }

            var minimumPeakMatch = matchs;
            var matchThreshold = thresholds;
            var maxEdgeNumPerNode = nodes;
            var maxPrecursorDiff = 400.0;
            var maxPrecursorDiff_Percent = 100;

            var inputfilename = Path.GetFileNameWithoutExtension(input);
            var output_node_file = Path.Combine(outputdir, inputfilename  + "_node.txt");
            var output_edge_file = Path.Combine(outputdir, inputfilename  + "_edge.txt");

            var spectra = MspFileParser.MspFileReader(input);

            Console.WriteLine("Converting to normalized spectra");
            foreach (var record in spectra) {
                record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz);
            }

            Console.WriteLine("Creating molecular networking in plant spectra");
            var node2links = new Dictionary<int, List<LinkNode>>();
            var counter = 0;
            var max = spectra.Count * spectra.Count;
            for (int i = 0; i < spectra.Count; i++) {
                for (int j = i + 1; j < spectra.Count; j++) {
                    counter++;
                    Console.Write("{0} / {1}", counter, max);
                    Console.SetCursorPosition(0, Console.CursorTop);

                    var prop1 = spectra[i];
                    var prop2 = spectra[j];
                    var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
                    if (massDiff > maxPrecursorDiff) continue;
                    if (Math.Max(prop1.PrecursorMz, prop2.PrecursorMz) * maxPrecursorDiff_Percent * 0.01 - Math.Min(prop1.PrecursorMz, prop2.PrecursorMz) < 0) continue;

                    var scoreitem = MsScanMatching.GetBonanzaScore(prop1, prop2);
                    if (scoreitem[1] < minimumPeakMatch) continue;
                    if (scoreitem[0] < matchThreshold) continue;

                    if (node2links.ContainsKey(i)) {
                        node2links[i].Add(new LinkNode() { Score = scoreitem, Node = spectra[j] });
                    }
                    else {
                        node2links[i] = new List<LinkNode>() { new LinkNode() { Score = scoreitem, Node = spectra[j] } };
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

            var nodeDict = new Dictionary<string, MoleculeMsReference>();
            using (var sw = new StreamWriter(output_edge_file)) {
                sw.WriteLine("Source\tTarget\tSimilarity\tMatchNumber");
                foreach (var item in cNode2Links) {
                    foreach (var link in item.Value) {

                        var source_node_id = spectra[item.Key].Comment;
                        var target_node_id = link.Node.Comment;

                        sw.WriteLine(source_node_id + "\t" + target_node_id + "\t" + link.Score[0] + "\t" + link.Score[1]);

                        if (!nodeDict.ContainsKey(source_node_id)) {
                            nodeDict[source_node_id] = spectra[item.Key];
                        }
                        if (!nodeDict.ContainsKey(target_node_id)) {
                            nodeDict[target_node_id] = link.Node;
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(output_node_file)) {
                sw.WriteLine("ID\tMz\tRt\tName\tAdduct");
                foreach (var item in nodeDict) {
                    var key = item.Key;
                    var record = item.Value;
                    var lines = new List<string>() {
                        key, record.PrecursorMz.ToString(), record.ChromXs.Value.ToString(),
                        record.Name,
                        record.AdductType.ToString(),
                    };
                    sw.WriteLine(String.Join("\t", lines));
                }
            }
        }

        public static void refRun(string input, string reffile, string outputdir, double matchs, double thresholds, double nodes) {
            if (!Directory.Exists(outputdir)) {
                Directory.CreateDirectory(outputdir);
            } //reffile追加
            var minimumPeakMatch = 1; //1
            var matchThreshold = 0.80; //0.50
            var maxEdgeNumPerNode = 5; //5
            var maxPrecursorDiff = 400.0; //400.0
            var maxPrecursorDiff_Percent = 100;
            var inputfilename = Path.GetFileNameWithoutExtension(input);
            var output_node_file = Path.Combine(outputdir, inputfilename + "_node.txt");
            var output_edge_file = Path.Combine(outputdir, inputfilename + "_edge.txt");
            var spectra = MspFileParser.MspFileReader(input);
            var refspectra = MspFileParser.MspFileReader(reffile);
            Console.WriteLine("Converting to normalized spectra");
            foreach (var record in spectra) {
                record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz);
            }
            foreach (var record in refspectra) {
                record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz);
            }
            Console.WriteLine("Creating molecular networking in plant spectra");
            var node2links = new Dictionary<int, List<LinkNode>>();
            var counter = 0;
            var counterx = 0;
            var countnum = 0;
            var max = spectra.Count * refspectra.Count;
            for (int i = 0; i < spectra.Count; i++) {
                for (int j = 0; j < refspectra.Count; j++) {
                    counter++;
                    if (counter == 1000000) {
                        counterx++;
                        countnum = counterx * 1000000;
                        Console.Write(countnum);
                        counter = 0;
                    }
                    //Console.Write("{0} / {1}", counter, max);
                    Console.SetCursorPosition(0, Console.CursorTop);
                    var prop1 = spectra[i];
                    var prop2 = refspectra[j];
                    var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
                    if (massDiff > maxPrecursorDiff) continue;
                    if (Math.Max(prop1.PrecursorMz, prop2.PrecursorMz) * maxPrecursorDiff_Percent * 0.01 - Math.Min(prop1.PrecursorMz, prop2.PrecursorMz) < 0) continue;
                    var scoreitem = MsScanMatching.GetBonanzaScore(prop1, prop2);
                    //var scoreitem = MsScanMatching.GetModifiedDotProductScore(prop1, prop2);
                    if (scoreitem[1] < minimumPeakMatch) continue;
                    if (scoreitem[0] < matchThreshold) continue;
                    if (node2links.ContainsKey(i)) {
                        node2links[i].Add(new LinkNode() { Score = scoreitem, Node = refspectra[j] });
                    }
                    else {
                        node2links[i] = new List<LinkNode>() { new LinkNode() { Score = scoreitem, Node = refspectra[j] } };
                    }
                }
            }
            Console.WriteLine("Finish comparing");
            var cNode2Links = new Dictionary<int, List<LinkNode>>();
            foreach (var item in node2links) {
                var nitem = item.Value.OrderByDescending(n => n.Score[0]).ToList();
                cNode2Links[item.Key] = new List<LinkNode>();
                for (int i = 0; i < nitem.Count; i++) {
                    if (i > maxEdgeNumPerNode - 1) break;
                    cNode2Links[item.Key].Add(nitem[i]);
                }
            }
            var nodeDict = new Dictionary<string, MoleculeMsReference>();
            using (var sw = new StreamWriter(output_edge_file)) {
                sw.WriteLine("Source\tTarget\tSimilarity\tMatchNumber");
                foreach (var item in cNode2Links) {
                    foreach (var link in item.Value) {
                        //var source_node_id = spectra[item.Key].Name;
                        var source_node_id = spectra[item.Key].Name;
                        var target_node_id = "ID=" + spectra[item.Key].ScanID + " " + link.Node.Name;
                        sw.WriteLine(source_node_id + "\t" + target_node_id + "\t" + link.Score[0] + "\t" + link.Score[1]);
                        if (!nodeDict.ContainsKey(source_node_id)) {
                            nodeDict[source_node_id] = spectra[item.Key];
                        }
                        if (!nodeDict.ContainsKey(target_node_id)) {
                            nodeDict[target_node_id] = link.Node;
                        }
                    }
                }
            }
            using (var sw = new StreamWriter(output_node_file)) {
                sw.WriteLine("ID\tMz\tRt\tName\tAdduct");
                foreach (var item in nodeDict) {
                    var key = item.Key;
                    var record = item.Value;
                    var lines = new List<string>() {
                        key, record.PrecursorMz.ToString(), record.ChromXs.Value.ToString(),
                        record.Name,
                        record.AdductType.ToString(),
                    };
                    sw.WriteLine(String.Join("\t", lines));
                }
            }
        }

    }
}
