using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Algorithm.Function;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Enum;
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

        /// <summary>
        /// The v1 ID of an MSP record: its DatabaseUniqueIdentifier, or else its ordinal in the
        /// unfiltered file (ScanID).
        /// </summary>
        /// <remarks>
        /// The record is always the one the loop scored. The ID used to be looked up as
        /// <c>cRecords[edge.source]</c>, but edge.source is the ScanID, an ordinal in the UNFILTERED
        /// file, while cRecords holds only the records of the selected ion mode that have peaks. Any
        /// file with a record filtered out before the scored ones gave the wrong ID or threw
        /// ArgumentOutOfRangeException.
        /// </remarks>
        internal static string V1RecordId(MoleculeMsReference record) {
            return !record.DatabaseUniqueIdentifier.IsEmptyOrNull() ? record.DatabaseUniqueIdentifier : record.ScanID.ToString();
        }

        private const int ProgressInterval = 100;

        // Whole lines on standard error: standard output may be a pipe or redirected to a file, and
        // cursor positioning throws IOException when no console is attached.
        private static void ReportProgress(int done, int total) {
            if (done % ProgressInterval == 0 || done == total) {
                Console.Error.WriteLine("{0} / {1}", done, total);
            }
        }

        public int Map2TargetFile(string targetFile, string inputFile, string methodFile, string outputfile, string ionMode) {
            var param = ConfigParser.ReadForMoleculerNetworkingParameter(methodFile);
            var counter = 0;

            var t_records = LibraryHandler.ReadMspLibrary(targetFile);
            var t_cRecords = t_records.Where(n => n.IonMode.ToString() == ionMode && n.Spectrum?.Count() > 0).ToList();

            var i_records = LibraryHandler.ReadMspLibrary(inputFile);
            var i_cRecords = i_records.Where(n => n.IonMode.ToString() == ionMode && n.Spectrum?.Count() > 0).ToList();

            foreach (var record in t_cRecords)
                record.Spectrum = MsnSettings.Preprocess(param, record.Spectrum, record.PrecursorMz);
            foreach (var record in i_cRecords)
                record.Spectrum = MsnSettings.Preprocess(param, record.Spectrum, record.PrecursorMz);

            AtomicOutputFile.Write(outputfile, sw => {
                WriteEdgeHeader(sw, param.MsmsSimilarityCalc);
                for (int i = 0; i < i_cRecords.Count; i++) {
                    for (int j = 0; j < t_cRecords.Count; j++) {
                        var edge = MoleculerNetworkingBase.GetEdge(i_cRecords[i], t_cRecords[j], param.MnMassTolerance,
                            param.MinimumPeakMatch, param.MnSpectrumSimilarityCutOff, param.MaxEdgeNumberPerNode,
                            param.MaxPrecursorDifference, param.MaxPrecursorDifferenceAsPercent,
                            param.MsmsSimilarityCalc);
                        if (edge != null) {
                            sw.WriteLine(V1RecordId(i_cRecords[i]) + "\t" + V1RecordId(t_cRecords[j]) + "\t" + String.Join("\t", edge.scores));
                        }
                    }
                    ReportProgress(++counter, i_cRecords.Count);
                }
            });
            return 0;
        }

        public int Run4Onefile(string intputfile, string outputfile, string methodFile, string ionMode) {
            var param = ConfigParser.ReadForMoleculerNetworkingParameter(methodFile);
            var counter = 0;

            var records = LibraryHandler.ReadMspLibrary(intputfile);
            var cRecords = records.Where(n => n.IonMode.ToString() == ionMode && n.Spectrum?.Count() > 0).ToList();

            Console.WriteLine("Total records count: {0}", cRecords.Count);
            foreach (var record in cRecords)
                record.Spectrum = MsnSettings.Preprocess(param, record.Spectrum, record.PrecursorMz);
            AtomicOutputFile.Write(outputfile, sw => {
                WriteEdgeHeader(sw, param.MsmsSimilarityCalc);
                for (int i = 0; i < cRecords.Count; i++) {
                    for (int j = i + 1; j < cRecords.Count; j++) {
                        var edge = MoleculerNetworkingBase.GetEdge(cRecords[i], cRecords[j], param.MnMassTolerance,
                            param.MinimumPeakMatch, param.MnSpectrumSimilarityCutOff, param.MaxEdgeNumberPerNode,
                            param.MaxPrecursorDifference, param.MaxPrecursorDifferenceAsPercent,
                            param.MsmsSimilarityCalc);
                        if (edge != null) {
                            sw.WriteLine(V1RecordId(cRecords[i]) + "\t" + V1RecordId(cRecords[j]) + "\t" + String.Join("\t", edge.scores));
                        }
                    }
                    ReportProgress(++counter, cRecords.Count);
                }
            });
            return 0;
        }

        /// <summary>
        /// <c>msn --format v2</c> on MSP input: the input with itself (<paramref name="targetFile"/>
        /// null) or with a target library.
        /// </summary>
        public int RunV2(string inputFile, string? targetFile, string outputFile, string methodFile, string ionMode, MsnV2Options options) {
            var parameter = ConfigParser.ReadForMoleculerNetworkingParameter(methodFile);
            MsnV2.Kernels(parameter, options);
            var query = MsnV2.LoadMsp(inputFile, ionMode);
            var target = targetFile is null ? null : MsnV2.LoadMsp(targetFile, ionMode);
            Console.WriteLine("Total records count: {0}", query.Records.Count);
            MsnV2.Write(outputFile, query, target, ionMode, parameter, options);
            return MsnExitCode.Success;
        }

        public int Run4Binary(string inputFile, string dclFile, string outputFile, string methodFile, string ionMode, MsnV2Options? v2 = null) {
            var parameter = ConfigParser.ReadForMoleculerNetworkingParameter(methodFile);
            if (v2 is not null) {
                MsnV2.Kernels(parameter, v2);
            }
            if (Path.GetExtension(inputFile).Equals(".arf", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(inputFile).Equals(".arf2", StringComparison.OrdinalIgnoreCase)) {
                var input = MolecularNetworkingInputLoader.LoadAlignment(inputFile, dclFile);
                return v2 is null ? RunInput(input, outputFile, parameter, ionMode) : RunInputV2(input, inputFile, outputFile, parameter, ionMode, v2);
            }
            else {
                var input = MolecularNetworkingInputLoader.LoadAnalysis(inputFile, dclFile);
                return v2 is null ? RunInput(input, outputFile, parameter, ionMode) : RunInputV2(input, inputFile, outputFile, parameter, ionMode, v2);
            }
        }

        private static int RunInputV2<T>(MolecularNetworkingInput<T> input, string inputFile, string outputFile, MolecularSpectrumNetworkingBaseParameter parameter, string ionMode, MsnV2Options options) where T : CompMs.Common.Interfaces.IMoleculeProperty, CompMs.Common.Interfaces.IChromatogramPeak {
            // A project names its peak lists .pai/.arf; they are stored as .pai2/.arf2.
            var stored = File.Exists(inputFile) ? inputFile : inputFile + "2";
            var query = MsnV2.LoadBinary(input, stored, ParseIonMode(ionMode));
            MsnV2.Write(outputFile, query, null, ionMode, parameter, options);
            return MsnExitCode.Success;
        }

        public int Run4Project(string projectFile, string outputFile, string methodFile, string ionMode, bool alignment, string analysisFileName = null, MsnV2Options? v2 = null) {
            var storage = MolecularNetworkingInputLoader.LoadProject(projectFile);
            if (alignment && string.IsNullOrWhiteSpace(analysisFileName)) {
                var alignmentFile = storage.AlignmentFiles.FirstOrDefault(file => MolecularNetworkingInputLoader.StoredFileExists(file.FilePath));
                if (alignmentFile is null) throw new FileNotFoundException("No alignment result file was found in the project.", projectFile);
                return Run4Binary(alignmentFile.FilePath, alignmentFile.SpectraFilePath, outputFile, methodFile, ionMode, v2);
            }
            var analysisFile = MolecularNetworkingInputLoader.SelectAnalysisFile(storage.AnalysisFiles, analysisFileName, projectFile);
            return Run4Binary(analysisFile.PeakAreaBeanInformationFilePath, analysisFile.DeconvolutionFilePath, outputFile, methodFile, ionMode, v2);
        }

        private static int RunInput<T>(MolecularNetworkingInput<T> input, string outputFile, MolecularSpectrumNetworkingBaseParameter parameter, string ionMode) where T : CompMs.Common.Interfaces.IMoleculeProperty, CompMs.Common.Interfaces.IChromatogramPeak {
            var targetIonMode = ParseIonMode(ionMode);
            var selected = input.Spots.Select((spot, index) => (spot, scan: input.Scans[index]))
                .Where(item => item.scan.IonMode == targetIonMode && item.scan.Spectrum.Count > 0).ToList();
            var spots = selected.Select(item => item.spot).ToList();
            var scans = selected.Select(item => item.scan).ToList();
            for (var i = 0; i < scans.Count; i++) {
                scans[i].Spectrum = MsnSettings.Preprocess(parameter, scans[i].Spectrum, scans[i].PrecursorMz);
            }

            // Same pairing as the MSP path (Run4Onefile): every unordered pair is scored once.
            AtomicOutputFile.Write(outputFile, writer => {
                WriteEdgeHeader(writer, parameter.MsmsSimilarityCalc);
                for (var i = 0; i < scans.Count; i++) {
                    for (var j = i + 1; j < scans.Count; j++) {
                        var edge = MoleculerNetworkingBase.GetEdge(scans[i], scans[j], parameter.MnMassTolerance,
                            parameter.MinimumPeakMatch, parameter.MnSpectrumSimilarityCutOff, parameter.MaxEdgeNumberPerNode,
                            parameter.MaxPrecursorDifference, parameter.MaxPrecursorDifferenceAsPercent,
                            parameter.MsmsSimilarityCalc);
                        if (edge != null) {
                            writer.WriteLine($"{spots[i].ID}\t{spots[j].ID}\t{String.Join("\t", edge.scores)}");
                        }
                    }
                }
            });
            return 0;
        }

        private static IonMode ParseIonMode(string ionMode) {
            if (Enum.TryParse<IonMode>(ionMode, ignoreCase: true, out var parsed)
                && (parsed == IonMode.Positive || parsed == IonMode.Negative)) {
                return parsed;
            }
            throw new ArgumentException($"Unknown ion mode '{ionMode}'. Use Positive or Negative.", nameof(ionMode));
        }

        private static void WriteEdgeHeader(TextWriter writer, MsmsSimilarityCalc similarityCalc) {
            if (similarityCalc == MsmsSimilarityCalc.All) {
                writer.WriteLine("SourceID\tTargetID\tBonanzaScore\tMatchPeakCount\tModDotScore\tCosineScore");
            }
            else {
                writer.WriteLine("SourceID\tTargetID\tScore\tMatchPeakCount");
            }
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
                    record.Spectrum = MsnSettings.Preprocess(param, record.Spectrum, record.PrecursorMz);
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
                        record.Spectrum = MsnSettings.Preprocess(param, record.Spectrum, record.PrecursorMz);

                    var sameFileOffset = inputA == inputB ? 1 : 0;

                    // Console.WriteLine("Start {0} vs {1}", Path.GetFileNameWithoutExtension(inputA), Path.GetFileNameWithoutExtension(inputB));
                    AtomicOutputFile.Produce(outputPath, partialPath => MoleculerNetworkingBase.ExportAllEdges(partialPath, inputA, inputB, recordsA, recordsB, param.MnMassTolerance,
                        param.MinimumPeakMatch, param.MnSpectrumSimilarityCutOff, param.MaxEdgeNumberPerNode + sameFileOffset,
                        param.MaxPrecursorDifference, param.MaxPrecursorDifferenceAsPercent,
                        param.MsmsSimilarityCalc,
                        null));
                    lock (syncObj) {
                        progress++;
                        Console.Error.WriteLine("Progress {0} in {1}/{2} by time {3} sec for Query1 {4} vs Query2 {5}", outputName, progress, files.Count, stopwatch.ElapsedMilliseconds * 0.001, recordsA.Count, recordsB.Count);
                    }
                });
                counter++;
                Console.Error.WriteLine("Done {0}/{1} by time {2} sec", counter, files.Count, masterSW.ElapsedMilliseconds * 0.001);
            }
            return 0;
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
                    record.Spectrum = MsnSettings.Preprocess(param, record.Spectrum, record.PrecursorMz);
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
                        record.Spectrum = MsnSettings.Preprocess(param, record.Spectrum, record.PrecursorMz);

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
                        Console.Error.WriteLine("Progress {0} in {1}/{2} by time {3} sec for Query1 {4} vs Query2 {5}", outputName, progress, files.Count, stopwatch.ElapsedMilliseconds * 0.001, recordsA.Count, recordsB.Count);
                    }
                });
                counter++;
                Console.Error.WriteLine("Done {0}/{1} by time {2} sec", counter, files.Count, masterSW.ElapsedMilliseconds * 0.001);
            }
            return 0;
        }

        private void ExportEdges(string path, List<EdgeData> edges, string inputA, string inputB) {
            AtomicOutputFile.Write(path, sw => {
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
            });
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
