using CompMs.Common.Algorithm.Function;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.EadSpectraAnalysis {
    public class LinkNode {
        public double[] Score { get; set; }
        public MoleculeMsReference Node { get; set; }
    }
    public sealed class EadSpectraAnalysis {
        public static void GenerateSpectralEntropyList(string inputdir, string outputdir) {
            var folders = Directory.GetDirectories(inputdir, "*", SearchOption.TopDirectoryOnly);
            if (!Directory.Exists(outputdir)) {
                Directory.CreateDirectory(outputdir);
            }
            var outputfilename = "entropy.txt";
            var outputfile = Path.Combine(outputdir, outputfilename);
            using (var sw = new StreamWriter(outputfile)) {
                sw.WriteLine("Name\tEntropy\tEnergy");
                foreach (var folder in folders) {
                    var files = Directory.GetFiles(folder, "*.msp", SearchOption.TopDirectoryOnly);
                    foreach (var file in files) {
                        var records = MspFileParser.MspFileReader(file);
                        for (int i = 0; i < records.Count; i++) {
                            var record = records[i];
                            var normalizedSpectrum = SpectrumHandler.GetNormalizedPeak4SpectralEntropyCalc(record.Spectrum, record.PrecursorMz);
                            var entropy = MsScanMatching.GetSpectralEntropy(normalizedSpectrum);
                            sw.WriteLine(record.Comment.Replace("_Curated", "") + "\t" + entropy + "\t" + record.FragmentationCondition);
                        }
                    }
                }
            }
        }

        public static void GenerateSpectralEntropyListAsSeparateFormat(string inputdir, string mspfilelist, string output) {
            var folders = Directory.GetDirectories(inputdir, "Curated_*", SearchOption.TopDirectoryOnly);
            var foldernames = folders.Select(n => Path.GetFileNameWithoutExtension(n)).ToList();

            var mspfiles = new List<string>();
            using (var sr = new StreamReader(mspfilelist)) {
                while (sr.Peek() > -1) {
                    mspfiles.Add(sr.ReadLine());
                }
            }

            var recordnames = new List<string>();
            var entropiesList = new List<List<string>>();
            for (int j = 0; j < folders.Length; j++) {
                var mspfile = Directory.GetFiles(folders[j], "*.msp", SearchOption.TopDirectoryOnly)[0];
                var records = MspFileParser.MspFileReader(mspfile);
                var entroies = new List<string>();
                for (int i = 0; i < records.Count; i++) {
                    var record = records[i];
                    var normalizedSpectrum = SpectrumHandler.GetNormalizedPeak4SpectralEntropyCalc(record.Spectrum, record.PrecursorMz);
                    var entropy = MsScanMatching.GetSpectralEntropy(normalizedSpectrum);
                    entroies.Add(Math.Round(entropy, 5).ToString());
                    if (j == 0) {
                        recordnames.Add(record.Comment.Replace("_Curated", ""));
                    }
                }
                entropiesList.Add(entroies);
            }

            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("Name" + String.Join("\t", foldernames));
                for (int i = 0; i < recordnames.Count; i++) {
                    var exports = new List<string>();
                    for (int j = 0; j < entropiesList.Count; j++) {
                        exports.Add(entropiesList[j][i].ToString());
                    }
                    sw.WriteLine(recordnames[i] + "\t" + String.Join("\t", exports));
                }
            }



            //var counter = 0;
            //using (var sw = new StreamWriter(output)) {
            //    sw.WriteLine("Name" + "\t" + String.Join("\t", foldernames));
            //    foreach (var filename in mspfiles) {
            //        counter++;
            //        var entropies = new List<string>();
            //        foreach (var folder in folders) entropies.Add("0");
            //        for (int j = 0; j < folders.Length; j++) {
            //            var mspfile = Directory.GetFiles(folders[j], "*.msp", SearchOption.TopDirectoryOnly)[0];
            //            var records = MspFileParser.MspFileReader(mspfile);
            //            for (int i = 0; i < records.Count; i++) {
            //                var record = records[i];
            //                if (record.Comment.Replace("_Curated", "") == filename) {
            //                    var normalizedSpectrum = SpectrumHandler.GetNormalizedPeak4SpectralEntropyCalc(record.Spectrum, record.PrecursorMz);
            //                    var entropy = MsScanMatching.GetSpectralEntropy(normalizedSpectrum);
            //                    entropies[j] = Math.Round(entropy, 5).ToString();
            //                    break;
            //                }
            //            }
            //        }
            //        sw.WriteLine(filename + "\t" + String.Join("\t", entropies));
            //        Console.WriteLine(counter);
            //    }
            //}
        }

        public static void Check144ExistenceInMspFiles(string inputdir, string mspfilelist, string output) {
            var folders = Directory.GetDirectories(inputdir, "Curated_*", SearchOption.TopDirectoryOnly);
            var foldernames = folders.Select(n => Path.GetFileNameWithoutExtension(n)).ToList();

            var mspfiles = new List<string>();
            using (var sr = new StreamReader(mspfilelist)) {
                while (sr.Peek() > -1) {
                    mspfiles.Add(sr.ReadLine());
                }
            }

            var recordnames = new List<string>();
            var entropiesList = new List<List<string>>();
            for (int j = 0; j < folders.Length; j++) {
                var mspfile = Directory.GetFiles(folders[j], "*.msp", SearchOption.TopDirectoryOnly)[0];
                var records = MspFileParser.MspFileReader(mspfile);
                var entroies = new List<string>();
                for (int i = 0; i < records.Count; i++) {
                    var record = records[i];
                    var flg = false;
                    foreach (var spec in record.Spectrum) {
                        if (Math.Abs(spec.Mass - 144.0807) < 0.01) {
                            flg = true;
                            break;
                        }
                    }
                    if (j == 0) {
                        recordnames.Add(record.Comment.Replace("_Curated", ""));
                    }
                    entroies.Add(flg.ToString());
                }
                entropiesList.Add(entroies);
            }

            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("Name" + String.Join("\t", foldernames));
                for (int i = 0; i < recordnames.Count; i++) {
                    var exports = new List<string>();
                    for (int j = 0; j < entropiesList.Count; j++) {
                        exports.Add(entropiesList[j][i].ToString());
                    }
                    sw.WriteLine(recordnames[i] + "\t" + String.Join("\t", exports));
                }
            }
        }


        public static void TestMolecularNetworkingFunctions(string inputfile, string outputdir) {
            var minimumPeakMatch = 4;
            var matchThreshold = 0.9;
            var maxEdgeNumPerNode = 10;
            var maxPrecursorDiff = 400.0;
            var maxPrecursorDiff_Percent = 100;
            var records = MspFileParser.MspFileReader(inputfile);

            var processedSpectraSaveFile = Path.Combine(outputdir, "processedspectra.msp");

            using (var sw = new StreamWriter(processedSpectraSaveFile)) {
                foreach (var record in records) {
                    record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz);
                    MspFileParser.WriteSpectrumAsMsp(record, sw);
                }
            }

            for (int i = 0; i < records.Count; i++) {
                for (int j = i + 1; j < records.Count; j++) {
                    var prop1 = records[i];
                    var prop2 = records[j];
                    var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
                    if (massDiff > maxPrecursorDiff) continue;
                    if (Math.Max(prop1.PrecursorMz, prop2.PrecursorMz) * maxPrecursorDiff_Percent * 0.01 - Math.Min(prop1.PrecursorMz, prop2.PrecursorMz) < 0) continue;

                    var scoreitem = MsScanMatching.GetModifiedDotProductScore(prop1, prop2);
                    if (scoreitem[1] < minimumPeakMatch) continue;
                    if (scoreitem[0] < matchThreshold) continue;
                }
            }
        }

        public static void CountTrueFalse(string inputdir, string output) {
            var files = Directory.GetFiles(inputdir, "*.txt", SearchOption.TopDirectoryOnly);
            var dict = new Dictionary<string, string>() {
                { "Curated_10CID_edge", "CID 10V" },
                { "Curated_20CID_edge", "CID 20V" },
                { "Curated_40CID_edge", "CID 40V" },
                { "Curated_10CID_15CES_edge", "CID 10V CES 15V" },
                { "Curated_20CID_15CES_edge", "CID 20V CES 15V" },
                { "Curated_40CID_15CES_edge", "CID 40V CES 15V" },
                { "Curated_10KE_edge", "EAD 10eV CID 10V" },
                { "Curated_15KE_edge", "EAD 15eV CID 10V" },
                { "Curated_20KE_edge", "EAD 20eV CID 10V" },
            };

            using (var sw = new StreamWriter(output)) {
                //sw.WriteLine("Name\tTotal\tSuperclass\tClass\tSubclass\tParent\tSuperclassRatio\tClassRatio\tSubclassRatio\tParentRatio");
                sw.WriteLine("Name,Label,Count,Ratio,CountPosition,RatioPosition");
                foreach (var f in files) {
                    var filename = Path.GetFileNameWithoutExtension(f);
                    var filecomment = dict[filename];
                    var TotalCount = 0;
                    var MatchSuperClass = 0;
                    var MatchClass = 0;
                    var MatchSubclass = 0;
                    var MatchParent = 0;
                    using (var sr = new StreamReader(f)) {
                        sr.ReadLine();
                        while (sr.Peek() > -1) {
                            TotalCount++;
                            var line = sr.ReadLine();
                            var linearray = line.Split('\t');
                            if (linearray[8].Length > 0 && linearray[12].Length > 0 &&
                                linearray[4] == "TRUE") { MatchSuperClass++; }
                            if (linearray[9].Length > 0 && linearray[13].Length > 0 &&
                                linearray[5] == "TRUE") { MatchClass++; }
                            if (linearray[10].Length > 0 && linearray[14].Length > 0 &&
                                linearray[6] == "TRUE") { MatchSubclass++; }
                            if (linearray[11].Length > 0 && linearray[15].Length > 0 &&
                                linearray[7] == "TRUE") { MatchParent++; }
                        }
                    }
                    //var export = new List<string>() {
                    //    filecomment,
                    //    TotalCount.ToString(),
                    //    MatchSuperClass.ToString(),
                    //    MatchClass.ToString(),
                    //    MatchSubclass.ToString(),
                    //    MatchParent.ToString(),
                    //    ((double)MatchSuperClass / (double)TotalCount * 100.0).ToString(),
                    //    ((double)MatchClass / (double)TotalCount * 100.0).ToString(),
                    //    ((double)MatchSubclass / (double)TotalCount * 100.0).ToString(),
                    //    ((double)MatchParent / (double)TotalCount * 100.0).ToString()
                    //};
                    //sw.WriteLine(String.Join("\t", export));
                    var superClassRatio = (double)MatchSuperClass / (double)TotalCount * 100.0;
                    var classRatio = (double)MatchClass / (double)TotalCount * 100.0;
                    var subClassRatio = (double)MatchSubclass / (double)TotalCount * 100.0;
                    var parentRatio = (double)MatchParent / (double)TotalCount * 100.0;


                    var matchSuperClassPosition = MatchSuperClass * 0.5;
                    var matchSuperClassRatioPosition = superClassRatio * 0.5;
                    var matchClassPosition = MatchSuperClass + MatchClass * 0.5;
                    var matchClassRatioPosition = superClassRatio + classRatio * 0.5;
                    var matchSubclassPosition = MatchSuperClass + MatchClass + MatchSubclass * 0.5;
                    var matchSubclassRatioPosition = superClassRatio + classRatio + subClassRatio * 0.5;
                    var matchParentPosition = MatchSuperClass + MatchClass + MatchSubclass + MatchParent * 0.5;
                    var matchParentRatioPosition = superClassRatio + classRatio + subClassRatio + parentRatio * 0.5;

                    sw.WriteLine(filecomment + ",4_SuperClass," + MatchSuperClass + "," + superClassRatio.ToString() + "," + matchSuperClassPosition + "," + matchSuperClassRatioPosition.ToString());
                    sw.WriteLine(filecomment + ",3_Class," + MatchClass + "," + classRatio.ToString() + "," + matchClassPosition + "," + matchClassRatioPosition.ToString());
                    sw.WriteLine(filecomment + ",2_Subclass," + MatchSubclass + "," + subClassRatio.ToString() + "," + matchSubclassPosition + "," + matchSubclassRatioPosition.ToString());
                    sw.WriteLine(filecomment + ",1_Parent," + MatchParent + "," + parentRatio.ToString() + "," + matchParentPosition + "," + matchParentRatioPosition.ToString());
                }
            }
        }

        public static void GenerateMoleculerSpectrumNetforkFilesByModifiedDotProductFunction(string inputdir, string outputdir) {
            var folders = Directory.GetDirectories(inputdir, "*", SearchOption.TopDirectoryOnly);
            if (!Directory.Exists(outputdir)) {
                Directory.CreateDirectory(outputdir);
            }

            var minimumPeakMatch = 6;
            var matchThreshold = 0.8;
            var maxEdgeNumPerNode = 10;
            var maxPrecursorDiff = 400.0;
            var maxPrecursorDiff_Percent = 100;

            foreach (var folder in folders) {
                Console.WriteLine("Start {0}", folder);
                var files = Directory.GetFiles(folder, "*.msp", SearchOption.TopDirectoryOnly);
                foreach (var file in files) {
                    var edgefilename = Path.GetFileNameWithoutExtension(file) + "_edge.txt";
                    var edgefile = Path.Combine(outputdir, edgefilename);
                    var records = MspFileParser.MspFileReader(file);
                    foreach (var record in records) {
                        record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz);
                    }

                    var node2links = new Dictionary<int, List<LinkNode>>();

                    for (int i = 0; i < records.Count; i++) {
                        for (int j = i + 1; j < records.Count; j++) {
                            var prop1 = records[i];
                            var prop2 = records[j];
                            var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
                            if (massDiff > maxPrecursorDiff) continue;
                            if (Math.Max(prop1.PrecursorMz, prop2.PrecursorMz) * maxPrecursorDiff_Percent * 0.01 - Math.Min(prop1.PrecursorMz, prop2.PrecursorMz) < 0) continue;

                            var scoreitem = MsScanMatching.GetModifiedDotProductScore(prop1, prop2);
                            if (scoreitem[1] < minimumPeakMatch) continue;
                            if (scoreitem[0] < matchThreshold) continue;

                            if (node2links.ContainsKey(i)) {
                                node2links[i].Add(new LinkNode() { Score = scoreitem, Node = records[j] });
                            }
                            else {
                                node2links[i] = new List<LinkNode>() { new LinkNode() { Score = scoreitem, Node = records[j] } };
                            }
                        }
                    }

                    var cNode2Links = new Dictionary<int, List<LinkNode>>();

                    foreach (var item in node2links) {
                        var nitem = item.Value.OrderByDescending(n => n.Score[1]).ToList();
                        cNode2Links[item.Key] = new List<LinkNode>();
                        for (int i = 0; i < nitem.Count; i++) {
                            if (i > maxEdgeNumPerNode - 1) break;
                            cNode2Links[item.Key].Add(nitem[i]);
                        }
                    }

                    using (var sw = new StreamWriter(edgefile)) {
                        sw.WriteLine("Source\tTarget\tSimilarity\tMatchNumber\tMatchSuperClass\tMatchClass\tMatchSubclass\tMatchParent\tSourceSuperClass\tSourceClass\tSourceSubclass\tSourceParent\tTargetSuperClass\tTargetClass\tTargetSubclass\tTargetParent");
                        foreach (var item in cNode2Links) {
                            foreach (var link in item.Value) {

                                var sourceOntologies = records[item.Key].Ontology.Split(';').ToList().Where(n => n != string.Empty).Select(n => n.Split('_')[1]).ToList();
                                var targetOntologies = link.Node.Ontology.Split(';').ToList().Where(n => n != string.Empty).Select(n => n.Split('_')[1]).ToList();
                                var matcher = new List<string>();
                                for (int i = 0; i < sourceOntologies.Count; i++) {
                                    if (sourceOntologies[i] != targetOntologies[i]) {
                                        matcher.Add("FALSE");
                                    }
                                    else {
                                        matcher.Add("TRUE");
                                    }
                                }

                                sw.WriteLine(records[item.Key].Comment.Replace("_Curated", "") + "\t" +
                                    link.Node.Comment.Replace("_Curated", "") + "\t" + link.Score[0] + "\t" + link.Score[1] + "\t" +
                                    String.Join("\t", matcher) + "\t" +
                                    String.Join("\t", sourceOntologies) + "\t" +
                                    String.Join("\t", targetOntologies));
                            }
                        }
                    }
                }
            }
        }
    }
}
