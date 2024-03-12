using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Algorithm.Function;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using NCDK.QSAR.Descriptors.Atomic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process.MoleculerNetworking {
    public class MoleculerNetworkProcess {

        public int Run(string inputDir, string outputDir, string methodFile, string ionMode, bool isOverwrite) {
            var files = ReadInput(inputDir);
            var dt = DateTime.Now;
            var folder = Path.Combine(outputDir);
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            var param = ConfigParser.ReadForMoleculerNetworkingParameter(methodFile);

            var counter = 0;
            var syncObj = new object();

            Console.WriteLine("Total file count: {0}", files.Count);
            for (int i = 0; i < files.Count; i++) {
                var masterSW = Stopwatch.StartNew();
                var inputA = files[i];
                var recordsA = LibraryHandler.ReadMspLibrary(inputA).Where(n => n.IonMode.ToString() == ionMode && n.Spectrum?.Count() > 0).ToList();
                if (recordsA.Count <= 1) continue;
                foreach (var record in recordsA) 
                    record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz, absoluteAbundanceCutOff: param.MnAbsoluteAbundanceCutOff, relativeAbundanceCutOff: param.MnRelativeAbundanceCutOff);
                var progress = 0;
                Parallel.For(i, files.Count, j => {
                    var stopwatch = Stopwatch.StartNew();
                    var inputB = files[j];
                    var outputName = Path.GetFileNameWithoutExtension(inputA) + "_mn_" + Path.GetFileNameWithoutExtension(inputB) + ".pairs";
                    var outputPath = Path.Combine(folder, outputName);

                    if (File.Exists(outputPath) && !isOverwrite) {
                        return;
                    }

                    var recordsB = LibraryHandler.ReadMspLibrary(inputB).Where(n => n.IonMode.ToString() == ionMode && n.Spectrum?.Count() > 0).ToList();
                    if (recordsB.Count <= 1) return;

                    foreach (var record in recordsB)
                        record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz, absoluteAbundanceCutOff: param.MnAbsoluteAbundanceCutOff, relativeAbundanceCutOff: param.MnRelativeAbundanceCutOff);

                    var sameFileOffset = inputA == inputB ? 1 : 0;

                    // Console.WriteLine("Start {0} vs {1}", Path.GetFileNameWithoutExtension(inputA), Path.GetFileNameWithoutExtension(inputB));
                    var edges = MoleculerNetworkingBase.GenerateEdges(recordsA, recordsB, param.MnMassTolerance,
                        param.MinimumPeakMatch, param.MnSpectrumSimilarityCutOff, param.MaxEdgeNumberPerNode + sameFileOffset,
                        param.MaxPrecursorDifference, param.MaxPrecursorDifferenceAsPercent,
                        param.MsmsSimilarityCalc,
                        null);
                    // Console.WriteLine();
                    // Console.WriteLine("Time {0} sec", stopwatch.ElapsedMilliseconds * 0.001);

                    
                    ExportEdges(outputPath, edges, inputA, inputB);
                    lock (syncObj) {
                        progress++;
                        Console.WriteLine("Progress {0} in {1}/{2} by time {3} sec for Query1 {4} vs Query2 {5}", outputName, progress, files.Count, stopwatch.ElapsedMilliseconds * 0.001, recordsA.Count, recordsB.Count);
                    }
                });
                counter++;
                Console.WriteLine("Done {0}/{1} by time {2} sec", counter, files.Count, masterSW.ElapsedMilliseconds * 0.001);

                //for (int j = i; j < files.Count; j++) {
                //    var inputB = files[j];
                //    var recordsB = LibraryHandler.ReadMspLibrary(inputB).Where(n => n.IonMode.ToString() == ionMode && n.Spectrum?.Count() > 0).ToList();
                //    if (recordsB.Count <= 1) continue;

                //    foreach (var record in recordsB)
                //        record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz, absoluteAbundanceCutOff: param.MnAbsoluteAbundanceCutOff, relativeAbundanceCutOff: param.MnRelativeAbundanceCutOff);

                //    var sameFileOffset = inputA == inputB ? 1 : 0;

                //    Console.WriteLine("Start {0} vs {1}", Path.GetFileNameWithoutExtension(inputA), Path.GetFileNameWithoutExtension(inputB));
                //    var stopwatch = Stopwatch.StartNew();
                //    var edges = MoleculerNetworkingBase.GenerateEdges(recordsA, recordsB, param.MnMassTolerance,
                //        param.MinimumPeakMatch, param.MnSpectrumSimilarityCutOff, param.MaxEdgeNumberPerNode + sameFileOffset,
                //        param.MaxPrecursorDifference, param.MaxPrecursorDifferenceAsPercent, 
                //        param.MsmsSimilarityCalc == Common.Enum.MsmsSimilarityCalc.Bonanza ? true : false,
                //        null);
                //    Console.WriteLine();
                //    Console.WriteLine("Time {0} sec", stopwatch.ElapsedMilliseconds * 0.001);

                //    var outputName = Path.GetFileNameWithoutExtension(inputA) + "_mn_" + Path.GetFileNameWithoutExtension(inputB) + ".pairs";
                //    var outputPath = Path.Combine(folder, outputName);
                //    ExportEdges(outputPath, edges, inputA, inputB);
                //    Console.WriteLine("Done");
                //}

                counter++;
                Console.WriteLine("Finish processing against {0}", Path.GetFileNameWithoutExtension(inputA));
            }
            return 1;
        }

        private void ExportEdges(string path, List<EdgeData> edges, string inputA, string inputB) {
            using (var sw = new StreamWriter(path, false)) {
                if (edges.IsEmptyOrNull()) return;
                var fedge = edges.FirstOrDefault();
                if (fedge.scores.Count > 2) {
                    sw.WriteLine("SourceID: {0}\tTargetID: {1}\tBonanzaScore\tBonanzaMatchPeakCount\tModDotScore\tModDotMatchPeakCount\tCosineScore\tCosineMatchPeakCount", inputA, inputB);
                }
                else {
                    sw.WriteLine("SourceID: {0}\tTargetID: {1}\tSimilarityScore\tMatchPeakCount", inputA, inputB);
                }

                var isABMatched = inputA == inputB;

                if (isABMatched) {
                    edges = edges.Where(n => n.source != n.target).ToList();
                    var dones = new List<string>();
                    foreach (var edge in edges) {
                        var st = Math.Min(edge.source, edge.target) + "_" + Math.Max(edge.source, edge.target);
                        if (!dones.Contains(st)) {
                            sw.WriteLine(edge.source + "\t" + edge.target + "\t" + String.Join("\t", edge.scores));
                            dones.Add(st);
                        }
                    }
                }
                else {
                    foreach (var edge in edges) {
                        if (fedge.scores.Count == 2) {
                            sw.WriteLine(edge.source + "\t" + edge.target + "\t" + String.Join("\t", edge.scores));
                        }
                    }
                }
            }
        }

        private List<string> ReadInput(string inputDir) {
            FileAttributes attributes = File.GetAttributes(inputDir);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                Debug.WriteLine(String.Format("{0} is a folder", inputDir));
                return Directory.GetFiles(inputDir, "*.*msp", SearchOption.AllDirectories)?.ToList();
            }
            else {
                Debug.WriteLine(String.Format("{0} is not a folder", inputDir));
                return null;
            }
        }
    }
}
