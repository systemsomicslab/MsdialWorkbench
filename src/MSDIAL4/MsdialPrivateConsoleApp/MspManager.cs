using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompMs.Common.Extension;

namespace MsdialPrivateConsoleApp {
    public sealed class MspManager {
        private MspManager() {

        }

        public static void AddMetadata2Msp(string inputfolder, string tablefile, string outputfolder) {
            var mspfiles = Directory.GetFiles(inputfolder);
            var proton = 1.007276;
            var na = 22.989218;
            var nh4 = 18.033823;

            if (inputfolder == outputfolder) {
                Console.WriteLine("input and output folders must be differently located.");
                return;
            }

            var dict = new Dictionary<int, string>();
            dict.Add(0, "CID 45V");
            dict.Add(1, "EAD 8eV CID 12V");
            dict.Add(2, "EAD 10eV CID 12V");
            dict.Add(3, "EAD 12eV CID 12V");
            dict.Add(4, "EAD 14eV CID 12V");
            dict.Add(5, "EAD 16eV CID 12V");
            dict.Add(6, "EAD 18eV CID 12V");
            dict.Add(7, "EAD 20eV CID 12V");

            var queries = new List<string[]>();
            using (var sr = new StreamReader(tablefile)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');
                    queries.Add(linearray);
                }
            }

            foreach (var mspfile in mspfiles) {
                var records = MspFileParser.MspFileReader(mspfile);
                var mspfilename = Path.GetFileName(mspfile);

                foreach (var query in queries) {
                    var id = query[1];
                    if (mspfilename.Contains(id)) {
                        var exactmass = double.Parse(query[10]);
                        var mz = query[12] == "[M+H]+" ? exactmass + proton :
                            query[12] == "[M+NH4]+" ? exactmass + nh4 :
                            query[12] == "[M+2H]2+" ? exactmass / 2 + proton :
                            query[12] == "[M+2Na]2+" ? exactmass / 2 + na :
                            exactmass + na;

                        var output = Path.Combine(outputfolder, mspfilename);

                        using (var sw = new StreamWriter(output)) {
                            for (int i = 0; i < records.Count; i++) {
                                var record = records[i];
                                var ce = "EAD 14eV CID 10V";
                                if (records.Count == 8) {
                                    ce = dict[i];
                                }
                                else {
                                    Console.WriteLine(output);
                                }

                                sw.WriteLine("NAME: " + query[3]);
                                sw.WriteLine("PRECURSORMZ: " + mz);
                                sw.WriteLine("PRECURSORTYPE: " + query[12]);
                                sw.WriteLine("FORMULA: " + query[11]);
                                sw.WriteLine("ONTOLOGY: " + query[9]);
                                sw.WriteLine("INCHIKEY: " + query[7]);
                                sw.WriteLine("SMILES: " + query[8]);
                                sw.WriteLine("COLLISIONENERGY: " + ce);
                                sw.WriteLine("RETENTIONTIME: " + record.ChromXs.RT.Value);
                                sw.WriteLine("COMMENT: " + record.Comment);
                                sw.WriteLine("Num Peaks: " + record.Spectrum.Count);
                                foreach (var peak in record.Spectrum) {
                                    sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
                                }
                                sw.WriteLine();
                            }
                        }


                        break;
                    }
                }
            }

        }

        public static void ExactMassChecker(string inputfolder, string tablefile, string outputfile) {
            var mspfiles = Directory.GetFiles(inputfolder);
            var proton = 1.007276;
            var na = 22.989218;
            var nh4 = 18.033823;

            var queries = new List<string[]>();
            using (var sr = new StreamReader(tablefile)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');
                    queries.Add(linearray);
                }
            }

            using (var sw = new StreamWriter(outputfile)) {
                foreach (var mspfile in mspfiles) {
                    var records = MspFileParser.MspFileReader(mspfile);
                    var mspfilename = Path.GetFileNameWithoutExtension(mspfile);

                    foreach (var query in queries) {
                        var id = query[7];
                        if (mspfilename.Contains(id)) {

                            var exactmass = double.Parse(query[9]);
                            var mz = query[11] == "[M+H]+" ? exactmass + proton :
                                query[11] == "[M+NH4]+" ? exactmass + nh4 :
                                query[11] == "[M+2H]2+" ? exactmass / 2 + proton :
                                query[11] == "[M+2Na]2+" ? exactmass / 2 + na :
                                exactmass + na;

                            if (Math.Abs(mz - records[0].PrecursorMz) < 0.02) {
                                sw.WriteLine(mspfilename + "\t" + "TRUE");
                            }
                            else {
                                sw.WriteLine(mspfilename + "\t" + "FALSE");
                            }
                            break;
                        }
                    }
                }
            }
        }

        public static void CurateOntologyField(string mspfile, string inchikeytable, string new_mspfile) {
            var queries = MspFileParser.MspFileReader(mspfile);
            var inchikey2ontology = new Dictionary<string, string>();
            using (var sr = new StreamReader(inchikeytable, true)) { // key contains the list of short inchikey without header
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var linearray = line.Split('\t');
                    inchikey2ontology[linearray[0]] = linearray[2];
                }
            }

            using (var sw = new StreamWriter(new_mspfile, false, Encoding.ASCII)) {
                foreach (var key in queries) {

                    var inchikey = key.InChIKey;
                    if (inchikey.IsEmptyOrNull()) continue;
                    var shortinchikey = inchikey.Split('-')[0];
                    if (!inchikey2ontology.ContainsKey(shortinchikey)) continue;
                    if (key.AdductType.AdductIonName != "[M+H]+" && key.AdductType.AdductIonName != "[M-H]-") continue; // temp

                    sw.WriteLine(String.Join(": ", new string[] { "NAME", key.Name }));
                    sw.WriteLine(String.Join(": ", new string[] { "PRECURSORMZ", key.PrecursorMz.ToString() }));
                    sw.WriteLine(String.Join(": ", new string[] { "PRECURSORTYPE", key.AdductType.AdductIonName }));
                    sw.WriteLine(String.Join(": ", new string[] { "IONMODE", key.IonMode.ToString() }));
                    sw.WriteLine(String.Join(": ", new string[] { "FORMULA", key.Formula.FormulaString }));
                    sw.WriteLine(String.Join(": ", new string[] { "ONTOLOGY", inchikey2ontology[shortinchikey] }));
                    sw.WriteLine(String.Join(": ", new string[] { "INCHIKEY", key.InChIKey }));
                    sw.WriteLine(String.Join(": ", new string[] { "SMILES", key.SMILES }));
                    sw.WriteLine(String.Join(": ", new string[] { "RETENTIONTIME", key.ChromXs.RT.Value.ToString() }));
                    sw.WriteLine(String.Join(": ", new string[] { "CCS", key.CollisionCrossSection.ToString() }));
                    sw.WriteLine(String.Join(": ", new string[] { "COMMENT", key.Comment }));

                    var peaks = key.Spectrum;
                    //spectrum num
                    var numPeaks = peaks.Count.ToString();
                    sw.WriteLine(String.Join(": ", new string[] { "Num Peaks", numPeaks }));
                    //spectrum list
                    foreach (var peak in peaks) {
                        sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
                    };
                    sw.WriteLine();
                }
            }
        }
    }
}
