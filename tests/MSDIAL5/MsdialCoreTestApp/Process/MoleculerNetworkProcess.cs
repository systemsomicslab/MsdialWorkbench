using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Algorithm.Function;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
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

        public int Run4AllEdgeGeneration(string inputDir, string outputDir, string methodFile, string ionMode, bool isOverwrite, IReadOnlyList<string>? inputFiles = null) {
            var files = inputFiles?.ToList() ?? ReadInput(inputDir);
            var dt = DateTime.Now;
            var folder = Path.Combine(outputDir);
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }
            ExportFileMapping(Path.Combine(folder, "files.tsv"), files);

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

        public int Run(
            string inputDir,
            string outputDir,
            string methodFile,
            string ionMode,
            bool isOverwrite,
            string? analysisFileCsv = null,
            IReadOnlyList<string>? inputFiles = null) {
            var files = inputFiles?.ToList() ?? ReadInput(inputDir);
            var dt = DateTime.Now;
            var folder = Path.Combine(outputDir);
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            var param = ConfigParser.ReadForMoleculerNetworkingParameter(methodFile);

            var counter = 0;
            var syncObj = new object();

            ExportFileMapping(Path.Combine(folder, "files.tsv"), files);
            ExportNodeTable(Path.Combine(folder, "nodes.tsv"), files, ionMode, analysisFileCsv);

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

                    
                    ExportEdges(outputPath, edges, inputA, inputB, recordsA, recordsB);
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

        private void ExportEdges(
            string path,
            List<EdgeData> edges,
            string inputA,
            string inputB,
            IReadOnlyList<MoleculeMsReference> recordsA,
            IReadOnlyList<MoleculeMsReference> recordsB) {
            var recordsByScanIdA = recordsA.ToDictionary(record => record.ScanID);
            var recordsByScanIdB = recordsB.ToDictionary(record => record.ScanID);
            using (var sw = new StreamWriter(path, false)) {
                if (edges.IsEmptyOrNull()) return;
                var fedge = edges.FirstOrDefault();
                if (fedge.scores.Count > 2) {
                    sw.WriteLine("SourceID\tTargetID\tBonanzaScore\tMatchPeakCount\tModDotScore\tCosineScore");
                }
                else {
                    sw.WriteLine("SourceID\tTargetID\tSimilarityScore\tMatchPeakCount");
                }

                var isABMatched = inputA == inputB;

                if (isABMatched) {
                    edges = edges.Where(n => n.source != n.target).ToList();
                    var dones = new List<string>();
                    foreach (var edge in edges) {
                        var st = Math.Min(edge.source, edge.target) + "_" + Math.Max(edge.source, edge.target);
                        if (!dones.Contains(st)) {
                            sw.WriteLine(GetNodeId(inputA, recordsByScanIdA[edge.source], edge.source) + "\t"
                                + GetNodeId(inputB, recordsByScanIdB[edge.target], edge.target) + "\t"
                                + String.Join("\t", edge.scores));
                            dones.Add(st);
                        }
                    }
                }
                else {
                    foreach (var edge in edges) {
                        sw.WriteLine(GetNodeId(inputA, recordsByScanIdA[edge.source], edge.source) + "\t"
                            + GetNodeId(inputB, recordsByScanIdB[edge.target], edge.target) + "\t"
                            + String.Join("\t", edge.scores));
                    }
                }
            }
        }

        private static void ExportFileMapping(string outputPath, IReadOnlyList<string> files) {
            using var sw = new StreamWriter(outputPath, false, new UTF8Encoding(false));
            sw.WriteLine("FileID\tMspFilePath");
            foreach (var file in files) {
                sw.WriteLine(EscapeTsv(Path.GetFileNameWithoutExtension(file)) + "\t" + EscapeTsv(Path.GetFullPath(file)));
            }
        }

        private void ExportNodeTable(string outputPath, IReadOnlyList<string> files, string ionMode, string? analysisFileCsv) {
            var analysisFileMetadata = String.IsNullOrEmpty(analysisFileCsv)
                ? new Dictionary<string, AnalysisFileBean>(StringComparer.OrdinalIgnoreCase)
                : AnalysisFilesParser.ReadCsvContents(analysisFileCsv)
                    .GroupBy(file => file.AnalysisFileName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var nodes = files
                .SelectMany(file => LibraryHandler.ReadMspLibrary(file)
                    .Where(record => record.IonMode.ToString() == ionMode && record.Spectrum?.Count > 0)
                    .Select((record, index) => new {
                        File = file,
                        Record = record,
                        Index = index,
                        CommentFields = ParseComment(record.Comment),
                    }))
                .ToList();
            var commentKeys = nodes
                .SelectMany(node => node.CommentFields.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var sw = new StreamWriter(outputPath, false, new UTF8Encoding(false));
            var header = new List<string> {
                "ID", "File", "PeakID", "Name", "PrecursorMz", "PrecursorType", "IonMode",
                "RetentionTime", "Formula", "Ontology", "InChIKey", "SMILES", "Comment",
                "FilePath", "FileName", "FileType", "ClassID", "AcquisitionType",
                "BatchOrder", "AnalyticalOrder", "Factor",
            };
            header.AddRange(commentKeys.Where(key => !key.Equals("PEAKID", StringComparison.OrdinalIgnoreCase))
                .Select(key => "Comment_" + key));
            sw.WriteLine(String.Join("\t", header));

            foreach (var node in nodes) {
                var peakId = GetPeakId(node.Record, node.Index);
                var fileName = Path.GetFileNameWithoutExtension(node.File);
                analysisFileMetadata.TryGetValue(fileName, out var metadata);
                var values = new List<string> {
                    GetNodeId(node.File, node.Record, node.Index),
                    fileName,
                    peakId,
                    node.Record.Name,
                    node.Record.PrecursorMz.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    node.Record.AdductType?.ToString() ?? String.Empty,
                    node.Record.IonMode.ToString(),
                    node.Record.ChromXs.RT.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    node.Record.Formula?.FormulaString ?? String.Empty,
                    node.Record.Ontology,
                    node.Record.InChIKey,
                    node.Record.SMILES,
                    node.Record.Comment,
                    metadata?.AnalysisFilePath ?? String.Empty,
                    metadata?.AnalysisFileName ?? fileName,
                    metadata?.AnalysisFileType.ToString() ?? String.Empty,
                    metadata?.AnalysisFileClass ?? String.Empty,
                    metadata?.AcquisitionType.ToString() ?? String.Empty,
                    metadata?.AnalysisBatch.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? String.Empty,
                    metadata?.AnalysisFileAnalyticalOrder.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? String.Empty,
                    metadata?.DilutionFactor.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? String.Empty,
                };
                values.AddRange(commentKeys
                    .Where(key => !key.Equals("PEAKID", StringComparison.OrdinalIgnoreCase))
                    .Select(key => node.CommentFields.TryGetValue(key, out var value) ? value : String.Empty));
                sw.WriteLine(String.Join("\t", values.Select(EscapeTsv)));
            }
        }

        private static string GetNodeId(string file, MoleculeMsReference record, int fallbackIndex) {
            return Path.GetFileNameWithoutExtension(file) + "_" + GetPeakId(record, fallbackIndex);
        }

        private static string GetPeakId(MoleculeMsReference record, int fallbackIndex) {
            var fields = ParseComment(record.Comment);
            return fields.TryGetValue("PEAKID", out var peakId) && !String.IsNullOrWhiteSpace(peakId)
                ? peakId
                : fallbackIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, string> ParseComment(string comment) {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in (comment ?? String.Empty).Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries)) {
                var separator = token.IndexOf('=');
                if (separator <= 0) {
                    continue;
                }
                var key = token.Substring(0, separator).Trim();
                var value = token.Substring(separator + 1).Trim();
                if (!String.IsNullOrEmpty(key)) {
                    fields[key] = value;
                }
            }
            return fields;
        }

        private static string EscapeTsv(string value) {
            return (value ?? String.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
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
