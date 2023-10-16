using CompMs.App.MsdialConsole.EadSpectraAnalysis;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.MolecularNetwork {

  
    public sealed class MoleculerSpectrumNetworkingTest {
        private MoleculerSpectrumNetworkingTest() { }

        public static void MergeEdgeFiles(string inputfolder, string output) {
            var files = Directory.GetFiles(inputfolder, "*.pairs");
            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("Source\tTarget\tScore\tMatchCount");
                foreach (var file in files) {
                    using (var sr = new StreamReader(file)) {
                        var header = sr.ReadLine().Split('\t');
                        var sourceFileName = Path.GetFileNameWithoutExtension(header[0].Substring(10));
                        var targetFileName = Path.GetFileNameWithoutExtension(header[1].Substring(10));
                        while (sr.Peek() > -1) {
                            var linearray = sr.ReadLine().Split('\t');
                            var newarray = new List<string>() { 
                                sourceFileName + "_" + linearray[0],
                                targetFileName + "_" + linearray[1],
                                linearray[2],
                                linearray[3]
                            };
                            sw.WriteLine(String.Join("\t", newarray));
                        }
                    }
                }
            }
        }

        public static void MergeNodeFiles(string inputfolder, string output) {
            var files = Directory.GetFiles(inputfolder, "*.mdpeak");
            var isfirstfile = true;
            using (var sw = new StreamWriter(output)) {
                foreach (var file in files) {
                    var filename = Path.GetFileNameWithoutExtension(file);
                    using (var sr = new StreamReader(file)) {
                        var header = sr.ReadLine().Split('\t');
                        if (isfirstfile) {
                            var newheder = new List<string>() { "File_ID", "File" };
                            foreach (var line in header) newheder.Add(line);
                            sw.WriteLine(String.Join("\t", newheder));
                        }
                        while (sr.Peek() > -1) {
                            var linearray = sr.ReadLine().Split('\t');
                            if (linearray[1].Contains("w/o MS2")) {
                                linearray[15] = "null";
                                linearray[16] = "null";
                                linearray[17] = "null";
                                linearray[18] = "null";
                            }
                            var newarray = new List<string>() { filename + "_" + linearray[0], filename };
                            foreach (var line in linearray) newarray.Add(line);
                            sw.WriteLine(String.Join("\t", newarray));
                        }
                    }
                }
            }
        }

        public static void Run(string input, string outputdir, string version) {
            if (!Directory.Exists(outputdir)) {
                Directory.CreateDirectory(outputdir);
            }

            var minimumPeakMatch = 6;
            var matchThreshold = 0.95;
            var maxEdgeNumPerNode = 5;
            var maxPrecursorDiff = 400.0;
            var maxPrecursorDiff_Percent = 100;

            var inputfilename = Path.GetFileNameWithoutExtension(input);
            var output_node_file = Path.Combine(outputdir, inputfilename + "_" + version + "_node.txt");
            var output_edge_file = Path.Combine(outputdir, inputfilename + "_" + version + "_edge.txt");

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

                    var scoreitem = MsScanMatching.GetModifiedDotProductScore(prop1, prop2);
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
    }
}
