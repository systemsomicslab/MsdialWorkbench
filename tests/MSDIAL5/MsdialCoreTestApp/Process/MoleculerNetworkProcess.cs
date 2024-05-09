using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Algorithm.Function;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
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

        public void GetMsp4Model(string inputMspFile, string inputEdgeFile, string outputMspFile) {
            var records = LibraryHandler.ReadMspLibrary(inputMspFile);
            var nodes = new List<string>();
            using (var sr = new StreamReader(inputEdgeFile)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');
                    if (!nodes.Contains(linearray[0])) nodes.Add(linearray[0]);
                    if (!nodes.Contains(linearray[1])) nodes.Add(linearray[1]);
                }
            }
            var nrecords = new List<MoleculeMsReference>();
            foreach (var record in records) {
                if (nodes.Contains(record.DatabaseUniqueIdentifier)) {
                    nrecords.Add(record);
                }
            }

            using (var sw = new StreamWriter(outputMspFile)) {
                foreach (var record in nrecords) {
                    MspFileParser.WriteMspFields(record, sw);
                }
            }
        }

        public int Map2TargetFile(string targetFile, string inputFile, string methodFile, string outputfile, string ionMode) {
            var dt = DateTime.Now;
            var param = ConfigParser.ReadForMoleculerNetworkingParameter(methodFile);
            var counter = 0;

            var t_records = LibraryHandler.ReadMspLibrary(targetFile);
            var t_cRecords = t_records.Where(n => n.IonMode.ToString() == ionMode && n.Spectrum?.Count() > 0).ToList();

            var i_records = LibraryHandler.ReadMspLibrary(inputFile);
            var i_cRecords = i_records.Where(n => n.IonMode.ToString() == ionMode && n.Spectrum?.Count() > 0).ToList();

            foreach (var record in t_cRecords)
                record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz, absoluteAbundanceCutOff: param.MnAbsoluteAbundanceCutOff, relativeAbundanceCutOff: param.MnRelativeAbundanceCutOff);
            foreach (var record in i_cRecords)
                record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz, absoluteAbundanceCutOff: param.MnAbsoluteAbundanceCutOff, relativeAbundanceCutOff: param.MnRelativeAbundanceCutOff);

            using (var sw = new StreamWriter(outputfile)) {
                if (param.MsmsSimilarityCalc == Common.Enum.MsmsSimilarityCalc.All) {
                    sw.WriteLine("SourceID\tTargetID\tBonanzaScore\tMatchPeakCount\tModDotScore\tCosineScore");
                }
                else {
                    sw.WriteLine("SourceID\tTargetID\tScore\tMatchPeakCount");
                }
                for (int i = 0; i < i_cRecords.Count; i++) {
                    for (int j = 0; j < t_cRecords.Count; j++) {
                        var edge = MoleculerNetworkingBase.GetEdge(i_cRecords[i], t_cRecords[j], param.MnMassTolerance,
                            param.MinimumPeakMatch, param.MnSpectrumSimilarityCutOff, param.MaxEdgeNumberPerNode,
                            param.MaxPrecursorDifference, param.MaxPrecursorDifferenceAsPercent,
                            param.MsmsSimilarityCalc);
                        if (edge != null) {
                            var source = !i_cRecords[edge.source].DatabaseUniqueIdentifier.IsEmptyOrNull() ? i_cRecords[edge.source].DatabaseUniqueIdentifier : edge.source.ToString();
                            var target = !t_cRecords[edge.target].DatabaseUniqueIdentifier.IsEmptyOrNull() ? t_cRecords[edge.target].DatabaseUniqueIdentifier : edge.target.ToString();
                            sw.WriteLine(source + "\t" + target + "\t" + String.Join("\t", edge.scores));
                        }
                    }
                    counter++;
                    if (counter % 100 == 0) {
                        Console.Write("{0} / {1}", counter, i_cRecords.Count);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                }
            }
            return 1;
        } 

        public int Run4Onefile(string intputfile, string outputfile, string methodFile, string ionMode) {
            var dt = DateTime.Now;
            var param = ConfigParser.ReadForMoleculerNetworkingParameter(methodFile);
            var counter = 0;

            var records = LibraryHandler.ReadMspLibrary(intputfile);
            var cRecords = records.Where(n => n.IonMode.ToString() == ionMode && n.Spectrum?.Count() > 0).ToList();

            Console.WriteLine("Total records count: {0}", cRecords.Count);
            foreach (var record in cRecords)
                record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz, absoluteAbundanceCutOff: param.MnAbsoluteAbundanceCutOff, relativeAbundanceCutOff: param.MnRelativeAbundanceCutOff);
            using (var sw = new StreamWriter(outputfile)) {
                if (param.MsmsSimilarityCalc == Common.Enum.MsmsSimilarityCalc.All) {
                    sw.WriteLine("SourceID\tTargetID\tBonanzaScore\tMatchPeakCount\tModDotScore\tCosineScore");
                }
                else {
                    sw.WriteLine("SourceID\tTargetID\tScore\tMatchPeakCount");
                }
                for (int i = 0; i < cRecords.Count; i++) {
                    for (int j = i + 1; j < cRecords.Count; j++) {
                        var edge = MoleculerNetworkingBase.GetEdge(cRecords[i], cRecords[j], param.MnMassTolerance,
                            param.MinimumPeakMatch, param.MnSpectrumSimilarityCutOff, param.MaxEdgeNumberPerNode,
                            param.MaxPrecursorDifference, param.MaxPrecursorDifferenceAsPercent,
                            param.MsmsSimilarityCalc);
                        if (edge != null) {
                            var source = !cRecords[edge.source].DatabaseUniqueIdentifier.IsEmptyOrNull() ? cRecords[edge.source].DatabaseUniqueIdentifier : edge.source.ToString();
                            var target = !cRecords[edge.target].DatabaseUniqueIdentifier.IsEmptyOrNull() ? cRecords[edge.target].DatabaseUniqueIdentifier : edge.target.ToString();
                            sw.WriteLine(source + "\t" + target + "\t" + String.Join("\t", edge.scores));
                        }
                    }
                    counter++;
                    if (counter % 100 == 0) {
                        Console.Write("{0} / {1}", counter, cRecords.Count);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                }
            }
            return 1;
        }

        public int Run4AllEdgeGeneration(string inputDir, string outputDir, string methodFile, string ionMode, bool isOverwrite) {
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

                    if (System.IO.File.Exists(outputPath) && !isOverwrite) {
                        return;
                    }

                    var recordsB = LibraryHandler.ReadMspLibrary(inputB).Where(n => n.IonMode.ToString() == ionMode && n.Spectrum?.Count() > 0).ToList();
                    if (recordsB.Count <= 1) return;

                    foreach (var record in recordsB)
                        record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz, absoluteAbundanceCutOff: param.MnAbsoluteAbundanceCutOff, relativeAbundanceCutOff: param.MnRelativeAbundanceCutOff);

                    var sameFileOffset = inputA == inputB ? 1 : 0;

                    // Console.WriteLine("Start {0} vs {1}", Path.GetFileNameWithoutExtension(inputA), Path.GetFileNameWithoutExtension(inputB));
                    MoleculerNetworkingBase.ExportAllEdges(outputPath, inputA, inputB, recordsA, recordsB, param.MnMassTolerance,
                        param.MinimumPeakMatch, param.MnSpectrumSimilarityCutOff, param.MaxEdgeNumberPerNode + sameFileOffset,
                        param.MaxPrecursorDifference, param.MaxPrecursorDifferenceAsPercent,
                        param.MsmsSimilarityCalc,
                        null);
                    lock (syncObj) {
                        progress++;
                        Console.WriteLine("Progress {0} in {1}/{2} by time {3} sec for Query1 {4} vs Query2 {5}", outputName, progress, files.Count, stopwatch.ElapsedMilliseconds * 0.001, recordsA.Count, recordsB.Count);
                    }
                });
                counter++;
                Console.WriteLine("Done {0}/{1} by time {2} sec", counter, files.Count, masterSW.ElapsedMilliseconds * 0.001);
            }
            return 1;
        }

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

                    if (System.IO.File.Exists(outputPath) && !isOverwrite) {
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
            }
            return 1;
        }

        private void ExportEdges(string path, List<EdgeData> edges, string inputA, string inputB) {
            using (var sw = new StreamWriter(path, false)) {
                if (edges.IsEmptyOrNull()) return;
                var fedge = edges.FirstOrDefault();
                if (fedge.scores.Count > 2) {
                    sw.WriteLine("SourceID: {0}\tTargetID: {1}\tBonanzaScore\tMatchPeakCount\tModDotScore\tCosineScore", inputA, inputB);
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
                        sw.WriteLine(edge.source + "\t" + edge.target + "\t" + String.Join("\t", edge.scores));
                    }
                }
            }
        }

        private List<string> ReadInput(string inputDir) {
            FileAttributes attributes = System.IO.File.GetAttributes(inputDir);
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
