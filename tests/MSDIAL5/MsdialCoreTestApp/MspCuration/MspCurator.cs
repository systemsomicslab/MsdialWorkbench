using CompMs.Common.Parser;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CompMs.Common.Components;
using CompMs.Common.MessagePack;

namespace CompMs.App.MsdialConsole.MspCuration {

    public sealed class MspCurator {
        private MspCurator() { }

        public static void MergeMSPs(string inputDir, string inputFileList, string output) {
            var files = new List<string>();
            using (var sr = new StreamReader(inputFileList)) {
                while (sr.Peek() != -1) { 
                    files.Add(sr.ReadLine());
                }
            }
            using (var sw = new StreamWriter(output)) {
                foreach (var file in files) {
                    var filepath = Path.Combine(inputDir, file);
                    var mspRecords = MspFileParser.MspFileReader(filepath);
                    foreach (var mspRecord in mspRecords) {
                        MspFileParser.WriteSpectrumAsMsp(mspRecord, sw);
                    }
                }
            }
        }

        public static void Batch_ExtractMSPsByCEField(string inputdir, string outputdir, string termfilter = "") {
            var files = Directory.GetFiles(inputdir);
            var records = new List<MoleculeMsReference>();
            foreach (var file in files) {
                var mspRecords = MspFileParser.MspFileReader(file);
                foreach (var mspRecord in mspRecords) {
                    mspRecord.Comment = Path.GetFileNameWithoutExtension(file);
                    records.Add(mspRecord);
                }
            }

            var dict = new Dictionary<string, string>() {
                { "Curated_10CID", "CID 10V" },
                { "Curated_20CID", "CID 20V" },
                { "Curated_40CID", "CID 40V" },
                { "Curated_10CID_15CES", "CID 10V CES 15V" },
                { "Curated_20CID_15CES", "CID 20V CES 15V" },
                { "Curated_40CID_15CES", "CID 40V CES 15V" },
                { "Curated_10KE", "EAD 10eV CID 10V" },
                { "Curated_15KE", "EAD 15eV CID 10V" },
                { "Curated_20KE", "EAD 20eV CID 10V" },
            };

            foreach (var item in dict) {
                var filepath = Path.Combine(outputdir, item.Key);
                if (!Directory.Exists(filepath)) {
                    Directory.CreateDirectory(filepath);
                }
            }
            var dir_list = Directory.GetDirectories(outputdir, "*", SearchOption.TopDirectoryOnly);
            foreach (var dir in dir_list) {
                var dirname = Path.GetFileNameWithoutExtension(dir);
                if (!dirname.Contains("Curated")) continue;
                var path = Path.Combine(dir, dirname + ".msp");


                using (var sw = new StreamWriter(path)) {
                    foreach (var record in records) {
                        if (termfilter != "" && !record.Ontology.Contains(termfilter)) continue;
                        if (record.FragmentationCondition == dict[dirname]) {
                            MspFileParser.WriteSpectrumAsMsp(record, sw);
                        }
                    }
                }
            }
        }

        public static void ExtractMSPsByCEField(string inputDir, string output, string fieldname) {
            var files = Directory.GetFiles(inputDir);
            var records = new List<MoleculeMsReference>();
            foreach (var file in files) {
                var mspRecords = MspFileParser.MspFileReader(file);
                foreach (var mspRecord in mspRecords) {
                    records.Add(mspRecord);
                }
            }

            using (var sw = new StreamWriter(output)) {
                foreach (var record in records) {
                    if (record.FragmentationCondition == fieldname) {
                        MspFileParser.WriteSpectrumAsMsp(record, sw);
                    }
                }
            }
        }

        public static void WriteRtMzInChIKey(string mspfile) {
            var mspRecords = MspFileParser.MspFileReader(mspfile);
            var filename = System.IO.Path.GetFileNameWithoutExtension(mspfile);
            var directory = Path.GetDirectoryName(mspfile);
            var output = Path.Combine(directory, filename + "_kiuchi_temp.txt");
            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("RT\tMZ\tInChIKey");
                foreach (var query in mspRecords) {
                    sw.WriteLine(query.ChromXs.RT.Value + "\t" + query.PrecursorMz + "\t" + query.InChIKey);
                }
            }
        }

        public static void ExtractCCSValues(string input, string output) {
            var queries = MoleculeMsRefMethods.LoadMspFromFile(input);
            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("Name\tInChIKey\tShortInCHIKey\tAdduct\tCCS");
                foreach (var query in queries) {
                    var line = new List<string>() { query.Name, query.InChIKey, query.InChIKey.Split('-')[0], query.AdductType.ToString(), query.CollisionCrossSection.ToString() };
                    sw.WriteLine(String.Join("\t", line));
                }
            }
        }

        public static void MatchInChIKeyAndAdductToExtractCCS(string inputExp, string inputRef, string output) {
            var dict = new Dictionary<string, string>();    
            using (var sr = new StreamReader(inputRef)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');

                    var adduct = linearray[3];
                    var shortinchi = linearray[2];
                    var inchi_adduct = shortinchi + "_" + adduct;
                    var ccs = linearray[4];
                    if (!dict.ContainsKey(inchi_adduct)) {
                        dict.Add(inchi_adduct, ccs);
                    }
                }
            }

            var ccsvalues = new List<string>();
            using (var sr = new StreamReader(inputExp)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');

                    var adduct = linearray[3];
                    var shortinchi = linearray[4].Split('-')[0];
                    var inchi_adduct = shortinchi + "_" + adduct;

                    if (dict.ContainsKey(inchi_adduct)) {
                        ccsvalues.Add(dict[inchi_adduct]);
                    }
                    else {
                        ccsvalues.Add("null");
                    }
                }
            }

            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("ccs");
                foreach (var ccs in ccsvalues) sw.WriteLine(ccs);
            }
        }

        public static void MatchNameAndAdductToExtractCCS(string inputExp, string inputRef, string output) {
            var dict = new Dictionary<string, string>();
            using (var sr = new StreamReader(inputRef)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');

                    var adduct = linearray[3];
                    var name = linearray[0];
                    var name_adduct = name + "_" + adduct;
                    var ccs = linearray[4];
                    if (!dict.ContainsKey(name_adduct)) {
                        dict.Add(name_adduct, ccs);
                    }
                }
            }

            var ccsvalues = new List<string>();
            using (var sr = new StreamReader(inputExp)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');

                    var adduct = linearray[3];
                    var name = linearray[0];
                    var name_adduct = name + "_" + adduct;

                    if (dict.ContainsKey(name_adduct)) {
                        ccsvalues.Add(dict[name_adduct]);
                    }
                    else {
                        ccsvalues.Add("null");
                    }
                }
            }

            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("ccs");
                foreach (var ccs in ccsvalues) sw.WriteLine(ccs);
            }
        }

        public static void AddRT2MspQueries(string mspfile, string textfile) {
            Console.WriteLine("Reading msp file");
            var mspRecords = MspFileParser.MspFileReader(mspfile);
            var inchi2RT = new Dictionary<string, string>();
            using (var sr = new StreamReader(textfile)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty || line is null) continue;
                    var linearray = line.Split('\t');
                    var shortInchi = linearray[3].Substring(0, 14);
                    if (inchi2RT.ContainsKey(shortInchi)) continue;
                    inchi2RT[shortInchi] = linearray[2];
                }
            }

            // add retention time field to msp record
            foreach (var mRecord in mspRecords) {
                if (mRecord.InChIKey.IsEmptyOrNull()) {
                    mRecord.ChromXs.RT = new RetentionTime(-1, mRecord.ChromXs.RT.Unit);
                    continue;
                }
                var shortInChi = mRecord.InChIKey.Substring(0, 14);
                if (inchi2RT.ContainsKey(shortInChi)) {
                    double d;
                    if (double.TryParse(inchi2RT[shortInChi], out d)) {
                        mRecord.ChromXs.RT = new RetentionTime(d, mRecord.ChromXs.RT.Unit);
                    }
                    else {
                        mRecord.ChromXs.RT = new RetentionTime(-1, mRecord.ChromXs.RT.Unit);
                    }
                }
                else {
                    mRecord.ChromXs.RT = new RetentionTime(-1, mRecord.ChromXs.RT.Unit);
                }
            }

            var filename = System.IO.Path.GetFileNameWithoutExtension(mspfile);
            var directory = Path.GetDirectoryName(mspfile);

            var outputfileWithRT = Path.Combine(directory, filename + "_PfppRT.msp");
            var outputfileWithoutRT = Path.Combine(directory, filename + "_WithoutRT.msp");

            Console.WriteLine("Writing to msp file");
            MspFileParser.WriteAsMsp(outputfileWithRT, mspRecords.Where(n => n.ChromXs.RT.Value > 0).ToList());
            MspFileParser.WriteAsMsp(outputfileWithoutRT, mspRecords.Where(n => n.ChromXs.RT.Value ==  -1).ToList());
        }
    }
}
