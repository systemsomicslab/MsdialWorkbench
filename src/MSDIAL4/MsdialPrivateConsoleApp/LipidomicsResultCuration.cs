using Msdial.Lcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.Lipidomics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MsdialPrivateConsoleApp {

    public class TestQuery {

        public string NAME { get; set; }
        public string RETENTIONTIME { get; set; }
        public string CCS { get; set; }
        public string PRECURSORMZ { get; set; }
        public string PRECURSORTYPE { get; set; }
        public string IONMODE { get; set; }
        public string FORMULA { get; set; }
        public string ONTOLOGY { get; set; }
        public string INCHIKEY { get; set; }
        public string SMILES { get; set; }
        public string COMMENT { get; set; }
        public string NumPeaks { get; set; }
        public string Spectrum { get; set; }

        public PeakAreaBean peakAreaBean { get; set; } = null;
        public MS2DecResult ms2DecResult { get; set; } = null;
        public DriftSpotBean driftSpotBean { get; set; } = null;
    }
    public class MsdialAlignmetMetaDataField {
        public int AlignmentID { get; set; }
        public double Rt { get; set; }
        public double Dt { get; set; }
        public double Ccs { get; set; }
        public double Mz { get; set; }
        public string MetaboliteName { get; set; }
        public string MetAdduct { get; set; }
        public string Adduct { get; set; }
        public string Formula { get; set; }
        public string Ontology { get; set; }
        public string InChIKey { get; set; }
        public string SMILES { get; set; }
        public string Annotation { get; set; }
        public string Comment { get; set; }
        public string SpectrumRefFileName { get; set; }
        public string Spectrum { get; set; }
        public double SN { get; set; }
        public LipidMolecule Molecule { get; set; }
        public double HeightAverage { get; set; }
        public double HeightStdev { get; set; }
        public double NormalizedValueAverage { get; set; }
        public double NormalizedValueStdev { get; set; }
        public string InternalStandardString { get; set; }
    }

    public class LipidStatistics {
        public int LipidID { get; set; }
        public string LipidClass { get; set; }
        public string LipidCategory { get; set; }
        public Dictionary<string, List<string>> CategoryToLipidNames { get; set; } = new Dictionary<string, List<string>>();
        public int UniqueLipidCount { get; set; }
        public List<string> UniqueLipids { get; set; } = new List<string>();
    }

    public class TestResult {
        public float RefRt { get; set; }
        public float RefCCS { get; set; }
        public float TruePositive { get; set; }
        public float TrueNegative { get; set; }
        public float FalsePositive { get; set; }
        public float FalseNegative { get; set; }
        public float Accuracy { get; set; }
        public float Precision { get; set; }
        public float Recall { get; set; }
        public float Specificity { get; set; }
        public float FMeasure { get; set; }
        public float FDR { get; set; }

    }

    public class EadResultObj {
        public string FileName { get; set; } = string.Empty;
        public string Lipid { get; set; } = string.Empty;
        public string File_Lipid => FileName + "_" + Lipid;
        public string Result { get; set; } = string.Empty;
        public string AnnotationLabel { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string TotalScore { get; set; } = string.Empty;

        public string ClassID => FileName.Split('_')[1] == "Lsplash" ? FileName.Split('_')[2] : FileName.Split('_')[1];
        public string ReplicateID => int.TryParse(FileName.Split('_')[FileName.Split('_').Length - 1], out int id) ? id.ToString() : "1";
    }

    public sealed class LipidomicsResultCuration {
        private LipidomicsResultCuration() { }

        // msdial5 paper
        public static void EadValidationResultExport(string annofile, string pairfile, string peaknamefile, string exportfile) {

            // parse pairfile
            var name2type = new Dictionary<string, string>();
            using (var sr = new StreamReader(pairfile)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');
                    if (linearray.Length == 2) {
                        var name = linearray[0];
                        var type = linearray[1];
                        name2type[name] = type;
                    }
                }
            };
            // parse annofile
            var resultlist = new List<EadResultObj>();
            var files = new List<string>();
            var lipids = new List<string>();
            using (var sr = new StreamReader(annofile)) {
                var line = sr.ReadLine();
                var linearray = line.Split('\t');
                for (int i = 2; i < linearray.Length; i++) files.Add(linearray[i]);

                while (sr.Peek() > -1) {
                    line = sr.ReadLine();
                    linearray = line.Split('\t');

                    var lipid = linearray[1];
                    lipids.Add(lipid);
                    for (int i = 2; i < linearray.Length; i++) {
                        resultlist.Add(new EadResultObj() {
                            FileName = files[i - 2],
                            Lipid = lipid, AnnotationLabel = linearray[i], Type = linearray[i] == "no MS/MS" ? "noMS2" : string.Empty,
                            Result = linearray[i] == "no MS/MS" ? "null" : string.Empty
                        });
                    }
                }
            };

            using (var sr = new StreamReader(peaknamefile)) {
                var line = sr.ReadLine();
                var linearray = line.Split('\t');
                var headerfiles = new List<string>();
                for (int i = 3; i < linearray.Length; i++) headerfiles.Add(linearray[i]);

                while (sr.Peek() > -1) {
                    line = sr.ReadLine();
                    linearray = line.Split('\t');
                    var lipid = linearray[1];
                    for (int i = 3; i < linearray.Length; i++) {
                        var tFile = headerfiles[i - 3];
                        var tLipid = linearray[1];
                        foreach (var result in resultlist) {
                            var rFile = result.FileName;
                            var rLipid = result.Lipid;
                            if (tFile == rFile && tLipid == rLipid) {
                                if (result.Type != "noMS2") {
                                    result.Result = linearray[i];
                                    if (name2type.ContainsKey(result.Result)) {
                                        result.Type = name2type[result.Result];
                                    }
                                    else {
                                        result.Type = "misslabel";
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            };

            using (var sw = new StreamWriter(exportfile)) {
                var header = new List<string>() { "file_lipid", "file", "lipid", "result", "type" };
                sw.WriteLine(String.Join("\t", header));
                foreach (var result in resultlist) {
                    var sResult = new List<string>() { result.File_Lipid, result.FileName, result.Lipid, result.Result, result.Type };
                    sw.WriteLine(String.Join("\t", sResult));
                }
            }
        }

        public static void EadValidationResultExport(string xmlfile, string pairfile) {
            // parse pairfile
            var name2type = new Dictionary<string, string>();
            using (var sr = new StreamReader(pairfile)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');
                    if (linearray.Length == 2) {
                        var name = linearray[0];
                        var type = linearray[1];
                        name2type[name] = type;
                    }
                }
            };

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlfile);

            var alignedSpots = new List<EadResultObj>();
            // <MatchedSpot>要素をすべて取得
            XmlNodeList matchedSpotNodes = xmlDoc.DocumentElement.SelectNodes("//MatchedSpot");

            foreach (XmlNode matchedSpot in matchedSpotNodes) {
                // <Reference>の<Name>を取得
                string referenceName = matchedSpot.SelectSingleNode("Reference/Name")?.InnerText ?? "";

                // <AlignedSpot>要素をすべて取得
                XmlNodeList alignedSpotNodes = matchedSpot.SelectNodes("AlignedSpot");

                foreach (XmlNode alignedSpot in alignedSpotNodes) {
                    // AlignedSpotの<Name>を取得
                    string alignedName = alignedSpot.SelectSingleNode("Name")?.InnerText ?? "";

                    // <Peak>要素をすべて取得
                    XmlNodeList peakNodes = alignedSpot.SelectNodes("Peak");

                    foreach (XmlNode peak in peakNodes) {
                        // 各フィールドの値を取得
                        string file = peak.SelectSingleNode("File")?.InnerText ?? "";
                        string peakName = peak.SelectSingleNode("Name")?.InnerText ?? "null";
                        string totalScore = peak.SelectSingleNode("TotalScore")?.InnerText ?? "null";
                        string isMsmsAssigned = peak.SelectSingleNode("IsMsmsAssigned")?.InnerText ?? "null";
                        string labeltype = "noMS2";

                        if (peakName.Contains("|")) peakName = peakName.Split('|')[1];

                        if (isMsmsAssigned != "false") {
                            if (name2type.ContainsKey(peakName)) {
                                labeltype = name2type[peakName];
                            }
                            else {
                                labeltype = "misslabel";
                            }
                        }
                        alignedSpots.Add(new EadResultObj() {
                            FileName = file, Lipid = referenceName, Result = peakName, TotalScore = totalScore, Type = labeltype
                        });
                    }
                }
            }

            var directory = Path.GetDirectoryName(xmlfile);
            var xmlname = Path.GetFileNameWithoutExtension(xmlfile);
            var exportfile = Path.Combine(directory, xmlname + ".resultall");
            var allcount = 0;
            var misscount = 0;
            using (var sw = new StreamWriter(exportfile)) {
                var header = new List<string>() { "file_lipid", "file", "class", "replicate_id", "lipid", "result", "score", "type" };
                sw.WriteLine(String.Join("\t", header));
                foreach (var spot in alignedSpots) {
                    var sResult = new List<string>() { spot.File_Lipid, spot.FileName, spot.ClassID, spot.ReplicateID, spot.Lipid, spot.Result, spot.TotalScore, spot.Type };
                    sw.WriteLine(String.Join("\t", sResult));

                    if (spot.Type == "noMS2") continue;
                    if (spot.Type == "misslabel") misscount++;
                    allcount++;
                }
            }

            Console.WriteLine("query {0}, miss {1}, percent {2}", allcount, misscount, ((double)misscount / (double)allcount) * 100);

            var exportfile_summary = Path.Combine(directory, xmlname + ".resultsummary");

            var dict = new Dictionary<string, List<EadResultObj>>();
            foreach (var spot in alignedSpots) {
                var classid = spot.ClassID;
                var lipid = spot.Lipid;
                var key = classid + "|" + lipid;
                if (dict.ContainsKey(key)) {
                    dict[key].Add(spot);
                }
                else {
                    dict[key] = new List<EadResultObj>() { spot };
                }
            }
            using (var sw = new StreamWriter(exportfile_summary)) {
                var header = new List<string>() { "class", "lipid", "result", "score", "type" };
                sw.WriteLine(String.Join("\t", header));

                foreach (var item in dict) {
                    var key = item.Key;
                    var value = item.Value;

                    var classid = key.Split('|')[0];
                    var lipid = key.Split('|')[1];
                    
                    if (value.Count < 3) {
                        var sResult = new List<string>() { classid, lipid, value[0].Result, value[0].TotalScore, value[0].Type };
                        sw.WriteLine(String.Join("\t", sResult));
                    }
                    else {
                        var sorted = value.OrderByDescending(n => n.TotalScore).ToList();
                        var distinctlabels = sorted.Select(n => n.Result).Distinct().ToList();
                        if (distinctlabels.Count() == 3 || distinctlabels.Count() == 1) {
                            var sResult = new List<string>() { classid, lipid, sorted[0].Result, sorted[0].TotalScore, sorted[0].Type };
                            sw.WriteLine(String.Join("\t", sResult));
                        }
                        else {
                            if (sorted[0].Result == sorted[1].Result) {
                                var sResult = new List<string>() { classid, lipid, sorted[0].Result, sorted[0].TotalScore, sorted[0].Type };
                                sw.WriteLine(String.Join("\t", sResult));
                            }
                            else if (sorted[0].Result == sorted[2].Result) {
                                var sResult = new List<string>() { classid, lipid, sorted[0].Result, sorted[0].TotalScore, sorted[0].Type };
                                sw.WriteLine(String.Join("\t", sResult));
                            }
                            else if (sorted[1].Result == sorted[2].Result) {
                                var sResult = new List<string>() { classid, lipid, sorted[1].Result, sorted[1].TotalScore, sorted[1].Type };
                                sw.WriteLine(String.Join("\t", sResult));
                            }
                        }
                    }
                }
            }
        }

        public static void Text2Msp(string input, string output) {

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                using (var sr = new StreamReader(input, Encoding.ASCII)) {
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        var lineArray = line.Split('\t');
                        var specString = lineArray[17];
                        var peaks = textToSpectrumList(specString, ':', ' ');

                        sw.WriteLine("NAME: " + lineArray[3]);
                        sw.WriteLine("PRECURSORMZ: " + lineArray[2]);
                        sw.WriteLine("PRECURSORTYPE: " + lineArray[4]);
                        sw.WriteLine("COMMENT: " + lineArray[0]);
                        sw.WriteLine("Num Peaks: " + peaks.Count);
                        foreach (var peak in peaks) {
                            sw.WriteLine(peak[0] + "\t" + peak[1]);
                        }
                        sw.WriteLine();
                    }
                }
            }
        }

        public static void Name2Smiles(string input, string output) {

            var lipidnames = new List<string>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.Contains("|")) {
                        line = line.Split('|')[1];
                    }
                    lipidnames.Add(line.Trim());
                }
            }

            var mspDB = LipidomicsConverter.SerializedObjectToMspQeries(@"E:\0_SourceCode\msdialworkbench\MsDial\bin\Debug\Msp20210426080355_converted.lbm2");
            var molecules = new List<LipidMolecule>();
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var lipid in lipidnames) {
                    var flag = false;
                    var lipidclass = lipid.Split(' ')[0];
                    foreach (var query in mspDB.Where(n => n.IonMode == IonMode.Negative && n.CompoundClass == lipidclass)) {
                        var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(query);
                        if (lipid == molecule.LipidName) {
                            flag = true;
                            sw.WriteLine(query.InchiKey + "\t" + query.Smiles);
                            break;
                        }
                        if (flag) break;
                    }
                    if (flag == false) {
                        sw.WriteLine("null" + "\t" + "null");
                    }
                }
            }
        }

        public static void ExtractSuspectOrigins(string lipidfile, string inputfolder, string output) {
            var checkedlipids = new List<string>();
            using (var sr = new StreamReader(lipidfile, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    checkedlipids.Add(line);
                }
            }

            var mismatchlist = new List<string[]>();
            var files = System.IO.Directory.GetFiles(inputfolder);
            foreach (var lipid in checkedlipids) {
                foreach (var file in files) {
                    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                    var isIonMobilityFormat = false;
                    if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;

                    using (var sr = new StreamReader(file, Encoding.ASCII)) {
                        sr.ReadLine();
                        sr.ReadLine();
                        sr.ReadLine();
                        sr.ReadLine();
                        sr.ReadLine();
                        while (sr.Peek() > -1) {
                            var line = sr.ReadLine();
                            if (line == string.Empty) break;
                            var lineArray = line.Split('\t');
                            var id = lineArray[0];
                            var mz = double.Parse(lineArray[2]);
                            var metname = isIonMobilityFormat ? lineArray[5] : lineArray[3];
                            var adduct = isIonMobilityFormat ? lineArray[6] : lineArray[4];
                            var formula = isIonMobilityFormat ? lineArray[13] : lineArray[10];
                            var comment = isIonMobilityFormat ? lineArray[22] : lineArray[18];
                            var rtMatched = isIonMobilityFormat ? lineArray[18] : lineArray[15];
                            var ccsMatched = isIonMobilityFormat ? lineArray[19] : "FALSE";
                            var mzMatched = isIonMobilityFormat ? lineArray[20] : lineArray[16];
                            var ms2Matched = isIonMobilityFormat ? lineArray[21] : lineArray[17];
                            if (comment.Contains("IS")) continue;
                            if (metname.Contains("RIKEN")) continue;
                            if (metname.Contains("Unknown") || metname.Contains("w/o")) continue;

                            if (metname == lipid) {
                                mismatchlist.Add(new string[] { lipid, filename });
                            }
                        }
                    }
                }
            }
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var miskey in mismatchlist) {
                    sw.WriteLine(String.Join("\t", miskey));
                }
            }
        }

        public static void AdductCurator(string inputfolder, string output) {
            var mismatchlist = new List<string[]>();
            var files = System.IO.Directory.GetFiles(inputfolder);
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var isIonMobilityFormat = false;
                if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;

                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        var id = lineArray[0];
                        var mz = double.Parse(lineArray[2]);
                        var metname = isIonMobilityFormat ? lineArray[5] : lineArray[3];
                        var adduct = isIonMobilityFormat ? lineArray[6] : lineArray[4];
                        var formula = isIonMobilityFormat ? lineArray[13] : lineArray[10];
                        var comment = isIonMobilityFormat ? lineArray[22] : lineArray[18];
                        var rtMatched = isIonMobilityFormat ? lineArray[18] : lineArray[15];
                        var ccsMatched = isIonMobilityFormat ? lineArray[19] : "FALSE";
                        var mzMatched = isIonMobilityFormat ? lineArray[20] : lineArray[16];
                        var ms2Matched = isIonMobilityFormat ? lineArray[21] : lineArray[17];
                        if (comment.Contains("IS")) continue;
                        if (metname.Contains("RIKEN")) continue;
                        if (metname.Contains("Unknown") || metname.Contains("w/o")) continue;

                        var adductObj = AdductIonParcer.GetAdductIonBean(adduct);
                        var formulaObj = FormulaStringParcer.OrganicElementsReader(formula);
                        var exactMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(adductObj, formulaObj.Mass);
                        if (Math.Abs(mz - exactMz) > 0.1) {
                            mismatchlist.Add(new string[] { filename, id, metname, adduct, mz.ToString(), exactMz.ToString(), (mz - exactMz).ToString() });
                        }
                    }
                }
            }
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var miskey in mismatchlist) {
                    sw.WriteLine(String.Join("\t", miskey));
                }
            }
        }

        public static void OntologyCurator(string inputfolder, string output) {
            var matchlist = new List<string[]>();
            var files = System.IO.Directory.GetFiles(inputfolder);
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var isIonMobilityFormat = false;
                if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;

                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        var id = lineArray[0];
                        var mz = double.Parse(lineArray[2]);
                        var metname = isIonMobilityFormat ? lineArray[5] : lineArray[3];
                        var adduct = isIonMobilityFormat ? lineArray[6] : lineArray[4];
                        var formula = isIonMobilityFormat ? lineArray[13] : lineArray[10];
                        var comment = isIonMobilityFormat ? lineArray[22] : lineArray[18];
                        var ontology = isIonMobilityFormat ? lineArray[14] : lineArray[11];
                        if (comment.Contains("IS")) continue;
                        if (metname.Contains("RIKEN")) continue;

                        var adductObj = AdductIonParcer.GetAdductIonBean(adduct);
                        var formulaObj = FormulaStringParcer.OrganicElementsReader(formula);
                        var exactMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(adductObj, formulaObj.Mass);
                        if (ontology == "BileAcid" && adduct == "[M+Hac-H]-") {
                            matchlist.Add(new string[] { filename, id, metname, ontology, adduct });
                        }
                        else if (ontology == "CholesterolSulfate" && adduct == "[M-H]-") {
                            matchlist.Add(new string[] { filename, id, metname, ontology, adduct });
                        }
                        else if (ontology == "PI_Cer") {
                            matchlist.Add(new string[] { filename, id, metname, ontology, adduct });
                        }
                        else if (ontology == "Vitamin") {
                            matchlist.Add(new string[] { filename, id, metname, ontology, adduct });
                        }
                        else if (ontology == "Others") {
                            matchlist.Add(new string[] { filename, id, metname, ontology, adduct });
                        }
                    }
                }
            }
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var miskey in matchlist) {
                    sw.WriteLine(String.Join("\t", miskey));
                }
            }
        }

        
        public static void AdductCorrection(string inputfolder, string correctfile, string outputfolder) {
            var files = System.IO.Directory.GetFiles(inputfolder);
            var correctlist = new List<string[]>();
            using (var sr = new StreamReader(correctfile, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');
                    correctlist.Add(lineArray);
                }
            }

            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var output = outputfolder + "\\" + filename + ".txt";
                var isIonMobilityFormat = false;
                if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;
                using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                    using (var sr = new StreamReader(file, Encoding.ASCII)) {
                        sw.WriteLine(sr.ReadLine());
                        sw.WriteLine(sr.ReadLine());
                        sw.WriteLine(sr.ReadLine());
                        sw.WriteLine(sr.ReadLine());
                        sw.WriteLine(sr.ReadLine());
                        while (sr.Peek() > -1) {
                            var line = sr.ReadLine();
                            if (line == string.Empty) break;
                            var lineArray = line.Split('\t');
                            var id = lineArray[0];
                            var mz = double.Parse(lineArray[2]);
                            var metname = isIonMobilityFormat ? lineArray[5] : lineArray[3];
                            var adduct = isIonMobilityFormat ? lineArray[6] : lineArray[4];
                            var formula = isIonMobilityFormat ? lineArray[13] : lineArray[10];
                            var comment = isIonMobilityFormat ? lineArray[22] : lineArray[18];
                            var ontology = isIonMobilityFormat ? lineArray[14] : lineArray[11];
                            var correctAdduct = string.Empty;
                            foreach (var key in correctlist) {
                                if (key[0] == filename && key[1] == id) {
                                    correctAdduct = key[4];
                                    break;
                                }
                            }
                            if (correctAdduct != string.Empty) {
                                if (isIonMobilityFormat) {
                                    lineArray[6] = correctAdduct;
                                }
                                else {
                                    lineArray[4] = correctAdduct;
                                }
                            }
                            if (adduct == "[M+Hac-H]-") {
                                if (isIonMobilityFormat) {
                                    lineArray[6] = "[M+CH3COO]-";
                                }
                                else {
                                    lineArray[4] = "[M+CH3COO]-";
                                }
                            }
                            if (ontology == "CholesterolSulfate") {
                                if (isIonMobilityFormat) {
                                    lineArray[5] = "CSSulfate";
                                    lineArray[14] = "SSulfate";
                                }
                                else {
                                    lineArray[3] = "CSSulfate";
                                    lineArray[11] = "SSulfate";
                                }
                            }
                            if (ontology == "Others" && metname == "Deoxycholic acid") {
                                if (isIonMobilityFormat) {
                                    lineArray[5] = "DCA";
                                    lineArray[14] = "BileAcid";
                                }
                                else {
                                    lineArray[3] = "DCA";
                                    lineArray[11] = "BileAcid";
                                }
                            }

                            if (ontology == "Others" && metname == "Carnitine") {
                                if (isIonMobilityFormat) {
                                    lineArray[5] = "ACar 0:0";
                                    lineArray[14] = "ACar";
                                }
                                else {
                                    lineArray[3] = "ACar 0:0";
                                    lineArray[11] = "ACar";
                                }
                            }

                            if (ontology == "PI_Cer") {
                                if (isIonMobilityFormat) {
                                    lineArray[14] = "PI_Cer+O";
                                }
                                else {
                                    lineArray[11] = "PI_Cer+O";
                                }
                            }
                            sw.WriteLine(String.Join("\t", lineArray));
                        }
                    }
                }
            }
        }

        public static void ExtractOntologyAdductPair(string inputfolder, string output) {
            var files = System.IO.Directory.GetFiles(inputfolder);
            var ontologyAdducts = new List<string>();
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var isIonMobilityFormat = false;
                if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;

                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        var id = lineArray[0];
                        var mz = double.Parse(lineArray[2]);
                        var metname = isIonMobilityFormat ? lineArray[5] : lineArray[3];
                        var adduct = isIonMobilityFormat ? lineArray[6] : lineArray[4];
                        var formula = isIonMobilityFormat ? lineArray[13] : lineArray[10];
                        var comment = isIonMobilityFormat ? lineArray[22] : lineArray[18];
                        var ontology = isIonMobilityFormat ? lineArray[14] : lineArray[11];
                        if (comment.Contains("IS")) continue;
                        if (metname.Contains("RIKEN")) continue;
                        var ontology_adduct = ontology + "%" + adduct;
                        if (!ontologyAdducts.Contains(ontology_adduct)) {
                            ontologyAdducts.Add(ontology_adduct);
                        }
                    }
                }
            }

            ontologyAdducts.Sort();
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Ontology\tAdduct");
                foreach (var key in ontologyAdducts) {
                    sw.WriteLine(key.Split('%')[0] + "\t" + key.Split('%')[1]);
                }
            }
        }

        public static void CheckOntologyAdductPair(string inputfolder, string output) {
            var files = System.IO.Directory.GetFiles(inputfolder);
            var checkfiles = new List<string>();
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var isIonMobilityFormat = false;
                if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;

                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        var id = lineArray[0];
                        var mz = double.Parse(lineArray[2]);
                        var metname = isIonMobilityFormat ? lineArray[5] : lineArray[3];
                        var adduct = isIonMobilityFormat ? lineArray[6] : lineArray[4];
                        var formula = isIonMobilityFormat ? lineArray[13] : lineArray[10];
                        var comment = isIonMobilityFormat ? lineArray[22] : lineArray[18];
                        var ontology = isIonMobilityFormat ? lineArray[14] : lineArray[11];

                        if (ontology == "SM" || ontology == "SM+O") {
                            if (adduct == "[M-H]-") {
                                checkfiles.Add(filename);
                            }
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("File");
                foreach (var key in checkfiles) {
                    sw.WriteLine(key);
                }
            }
        }

        public static void ExportQuantFiles(string inputfolder, string outputfolder) {
            var posLipidlist = new List<string[]>() {
                new string[] { "ACar", "[M]+" },
                new string[] { "ACar", "[M+H]+" },
                new string[] { "ADGGA", "[M+NH4]+" },
                new string[] { "AHexCAS", "[M+NH4]+" },
                new string[] { "AHexCS", "[M+NH4]+" },
                new string[] { "AHexSIS", "[M+NH4]+" },
                new string[] { "AHexSTS", "[M+NH4]+" },
                new string[] { "BMP", "[M+NH4]+" },
                new string[] { "BRSE", "[M+NH4]+" },
                new string[] { "CASE", "[M+NH4]+" },
                new string[] { "CE", "[M+NH4]+" },
                new string[] { "CerP", "[M+H]+" },
                new string[] { "Cholesterol", "[M-H2O+H]+" },
                new string[] { "CL", "[M+NH4]+" },
                new string[] { "CoQ", "[M+H]+" },
                new string[] { "DAG", "[M+NH4]+" },
                new string[] { "DCAE", "[M+NH4]+" },
                new string[] { "DGCC", "[M+H]+" },
                new string[] { "DGTS", "[M+H]+" },
                new string[] { "DGTA", "[M+H]+" },
                new string[] { "EtherDAG", "[M+NH4]+" },
                new string[] { "EtherLPC", "[M+H]+" },
                new string[] { "EtherLPE", "[M+H]+" },
                new string[] { "EtherPE(Plasmalogen)", "[M+H]+" },
                new string[] { "EtherTAG", "[M+NH4]+" },
                new string[] { "GDCAE", "[M+NH4]+" },
                new string[] { "GLCAE", "[M+NH4]+" },
                new string[] { "GM3", "[M+NH4]+" },
                new string[] { "HBMP", "[M+NH4]+" },
                new string[] { "Hex2Cer", "[M+H]+" },
                new string[] { "Hex3Cer", "[M+H]+" },
                new string[] { "LDGCC", "[M+H]+" },
                new string[] { "LDGTS", "[M+H]+" },
                new string[] { "LDGTA", "[M+H]+" },
                new string[] { "LPC", "[M+H]+" },
                new string[] { "LPE", "[M+H]+" },
                new string[] { "MAG", "[M+NH4]+" },
                new string[] { "NAAG", "[M+H]+" },
                new string[] { "NAAGS", "[M+NH4]+" },
                new string[] { "NAAO", "[M+H]+" },
                new string[] { "NAE", "[M+H]+" },
                new string[] { "Phytosphingosine", "[M+H]+" },
                new string[] { "SHex", "[M+NH4]+" },
                new string[] { "SHexCer", "[M+H]+" },
                new string[] { "SHexCer+O", "[M+H]+" },
                new string[] { "SISE", "[M+NH4]+" },
                new string[] { "SL", "[M+H]+" },
                new string[] { "SL+O", "[M+H]+" },
                new string[] { "Sphinganine", "[M+H]+" },
                new string[] { "Sphingosine", "[M+H]+" },
                new string[] { "SQDG", "[M+NH4]+" },
                new string[] { "STSE", "[M+NH4]+" },
                new string[] { "TAG", "[M+NH4]+" },
                new string[] { "TDCAE", "[M+NH4]+" },
                new string[] { "TLCAE", "[M+NH4]+" },
                new string[] { "VAE", "[M+Na]+" },
                new string[] { "Vitamin", "[M+H]+" },
                new string[] { "AHexCer", "[M+CH3COO]-" },
                new string[] { "AHexCer", "[M+HCOO]-" },
                new string[] { "ASM", "[M+CH3COO]-" },
                new string[] { "ASM", "[M+HCOO]-" },
                new string[] { "BASulfate", "[M-H]-" },
                new string[] { "BileAcid", "[M-H]-" },
                new string[] { "Cer_ADS", "[M+CH3COO]-" },
                new string[] { "Cer_AP", "[M+CH3COO]-" },
                new string[] { "Cer_AS", "[M+CH3COO]-" },
                new string[] { "Cer_BDS", "[M+CH3COO]-" },
                new string[] { "Cer_BS", "[M+CH3COO]-" },
                new string[] { "Cer_EBDS", "[M+CH3COO]-" },
                new string[] { "Cer_EOS", "[M+CH3COO]-" },
                new string[] { "Cer_EODS", "[M+CH3COO]-" },
                new string[] { "Cer_HDS", "[M+CH3COO]-" },
                new string[] { "Cer_HS", "[M+CH3COO]-" },
                new string[] { "Cer_NDS", "[M+CH3COO]-" },
                new string[] { "Cer_NP", "[M+CH3COO]-" },
                new string[] { "Cer_NS", "[M+CH3COO]-" },
                new string[] { "Cer_ADS", "[M+HCOO]-" },
                new string[] { "Cer_AP", "[M+HCOO]-" },
                new string[] { "Cer_AS", "[M+HCOO]-" },
                new string[] { "Cer_BDS", "[M+HCOO]-" },
                new string[] { "Cer_BS", "[M+HCOO]-" },
                new string[] { "Cer_EBDS", "[M+HCOO]-" },
                new string[] { "Cer_EOS", "[M+HCOO]-" },
                new string[] { "Cer_EODS", "[M+HCOO]-" },
                new string[] { "Cer_HDS", "[M+HCOO]-" },
                new string[] { "Cer_HS", "[M+HCOO]-" },
                new string[] { "Cer_NDS", "[M+HCOO]-" },
                new string[] { "Cer_NP", "[M+HCOO]-" },
                new string[] { "Cer_NS", "[M+HCOO]-" },
                new string[] { "CL", "[M-H]-" },
                new string[] { "DGDG", "[M+CH3COO]-" },
                new string[] { "DGDG", "[M+HCOO]-" },
                new string[] { "DGGA", "[M-H]-" },
                new string[] { "DLCL", "[M-H]-" },
                new string[] { "EtherLPG", "[M-H]-" },
                new string[] { "EtherMGDG", "[M+CH3COO]-" },
                new string[] { "EtherDGDG", "[M+CH3COO]-" },
                new string[] { "EtherMGDG", "[M+HCOO]-" },
                new string[] { "EtherDGDG", "[M+HCOO]-" },
                new string[] { "EtherOxPE", "[M-H]-" },
                new string[] { "EtherPC", "[M+CH3COO]-" },
                new string[] { "EtherPC", "[M+HCOO]-" },
                new string[] { "EtherPE", "[M-H]-" },
                new string[] { "EtherPG", "[M-H]-" },
                new string[] { "EtherPI", "[M-H]-" },
                new string[] { "EtherPS", "[M-H]-" },
                new string[] { "FA", "[M-H]-" },
                new string[] { "FAHFA", "[M-H]-" },
                new string[] { "GM3", "[M-H]-" },
                new string[] { "HBMP", "[M-H]-" },
                new string[] { "HexCer_AP", "[M+CH3COO]-" },
                new string[] { "HexCer_EOS", "[M+CH3COO]-" },
                new string[] { "HexCer_HDS", "[M+CH3COO]-" },
                new string[] { "HexCer_HS", "[M+CH3COO]-" },
                new string[] { "HexCer_NDS", "[M+CH3COO]-" },
                new string[] { "HexCer_NS", "[M+CH3COO]-" },
                new string[] { "HexCer_AP", "[M+HCOO]-" },
                new string[] { "HexCer_EOS", "[M+HCOO]-" },
                new string[] { "HexCer_HDS", "[M+HCOO]-" },
                new string[] { "HexCer_HS", "[M+HCOO]-" },
                new string[] { "HexCer_NDS", "[M+HCOO]-" },
                new string[] { "HexCer_NS", "[M+HCOO]-" },
                new string[] { "LCL", "[M-H]-" },
                new string[] { "LNAPE", "[M-H]-" },
                new string[] { "LNAPS", "[M-H]-" },
                new string[] { "LPA", "[M-H]-" },
                new string[] { "LPG", "[M-H]-" },
                new string[] { "LPI", "[M-H]-" },
                new string[] { "LPS", "[M-H]-" },
                new string[] { "MGDG", "[M+CH3COO]-" },
                new string[] { "OxPC", "[M+CH3COO]-" },
                new string[] { "MGDG", "[M+HCOO]-" },
                new string[] { "OxPC", "[M+HCOO]-" },
                new string[] { "OxPE", "[M-H]-" },
                new string[] { "OxPG", "[M-H]-" },
                new string[] { "OxPI", "[M-H]-" },
                new string[] { "OxPS", "[M-H]-" },
                new string[] { "PA", "[M-H]-" },
                new string[] { "PC", "[M+CH3COO]-" },
                new string[] { "PC", "[M+HCOO]-" },
                new string[] { "PE", "[M-H]-" },
                new string[] { "PE_Cer", "[M-H]-" },
                new string[] { "PE_Cer+O", "[M-H]-" },
                new string[] { "PEtOH", "[M-H]-" },
                new string[] { "PG", "[M-H]-" },
                new string[] { "PI", "[M-H]-" },
                new string[] { "PI_Cer+O", "[M-H]-" },
                new string[] { "PMeOH", "[M-H]-" },
                new string[] { "PS", "[M-H]-" },
                new string[] { "SHexCer", "[M-H]-" },
                new string[] { "SHexCer+O", "[M-H]-" },
                new string[] { "SM", "[M+CH3COO]-" },
                new string[] { "SM+O", "[M+CH3COO]-" },
                new string[] { "SM", "[M+HCOO]-" },
                new string[] { "SM+O", "[M+HCOO]-" },
                new string[] { "SQDG", "[M-H]-" },
                new string[] { "SSulfate", "[M-H]-" },
                new string[] { "AHexBRS", "[M+CH3COO]-" },
                new string[] { "Vitamin", "[M-H]-" },
                new string[] { "Vitamin", "[M+CH3COO]-" },
                new string[] { "Vitamin", "[M+HCOO]-" },
                // for aging
                new string[] { "CSLPHex", "[M-H]-" },
                new string[] { "EtherOxPC", "[M+CH3COO]-" },
                new string[] { "SISPHex", "[M-H]-" },
                new string[] { "SPE", "[M+H]+" },
                new string[] { "SPE", "[M-H]-" },
            };
            //var negLipidlist = new List<string[]>() {
            //    new string[] { "AHexCer", "[M+CH3COO]-" },
            //    new string[] { "AHexCer", "[M+HCOO]-" },
            //    new string[] { "ASM", "[M+CH3COO]-" },
            //    new string[] { "ASM", "[M+HCOO]-" },
            //    new string[] { "BASulfate", "[M-H]-" },
            //    new string[] { "BileAcid", "[M-H]-" },
            //    new string[] { "Cer_ADS", "[M+CH3COO]-" },
            //    new string[] { "Cer_AP", "[M+CH3COO]-" },
            //    new string[] { "Cer_AS", "[M+CH3COO]-" },
            //    new string[] { "Cer_BDS", "[M+CH3COO]-" },
            //    new string[] { "Cer_BS", "[M+CH3COO]-" },
            //    new string[] { "Cer_EBDS", "[M+CH3COO]-" },
            //    new string[] { "Cer_EOS", "[M+CH3COO]-" },
            //    new string[] { "Cer_EODS", "[M+CH3COO]-" },
            //    new string[] { "Cer_HDS", "[M+CH3COO]-" },
            //    new string[] { "Cer_HS", "[M+CH3COO]-" },
            //    new string[] { "Cer_NDS", "[M+CH3COO]-" },
            //    new string[] { "Cer_NP", "[M+CH3COO]-" },
            //    new string[] { "Cer_NS", "[M+CH3COO]-" },
            //    new string[] { "Cer_ADS", "[M+HCOO]-" },
            //    new string[] { "Cer_AP", "[M+HCOO]-" },
            //    new string[] { "Cer_AS", "[M+HCOO]-" },
            //    new string[] { "Cer_BDS", "[M+HCOO]-" },
            //    new string[] { "Cer_BS", "[M+HCOO]-" },
            //    new string[] { "Cer_EBDS", "[M+HCOO]-" },
            //    new string[] { "Cer_EOS", "[M+HCOO]-" },
            //    new string[] { "Cer_EODS", "[M+HCOO]-" },
            //    new string[] { "Cer_HDS", "[M+HCOO]-" },
            //    new string[] { "Cer_HS", "[M+HCOO]-" },
            //    new string[] { "Cer_NDS", "[M+HCOO]-" },
            //    new string[] { "Cer_NP", "[M+HCOO]-" },
            //    new string[] { "Cer_NS", "[M+HCOO]-" },
            //    new string[] { "CL", "[M-H]-" },
            //    new string[] { "DGDG", "[M+CH3COO]-" },
            //    new string[] { "DGDG", "[M+HCOO]-" },
            //    new string[] { "DGGA", "[M-H]-" },
            //    new string[] { "DLCL", "[M-H]-" },
            //    new string[] { "EtherLPG", "[M-H]-" },
            //    new string[] { "EtherMGDG", "[M+CH3COO]-" },
            //    new string[] { "EtherDGDG", "[M+CH3COO]-" },
            //    new string[] { "EtherMGDG", "[M+HCOO]-" },
            //    new string[] { "EtherDGDG", "[M+HCOO]-" },
            //    new string[] { "EtherOxPE", "[M-H]-" },
            //    new string[] { "EtherPC", "[M+CH3COO]-" },
            //    new string[] { "EtherPC", "[M+HCOO]-" },
            //    new string[] { "EtherPE", "[M-H]-" },
            //    new string[] { "EtherPG", "[M-H]-" },
            //    new string[] { "EtherPI", "[M-H]-" },
            //    new string[] { "EtherPS", "[M-H]-" },
            //    new string[] { "FA", "[M-H]-" },
            //    new string[] { "FAHFA", "[M-H]-" },
            //    new string[] { "GM3", "[M-H]-" },
            //    new string[] { "HBMP", "[M-H]-" },
            //    new string[] { "HexCer_AP", "[M+CH3COO]-" },
            //    new string[] { "HexCer_EOS", "[M+CH3COO]-" },
            //    new string[] { "HexCer_HDS", "[M+CH3COO]-" },
            //    new string[] { "HexCer_HS", "[M+CH3COO]-" },
            //    new string[] { "HexCer_NDS", "[M+CH3COO]-" },
            //    new string[] { "HexCer_NS", "[M+CH3COO]-" },
            //    new string[] { "HexCer_AP", "[M+HCOO]-" },
            //    new string[] { "HexCer_EOS", "[M+HCOO]-" },
            //    new string[] { "HexCer_HDS", "[M+HCOO]-" },
            //    new string[] { "HexCer_HS", "[M+HCOO]-" },
            //    new string[] { "HexCer_NDS", "[M+HCOO]-" },
            //    new string[] { "HexCer_NS", "[M+HCOO]-" },
            //    new string[] { "LCL", "[M-H]-" },
            //    new string[] { "LNAPE", "[M-H]-" },
            //    new string[] { "LNAPS", "[M-H]-" },
            //    new string[] { "LPA", "[M-H]-" },
            //    new string[] { "LPG", "[M-H]-" },
            //    new string[] { "LPI", "[M-H]-" },
            //    new string[] { "LPS", "[M-H]-" },
            //    new string[] { "MGDG", "[M+CH3COO]-" },
            //    new string[] { "OxPC", "[M+CH3COO]-" },
            //    new string[] { "MGDG", "[M+HCOO]-" },
            //    new string[] { "OxPC", "[M+HCOO]-" },
            //    new string[] { "OxPE", "[M-H]-" },
            //    new string[] { "OxPG", "[M-H]-" },
            //    new string[] { "OxPI", "[M-H]-" },
            //    new string[] { "OxPS", "[M-H]-" },
            //    new string[] { "PA", "[M-H]-" },
            //    new string[] { "PC", "[M+CH3COO]-" },
            //    new string[] { "PC", "[M+HCOO]-" },
            //    new string[] { "PE", "[M-H]-" },
            //    new string[] { "PE_Cer", "[M-H]-" },
            //    new string[] { "PE_Cer+O", "[M-H]-" },
            //    new string[] { "PEtOH", "[M-H]-" },
            //    new string[] { "PG", "[M-H]-" },
            //    new string[] { "PI", "[M-H]-" },
            //    new string[] { "PI_Cer+O", "[M-H]-" },
            //    new string[] { "PMeOH", "[M-H]-" },
            //    new string[] { "PS", "[M-H]-" },
            //    new string[] { "SHexCer", "[M-H]-" },
            //    new string[] { "SHexCer+O", "[M-H]-" },
            //    new string[] { "SM", "[M-H]-" },
            //    new string[] { "SM+O", "[M-H]-" },
            //    new string[] { "SQDG", "[M-H]-" },
            //    new string[] { "SSulfate", "[M-H]-" },
            //    new string[] { "Vitamine", "[M-H]-" },
            //    new string[] { "Vitamine", "[M+CH3COO]-" },
            //    new string[] { "Vitamine", "[M+HCOO]-" },
            //};

            var files = System.IO.Directory.GetFiles(inputfolder);
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var output = outputfolder + "\\" + filename.Replace("-named", "-quant") + ".txt";
                var isIonMobilityFormat = false;
                if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;
                using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                    using (var sr = new StreamReader(file, Encoding.ASCII)) {
                        sw.WriteLine(sr.ReadLine());
                        sw.WriteLine(sr.ReadLine());
                        sw.WriteLine(sr.ReadLine());
                        sw.WriteLine(sr.ReadLine());
                        sw.WriteLine(sr.ReadLine());
                        while (sr.Peek() > -1) {
                            var line = sr.ReadLine();
                            if (line == string.Empty) break;
                            var lineArray = line.Split('\t');
                            var id = lineArray[0];
                            var mz = double.Parse(lineArray[2]);
                            var metname = isIonMobilityFormat ? lineArray[5] : lineArray[3];
                            var adduct = isIonMobilityFormat ? lineArray[6] : lineArray[4];
                            var formula = isIonMobilityFormat ? lineArray[13] : lineArray[10];
                            var comment = isIonMobilityFormat ? lineArray[22] : lineArray[18];
                            var ontology = isIonMobilityFormat ? lineArray[14] : lineArray[11];
                            //temp
                            //if (isIonMobilityFormat && lineArray[3] == "-1") continue;

                            if (metname.Contains("RIKEN")) continue;
                            if (comment.Contains("IS")) {
                                sw.WriteLine(String.Join("\t", lineArray));
                            }
                            else {
                                var isExist = false;
                                foreach (var pair in posLipidlist) {
                                    if (pair[0] == ontology && pair[1] == adduct) {
                                        isExist = true;
                                        break;
                                    }
                                }
                                if (isExist) {
                                    sw.WriteLine(String.Join("\t", lineArray));
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ExportDdaStatistics(string inputfolder, string output) {
            var files = System.IO.Directory.GetFiles(inputfolder);
            var metafields = new List<MsdialAlignmetMetaDataField>();
            var excludelist = new List<string>() {
                "DAG 17:0e/28:7",
                "FA 36:10",
                "FA 38:9",
                "FA 40:10",
                "FA 40:11",
                "FA 42:12",
                "FA 44:10",
                "FA 44:4",
                "FA 44:8",
                "LDGCC 40:6",
                "LDGCC 42:7",
                "PC 16:4e/18:1",
                "PC 16:4e/22:6",
                "PC 16:3-26:7",
                "PC 28:3e/18:2",
                "PC 28:5e/12:0",
                "PC 28:7e/18:1",
                "PC 8:0-32:8",
                "PC 8:0-32:9",
                "Cer-NS d15:3/38:9",
                "Cer-NS d16:1/36:10",
                "Cer-NS d18:1/36:10",
                "Cer-NS d18:1/38:10",
                "Cer-NS d20:2/36:10",
                "TAG 16:0-19:0-38:10"
            };
            var pe_cer_conversions = new List<string[]>() {
                new string[]{ "PE-Cer t20:0/14:1", "PE-Cer d16:1/18:0+O", @"CCCCCCCCCCCCCCCC(O)CC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCC", "KNGCQTKRBSUXLX-BYCLXTJYNA-N" },
                new string[]{ "PE-Cer d18:0/16:1", "PE-Cer d18:1/16:0", @"CCCCCCCCCCCCCCCC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCCCC", "JTVNQMOIYKXJKF-ORIPQNMZNA-N" },
                new string[]{ "PE-Cer d20:0/16:1", "PE-Cer d18:1/18:0", @"CCCCCCCCCCCCCCCCCC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCCCC", "ODXAYQMCAAQOSY-OWWNRXNENA-N" },
                new string[]{ "PE-Cer d26:1/16:1", "PE-Cer d18:1/24:1", @"CCCCCCCCCCCCC\C=C\C(O)C(COP(O)(=O)OCCN)NC(=O)CCCCCCCCCCC\C=C/CCCCCCCCCC", "SORHVKVBHXFRAM-AKTWBEBINA-N" }
            };

            var hexcer_eos_conversions = new List<string[]>() {
                 new string[] {"HexCer-EOS d17:1/32:0-O-18:2", @"CCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC","CGLSKLYKULJGNR-NCPWIQSONA-N" },
                 new string[] {"HexCer-EOS d17:1/34:1-O-18:2", @"CCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCC\C=C\CCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "YWAMKMPXTXJEIE-IRMUNXAANA-N" },
                 new string[] {"HexCer-EOS d17:1/36:1-O-18:2", @"CCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCC\C=C\CCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "JHBYQIZVVGBZAF-REJDZBDGNA-N" },
                 new string[] {"HexCer-EOS d18:1/32:0-O-18:2", @"CCCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "GRNOIEDXTPLWNI-FHDUZSEONA-N" },
                 new string[] {"HexCer-EOS d18:1/34:0-O-18:2", @"CCCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "PAIFHGDNASKFQQ-APUKNJKBNA-N" },
                 new string[] {"HexCer-EOS d18:1/34:1-O-18:2", @"CCCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCC\C=C\CCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "GZKQHTAYXBKUDZ-JVPOEFIKNA-N" },
                 new string[] {"HexCer-EOS d18:1/36:1-O-18:2", @"CCCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCC\C=C\CCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "DSUASOJSBHLJPB-QZUQHBRMNA-N" }
            };

            var etherpe_conversions = new List<string[]>() {
                 new string[]{ "PE 14:-1p/22:2", "PE 14:0e/22:2" },
            };


            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var isIonMobilityFormat = false;
                if (filename.Contains("12_") || filename.Contains("13_")) continue;
                if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;
                if (isIonMobilityFormat) continue;
                    using (var sr = new StreamReader(file, Encoding.ASCII)) {
                        sr.ReadLine();
                        sr.ReadLine();
                        sr.ReadLine();
                        sr.ReadLine();
                        sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        var id = int.Parse(lineArray[0]);
                        var mz = double.Parse(lineArray[2]);
                        var rt = double.Parse(lineArray[1]);
                        var dt = isIonMobilityFormat ? double.Parse(lineArray[3]) : -1.0;
                        var ccs = isIonMobilityFormat ? double.Parse(lineArray[4]) : -1.0;
                        var metname = isIonMobilityFormat ? lineArray[5] : lineArray[3];
                        var adduct = isIonMobilityFormat ? lineArray[6] : lineArray[4];
                        var formula = isIonMobilityFormat ? lineArray[13] : lineArray[10];
                        var comment = isIonMobilityFormat ? lineArray[22] : lineArray[18];
                        var ontology = isIonMobilityFormat ? lineArray[14] : lineArray[11];
                        var smiles = isIonMobilityFormat ? lineArray[16] : lineArray[13];
                        var inchikey = isIonMobilityFormat ? lineArray[15] : lineArray[12];
                        var annotationtag = isIonMobilityFormat ? lineArray[17] : lineArray[14];
                        var sn = isIonMobilityFormat ? double.Parse(lineArray[32]) : double.Parse(lineArray[27]);
                        var specfile = isIonMobilityFormat ? lineArray[33] : lineArray[28];
                        var specstring = isIonMobilityFormat ? lineArray[35] : lineArray[30];
                        if (metname.Contains("RIKEN")) continue;
                        if (comment.Contains("IS")) continue;
                        if (ontology.Contains("Others")) continue;
                        if (ontology.Contains("SPE")) continue;
                        // excluded lipid check
                        var isExcluded = false;
                        foreach (var excludedlipid in excludelist) {
                            if (metname == excludedlipid) {
                                isExcluded = true;
                                break;
                            }
                        }
                        if (isExcluded) continue;

                        // pe cer check
                        foreach (var pecer in pe_cer_conversions) {
                            if (metname == pecer[0]) {
                                metname = pecer[1];
                                smiles = pecer[2];
                                inchikey = pecer[3];
                            }
                        }

                        // hex cer eos check
                        foreach (var hexcer in hexcer_eos_conversions) {
                            if (metname == hexcer[0]) {
                                smiles = hexcer[1];
                                inchikey = hexcer[2];
                            }
                        }

                        // pe check
                        foreach (var pe in etherpe_conversions) {
                            if (metname == pe[0]) {
                                metname = pe[1];
                                ontology = "EtherPE";
                            }
                        }


                        var field = new MsdialAlignmetMetaDataField() {
                            AlignmentID = id, Rt = rt, Dt = dt, Ccs = ccs, Mz = mz,
                            MetaboliteName = metname, Adduct = adduct, Formula = formula, Ontology = ontology, InChIKey = inchikey,
                            SMILES = smiles, Annotation = annotationtag, Comment = comment, SN = sn, SpectrumRefFileName = specfile, Spectrum = specstring,
                            MetAdduct = metname + "%" + adduct
                        };
                        metafields.Add(field);
                    }
                }
            }

            metafields = metafields.OrderBy(n => n.MetAdduct).ThenByDescending(n => n.SN).ToList();
            var lastkey = metafields[0].MetAdduct;
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                var header = new List<string>() { "AlignmentID", "Rt", "Mz", "MetAdduct", "MetaboliteName", "Adduct", "Formula", "Ontology", "InChIKey", "SMILES", "Annotation", "Comment", "SN", "SpectrumRefFileName", "Spectrum" };
                sw.WriteLine(String.Join("\t", header.ToArray()));
                writeDdaField(sw, metafields[0]);
                for (int i = 1; i < metafields.Count; i++) {
                    var metAdduct = metafields[i].MetAdduct;
                    if (lastkey == metAdduct) continue;

                    lastkey = metAdduct;
                    writeDdaField(sw, metafields[i]);
                }
            }
        }

        public static void ExportPasefStatistics(string inputfolder, string output) {
            var files = System.IO.Directory.GetFiles(inputfolder);
            var metafields = new List<MsdialAlignmetMetaDataField>();
            var excludelist = new List<string>() {
                "DAG 17:0e/28:7",
                "FA 36:10",
                "FA 38:9",
                "FA 40:10",
                "FA 40:11",
                "FA 42:12",
                "FA 44:10",
                "FA 44:4",
                "FA 44:8",
                "LDGCC 40:6",
                "LDGCC 42:7",
                "PC 16:4e/18:1",
                "PC 16:4e/22:6",
                "PC 16:3-26:7",
                "PC 28:3e/18:2",
                "PC 28:5e/12:0",
                "PC 28:7e/18:1",
                "PC 8:0-32:8",
                "PC 8:0-32:9",
                "Cer-NS d15:3/38:9",
                "Cer-NS d16:1/36:10",
                "Cer-NS d18:1/36:10",
                "Cer-NS d18:1/38:10",
                "Cer-NS d20:2/36:10",
                "TAG 16:0-19:0-38:10"
            };
            var pe_cer_conversions = new List<string[]>() {
                new string[]{ "PE-Cer t20:0/14:1", "PE-Cer d16:1/18:0+O", @"CCCCCCCCCCCCCCCC(O)CC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCC", "KNGCQTKRBSUXLX-BYCLXTJYNA-N" },
                new string[]{ "PE-Cer d18:0/16:1", "PE-Cer d18:1/16:0", @"CCCCCCCCCCCCCCCC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCCCC", "JTVNQMOIYKXJKF-ORIPQNMZNA-N" },
                new string[]{ "PE-Cer d20:0/16:1", "PE-Cer d18:1/18:0", @"CCCCCCCCCCCCCCCCCC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCCCC", "ODXAYQMCAAQOSY-OWWNRXNENA-N" },
                new string[]{ "PE-Cer d26:1/16:1", "PE-Cer d18:1/24:1", @"CCCCCCCCCCCCC\C=C\C(O)C(COP(O)(=O)OCCN)NC(=O)CCCCCCCCCCC\C=C/CCCCCCCCCC", "SORHVKVBHXFRAM-AKTWBEBINA-N" }
            };

            var hexcer_eos_conversions = new List<string[]>() {
                 new string[] {"HexCer-EOS d17:1/32:0-O-18:2", @"CCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC","CGLSKLYKULJGNR-NCPWIQSONA-N" },
                 new string[] {"HexCer-EOS d17:1/34:1-O-18:2", @"CCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCC\C=C\CCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "YWAMKMPXTXJEIE-IRMUNXAANA-N" },
                 new string[] {"HexCer-EOS d17:1/36:1-O-18:2", @"CCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCC\C=C\CCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "JHBYQIZVVGBZAF-REJDZBDGNA-N" },
                 new string[] {"HexCer-EOS d18:1/32:0-O-18:2", @"CCCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "GRNOIEDXTPLWNI-FHDUZSEONA-N" },
                 new string[] {"HexCer-EOS d18:1/34:0-O-18:2", @"CCCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "PAIFHGDNASKFQQ-APUKNJKBNA-N" },
                 new string[] {"HexCer-EOS d18:1/34:1-O-18:2", @"CCCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCC\C=C\CCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "GZKQHTAYXBKUDZ-JVPOEFIKNA-N" },
                 new string[] {"HexCer-EOS d18:1/36:1-O-18:2", @"CCCCCCCCCCCCC\C=C\C(O)C(COC1OC(CO)C(O)C(O)C1O)NC(=O)CCCCCCCCCCCCCC\C=C\CCCCCCCCCCCCCCCCCCCOC(=O)CCCCCCC\C=C/C\C=C/CCCCC", "DSUASOJSBHLJPB-QZUQHBRMNA-N" }
            };
            var etherpe_conversions = new List<string[]>() {
                 new string[]{ "PE 14:-1p/22:2", "PE 14:0e/22:2" },
            };
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var isIonMobilityFormat = false;
                if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;
                if (!isIonMobilityFormat) continue;
                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        var id = int.Parse(lineArray[0]);
                        var mz = double.Parse(lineArray[2]);
                        var rt = double.Parse(lineArray[1]);
                        var dt = isIonMobilityFormat ? double.Parse(lineArray[3]) : -1.0;
                        var ccs = isIonMobilityFormat ? double.Parse(lineArray[4]) : -1.0;
                        var metname = isIonMobilityFormat ? lineArray[5] : lineArray[3];
                        var adduct = isIonMobilityFormat ? lineArray[6] : lineArray[4];
                        var formula = isIonMobilityFormat ? lineArray[13] : lineArray[10];
                        var comment = isIonMobilityFormat ? lineArray[22] : lineArray[18];
                        var ontology = isIonMobilityFormat ? lineArray[14] : lineArray[11];
                        var smiles = isIonMobilityFormat ? lineArray[16] : lineArray[13];
                        var inchikey = isIonMobilityFormat ? lineArray[15] : lineArray[12];
                        var annotationtag = isIonMobilityFormat ? lineArray[17] : lineArray[14];
                        var sn = isIonMobilityFormat ? double.Parse(lineArray[32]) : double.Parse(lineArray[27]);
                        var specfile = isIonMobilityFormat ? lineArray[33] : lineArray[28];
                        var specstring = isIonMobilityFormat ? lineArray[35] : lineArray[30];
                        if (metname.Contains("RIKEN")) continue;
                        if (comment.Contains("IS")) continue;
                        if (ontology.Contains("Others")) continue;
                        if (ontology.Contains("SPE")) continue;
                        if (dt < 0) continue;
                        // excluded lipid check
                        var isExcluded = false;
                        foreach (var excludedlipid in excludelist) {
                            if (metname == excludedlipid) {
                                isExcluded = true;
                                break;
                            }
                        }
                        if (isExcluded) continue;

                        // pe cer check
                        foreach (var pecer in pe_cer_conversions) {
                            if (metname == pecer[0]) {
                                metname = pecer[1];
                                smiles = pecer[2];
                                inchikey = pecer[3];
                            }
                        }

                        // hex cer eos check
                        foreach (var hexcer in hexcer_eos_conversions) {
                            if (metname == hexcer[0]) {
                                smiles = hexcer[1];
                                inchikey = hexcer[2];
                            }
                        }

                        // pe check
                        foreach (var pe in etherpe_conversions) {
                            if (metname == pe[0]) {
                                metname = pe[1];
                                ontology = "EtherPE";
                            }
                        }
                        var field = new MsdialAlignmetMetaDataField() {
                            AlignmentID = id, Rt = rt, Dt = dt, Ccs = ccs, Mz = mz,
                            MetaboliteName = metname, Adduct = adduct, Formula = formula, Ontology = ontology, InChIKey = inchikey,
                            SMILES = smiles, Annotation = annotationtag, Comment = comment, SN = sn, SpectrumRefFileName = specfile, Spectrum = specstring,
                            MetAdduct = metname + "%" + adduct
                        };
                        metafields.Add(field);
                    }
                }
            }

            metafields = metafields.OrderBy(n => n.MetAdduct).ThenByDescending(n => n.SN).ToList();
            var lastkey = metafields[0].MetAdduct;
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                var header = new List<string>() { "AlignmentID", "Rt", "Dt", "Ccs", "Mz", "MetAdduct", "MetaboliteName", "Adduct", "Formula", "Ontology", "InChIKey", "SMILES", "Annotation", "Comment", "SN", "SpectrumRefFileName", "Spectrum" };
                sw.WriteLine(String.Join("\t", header.ToArray()));
                writePasefField(sw, metafields[0]);
                for (int i = 1; i < metafields.Count; i++) {
                    var metAdduct = metafields[i].MetAdduct;
                    if (lastkey == metAdduct) continue;
                    lastkey = metAdduct;
                    writePasefField(sw, metafields[i]);
                }
            }
        }


        private static void writeDdaField(StreamWriter sw, MsdialAlignmetMetaDataField field) {
            var fieldArray = new List<string>() { field.AlignmentID.ToString(), field.Rt.ToString(), field.Mz.ToString(), field.MetAdduct,
            field.MetaboliteName, field.Adduct, field.Formula, field.Ontology, field.InChIKey, field.SMILES, field.Annotation, field.Comment, field.SN.ToString(), field.SpectrumRefFileName, field.Spectrum };
            sw.WriteLine(String.Join("\t", fieldArray.ToArray()));
        }

        private static void writePasefField(StreamWriter sw, MsdialAlignmetMetaDataField field) {
            var fieldArray = new List<string>() { field.AlignmentID.ToString(), field.Rt.ToString(), field.Dt.ToString(), field.Ccs.ToString(), field.Mz.ToString(), field.MetAdduct,
            field.MetaboliteName, field.Adduct, field.Formula, field.Ontology, field.InChIKey, field.SMILES, field.Annotation, field.Comment, field.SN.ToString(), field.SpectrumRefFileName, field.Spectrum };
            sw.WriteLine(String.Join("\t", fieldArray.ToArray()));
        }

        public static void QuantDiff(string inputfolder1, string inputfolder2, string outputfolder) {
            var originalfiles = System.IO.Directory.GetFiles(inputfolder1);
            var curatedfiles = System.IO.Directory.GetFiles(inputfolder2);
            foreach (var originalfile in originalfiles) {
                var originalfilename = System.IO.Path.GetFileNameWithoutExtension(originalfile);
                var curatedfilepath = inputfolder2 + "\\" + originalfilename + ".txt";
                var output = outputfolder + "\\" + originalfilename + "-diff.txt";
                var isIonMobilityFormat = false;
                if (originalfilename.Contains("31_") || originalfilename.Contains("50-65_") || originalfilename.Contains("50-65VS2_") || originalfilename.Contains("84_")) isIonMobilityFormat = true;

                var originallist = new List<string[]>();
                var curatedlist = new List<string[]>();
                var header = string.Empty;
                using (var sr = new StreamReader(originalfile, Encoding.ASCII)) {
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    header = sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        originallist.Add(lineArray);
                    }
                }

                using (var sr = new StreamReader(curatedfilepath, Encoding.ASCII)) {
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        curatedlist.Add(lineArray);
                    }
                }

                //exist check
                // origin -> curated
                var strings = new List<string>();
                foreach (var origin in originallist) {
                    var isExist = false;
                    foreach (var curate in curatedlist) {
                        if (origin[0] == curate[0]) {
                            isExist = true;
                            break;
                        }
                    }
                    if (!isExist) {
                        strings.Add(String.Join("\t", origin));
                    }
                }

                // curated -> origin
                foreach (var curate in curatedlist) {
                    var isExist = false;
                    foreach (var origin in originallist) {
                        if (origin[0] == curate[0]) {
                            isExist = true;
                            break;
                        }
                    }
                    if (!isExist) {
                        strings.Add(String.Join("\t", curate));
                    }
                }

                if (strings.Count == 0) continue;
                using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                    sw.WriteLine(String.Join("\t", header.Split('\t')));
                    // origin -> curated
                    foreach (var origin in originallist) {
                        var isExist = false;
                        foreach (var curate in curatedlist) {
                            if (origin[0] == curate[0]) {
                                isExist = true;
                                break;
                            }
                        }
                        if (!isExist) {
                            sw.WriteLine(String.Join("\t", origin));
                            Console.WriteLine(originalfilename);
                        }
                    }

                    // curated -> origin
                    foreach (var curate in curatedlist) {
                        var isExist = false;
                        foreach (var origin in originallist) {
                            if (origin[0] == curate[0]) {
                                isExist = true;
                                break;
                            }
                        }
                        if (!isExist) {
                            sw.WriteLine(String.Join("\t", curate));
                        }
                    }
                }
            }

        }

        public static void DdaMSPsToMeta(string inputfolder, string output, string exportion) {
            var excludelist = new List<string>() {
                "DAG 17:0e/28:7",
                "FA 36:10",
                "FA 38:9",
                "FA 40:10",
                "FA 40:11",
                "FA 42:12",
                "FA 44:10",
                "FA 44:4",
                "FA 44:8",
                "LDGCC 40:6",
                "LDGCC 42:7",
                "PC 16:4e/18:1",
                "PC 16:4e/22:6",
                "PC 16:3-26:7",
                "PC 28:3e/18:2",
                "PC 28:5e/12:0",
                "PC 28:7e/18:1",
                "PC 8:0-32:8",
                "PC 8:0-32:9",
                "Cer-NS d15:3/38:9",
                "Cer-NS d16:1/36:10",
                "Cer-NS d18:1/36:10",
                "Cer-NS d18:1/38:10",
                "Cer-NS d20:2/36:10",
                "TAG 16:0-19:0-38:10"
            };
            var pe_cer_conversions = new List<string[]>() {
                new string[]{ "PE-Cer t34:1; PE-Cer t20:0/14:1; [M-H]-", "PE-Cer d34:1+O; PE-Cer d16:1/18:0+O; [M-H]-", @"CCCCCCCCCCCCCCCC(O)CC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCC", "KNGCQTKRBSUXLX-BYCLXTJYNA-N" },
                new string[]{ "PE-Cer d34:1; PE-Cer d18:0/16:1; [M-H]-", "PE-Cer d34:1; PE-Cer d18:1/16:0; [M-H]-", @"CCCCCCCCCCCCCCCC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCCCC", "JTVNQMOIYKXJKF-ORIPQNMZNA-N" },
                new string[]{ "PE-Cer d36:1; PE-Cer d20:0/16:1; [M-H]-", "PE-Cer d36:1; PE-Cer d18:1/18:0; [M-H]-", @"CCCCCCCCCCCCCCCCCC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCCCC", "ODXAYQMCAAQOSY-OWWNRXNENA-N" },
                new string[]{ "PE-Cer d42:2; PE-Cer d26:1/16:1; [M-H]-", "PE-Cer d42:2; PE-Cer d18:1/24:1; [M-H]-", @"CCCCCCCCCCCCC\C=C\C(O)C(COP(O)(=O)OCCN)NC(=O)CCCCCCCCCCC\C=C/CCCCCCCCCC", "SORHVKVBHXFRAM-AKTWBEBINA-N" }
            };
            var etherpe_conversions = new List<string[]>() {
                 new string[]{ "PE 14:-1p/22:2", "PE 14:0e/22:2" },
            };

            var files = System.IO.Directory.GetFiles(inputfolder);
            var header = new List<string>() { "NAME", "RETENTIONTIME", "PRECURSORMZ", "PRECURSORTYPE", "IONMODE", "FORMULA", "ONTOLOGY", "INCHIKEY", "SMILES", "COMMENT", "Num Peaks", "Spectrum" };
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine(String.Join("\t", header.ToArray()));
                foreach (var file in files) {
                    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                    var isIonMobilityFormat = false;
                    if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;
                    if (isIonMobilityFormat) continue;
                    if (filename.Contains("neg") && exportion == "Positive") continue;
                    if (filename.Contains("pos") && exportion == "Negative") continue;

                    var mspfields = MspFileParcer.MspFileReader(file);
                    foreach (var field in mspfields) {
                        var metname = field.Name;
                        var rt = field.RetentionTime;
                        var mz = field.PrecursorMz;
                        var adduct = field.AdductIonBean.AdductIonName;
                        var ionmode = field.IonMode;
                        var formula = field.Formula;
                        var ontology = field.Ontology;
                        var inchikey = field.InchiKey;
                        var smiles = field.Smiles;
                        var comment = filename + "%" + field.Comment;
                        var numpeaks = field.MzIntensityCommentBeanList.Count();
                        var specString = GetSpectrumString(field.MzIntensityCommentBeanList);
                        if (metname.Contains("(d")) continue;
                        if (metname.Contains("(13C")) continue;
                        if (ontology.Contains("Others")) continue;
                        if (ontology.StartsWith("IS")) continue;
                        if (ontology == "FA") continue;
                        if (ontology == "BileAcid") continue;
                        if (ontology == "NAE") continue;
                        if (ontology == "SPE") continue;
                        if (adduct == "[M+Na]+") continue;

                        if (metname.Contains("w/o MS2:") || metname.Contains("Unsettled:") || metname.Contains("Unknown") || metname.Contains("RIKEN") || metname == string.Empty) {
                            metname = "Unknown";
                            formula = "null";
                            ontology = "null";
                            inchikey = "null";
                            smiles = "null";
                        } 

                        if (ionmode == IonMode.Negative && ontology == "BMP") {
                            metname = metname.Replace("BMP", "PG");
                            ontology = "PG";
                        }
                        if (metname != "Unknown") {
                            var annotation = LipidomicsConverter.LipidomicsAnnotationLevel(metname, ontology, adduct);
                            if (annotation != "Chain resolved" && annotation != "Chain semi-resolved") {
                                metname = "Unknown";
                                formula = "null";
                                ontology = "null";
                                inchikey = "null";
                                smiles = "null";
                            }
                        }

                        // excluded lipid check
                        var isExcluded = false;
                        foreach (var excludedlipid in excludelist) {
                            if (metname.Contains(excludedlipid)) {
                                isExcluded = true;
                                break;
                            }
                        }
                        if (isExcluded) continue;

                        // pe cer check
                        foreach (var pecer in pe_cer_conversions) {
                            if (metname == pecer[0]) {
                                metname = pecer[1];
                                smiles = pecer[2];
                                inchikey = pecer[3];
                            }
                        }

                        // pe check
                        foreach (var pe in etherpe_conversions) {
                            if (metname == pe[0]) {
                                metname = pe[1];
                                ontology = "EtherPE";
                            }
                        }

                        var exportfield = new List<string>() { metname, rt.ToString(), mz.ToString(), adduct, ionmode.ToString(), formula, ontology, inchikey, smiles, comment, numpeaks.ToString(), specString };
                        sw.WriteLine(String.Join("\t", exportfield.ToArray()));
                    }
                }
            }
        }

        public static void PasefMSPsToMeta(string inputfolder, string output, string exportion) {
            var excludelist = new List<string>() {
                "DAG 17:0e/28:7",
                "FA 36:10",
                "FA 38:9",
                "FA 40:10",
                "FA 40:11",
                "FA 42:12",
                "FA 44:10",
                "FA 44:4",
                "FA 44:8",
                "LDGCC 40:6",
                "LDGCC 42:7",
                "PC 16:4e/18:1",
                "PC 16:4e/22:6",
                "PC 16:3-26:7",
                "PC 28:3e/18:2",
                "PC 28:5e/12:0",
                "PC 28:7e/18:1",
                "PC 8:0-32:8",
                "PC 8:0-32:9",
                "Cer-NS d15:3/38:9",
                "Cer-NS d16:1/36:10",
                "Cer-NS d18:1/36:10",
                "Cer-NS d18:1/38:10",
                "Cer-NS d20:2/36:10",
                "TAG 16:0-19:0-38:10"
            };
            var pe_cer_conversions = new List<string[]>() {
                new string[]{ "PE-Cer t34:1; PE-Cer t20:0/14:1; [M-H]-", "PE-Cer d34:1+O; PE-Cer d16:1/18:0+O; [M-H]-", @"CCCCCCCCCCCCCCCC(O)CC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCC", "KNGCQTKRBSUXLX-BYCLXTJYNA-N" },
                new string[]{ "PE-Cer d34:1; PE-Cer d18:0/16:1; [M-H]-", "PE-Cer d34:1; PE-Cer d18:1/16:0; [M-H]-", @"CCCCCCCCCCCCCCCC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCCCC", "JTVNQMOIYKXJKF-ORIPQNMZNA-N" },
                new string[]{ "PE-Cer d36:1; PE-Cer d20:0/16:1; [M-H]-", "PE-Cer d36:1; PE-Cer d18:1/18:0; [M-H]-", @"CCCCCCCCCCCCCCCCCC(=O)NC(COP(O)(=O)OCCN)C(O)\C=C\CCCCCCCCCCCCC", "ODXAYQMCAAQOSY-OWWNRXNENA-N" },
                new string[]{ "PE-Cer d42:2; PE-Cer d26:1/16:1; [M-H]-", "PE-Cer d42:2; PE-Cer d18:1/24:1; [M-H]-", @"CCCCCCCCCCCCC\C=C\C(O)C(COP(O)(=O)OCCN)NC(=O)CCCCCCCCCCC\C=C/CCCCCCCCCC", "SORHVKVBHXFRAM-AKTWBEBINA-N" }
            };
            var etherpe_conversions = new List<string[]>() {
                 new string[]{ "PE 14:-1p/22:2", "PE 36:2e; PE 14:0e/22:2; [M+H]+" },
            };

            var files = System.IO.Directory.GetFiles(inputfolder, "*.msp");
            var header = new List<string>() { "NAME", "RETENTIONTIME", "CCS", "PRECURSORMZ", "PRECURSORTYPE", "IONMODE", "FORMULA", "ONTOLOGY", "INCHIKEY", "SMILES", "COMMENT", "Num Peaks", "Spectrum" };
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine(String.Join("\t", header.ToArray()));
                foreach (var file in files) {
                    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                    var isIonMobilityFormat = false;
                    if (filename.Contains("31_") || filename.Contains("50-65_") || filename.Contains("50-65VS2_") || filename.Contains("84_")) isIonMobilityFormat = true;
                    if (!isIonMobilityFormat) continue;
                    if (filename.Contains("neg") && exportion == "Positive") continue;
                    if (filename.Contains("pos") && exportion == "Negative") continue;

                    var mspfields = MspFileParcer.MspFileReader(file);
                    foreach (var field in mspfields) {
                        var metname = field.Name;
                        var rt = field.RetentionTime;
                        var ccs = field.CollisionCrossSection;
                        var mz = field.PrecursorMz;
                        var adduct = field.AdductIonBean.AdductIonName;
                        var ionmode = field.IonMode;
                        var formula = field.Formula;
                        var ontology = field.Ontology;
                        var inchikey = field.InchiKey;
                        var smiles = field.Smiles;
                        var comment = filename + "%" + field.Comment;
                        var numpeaks = field.MzIntensityCommentBeanList.Count();
                        var specString = GetSpectrumString(field.MzIntensityCommentBeanList);
                        if (metname.Contains("(d")) continue;
                        if (metname.Contains("(13C")) continue;
                        if (ontology.Contains("Others")) continue;
                        if (ontology.StartsWith("IS")) continue;
                        if (ontology == "FA") continue;
                        if (ontology == "ACar") continue;
                        if (ontology == "BileAcid") continue;
                        if (ontology == "NAE") continue;
                        if (ontology == "SPE") continue;
                        if (adduct == "[M+Na]+") continue;

                        if (ccs < 0) continue;
                        if (metname.Contains("w/o MS2:") || metname.Contains("Unsettled:") || metname.Contains("Unknown") || metname.Contains("RIKEN") || metname == string.Empty) {
                            metname = "Unknown";
                            formula = "null";
                            ontology = "null";
                            inchikey = "null";
                            smiles = "null";
                        }
                        if (ionmode == IonMode.Negative && ontology == "BMP") {
                            metname = metname.Replace("BMP", "PG");
                            ontology = "PG";
                        }
                        if (metname != "Unknown") {
                            var annotation = LipidomicsConverter.LipidomicsAnnotationLevel(metname, ontology, adduct);
                            if (annotation != "Chain resolved" && annotation != "Chain semi-resolved") {
                                metname = "Unknown";
                                formula = "null";
                                ontology = "null";
                                inchikey = "null";
                                smiles = "null";
                            }
                        }
                        // excluded lipid check
                        var isExcluded = false;
                        foreach (var excludedlipid in excludelist) {
                            if (metname.Contains(excludedlipid)) {
                                isExcluded = true;
                                break;
                            }
                        }
                        if (isExcluded) continue;

                        // pe cer check
                        foreach (var pecer in pe_cer_conversions) {
                            if (metname == pecer[0]) {
                                metname = pecer[1];
                                smiles = pecer[2];
                                inchikey = pecer[3];
                            }
                        }
                        // pe check
                        foreach (var pe in etherpe_conversions) {
                            if (metname.Contains(pe[0])) {
                                metname = pe[1];
                                ontology = "EtherPE";
                            }
                        }
                        var exportfield = new List<string>() { metname, rt.ToString(), ccs.ToString(), mz.ToString(), adduct, ionmode.ToString(), formula, ontology, inchikey, smiles, comment, numpeaks.ToString(), specString };
                        sw.WriteLine(String.Join("\t", exportfield.ToArray()));
                    }
                }
            }
        }

        public static string GetSpectrumString(List<MzIntensityCommentBean> massSpectra) {
            if (massSpectra == null || massSpectra.Count == 0)
                return string.Empty;

            var specString = string.Empty;
            for (int i = 0; i < massSpectra.Count; i++) {
                var spec = massSpectra[i];
                var mz = Math.Round(spec.Mz, 5);
                var intensity = Math.Round(spec.Intensity, 0);
                var sString = mz + ":" + intensity;

                if (i == massSpectra.Count - 1)
                    specString += sString;
                else
                    specString += sString + " ";

                if (specString.Length >= 30000) break;
            }

            return specString;
        }

        public static void AdductCuratorForMspText(string inputfolder, string output) {
            var mismatchlist = new List<string[]>();
            var files = System.IO.Directory.GetFiles(inputfolder);
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var isIonMobilityFormat = false;
                if (filename.Contains("Pasef_")) isIonMobilityFormat = true;

                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        var mz = isIonMobilityFormat ? double.Parse(lineArray[3]) : double.Parse(lineArray[2]);
                        var metname = lineArray[0];
                        var adduct = isIonMobilityFormat ? lineArray[4] : lineArray[3];
                        var formula = isIonMobilityFormat ? lineArray[6] : lineArray[5];
                        var comment = isIonMobilityFormat ? lineArray[10] : lineArray[9];
                        if (metname.Contains("Unknown")) continue;

                        var adductObj = AdductIonParcer.GetAdductIonBean(adduct);
                        var formulaObj = FormulaStringParcer.OrganicElementsReader(formula);
                        var exactMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(adductObj, formulaObj.Mass);
                        if (Math.Abs(mz - exactMz) > 0.1) {
                            mismatchlist.Add(new string[] { comment, metname, adduct, mz.ToString(), exactMz.ToString(), (mz - exactMz).ToString() });
                        }
                    }
                }
            }
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var miskey in mismatchlist) {
                    sw.WriteLine(String.Join("\t", miskey));
                }
            }
        }

        public static void FalseDiscoveryRateCalculation(string inputspec, string inputref, string inputqueries, IonMode ionmode, 
            float rttol, float ms1tol, float ms2tol, float ccstol, string output) {
            Console.WriteLine("Importing libraries");
            var param = parameterConfig(inputqueries, ionmode, rttol, ms1tol, ms2tol, ccstol);
            var testQueries = getTestQueries(inputspec, param.IsIonMobility, ionmode);
            var lbmQuery = param.LipidQueryBean;
            var mspQueries = LbmFileParcer.ReadSerializedObjectLibrary(inputref, lbmQuery.LbmQueries, ionmode, lbmQuery.SolventType, lbmQuery.CollisionType);
            var projectProp = new ProjectPropertyBean() { DataType = DataType.Centroid, DataTypeMS2 = DataType.Centroid, IonMode = ionmode,
                IsLabPrivateVersion = true, MethodType = MethodType.ddMSMS, TargetOmics = TargetOmics.Lipidomics };

            testQueries = testQueries.OrderBy(n => n.peakAreaBean.AccurateMass).ToList();
            mspQueries = mspQueries.OrderBy(n => n.PrecursorMz).ToList();
            var counter = 0;
            foreach (var query in mspQueries) {
                query.Id = counter;
                counter++;
            }

            Console.WriteLine("Start calculations..");
            int tp = 0, tn = 0, fp = 0, fn = 0;
            int total = testQueries.Count;
            var ms1Spectra = new ObservableCollection<double[]>();
            var unmatchedlist = new List<string[]>();
            for (int i = 0; i < testQueries.Count; i++) {
                var testQ = testQueries[i];
                var ms2Spectra = Identification.getNormalizedMs2Spectra(testQ.ms2DecResult.MassSpectra, param);
                if (param.IsIonMobility) {
                    Identification.similarityCalculationsIonMobilityMsMsIncluded(testQ.driftSpotBean, testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                }
                else {
                    Identification.similarityCalculationsMsMsIncluded(testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                }

                var metname = param.IsIonMobility ? testQ.driftSpotBean.MetaboliteName : testQ.peakAreaBean.MetaboliteName;
                if (metname.Contains("RIKEN") || metname.Contains("w/o MS2:") || metname == null || metname == string.Empty) metname = "Unknown";
                var refID = param.IsIonMobility ? testQ.driftSpotBean.LibraryID : testQ.peakAreaBean.LibraryID;

                var ontology = refID >= 0 ? mspQueries[refID].CompoundClass : "null";
                var adduct = refID >= 0 ? mspQueries[refID].AdductIonBean.AdductIonName : "null";
                var refRt = refID >= 0 ? mspQueries[refID].RetentionTime.ToString() : "null";
                var refCCS = refID >= 0 ? mspQueries[refID].CollisionCrossSection.ToString() : "null";
                if (ontology == "FA" || ontology == "NAE" || ontology == "BileAcid") metname = "Unknown";
                if (metname != "Unknown") {
                    var annotation = LipidomicsConverter.LipidomicsAnnotationLevel(metname, ontology, adduct);
                    if (annotation != "Chain resolved" && annotation != "Chain semi-resolved") {
                        metname = "Unknown";
                        ontology = "null";
                    }
                }
                if (adduct == "[M+Na]+") metname = "Unknown";
                if (metname.Contains("(d")) metname = "Unknown";
                if (metname != "Unknown" && metname == testQ.NAME) {
                    tp++;
                }
                else if ((ontology == "TAG" || ontology == "EtherTAG") && metname.Split(';')[0] == testQ.NAME.Split(';')[0]) {
                    tp++;
                }
                else if (metname == "Unknown" && testQ.NAME == "Unknown") {
                    tn++;
                }
                else {
                    if (metname == "Unknown") {
                        var error = new string[] { "False negative", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS, testQ.PRECURSORMZ, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                        unmatchedlist.Add(error);
                        fn++;
                    }
                    else {
                        var error = new string[] { "False positive", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS, testQ.PRECURSORMZ, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                        unmatchedlist.Add(error);
                        fp++;
                    }
                }

                Console.Write("{0} / {1}", i + 1, total);
                Console.SetCursorPosition(0, Console.CursorTop);
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                //http://ibisforest.org/index.php?F%E5%80%A4
                sw.WriteLine("Summary");
                sw.WriteLine("True positive" + "\t" + tp);
                sw.WriteLine("True negative" + "\t" + tn);
                sw.WriteLine("False positive" + "\t" + fp);
                sw.WriteLine("False negative" + "\t" + fn);

                var accuracy = Math.Round((double)(tp + tn) / (double)(tp + fp + tn + fn) * 100, 2);
                var precision = Math.Round((double)tp / (double)(tp + fp) * 100, 2);
                var recall = Math.Round((double)tp / (double)(tp + fn) * 100, 2);
                var specificity = Math.Round((double)tn / (double)(fp + tn) * 100, 2);
                var fscore = (double)(recall * 2 * precision) / (double)(recall + precision);
                var fdr = Math.Round((double)fp / (double)(fp + tp) * 100, 2);

                sw.WriteLine("Accuracy" + "\t" + accuracy.ToString() + "%");
                sw.WriteLine("Precision" + "\t" + precision.ToString() + "%");
                sw.WriteLine("Recall" + "\t" + recall.ToString() + "%");
                sw.WriteLine("Specificity" + "\t" + specificity.ToString() + "%");
                sw.WriteLine("F-measure" + "\t" + fscore.ToString());
                sw.WriteLine("FDR" + "\t" + fdr.ToString());

                var header = new string[] { "Type", "Automatic annotation", "Curated annotation", "Query RT", "Ref RT", "Query CCS", "Ref CCS", "MZ", "Adduct", "Formula", "Ontology", "SMILES", "InChIKey", "Comment" };
                sw.WriteLine(String.Join("\t", header));
                foreach (var miskey in unmatchedlist) {
                    sw.WriteLine(String.Join("\t", miskey));
                }
            }
        }

        public static void FalseDiscoveryRateCalculationVS2(string inputspec, string inputref, string inputqueries, IonMode ionmode,
           float rttol, float ms1tol, float ms2tol, float ccstol, string output) {
            Console.WriteLine("Importing libraries");
            var param = parameterConfig(inputqueries, ionmode, rttol, ms1tol, ms2tol, ccstol);
            var param2 = parameterConfig(inputqueries, ionmode, rttol, ms1tol + 0.0025F, ms2tol, ccstol);
            var testQueries = getTestQueries(inputspec, param.IsIonMobility, ionmode);
            var lbmQuery = param.LipidQueryBean;
            var mspQueries = LbmFileParcer.ReadSerializedObjectLibrary(inputref, lbmQuery.LbmQueries, ionmode, lbmQuery.SolventType, lbmQuery.CollisionType);
            var projectProp = new ProjectPropertyBean() {
                DataType = DataType.Centroid, DataTypeMS2 = DataType.Centroid, IonMode = ionmode,
                IsLabPrivateVersion = true, MethodType = MethodType.ddMSMS, TargetOmics = TargetOmics.Lipidomics
            };

            testQueries = testQueries.OrderBy(n => n.peakAreaBean.AccurateMass).ToList();
            mspQueries = mspQueries.OrderBy(n => n.PrecursorMz).ToList();
            var counter = 0;
            foreach (var query in mspQueries) {
                query.Id = counter;
                counter++;
            }

            Console.WriteLine("Start calculations..");
            int tp = 0, tn = 0, fp = 0, fn = 0;
            int total = testQueries.Count;
            var ms1Spectra = new ObservableCollection<double[]>();
            var unmatchedlist = new List<string[]>();
            for (int i = 0; i < testQueries.Count; i++) {
                var testQ = testQueries[i];
                var ms2Spectra = Identification.getNormalizedMs2Spectra(testQ.ms2DecResult.MassSpectra, param);
                List<MatchedCandidate> candidates = null;
                if (param.IsIonMobility) {
                    candidates = Identification.GetImMsMsMatchedCandidatesTest(testQ.driftSpotBean, testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                }
                else {
                    candidates = Identification.GetMsMsMatchedCandidatesTest(testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                }
                if (testQ.COMMENT == "25_RIKEN IMS Sciex Mouse tissues_Mouse_Plasma_pos%2403") {
                    //Console.WriteLine();
                }
                if (candidates == null || candidates.Count == 0) {
                    if (testQ.NAME == "Unknown") {
                        tn++;
                        Console.Write("{0} / {1}", i + 1, total);
                        Console.SetCursorPosition(0, Console.CursorTop);
                        continue;
                    }
                    else {
                        if (param.IsIonMobility) {
                            candidates = Identification.GetImMsMsMatchedCandidatesTest(testQ.driftSpotBean, testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param2, projectProp);
                        }
                        else {
                            candidates = Identification.GetMsMsMatchedCandidatesTest(testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param2, projectProp);
                        }
                        if (candidates == null || candidates.Count == 0) {
                            var error = new string[] { "False negative", "Unknown", testQ.NAME, testQ.RETENTIONTIME, "null", testQ.CCS, "null",
                            testQ.PRECURSORMZ, "null", testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                            unmatchedlist.Add(error);
                            fn++;
                            Console.Write("{0} / {1}", i + 1, total);
                            Console.SetCursorPosition(0, Console.CursorTop);
                            continue;
                        }
                    }
                }
                
                var isMatchedNameExist = false;
                var matchedId = -1;
                for (int j = 0; j < candidates.Count; j++) {
                    var localMetName = candidates[j].AnnotatedName;
                    if (localMetName.Contains('|')) {
                        localMetName = localMetName.Split('|')[localMetName.Split('|').Length - 1];
                    }
                    if (localMetName == testQ.NAME) {
                        isMatchedNameExist = true;
                        matchedId = j;
                        break;
                    }
                }

                var metname = candidates[0].AnnotatedName;
                if (metname.Contains("RIKEN") || metname.Contains("w/o MS2:") || metname == null || metname == string.Empty) metname = "Unknown";
                var refID = candidates[0].MspID;

                var ontology = refID >= 0 ? mspQueries[refID].CompoundClass : "null";
                var adduct = refID >= 0 ? mspQueries[refID].AdductIonBean.AdductIonName : "null";
                var refRt = refID >= 0 ? mspQueries[refID].RetentionTime.ToString() : "null";
                var refCCS = refID >= 0 ? mspQueries[refID].CollisionCrossSection.ToString() : "null";
                var refMz = refID >= 0 ? mspQueries[refID].PrecursorMz.ToString() : "null";
                if (ontology == "FA" || ontology == "NAE" || ontology == "BileAcid") metname = "Unknown";
                if (metname == "CE 0:0") metname = "Unknown";
                if (metname != "Unknown") {
                    var annotation = LipidomicsConverter.LipidomicsAnnotationLevelVS2(metname, ontology, adduct);
                    if (annotation != "Chain resolved" && annotation != "Chain semi-resolved") {
                        metname = "Unknown";
                        ontology = "null";
                    }
                }
                if (adduct == "[M+Na]+") metname = "Unknown";
                if (metname.Contains("(d")) metname = "Unknown";

                if (metname != "Unknown" && isMatchedNameExist) {
                    tp++;
                }
                //else if ((ontology == "TG" || ontology == "EtherTG") && metname.Split(';')[0] == testQ.NAME.Split(';')[0]) {
                //    tp++;
                //}
                else if (metname == "Unknown" && testQ.NAME == "Unknown") {
                    tn++;
                }
                else {
                    if (metname == "Unknown") {
                        var error = new string[] { "False negative", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS, 
                            testQ.PRECURSORMZ, refMz, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                        unmatchedlist.Add(error);
                        fn++;
                    }
                    else {
                        var error = new string[] { "False positive", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS, 
                            testQ.PRECURSORMZ, refMz, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                        unmatchedlist.Add(error);
                        fp++;
                    }
                }
                Console.Write("{0} / {1}", i + 1, total);
                Console.SetCursorPosition(0, Console.CursorTop);
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                //http://ibisforest.org/index.php?F%E5%80%A4
                sw.WriteLine("Summary");
                sw.WriteLine("True positive" + "\t" + tp);
                sw.WriteLine("True negative" + "\t" + tn);
                sw.WriteLine("False positive" + "\t" + fp);
                sw.WriteLine("False negative" + "\t" + fn);

                var accuracy = Math.Round((double)(tp + tn) / (double)(tp + fp + tn + fn) * 100, 2);
                var precision = Math.Round((double)tp / (double)(tp + fp) * 100, 2);
                var recall = Math.Round((double)tp / (double)(tp + fn) * 100, 2);
                var specificity = Math.Round((double)tn / (double)(fp + tn) * 100, 2);
                var fscore = (double)(recall * 2 * precision) / (double)(recall + precision);
                var fdr = Math.Round((double)fp / (double)(fp + tp) * 100, 2);

                sw.WriteLine("Accuracy" + "\t" + accuracy.ToString() + "%");
                sw.WriteLine("Precision" + "\t" + precision.ToString() + "%");
                sw.WriteLine("Recall" + "\t" + recall.ToString() + "%");
                sw.WriteLine("Specificity" + "\t" + specificity.ToString() + "%");
                sw.WriteLine("F-measure" + "\t" + fscore.ToString());
                sw.WriteLine("FDR" + "\t" + fdr.ToString());

                var header = new string[] { "Type", "Automatic annotation", "Curated annotation", "Query RT", "Ref RT", "Query CCS", "Ref CCS", 
                    "MZ", "Ref MZ", "Adduct", "Formula", "Ontology", "SMILES", "InChIKey", "Comment" };
                sw.WriteLine(String.Join("\t", header));
                foreach (var miskey in unmatchedlist) {
                    sw.WriteLine(String.Join("\t", miskey));
                }
            }
        }

        public static void AnnotationForTestQueries(string inputspec, string inputref, string inputqueries, IonMode ionmode,
           float rttol, float ms1tol, float ms2tol, float ccstol, string outputDir) {
            
            Console.WriteLine("Importing libraries");
            var param = parameterConfig(inputqueries, ionmode, rttol, ms1tol, ms2tol, ccstol);
            var param2 = parameterConfig(inputqueries, ionmode, rttol, ms1tol + 0.0025F, ms2tol, ccstol);
            var testQueries = getTestQueries(inputspec, param.IsIonMobility, ionmode);
            var lbmQuery = param.LipidQueryBean;
            var mspQueries = LbmFileParcer.ReadSerializedObjectLibrary(inputref, lbmQuery.LbmQueries, ionmode, lbmQuery.SolventType, lbmQuery.CollisionType);
            var projectProp = new ProjectPropertyBean() {
                DataType = DataType.Centroid, DataTypeMS2 = DataType.Centroid, IonMode = ionmode,
                IsLabPrivateVersion = true, MethodType = MethodType.ddMSMS, TargetOmics = TargetOmics.Lipidomics
            };
            var outputDirForUnknowns = outputDir + " unknown";

            testQueries = testQueries.OrderBy(n => n.peakAreaBean.AccurateMass).ToList();
            mspQueries = mspQueries.OrderBy(n => n.PrecursorMz).ToList();
            var counter = 0;
            foreach (var query in mspQueries) {
                query.Id = counter;
                counter++;
            }

            Console.WriteLine("Start calculations..");
            int tp = 0, tn = 0, fp = 0, fn = 0;
            int total = testQueries.Count;
            var ms1Spectra = new ObservableCollection<double[]>();
            var unmatchedlist = new List<string[]>();
            for (int i = 0; i < testQueries.Count; i++) {
                var testQ = testQueries[i];
                var ms2Spectra = Identification.getNormalizedMs2Spectra(testQ.ms2DecResult.MassSpectra, param);
                List<MatchedCandidate> candidates = null;
                if (param.IsIonMobility) {
                    candidates = Identification.GetImMsMsMatchedCandidatesTest(testQ.driftSpotBean, testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                }
                else {
                    candidates = Identification.GetMsMsMatchedCandidatesTest(testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                }
                
                if (candidates == null || candidates.Count == 0) {

                    var tempname = "Unknown" + "%" + (i + 1).ToString();
                    var tempfile = outputDirForUnknowns + "\\" + tempname + ".msp";
                    using (var sw = new StreamWriter(tempfile, false, Encoding.ASCII)) {
                        sw.WriteLine("NAME: " + "Unknown");
                        sw.WriteLine("PRECURSORMZ: " + testQ.PRECURSORMZ);
                        sw.WriteLine("PRECURSORTYPE: " + testQ.PRECURSORTYPE);
                        sw.WriteLine("RETENTIONTIME: " + testQ.RETENTIONTIME);
                        sw.WriteLine("CCS: " + testQ.CCS);
                        sw.WriteLine("IONMODE: " + testQ.IONMODE);
                        sw.WriteLine("FORMULA: null");
                        sw.WriteLine("Ontology: null");
                        sw.WriteLine("SMILES: null");
                        sw.WriteLine("INCHIKEY: null");
                        sw.WriteLine("Comment: " + testQ.COMMENT);
                        sw.WriteLine("Num Peaks: " + testQ.ms2DecResult.MassSpectra.Count());

                        foreach (var peak in testQ.ms2DecResult.MassSpectra) {
                            sw.WriteLine(peak[0] + "\t" + peak[1]);
                        }
                    }

                    Console.Write("{0} / {1}", i + 1, total);
                    Console.SetCursorPosition(0, Console.CursorTop);
                    continue;
                }

                var metname = candidates[0].AnnotatedName;
                if (metname.Contains("RIKEN") || metname.Contains("w/o MS2:") || metname == null || metname == string.Empty) metname = "Unknown";
                var refID = candidates[0].MspID;

                var ontology = refID >= 0 ? mspQueries[refID].CompoundClass : "null";
                var formula = refID >= 0 ? mspQueries[refID].Formula : "null";
                var inchikey = refID >= 0 ? mspQueries[refID].InchiKey : "null";
                var smiles = refID >= 0 ? mspQueries[refID].Smiles : "null";
                var adduct = refID >= 0 ? mspQueries[refID].AdductIonBean.AdductIonName : "null";
                var refRt = refID >= 0 ? mspQueries[refID].RetentionTime.ToString() : "null";
                var refCCS = refID >= 0 ? mspQueries[refID].CollisionCrossSection.ToString() : "null";
                var refMz = refID >= 0 ? mspQueries[refID].PrecursorMz.ToString() : "null";
                
                if (ontology == "FA" || ontology == "NAE" || ontology == "BileAcid") metname = "Unknown";
                if (metname == "CE 0:0" || metname == "Cholesterol") metname = "Unknown";
                if (metname != "Unknown") {
                    var annotation = LipidomicsConverter.LipidomicsAnnotationLevelVS2(metname, ontology, adduct);
                    if (annotation != "Chain resolved") {
                        metname = "Unknown";
                        ontology = "null";
                    }
                }
                if (metname.Contains("(d")) metname = "Unknown";
                
                var comment = testQ.COMMENT;
                var filename = metname.Replace("|", "%").Replace(";", "_").Replace("/", "_").Replace("(", "_").Replace(")", "_").Replace(":", "_") + "%" +  (i + 1).ToString();
                if (metname == "Unknown") {
                    var filepath = outputDirForUnknowns + "\\" + filename + ".msp";
                    using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                        sw.WriteLine("NAME: " + "Unknown");
                        sw.WriteLine("PRECURSORMZ: " + testQ.PRECURSORMZ);
                        sw.WriteLine("PRECURSORTYPE: " + testQ.PRECURSORTYPE);
                        sw.WriteLine("RETENTIONTIME: " + testQ.RETENTIONTIME);
                        sw.WriteLine("CCS: " + testQ.CCS);
                        sw.WriteLine("IONMODE: " + testQ.IONMODE);
                        sw.WriteLine("FORMULA: null");
                        sw.WriteLine("Ontology: null");
                        sw.WriteLine("SMILES: null");
                        sw.WriteLine("INCHIKEY: null");
                        sw.WriteLine("Comment: " + testQ.COMMENT);
                        sw.WriteLine("Num Peaks: " + testQ.ms2DecResult.MassSpectra.Count());

                        foreach (var peak in testQ.ms2DecResult.MassSpectra) {
                            sw.WriteLine(peak[0] + "\t" + peak[1]);
                        }
                    }

                }
                else {
                    var filepath = outputDir + "\\" + filename + ".msp";
                    using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                        sw.WriteLine("NAME: " + metname);
                        sw.WriteLine("PRECURSORMZ: " + testQ.PRECURSORMZ);
                        sw.WriteLine("PRECURSORTYPE: " + adduct);
                        sw.WriteLine("RETENTIONTIME: " + testQ.RETENTIONTIME);
                        sw.WriteLine("CCS: " + testQ.CCS);
                        sw.WriteLine("IONMODE: " + testQ.IONMODE);
                        sw.WriteLine("FORMULA: " + formula);
                        sw.WriteLine("Ontology: " + ontology);
                        sw.WriteLine("SMILES: " + smiles);
                        sw.WriteLine("INCHIKEY: " + inchikey);
                        sw.WriteLine("Comment: " + testQ.COMMENT);
                        sw.WriteLine("Num Peaks: " + testQ.ms2DecResult.MassSpectra.Count());

                        foreach (var peak in testQ.ms2DecResult.MassSpectra) {
                            sw.WriteLine(peak[0] + "\t" + peak[1]);
                        }
                    }
                }
               

                Console.Write("{0} / {1}", i + 1, total);
                Console.SetCursorPosition(0, Console.CursorTop);
            }

           
        }


        public static void FalseDiscoveryRateCalculationRtDependency(string inputspec, string inputref, string inputqueries, IonMode ionmode,
           List<float> rttols, float ms1tol, float ms2tol, string output) {
            Console.WriteLine("Importing libraries");

            var param = parameterConfig(inputqueries, ionmode, 1.25F, ms1tol, ms2tol, -1);
            var param2 = parameterConfig(inputqueries, ionmode, 1.25F, ms1tol + 0.0025F, ms2tol, -1);
            var testQueries = getTestQueries(inputspec, param.IsIonMobility, ionmode);
            var lbmQuery = param.LipidQueryBean;
            var mspQueries = LbmFileParcer.ReadSerializedObjectLibrary(inputref, lbmQuery.LbmQueries, ionmode, lbmQuery.SolventType, lbmQuery.CollisionType);
            var projectProp = new ProjectPropertyBean() {
                DataType = DataType.Centroid, DataTypeMS2 = DataType.Centroid, IonMode = ionmode,
                IsLabPrivateVersion = true, MethodType = MethodType.ddMSMS, TargetOmics = TargetOmics.Lipidomics
            };

            testQueries = testQueries.OrderBy(n => n.peakAreaBean.AccurateMass).ToList();
            mspQueries = mspQueries.OrderBy(n => n.PrecursorMz).ToList();
            var counter = 0;
            foreach (var query in mspQueries) {
                query.Id = counter;
                counter++;
            }

            Console.WriteLine("Start calculations..");
            var results = new List<TestResult>();
            foreach (var rttol in rttols) {
                param = parameterConfig(inputqueries, ionmode, rttol, ms1tol, ms2tol, -1);
                int tp = 0, tn = 0, fp = 0, fn = 0;
                int total = testQueries.Count;
                var ms1Spectra = new ObservableCollection<double[]>();
                var unmatchedlist = new List<string[]>();
                for (int i = 0; i < testQueries.Count; i++) {
                    var testQ = testQueries[i];
                    var ms2Spectra = Identification.getNormalizedMs2Spectra(testQ.ms2DecResult.MassSpectra, param);
                    if (param.IsIonMobility) {
                        Identification.similarityCalculationsIonMobilityMsMsIncluded(testQ.driftSpotBean, testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                    }
                    else {
                        Identification.similarityCalculationsMsMsIncluded(testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                    }

                    var metname = param.IsIonMobility ? testQ.driftSpotBean.MetaboliteName : testQ.peakAreaBean.MetaboliteName;
                    if (metname.Contains("RIKEN") || metname.Contains("w/o MS2:") || metname == null || metname == string.Empty) metname = "Unknown";
                    var refID = param.IsIonMobility ? testQ.driftSpotBean.LibraryID : testQ.peakAreaBean.LibraryID;

                    var ontology = refID >= 0 ? mspQueries[refID].CompoundClass : "null";
                    var adduct = refID >= 0 ? mspQueries[refID].AdductIonBean.AdductIonName : "null";
                    var refRt = refID >= 0 ? mspQueries[refID].RetentionTime.ToString() : "null";
                    var refCCS = refID >= 0 ? mspQueries[refID].CollisionCrossSection.ToString() : "null";
                    if (ontology == "FA" || ontology == "NAE" || ontology == "BileAcid") metname = "Unknown";
                    if (metname != "Unknown") {
                        var annotation = LipidomicsConverter.LipidomicsAnnotationLevel(metname, ontology, adduct);
                        if (annotation != "Chain resolved" && annotation != "Chain semi-resolved") {
                            metname = "Unknown";
                            ontology = "null";
                        }
                    }
                    if (adduct == "[M+Na]+") metname = "Unknown";
                    if (metname.Contains("(d")) metname = "Unknown";

                    if (metname != "Unknown" && metname == testQ.NAME) {
                        tp++;
                    }
                    else if ((ontology == "TAG" || ontology == "EtherTAG") && metname.Split(';')[0] == testQ.NAME.Split(';')[0]) {
                        tp++;
                    }
                    else if (metname == "Unknown" && testQ.NAME == "Unknown") {
                        tn++;
                    }
                    else {
                        if (metname == "Unknown") {
                            var error = new string[] { "False negative", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS, testQ.PRECURSORMZ, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                            unmatchedlist.Add(error);
                            fn++;
                        }
                        else {
                            var error = new string[] { "False positive", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS, testQ.PRECURSORMZ, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                            unmatchedlist.Add(error);
                            fp++;
                        }
                    }

                    Console.Write("{0} / {1} at {2}", i + 1, total, rttol);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }

                var accuracy = Math.Round((double)(tp + tn) / (double)(tp + fp + tn + fn) * 100, 2);
                var precision = Math.Round((double)tp / (double)(tp + fp) * 100, 2);
                var recall = Math.Round((double)tp / (double)(tp + fn) * 100, 2);
                var specificity = Math.Round((double)tn / (double)(fp + tn) * 100, 2);
                var fscore = (double)(recall * 2 * precision) / (double)(recall + precision);
                var fdr = Math.Round((double)fp / (double)(fp + tp) * 100, 2);

                var testresult = new TestResult() {
                    RefRt = rttol, RefCCS = -1,
                    TruePositive = tp, TrueNegative = tn, FalsePositive = fp, FalseNegative = fn,
                    Accuracy = (float)accuracy, Precision = (float)precision, Recall = (float)recall, 
                    Specificity = (float)specificity, FMeasure = (float)fscore, FDR = (float)fdr
                };
                results.Add(testresult);
                Console.WriteLine();
            }

            var header = new List<string>() { "RefRT", "RefCcs", "TP", "TN", "FP", "FN", "FDR", "Accuracy", "Precision", "Recall", "Specificity", "F-Measure" };
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine(String.Join(",", header));
                foreach (var result in results) {
                    var resultString = new List<string>() { result.RefRt.ToString(), result.RefCCS.ToString(), result.TruePositive.ToString(),
                    result.TrueNegative.ToString(),  result.FalsePositive.ToString(), result.FalseNegative.ToString(), result.FDR.ToString(),
                     result.Accuracy.ToString(), result.Precision.ToString(), result.Recall.ToString(), result.Specificity.ToString(), result.FMeasure.ToString() };
                    sw.WriteLine(String.Join(",", resultString));
                }
            }
        }

        public static void FalseDiscoveryRateCalculationRtDependencyVS2(string inputspec, string inputref, string inputqueries, IonMode ionmode,
           List<float> rttols, float ms1tol, float ms2tol, string output) {
            Console.WriteLine("Importing libraries");

            var param = parameterConfig(inputqueries, ionmode, 1.25F, ms1tol, ms2tol, -1);
            var param2 = parameterConfig(inputqueries, ionmode, 1.25F, ms1tol + 0.0025F, ms2tol, -1);
            var testQueries = getTestQueries(inputspec, param.IsIonMobility, ionmode);
            var lbmQuery = param.LipidQueryBean;
            var mspQueries = LbmFileParcer.ReadSerializedObjectLibrary(inputref, lbmQuery.LbmQueries, ionmode, lbmQuery.SolventType, lbmQuery.CollisionType);
            var projectProp = new ProjectPropertyBean() {
                DataType = DataType.Centroid, DataTypeMS2 = DataType.Centroid, IonMode = ionmode,
                IsLabPrivateVersion = true, MethodType = MethodType.ddMSMS, TargetOmics = TargetOmics.Lipidomics
            };

            testQueries = testQueries.OrderBy(n => n.peakAreaBean.AccurateMass).ToList();
            mspQueries = mspQueries.OrderBy(n => n.PrecursorMz).ToList();
            var counter = 0;
            foreach (var query in mspQueries) {
                query.Id = counter;
                counter++;
            }

            Console.WriteLine("Start calculations..");
            var results = new List<TestResult>();
            foreach (var rttol in rttols) {
                param = parameterConfig(inputqueries, ionmode, rttol, ms1tol, ms2tol, -1);
                param2 = parameterConfig(inputqueries, ionmode, rttol, ms1tol + 0.0025F, ms2tol, -1);
                int tp = 0, tn = 0, fp = 0, fn = 0;
                int total = testQueries.Count;
                var ms1Spectra = new ObservableCollection<double[]>();
                var unmatchedlist = new List<string[]>();
                for (int i = 0; i < testQueries.Count; i++) {
                    var testQ = testQueries[i];
                    var ms2Spectra = Identification.getNormalizedMs2Spectra(testQ.ms2DecResult.MassSpectra, param);
                    List<MatchedCandidate> candidates = null;
                    if (param.IsIonMobility) {
                        candidates = Identification.GetImMsMsMatchedCandidatesTest(testQ.driftSpotBean, testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                    }
                    else {
                        candidates = Identification.GetMsMsMatchedCandidatesTest(testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                    }

                    if (candidates == null || candidates.Count == 0) {
                        if (testQ.NAME == "Unknown") {
                            tn++;
                            Console.Write("{0} / {1} at {2}", i + 1, total, rttol);
                            Console.SetCursorPosition(0, Console.CursorTop);
                            continue;
                        }
                        else {
                            if (param.IsIonMobility) {
                                candidates = Identification.GetImMsMsMatchedCandidatesTest(testQ.driftSpotBean, testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param2, projectProp);
                            }
                            else {
                                candidates = Identification.GetMsMsMatchedCandidatesTest(testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param2, projectProp);
                            }
                            if (candidates == null || candidates.Count == 0) {
                                var error = new string[] { "False negative", "Unknown", testQ.NAME, testQ.RETENTIONTIME, "null", testQ.CCS, "null",
                            testQ.PRECURSORMZ, "null", testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                                unmatchedlist.Add(error);
                                fn++;
                                Console.Write("{0} / {1} at {2}", i + 1, total, rttol);
                                Console.SetCursorPosition(0, Console.CursorTop);
                                continue;
                            }
                        }
                    }

                    var isMatchedNameExist = false;
                    var matchedId = -1;
                    for (int j = 0; j < candidates.Count; j++) {
                        var localMetName = candidates[j].AnnotatedName;
                        if (localMetName.Contains('|')) {
                            localMetName = localMetName.Split('|')[localMetName.Split('|').Length - 1];
                        }
                        if (localMetName == testQ.NAME) {
                            isMatchedNameExist = true;
                            matchedId = j;
                            break;
                        }
                    }

                    var metname = candidates[0].AnnotatedName;
                    if (metname.Contains("RIKEN") || metname.Contains("w/o MS2:") || metname == null || metname == string.Empty) metname = "Unknown";
                    var refID = candidates[0].MspID;

                    var ontology = refID >= 0 ? mspQueries[refID].CompoundClass : "null";
                    var adduct = refID >= 0 ? mspQueries[refID].AdductIonBean.AdductIonName : "null";
                    var refRt = refID >= 0 ? mspQueries[refID].RetentionTime.ToString() : "null";
                    var refCCS = refID >= 0 ? mspQueries[refID].CollisionCrossSection.ToString() : "null";
                    var refMz = refID >= 0 ? mspQueries[refID].PrecursorMz.ToString() : "null";
                    if (metname == "CE 0:0") metname = "Unknown";
                    if (ontology == "FA" || ontology == "NAE" || ontology == "BileAcid") metname = "Unknown";
                    if (metname != "Unknown") {
                        var annotation = LipidomicsConverter.LipidomicsAnnotationLevelVS2(metname, ontology, adduct);
                        if (annotation != "Chain resolved" && annotation != "Chain semi-resolved") {
                            metname = "Unknown";
                            ontology = "null";
                        }
                    }
                    if (adduct == "[M+Na]+") metname = "Unknown";
                    if (metname.Contains("(d")) metname = "Unknown";

                    if (metname != "Unknown" && isMatchedNameExist) {
                        tp++;
                    }
                    else if (metname == "Unknown" && testQ.NAME == "Unknown") {
                        tn++;
                    }
                    else {
                        if (metname == "Unknown") {
                            var error = new string[] { "False negative", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS,
                            testQ.PRECURSORMZ, refMz, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                            unmatchedlist.Add(error);
                            fn++;
                        }
                        else {
                            var error = new string[] { "False positive", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS,
                            testQ.PRECURSORMZ, refMz, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                            unmatchedlist.Add(error);
                            fp++;
                        }
                    }

                    Console.Write("{0} / {1} at {2}", i + 1, total, rttol);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }

                var accuracy = Math.Round((double)(tp + tn) / (double)(tp + fp + tn + fn) * 100, 2);
                var precision = Math.Round((double)tp / (double)(tp + fp) * 100, 2);
                var recall = Math.Round((double)tp / (double)(tp + fn) * 100, 2);
                var specificity = Math.Round((double)tn / (double)(fp + tn) * 100, 2);
                var fscore = (double)(recall * 2 * precision) / (double)(recall + precision);
                var fdr = Math.Round((double)fp / (double)(fp + tp) * 100, 2);

                var testresult = new TestResult() {
                    RefRt = rttol, RefCCS = -1,
                    TruePositive = tp, TrueNegative = tn, FalsePositive = fp, FalseNegative = fn,
                    Accuracy = (float)accuracy, Precision = (float)precision, Recall = (float)recall,
                    Specificity = (float)specificity, FMeasure = (float)fscore, FDR = (float)fdr
                };
                results.Add(testresult);
                Console.WriteLine();
            }

            var header = new List<string>() { "RefRT", "RefCcs", "TP", "TN", "FP", "FN", "FDR", "Accuracy", "Precision", "Recall", "Specificity", "F-Measure" };
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine(String.Join(",", header));
                foreach (var result in results) {
                    var resultString = new List<string>() { result.RefRt.ToString(), result.RefCCS.ToString(), result.TruePositive.ToString(),
                    result.TrueNegative.ToString(),  result.FalsePositive.ToString(), result.FalseNegative.ToString(), result.FDR.ToString(),
                     result.Accuracy.ToString(), result.Precision.ToString(), result.Recall.ToString(), result.Specificity.ToString(), result.FMeasure.ToString() };
                    sw.WriteLine(String.Join(",", resultString));
                }
            }
        }


        public static void FalseDiscoveryRateCalculationCcsDependency(string inputspec, string inputref, string inputqueries, IonMode ionmode,
           List<float> ccstols, float rttol, float ms1tol, float ms2tol, string output) {
            Console.WriteLine("Importing libraries");

            var param = parameterConfig(inputqueries, ionmode, rttol, ms1tol, ms2tol, 10.0F);
            var testQueries = getTestQueries(inputspec, param.IsIonMobility, ionmode);
            var lbmQuery = param.LipidQueryBean;
            var mspQueries = LbmFileParcer.ReadSerializedObjectLibrary(inputref, lbmQuery.LbmQueries, ionmode, lbmQuery.SolventType, lbmQuery.CollisionType);
            var projectProp = new ProjectPropertyBean() {
                DataType = DataType.Centroid, DataTypeMS2 = DataType.Centroid, IonMode = ionmode,
                IsLabPrivateVersion = true, MethodType = MethodType.ddMSMS, TargetOmics = TargetOmics.Lipidomics
            };

            testQueries = testQueries.OrderBy(n => n.peakAreaBean.AccurateMass).ToList();
            mspQueries = mspQueries.OrderBy(n => n.PrecursorMz).ToList();
            var counter = 0;
            foreach (var query in mspQueries) {
                query.Id = counter;
                counter++;
            }

            Console.WriteLine("Start calculations..");
            var results = new List<TestResult>();
            foreach (var ccsTol in ccstols) {
                param = parameterConfig(inputqueries, ionmode, rttol, ms1tol, ms2tol, ccsTol);
                int tp = 0, tn = 0, fp = 0, fn = 0;
                int total = testQueries.Count;
                var ms1Spectra = new ObservableCollection<double[]>();
                var unmatchedlist = new List<string[]>();
                for (int i = 0; i < testQueries.Count; i++) {
                    var testQ = testQueries[i];
                    var ms2Spectra = Identification.getNormalizedMs2Spectra(testQ.ms2DecResult.MassSpectra, param);
                    if (param.IsIonMobility) {
                        Identification.similarityCalculationsIonMobilityMsMsIncluded(testQ.driftSpotBean, testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                    }
                    else {
                        Identification.similarityCalculationsMsMsIncluded(testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                    }

                    var metname = param.IsIonMobility ? testQ.driftSpotBean.MetaboliteName : testQ.peakAreaBean.MetaboliteName;
                    if (metname.Contains("RIKEN") || metname.Contains("w/o MS2:") || metname == null || metname == string.Empty) metname = "Unknown";
                    var refID = param.IsIonMobility ? testQ.driftSpotBean.LibraryID : testQ.peakAreaBean.LibraryID;

                    var ontology = refID >= 0 ? mspQueries[refID].CompoundClass : "null";
                    var adduct = refID >= 0 ? mspQueries[refID].AdductIonBean.AdductIonName : "null";
                    var refRt = refID >= 0 ? mspQueries[refID].RetentionTime.ToString() : "null";
                    var refCCS = refID >= 0 ? mspQueries[refID].CollisionCrossSection.ToString() : "null";
                    if (ontology == "FA" || ontology == "NAE" || ontology == "BileAcid" || ontology == "ACar") metname = "Unknown";
                    if (metname != "Unknown") {
                        var annotation = LipidomicsConverter.LipidomicsAnnotationLevel(metname, ontology, adduct);
                        if (annotation != "Chain resolved" && annotation != "Chain semi-resolved") {
                            metname = "Unknown";
                            ontology = "null";
                        }
                    }
                    if (adduct == "[M+Na]+") metname = "Unknown";
                    if (metname.Contains("(d")) metname = "Unknown";
                    if (metname != "Unknown" && metname == testQ.NAME) {
                        tp++;
                    }
                    else if ((ontology == "TAG" || ontology == "EtherTAG") && metname.Split(';')[0] == testQ.NAME.Split(';')[0]) {
                        tp++;
                    }
                    else if (metname == "Unknown" && testQ.NAME == "Unknown") {
                        tn++;
                    }
                    else {
                        if (metname == "Unknown") {
                            var error = new string[] { "False negative", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS, testQ.PRECURSORMZ, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                            unmatchedlist.Add(error);
                            fn++;
                        }
                        else {
                            var error = new string[] { "False positive", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS, testQ.PRECURSORMZ, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                            unmatchedlist.Add(error);
                            fp++;
                        }
                    }

                    Console.Write("{0} / {1} at {2}", i + 1, total, ccsTol);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }

                var accuracy = Math.Round((double)(tp + tn) / (double)(tp + fp + tn + fn) * 100, 2);
                var precision = Math.Round((double)tp / (double)(tp + fp) * 100, 2);
                var recall = Math.Round((double)tp / (double)(tp + fn) * 100, 2);
                var specificity = Math.Round((double)tn / (double)(fp + tn) * 100, 2);
                var fscore = (double)(recall * 2 * precision) / (double)(recall + precision);
                var fdr = Math.Round((double)fp / (double)(fp + tp) * 100, 2);

                var testresult = new TestResult() {
                    RefRt = rttol, RefCCS = ccsTol,
                    TruePositive = tp, TrueNegative = tn, FalsePositive = fp, FalseNegative = fn,
                    Accuracy = (float)accuracy, Precision = (float)precision, Recall = (float)recall,
                    Specificity = (float)specificity, FMeasure = (float)fscore, FDR = (float)fdr
                };
                results.Add(testresult);
                Console.WriteLine();

            }

            var header = new List<string>() { "RefRT", "RefCcs", "TP", "TN", "FP", "FN", "FDR", "Accuracy", "Precision", "Recall", "Specificity", "F-Measure" };
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine(String.Join(",", header));
                foreach (var result in results) {
                    var resultString = new List<string>() { result.RefRt.ToString(), result.RefCCS.ToString(), result.TruePositive.ToString(),
                    result.TrueNegative.ToString(),  result.FalsePositive.ToString(), result.FalseNegative.ToString(), result.FDR.ToString(),
                     result.Accuracy.ToString(), result.Precision.ToString(), result.Recall.ToString(), result.Specificity.ToString(), result.FMeasure.ToString() };
                    sw.WriteLine(String.Join(",", resultString));
                }
            }
        }

        public static void ExtractInChIKeySmilesPair(string input, string output) {
            var queries = LipidomicsConverter.SerializedObjectToMspQeries(input);
            var inchikeyToSmiles = new Dictionary<string, string>();
            foreach (var query in queries) {
                var inchikey = query.InchiKey;
                var smiles = query.Smiles;
                if (inchikey == null || inchikey == string.Empty) continue;
                if (!inchikeyToSmiles.ContainsKey(inchikey))
                    inchikeyToSmiles[inchikey] = smiles;
            }

            
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine(String.Join(",", new string[] { "InChIKey", "SMILES" }));
                foreach (var pair in inchikeyToSmiles) {
                    sw.WriteLine(String.Join(",", new string[] { pair.Key, pair.Value }));
                }
            }
        }

        public static void FalseDiscoveryRateCalculationCcsDependencyVS2(string inputspec, string inputref, string inputqueries, IonMode ionmode,
           List<float> ccstols, float rttol, float ms1tol, float ms2tol, string output) {
            Console.WriteLine("Importing libraries");

            var param = parameterConfig(inputqueries, ionmode, rttol, ms1tol, ms2tol, 10.0F);
            var param2 = parameterConfig(inputqueries, ionmode, rttol, ms1tol + 0.0025F, ms2tol, 10.0F);
            var testQueries = getTestQueries(inputspec, param.IsIonMobility, ionmode);
            var lbmQuery = param.LipidQueryBean;
            var mspQueries = LbmFileParcer.ReadSerializedObjectLibrary(inputref, lbmQuery.LbmQueries, ionmode, lbmQuery.SolventType, lbmQuery.CollisionType);
            var projectProp = new ProjectPropertyBean() {
                DataType = DataType.Centroid, DataTypeMS2 = DataType.Centroid, IonMode = ionmode,
                IsLabPrivateVersion = true, MethodType = MethodType.ddMSMS, TargetOmics = TargetOmics.Lipidomics
            };

            testQueries = testQueries.OrderBy(n => n.peakAreaBean.AccurateMass).ToList();
            mspQueries = mspQueries.OrderBy(n => n.PrecursorMz).ToList();
            var counter = 0;
            foreach (var query in mspQueries) {
                query.Id = counter;
                counter++;
            }

            Console.WriteLine("Start calculations..");
            var results = new List<TestResult>();
            foreach (var ccsTol in ccstols) {
                param = parameterConfig(inputqueries, ionmode, rttol, ms1tol, ms2tol, ccsTol);
                param2 = parameterConfig(inputqueries, ionmode, rttol, ms1tol + 0.0025F, ms2tol, ccsTol);
                int tp = 0, tn = 0, fp = 0, fn = 0;
                int total = testQueries.Count;
                var ms1Spectra = new ObservableCollection<double[]>();
                var unmatchedlist = new List<string[]>();
                for (int i = 0; i < testQueries.Count; i++) {
                    var testQ = testQueries[i];
                    var ms2Spectra = Identification.getNormalizedMs2Spectra(testQ.ms2DecResult.MassSpectra, param);
                    List<MatchedCandidate> candidates = null;
                    if (param.IsIonMobility) {
                        candidates = Identification.GetImMsMsMatchedCandidatesTest(testQ.driftSpotBean, testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                    }
                    else {
                        candidates = Identification.GetMsMsMatchedCandidatesTest(testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param, projectProp);
                    }

                    if (candidates == null || candidates.Count == 0) {
                        if (testQ.NAME == "Unknown") {
                            tn++;
                            Console.Write("{0} / {1} at {2}", i + 1, total, ccsTol);
                            Console.SetCursorPosition(0, Console.CursorTop);
                            continue;
                        }
                        else {
                            if (param.IsIonMobility) {
                                candidates = Identification.GetImMsMsMatchedCandidatesTest(testQ.driftSpotBean, testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param2, projectProp);
                            }
                            else {
                                candidates = Identification.GetMsMsMatchedCandidatesTest(testQ.peakAreaBean, ms1Spectra, ms2Spectra, mspQueries, param2, projectProp);
                            }
                            if (candidates == null || candidates.Count == 0) {
                                var error = new string[] { "False negative", "Unknown", testQ.NAME, testQ.RETENTIONTIME, "null", testQ.CCS, "null",
                            testQ.PRECURSORMZ, "null", testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                                unmatchedlist.Add(error);
                                fn++;
                                Console.Write("{0} / {1} at {2}", i + 1, total, ccsTol);
                                Console.SetCursorPosition(0, Console.CursorTop);
                                continue;
                            }
                        }
                    }

                    var isMatchedNameExist = false;
                    var matchedId = -1;
                    for (int j = 0; j < candidates.Count; j++) {
                        var localMetName = candidates[j].AnnotatedName;
                        if (localMetName.Contains('|')) {
                            localMetName = localMetName.Split('|')[localMetName.Split('|').Length - 1];
                        }
                        if (localMetName == testQ.NAME) {
                            isMatchedNameExist = true;
                            matchedId = j;
                            break;
                        }
                    }

                    var metname = candidates[0].AnnotatedName;
                    if (metname.Contains("RIKEN") || metname.Contains("w/o MS2:") || metname == null || metname == string.Empty) metname = "Unknown";
                    var refID = candidates[0].MspID;

                    var ontology = refID >= 0 ? mspQueries[refID].CompoundClass : "null";
                    var adduct = refID >= 0 ? mspQueries[refID].AdductIonBean.AdductIonName : "null";
                    var refRt = refID >= 0 ? mspQueries[refID].RetentionTime.ToString() : "null";
                    var refCCS = refID >= 0 ? mspQueries[refID].CollisionCrossSection.ToString() : "null";
                    var refMz = refID >= 0 ? mspQueries[refID].PrecursorMz.ToString() : "null";
                    if (ontology == "FA" || ontology == "NAE" || ontology == "BileAcid") metname = "Unknown";
                    if (metname != "Unknown") {
                        var annotation = LipidomicsConverter.LipidomicsAnnotationLevelVS2(metname, ontology, adduct);
                        if (annotation != "Chain resolved" && annotation != "Chain semi-resolved") {
                            metname = "Unknown";
                            ontology = "null";
                        }
                    }
                    if (adduct == "[M+Na]+") metname = "Unknown";
                    if (metname.Contains("(d")) metname = "Unknown";
                    if (metname != "Unknown" && isMatchedNameExist) {
                        tp++;
                    }
                    else if (metname == "Unknown" && testQ.NAME == "Unknown") {
                        tn++;
                    }
                    else {
                        if (metname == "Unknown") {
                            var error = new string[] { "False negative", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS,
                            testQ.PRECURSORMZ, refMz, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                            unmatchedlist.Add(error);
                            fn++;
                        }
                        else {
                            var error = new string[] { "False positive", metname, testQ.NAME, testQ.RETENTIONTIME, refRt, testQ.CCS, refCCS,
                            testQ.PRECURSORMZ, refMz, testQ.PRECURSORTYPE, testQ.FORMULA, testQ.ONTOLOGY, testQ.SMILES, testQ.INCHIKEY, testQ.COMMENT };
                            unmatchedlist.Add(error);
                            fp++;
                        }
                    }

                    Console.Write("{0} / {1} at {2}", i + 1, total, ccsTol);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }

                var accuracy = Math.Round((double)(tp + tn) / (double)(tp + fp + tn + fn) * 100, 2);
                var precision = Math.Round((double)tp / (double)(tp + fp) * 100, 2);
                var recall = Math.Round((double)tp / (double)(tp + fn) * 100, 2);
                var specificity = Math.Round((double)tn / (double)(fp + tn) * 100, 2);
                var fscore = (double)(recall * 2 * precision) / (double)(recall + precision);
                var fdr = Math.Round((double)fp / (double)(fp + tp) * 100, 2);

                var testresult = new TestResult() {
                    RefRt = rttol, RefCCS = ccsTol,
                    TruePositive = tp, TrueNegative = tn, FalsePositive = fp, FalseNegative = fn,
                    Accuracy = (float)accuracy, Precision = (float)precision, Recall = (float)recall,
                    Specificity = (float)specificity, FMeasure = (float)fscore, FDR = (float)fdr
                };
                results.Add(testresult);
                Console.WriteLine();

            }

            var header = new List<string>() { "RefRT", "RefCcs", "TP", "TN", "FP", "FN", "FDR", "Accuracy", "Precision", "Recall", "Specificity", "F-Measure" };
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine(String.Join(",", header));
                foreach (var result in results) {
                    var resultString = new List<string>() { result.RefRt.ToString(), result.RefCCS.ToString(), result.TruePositive.ToString(),
                    result.TrueNegative.ToString(),  result.FalsePositive.ToString(), result.FalseNegative.ToString(), result.FDR.ToString(),
                     result.Accuracy.ToString(), result.Precision.ToString(), result.Recall.ToString(), result.Specificity.ToString(), result.FMeasure.ToString() };
                    sw.WriteLine(String.Join(",", resultString));
                }
            }
        }

        public static void ConvertUnrealizedLipidsToUnknownsInSpectralKit(string input, string output, bool isIonMobility, IonMode ionmode) {

            var dda_neg_exlipids = new List<string>() {
                //"HexCer-NS d33:1; HexCer-NS d17:1/16:0; [M+CH3COO]-",
                //"LPE 14:1; [M-H]-",
                //"LPG 14:1; [M-H]-",
                //"LPG 17:0; [M-H]-",
                //"PG 31:1; PG 17:0-14:1; [M-H]-",
                //"PG 31:1; PG 17:0-14:1; [M-H]-",
                //"PI 36:3; PI 18:0-18:3; [M-H]-",
                //"PI 36:3; PI 18:0-18:3; [M-H]-",
                //"HexCer-EOS d67:3; HexCer-EOS d49:1-O-18:2; [M+CH3COO]-",
                //"HexCer-EOS d68:3; HexCer-EOS d50:1-O-18:2; [M+CH3COO]-",
                //"HexCer-EOS d69:4; HexCer-EOS d51:2-O-18:2; [M+CH3COO]-",
                //"HexCer-EOS d70:3; HexCer-EOS d52:1-O-18:2; [M+CH3COO]-",
                //"HexCer-EOS d70:4; HexCer-EOS d52:2-O-18:2; [M+CH3COO]-",
                //"HexCer-EOS d71:4; HexCer-EOS d53:2-O-18:2; [M+CH3COO]-",
                //"HexCer-EOS d72:4; HexCer-EOS d54:2-O-18:2; [M+CH3COO]-"
            };

            var dda_pos_exlipids = new List<string>() {
                "PE 36:1p; PE 14:-1p/22:2; [M+H]+",
                //"DAG 35:1e; DAG 16:0e/19:1; [M+NH4]+",
                //"DAG 38:1e; DAG 16:0e/22:1; [M+NH4]+",
                //"DAG 40:1e; DAG 16:0e/24:1; [M+NH4]+",
                //"HexCer-NS d32:1; HexCer-NS d18:1/14:0; [M+H]+",
                //"LPE 14:1; [M+H]+",
                "SM d32:1; SM d12:0/20:1; [M+H]+",
                //"TAG 50:2e; TAG 18:1e-14:0-18:1; [M+NH4]+"
            };

            var pasef_pos_exlipids = new List<string>() {
                "Cer-EOS d44:2; Cer-EOS d18:1/26:1; [M+H]+",
                "Cer-EOS d46:2; Cer-EOS d18:1/28:1; [M+H]+",
                //"Cer-EOS d46:3; Cer-EOS d18:1/28:2; [M+H]+",
                //"Cer-NS d52:7; Cer-NS d18:1/34:6; [M+H]+",
                //"DAG 31:1e; DAG 15:1e/16:0; [M+NH4]+",
                "DAG 37:4; DAG 19:0-18:4; [M+NH4]+",
                //"DAG 46:6; DAG 18:1-28:5; [M+NH4]+",
                "HexCer-AP t44:0+O; HexCer-AP t24:0/20:0+O; [M+H]+",
                "HexCer-AP t45:0+O; HexCer-AP t14:0/31:0+O; [M+H]+",
                "NAAG 33:0; NAAG 15:0/18:0; [M+H]+",
                "NAAG 34:1; NAAG 19:0/15:1; [M+H]+",
                "PC 34:5; PC 16:1-18:4; [M+H]+",
                //"PC 39:7; PC 19:2-20:5; [M+H]+",
                //"PC 40:6; PC 18:2-22:4; [M+H]+",
                //"PC 40:8; PC 18:3-22:5; [M+H]+",
                //"PC 42:9; PC 18:3-24:6; [M+H]+",
                //"PC 43:3; PC 26:1-17:2; [M+H]+",
                //"PC 46:5; PC 20:2-26:3; [M+H]+",
                //"PE 28:0; PE 13:0-15:0; [M+H]+",
                //"PE 36:5; PE 14:1-22:4; [M+H]+",
                "PE 36:5p; PE 20:4p/16:1; [M+H]+",
                //"PE 37:1p; PE 16:0p/21:1; [M+H]+",
                //"PE 38:5p; PE 18:3p/20:2; [M+H]+",
                //"PE 40:5; PE 20:1-20:4; [M+H]+",
                "PE 40:8; PE 16:3-24:5; [M+H]+",
                //"PE 41:5p; PE 21:1p/20:4; [M+H]+",
                //"PE 42:5p; PE 20:2p/22:3; [M+H]+",
                "PE 42:9; PE 18:4-24:5; [M+H]+",
                //"PE 44:5p; PE 20:1p/24:4; [M+H]+",
                "PE 44:6p; PE 28:5p/16:1; [M+H]+",
                //"PE 46:5p; PE 24:1p/22:4; [M+H]+",
                //"PS 41:1; PS 24:0-17:1; [M+H]+",
                //"SHexCer d32:0+O; SHexCer d16:0/16:0+O; [M+H]+",
                "SHexCer d36:3+O; SHexCer d22:3/14:0+O; [M+H]+",
                "SHexCer d38:2; SHexCer d24:1/14:1; [M+H]+",
                //"SISE 14:0; [M+NH4]+",
                //"SM d36:2; SM d14:1/22:1; [M+H]+",
                //"SM d42:5; SM d18:2/24:3; [M+H]+",
                //"SM d44:3; SM d20:1/24:2; [M+H]+",
                //"SM d44:4; SM d18:0/26:4; [M+H]+",
                //"TAG 42:3; TAG 8:0-18:1-16:2; [M+NH4]+",
                //"TAG 46:2; TAG 14:0-16:0-16:2; [M+NH4]+",
                //"TAG 46:2; TAG 14:0-16:0-16:2; [M+NH4]+",
                //"TAG 47:4; TAG 14:0-15:1-18:3; [M+NH4]+",
                //"TAG 51:4; TAG 19:0-14:1-18:3; [M+NH4]+",
                //"TAG 52:10; TAG 16:3-18:3-18:4; [M+NH4]+",
                //"TAG 52:6; TAG 14:0-16:1-22:5; [M+NH4]+",
                "TAG 52:7; TAG 16:1-18:1-18:5; [M+NH4]+",
                //"TAG 54:6; TAG 9:0-19:2-26:4; [M+NH4]+",
                "TAG 55:10; TAG 17:0-18:5-20:5; [M+NH4]+",
                //"TAG 55:2; TAG 16:0-21:0-18:2; [M+NH4]+",
                "TAG 55:8; TAG 19:0-18:3-18:5; [M+NH4]+",
                //"TAG 56:7; TAG 9:0-17:0-30:7; [M+NH4]+",
                //"BRSE 19:0; [M+NH4]+",
                //"CE 30:1; [M+NH4]+",
                //"Cer-AP t41:1+O; Cer-AP t18:0/23:1+O; [M+H]+",
                //"Cer-AP t42:2+O; Cer-AP t18:1/24:1+O; [M+H]+",
                //"Cer-AP t44:0+O; Cer-AP t20:0/24:0+O; [M+H]+",
                //"Cer-AP t45:1+O; Cer-AP t17:0/28:1+O; [M+H]+",
                //"Cer-AP t46:0+O; Cer-AP t22:0/24:0+O; [M+H]+",
                //"Cer-NS d46:2; Cer-NS d20:2/26:0; [M+H]+",
                //"DAG 38:1e; DAG 17:0e/21:1; [M+NH4]+",
                //"DAG 42:3e; DAG 20:1e/22:2; [M+NH4]+",
                //"DAG 42:4; DAG 22:1-20:3; [M+NH4]+",
                //"HBMP 54:2; HBMP 16:0-16:0-22:2; [M+NH4]+",
                //"HexCer-AP t33:1+O; HexCer-AP t20:1/13:0+O; [M+H]+",
                //"HexCer-AP t33:2+O; HexCer-AP t20:2/13:0+O; [M+H]+",
                //"HexCer-HDS d42:0+O; HexCer-HDS d22:0/20:0+O; [M+H]+",
                //"LPE 23:0e; [M+H]+",
                //"PE 36:0e; PE 20:0e/16:0; [M+H]+",
                //"PE 42:2p; PE 18:1p/24:1; [M+H]+",
                //"PS 38:0; PS 19:0-19:0; [M+H]+",
                //"PS 40:2; PS 22:0-18:2; [M+H]+",
                //"PS 42:3; PS 18:1-24:2; [M+H]+",
                //"PS 42:7; PS 20:1-22:6; [M+H]+",
                //"TAG 36:1; TAG 8:0-10:0-18:1; [M+NH4]+",
                //"TAG 37:1; TAG 9:0-14:0-14:1; [M+NH4]+",
                //"TAG 42:0; TAG 13:0-14:0-15:0; [M+NH4]+",
                //"TAG 43:1; TAG 13:0-14:0-16:1; [M+NH4]+",
                //"TAG 44:4; TAG 8:0-16:0-20:4; [M+NH4]+",
                //"TAG 48:7; TAG 8:0-18:1-22:6; [M+NH4]+",
                //"TAG 50:6; TAG 16:0-16:2-18:4; [M+NH4]+",
                //"TAG 52:9; TAG 16:2-18:3-18:4; [M+NH4]+",
                //"TAG 54:4; TAG 16:1-18:1-20:2; [M+NH4]+",
                //"TAG 55:10; TAG 9:0-18:3-28:7; [M+NH4]+",
                //"TAG 55:6; TAG 18:1-19:2-18:3; [M+NH4]+",
                "TAG 55:8; TAG 18:1-19:2-18:5; [M+NH4]+",
                //"TAG 55:9; TAG 16:1-17:2-22:6; [M+NH4]+",
                //"TAG 56:0; TAG 14:0-18:0-24:0; [M+NH4]+",
                //"TAG 56:1e; TAG 20:0e-16:0-20:1; [M+NH4]+",
                //"TAG 56:4e; TAG 18:1e-20:1-18:2; [M+NH4]+",
                //"TAG 57:10; TAG 9:0-18:2-30:8; [M+NH4]+",
                //"TAG 57:6; TAG 18:1-19:1-20:4; [M+NH4]+",
                //"TAG 57:9; TAG 16:1-19:2-22:6; [M+NH4]+",
                //"TAG 57:9; TAG 18:2-19:2-20:5; [M+NH4]+",
                //"TAG 58:0; TAG 16:0-16:0-26:0; [M+NH4]+",
                //"TAG 58:5; TAG 16:1-20:2-22:2; [M+NH4]+",
                //"TAG 58:6; TAG 18:1-22:2-18:3; [M+NH4]+",
                //"TAG 59:2; TAG 22:0-18:1-19:1; [M+NH4]+",
                //"TAG 60:0; TAG 18:0-20:0-22:0; [M+NH4]+",
                //"TAG 60:1e; TAG 20:0e-20:0-20:1; [M+NH4]+",
                //"TAG 60:9; TAG 16:0-22:4-22:5; [M+NH4]+",
                //"TAG 60:9; TAG 18:1-20:3-22:5; [M+NH4]+",
                //"TAG 62:0; TAG 15:0-22:0-25:0; [M+NH4]+",
                //"TAG 62:10; TAG 20:2-20:2-22:6; [M+NH4]+",
                //"TAG 62:2e; TAG 20:0e-20:1-22:1; [M+NH4]+",
                //"TAG 62:7; TAG 16:1-24:1-22:5; [M+NH4]+",
                //"TAG 62:7e; TAG 18:0e-22:3-22:4; [M+NH4]+",
                //"TAG 63:2; TAG 25:0-18:1-20:1; [M+NH4]+",
                //"TAG 64:5; TAG 20:1-24:1-20:3; [M+NH4]+",
                //"TAG 66:11; TAG 16:0-22:5-28:6; [M+NH4]+",
                //"TAG 66:8; TAG 22:1-22:1-22:6; [M+NH4]+",
                //"TAG 68:14; TAG 24:4-22:5-22:5; [M+NH4]+"
            };

            var pasef_neg_exlipids = new List<string>() {
                //"AHexCer d58:2+O; AHexCer 16:0/d18:1/24:1+O; [M+CH3COO]-",
                //"AHexSIS 18:3; [M+CH3COO]-",
                //"Cer-ADS d44:1+O; Cer-ADS d28:0/16:1+O; [M-H]-",
                //"Cer-AP t43:1+O; Cer-AP t24:1/19:0+O; [M-H]-",
                //"Cer-AP t44:1+O; Cer-AP t24:1/20:0+O; [M+CH3COO]-",
                //"Cer-AP t44:1+O; Cer-AP t25:1/19:0+O; [M-H]-",
                //"Cer-AP t45:0+O; Cer-AP t28:0/17:0+O; [M-H]-",
                //"Cer-HDS d34:1+O; Cer-HDS d19:0/15:1+O; [M-H]-",
                //"Cer-EODS d48:0; Cer-EODS d19:0/14:0-O-15:0; [M-H]-",
                //"Cer-EODS d58:0; Cer-EODS d42:0-O-16:0; [M-H]-",
                //"Cer-EODS d59:0; Cer-EODS d41:0-O-18:0; [M-H]-",
                //"Cer-EODS d59:0; Cer-EODS d43:0-O-16:0; [M-H]-",
                //"Cer-EODS d60:0; Cer-EODS d42:0-O-18:0; [M-H]-",
                //"Cer-EODS d61:0; Cer-EODS d43:0-O-18:0; [M-H]-",
                //"Cer-EOS d51:2; Cer-EOS d33:1-O-18:1; [M+CH3COO]-",
                //"Cer-EOS d58:1; Cer-EOS d42:1-O-16:0; [M-H]-",
                //"Cer-EOS d58:1; Cer-EOS d42:1-O-16:0; [M-H]-",
                //"Cer-EOS d60:1; Cer-EOS d42:1-O-18:0; [M-H]-",
                //"Cer-EOS d60:1; Cer-EOS d42:1-O-18:0; [M-H]-",
                //"Cer-HS d41:3+O; Cer-HS d20:2/21:1+O; [M+CH3COO]-",
                //"Cer-BDS d40:0+O; Cer-BDS d16:0/24:0+O; [M-H]-",
                //"CL 66:3; CL 32:0-34:3; [M-H]-",
                //"CL 74:10; CL 36:5-38:5; [M-H]-",
                //"DGGA 36:5; DGGA 18:2-18:3; [M-H]-",
                //"FAHFA 37:0; FAHFA 16:0/21:0; [M-H]-",
                //"HBMP 42:0; HBMP 14:0-14:0-14:0; [M-H]-",
                //"HexCer-HS d42:1+O; HexCer-HS d19:1/23:0+O; [M+CH3COO]-",
                //"HexCer-HS d43:2+O; HexCer-HS d21:1/22:1+O; [M+CH3COO]-",
                //"LNAPS 34:0; LNAPS 19:0/n-15:0; [M-H]-",
                //"LNAPS 34:0; LNAPS 19:0/n-15:0; [M-H]-",
                //"LPA 20:5; [M-H]-",
                //"MGDG 32:1e; MGDG 17:1e/15:0; [M+CH3COO]-",
                //"MGDG 33:1e; MGDG 16:1e/17:0; [M+CH3COO]-",
                //"MGDG 34:3e; MGDG 16:1e/18:2; [M+CH3COO]-",
                "MGDG 34:5e; MGDG 20:5e/14:0; [M+CH3COO]-",
                //"MGDG 35:3e; MGDG 18:3e/17:0; [M+CH3COO]-",
                //"MGDG 39:4; MGDG 19:0-20:4; [M+CH3COO]-",
                //"NAAG 35:0; NAAG 20:0/15:0; [M-H]-",
                //"NAAG 39:0; NAAG 22:0/17:0; [M-H]-",
                //"NAAG 40:0; NAAG 18:0/22:0; [M-H]-",
                //"NAAG 43:0; NAAG 18:0/25:0; [M-H]-",
                //"NAAG 43:0; NAAG 19:0/24:0; [M-H]-",
                //"OxPS 38:5+3O; OxPS 18:1-20:4+3O; [M-H]-",
                //"PC 34:3; PC 14:0-20:3; [M+CH3COO]-",
                //"PE 31:1; PE 14:0-17:1; [M-H]-",
                //"PE 35:2; PE 15:1-20:1; [M-H]-",
                //"PE 35:3; PE 18:1-17:2; [M-H]-",
                //"PEtOH 30:0; PEtOH 15:0-15:0; [M-H]-",
                //"PEtOH 31:0; PEtOH 15:0-16:0; [M-H]-",
                //"PEtOH 32:1; PEtOH 14:0-18:1; [M-H]-",
                //"PEtOH 35:4; PEtOH 15:0-20:4; [M-H]-",
                //"PEtOH 40:0; PEtOH 20:0-20:0; [M-H]-",
                //"PEtOH 42:3; PEtOH 24:1-18:2; [M-H]-",
                //"PEtOH 44:10; PEtOH 22:5-22:5; [M-H]-",
                //"PEtOH 44:2; PEtOH 22:1-22:1; [M-H]-",
                //"PG 35:2e; PG 16:2e/19:0; [M-H]-",
                //"PG 36:4; PG 18:1-18:3; [M-H]-",
                //"PG 37:5e; PG 17:2e/20:3; [M-H]-",
                //"PG 40:1e; PG 20:1e/20:0; [M-H]-",
                //"PMeOH 34:0; PMeOH 16:0-18:0; [M-H]-",
                //"PMeOH 40:1; PMeOH 20:0-20:1; [M-H]-"
            };

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                using (StreamReader sr = new StreamReader(input, Encoding.ASCII)) {
                    sw.WriteLine(sr.ReadLine());
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        var lineArray = line.Split('\t');
                        var metname = lineArray[0];
                        var rt = float.Parse(lineArray[1]);
                        var ccs = isIonMobility ? float.Parse(lineArray[2]) : -1.0F;
                        var mz = isIonMobility ? float.Parse(lineArray[3]) : float.Parse(lineArray[2]);
                        var adduct = isIonMobility ? lineArray[4] : lineArray[3];
                        var ionmodestring = isIonMobility ? lineArray[5] : lineArray[4];
                        var formula = isIonMobility ? lineArray[6] : lineArray[5];
                        var ontology = isIonMobility ? lineArray[7] : lineArray[6];
                        var inchikey = isIonMobility ? lineArray[8] : lineArray[7];
                        var smiles = isIonMobility ? lineArray[9] : lineArray[8];
                        var comment = isIonMobility ? lineArray[10] : lineArray[9];
                        var numpeaks = isIonMobility ? lineArray[11] : lineArray[10];
                        var specString = isIonMobility ? lineArray[12] : lineArray[11];
                        if (rt > 16.5) continue;
                        var replacedname = metname;
                        //if (ionmode == IonMode.Positive && isIonMobility) {
                        //    foreach (var exlipid in pasef_pos_exlipids) {
                        //        if (metname == exlipid) {
                        //            replacedname = "Unknown";
                        //            break;
                        //        }
                        //    }
                        //} 
                        //else if (ionmode == IonMode.Negative && isIonMobility) {
                        //    foreach (var exlipid in pasef_neg_exlipids) {
                        //        if (metname == exlipid) {
                        //            replacedname = "Unknown";
                        //            break;
                        //        }
                        //    }
                        //}
                        //else if(ionmode == IonMode.Positive && !isIonMobility) {
                        //    foreach (var exlipid in dda_pos_exlipids) {
                        //        if (metname == exlipid) {
                        //            replacedname = "Unknown";
                        //            break;
                        //        }
                        //    }
                        //}
                        //else if (ionmode == IonMode.Negative && !isIonMobility) {
                        //    foreach (var exlipid in dda_neg_exlipids) {
                        //        if (metname == exlipid) {
                        //            replacedname = "Unknown";
                        //            break;
                        //        }
                        //    }
                        //}
                        //lineArray[0] = replacedname;
                        sw.WriteLine(string.Join("\t", lineArray));
                    }
                }
            }
        }

        private static List<TestQuery> getTestQueries(string inputspec, bool isIonMobility, IonMode ionmode) {
            var queries = new List<TestQuery>();
            using (StreamReader sr = new StreamReader(inputspec, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var lineArray = line.Split('\t');
                    var metname = lineArray[0];
                    var rt = float.Parse(lineArray[1]);
                    var ccs = isIonMobility ? float.Parse(lineArray[2]) : -1.0F;
                    var mz = isIonMobility ? float.Parse(lineArray[3]) : float.Parse(lineArray[2]);
                    var adduct = isIonMobility ? lineArray[4] : lineArray[3];
                    var ionmodestring = isIonMobility ? lineArray[5] : lineArray[4];
                    var formula = isIonMobility ? lineArray[6] : lineArray[5];
                    var ontology = isIonMobility ? lineArray[7] : lineArray[6];
                    var inchikey = isIonMobility ? lineArray[8] : lineArray[7];
                    var smiles = isIonMobility ? lineArray[9] : lineArray[8];
                    var comment = isIonMobility ? lineArray[10] : lineArray[9];
                    var numpeaks = isIonMobility ? lineArray[11] : lineArray[10];
                    var specString = isIonMobility ? lineArray[12] : lineArray[11];
                    var spec = textToSpectrumList(specString, ':', ' ');

                    var testQuery = new TestQuery() {
                        NAME = metname, CCS = ccs.ToString(), COMMENT = comment, FORMULA = formula, INCHIKEY = inchikey, IONMODE = ionmodestring,
                        NumPeaks = numpeaks, ONTOLOGY = ontology, PRECURSORMZ = mz.ToString(), PRECURSORTYPE = adduct, RETENTIONTIME = rt.ToString(), SMILES = smiles, Spectrum = specString,
                        ms2DecResult = new MS2DecResult() {
                            Ms1AccurateMass = mz, MassSpectra = spec, Ms1PeakHeight = 100, PeakTopRetentionTime = rt
                        }, peakAreaBean = new PeakAreaBean() {
                            AccurateMass = mz, AdductIonName = ionmode == IonMode.Positive ? "[M+H]+" : "[M-H]-", ChargeNumber = 1, IsotopeWeightNumber = 0, RtAtPeakTop = rt, Comment = comment,
                        }, driftSpotBean = new DriftSpotBean() {
                            AccurateMass = mz, AdductIonName = ionmode == IonMode.Positive ? "[M+H]+" : "[M-H]-", ChargeNumber = 1, IsotopeWeightNumber = 0, Ccs = ccs, Comment = comment,
                        }
                    };
                    queries.Add(testQuery);
                }
            }
            return queries;
        }

        private static List<double[]> textToSpectrumList(string specText, char mzIntSep, char peakSep, double threshold = 0.0) {
            var peaks = new List<double[]>();

            if (specText == null || specText == string.Empty) return peaks;
            var specArray = specText.Split(peakSep).ToList();
            if (specArray.Count == 0) return peaks;

            foreach (var spec in specArray) {
                var mzInt = spec.Split(mzIntSep).ToArray();
                if (mzInt.Length >= 2) {
                    var mz = mzInt[0];
                    var intensity = mzInt[1];
                    if (double.Parse(intensity) > threshold) {
                        peaks.Add(new double[] { double.Parse(mz), double.Parse(intensity) });
                    }
                }
            }

            return peaks;
        }

        private static AnalysisParametersBean parameterConfig(string inputqueries, IonMode ionmode, float rttol, float ms1tol, float ms2tol, float ccstol) {
            var param = new AnalysisParametersBean() {
                RetentionTimeLibrarySearchTolerance = rttol, Ms1LibrarySearchTolerance = ms1tol, Ms2LibrarySearchTolerance = ms2tol, CcsSearchTolerance = ccstol,
                IsUseRetentionInfoForIdentificationFiltering = true, IsUseCcsForIdentificationScoring = true, IsUseCcsForIdentificationFiltering = true, IsUseRetentionInfoForIdentificationScoring = true, Ms2MassRangeEnd = 1700, Ms2MassRangeBegin = 50
            };
            if (ccstol < 0) param.IsIonMobility = false; else param.IsIonMobility = true;

            var lipidQueryBean = new LipidQueryBean();
            var queries = LbmQueryParcer.GetLbmQueries(inputqueries, true);
            foreach (var query in queries) {
                if (query.IonMode != ionmode) {
                    query.IsSelected = false;
                } else {
                    query.IsSelected = true;
                }
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.EtherDGDG && query.AdductIon.AdductIonName == "[M+NH4]+") query.IsSelected = false;
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.DG && query.AdductIon.AdductIonName == "[M+Na]+") query.IsSelected = false;
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.SPE && query.AdductIon.AdductIonName == "[M+H]+") query.IsSelected = false;
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.SPEHex && query.AdductIon.AdductIonName == "[M+NH4]+") query.IsSelected = false;
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.SPGHex && query.AdductIon.AdductIonName == "[M+NH4]+") query.IsSelected = false;
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.CSLPHex && query.AdductIon.AdductIonName == "[M+NH4]+") query.IsSelected = false;
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.BRSLPHex && query.AdductIon.AdductIonName == "[M+NH4]+") query.IsSelected = false;
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.CASLPHex && query.AdductIon.AdductIonName == "[M+NH4]+") query.IsSelected = false;
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.SISPHex && query.AdductIon.AdductIonName == "[M+NH4]+") query.IsSelected = false;
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.STSPHex && query.AdductIon.AdductIonName == "[M+NH4]+") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.SPE && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.SPGHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.CSLPHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.BRSLPHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.CASLPHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.SISLPHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.STSLPHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.CSPHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.BRSPHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.CASPHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.SISPHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.STSPHex && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.EtherOxPC && query.AdductIon.AdductIonName == "[M+CH3COO]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.Cer_OS && query.AdductIon.AdductIonName == "[M+CH3COO]-") query.IsSelected = false;
                if (ionmode == IonMode.Positive && query.LbmClass == LbmClass.Cer_NDOS && query.AdductIon.AdductIonName == "[M+H]+") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.Ac2PIM1 && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.Ac2PIM2 && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.Ac3PIM2 && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.Ac4PIM2 && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.LbmClass == LbmClass.LipidA && query.AdductIon.AdductIonName == "[M-H]-") query.IsSelected = false;
                if (ionmode == IonMode.Negative && query.AdductIon.AdductIonName == "[M+HCOO]-") query.IsSelected = false;
                if (query.LbmClass == LbmClass.Others) query.IsSelected = false;
                if (query.LbmClass == LbmClass.Unknown) query.IsSelected = false;
                if (query.LbmClass == LbmClass.SPLASH) query.IsSelected = false;

            }
            lipidQueryBean.IonMode = ionmode;
            lipidQueryBean.SolventType = SolventType.CH3COONH4;
            lipidQueryBean.CollisionType = CollisionType.CID;
            lipidQueryBean.LbmQueries = queries;

            param.LipidQueryBean = lipidQueryBean;
            return param;
        }

        public static void ExportQuantTsvOntologies(string inputfolder, string output) {
            var files = System.IO.Directory.GetFiles(inputfolder, "*.tsv");
            var ontologies = new List<string>();
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var patern2 = false;
                var patern3 = false;
                if (filename.Contains("30_")) patern2 = true;
                var index = int.Parse(filename.Split('_')[0]);
                if ((index >= 47 && index <= 62) || index == 84) patern3 = true;
                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    for (int i = 0; i < 10; i++) sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        if (lineArray[0] == string.Empty) continue;
                        var metname = patern3 ? lineArray[6] : patern2 ? lineArray[5] : lineArray[3];
                        var ontology = patern3 ? lineArray[15] : patern2 ? lineArray[14] : lineArray[11];
                        var annotationtag = patern3 ? lineArray[18] : patern2 ? lineArray[17] : lineArray[14];
                        var comment = patern3 ? lineArray[23] : patern2 ? lineArray[22] : lineArray[18];
                        if (comment.Contains("IS") || metname.Contains("(d") || metname.Contains("(13C") || metname.Contains("SPLASH")) continue;
                        if (annotationtag.Contains("Class") && ontology != "SM+O") continue;
                        if (ontology == "PC" || ontology == "TAG" || ontology == "DAG" || ontology == "Cer_NS" || ontology == "CE") {
                            if (metname.Contains("30:5") || metname.Contains("30:6") || metname.Contains("32:5")
                                || metname.Contains("32:6") || metname.Contains("34:5") || metname.Contains("34:6")) {
                                ontology += "_VLCPUFA";
                                //ontologies.Add(metname + "\t" + filename);
                            }
                        }
                        //if (ontology == "MGDG") {
                        //    ontologies.Add(metname + "\t" + filename);
                        //}
                        if (!ontologies.Contains(ontology)) {
                            if (ontology == string.Empty) {
                                Console.WriteLine();
                            }
                            ontologies.Add(ontology);
                        }
                    }
                }
            }
            ontologies.Sort();
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var ontology in ontologies) {
                    sw.WriteLine(ontology);
                }
            }
        }

        public static void ExportQuantStatistics(string inputfolder, string category_list_file, string tissue_category_file, string lipid_category_file, string output) {
            var files = System.IO.Directory.GetFiles(inputfolder, "*.tsv");
            var categories = new List<string>();
            var filenameToCategory = new Dictionary<string, string>();
            var lipidclassToStat = new Dictionary<string, LipidStatistics>();
            var lipidnameToCategory = new Dictionary<string, string>();
            var lipidlist = new List<string>();
            using (var sr = new StreamReader(category_list_file, Encoding.ASCII)) {
                while(sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    categories.Add(line.Trim());
                }
            }

            using (var sr = new StreamReader(tissue_category_file, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');
                    filenameToCategory[lineArray[0]] = lineArray[4];
                }
            }

            using (var sr = new StreamReader(lipid_category_file, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');
                    var lipidinfo = new LipidStatistics() { LipidID = int.Parse(lineArray[0]), LipidClass = lineArray[1], LipidCategory = lineArray[2] };
                    foreach (var cat in categories) {
                        lipidinfo.CategoryToLipidNames[cat] = new List<string>();
                    }
                    lipidclassToStat[lipidinfo.LipidClass] = lipidinfo;
                    lipidnameToCategory[lipidinfo.LipidClass] = lineArray[2];
                    lipidlist.Add(lipidinfo.LipidClass);
                }
            }

            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                if (!filenameToCategory.ContainsKey(filename)) continue;
                var category = filenameToCategory[filename];
                var patern2 = false;
                var patern3 = false;
                if (filename.Contains("30_")) patern2 = true;
                var index = int.Parse(filename.Split('_')[0]);
                if ((index >= 47 && index <= 62) || index == 84) patern3 = true;
                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    for (int i = 0; i < 10; i++) sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        if (lineArray[0] == string.Empty) continue;

                        var metname = patern3 ? lineArray[6] : patern2 ? lineArray[5] : lineArray[3];
                        var ontology = patern3 ? lineArray[15] : patern2 ? lineArray[14] : lineArray[11];
                        var annotationtag = patern3 ? lineArray[18] : patern2 ? lineArray[17] : lineArray[14];
                        var comment = patern3 ? lineArray[23] : patern2 ? lineArray[22] : lineArray[18];
                        if (comment.Contains("IS") || metname.Contains("(d") || metname.Contains("(13C") || metname.Contains("SPLASH")) continue;
                        if (annotationtag.Contains("Class") && ontology != "SM+O") continue;
                        if (ontology == "PC" || ontology == "TAG" || ontology == "DAG" || ontology == "Cer_NS" || ontology == "CE" || ontology == "SM") {
                            if (metname.Contains("30:5") || metname.Contains("30:6") || metname.Contains("32:5")
                                || metname.Contains("32:6") || metname.Contains("34:5") || metname.Contains("34:6")) {
                                ontology += "_VLCPUFA";
                            }
                        }

                        var info = lipidclassToStat[ontology];
                        if (!info.CategoryToLipidNames[category].Contains(metname))
                            info.CategoryToLipidNames[category].Add(metname);
                        if (!info.UniqueLipids.Contains(metname)) {
                            info.UniqueLipids.Add(metname);
                        }
                    }
                }
            }

            var header = "Name,Category," + String.Join(",", categories);
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine(header.Replace('_', ' '));
                foreach(var lipid in lipidlist) {
                    var lipidinfo = lipidclassToStat[lipid];
                    if (lipidinfo.UniqueLipids.Count() == 0) continue;
                    
                    var row = lipidinfo.LipidClass.Replace('_', '-') + " (" + lipidinfo.UniqueLipids.Count() + ")" + "," + lipidinfo.LipidCategory;
                    foreach (var cat in categories) {
                        row += "," + lipidinfo.CategoryToLipidNames[cat].Count();
                    }
                    sw.WriteLine(row);
                }
            }

            var nameToCounts = new Dictionary<string, List<int>>();
            var nameToMax = new Dictionary<string, int>();
            var nameToMin = new Dictionary<string, int>();
            var names = new List<string>();
            using (var sr = new StreamReader(output, Encoding.ASCII)) {
                header = sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split(',');
                    names.Add(lineArray[0]);

                    nameToCounts[lineArray[0]] = new List<int>();
                    for (int i = 2; i < lineArray.Length; i++) {
                        nameToCounts[lineArray[0]].Add(int.Parse(lineArray[i]));
                    }
                    nameToMax[lineArray[0]] = nameToCounts[lineArray[0]].Max();
                    nameToMin[lineArray[0]] = nameToCounts[lineArray[0]].Min();
                }
            }

            var output2 = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "-vs2.csv";
            using (var sw = new StreamWriter(output2, false, Encoding.ASCII)) {
                sw.WriteLine(header);
                foreach (var lipid in names) {
                    if (!nameToCounts.ContainsKey(lipid)) continue; 
                    var lipidinfo = nameToCounts[lipid];
                    var lipidmax = nameToMax[lipid];
                    var lipidmin = nameToMin[lipid];
                    var lipidname = lipid.Split('(')[0].Trim();

                    if (lipidname == "ACar") lipidname = "CAR";
                    if (lipidname == "NAAG") lipidname = "NAGly";
                    if (lipidname == "NAAGS") lipidname = "NAGlySer";
                    if (lipidname == "NAAO") lipidname = "NAOrn";
                    if (lipidname == "EtherMGDG") lipidname = "MGDG-O";
                    if (lipidname == "EtherSMGDG") lipidname = "SMGDG-O";
                    if (lipidname == "EtherDGDG") lipidname = "DGDG-O";
                    if (lipidname == "MAG") lipidname = "MG";
                    if (lipidname == "DAG") lipidname = "DG";
                    if (lipidname == "TAG") lipidname = "TG";
                    if (lipidname == "DAG_VLCPUFA") lipidname = "DG_VLCPUFA";
                    if (lipidname == "TAG_VLCPUFA") lipidname = "TG_VLCPUFA";
                    if (lipidname == "TAG") lipidname = "TG";
                    if (lipidname == "EtherDAG") lipidname = "DG-O";
                    if (lipidname == "EtherTAG") lipidname = "TG-O";
                    if (lipidname == "EtherPC") lipidname = "PC-O";
                    if (lipidname == "EtherLPC") lipidname = "LPC-O";
                    if (lipidname == "EtherPE") lipidname = "PE-O";
                    if (lipidname == "EtherPE(Plasmalogen)") lipidname = "PE-P";
                    if (lipidname == "EtherLPE") lipidname = "LPE-O";
                    if (lipidname == "EtherPG") lipidname = "PG-O";
                    if (lipidname == "EtherLPG") lipidname = "LPG-O";
                    if (lipidname == "LCL") lipidname = "MLCL";
                    if (lipidname == "EtherPI") lipidname = "PI-O";
                    if (lipidname == "EtherPS") lipidname = "PS-O";
                    if (lipidname == "EtherOxPE") lipidname = "OxPE-O";
                    if (lipidname == "Sphinganine") lipidname = "DHSph";
                    if (lipidname == "Sphingosine") lipidname = "Sph";
                    if (lipidname == "Phytosphingosine") lipidname = "PhytoSph";
                    if (lipidname == "BileAcid") lipidname = "Bile acids";

                    lipidname = lipidname.Replace("+O", ";O");
                    lipidname += " (" + lipid.Split('(')[1];
                    if (lipid.Contains("EtherPE(Plasmalogen)")) {
                        lipidname = lipid.Replace("EtherPE(Plasmalogen)", "PE-P");
                    }


                    var row = lipidname + "," + lipidnameToCategory[lipid.Split('(')[0].Replace('-','_').Trim()];
                    foreach (var num in lipidinfo) {
                        row += "," + ((double)(num - lipidmin) / (double)(lipidmax - lipidmin) * 2.0 - 1.0).ToString();
                    }
                    sw.WriteLine(row);
                }
            }
        }

        public static void Msdial4TsvToLipoqualityDataFormatPaired(string input, string outputdir) {
           
            var filename = System.IO.Path.GetFileNameWithoutExtension(input);
            var index = int.Parse(filename.Split('_')[0]);
            var patern2 = false;
            var patern3 = false;
            if (filename.Contains("30_")) patern2 = true;
            if ((index >= 47 && index <= 62) || index == 84) patern3 = true;

            var lipidWtData = new List<MsdialAlignmetMetaDataField>();
            var lipidMutantData = new List<MsdialAlignmetMetaDataField>();

            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                for (int i = 0; i < 10; i++) sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');
                    var metadatafieldWt = getMsdialAlignmentMetadataField(lineArray, patern2, patern3);
                    if (metadatafieldWt == null) continue;
                    if (metadatafieldWt.Molecule.IsValidatedFormat == false) continue;
                    var metadatafieldMutant = getMsdialAlignmentMetadataField(lineArray, patern2, patern3);

                    var heightBeginWt = 35;
                    var heightEndWt = 37;
                    var heightBeginMutant = 32;
                    var heightEndMutant = 34;
                    var lipidIsId = 38;
                    var normBeginWt = 43;
                    var normEndWt = 45;
                    var normBeginMutant = 40;
                    var normEndWtMutant = 42;

                    if (index == 29) {
                        heightBeginWt = 36;
                        heightEndWt = 44;
                        heightBeginMutant = 45;
                        heightEndMutant = 53;
                        lipidIsId = 54;
                        normBeginWt = 60;
                        normEndWt = 68;
                        normBeginMutant = 69;
                        normEndWtMutant = 77;
                    } 
                    else if (index == 30) {
                        if (filename.EndsWith("RT")) continue;
                        heightBeginWt = 41;
                        heightEndWt = 51;
                        heightBeginMutant = 52;
                        heightEndMutant = 63;
                        lipidIsId = 64;
                        normBeginWt = 70;
                        normEndWt = 80;
                        normBeginMutant = 81;
                        normEndWtMutant = 92;
                    }

                    var heightWtArray = new List<double>();
                    var heightMutantArray = new List<double>();
                    var normWtArray = new List<double>();
                    var normMutantArray = new List<double>();
                    
                    for (int i = heightBeginWt; i <= heightEndWt; i++) {
                        heightWtArray.Add(double.Parse(lineArray[i]));
                    }

                    for (int i = heightBeginMutant; i <= heightEndMutant; i++) {
                        heightMutantArray.Add(double.Parse(lineArray[i]));
                    }

                    for (int i = normBeginWt; i <= normEndWt; i++) {
                        normWtArray.Add(double.Parse(lineArray[i]));
                    }

                    for (int i = normBeginMutant; i <= normEndWtMutant; i++) {
                        normMutantArray.Add(double.Parse(lineArray[i]));
                    }

                    var lipidis = lineArray[lipidIsId];
                    if (lipidis == "ACar+") lipidis = "CAR+";
                    if (lipidis == "MAG+") lipidis = "MG+";
                    if (lipidis == "DAG+") lipidis = "DG+";
                    if (lipidis == "TAG+") lipidis = "TG+";
                    if (lipidis == "Sphinganine+") lipidis = "DHSph+";
                    if (lipidis == "Sphingosine+") lipidis = "Sph+";
                    if (lipidis == "Phytosphingosine+") lipidis = "PhytoSph+";

                    var heightAveWt = BasicMathematics.Mean(heightWtArray.ToArray());
                    var heightStdWt = BasicMathematics.Stdev(heightWtArray.ToArray());
                    var normalizedAveWt = BasicMathematics.Mean(normWtArray.ToArray());
                    var normalizedStdWt = BasicMathematics.Stdev(normWtArray.ToArray());

                    var heightAveMutant = BasicMathematics.Mean(heightMutantArray.ToArray());
                    var heightStdMutant = BasicMathematics.Stdev(heightMutantArray.ToArray());
                    var normalizedAveMutant = BasicMathematics.Mean(normMutantArray.ToArray());
                    var normalizedStdMutant = BasicMathematics.Stdev(normMutantArray.ToArray());

                    metadatafieldWt.HeightAverage = heightAveWt;
                    metadatafieldWt.HeightStdev = heightStdWt;
                    metadatafieldWt.NormalizedValueAverage = normalizedAveWt;
                    metadatafieldWt.NormalizedValueStdev = normalizedStdWt;
                    metadatafieldWt.InternalStandardString = lipidis;

                    metadatafieldMutant.HeightAverage = heightAveMutant;
                    metadatafieldMutant.HeightStdev = heightStdMutant;
                    metadatafieldMutant.NormalizedValueAverage = normalizedAveMutant;
                    metadatafieldMutant.NormalizedValueStdev = normalizedStdMutant;
                    metadatafieldMutant.InternalStandardString = lipidis;

                    lipidWtData.Add(metadatafieldWt);
                    lipidMutantData.Add(metadatafieldMutant);
                }
            }

            // writer for wt
            var sample_name_wt = "null";
            var sample_name_mutant = "null";
            var sample_category = "Mouse";
            var sample_tissueorspecies = "null";
            var sample_genotypeWt = "C57BL/6J";
            var sample_genotypeMutant = "B6-fads2/J";
            var sample_pertubationWt = "null";
            var sample_pertubationMutant = "null";
            var sample_dietcluture = "CE2";
            var quantunitHeight = "Height";
            var quantunitNormalized = "pmol/mg tissue";
            var biologicalreplicates = 3;
            var technicalreplicates = 0;
            var methodlink = "null";
            switch (index) {
                case 1:
                    sample_name_wt = "Mouse adrenal gland B6/J";
                    sample_name_mutant = "Mouse adrenal gland B6-fads2/J";
                    sample_tissueorspecies = "Adrenal gland";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 2:
                    sample_name_wt = "Mouse brain B6/J";
                    sample_name_mutant = "Mouse brain B6-fads2/J";
                    sample_tissueorspecies = "Brain";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 3:
                    sample_name_wt = "Mouse brown adipose tissue B6/J";
                    sample_name_mutant = "Mouse brown adipose tissue B6-fads2/J";
                    sample_tissueorspecies = "Brown adipose tissue";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 4:
                    sample_name_wt = "Mouse eye B6/J";
                    sample_name_mutant = "Mouse eye B6-fads2/J";
                    sample_tissueorspecies = "Eye";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 5:
                    sample_name_wt = "Mouse kidney B6/J";
                    sample_name_mutant = "Mouse kidney B6-fads2/J";
                    sample_tissueorspecies = "Kidney";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 6:
                    sample_name_wt = "Mouse liver B6/J";
                    sample_name_mutant = "Mouse liver B6-fads2/J";
                    sample_tissueorspecies = "Liver";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 7:
                    sample_name_wt = "Mouse skeletal muscle B6/J";
                    sample_name_mutant = "Mouse skeletal muscle B6-fads2/J";
                    sample_tissueorspecies = "Skeletal muscle";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 8:
                    sample_name_wt = "Mouse skin B6/J";
                    sample_name_mutant = "Mouse skin B6-fads2/J";
                    sample_tissueorspecies = "Skin";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 9:
                    sample_name_wt = "Mouse small intestine B6/J";
                    sample_name_mutant = "Mouse small intestine B6-fads2/J";
                    sample_tissueorspecies = "Small intestine";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 10:
                    sample_name_wt = "Mouse testis B6/J";
                    sample_name_mutant = "Mouse testis B6-fads2/J";
                    sample_tissueorspecies = "Testis";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 11:
                    sample_name_wt = "Mouse white adipose tissue B6/J";
                    sample_name_mutant = "Mouse white adipose tissue B6-fads2/J";
                    sample_tissueorspecies = "White adipose tissue";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 82:
                    sample_name_wt = "Mouse feces B6/J";
                    sample_name_mutant = "Mouse feces B6-fads2/J";
                    sample_tissueorspecies = "Feces";
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 29:
                    sample_name_wt = "Arabidopsis Col-0 shoot 22℃";
                    sample_name_mutant = "Arabidopsis Col-0 shoot 38℃";
                    sample_category = "Plant";
                    sample_tissueorspecies = "Arabidopsis thaliana/Shoot";
                    sample_genotypeWt = "Col-0";
                    sample_genotypeMutant = "Col-0";
                    sample_pertubationWt = "Control(22℃)";
                    sample_pertubationMutant = "Heat stress(38℃)";
                    sample_dietcluture = "Agar MS medium";
                    quantunitHeight = "Height";
                    quantunitNormalized = "Height*Average(IS Height)/IS Height";
                    biologicalreplicates = 3;
                    technicalreplicates = 3;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS03";
                    break;
                case 30:
                    sample_name_wt = "Arabidopsis Col-0 shoot 22℃";
                    sample_name_mutant = "Arabidopsis Col-0 shoot 38℃";
                    sample_category = "Plant";
                    sample_tissueorspecies = "Arabidopsis thaliana/Shoot";
                    sample_genotypeWt = "Col-0";
                    sample_genotypeMutant = "Col-0";
                    sample_pertubationWt = "Control(22℃)";
                    sample_pertubationMutant = "Heat stress(38℃)";
                    sample_dietcluture = "Agar MS medium";
                    quantunitHeight = "Height";
                    quantunitNormalized = "Height*Average(IS Height)/IS Height";
                    biologicalreplicates = 3;
                    technicalreplicates = 4;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS04";
                    break;
            }

            var inputfilename = System.IO.Path.GetFileNameWithoutExtension(input);
            var outputfile_height_wt = outputdir + "\\" + inputfilename + "_Height_Ctr.txt";
            var outputfile_norm_wt = outputdir + "\\" + inputfilename + "_Normalized_Ctr.txt";
            var outputfile_height_fads2 = outputdir + "\\" + inputfilename + "_Height_Compared.txt";
            var outputfile_norm_fads2 = outputdir + "\\" + inputfilename + "_Normalized_Compared.txt";

            var header = new List<string>() { "Lipid category", "Lipid subclass", "Total chain", "SN1 chain", "SN2 chain", "SN3 chain", "SN4 chain",
                "Formula", "InChIKey", "SMILES", "RT(min)", "m/z", "Quant value", "Standard deviation", "Alignment ID", "Adduct", "MSMS", "Internal standard", "CCS", "Annotation level" };

            using (var sw = new StreamWriter(outputfile_height_wt, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_wt);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotypeWt);
                sw.WriteLine("Perturbation\t" + sample_pertubationWt);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitHeight);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidWtData) {

                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));
                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }

                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.HeightAverage.ToString(), field.HeightStdev.ToString(), field.AlignmentID.ToString(), field.Adduct, field.Spectrum, "null", field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_norm_wt, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_wt);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotypeWt);
                sw.WriteLine("Perturbation\t" + sample_pertubationWt);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitNormalized);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidWtData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.NormalizedValueAverage.ToString(), field.NormalizedValueStdev.ToString(), field.AlignmentID.ToString(),
                        field.Adduct, field.Spectrum, field.InternalStandardString, field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_height_fads2, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_mutant);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotypeMutant);
                sw.WriteLine("Perturbation\t" + sample_pertubationMutant);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitHeight);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidMutantData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.HeightAverage.ToString(), field.HeightStdev.ToString(), field.AlignmentID.ToString(), 
                        field.Adduct, field.Spectrum, "null", field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_norm_fads2, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_mutant);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotypeMutant);
                sw.WriteLine("Perturbation\t" + sample_pertubationMutant);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitNormalized);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidMutantData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.NormalizedValueAverage.ToString(), field.NormalizedValueStdev.ToString(), 
                        field.AlignmentID.ToString(), field.Adduct, field.Spectrum, field.InternalStandardString, field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }
        }

        public static void Msdial4TsvToLipoqualityDataFormatFourPaired(string input, string outputdir) {

            var filename = System.IO.Path.GetFileNameWithoutExtension(input);
            var index = int.Parse(filename.Split('_')[0]);
            var patern2 = false;
            var patern3 = false;
            if (filename.Contains("30_")) patern2 = true;
            if ((index >= 47 && index <= 62) || index == 84) patern3 = true;

            var lipidCtrData = new List<MsdialAlignmetMetaDataField>();
            var lipidAraData = new List<MsdialAlignmetMetaDataField>();
            var lipidEpaData = new List<MsdialAlignmetMetaDataField>();
            var lipidDhaData = new List<MsdialAlignmetMetaDataField>();

            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                for (int i = 0; i < 10; i++) sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');
                    var metadatafieldCtr = getMsdialAlignmentMetadataField(lineArray, patern2, patern3);
                    if (metadatafieldCtr == null) continue;
                    if (metadatafieldCtr.Molecule.IsValidatedFormat == false) continue;
                    var metadatafieldAra = getMsdialAlignmentMetadataField(lineArray, patern2, patern3);
                    var metadatafieldEpa = getMsdialAlignmentMetadataField(lineArray, patern2, patern3);
                    var metadatafieldDha = getMsdialAlignmentMetadataField(lineArray, patern2, patern3);

                    var heightBeginCtr = 46;
                    var heightEndCtr = 50;
                    var heightBeginAra = 31;
                    var heightEndAra = 35;
                    var heightBeginEpa = 41;
                    var heightEndEpa = 45;
                    var heightBeginDha = 36;
                    var heightEndDha = 40;
                    var lipidIsId = 53;
                    var normBeginCtr = 69;
                    var normEndCtr = 73;
                    var normBeginAra = 54;
                    var normEndAra = 58;
                    var normBeginEpa = 64;
                    var normEndEpa = 68;
                    var normBeginDha = 59;
                    var normEndDha= 63;

                    if (index == 19 || index == 22 || index == 28) {
                        heightBeginCtr = 37;
                        heightEndCtr = 41;
                        heightBeginAra = 31;
                        heightEndAra = 35;
                        heightBeginEpa = 47;
                        heightEndEpa = 51;
                        heightBeginDha = 42;
                        heightEndDha = 46;
                        lipidIsId = 53;
                        normBeginCtr = 60;
                        normEndCtr = 64;
                        normBeginAra = 54;
                        normEndAra = 58;
                        normBeginEpa = 70;
                        normEndEpa = 74;
                        normBeginDha = 65;
                        normEndDha = 69;
                    }
                    else if (index == 20 || index == 25 || index == 26) {
                        heightBeginCtr = 47;
                        heightEndCtr = 51;
                        heightBeginAra = 31;
                        heightEndAra = 35;
                        heightBeginEpa = 42;
                        heightEndEpa = 46;
                        heightBeginDha = 37;
                        heightEndDha = 41;
                        lipidIsId = 53;
                        normBeginCtr = 70;
                        normEndCtr = 74;
                        normBeginAra = 54;
                        normEndAra = 58;
                        normBeginEpa = 65;
                        normEndEpa = 69;
                        normBeginDha = 60;
                        normEndDha = 64;
                    }
                    else if (index == 21) {
                        heightBeginCtr = 44;
                        heightEndCtr = 47;
                        heightBeginAra = 31;
                        heightEndAra = 34;
                        heightBeginEpa = 40;
                        heightEndEpa = 43;
                        heightBeginDha = 36;
                        heightEndDha = 39;
                        lipidIsId = 49;
                        normBeginCtr = 63;
                        normEndCtr = 66;
                        normBeginAra = 50;
                        normEndAra = 53;
                        normBeginEpa = 59;
                        normEndEpa = 62;
                        normBeginDha = 55;
                        normEndDha = 58;
                    }
                    else if (index == 23) {
                        heightBeginCtr = 34;
                        heightEndCtr = 36;
                        heightBeginAra = 31;
                        heightEndAra = 33;
                        heightBeginEpa = 40;
                        heightEndEpa = 42;
                        heightBeginDha = 37;
                        heightEndDha = 39;
                        lipidIsId = 43;
                        normBeginCtr = 47;
                        normEndCtr = 49;
                        normBeginAra = 44;
                        normEndAra = 46;
                        normBeginEpa = 53;
                        normEndEpa = 55;
                        normBeginDha = 50;
                        normEndDha = 52;
                    }
                    else if (index == 24 || index == 27) {
                        heightBeginCtr = 47;
                        heightEndCtr = 51;
                        heightBeginAra = 32;
                        heightEndAra = 36;
                        heightBeginEpa = 42;
                        heightEndEpa = 46;
                        heightBeginDha = 37;
                        heightEndDha = 41;
                        lipidIsId = 53;
                        normBeginCtr = 70;
                        normEndCtr = 74;
                        normBeginAra = 55;
                        normEndAra = 59;
                        normBeginEpa = 65;
                        normEndEpa = 69;
                        normBeginDha = 60;
                        normEndDha = 64;
                    }

                    var heightCtrArray = new List<double>();
                    var heightAraArray = new List<double>();
                    var heightEpaArray = new List<double>();
                    var heightDhaArray = new List<double>();
                    var normCtrArray = new List<double>();
                    var normAraArray = new List<double>();
                    var normEpaArray = new List<double>();
                    var normDhaArray = new List<double>();

                    for (int i = heightBeginCtr; i <= heightEndCtr; i++) {
                        heightCtrArray.Add(double.Parse(lineArray[i]));
                    }

                    for (int i = heightBeginAra; i <= heightEndAra; i++) {
                        heightAraArray.Add(double.Parse(lineArray[i]));
                    }

                    for (int i = heightBeginEpa; i <= heightEndEpa; i++) {
                        heightEpaArray.Add(double.Parse(lineArray[i]));
                    }

                    for (int i = heightBeginDha; i <= heightEndDha; i++) {
                        heightDhaArray.Add(double.Parse(lineArray[i]));
                    }

                    for (int i = normBeginCtr; i <= normEndCtr; i++) {
                        normCtrArray.Add(double.Parse(lineArray[i]));
                    }

                    for (int i = normBeginAra; i <= normEndAra; i++) {
                        normAraArray.Add(double.Parse(lineArray[i]));
                    }

                    for (int i = normBeginEpa; i <= normEndEpa; i++) {
                        normEpaArray.Add(double.Parse(lineArray[i]));
                    }

                    for (int i = normBeginDha; i <= normEndDha; i++) {
                        normDhaArray.Add(double.Parse(lineArray[i]));
                    }

                    var lipidis = lineArray[lipidIsId];
                    if (lipidis == "ACar+") lipidis = "CAR+";
                    if (lipidis == "MAG+") lipidis = "MG+";
                    if (lipidis == "DAG+") lipidis = "DG+";
                    if (lipidis == "TAG+") lipidis = "TG+";
                    if (lipidis == "Sphinganine+") lipidis = "DHSph+";
                    if (lipidis == "Sphingosine+") lipidis = "Sph+";
                    if (lipidis == "Phytosphingosine+") lipidis = "PhytoSph+";

                    var heightAveCtr = BasicMathematics.Mean(heightCtrArray.ToArray());
                    var heightStdCtr = BasicMathematics.Stdev(heightCtrArray.ToArray());
                    var normalizedAveCtr = BasicMathematics.Mean(normCtrArray.ToArray());
                    var normalizedStdCtr = BasicMathematics.Stdev(normCtrArray.ToArray());

                    var heightAveAra = BasicMathematics.Mean(heightAraArray.ToArray());
                    var heightStdAra = BasicMathematics.Stdev(heightAraArray.ToArray());
                    var normalizedAveAra = BasicMathematics.Mean(normAraArray.ToArray());
                    var normalizedStdAra = BasicMathematics.Stdev(normAraArray.ToArray());

                    var heightAveEpa = BasicMathematics.Mean(heightEpaArray.ToArray());
                    var heightStdEpa = BasicMathematics.Stdev(heightEpaArray.ToArray());
                    var normalizedAveEpa = BasicMathematics.Mean(normEpaArray.ToArray());
                    var normalizedStdEpa = BasicMathematics.Stdev(normEpaArray.ToArray());

                    var heightAveDha = BasicMathematics.Mean(heightDhaArray.ToArray());
                    var heightStdDha = BasicMathematics.Stdev(heightDhaArray.ToArray());
                    var normalizedAveDha = BasicMathematics.Mean(normDhaArray.ToArray());
                    var normalizedStdDha = BasicMathematics.Stdev(normDhaArray.ToArray());

                    metadatafieldCtr.HeightAverage = heightAveCtr;
                    metadatafieldCtr.HeightStdev = heightStdCtr;
                    metadatafieldCtr.NormalizedValueAverage = normalizedAveCtr;
                    metadatafieldCtr.NormalizedValueStdev = normalizedStdCtr;
                    metadatafieldCtr.InternalStandardString = lipidis;

                    metadatafieldAra.HeightAverage = heightAveAra;
                    metadatafieldAra.HeightStdev = heightStdAra;
                    metadatafieldAra.NormalizedValueAverage = normalizedAveAra;
                    metadatafieldAra.NormalizedValueStdev = normalizedStdAra;
                    metadatafieldAra.InternalStandardString = lipidis;

                    metadatafieldEpa.HeightAverage = heightAveEpa;
                    metadatafieldEpa.HeightStdev = heightStdEpa;
                    metadatafieldEpa.NormalizedValueAverage = normalizedAveEpa;
                    metadatafieldEpa.NormalizedValueStdev = normalizedStdEpa;
                    metadatafieldEpa.InternalStandardString = lipidis;

                    metadatafieldDha.HeightAverage = heightAveDha;
                    metadatafieldDha.HeightStdev = heightStdDha;
                    metadatafieldDha.NormalizedValueAverage = normalizedAveDha;
                    metadatafieldDha.NormalizedValueStdev = normalizedStdDha;
                    metadatafieldDha.InternalStandardString = lipidis;

                    lipidCtrData.Add(metadatafieldCtr);
                    lipidAraData.Add(metadatafieldAra);
                    lipidEpaData.Add(metadatafieldEpa);
                    lipidDhaData.Add(metadatafieldDha);
                }
            }

            // writer for wt
            var sample_name_ctr = "null";
            var sample_name_ara = "null";
            var sample_name_epa = "null";
            var sample_name_dha = "null";
            var sample_category = "Mouse";
            var sample_tissueorspecies = "null";
            var sample_genotype = "C57BL/6J";
            var sample_pertubation = "null";
            var sample_dietcluture_ctr = "F1(fish-meal free)";
            var sample_dietcluture_ara = "F1+1%EE-ARA";
            var sample_dietcluture_epa = "F1+1%EE-EPA";
            var sample_dietcluture_dha = "F1+1%EE-DHA";
            var quantunitHeight = "Height";
            var quantunitNormalized = "pmol/mg tissue";
            var biologicalreplicates = 5;
            var technicalreplicates = 0;
            var methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS02";
            switch (index) {
                case 18:
                    sample_name_ctr = "Mouse brain B6/J Control diet";
                    sample_name_ara = "Mouse brain B6/J ARA-rich diet";
                    sample_name_epa = "Mouse brain B6/J EPA-rich diet";
                    sample_name_dha = "Mouse brain B6/J DHA-rich diet";
                    sample_tissueorspecies = "Brain";
                    break;
                case 19:
                    sample_name_ctr = "Mouse heart B6/J Control diet";
                    sample_name_ara = "Mouse heart B6/J ARA-rich diet";
                    sample_name_epa = "Mouse heart B6/J EPA-rich diet";
                    sample_name_dha = "Mouse heart B6/J DHA-rich diet";
                    sample_tissueorspecies = "Heart";
                    break;
                case 20:
                    sample_name_ctr = "Mouse kidney B6/J Control diet";
                    sample_name_ara = "Mouse kidney B6/J ARA-rich diet";
                    sample_name_epa = "Mouse kidney B6/J EPA-rich diet";
                    sample_name_dha = "Mouse kidney B6/J DHA-rich diet";
                    sample_tissueorspecies = "Kidney";
                    break;
                case 21:
                    sample_name_ctr = "Mouse liver B6/J Control diet";
                    sample_name_ara = "Mouse liver B6/J ARA-rich diet";
                    sample_name_epa = "Mouse liver B6/J EPA-rich diet";
                    sample_name_dha = "Mouse liver B6/J DHA-rich diet";
                    sample_tissueorspecies = "Liver";
                    biologicalreplicates = 4;
                    break;
                case 22:
                    sample_name_ctr = "Mouse lung B6/J Control diet";
                    sample_name_ara = "Mouse lung B6/J ARA-rich diet";
                    sample_name_epa = "Mouse lung B6/J EPA-rich diet";
                    sample_name_dha = "Mouse lung B6/J DHA-rich diet";
                    sample_tissueorspecies = "Lung";
                    break;
                case 23:
                    sample_name_ctr = "Mouse macrophage B6/J Control diet";
                    sample_name_ara = "Mouse macrophage B6/J ARA-rich diet";
                    sample_name_epa = "Mouse macrophage B6/J EPA-rich diet";
                    sample_name_dha = "Mouse macrophage B6/J DHA-rich diet";
                    sample_tissueorspecies = "Macrophage";
                    biologicalreplicates = 0;
                    technicalreplicates = 3;
                    break;
                case 24:
                    sample_name_ctr = "Mouse plasma B6/J Control diet";
                    sample_name_ara = "Mouse plasma B6/J ARA-rich diet";
                    sample_name_epa = "Mouse plasma B6/J EPA-rich diet";
                    sample_name_dha = "Mouse plasma B6/J DHA-rich diet";
                    sample_tissueorspecies = "Plasma";
                    quantunitNormalized = "pmol/uL plasma";
                    break;
                case 25:
                    sample_name_ctr = "Mouse skeletal muscle B6/J Control diet";
                    sample_name_ara = "Mouse skeletal muscle B6/J ARA-rich diet";
                    sample_name_epa = "Mouse skeletal muscle B6/J EPA-rich diet";
                    sample_name_dha = "Mouse skeletal muscle B6/J DHA-rich diet";
                    sample_tissueorspecies = "Skeletal muscle";
                    break;
                case 26:
                    sample_name_ctr = "Mouse small intestine B6/J Control diet";
                    sample_name_ara = "Mouse small intestine B6/J ARA-rich diet";
                    sample_name_epa = "Mouse small intestine B6/J EPA-rich diet";
                    sample_name_dha = "Mouse small intestine B6/J DHA-rich diet";
                    sample_tissueorspecies = "Small intestine";
                    break;
                case 27:
                    sample_name_ctr = "Mouse spleen B6/J Control diet";
                    sample_name_ara = "Mouse spleen B6/J ARA-rich diet";
                    sample_name_epa = "Mouse spleen B6/J EPA-rich diet";
                    sample_name_dha = "Mouse spleen B6/J DHA-rich diet";
                    sample_tissueorspecies = "Spleen";
                    break;
                case 28:
                    sample_name_ctr = "Mouse white adipose tissue B6/J Control diet";
                    sample_name_ara = "Mouse white adipose tissue B6/J ARA-rich diet";
                    sample_name_epa = "Mouse white adipose tissue B6/J EPA-rich diet";
                    sample_name_dha = "Mouse white adipose tissue B6/J DHA-rich diet";
                    sample_tissueorspecies = "White adipose tissue";
                    break;
                
            }

            var inputfilename = System.IO.Path.GetFileNameWithoutExtension(input);
            var outputfile_height_ctr = outputdir + "\\" + inputfilename + "_Height_Ctr.txt";
            var outputfile_norm_ctr = outputdir + "\\" + inputfilename + "_Normalized_Ctr.txt";
            var outputfile_height_ara = outputdir + "\\" + inputfilename + "_Height_Ara.txt";
            var outputfile_norm_ara = outputdir + "\\" + inputfilename + "_Normalized_Ara.txt";
            var outputfile_height_epa = outputdir + "\\" + inputfilename + "_Height_Epa.txt";
            var outputfile_norm_epa = outputdir + "\\" + inputfilename + "_Normalized_Epa.txt";
            var outputfile_height_dha = outputdir + "\\" + inputfilename + "_Height_Dha.txt";
            var outputfile_norm_dha = outputdir + "\\" + inputfilename + "_Normalized_Dha.txt";

            var header = new List<string>() { "Lipid category", "Lipid subclass", "Total chain", "SN1 chain", "SN2 chain", "SN3 chain", "SN4 chain",
                "Formula", "InChIKey", "SMILES", "RT(min)", "m/z", "Quant value", "Standard deviation", "Alignment ID", "Adduct", "MSMS", "Internal standard", "CCS", "Annotation level" };

            using (var sw = new StreamWriter(outputfile_height_ctr, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_ctr);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotype);
                sw.WriteLine("Perturbation\t" + sample_pertubation);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture_ctr);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitHeight);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidCtrData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.HeightAverage.ToString(), field.HeightStdev.ToString(), field.AlignmentID.ToString(), field.Adduct, field.Spectrum, "null", field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_norm_ctr, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_ctr);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotype);
                sw.WriteLine("Perturbation\t" + sample_pertubation);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture_ctr);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitNormalized);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidCtrData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.NormalizedValueAverage.ToString(), field.NormalizedValueStdev.ToString(), field.AlignmentID.ToString(),
                        field.Adduct, field.Spectrum, field.InternalStandardString, field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_height_ara, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_ara);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotype);
                sw.WriteLine("Perturbation\t" + sample_pertubation);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture_ara);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitHeight);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidAraData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.HeightAverage.ToString(), field.HeightStdev.ToString(), field.AlignmentID.ToString(),
                        field.Adduct, field.Spectrum, "null", field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_norm_ara, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_ara);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotype);
                sw.WriteLine("Perturbation\t" + sample_pertubation);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture_ara);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitNormalized);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidAraData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.NormalizedValueAverage.ToString(), field.NormalizedValueStdev.ToString(),
                        field.AlignmentID.ToString(), field.Adduct, field.Spectrum, field.InternalStandardString, field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_height_epa, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_epa);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotype);
                sw.WriteLine("Perturbation\t" + sample_pertubation);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture_epa);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitHeight);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidEpaData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.HeightAverage.ToString(), field.HeightStdev.ToString(), field.AlignmentID.ToString(),
                        field.Adduct, field.Spectrum, "null", field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_norm_epa, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_epa);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotype);
                sw.WriteLine("Perturbation\t" + sample_pertubation);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture_epa);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitNormalized);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidEpaData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.NormalizedValueAverage.ToString(), field.NormalizedValueStdev.ToString(),
                        field.AlignmentID.ToString(), field.Adduct, field.Spectrum, field.InternalStandardString, field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation};

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_height_dha, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_dha);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotype);
                sw.WriteLine("Perturbation\t" + sample_pertubation);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture_dha);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitHeight);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidDhaData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.HeightAverage.ToString(), field.HeightStdev.ToString(), field.AlignmentID.ToString(),
                        field.Adduct, field.Spectrum, "null", field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_norm_dha, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name_dha);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotype);
                sw.WriteLine("Perturbation\t" + sample_pertubation);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture_dha);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitNormalized);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidDhaData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.NormalizedValueAverage.ToString(), field.NormalizedValueStdev.ToString(),
                        field.AlignmentID.ToString(), field.Adduct, field.Spectrum, field.InternalStandardString, field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }
        }

        public static void Msdial4TsvToLipoqualityDataFormatOneBioSample(string input, string outputdir) {

            var filename = System.IO.Path.GetFileNameWithoutExtension(input);
            var index = int.Parse(filename.Split('_')[0]);
            var patern2 = false;
            var patern3 = false;
            if (filename.Contains("30_")) patern2 = true;
            if ((index >= 47 && index <= 62) || index == 84) patern3 = true;

            var lipidData = new List<MsdialAlignmetMetaDataField>();

            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                for (int i = 0; i < 10; i++) sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');
                    var metadatafield = getMsdialAlignmentMetadataField(lineArray, patern2, patern3);
                    if (metadatafield == null) continue;
                    if (metadatafield.Molecule.IsValidatedFormat == false) continue;

                    // index = 14
                    var heightBegin = 32;
                    var heightEnd = 36;
                    var lipidIsId = 37;
                    var normBegin = 39;
                    var normEnd = 43;
                    if (index == 15) {
                        heightBegin = 31;
                        heightEnd = 34;
                        lipidIsId = 36;
                        normBegin = 37;
                        normEnd = 40;
                    }
                    else if (index == 16 || index == 17) {
                        heightBegin = 32;
                        heightEnd = 34;
                        lipidIsId = 35;
                        normBegin = 37;
                        normEnd = 39;
                    }
                    else if (index >= 31 && 46 >= index) {
                        heightBegin = 33;
                        heightEnd = 35;
                        lipidIsId = 36;
                        normBegin = 39;
                        normEnd = 41;
                    }
                    else if (index >= 47 && 62 >= index) {
                        heightBegin = 40;
                        heightEnd = 42;
                        lipidIsId = 43;
                        normBegin = 47;
                        normEnd = 49;
                        if (filename.EndsWith("RT")) continue;
                    }
                    else if (index >= 63 && 65 >= index) {
                        heightBegin = 32;
                        heightEnd = 34;
                        lipidIsId = 35;
                        normBegin = 37;
                        normEnd = 39;
                    }
                    else if (index >= 67 && 70 >= index) {
                        heightBegin = 33;
                        heightEnd = 36;
                        lipidIsId = 37;
                        normBegin = 40;
                        normEnd = 43;
                    }
                    else if (index >= 71 && 75 >= index) {
                        heightBegin = 33;
                        heightEnd = 35;
                        lipidIsId = 36;
                        normBegin = 39;
                        normEnd = 41;
                    }
                    else if (index == 76 || index == 77 || index == 83) {
                        heightBegin = 32;
                        heightEnd = 36;
                        lipidIsId = 37;
                        normBegin = 39;
                        normEnd = 43;
                    }
                    else if (index == 78) {
                        heightBegin = 35;
                        heightEnd = 38;
                        lipidIsId = 39;
                        normBegin = 44;
                        normEnd = 47;
                    }
                    else if (index == 79 || index == 81) {
                        heightBegin = 32;
                        heightEnd = 34;
                        lipidIsId = 35;
                        normBegin = 37;
                        normEnd = 39;
                    }
                    else if (index == 80) {
                        heightBegin = 32;
                        heightEnd = 37;
                        lipidIsId = 38;
                        normBegin = 40;
                        normEnd = 45;
                    }
                    else if (index == 84) {
                        heightBegin = 38;
                        heightEnd = 42;
                        lipidIsId = 43;
                        normBegin = 45;
                        normEnd = 49;
                    }
                    var heightArray = new List<double>();
                    var normArray = new List<double>();

                    for (int i = heightBegin; i <= heightEnd; i++) {
                        heightArray.Add(double.Parse(lineArray[i]));
                    }

                    var isCorrectValue = true;
                    for (int i = normBegin; i <= normEnd; i++) {
                        double double_value;
                        if (double.TryParse(lineArray[i], out double_value))
                            normArray.Add(double_value);
                        else {
                            isCorrectValue = false;
                            break;
                        }
                    }
                    if (isCorrectValue == false) continue;
                    var lipidis = lineArray[lipidIsId];
                    if (lipidis == "ACar+") lipidis = "CAR+";
                    if (lipidis == "MAG+") lipidis = "MG+";
                    if (lipidis == "DAG+") lipidis = "DG+";
                    if (lipidis == "TAG+") lipidis = "TG+";
                    if (lipidis == "Sphinganine+") lipidis = "DHSph+";
                    if (lipidis == "Sphingosine+") lipidis = "Sph+";
                    if (lipidis == "Phytosphingosine+") lipidis = "PhytoSph+";

                    var heightAve = BasicMathematics.Mean(heightArray.ToArray());
                    var heightStd = BasicMathematics.Stdev(heightArray.ToArray());
                    var normalizedAve = BasicMathematics.Mean(normArray.ToArray());
                    var normalizedStd = BasicMathematics.Stdev(normArray.ToArray());

                    metadatafield.HeightAverage = heightAve;
                    metadatafield.HeightStdev = heightStd;
                    metadatafield.NormalizedValueAverage = normalizedAve;
                    metadatafield.NormalizedValueStdev = normalizedStd;
                    metadatafield.InternalStandardString = lipidis;
                    lipidData.Add(metadatafield);
                }
            }

            // writer for wt
            var sample_name = "null";
            var sample_category = "null";
            var sample_tissueorspecies = "null";
            var sample_genotype = "null";
            var sample_pertubation = "null";
            var sample_dietcluture = "null";
            var quantunitHeight = "Height";
            var quantunitNormalized = "pmol/mg tissue";
            var biologicalreplicates = 3;
            var technicalreplicates = 0;
            var methodlink = "null";
            switch (index) {
                case 14:
                    sample_name = "Cultured cell 3T3-L1";
                    sample_category = "Mouse cultured cell";
                    sample_tissueorspecies = "3T3-L1";
                    sample_genotype = "WT";
                    sample_pertubation = "null";
                    sample_dietcluture = "Control";
                    quantunitNormalized = "Height/mM phosphorus";
                    biologicalreplicates = 5;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS02";
                    break;
                case 15:
                    sample_name = "Cultured cell C2C12";
                    sample_category = "Mouse cultured cell";
                    sample_tissueorspecies = "C2C12";
                    sample_genotype = "WT";
                    sample_pertubation = "null";
                    sample_dietcluture = "Control";
                    quantunitNormalized = "Height/mM phosphorus";
                    biologicalreplicates = 4;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS02";
                    break;
                case 16:
                    sample_name = "Cultured cell HEK293";
                    sample_category = "Human cultured cell";
                    sample_tissueorspecies = "HEK293";
                    sample_genotype = "WT";
                    sample_pertubation = "null";
                    sample_dietcluture = "Control";
                    quantunitNormalized = "Height/mM phosphorus";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS02";
                    break;
                case 17:
                    sample_name = "Cultured cell HeLa";
                    sample_category = "Human cultured cell";
                    sample_tissueorspecies = "HeLa";
                    sample_genotype = "WT";
                    sample_pertubation = "null";
                    sample_dietcluture = "Control";
                    quantunitNormalized = "Height/mM phosphorus";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS02";
                    break;
                case 31:
                    sample_name = "Mouse adrenal gland B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Adrenal gland";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 32:
                    sample_name = "Mouse brain B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Brain";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 33:
                    sample_name = "Mouse eye B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Eye";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 34:
                    sample_name = "Mouse feces B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Feces";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 35:
                    sample_name = "Mouse heart B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Heart";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 36:
                    sample_name = "Mouse kidney B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Kidney";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 37:
                    sample_name = "Mouse large intestine B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Large intestine";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 38:
                    sample_name = "Mouse liver B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Liver";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 39:
                    sample_name = "Mouse lung B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Lung";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 40:
                    sample_name = "Mouse pancreas B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Pancreas";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 41:
                    sample_name = "Mouse plasma B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Plasma";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/uL plasma";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 42:
                    sample_name = "Mouse skeletal muscle B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Skeletal muscle";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 43:
                    sample_name = "Mouse skin B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Skin";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 44:
                    sample_name = "Mouse small intestine B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Small intestine";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 45:
                    sample_name = "Mouse spleen B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Spleen";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 46:
                    sample_name = "Mouse testis B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Testis";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 47:
                    sample_name = "Mouse adrenal gland B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Adrenal gland";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 48:
                    sample_name = "Mouse brain B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Brain";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 49:
                    sample_name = "Mouse eye B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Eye";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 50:
                    sample_name = "Mouse feces B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Feces";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 51:
                    sample_name = "Mouse heart B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Heart";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 52:
                    sample_name = "Mouse kidney B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Kidney";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 53:
                    sample_name = "Mouse large intestine B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Large intestine";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 54:
                    sample_name = "Mouse liver B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Liver";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 55:
                    sample_name = "Mouse lung B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Lung";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 56:
                    sample_name = "Mouse pancreas B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Pancreas";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 57:
                    sample_name = "Mouse plasma B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Plasma";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/uL plasma";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 58:
                    sample_name = "Mouse skeletal muscle B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Skeletal muscle";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 59:
                    sample_name = "Mouse skin B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Skin";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 60:
                    sample_name = "Mouse small intestine B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Small intestine";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 61:
                    sample_name = "Mouse spleen B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Spleen";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 62:
                    sample_name = "Mouse testis B6/J";
                    sample_category = "Mouse";
                    sample_tissueorspecies = "Testis";
                    sample_genotype = "C57B6/J";
                    sample_pertubation = "null";
                    sample_dietcluture = "CE2";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
                case 63:
                    sample_name = "Arabidopsis Col-0";
                    sample_category = "Plant";
                    sample_tissueorspecies = "Arabidopsis thaliana";
                    sample_genotype = "Col-0";
                    sample_pertubation = "null";
                    sample_dietcluture = "On soil (PROMIX BX)";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS07";
                    break;
                case 64:
                    sample_name = "Oryza sativa";
                    sample_category = "Plant";
                    sample_tissueorspecies = "Oryza sativa";
                    sample_genotype = "Nipponbare";
                    sample_pertubation = "null";
                    sample_dietcluture = "On wet commercial soil (Bonsol II)";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS07";
                    break;
                case 65:
                    sample_name = "Solanum melongena";
                    sample_category = "Plant";
                    sample_tissueorspecies = "Solanum melongena";
                    sample_genotype = "Solanum melongena";
                    sample_pertubation = "null";
                    sample_dietcluture = "On soil (PROMIX BX)";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 3;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS07";
                    break;
                case 67:
                    sample_name = "Chlamydomonas reinhardti";
                    sample_category = "Algae";
                    sample_tissueorspecies = "Chlamydomonas reinhardti";
                    sample_genotype = "CC125";
                    sample_pertubation = "null";
                    sample_dietcluture = "TAP";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 4;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE127:/MS1";
                    break;
                case 68:
                    sample_name = "Auxenochlorella protothecoides";
                    sample_category = "Algae";
                    sample_tissueorspecies = "Auxenochlorella protothecoides";
                    sample_genotype = "UTEX 2341";
                    sample_pertubation = "null";
                    sample_dietcluture = "N8-NH4 medium";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 4;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE127:/MS1";
                    break;
                case 69:
                    sample_name = "Chlorella sorokiniana";
                    sample_category = "Algae";
                    sample_tissueorspecies = "Chlorella sorokiniana";
                    sample_genotype = "UTEX 2805";
                    sample_pertubation = "null";
                    sample_dietcluture = "N8 medium";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 4;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE127:/MS1";
                    break;
                case 70:
                    sample_name = "Chlorella variabilis";
                    sample_category = "Algae";
                    sample_tissueorspecies = "Chlorella variabilis";
                    sample_genotype = "ATCC NC64A";
                    sample_pertubation = "null";
                    sample_dietcluture = "N8-NH4 medium";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 4;
                    technicalreplicates = 0;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE127:/MS1";
                    break;
                case 71:
                    sample_name = "Dunaliella salina";
                    sample_category = "Algae";
                    sample_tissueorspecies = "Dunaliella salina";
                    sample_genotype = "UTEX LB200";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 0;
                    technicalreplicates = 3;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE127:/MS1";
                    break;
                case 72:
                    sample_name = "Euglena gracilis";
                    sample_category = "Algae";
                    sample_tissueorspecies = "Euglena gracilis";
                    sample_genotype = "UTEX B367";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 0;
                    technicalreplicates = 3;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE127:/MS1";
                    break;
                case 73:
                    sample_name = "Nannochloropsis oculata";
                    sample_category = "Algae";
                    sample_tissueorspecies = "Nannochloropsis oculata";
                    sample_genotype = "UTEX LB2164";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 0;
                    technicalreplicates = 3;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE127:/MS1";
                    break;
                case 74:
                    sample_name = "Pavlova lutheri";
                    sample_category = "Algae";
                    sample_tissueorspecies = "Pavlova lutheri";
                    sample_genotype = "UTEX LB1293";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 0;
                    technicalreplicates = 3;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE127:/MS1";
                    break;
                case 75:
                    sample_name = "Pleurochrysis carterae";
                    sample_category = "Algae";
                    sample_tissueorspecies = "Pleurochrysis carterae";
                    sample_genotype = "UTEX LB1014";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/mg tissue";
                    biologicalreplicates = 0;
                    technicalreplicates = 3;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE127:/MS1";
                    break;
                case 76:
                    sample_name = "Human plasma (SRM 1950)";
                    sample_category = "Human";
                    sample_tissueorspecies = "Plasma";
                    sample_genotype = "SRM 1950";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/uL plasma";
                    biologicalreplicates = 0;
                    technicalreplicates = 5;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS01";
                    break;
                case 77:
                    sample_name = "Human plasma (SRM 1950)";
                    sample_category = "Human";
                    sample_tissueorspecies = "Plasma";
                    sample_genotype = "SRM 1950";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/uL plasma";
                    biologicalreplicates = 0;
                    technicalreplicates = 5;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS02";
                    break;
                case 78:
                    sample_name = "Human plasma (SRM 1950)";
                    sample_category = "Human";
                    sample_tissueorspecies = "Plasma";
                    sample_genotype = "SRM 1950";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/uL plasma";
                    biologicalreplicates = 0;
                    technicalreplicates = 4;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS07";
                    break;
                case 79:
                    sample_name = "Human plasma (SRM 1950)";
                    sample_category = "Human";
                    sample_tissueorspecies = "Plasma";
                    sample_genotype = "SRM 1950";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/uL plasma";
                    biologicalreplicates = 0;
                    technicalreplicates = 4;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS09";
                    break;
                case 80:
                    sample_name = "Human plasma (SRM 1950)";
                    sample_category = "Human";
                    sample_tissueorspecies = "Plasma";
                    sample_genotype = "SRM 1950";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/uL plasma";
                    biologicalreplicates = 0;
                    technicalreplicates = 6;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS10";
                    break;
                case 81:
                    sample_name = "Human plasma (SRM 1950)";
                    sample_category = "Human";
                    sample_tissueorspecies = "Plasma";
                    sample_genotype = "SRM 1950";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/uL plasma";
                    biologicalreplicates = 0;
                    technicalreplicates = 3;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS08";
                    break;
                case 83:
                    sample_name = "Human plasma (SRM 1950)";
                    sample_category = "Human";
                    sample_tissueorspecies = "Plasma";
                    sample_genotype = "SRM 1950";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/uL plasma";
                    biologicalreplicates = 0;
                    technicalreplicates = 5;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS05";
                    break;
                case 84:
                    sample_name = "Human plasma (SRM 1950)";
                    sample_category = "Human";
                    sample_tissueorspecies = "Plasma";
                    sample_genotype = "SRM 1950";
                    sample_pertubation = "null";
                    sample_dietcluture = "null";
                    quantunitNormalized = "pmol/uL plasma";
                    biologicalreplicates = 0;
                    technicalreplicates = 5;
                    methodlink = @"http://metabolonote.kazusa.or.jp/SE196:/MS06";
                    break;
            }

            var inputfilename = System.IO.Path.GetFileNameWithoutExtension(input);
            var outputfile_height_wt = outputdir + "\\" + inputfilename + "_Height_Control.txt";
            var outputfile_norm_wt = outputdir + "\\" + inputfilename + "_Normalized_Control.txt";

            var header = new List<string>() { "Lipid category", "Lipid subclass", "Total chain", "SN1 chain", "SN2 chain", "SN3 chain", "SN4 chain",
                "Formula", "InChIKey", "SMILES", "RT(min)", "m/z", "Quant value", "Standard deviation", "Alignment ID", "Adduct", "MSMS", "Internal standard", "CCS", "Annotation level" };

            using (var sw = new StreamWriter(outputfile_height_wt, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotype);
                sw.WriteLine("Perturbation\t" + sample_pertubation);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitHeight);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.HeightAverage.ToString(), field.HeightStdev.ToString(), field.AlignmentID.ToString(), field.Adduct, field.Spectrum, "null", field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            using (var sw = new StreamWriter(outputfile_norm_wt, false, Encoding.ASCII)) {
                sw.WriteLine("Sample name\t" + sample_name);
                sw.WriteLine("Category\t" + sample_category);
                sw.WriteLine("Tissue/Species\t" + sample_tissueorspecies);
                sw.WriteLine("Genotype/Background\t" + sample_genotype);
                sw.WriteLine("Perturbation\t" + sample_pertubation);
                sw.WriteLine("Diet/Culture\t" + sample_dietcluture);
                sw.WriteLine("Biological replicate\t" + biologicalreplicates);
                sw.WriteLine("Technical replicate\t" + technicalreplicates);
                sw.WriteLine("Quantification unit\t" + quantunitNormalized);
                sw.WriteLine("Date\tJanuary 1th, 2020");
                sw.WriteLine("Method link\t" + methodlink);

                sw.WriteLine(String.Join("\t", header));
                foreach (var field in lipidData) {
                    var ontology = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(field.Ontology));

                    var totalchain = field.Molecule.TotalChainString;
                    var sn1acyl = field.Molecule.Sn1AcylChainString;
                    if (ontology == "BileAcid" || ontology == "SHex" || ontology == "SSulfate" ||
                        ontology == "BAHex" || ontology == "BASulfate" || ontology.Contains("Vitamin")) {
                        totalchain = field.MetaboliteName;
                        sn1acyl = field.MetaboliteName;
                    }
                    var fieldarray = new List<string>() { field.Molecule.LipidCategory, ontology, totalchain, sn1acyl,
                    field.Molecule.Sn2AcylChainString, field.Molecule.Sn3AcylChainString, field.Molecule.Sn4AcylChainString, field.Formula, field.InChIKey, field.SMILES, field.Rt.ToString(),
                    field.Mz.ToString(), field.NormalizedValueAverage.ToString(), field.NormalizedValueStdev.ToString(), field.AlignmentID.ToString(), 
                        field.Adduct, field.Spectrum, field.InternalStandardString, field.Ccs > 0 ? field.Ccs.ToString() : "null", field.Annotation };

                    sw.WriteLine(String.Join("\t", fieldarray));
                }
            }

            
        }

        private static MsdialAlignmetMetaDataField getMsdialAlignmentMetadataField(string[] lineArray, bool patern2, bool patern3) {
            if (lineArray[0] == string.Empty) return null;

            var alignmentID = patern3 ? lineArray[1] : lineArray[0];
            var metname = patern3 ? lineArray[6] : patern2 ? lineArray[5] : lineArray[3];
            var comment = patern3 ? lineArray[23] : patern2 ? lineArray[22] : lineArray[18];
            if (comment.Contains("IS") || metname.Contains("(d") || metname.Contains("(13C") || metname.Contains("SPLASH")) return null;

            var formula = patern3 ? lineArray[14] : patern2 ? lineArray[13] : lineArray[10];
            var ontology = patern3 ? lineArray[15] : patern2 ? lineArray[14] : lineArray[11];
            var inchikey = patern3 ? lineArray[16] : patern2 ? lineArray[15] : lineArray[12];
            var smiles = patern3 ? lineArray[17] : patern2 ? lineArray[16] : lineArray[13];
            var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(metname, ontology);
            var annotationtag = patern3 ? lineArray[18] : patern2 ? lineArray[17] : lineArray[14];

            var rt = patern3 ? lineArray[2] : patern2 ? lineArray[1] : lineArray[1];
            var ccs = patern3 ? lineArray[5] : patern2 ? lineArray[4] : "-1.0";
            var mz = patern3 ? lineArray[3] : patern2 ? lineArray[2] : lineArray[2];
            var adduct = patern3 ? lineArray[7] : patern2 ? lineArray[6] : lineArray[4];
            var ms2string = patern3 ? lineArray[36] : patern2 ? lineArray[35] : lineArray[28];

            var field = new MsdialAlignmetMetaDataField() {
                AlignmentID = int.Parse(alignmentID), MetaboliteName = metname, Comment = comment, Formula = formula, Ontology = ontology, InChIKey = inchikey, SMILES = smiles, Molecule = molecule,
                Annotation = annotationtag, Rt = double.Parse(rt), Ccs = double.Parse(ccs), Mz = double.Parse(mz), Adduct = adduct, Spectrum = ms2string
            };
            return field;
        }

        public static void ExportHumanPlasmaQuantDetail(string inputfolder, string output) {
            var files = System.IO.Directory.GetFiles(inputfolder, "*.tsv");
            var uniquelipids = new List<string>();
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var index = int.Parse(filename.Split('_')[0]);
                if (index != 66 && index != 76 && index != 78 && index != 79 && index != 80) continue;
                var patern2 = false;
                var patern3 = false;
                if (filename.Contains("30_")) patern2 = true;
                if ((index >= 47 && index <= 62) || index == 84) patern3 = true;

                var fileuniquelipids = new List<string>();
                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    for (int i = 0; i < 10; i++) sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        if (lineArray[0] == string.Empty) continue;
                        var metname = patern3 ? lineArray[6] : patern2 ? lineArray[5] : lineArray[3];
                        if (metname.Contains("-SN1")) metname = metname.Replace("-SN1", "");
                        var ontology = patern3 ? lineArray[15] : patern2 ? lineArray[14] : lineArray[11];
                        var annotationtag = patern3 ? lineArray[18] : patern2 ? lineArray[17] : lineArray[14];
                        var comment = patern3 ? lineArray[23] : patern2 ? lineArray[22] : lineArray[18];
                        if (comment.Contains("IS") || metname.Contains("(d") || metname.Contains("(13C") || metname.Contains("SPLASH")) continue;
                        if (annotationtag.Contains("Class")) continue;

                        if (!fileuniquelipids.Contains(metname)) fileuniquelipids.Add(metname);
                        if (!uniquelipids.Contains(metname)) uniquelipids.Add(metname);
                    }
                }

                var outputfile = System.IO.Path.GetDirectoryName(output) + "\\" + filename + "-unique.csv";
                fileuniquelipids.Sort();
                using (var sw = new StreamWriter(outputfile, false, Encoding.ASCII)) {
                    foreach (var lipid in fileuniquelipids) {
                        sw.WriteLine(lipid);
                    }
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var lipid in uniquelipids) {
                    sw.WriteLine(lipid);
                }
            }

            var lipidToExists = new Dictionary<string, List<bool>>();
            foreach (var lipid in uniquelipids) {
                lipidToExists[lipid] = new List<bool>();
                for (int i = 0; i < 5; i++) {
                    lipidToExists[lipid].Add(false);
                }
            }

            var counter = 0;
            var filenames = new List<string>();
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var index = int.Parse(filename.Split('_')[0]);
                if (index != 66 && index != 76 && index != 78 && index != 79 && index != 80) continue;
                var patern2 = false;
                var patern3 = false;
                filenames.Add(filename);
                if (filename.Contains("30_")) patern2 = true;
                if ((index >= 47 && index <= 62) || index == 84) patern3 = true;
                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    for (int i = 0; i < 10; i++) sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        if (lineArray[0] == string.Empty) continue;
                        var metname = patern3 ? lineArray[6] : patern2 ? lineArray[5] : lineArray[3];
                        if (metname.Contains("-SN1")) metname = metname.Replace("-SN1", "");
                        var ontology = patern3 ? lineArray[15] : patern2 ? lineArray[14] : lineArray[11];
                        var annotationtag = patern3 ? lineArray[18] : patern2 ? lineArray[17] : lineArray[14];
                        var comment = patern3 ? lineArray[23] : patern2 ? lineArray[22] : lineArray[18];
                        if (comment.Contains("IS") || metname.Contains("(d") || metname.Contains("(13C") || metname.Contains("SPLASH")) continue;
                        if (annotationtag.Contains("Class")) continue;

                        lipidToExists[metname][counter] = true;
                    }
                }
                counter++;
            }

            uniquelipids.Sort();
            var outputfile2 = System.IO.Path.GetDirectoryName(output) + "\\" + "ExistMatrix.csv";
            using (var sw = new StreamWriter(outputfile2, false, Encoding.ASCII)) {
                sw.WriteLine("Name,Count," + String.Join(",", filenames));
                foreach (var lipid in uniquelipids) {
                    var exists = lipidToExists[lipid];
                    sw.WriteLine(lipid + "," + exists.Count(n => n == true) + "," + String.Join(",", exists.ToArray()));
                }
            }
        }

        public static void ConvertTxtToBoxplotFormat(string input, string output) {
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                using (var sr = new StreamReader(input, Encoding.ASCII)) {
                    var header = sr.ReadLine();
                    var headerArray = header.Split('\t');
                    sw.WriteLine("MetName,Values,Origin");
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        for (int i = 1; i < lineArray.Length; i++) {
                            sw.WriteLine(lineArray[0] + "," + lineArray[i] + "," + headerArray[i]);
                        }
                    }
                }
            }
        }

        public static void CalculateCod(string input, string output) {
            var cods = new List<double>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var lineArray = line.Split('\t');
                    var values = new List<double>();
                    foreach (var value in lineArray) {
                        if (value == "null") continue;
                        values.Add(double.Parse(value));
                    }
                    var median = BasicMathematics.Median(values.ToArray());
                    var mediandevArray = new List<double>();
                    foreach (var value in values) {
                        mediandevArray.Add(Math.Abs(value - median));
                    }
                    var mad = BasicMathematics.Median(mediandevArray.ToArray());
                    var u = Math.Sqrt(0.5 * Math.PI / (double)values.Count()) * 1.483 * mad;
                    var cod = 100.0 * u / median;
                    cods.Add(cod);
                }
            }
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("COD (%)");
                foreach (var cod in cods) {
                    sw.WriteLine(cod);
                }
            }
        }
        public static void ExportHumanPlasmaQuantSummary(string inputfolder, string output) {
            var files = System.IO.Directory.GetFiles(inputfolder, "*.tsv");
            var uniquelipids = new List<string>();
            var classlevelUniqueLipids = new List<string>();
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var index = int.Parse(filename.Split('_')[0]);
                if (index != 76 && index != 77 && index != 78 && index != 79 && index != 80 && index != 81 && index != 83 && index != 84) continue;
                var patern2 = false;
                var patern3 = false;
                if (filename.Contains("30_")) patern2 = true;
                if ((index >= 47 && index <= 62) || index == 84) patern3 = true;

                var fileuniquelipids = new List<string>();
                var fileClassLevelUniquelipids = new List<string>();
                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    for (int i = 0; i < 10; i++) sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        if (lineArray[0] == string.Empty) continue;
                        var metname = patern3 ? lineArray[6] : patern2 ? lineArray[5] : lineArray[3];
                        if (metname.Contains("-SN1")) metname = metname.Replace("-SN1", "");
                        var ontology = patern3 ? lineArray[15] : patern2 ? lineArray[14] : lineArray[11];
                        var annotationtag = patern3 ? lineArray[18] : patern2 ? lineArray[17] : lineArray[14];
                        var comment = patern3 ? lineArray[23] : patern2 ? lineArray[22] : lineArray[18];
                        if (comment.Contains("IS") || metname.Contains("(d") || metname.Contains("(13C") || metname.Contains("SPLASH")) continue;

                        var classlevelName = metname;
                        if (annotationtag.Contains("Class") || annotationtag.Contains("Chain")) {
                            var molecule = LipidomicsConverter.GetLipidMoleculeNameProperties(metname);
                            classlevelName = molecule.SublevelLipidName;
                        }

                        if (!fileClassLevelUniquelipids.Contains(classlevelName)) fileClassLevelUniquelipids.Add(classlevelName);
                        if (!classlevelUniqueLipids.Contains(classlevelName)) classlevelUniqueLipids.Add(classlevelName);

                        if (annotationtag.Contains("Class")) continue;

                        if (!fileuniquelipids.Contains(metname)) fileuniquelipids.Add(metname);
                        if (!uniquelipids.Contains(metname)) uniquelipids.Add(metname); 
                    }
                }

                var outputfileChainLevel = System.IO.Path.GetDirectoryName(output) + "\\" + filename + "-unique-chainlevel.csv";
                fileuniquelipids.Sort();
                using (var sw = new StreamWriter(outputfileChainLevel, false, Encoding.ASCII)) {
                    foreach (var lipid in fileuniquelipids) {
                        sw.WriteLine(lipid);
                    }
                }

                var outputfileClassLevel = System.IO.Path.GetDirectoryName(output) + "\\" + filename + "-unique-classlevel.csv";
                fileClassLevelUniquelipids.Sort();
                using (var sw = new StreamWriter(outputfileClassLevel, false, Encoding.ASCII)) {
                    foreach (var lipid in fileClassLevelUniquelipids) {
                        sw.WriteLine(lipid);
                    }
                }
            }

            var outputfilename = System.IO.Path.GetFileNameWithoutExtension(output);
            var outputDir = System.IO.Path.GetDirectoryName(output);
            var outputChainLevel = outputDir + "\\" + outputfilename + "-unique-chainlevelprop.csv";
           
            uniquelipids.Sort();
            using (var sw = new StreamWriter(outputChainLevel, false, Encoding.ASCII)) {
                foreach (var lipid in uniquelipids) {
                    sw.WriteLine(lipid);
                }
            }

            var outputClassLevel = outputDir + "\\" + outputfilename + "-unique-classlevelprop.csv";
            classlevelUniqueLipids.Sort();
            using (var sw = new StreamWriter(outputClassLevel, false, Encoding.ASCII)) {
                foreach (var lipid in classlevelUniqueLipids) {
                    sw.WriteLine(lipid);
                }
            }

            var lipidToExists = new Dictionary<string, List<bool>>();
            var lipidToAves = new Dictionary<string, List<string[]>>();
            foreach (var lipid in uniquelipids) {
                lipidToExists[lipid] = new List<bool>();
                lipidToAves[lipid] = new List<string[]>();
                for (int i = 0; i < 8; i++) {
                    lipidToExists[lipid].Add(false);
                    lipidToAves[lipid].Add(new string[] { "null", "null" });
                }
            }

            var lipidToExistsAtClassLevel = new Dictionary<string, List<bool>>();
            var lipidToAvesAtClassLevel = new Dictionary<string, List<string[]>>();
            foreach (var lipid in classlevelUniqueLipids) {
                lipidToExistsAtClassLevel[lipid] = new List<bool>();
                lipidToAvesAtClassLevel[lipid] = new List<string[]>();

                for (int i = 0; i < 8; i++) {
                    lipidToExistsAtClassLevel[lipid].Add(false);
                    lipidToAvesAtClassLevel[lipid].Add(new string[] { "null", "null" });
                }
            }

            var counter = 0;
            var filenames = new List<string>();
            foreach (var file in files) {
                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                var index = int.Parse(filename.Split('_')[0]);
                if (index != 76 && index != 77 && index != 78 && index != 79 && index != 80 && index != 81 && index != 83 && index != 84) continue;
                var patern2 = false;
                var patern3 = false;

                var lipidIsStardIndex = 37;
                var valueStartIndex = 39;
                var valueEndIndex = 43;

                if (index == 78) {
                    lipidIsStardIndex = 39;
                    valueStartIndex = 44;
                    valueEndIndex = 47;
                } else if (index == 79 || index == 81) {
                    lipidIsStardIndex = 35;
                    valueStartIndex = 37;
                    valueEndIndex = 39;
                }
                else if (index == 80) {
                    lipidIsStardIndex = 38;
                    valueStartIndex = 40;
                    valueEndIndex = 45;
                }
                else if (index == 83) {
                    lipidIsStardIndex = 37;
                    valueStartIndex = 39;
                    valueEndIndex = 43;
                }
                else if (index == 84) {
                    lipidIsStardIndex = 43;
                    valueStartIndex = 45;
                    valueEndIndex = 49;
                }


                filenames.Add(filename);
                if (filename.Contains("30_")) patern2 = true;
                if ((index >= 47 && index <= 62) || index == 84) patern3 = true;


                var lipidToOriginAtClassLevel = new Dictionary<string, List<string>>();
                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    for (int i = 0; i < 10; i++) sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;
                        var lineArray = line.Split('\t');
                        if (lineArray[0] == string.Empty) continue;
                        var metname = patern3 ? lineArray[6] : patern2 ? lineArray[5] : lineArray[3];
                        if (metname.Contains("-SN1")) metname = metname.Replace("-SN1", "");
                        var ontology = patern3 ? lineArray[15] : patern2 ? lineArray[14] : lineArray[11];
                        var annotationtag = patern3 ? lineArray[18] : patern2 ? lineArray[17] : lineArray[14];
                        var comment = patern3 ? lineArray[23] : patern2 ? lineArray[22] : lineArray[18];
                        if (comment.Contains("IS") || metname.Contains("(d") || metname.Contains("(13C") || metname.Contains("SPLASH")) continue;
                        var isLipidName = lineArray[lipidIsStardIndex];
                        var values = new List<double>();
                        for (int i = valueStartIndex; i <= valueEndIndex; i++) values.Add(double.Parse(lineArray[i]));
                        var average = BasicMathematics.Mean(values.ToArray());
                        var valueString = String.Join(",", values);
                        var classlevelName = metname;
                        if (annotationtag.Contains("Class") || annotationtag.Contains("Chain")) {
                            var molecule = LipidomicsConverter.GetLipidMoleculeNameProperties(metname);
                            classlevelName = molecule.SublevelLipidName;
                        }
                        lipidToExistsAtClassLevel[classlevelName][counter] = true;
                        if (lipidToAvesAtClassLevel[classlevelName][counter][0] != "null" && lipidToAvesAtClassLevel[classlevelName][counter][1] != "null") {
                            var currentValue = double.Parse(lipidToAvesAtClassLevel[classlevelName][counter][1]);
                            if (currentValue < average) {
                                lipidToAvesAtClassLevel[classlevelName][counter][1] = average.ToString();
                                lipidToOriginAtClassLevel[classlevelName][1] = valueString;
                            }
                        }
                        else {



                            lipidToAvesAtClassLevel[classlevelName][counter][0] = isLipidName;
                            lipidToAvesAtClassLevel[classlevelName][counter][1] = average.ToString();

                            lipidToOriginAtClassLevel[classlevelName] = new List<string>() { isLipidName, valueString };
                        }

                        if (annotationtag.Contains("Class")) continue;

                        lipidToExists[metname][counter] = true;
                        if (lipidToAves[metname][counter][0] != "null" && lipidToAves[metname][counter][1] != "null") {
                            var currentValue = double.Parse(lipidToAves[metname][counter][1]);
                            if (currentValue < average) {
                                lipidToAves[metname][counter][1] = average.ToString();
                            }
                        }
                        else {

                            lipidToAves[metname][counter][0] = isLipidName;
                            lipidToAves[metname][counter][1] = average.ToString();
                        }
                    }
                }

                var outputeachfile = outputDir + "\\" + filename + "-class-originaldata.csv"; ;
                using (var sw = new StreamWriter(outputeachfile, false, Encoding.ASCII)) {
                    var headerString = "Name,LipidIS";
                    for (int i = 0; i < valueEndIndex - valueStartIndex + 1; i++) {
                        headerString += ",file_" + (i + 1).ToString();
                    }
                    sw.WriteLine(headerString);
                    foreach (var lipid in lipidToOriginAtClassLevel) {
                        var key = lipid.Key;
                        var values = lipid.Value;
                        var isinfo = values[0];
                        var quants = values[1];

                        sw.WriteLine(key + "," + isinfo + "," + quants);
                    }
                }
                counter++;
            }

            uniquelipids.Sort();
            var outputfileChainMatrix = outputDir + "\\" + outputfilename + "-chain-matrix.csv"; ;
            using (var sw = new StreamWriter(outputfileChainMatrix, false, Encoding.ASCII)) {
                sw.WriteLine("Name,Count," + String.Join(",", filenames));
                foreach (var lipid in uniquelipids) {
                    var exists = lipidToExists[lipid];
                    sw.WriteLine(lipid + "," + exists.Count(n => n == true) + "," + String.Join(",", exists.ToArray()));
                }
            }

            var outputfileChainAveMatrix = outputDir + "\\" + outputfilename + "-chain-ave-matrix.csv"; ;
            using (var sw = new StreamWriter(outputfileChainAveMatrix, false, Encoding.ASCII)) {
                sw.WriteLine("Name,Count," + String.Join(",", filenames) + "," + String.Join(",", filenames));
                foreach (var lipid in uniquelipids) {
                    var exists = lipidToExists[lipid];
                    var values = lipidToAves[lipid];
                    sw.WriteLine(lipid + "," + exists.Count(n => n == true) + "," + String.Join(",", values.Select(n => n[0]).ToArray()) + "," + String.Join(",", values.Select(n => n[1]).ToArray()));
                }
            }

            classlevelUniqueLipids.Sort();
            var outputfileClassMatrix = outputDir + "\\" + outputfilename + "-class-matrix.csv"; ;
            using (var sw = new StreamWriter(outputfileClassMatrix, false, Encoding.ASCII)) {
                sw.WriteLine("Name,Count," + String.Join(",", filenames));
                foreach (var lipid in classlevelUniqueLipids) {
                    var exists = lipidToExistsAtClassLevel[lipid];
                    sw.WriteLine(lipid + "," + exists.Count(n => n == true) + "," + String.Join(",", exists.ToArray()));
                }
            }

            var outputfileClassAveMatrix = outputDir + "\\" + outputfilename + "-class-ave-matrix.csv"; ;
            using (var sw = new StreamWriter(outputfileClassAveMatrix, false, Encoding.ASCII)) {
                sw.WriteLine("Name,Count," + String.Join(",", filenames) + "," + String.Join(",", filenames));
                foreach (var lipid in classlevelUniqueLipids) {
                    var exists = lipidToExistsAtClassLevel[lipid];
                    var values = lipidToAvesAtClassLevel[lipid];
                    sw.WriteLine(lipid + "," + exists.Count(n => n == true) + "," + String.Join(",", values.Select(n => n[0]).ToArray()) + "," + String.Join(",", values.Select(n => n[1]).ToArray()));
                }
            }
        }
    }
}
