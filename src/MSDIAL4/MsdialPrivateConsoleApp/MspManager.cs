using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompMs.Common.Extension;
using CompMs.Common.Components;

namespace MsdialPrivateConsoleApp {
    public sealed class MspManager {
        private MspManager() {

        }

        public static void MergeMspFiles(string folder, string outputfile) {
            var files = Directory.GetFiles(folder, "*.*msp", SearchOption.AllDirectories);
            using (var sw = new StreamWriter(outputfile, false, Encoding.ASCII)) {
                foreach (var file in files) {
                    var queries = MspFileParser.MspFileReader(file);
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var rfilename = filename.Replace("MSMS-Public_experimentspectra-neg-VS19", "msp").
                        Replace("MetaboBank_db_", "").
                        Replace("ds_", "").
                        Replace("_mn_", "$");

                    var counter = 0;
                    foreach (var key in queries) {
                        var name = key.Name;
                        var formula = key.Formula == null ? "null" : key.Formula.ToString();
                        var ontology = key.Ontology;
                        var inchikey = key.InChIKey;
                        var smiles = key.SMILES;
                        var dbidentifier = key.Name;
                        if (rfilename != "msp") {
                            name = rfilename + "_" + counter.ToString();
                            formula = "null";
                            ontology = "null";
                            inchikey = "null";
                            smiles = "null";
                            dbidentifier = name;
                        }
                        else {
                            dbidentifier = "msp_" + counter.ToString();
                        }

                        sw.WriteLine(String.Join(": ", new string[] { "NAME", name }));
                        sw.WriteLine(String.Join(": ", new string[] { "PRECURSORMZ", key.PrecursorMz.ToString() }));
                        sw.WriteLine(String.Join(": ", new string[] { "PRECURSORTYPE", key.AdductType.AdductIonName }));
                        sw.WriteLine(String.Join(": ", new string[] { "IONMODE", key.IonMode.ToString() }));
                        sw.WriteLine(String.Join(": ", new string[] { "FORMULA", formula }));
                        sw.WriteLine(String.Join(": ", new string[] { "ONTOLOGY", ontology }));
                        sw.WriteLine(String.Join(": ", new string[] { "INCHIKEY", inchikey }));
                        sw.WriteLine(String.Join(": ", new string[] { "SMILES", smiles }));
                        sw.WriteLine(String.Join(": ", new string[] { "INSTRUMENTTYPE", key.InstrumentType }));
                        sw.WriteLine(String.Join(": ", new string[] { "COLLISIONENERGY", key.FragmentationCondition }));
                        sw.WriteLine(String.Join(": ", new string[] { "RETENTIONTIME", key.ChromXs.RT.Value.ToString() }));
                        sw.WriteLine(String.Join(": ", new string[] { "CCS", key.CollisionCrossSection.ToString() }));
                        sw.WriteLine(String.Join(": ", new string[] { "DatabaseUniqueIdentifier", dbidentifier }));
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
                        counter++;
                    }
                }
            }
        }

        public static void MergeEdgePairs(string folder, string outputfile) {
            var files = Directory.GetFiles(folder, "*.pairs", SearchOption.AllDirectories);
            using (var sw = new StreamWriter(outputfile, false, Encoding.ASCII)) {
                sw.WriteLine("source_id\ttarget_id\tBonanzaScore\tMatchPeakCount\tModDotScore\tCosineScore");
                foreach (var file in files) {
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var rfilename = filename.Replace("MSMS-Public_experimentspectra-neg-VS19", "msp").
                        Replace("MetaboBank_db_", "").
                        Replace("ds_", "").
                        Replace("_mn_", "$");
                    var leftfilename = rfilename.Split('$')[0];
                    var rightfilename = rfilename.Split('$')[1];

                    using (var sr = new StreamReader(file)) {
                        sr.ReadLine();
                        while (sr.Peek() > -1) {
                            var line = sr.ReadLine();
                            var linearray = line.Split('\t');
                            var matchpeak = int.Parse(linearray[3]);
                            if (matchpeak > 10) {
                                sw.WriteLine(leftfilename + "_" + linearray[0] + "\t" + rightfilename + "_" + linearray[1] + "\t" +
                                    linearray[2] + "\t" + linearray[3] + "\t" + linearray[4] + "\t" + linearray[5]);
                            }
                        }
                    }
                }
            }
        }

        public static void Msp2TextAsMsdialAlignmentResultFormat(string mspfile, string outputfile, string suffix) {
            var queries = MspFileParser.MspFileReader(mspfile);
            var header = new List<string>() {
                "Unique", "FileName", "Alignment ID", 
                "Average Rt(min)", "Average Mz", "Metabolite name",
                "Adduct type", "Post curation result", "Fill %", "MS/MS assigned", "Reference RT", 
                "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES", 
                "Annotation tag (VS1.0)", "RT matched", "m/z matched", "MS/MS matched", 
                "Comment", "Manually modified for quantification", "Manually modified for annotation", 
                "Isotope tracking parent ID", "Isotope tracking weight number", "Total score", "RT similarity", 
                "Dot product", "Reverse dot product", "Fragment presence %", "S/N average", 
                "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
            };

            using (var sw = new StreamWriter(outputfile, false, Encoding.ASCII)) {
                sw.WriteLine(String.Join("\t", header));
                var counter = 0;
                foreach (var query in queries) {
                    var ontology = "null";
                    if (query.Ontology.Split('|').Length == 4) ontology = query.Ontology.Split('|')[2];
                    else ontology = query.Ontology;
                    var stringvalues = new List<string>() {
                        suffix + "_" + counter.ToString(), suffix, counter.ToString(), query.ChromXs.Value.ToString(),
                        query.PrecursorMz.ToString(), query.Name, query.AdductType.ToString(), "null", "1", "TRUE", query.ChromXs.Value.ToString(),
                        query.PrecursorMz.ToString(), query.Formula.ToString(), ontology, query.InChIKey, query.SMILES,
                        "100", "TRUE", "TRUE", "TRUE", query.Comment, "FALSE", "FALSE", "null", "null", "100", "100", "100", "100", "100", "100", "null", "null",
                        "null" };
                    sw.WriteLine(String.Join("\t", stringvalues));
                    counter++;
                }
            }
        }

        public static void Msp2TextAsMsdialAlignmentResultFormat(string mspfile, string outputfile) {
            var queries = MspFileParser.MspFileReader(mspfile);
            var header = new List<string>() {
                "Unique", "FileName", "Alignment ID",
                "Average Rt(min)", "Average Mz", "Metabolite name",
                "Adduct type", "Post curation result", "Fill %", "MS/MS assigned", "Reference RT",
                "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES",
                "Annotation tag (VS1.0)", "RT matched", "m/z matched", "MS/MS matched",
                "Comment", "Manually modified for quantification", "Manually modified for annotation",
                "Isotope tracking parent ID", "Isotope tracking weight number", "Total score", "RT similarity",
                "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
                "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
            };

            using (var sw = new StreamWriter(outputfile, false, Encoding.ASCII)) {
                sw.WriteLine(String.Join("\t", header));
                var counter = 0;
                foreach (var query in queries) {
                    var ontology = "null";
                    if (query.Ontology.Split('|').Length == 4) ontology = query.Ontology.Split('|')[2];
                    else ontology = query.Ontology;
                    var formula = query.Formula == null ? null : query.Formula.ToString();
                    var filename = query.DatabaseUniqueIdentifier.Split('_').Length > 2 ?
                        query.DatabaseUniqueIdentifier.Split('_')[0] + "_" + query.DatabaseUniqueIdentifier.Split('_')[1] :
                        query.DatabaseUniqueIdentifier.Split('_')[0];
                    var id = query.DatabaseUniqueIdentifier.Split('_').Length > 2 ?
                        query.DatabaseUniqueIdentifier.Split('_')[2] :
                        query.DatabaseUniqueIdentifier.Split('_')[1];


                    var stringvalues = new List<string>() {
                        query.DatabaseUniqueIdentifier, filename, id, query.ChromXs.Value.ToString(),
                        query.PrecursorMz.ToString(), query.Name, query.AdductType.ToString(), "null", "1", "TRUE", query.ChromXs.Value.ToString(),
                        query.PrecursorMz.ToString(), formula, ontology, query.InChIKey, query.SMILES,
                        "100", "TRUE", "TRUE", "TRUE", query.Comment, "FALSE", "FALSE", "null", "null", "100", "100", "100", "100", "100", "100", "null", "null",
                        "null" };
                    sw.WriteLine(String.Join("\t", stringvalues));
                    counter++;
                }
            }
        }

        public static void ExtractNegativeSpectra(string mspfile, string new_mspfile) {
            var queries = MspFileParser.MspFileReader(mspfile);
            using (var sw = new StreamWriter(new_mspfile, false, Encoding.ASCII)) {
                foreach (var query in queries) {
                    var ionmode = query.AdductType.IonMode;
                    if (ionmode == CompMs.Common.Enum.IonMode.Negative) {
                        WriteAsMsp(sw, query);
                    }
                }
            }
        }

        public static void ExtractPositiveSpectra(string mspfile, string new_mspfile) {
            var queries = MspFileParser.MspFileReader(mspfile);
            using (var sw = new StreamWriter(new_mspfile, false, Encoding.ASCII)) {
                foreach (var query in queries) {
                    var ionmode = query.AdductType.IonMode;
                    if (ionmode == CompMs.Common.Enum.IonMode.Negative) {
                        WriteAsMsp(sw, query);
                    }
                }
            }
        }

        public static void AddOntologies(string mspfile, string inchikeytable, string new_mspfile) {
            var queries = MspFileParser.MspFileReader(mspfile);
            var inchikey2ontology = new Dictionary<string, string[]>();
            using (var sr = new StreamReader(inchikeytable, true)) { // key contains the list of short inchikey without header
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var linearray = line.Split('\t');

                    var classontology = linearray[4].IsEmptyOrNull() ? "null" : linearray[4];
                    inchikey2ontology[linearray[0].Split('-')[0]] = new string[] { linearray[2], linearray[3], classontology, linearray[1] };
                }
            }

            var unhandleinchikeys = new List<string>();
            var error_report_file = Path.Combine(Path.GetDirectoryName(new_mspfile), Path.GetFileNameWithoutExtension(new_mspfile) + "_unhandledinchikey.txt");
            using (var sw = new StreamWriter(new_mspfile, false, Encoding.ASCII)) {
                foreach (var key in queries) {

                    var inchikey = key.InChIKey;
                    if (inchikey.IsEmptyOrNull()) continue;
                    var shortinchikey = inchikey.Split('-')[0];
                    var ontologystring = "null|null|null|null";
                    if (inchikey2ontology.ContainsKey(shortinchikey)) {
                        ontologystring = String.Join("|", inchikey2ontology[shortinchikey]);
                    }
                    else {
                        unhandleinchikeys.Add(inchikey + "\t" + key.SMILES);
                    }
                    key.Ontology = ontologystring;
                    WriteAsMsp(sw, key);
                }
            }

            using (var sw = new StreamWriter(error_report_file, false, Encoding.ASCII)) {
                sw.WriteLine("InChIKey\tSMILES");
                foreach (var inchikey in unhandleinchikeys) sw.WriteLine(inchikey);
            }
        }

        public static void WriteAsMsp(StreamWriter sw, MoleculeMsReference key) {
            sw.WriteLine(String.Join(": ", new string[] { "NAME", key.Name }));
            sw.WriteLine(String.Join(": ", new string[] { "PRECURSORMZ", key.PrecursorMz.ToString() }));
            sw.WriteLine(String.Join(": ", new string[] { "PRECURSORTYPE", key.AdductType.AdductIonName }));
            sw.WriteLine(String.Join(": ", new string[] { "IONMODE", key.IonMode.ToString() }));
            sw.WriteLine(String.Join(": ", new string[] { "FORMULA", key.Formula.FormulaString }));
            sw.WriteLine(String.Join(": ", new string[] { "ONTOLOGY", key.Ontology }));
            sw.WriteLine(String.Join(": ", new string[] { "INCHIKEY", key.InChIKey }));
            sw.WriteLine(String.Join(": ", new string[] { "SMILES", key.SMILES }));
            sw.WriteLine(String.Join(": ", new string[] { "INSTRUMENTTYPE", key.InstrumentType }));
            sw.WriteLine(String.Join(": ", new string[] { "COLLISIONENERGY", key.FragmentationCondition }));
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
