using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class MsFinderValidationResultTemp {
        private int id;
        private string formula;
        private string inchikey;
        private string ontology;
        private string carbonCount;
        private string mz;
        private string rt;
        private double score;

        #region
        public int Id {
            get {
                return id;
            }

            set {
                id = value;
            }
        }

        public string Formula {
            get {
                return formula;
            }

            set {
                formula = value;
            }
        }

        public string Inchikey {
            get {
                return inchikey;
            }

            set {
                inchikey = value;
            }
        }

        public string Ontology {
            get {
                return ontology;
            }

            set {
                ontology = value;
            }
        }

        public string CarbonCount {
            get {
                return carbonCount;
            }

            set {
                carbonCount = value;
            }
        }

        public string Mz {
            get {
                return mz;
            }

            set {
                mz = value;
            }
        }

        public string Rt {
            get {
                return rt;
            }

            set {
                rt = value;
            }
        }

        public double Score {
            get {
                return score;
            }

            set {
                score = value;
            }
        }
        #endregion
    }

    public sealed class MsFinderValidation
    {
        public static void ExportPlantSpecializedMetaboliteAnnotationProjectResult(
            MainWindow mainWindow, MainWindowVM mainWindowVM, string filePath) {

            var results = new List<MsFinderValidationResultTemp>(); //[0] alignment ID [1] formula [2] inchikey [3] Ontology
            var files = mainWindowVM.AnalysisFiles;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var error = string.Empty;
            foreach (var rawfile in files) {
                var idString = string.Empty;
                var formulaString = "Unknown";
                var inchikeyString = "Unknown";
                var ontologyString = "Unknown";

                var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                var rawName = System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath);
                //idString = rawName.Split('_')[0];
                //idString = rawfile.RawDataFileName.Split('-')[1];
                //idString = rawData.ScanNumber.ToString();
                idString = rawData.Comment;

                var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }

                formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                if (formulaResults.Count == 1) {
                    formulaString = formulaResults[0].Formula.FormulaString;
                    ontologyString = formulaResults[0].ChemicalOntologyDescriptions.Count > 0 ?
                        formulaResults[0].ChemicalOntologyDescriptions[0] + "_FSEA" : "Unknown";
                }
                else if (formulaResults.Count > 1) {
                    var maxScore = formulaResults[0].TotalScore;
                    formulaString = formulaResults[0].Formula.FormulaString;
                    ontologyString = formulaResults[0].ChemicalOntologyDescriptions.Count > 0 ?
                       formulaResults[0].ChemicalOntologyDescriptions[0] + "_FSEA" : "Unknown";

                    //for (int i = 1; i < formulaResults.Count; i++) {
                    //    if (formulaResults[i].TotalScore + 0.5 < maxScore) break;
                    //    formulaString += "||" + formulaResults[i].Formula.FormulaString;
                    //    ontologyString += formulaResults[i].ChemicalOntologyDescriptions.Count > 0 ?
                    //    "||" + formulaResults[i].ChemicalOntologyDescriptions[0] : "||NA";
                    //}
                }

                var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                var sfdResults = new List<FragmenterResult>();
                var dict_Inchikeys_Formula = new Dictionary<string, string>();

                foreach (var sfdFile in sfdFiles) {
                    var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                    sfdResultMerge(sfdResults, sfdResult);

                    foreach (var result in sfdResult) {
                        if (!dict_Inchikeys_Formula.ContainsKey(result.Inchikey))
                            dict_Inchikeys_Formula[result.Inchikey] = System.IO.Path.GetFileNameWithoutExtension(sfdFile);
                    }
                }

                if (sfdResults.Count > 0) {
                    sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();
                    ontologyString = string.Empty;
                }

                if (sfdResults.Count == 1) {
                    inchikeyString = sfdResults[0].Inchikey;
                    ontologyString = sfdResults[0].Ontology;
                    formulaString = dict_Inchikeys_Formula[inchikeyString];
                }
                else if (sfdResults.Count > 1) {
                    inchikeyString = sfdResults[0].Inchikey;
                    ontologyString = sfdResults[0].Ontology;
                    formulaString = dict_Inchikeys_Formula[inchikeyString];
                    //var maxScore = sfdResults[0].TotalScore;
                    //for (int i = 1; i < sfdResults.Count; i++) {
                    //    if (sfdResults[i].TotalScore + 0.05 < maxScore) break;
                    //    inchikeyString += "||" + sfdResults[i].Inchikey;
                    //    ontologyString += sfdResults[i].Ontology != string.Empty ? "||" + sfdResults[i].Ontology : "||" + "NA";
                    //}
                }

                var tempResult = new MsFinderValidationResultTemp() {
                    Id = int.Parse(idString),
                    CarbonCount = rawData.CarbonNumberFromLabeledExperiment.ToString(),
                    Formula = formulaString,
                    Inchikey = inchikeyString,
                    Ontology = ontologyString
                };
                results.Add(tempResult);
            }

            //results = results.OrderBy(n => n.Id).ToList();
            results = results.OrderBy(n => n.Id).ToList();

            using (var sw = new StreamWriter(filePath, false, Encoding.ASCII)) {

                sw.WriteLine("Alignment ID\tCarbon count\tFormula\tOntology\tInChIKey");
                foreach (var result in results) {
                    sw.WriteLine(result.Id + "\t" + result.CarbonCount + "\t" + result.Formula + "\t" + result.Ontology + "\t" + result.Inchikey);
                }
            }
        }


        public static void MassBankTestResult(MainWindow mainWindow, MainWindowVM mainWindowVM, string filePath)
        {
            StructureResultExportTemp(mainWindow, mainWindowVM, filePath);
        }

        public static void FseaTestResult(MainWindow mainWindow, MainWindowVM mainWindowVM, string filePath, string ontologylistfilepath, string ionMode) {
            FseaResultExport(mainWindow, mainWindowVM, filePath, ontologylistfilepath, ionMode);
        }

        public static void FormulaAccuracyTest(MainWindow mainWindow, MainWindowVM mainWindowVM, string filePath) {
            FormulaResultExportTemp(mainWindow, mainWindowVM, filePath, false);

            var filename = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var filepathCfixed = System.IO.Path.GetDirectoryName(filePath) + "\\" + filename + "-carbonfixed.txt";
            FormulaResultExportTemp(mainWindow, mainWindowVM, filepathCfixed, true);
        }

       
        public static void FormulaVariablesTestResult(MainWindow mainWindow, MainWindowVM mainWindowVM, string filePath)
        {
            using (var sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                writeFormulaVariablesHeader(sw);

                var files = mainWindowVM.AnalysisFiles;
                var param = mainWindowVM.DataStorageBean.AnalysisParameter;
                var error = string.Empty;
                foreach (var rawfile in files)
                {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    if (rawData.InchiKey == null || rawData.InchiKey == string.Empty) rawData.InchiKey = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 1].Trim();
                    if (rawData.Formula == null || rawData.Formula == string.Empty) rawData.Formula = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 2].Trim();

                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }
                    formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                    int formulaRanking = getFormulaRanking(rawData.Formula, formulaResults);

                    writeResult(sw, rawData, formulaResults, formulaRanking);
                }
            }
        }

        public static void TopFiveFormulaResult(MainWindow mainWindow, MainWindowVM mainWindowVM, string filepath)
        {
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                sw.WriteLine("Database reported\tTotal");

                var files = mainWindowVM.AnalysisFiles;
                var param = mainWindowVM.DataStorageBean.AnalysisParameter;

                var databaseReported = 0;
                var total = 0;

                var error = string.Empty;

                foreach (var rawfile in files)
                {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    if (rawData.InchiKey == null || rawData.InchiKey == string.Empty) rawData.InchiKey = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 1].Trim();
                    if (rawData.Formula == null || rawData.Formula == string.Empty) rawData.Formula = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 2].Trim();

                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }

                    formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();

                    for (int i = 0; i < formulaResults.Count; i++)
                    {
                        if (i > 4) break;
                        total++;
                        if (formulaResults[i].ResourceRecords > 0) databaseReported++;
                    }
                }
                sw.WriteLine(databaseReported + "\t" + total);
            }
        }

        public static void PeakAssignmentResultExport(MainWindow mainWindow, MainWindowVM mainWindowVM, string filePath)
        {
            using (var sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                writePeakAssignmentHeader(sw);

                var files = mainWindowVM.AnalysisFiles;
                var param = mainWindowVM.DataStorageBean.AnalysisParameter;
                var error = string.Empty;

                foreach (var rawfile in files)
                {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    if (rawData.InchiKey == null || rawData.InchiKey == string.Empty) rawData.InchiKey = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 1].Trim();
                    if (rawData.Formula == null || rawData.Formula == string.Empty) rawData.Formula = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 2].Trim();

                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }
                    formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                    int formulaRanking = getFormulaRanking(rawData.Formula, formulaResults);

                    var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                    var sfdResults = new List<FragmenterResult>();

                    foreach (var sfdFile in sfdFiles)
                    {
                        var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                        sfdResultMerge(sfdResults, sfdResult);
                    }
                    sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();

                    int structureRanking = getStructureRanking(rawData.InchiKey, sfdResults);
                    writeResult(sw, rawData, formulaRanking, formulaResults, structureRanking, sfdResults, param);
                }
            }
        }

        private static void writeResult(StreamWriter sw, RawData rawData, int formulaRanking, List<FormulaResult> formulaResults, int structureRanking, List<FragmenterResult> sfdResults, AnalysisParamOfMsfinder param)
        {
            var spectrum = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
            var refinedSpectrum = FragmentAssigner.GetRefinedPeaklist(spectrum, param.RelativeAbundanceCutOff, rawData.PrecursorMz, param.Mass2Tolerance, param.MassTolType);
            var formulaResult = formulaResults[0];
            var productIonResult = formulaResult.ProductIonResult;

            var specNum = 0;
            foreach (var spec in refinedSpectrum)
            {
                if (spec.Comment == "M") specNum++;
            }

            for (int i = 0; i < sfdResults.Count; i++)
            {
                if (i + 1 != structureRanking) continue;

                sw.Write(rawData.RawdataFilePath + "\t");
                sw.Write(structureRanking + "\t");
                sw.Write(rawData.Name + "\t");
                sw.Write(rawData.PrecursorMz + "\t");
                sw.Write(rawData.PrecursorType + "\t");
                sw.Write(rawData.InstrumentType + "\t");
                sw.Write(rawData.Instrument + "\t");
                sw.Write(rawData.Authors + "\t");
                sw.Write(rawData.License + "\t");
                sw.Write(rawData.Formula + "\t");
                sw.Write(rawData.Smiles + "\t");
                sw.Write(rawData.Inchi + "\t");
                sw.Write(rawData.InchiKey + "\t");
                sw.Write(rawData.IonMode + "\t");
                sw.Write(rawData.CollisionEnergy + "\t");
                sw.Write(specNum + "\t");

                int p_f_C_0 = 0, p_f_C_1 = 0, p_f_C_2 = 0, p_f_N_0 = 0, p_f_N_1 = 0, p_f_N_2 = 0, p_f_O_0 = 0, p_f_O_1 = 0, p_f_O_2 = 0, p_f_S_0 = 0, p_f_S_1 = 0, p_f_S_2 = 0, p_f_P_0 = 0, p_f_P_1 = 0, p_f_P_2 = 0,
                    p_s_C_0 = 0, p_s_C_1 = 0, p_s_C_2 = 0, p_s_N_0 = 0, p_s_N_1 = 0, p_s_N_2 = 0, p_s_O_0 = 0, p_s_O_1 = 0, p_s_O_2 = 0, p_s_S_0 = 0, p_s_S_1 = 0, p_s_S_2 = 0, p_s_P_0 = 0, p_s_P_1 = 0, p_s_P_2 = 0,
                    n_f_C_0 = 0, n_f_C_1 = 0, n_f_C_2 = 0, n_f_N_0 = 0, n_f_N_1 = 0, n_f_N_2 = 0, n_f_O_0 = 0, n_f_O_1 = 0, n_f_O_2 = 0, n_f_S_0 = 0, n_f_S_1 = 0, n_f_S_2 = 0, n_f_P_0 = 0, n_f_P_1 = 0, n_f_P_2 = 0,
                    n_s_C_0 = 0, n_s_C_1 = 0, n_s_C_2 = 0, n_s_N_0 = 0, n_s_N_1 = 0, n_s_N_2 = 0, n_s_O_0 = 0, n_s_O_1 = 0, n_s_O_2 = 0, n_s_S_0 = 0, n_s_S_1 = 0, n_s_S_2 = 0, n_s_P_0 = 0, n_s_P_1 = 0, n_s_P_2 = 0; 

                int outOfRule = 0;

                int noAssignFormulaYes = 0, noAssignFormulaNo = 0, assignCounter = 0, ruleBasedAssignment = 0;

                for (int j = 0; j < refinedSpectrum.Count; j++)
                {
                    if (refinedSpectrum[j].Comment != "M") continue;

                    var mz = refinedSpectrum[j].Mz;
                    var flg = false;
                    foreach (var frag in sfdResults[i].FragmentPics)
                    {
                        if (Math.Abs(frag.Peak.Mz - mz) < 0.00001)
                        {
                            flg = true;
                            //var stringArray = frag.SuggestedRuleCombination.Split(';').ToArray();
                            var stringArray = frag.MatchedFragmentInfo.Smiles.Split(';').ToArray();

                            for (int k = 0; k < stringArray.Length - 1; k++)
                            {
                                #region
                                var elemAndNum = stringArray[k].Split('_').ToArray();
                                if (k == 0 && stringArray.Length == 2)
                                {
                                    if (rawData.IonMode == IonMode.Positive)
                                    {
                                        if (elemAndNum[0] == "C" && elemAndNum[1] == "-2") p_f_C_0++;
                                        else if (elemAndNum[0] == "C" && elemAndNum[1] == "-1") p_f_C_1++;
                                        else if (elemAndNum[0] == "C" && elemAndNum[1] == "0") p_f_C_2++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "-2") p_f_N_0++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "-1") p_f_N_1++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "0") p_f_N_2++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "-2") p_f_O_0++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "-1") p_f_O_1++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "0") p_f_O_2++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "-2") p_f_S_0++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "-1") p_f_S_1++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "0") p_f_S_2++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "-2") p_f_P_0++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "-1") p_f_P_1++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "0") p_f_P_2++;
                                    }
                                    else
                                    {
                                        if (elemAndNum[0] == "C" && elemAndNum[1] == "0") n_f_C_0++;
                                        else if (elemAndNum[0] == "C" && elemAndNum[1] == "-1") n_f_C_1++;
                                        else if (elemAndNum[0] == "C" && elemAndNum[1] == "-2") n_f_C_2++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "0") n_f_N_0++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "-1") n_f_N_1++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "-2") n_f_N_2++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "0") n_f_O_0++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "-1") n_f_O_1++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "-2") n_f_O_2++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "0") n_f_S_0++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "-1") n_f_S_1++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "-2") n_f_S_2++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "0") n_f_P_0++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "-1") n_f_P_1++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "-2") n_f_P_2++;
                                    }
                                }
                                else if (k > 0)
                                {
                                    if (rawData.IonMode == IonMode.Positive)
                                    {
                                        if (elemAndNum[0] == "C" && elemAndNum[1] == "0") p_s_C_2++;
                                        else if (elemAndNum[0] == "C" && elemAndNum[1] == "-1") p_s_C_1++;
                                        else if (elemAndNum[0] == "C" && elemAndNum[1] == "-2") p_s_C_0++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "0") p_s_N_2++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "-1") p_s_N_1++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "-2") p_s_N_0++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "0") p_s_O_2++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "-1") p_s_O_1++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "-2") p_s_O_0++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "0") p_s_S_2++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "-1") p_s_S_1++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "-2") p_s_S_0++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "0") p_s_P_2++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "-1") p_s_P_1++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "-2") p_s_P_0++;
                                    }
                                    else
                                    {
                                        if (elemAndNum[0] == "C" && elemAndNum[1] == "0") n_s_C_0++;
                                        else if (elemAndNum[0] == "C" && elemAndNum[1] == "-1") n_s_C_1++;
                                        else if (elemAndNum[0] == "C" && elemAndNum[1] == "-2") n_s_C_2++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "0") n_s_N_0++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "-1") n_s_N_1++;
                                        else if (elemAndNum[0] == "N" && elemAndNum[1] == "-2") n_s_N_2++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "0") n_s_O_0++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "-1") n_s_O_1++;
                                        else if (elemAndNum[0] == "O" && elemAndNum[1] == "-2") n_s_O_2++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "0") n_s_S_0++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "-1") n_s_S_1++;
                                        else if (elemAndNum[0] == "S" && elemAndNum[1] == "-2") n_s_S_2++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "0") n_s_P_0++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "-1") n_s_P_1++;
                                        else if (elemAndNum[0] == "P" && elemAndNum[1] == "-2") n_s_P_2++;
                                    }
                                }
                                #endregion
                            }

                            if (frag.MatchedFragmentInfo.IsHrRule == true)
                            {
                                ruleBasedAssignment++;
                            }
                            else
                            {
                                outOfRule++;
                            }
                            assignCounter++;
                            break;
                        }
                    }

                    if (flg == true) continue;
                    if (productIonResult.Count == 0) continue;

                    foreach (var product in productIonResult)
                    {
                        if (Math.Abs(product.Mass - mz) < 0.00001)
                        {
                            noAssignFormulaYes++;
                            break;
                        }
                    }
                }

                noAssignFormulaNo = specNum - noAssignFormulaYes - assignCounter;

                sw.Write(p_f_C_0 + "\t");
                sw.Write(p_f_C_1 + "\t");
                sw.Write(p_f_C_2 + "\t");

                sw.Write(p_f_N_0 + "\t");
                sw.Write(p_f_N_1 + "\t");
                sw.Write(p_f_N_2 + "\t");

                sw.Write(p_f_O_0 + "\t");
                sw.Write(p_f_O_1 + "\t");
                sw.Write(p_f_O_2 + "\t");

                sw.Write(p_f_S_0 + "\t");
                sw.Write(p_f_S_1 + "\t");
                sw.Write(p_f_S_2 + "\t");

                sw.Write(p_f_P_0 + "\t");
                sw.Write(p_f_P_1 + "\t");
                sw.Write(p_f_P_2 + "\t");

                sw.Write(p_s_C_0 + "\t");
                sw.Write(p_s_C_1 + "\t");
                sw.Write(p_s_C_2 + "\t");

                sw.Write(p_s_N_0 + "\t");
                sw.Write(p_s_N_1 + "\t");
                sw.Write(p_s_N_2 + "\t");

                sw.Write(p_s_O_0 + "\t");
                sw.Write(p_s_O_1 + "\t");
                sw.Write(p_s_O_2 + "\t");

                sw.Write(p_s_S_0 + "\t");
                sw.Write(p_s_S_1 + "\t");
                sw.Write(p_s_S_2 + "\t");

                sw.Write(p_s_P_0 + "\t");
                sw.Write(p_s_P_1 + "\t");
                sw.Write(p_s_P_2 + "\t");

                sw.Write(n_f_C_0 + "\t");
                sw.Write(n_f_C_1 + "\t");
                sw.Write(n_f_C_2 + "\t");

                sw.Write(n_f_N_0 + "\t");
                sw.Write(n_f_N_1 + "\t");
                sw.Write(n_f_N_2 + "\t");

                sw.Write(n_f_O_0 + "\t");
                sw.Write(n_f_O_1 + "\t");
                sw.Write(n_f_O_2 + "\t");

                sw.Write(n_f_S_0 + "\t");
                sw.Write(n_f_S_1 + "\t");
                sw.Write(n_f_S_2 + "\t");

                sw.Write(n_f_P_0 + "\t");
                sw.Write(n_f_P_1 + "\t");
                sw.Write(n_f_P_2 + "\t");

                sw.Write(n_s_C_0 + "\t");
                sw.Write(n_s_C_1 + "\t");
                sw.Write(n_s_C_2 + "\t");

                sw.Write(n_s_N_0 + "\t");
                sw.Write(n_s_N_1 + "\t");
                sw.Write(n_s_N_2 + "\t");

                sw.Write(n_s_O_0 + "\t");
                sw.Write(n_s_O_1 + "\t");
                sw.Write(n_s_O_2 + "\t");

                sw.Write(n_s_S_0 + "\t");
                sw.Write(n_s_S_1 + "\t");
                sw.Write(n_s_S_2 + "\t");

                sw.Write(n_s_P_0 + "\t");
                sw.Write(n_s_P_1 + "\t");
                sw.Write(n_s_P_2 + "\t");


                //sw.Write(rule1_C_O + "\t");
                
                //sw.Write(rule2_N_p2 + "\t");
                //sw.Write(rule2_O_p2 + "\t");
                
                //sw.Write(rule3_S_0 + "\t");
                //sw.Write(rule3_S_p2 + "\t");
                //sw.Write(rule3_P_0 + "\t");
                //sw.Write(rule3_P_p2 + "\t");
                
                //sw.Write(rule4_C_p1 + "\t");
                //sw.Write(rule4_C_m1 + "\t");
                //sw.Write(rule4_S_p1 + "\t");
                //sw.Write(rule4_S_m1 + "\t");
                //sw.Write(rule4_P_p1 + "\t");
                //sw.Write(rule4_P_m1 + "\t");
                
                //sw.Write(rule5_N_p1 + "\t");
                //sw.Write(rule5_O_p1 + "\t");
                
                //sw.Write(rule6_C_0 + "\t");
                //sw.Write(rule6_C_m2 + "\t");
                //sw.Write(rule6_P_0 + "\t");
                //sw.Write(rule6_P_m2 + "\t");
               
                //sw.Write(rule7_N_0 + "\t");
                //sw.Write(rule7_O_0 + "\t");
                
                //sw.Write(rule8_S_0 + "\t");
                //sw.Write(rule8_S_m1 + "\t");
                
                //sw.Write(rule9_N_p1 + "\t");
                //sw.Write(rule9_O_p1 + "\t");
                //sw.Write(rule9_S_p1 + "\t");
                
                //sw.Write(rule10_C_p1 + "\t");
                //sw.Write(rule10_C_m1 + "\t");

                //sw.Write(rule10_P_p1 + "\t");
                //sw.Write(rule10_P_m1 + "\t");


                sw.Write(ruleBasedAssignment + "\t");
                sw.Write(outOfRule + "\t");

                sw.Write(noAssignFormulaYes + "\t");
                sw.Write(noAssignFormulaNo + "\t");

                sw.Write(formulaResult.Formula.Cnum + "\t");
                sw.Write(formulaResult.Formula.Nnum + "\t");
                sw.Write(formulaResult.Formula.Onum + "\t");
                sw.Write(formulaResult.Formula.Snum + "\t");
                sw.Write(formulaResult.Formula.Pnum + "\t");

                sw.WriteLine(rawData.SpectrumType);
            }
        }

        private static void writePeakAssignmentHeader(StreamWriter sw)
        {
            sw.Write("File\t");
            sw.Write("StructureRanking\t");
            sw.Write("NAME\t");
            sw.Write("PRECURSORMZ\t");
            sw.Write("PRECURSORTYPE\t");
            sw.Write("INSTRUMENTTYPE\t");
            sw.Write("INSTRUMENT\t");
            sw.Write("Authors\t");
            sw.Write("License\t");
            sw.Write("FORMULA\t");
            sw.Write("SMILES\t");
            sw.Write("INCHI\t");
            sw.Write("INCHIKEY\t");
            sw.Write("IONTYPE\t");
            sw.Write("COLLISIONENERGY\t");
            sw.Write("Peak Num\t");

            sw.Write("First bond cleavage, Pos, C, 0\t");
            sw.Write("First bond cleavage, Pos, C, +1\t");
            sw.Write("First bond cleavage, Pos, C, +2\t");

            sw.Write("First bond cleavage, Pos, N, 0\t");
            sw.Write("First bond cleavage, Pos, N, +1\t");
            sw.Write("First bond cleavage, Pos, N, +2\t");

            sw.Write("First bond cleavage, Pos, O, 0\t");
            sw.Write("First bond cleavage, Pos, O, +1\t");
            sw.Write("First bond cleavage, Pos, O, +2\t");

            sw.Write("First bond cleavage, Pos, S, 0\t");
            sw.Write("First bond cleavage, Pos, S, +1\t");
            sw.Write("First bond cleavage, Pos, S, +2\t");

            sw.Write("First bond cleavage, Pos, P, 0\t");
            sw.Write("First bond cleavage, Pos, P, +1\t");
            sw.Write("First bond cleavage, Pos, P, +2\t");

            sw.Write("Multi bond cleavage, Pos, C, 0\t");
            sw.Write("Multi bond cleavage, Pos, C, +1\t");
            sw.Write("Multi bond cleavage, Pos, C, +2\t");

            sw.Write("Multi bond cleavage, Pos, N, 0\t");
            sw.Write("Multi bond cleavage, Pos, N, +1\t");
            sw.Write("Multi bond cleavage, Pos, N, +2\t");

            sw.Write("Multi bond cleavage, Pos, O, 0\t");
            sw.Write("Multi bond cleavage, Pos, O, +1\t");
            sw.Write("Multi bond cleavage, Pos, O, +2\t");

            sw.Write("Multi bond cleavage, Pos, S, 0\t");
            sw.Write("Multi bond cleavage, Pos, S, +1\t");
            sw.Write("Multi bond cleavage, Pos, S, +2\t");

            sw.Write("Multi bond cleavage, Pos, P, 0\t");
            sw.Write("Multi bond cleavage, Pos, P, +1\t");
            sw.Write("Multi bond cleavage, Pos, P, +2\t");

            //sw.Write("Other bond cleavage, Pos, C, +1\t");
            //sw.Write("Other bond cleavage, Pos, C, 0\t");
            //sw.Write("Other bond cleavage, Pos, C, -1\t");

            //sw.Write("Other bond cleavage, Pos, N, +1\t");
            //sw.Write("Other bond cleavage, Pos, N, 0\t");
            //sw.Write("Other bond cleavage, Pos, N, -1\t");

            //sw.Write("Other bond cleavage, Pos, O, +1\t");
            //sw.Write("Other bond cleavage, Pos, O, 0\t");
            //sw.Write("Other bond cleavage, Pos, O, -1\t");

            //sw.Write("Other bond cleavage, Pos, S, +1\t");
            //sw.Write("Other bond cleavage, Pos, S, 0\t");
            //sw.Write("Other bond cleavage, Pos, S, -1\t");

            //sw.Write("Other bond cleavage, Pos, P, +1\t");
            //sw.Write("Other bond cleavage, Pos, P, 0\t");
            //sw.Write("Other bond cleavage, Pos, P, -1\t");

            sw.Write("First bond cleavage, Neg, C, 0\t");
            sw.Write("First bond cleavage, Neg, C, -1\t");
            sw.Write("First bond cleavage, Neg, C, -2\t");

            sw.Write("First bond cleavage, Neg, N, 0\t");
            sw.Write("First bond cleavage, Neg, N, -1\t");
            sw.Write("First bond cleavage, Neg, N, -2\t");

            sw.Write("First bond cleavage, Neg, O, 0\t");
            sw.Write("First bond cleavage, Neg, O, -1\t");
            sw.Write("First bond cleavage, Neg, O, -2\t");

            sw.Write("First bond cleavage, Neg, S, 0\t");
            sw.Write("First bond cleavage, Neg, S, -1\t");
            sw.Write("First bond cleavage, Neg, S, -2\t");

            sw.Write("First bond cleavage, Neg, P, 0\t");
            sw.Write("First bond cleavage, Neg, P, -1\t");
            sw.Write("First bond cleavage, Neg, P, -2\t");

            //sw.Write("Other bond cleavage, Neg, C, +1\t");
            //sw.Write("Other bond cleavage, Neg, C, 0\t");
            //sw.Write("Other bond cleavage, Neg, C, -1\t");

            //sw.Write("Other bond cleavage, Neg, N, +1\t");
            //sw.Write("Other bond cleavage, Neg, N, 0\t");
            //sw.Write("Other bond cleavage, Neg, N, -1\t");

            //sw.Write("Other bond cleavage, Neg, O, +1\t");
            //sw.Write("Other bond cleavage, Neg, O, 0\t");
            //sw.Write("Other bond cleavage, Neg, O, -1\t");

            //sw.Write("Other bond cleavage, Neg, S, +1\t");
            //sw.Write("Other bond cleavage, Neg, S, 0\t");
            //sw.Write("Other bond cleavage, Neg, S, -1\t");

            //sw.Write("Other bond cleavage, Neg, P, +1\t");
            //sw.Write("Other bond cleavage, Neg, P, 0\t");
            //sw.Write("Other bond cleavage, Neg, P, -1\t");

            sw.Write("Multi bond cleavage, Neg, C, 0\t");
            sw.Write("Multi bond cleavage, Neg, C, -1\t");
            sw.Write("Multi bond cleavage, Neg, C, -2\t");

            sw.Write("Multi bond cleavage, Neg, N, 0\t");
            sw.Write("Multi bond cleavage, Neg, N, -1\t");
            sw.Write("Multi bond cleavage, Neg, N, -2\t");

            sw.Write("Multi bond cleavage, Neg, O, 0\t");
            sw.Write("Multi bond cleavage, Neg, O, -1\t");
            sw.Write("Multi bond cleavage, Neg, O, -2\t");

            sw.Write("Multi bond cleavage, Neg, S, 0\t");
            sw.Write("Multi bond cleavage, Neg, S, -1\t");
            sw.Write("Multi bond cleavage, Neg, S, -2\t");

            sw.Write("Multi bond cleavage, Neg, P, 0\t");
            sw.Write("Multi bond cleavage, Neg, P, -1\t");
            sw.Write("Multi bond cleavage, Neg, P, -2\t");

            //sw.Write("Rule 1, C, 0\t");
            //sw.Write("Rule 2, N, +2\t");
            //sw.Write("Rule 2, O, +2\t");
            //sw.Write("Rule 3, S, 0\t");
            //sw.Write("Rule 3, S, +2\t");
            //sw.Write("Rule 3, P, 0\t");
            //sw.Write("Rule 3, P, +2\t");
            //sw.Write("Rule 4, C, +1\t");
            //sw.Write("Rule 4, C, -1\t");
            //sw.Write("Rule 4, S, +1\t");
            //sw.Write("Rule 4, S, -1\t");
            //sw.Write("Rule 4, P, +1\t");
            //sw.Write("Rule 4, P, -1\t");
            //sw.Write("Rule 5, N, +1\t");
            //sw.Write("Rule 5, O, +1\t");
            //sw.Write("Rule 6, C, 0\t");
            //sw.Write("Rule 6, C, -2\t");
            //sw.Write("Rule 6, P, 0\t");
            //sw.Write("Rule 6, P, -2\t");
            //sw.Write("Rule 7, N, 0\t");
            //sw.Write("Rule 7, O, 0\t");
            //sw.Write("Rule 8, S, 0\t");
            //sw.Write("Rule 8, S, -1\t");
            //sw.Write("Rule 9, N, +1\t");
            //sw.Write("Rule 9, O, +1\t");
            //sw.Write("Rule 9, S, +1\t");
            //sw.Write("Rule 10, C, +1\t");
            //sw.Write("Rule 10, C, -1\t");
            //sw.Write("Rule 10, P, +1\t");
            //sw.Write("Rule 10, P, -1\t");

            sw.Write("Assigned: Rule-based\t");
            sw.Write("Assigned: Out of rules\t");
            sw.Write("No assignment (Formula Yes)\t");
            sw.Write("No assignment (Formula No)\t");
            sw.Write("C number\t");
            sw.Write("N number\t");
            sw.Write("O number\t");
            sw.Write("S number\t");
            sw.Write("P number\t");
            sw.WriteLine("SPECTRUMTYPE");
        }

        private static void writeFormulaVariablesHeader(StreamWriter sw)
        {
            sw.Write("File\t");
            sw.Write("FormulaRanking\t");
            sw.Write("CORRECT\t");
            sw.Write("MSMS\t");
            sw.Write("NAME\t");
            sw.Write("PRECURSORMZ\t");
            sw.Write("PRECURSORTYPE\t");
            sw.Write("INSTRUMENTTYPE\t");
            sw.Write("INSTRUMENT\t");
            sw.Write("Authors\t");
            sw.Write("License\t");
            sw.Write("FORMULA\t");
            sw.Write("SMILES\t");
            sw.Write("INCHI\t");
            sw.Write("INCHIKEY\t");
            sw.Write("IONTYPE\t");
            sw.Write("COLLISIONENERGY\t");
            sw.Write("SPECTRUMTYPE\t");
            sw.Write("Result formula\t");
            sw.Write("MatchedMass\t");
            sw.Write("MassDiff\t");
            sw.Write("M1IsotopicIntensity\t");
            sw.Write("M2IsotopicIntensity\t");
            sw.Write("M1IsotopicDiff\t");
            sw.Write("M2IsotopicDiff\t");
            sw.Write("MassDiffScore\t");
            sw.Write("IsotopicScore\t");
            sw.Write("ProductIonScore\t");
            sw.Write("NeutralLossScore\t");
            sw.Write("ResourceScore\t");
            sw.Write("TotalScore\t");
            sw.Write("NeutralLossHits\t");
            sw.Write("NeutralLossNum\t");
            sw.Write("ResourceNames\t");
            sw.WriteLine("ResourceRecords");
        }

        private static void writeResult(StreamWriter sw, RawData rawData, List<FormulaResult> formulaResults, int formulaRanking)
        {
            if (formulaRanking < 1) return;

            for (int i = 0; i < formulaResults.Count; i++)
            {
                sw.Write(rawData.RawdataFilePath + "\t");
                sw.Write(formulaRanking + "\t");
                if (i + 1 == formulaRanking) sw.Write("TRUE" + "\t"); else sw.Write("FALSE" + "\t");
                if (rawData.Ms2PeakNumber > 0) sw.Write("TRUE" + "\t"); else sw.Write("FALSE" + "\t");
                sw.Write(rawData.Name + "\t");
                sw.Write(rawData.PrecursorMz + "\t");
                sw.Write(rawData.PrecursorType + "\t");
                sw.Write(rawData.InstrumentType + "\t");
                sw.Write(rawData.Instrument + "\t");
                sw.Write(rawData.Authors + "\t");
                sw.Write(rawData.License + "\t");
                sw.Write(rawData.Formula + "\t");
                sw.Write(rawData.Smiles + "\t");
                sw.Write(rawData.Inchi + "\t");
                sw.Write(rawData.InchiKey + "\t");
                sw.Write(rawData.IonMode + "\t");
                sw.Write(rawData.CollisionEnergy + "\t");
                sw.Write(rawData.SpectrumType + "\t");

                var result = formulaResults[i];

                sw.Write(result.Formula.FormulaString + "\t");
                sw.Write(result.MatchedMass + "\t");
                sw.Write(result.MassDiff + "\t");
                sw.Write(result.M1IsotopicIntensity + "\t");
                sw.Write(result.M2IsotopicIntensity + "\t");
                sw.Write(result.M1IsotopicDiff + "\t");
                sw.Write(result.M2IsotopicDiff + "\t");
                sw.Write(result.MassDiffScore + "\t");
                sw.Write(result.IsotopicScore + "\t");
                sw.Write(result.ProductIonScore + "\t");
                sw.Write(result.NeutralLossScore + "\t");
                sw.Write(result.ResourceScore + "\t");
                sw.Write(result.TotalScore + "\t");
                sw.Write(result.NeutralLossHits + "\t");
                sw.Write(result.NeutralLossNum + "\t");
                sw.Write(result.ResourceNames + "\t");
                sw.WriteLine(result.ResourceRecords + "\t");

            }
        }

        public static void CasmiResultExport(MainWindow mainWindow, MainWindowVM mainWindowVM, string exportFolder)
        {
            var files = mainWindowVM.AnalysisFiles;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;

            for (int i = 1; i <= 243; i++) {
                var challengeString = getCasmiChallengeString(i);
                var rawFiles = new List<MsfinderQueryFile>();

                foreach (var rawfile in files) {
                    if (rawfile.RawDataFileName.Contains(challengeString))
                        rawFiles.Add(rawfile);
                }

                var sfdResults = new List<FragmenterResult>();
                foreach (var rawfile in rawFiles) {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);

                    foreach (var sfdFile in sfdFiles) {
                        var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                        sfdResultMerge(sfdResults, sfdResult);
                    }
                }
                sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();
                if (sfdResults.Count == 0) continue;

                var exportfile = exportFolder + "\\" + "TsugawaYamamoto-category3-" + challengeString + ".txt";
                using (var sw = new StreamWriter(exportfile, false, Encoding.ASCII)) {
                    foreach (var result in sfdResults) {
                        sw.WriteLine(result.Smiles + "\t" + Math.Round(result.TotalScore * 10, 3));
                    }
                }
            }
        }

        private static string getCasmiChallengeString(int number)
        {
            if (number < 10)
                return "challenge-00" + number.ToString();
            else if (number < 100)
                return "challenge-0" + number.ToString();
            else
                return "challenge-" + number.ToString();
        }

        public static void ScoreExport(MainWindow mainWindow, MainWindowVM mainWindowVM, string exportFile) {
            using (var sw = new StreamWriter(exportFile, false, Encoding.ASCII)) {

                writeScoreHeader(sw);

                var files = mainWindowVM.AnalysisFiles;
                var param = mainWindowVM.DataStorageBean.AnalysisParameter;
                var error = string.Empty;

                foreach (var rawfile in files) {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    if (rawData.InchiKey == null || rawData.InchiKey == string.Empty) rawData.InchiKey = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 1].Trim();
                    if (rawData.Formula == null || rawData.Formula == string.Empty) rawData.Formula = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 2].Trim();

                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }
                    formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                    int formulaRanking = getFormulaRanking(rawData.Formula, formulaResults);
                    int formulaCount = 0; if (formulaResults != null && formulaResults.Count > 0) formulaCount = formulaResults.Count;
                    if (formulaRanking <= 0) continue;

                    var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                    var sfdResults = new List<FragmenterResult>();

                    foreach (var sfdFile in sfdFiles) {
                        var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                        sfdResultMerge(sfdResults, sfdResult);
                    }

                    if (sfdResults.Count <= 1) continue;
                    sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();

                    int structureRanking = getStructureRanking(rawData.InchiKey, sfdResults);

                    if (structureRanking >= 1) {
                        var correctStructure = sfdResults[structureRanking - 1];
                        var incorrectStructure = sfdResults[0];
                        if (structureRanking - 1 == 0)
                            incorrectStructure = sfdResults[1];

                        var correctArray = new double[] { correctStructure.TotalHrLikelihood, correctStructure.TotalBcLikelihood, correctStructure.TotalMaLikelihood,
                        correctStructure.TotalFlLikelihood, correctStructure.TotalBeLikelihood, correctStructure.SubstructureAssignmentScore, correctStructure.DatabaseScore };

                        var incorrectArray = new double[] { incorrectStructure.TotalHrLikelihood, incorrectStructure.TotalBcLikelihood, incorrectStructure.TotalMaLikelihood,
                        incorrectStructure.TotalFlLikelihood, incorrectStructure.TotalBeLikelihood, incorrectStructure.SubstructureAssignmentScore, incorrectStructure.DatabaseScore };

                        var hrMean = (correctArray[0] + incorrectArray[0]) * 0.5;
                        var bcMean = (correctArray[1] + incorrectArray[1]) * 0.5;
                        var maMean = (correctArray[2] + incorrectArray[2]) * 0.5;
                        var flMean = (correctArray[3] + incorrectArray[3]) * 0.5;
                        var beMean = (correctArray[4] + incorrectArray[4]) * 0.5;
                        var jcMean = (correctArray[5] + incorrectArray[5]) * 0.5;
                        var dbMean = (correctArray[6] + incorrectArray[6]) * 0.5;

                        sw.Write(rawData.RawdataFilePath + "\t");
                        sw.Write(System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath) + "\t");
                        sw.Write("TRUE\t");
                        sw.WriteLine(Math.Round(correctArray[0] - hrMean, 6) + "\t" + Math.Round(correctArray[1] - bcMean, 6) + "\t" + Math.Round(correctArray[2] - maMean, 6) + "\t" +
                            Math.Round(correctArray[3] - flMean, 6) + "\t" + Math.Round(correctArray[4] - beMean, 6) + "\t" + Math.Round(correctArray[5] - jcMean, 6) + "\t" +
                            Math.Round(correctArray[6] - dbMean, 6));

                        sw.Write(rawData.RawdataFilePath + "\t");
                        sw.Write(System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath) + "\t");
                        sw.Write("FALSE\t");
                        sw.WriteLine(Math.Round(incorrectArray[0] - hrMean, 6) + "\t" + Math.Round(incorrectArray[1] - bcMean, 6) + "\t" + Math.Round(incorrectArray[2] - maMean, 6) + "\t" +
                           Math.Round(incorrectArray[3] - flMean, 6) + "\t" + Math.Round(incorrectArray[4] - beMean, 6) + "\t" + Math.Round(incorrectArray[5] - jcMean, 6) + "\t" +
                           Math.Round(incorrectArray[6] - dbMean, 6));






                        //var correctMax = correctArray.Max();
                        //var incorrectMax = incorrectArray.Max();


                        //var maxValue = Math.Max(correctMax, incorrectMax);
                        //if (maxValue <= 0) continue;

                        //sw.Write(rawData.RawdataFilePath + "\t");
                        //sw.Write(System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath) + "\t");
                        //sw.Write("TRUE\t");
                        //sw.WriteLine(Math.Round(correctArray[0] / maxValue, 6) + "\t" + Math.Round(correctArray[1] / maxValue, 6) + "\t" + Math.Round(correctArray[2] / maxValue, 6) + "\t" +
                        //    Math.Round(correctArray[3] / maxValue, 6) + "\t" + Math.Round(correctArray[4] / maxValue, 6) + "\t" + Math.Round(correctArray[5] / maxValue, 6) + "\t" +
                        //    Math.Round(correctArray[6] / maxValue, 6));

                        //sw.Write(rawData.RawdataFilePath + "\t");
                        //sw.Write(System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath) + "\t");
                        //sw.Write("FALSE\t");
                        //sw.WriteLine(Math.Round(incorrectArray[0] / maxValue, 6) + "\t" + Math.Round(incorrectArray[1] / maxValue, 6) + "\t" + Math.Round(incorrectArray[2] / maxValue, 6) + "\t" +
                        //    Math.Round(incorrectArray[3] / maxValue, 6) + "\t" + Math.Round(incorrectArray[4] / maxValue, 6) + "\t" + Math.Round(incorrectArray[5] / maxValue, 6) + "\t" +
                        //    Math.Round(incorrectArray[6] / maxValue, 6));
                    }
                }
            }
        }

        private static void writeScoreHeader(StreamWriter sw) {
            sw.Write("File\t");
            sw.Write("File name\t");
            sw.Write("Result\t");
            sw.Write("HR_score\t");
            sw.Write("BC_score\t");
            sw.Write("MA_score\t");
            sw.Write("FL_score\t");
            sw.Write("BE_score\t");
            sw.Write("JC_score\t");
            sw.WriteLine("DB_score");
        }


        public static void StructureResultExport(MainWindow mainWindow, MainWindowVM mainWindowVM, string exportFile)
        {
            var exportfilename = System.IO.Path.GetFileNameWithoutExtension(exportFile);
            var ionmode = exportfilename.Contains("Pos") ? IonMode.Positive : IonMode.Negative;

            using (var sw = new StreamWriter(exportFile, false, Encoding.ASCII))
            {
                writeHeader(sw);

                var files = mainWindowVM.AnalysisFiles;
                var param = mainWindowVM.DataStorageBean.AnalysisParameter;
                var error = string.Empty;

                foreach (var rawfile in files)
                {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    if (rawData.InchiKey == null || rawData.InchiKey == string.Empty) rawData.InchiKey = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 1].Trim();
                    if (rawData.Formula == null || rawData.Formula == string.Empty) rawData.Formula = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 2].Trim();
                    if (rawData.IonMode != ionmode) continue;

                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }
                    formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                    int formulaRanking = getFormulaRanking(rawData.Formula, formulaResults);
                    int formulaCount = 0; if (formulaResults != null && formulaResults.Count > 0) formulaCount = formulaResults.Count;

                    var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                    var sfdResults = new List<FragmenterResult>();

                    foreach (var sfdFile in sfdFiles)
                    {
                        var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                        sfdResultMerge(sfdResults, sfdResult);
                    }

                    if (sfdResults.Count == 0) continue;
                    //recalculateTotalScore(sfdResults);
                    sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();

                    int ontologyRanking = getOntologyRanking(rawData.Ontology, sfdResults);
                    int ontologyCount = 0; if (sfdResults != null && sfdResults.Count > 0) ontologyCount = sfdResults.Select(n => n.Ontology).Distinct().Count();

                    int structureRanking = getStructureRanking(rawData.InchiKey, sfdResults);
                    int structureCount = 0; if (sfdResults != null && sfdResults.Count > 0) structureCount = sfdResults.Count;

                    if (formulaRanking <= 0) continue;
                    if (structureRanking >= 1) {
                        var correctFormula = formulaResults[formulaRanking - 1];
                        var correctStructure = sfdResults[structureRanking - 1];
                        writeResult(sw, rawData, correctFormula, correctStructure, 
                            structureRanking, formulaRanking, ontologyRanking, 
                            structureCount, formulaCount, ontologyCount, true);

                        //for (int i = 0; i < sfdResults.Count; i++) {
                        //    if (i == structureRanking - 1) continue;
                        //    if (i >= 10) break;

                        //    writeResult(sw, rawData, correctFormula, sfdResults[i],
                        //        structureRanking, formulaRanking, structureCount, formulaCount, false);
                        //}
                    }
                }
            }
        }

        private static void FormulaResultExportTemp(MainWindow mainWindow, MainWindowVM mainWindowVM, string filePath, bool isCarbonFixed) {
            var exportfilename = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var ionmode = exportfilename.Contains("pos") ? IonMode.Positive : IonMode.Negative;
            var ranks = new int[101];
            var files = mainWindowVM.AnalysisFiles;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var error = string.Empty;

            foreach (var rawfile in files) {
                var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                if (rawData.InchiKey == null || rawData.InchiKey == string.Empty) rawData.InchiKey = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 1].Trim();
                if (rawData.Formula == null || rawData.Formula == string.Empty) rawData.Formula = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 2].Trim();
                if (rawData.IonMode != ionmode) continue;

                if (rawData.PrecursorType == "[M-2H]-") continue;
                var correctFormula = FormulaStringParcer.OrganicElementsReader(rawData.Formula);
                if (correctFormula.Fnum > 0 || correctFormula.Clnum > 0 ||
                    correctFormula.Brnum > 0 || correctFormula.Inum > 0 ||
                    correctFormula.Sinum > 0) continue;

                ranks[0]++;

                var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }
                formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                int formulaRanking = -1;
                if (isCarbonFixed)
                    formulaRanking = getFormulaRankingCarbonFixed(rawData.Formula, formulaResults);
                else
                    formulaRanking = getFormulaRanking(rawData.Formula, formulaResults);

                if (formulaRanking == -1)
                    Console.WriteLine(formulaRanking);
                if (formulaRanking == -1 || formulaRanking > 100) continue;
                ranks[formulaRanking]++;
            }

            using (var sw = new StreamWriter(filePath, false, Encoding.ASCII)) {
                sw.WriteLine("Total:\t" + ranks[0]);
                sw.WriteLine("Rank\tCount\t%\tCumaltive&");
                var total = ranks[0];
                var cumaP = 0.0;
                for (int i = 1; i <= 100; i++) {
                    cumaP += (double)ranks[i] / (double)total * 100;
                    sw.WriteLine(i + "\t" + ranks[i] + "\t" + (double)ranks[i] / (double)total * 100 + "\t" + cumaP);
                }
            }
        }


        public static void StructureResultExportTemp(MainWindow mainWindow, MainWindowVM mainWindowVM, string exportFile) {
            var exportfilename = System.IO.Path.GetFileNameWithoutExtension(exportFile);
            var ionmode = exportfilename.Contains("Pos") ? IonMode.Positive : IonMode.Negative;

            using (var sw = new StreamWriter(exportFile, false, Encoding.ASCII)) {
                var files = mainWindowVM.AnalysisFiles;
                var param = mainWindowVM.DataStorageBean.AnalysisParameter;
                var error = string.Empty;

                foreach (var rawfile in files) {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    if (rawData.InchiKey == null || rawData.InchiKey == string.Empty) rawData.InchiKey = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 1].Trim();
                    if (rawData.Formula == null || rawData.Formula == string.Empty) rawData.Formula = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 2].Trim();
                    if (rawData.IonMode != ionmode) continue;

                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }
                    formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                    int formulaRanking = getFormulaRanking(rawData.Formula, formulaResults);
                    int formulaCount = 0; if (formulaResults != null && formulaResults.Count > 0) formulaCount = formulaResults.Count;

                    var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                    var sfdResults = new List<FragmenterResult>();

                    foreach (var sfdFile in sfdFiles) {
                        var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                        sfdResultMerge(sfdResults, sfdResult);
                    }

                    if (sfdResults.Count == 0) continue;
                    //recalculateTotalScore(sfdResults);
                    sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();

                    int ontologyRanking = getOntologyRanking(rawData.Ontology, sfdResults);
                    int ontologyCount = 0; if (sfdResults != null && sfdResults.Count > 0) ontologyCount = sfdResults.Select(n => n.Ontology).Distinct().Count();

                    int structureRanking = getStructureRanking(rawData.InchiKey, sfdResults);
                    int structureCount = 0; if (sfdResults != null && sfdResults.Count > 0) structureCount = sfdResults.Count;

                    if (formulaRanking <= 0) continue;

                    if (structureRanking >= 1) {
                        var correctFormula = formulaResults[formulaRanking - 1];
                        var correctStructure = sfdResults[structureRanking - 1];


                        sw.WriteLine("NAME: " + rawData.Name);
                        sw.WriteLine("PRECURSORMZ: " + rawData.PrecursorMz);
                        sw.WriteLine("PRECURSORTYPE: " + rawData.PrecursorType);
                        sw.WriteLine("INSTRUMENTTYPE: " + rawData.InstrumentType);
                        sw.WriteLine("INSTRUMENT: " + rawData.Instrument);
                        sw.WriteLine("FORMULA: " + rawData.Formula);
                        sw.WriteLine("FORMULA: " + rawData.Ontology);
                        sw.WriteLine("SMILES: " + rawData.Smiles);
                        sw.WriteLine("INCHIKEY: " + rawData.InchiKey);
                        sw.WriteLine("IONMODE: " + rawData.IonMode);
                        sw.WriteLine("Comment: " + rawData.Comment);
                        sw.WriteLine("Num Peaks: " + rawData.Ms2PeakNumber);

                        foreach (var peak in rawData.Ms2Spectrum.PeakList) {
                            sw.WriteLine(peak.Mz + "\t" + peak.Intensity);
                        }
                        sw.WriteLine();
                    }
                }
            }
        }

        public static void FseaResultExport(MainWindow mainWindow, MainWindowVM mainWindowVM, 
            string filePath, string ontologylistfilepath, string ionMode) {

            //this is the source code for test 180622

            var posClassyfireOntToDefinedOntoDictionary = new Dictionary<string, string>();
            var negClassyfireOntToDefinedOntoDictionary = new Dictionary<string, string>();

            var posDefinedOntToIsExportDictionary = new Dictionary<string, string>();
            var negDefinedOntToIsExportDictionary = new Dictionary<string, string>();

            using (StreamReader sr = new StreamReader(ontologylistfilepath)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');

                    var classyfireOnt = lineArray[1];
                    var definedOnt = lineArray[3];
                    var ionmode = lineArray[8];
                    var isExport = lineArray[9];

                    if (ionmode == "Positive") {
                        if (!posClassyfireOntToDefinedOntoDictionary.ContainsKey(classyfireOnt))
                            posClassyfireOntToDefinedOntoDictionary[classyfireOnt] = definedOnt;

                        if (!posDefinedOntToIsExportDictionary.ContainsKey(definedOnt))
                            posDefinedOntToIsExportDictionary[definedOnt] = isExport;
                    }
                    else {
                        if (!negClassyfireOntToDefinedOntoDictionary.ContainsKey(classyfireOnt))
                            negClassyfireOntToDefinedOntoDictionary[classyfireOnt] = definedOnt;

                        if (!negDefinedOntToIsExportDictionary.ContainsKey(definedOnt))
                            negDefinedOntToIsExportDictionary[definedOnt] = isExport;
                    }
                }
            }

            using (var sw = new StreamWriter(filePath, false, Encoding.ASCII)) {
                writeHeader(sw);

                var files = mainWindowVM.AnalysisFiles;
                var param = mainWindowVM.DataStorageBean.AnalysisParameter;
                var error = string.Empty;

                foreach (var rawfile in files) {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    if (rawData.InchiKey == null || rawData.InchiKey == string.Empty) rawData.InchiKey = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 1].Trim();
                    if (rawData.Formula == null || rawData.Formula == string.Empty) rawData.Formula = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 2].Trim();
                    if (rawData.IonMode.ToString() != ionMode) continue;
                    //if (rawData.Ms2PeakNumber < 5) continue;

                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }
                    formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                    int formulaRanking = getFormulaRanking(rawData.Formula, formulaResults);
                    int formulaCount = 0; if (formulaResults != null && formulaResults.Count > 0) formulaCount = formulaResults.Count;

                    var ontologyResults = getOntologyResults(formulaResults);
                    //var definedOntResults = new List<MsFinderValidationResultTemp>();
                    //if (ionMode == "Positive") {
                    //    definedOntResults = getDefinedOntologyResults(ontologyResults, posClassyfireOntToDefinedOntoDictionary);
                    //    definedOntResults = getUniqueOntResults(definedOntResults, posDefinedOntToIsExportDictionary);
                    //}
                    //else {
                    //    definedOntResults = getDefinedOntologyResults(ontologyResults, negClassyfireOntToDefinedOntoDictionary);
                    //    definedOntResults = getUniqueOntResults(definedOntResults, negDefinedOntToIsExportDictionary);
                    //}

                    // definedOntResults = getUniqueOntResults(definedOntResults);
                    // int ontologyRanking = getOntologyRanking(rawData.Ontology, ontologyResults);
                    //int ontologyCount = ontologyResults.Count; 
                    int ontologyRanking = -1;
                    if (ionMode == "Positive") {
                        ontologyRanking = getDefinedOntologyRanking(rawData.Ontology, ontologyResults,
                            posClassyfireOntToDefinedOntoDictionary, posDefinedOntToIsExportDictionary);
                    }
                    else {
                        ontologyRanking = getDefinedOntologyRanking(rawData.Ontology, ontologyResults,
                            negClassyfireOntToDefinedOntoDictionary, negDefinedOntToIsExportDictionary);
                    }

                    int ontologyCount = ontologyResults.Count; 
                    
                    if (formulaRanking <= 0) continue;
                    if (ontologyRanking >= 1) {
                        var correctFormula = formulaResults[formulaRanking - 1];
                        var correctOntology = ontologyResults[ontologyRanking - 1];

                        writeResult(sw, rawData, correctFormula, null,
                           -1, formulaRanking, ontologyRanking,
                           -1, formulaCount, ontologyCount, true);
                    }
                    //if (ontologyRanking >= 1) {
                    //    var correctFormula = formulaResults[formulaRanking - 1];
                    //    var correctOntology = ontologyResults[ontologyRanking - 1];

                    //    writeResult(sw, rawData, correctFormula, null,
                    //       -1, formulaRanking, ontologyRanking,
                    //       -1, formulaCount, ontologyCount, true);
                    //}
                }
            }
        }

        private static List<MsFinderValidationResultTemp> getUniqueOntResults(List<MsFinderValidationResultTemp> ontologyResults, 
            Dictionary<string, string> definedOntToIsExport) {
            if (ontologyResults.Count == 0) return ontologyResults;

            var uResults = new List<MsFinderValidationResultTemp>() { ontologyResults[0] };
            var uOntologies = new List<string>() { ontologyResults[0].Ontology };
            for (int i = 1; i < ontologyResults.Count; i++) {
                var result = ontologyResults[i];
                if (!uOntologies.Contains(result.Ontology) && definedOntToIsExport[result.Ontology] == "TRUE") {
                    uResults.Add(result);
                    uOntologies.Add(result.Ontology);
                }
            }

            return uResults;
        }

        private static List<MsFinderValidationResultTemp> getDefinedOntologyResults(
            List<MsFinderValidationResultTemp> ontologyResults, 
            Dictionary<string, string> classyfireOntToDefinedOnto) {

            var newOntResults = new List<MsFinderValidationResultTemp>();
            foreach (var result in ontologyResults) {
                var nResult = new MsFinderValidationResultTemp() {
                    Ontology = classyfireOntToDefinedOnto[result.Ontology],
                    Score = result.Score
                };
                newOntResults.Add(nResult);
            }

            return newOntResults;
        }

        private static void recalculateTotalScore(List<FragmenterResult> sfdResults)
        {
            return;
            var hrFactor = 0.2;
            var bcFactor = 0.2;
            var maFactor = 0.2;
            var flFactor = 0.2;
            var beFactor = 0.2;
            var suFactor = 1.0;

            var maxHrScore = sfdResults.Max(n => n.TotalHrLikelihood); 
            var maxBcScore = sfdResults.Max(n => n.TotalBcLikelihood);
            var maxMaScore = sfdResults.Max(n => n.TotalMaLikelihood);
            var maxBeScore = sfdResults.Max(n => n.TotalBeLikelihood);
            var maxFlScore = sfdResults.Max(n => n.TotalFlLikelihood);
            var maxSubScore = sfdResults.Max(n => n.SubstructureAssignmentScore);

            foreach (var result in sfdResults) {
               
                if (maxHrScore > 0) result.TotalHrLikelihood = result.TotalHrLikelihood / maxHrScore;
                else result.TotalHrLikelihood = 0;

                if (maxBcScore > 0) result.TotalBcLikelihood = result.TotalBcLikelihood / maxBcScore;
                else result.TotalBcLikelihood = 0;

                if (maxMaScore > 0) result.TotalMaLikelihood = result.TotalMaLikelihood / maxMaScore;
                else result.TotalMaLikelihood = 0;

                if (maxBeScore > 0) result.TotalBeLikelihood = result.TotalBeLikelihood / maxBeScore;
                else result.TotalBeLikelihood = 0;

                if (maxFlScore > 0) result.TotalFlLikelihood = result.TotalFlLikelihood / maxFlScore;
                else result.TotalFlLikelihood = 0;

                if (maxSubScore > 0) result.SubstructureAssignmentScore = result.SubstructureAssignmentScore / maxSubScore;
                else result.SubstructureAssignmentScore = 0;

                var totalScore = result.TotalHrLikelihood * hrFactor +
                       result.TotalBcLikelihood * bcFactor +
                       result.TotalMaLikelihood * maFactor +
                       result.TotalFlLikelihood * flFactor +
                       result.TotalBeLikelihood * beFactor +
                       result.SubstructureAssignmentScore * suFactor;
                result.TotalScore = totalScore;
            }
        }

        public static void AllStructureCandidateReports(MainWindow mainWindow, MainWindowVM mainWindowVM, string exportFile)
        {
            using (var sw = new StreamWriter(exportFile, false, Encoding.ASCII)) {
                writeHeader(sw);

                sw.WriteLine("File\tFileName\tCorrect inchikey\tCorrect short inchikey\tCorrect SMILES\tInChIKey\tShort InChIKey\tSMILES\tRank");

                var files = mainWindowVM.AnalysisFiles;
                var param = mainWindowVM.DataStorageBean.AnalysisParameter;
                var error = string.Empty;

                foreach (var rawfile in files) {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    if (rawData.InchiKey == null || rawData.InchiKey == string.Empty) rawData.InchiKey = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 1].Trim();
                    if (rawData.Formula == null || rawData.Formula == string.Empty) rawData.Formula = rawData.Name.Split(';')[rawData.Name.Split(';').Length - 2].Trim();

                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }
                    formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                    int formulaRanking = getFormulaRanking(rawData.Formula, formulaResults);
                    int formulaCount = 0; if (formulaResults != null && formulaResults.Count > 0) formulaCount = formulaResults.Count;

                    var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                    var sfdResults = new List<FragmenterResult>();

                    foreach (var sfdFile in sfdFiles) {
                        var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                        sfdResultMerge(sfdResults, sfdResult);
                    }
                    sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();

                    var filePath = rawData.RawdataFilePath;
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    var correctInChIKey = rawData.InchiKey;
                    var correctShortInChI = rawData.InchiKey.Substring(0, 14);
                    var correctSMILES = rawData.Smiles;
                    var counter = 1;
                    foreach (var result in sfdResults) {
                        var inchikey = result.Inchikey;
                        var shortInchi = inchikey.Substring(0, 14);
                        var smiles = result.Smiles;
                        var rank = counter;

                        sw.WriteLine(filePath + "\t" + fileName + "\t" + correctInChIKey + "\t" + correctShortInChI
                            + "\t" + correctSMILES + "\t" + inchikey + "\t" + shortInchi + "\t" + smiles + "\t" + rank);

                        counter++;
                    }
                }
            }
        }

        private static void writeHeader(StreamWriter sw)
        {
            sw.Write("File\t");
            sw.Write("File name\t");
            sw.Write("FormulaRanking\t");
            sw.Write("FormulaCount\t");
            sw.Write("OntolgyRanking\t");
            sw.Write("OntologyCount\t");
            sw.Write("StructureRanking\t");
            sw.Write("StructureCount\t");
            //sw.Write("MSMS\t");

            sw.Write("NAME\t");
            sw.Write("PRECURSORMZ\t");
            sw.Write("PRECURSORTYPE\t");
            //sw.Write("INSTRUMENTTYPE\t");
            //sw.Write("INSTRUMENT\t");
            //sw.Write("Authors\t");
            //sw.Write("License\t");
            sw.Write("FORMULA\t");
            sw.Write("Ontology\t");
            sw.Write("SMILES\t");
            //sw.Write("INCHI\t");
            sw.Write("INCHIKEY\t");
            sw.Write("IONMODE\t");
            sw.WriteLine("COLLISIONENERGY");
            //sw.Write("FORMULA\t");
            //sw.Write("SPECTRUMTYPE\t");
            //sw.Write("TotalBondEnergy\t");
            //sw.Write("SubStructureInChIKeys\t");
            //sw.Write("Resources\t");
            //sw.Write("TotalScore\t");
            //sw.Write("PrecursorMz\t");
            //sw.Write("MSMS count\t");
            //sw.Write("TotalHrLikelihood\t");
            //sw.Write("TotalBcLikelihood\t");
            //sw.Write("TotalMaLikelihood\t");
            //sw.Write("TotalFlLikelihood\t");
            //sw.Write("TotalBeLikelihood\t");
            //sw.Write("SubstructureScore\t");
            //sw.WriteLine("DatabaseScore");
        }

        


        private static void writeResult(StreamWriter sw, RawData rawData, FormulaResult formulaResult, FragmenterResult fragmenterResult, 
            int structureRanking, int formulaRanking, int ontologyRanking, 
            int structureCount, int formulaCount, int ontologyCount, bool isCorrect)
        {

            //if (fragmenterResult.TotalHrLikelihood < 0) return;
            sw.Write(rawData.RawdataFilePath + "\t");
            sw.Write(System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath) + "\t");
            //sw.Write(isCorrect + "\t");
            //sw.Write(structureCount + "\t");
            sw.Write(formulaRanking + "\t");
            sw.Write(formulaCount + "\t");
            sw.Write(ontologyRanking + "\t");
            sw.Write(ontologyCount + "\t");
            sw.Write(structureRanking + "\t");
            sw.Write(structureCount + "\t");
            //if (rawData.Ms2PeakNumber > 0) sw.Write("TRUE" + "\t"); else sw.Write("FALSE" + "\t");
            sw.Write(rawData.Name + "\t");
            sw.Write(rawData.PrecursorMz + "\t");
            sw.Write(rawData.PrecursorType + "\t");
            //sw.Write(rawData.InstrumentType + "\t");
            //sw.Write(rawData.Instrument + "\t");
            //sw.Write(rawData.Authors + "\t");
            //sw.Write(rawData.License + "\t");
            sw.Write(rawData.Formula + "\t");
            sw.Write(rawData.Ontology + "\t");
            sw.Write(rawData.Smiles + "\t");
            //sw.Write(fragmenterResult.Smiles + "\t");
            //sw.Write(rawData.Inchi + "\t");
            sw.Write(rawData.InchiKey + "\t");
            sw.Write(rawData.IonMode + "\t");
            sw.WriteLine(rawData.CollisionEnergy);
            //sw.Write(rawData.Formula + "\t");
            //sw.Write(rawData.SpectrumType + "\t");
            //sw.Write(fragmenterResult.BondEnergyOfUnfragmentedMolecule + "\t");
            //sw.Write(fragmenterResult.SubstructureInChIKeys + "\t");
            //sw.Write(fragmenterResult.Resources + "\t");
            //sw.Write(fragmenterResult.TotalScore + "\t");
            //sw.Write(rawData.PrecursorMz + "\t");
            //if (formulaResult.ProductIonResult != null) sw.Write(formulaResult.ProductIonResult.Count + "\t");
            //else sw.Write(0 + "\t");
            //sw.Write(fragmenterResult.TotalHrLikelihood + "\t");
            //sw.Write(fragmenterResult.TotalBcLikelihood + "\t");
            //sw.Write(fragmenterResult.TotalMaLikelihood + "\t");
            //sw.Write(fragmenterResult.TotalFlLikelihood + "\t");
            //sw.Write(fragmenterResult.TotalBeLikelihood + "\t");
            //sw.Write(fragmenterResult.SubstructureAssignmentScore + "\t");
            //sw.WriteLine(fragmenterResult.DatabaseScore);
        }

        private static int getFormulaRanking(string formulaString, List<FormulaResult> formulaResults)
        {
            if (formulaResults == null || formulaResults.Count == 0) return -1;
            var tFormula = FormulaStringParcer.OrganicElementsReader(formulaString);
            for (int i = 0; i < formulaResults.Count; i++)
            {
                var formula = formulaResults[i].Formula;

                if (MolecularFormulaUtility.isFormulaMatch(formula, tFormula))
                {
                    return i + 1;
                }
            }

            return -1;
        }

        private static int getFormulaRankingCarbonFixed(string formulaString, List<FormulaResult> formulaResults) {
            if (formulaResults == null || formulaResults.Count == 0) return -1;
            var tFormula = FormulaStringParcer.OrganicElementsReader(formulaString);
            var carbonNum = tFormula.Cnum;
            var counter = 0;
            for (int i = 0; i < formulaResults.Count; i++) {
                var formula = formulaResults[i].Formula;
                if (formula.Cnum == carbonNum) {
                    counter++;
                }
                else {
                    continue;
                }

                if (MolecularFormulaUtility.isFormulaMatch(formula, tFormula)) {
                    return counter;
                }
            }

            return -1;
        }

        private static int getOntologyRanking(string ontology, List<FragmenterResult> sfdResults) {

            var ontologies = new List<string>();

            for (int i = 0; i < sfdResults.Count; i++) {
                if (ontology == sfdResults[i].Ontology) {
                    //return i + 1;
                    return ontologies.Count + 1;
                }
                else {
                    if (!ontologies.Contains(sfdResults[i].Ontology)) {
                        ontologies.Add(sfdResults[i].Ontology);
                    }
                }
            }
            return -1;
        }

        private static List<MsFinderValidationResultTemp> getOntologyResults(List<FormulaResult> formulaResults) {

            var results = new List<MsFinderValidationResultTemp>();
            foreach (var formula in formulaResults) {
                for (int i = 0; i < formula.ChemicalOntologyDescriptions.Count; i++) {
                    var ontDescript = formula.ChemicalOntologyDescriptions[i];
                    var ontScore = formula.ChemicalOntologyScores[i] / formula.TotalScore;
                    var ontoResult = new MsFinderValidationResultTemp() { Ontology = ontDescript, Score = ontScore };
                    results.Add(ontoResult);
                }
            }

            results = results.OrderBy(n => n.Score).ToList();

            return results;
        }

        private static List<MsFinderValidationResultTemp> getUniqueOntResults(List<MsFinderValidationResultTemp> ontologyResults) {
            if (ontologyResults.Count == 0) return ontologyResults;

            var uResults = new List<MsFinderValidationResultTemp>() { ontologyResults[0] };
            var uOntologies = new List<string>() { ontologyResults[0].Ontology };
            for (int i = 1; i < ontologyResults.Count; i++) {
                var result = ontologyResults[i];
                if (!uOntologies.Contains(result.Ontology)) {
                    uResults.Add(result);
                    uOntologies.Add(result.Ontology);
                }
            }

            return uResults;
        }


        private static int getDefinedOntologyRanking(string ontology, List<MsFinderValidationResultTemp> results,
            Dictionary<string, string> classyfireOntToDefinedOnt, Dictionary<string, string> definedOntToIsExport) {

            if (!classyfireOntToDefinedOnt.ContainsKey(ontology)) return -1;
            var definedOnt = classyfireOntToDefinedOnt[ontology];
            var isExport = definedOntToIsExport[definedOnt];

            if (isExport == "FALSE") return -1;
            if (results == null || results.Count == 0) return -1;

            var scoreDictionary = new Dictionary<string, int>();
            var totalCount = 0;

            for (int i = 0; i < results.Count; i++) {
                var score = Math.Round(results[i].Score, 11).ToString();
                totalCount++;

                if (!scoreDictionary.ContainsKey(score))
                    scoreDictionary[score] = 1;
                else
                    scoreDictionary[score]++;

                if (definedOnt == results[i].Ontology) {
                    //return i + 1;
                    return totalCount - scoreDictionary[score] + 1;
                }
            }

            return -1;
        }

        
        private static int getOntologyRanking(string ontology, List<MsFinderValidationResultTemp> results) {
            if (results == null || results.Count == 0) return -1;
            for (int i = 0; i < results.Count; i++) {
                if (ontology == results[i].Ontology) {
                    return i + 1;
                }
            }
            return -1;
        }

        private static int getStructureRanking(string inchiKey, List<FragmenterResult> sfdResults)
        {
            var shortInchiKey = inchiKey.Split('-')[0];
            
            for (int i = 0; i < sfdResults.Count; i++)
            {
                if (shortInchiKey == sfdResults[i].Inchikey.Split('-')[0])
                {
                    return i + 1;
                }
            }
            return -1;
        }

        private static void sfdResultMerge(List<FragmenterResult> mergedList, List<FragmenterResult> results)
        {
            if (results == null || results.Count == 0) return;

            foreach (var result in results)
            {
                if (result.ID == null || result.ID == string.Empty) continue;
                mergedList.Add(result);
            }
        }

        public static void MsfinderTestResultCuration(string input, string output) {
            var results = new List<string[]>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var lineArray = line.Split('\t');
                    results.Add(lineArray);
                }
            }

            var count = results.Count;
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {

                sw.WriteLine("Total: " + count);

                //formula ranking
                var cumaltive = 0;
                sw.WriteLine("Formula");
                sw.WriteLine("Rank\tCount\tCumalative count\t%");

                for (int i = 1; i <= 100; i++) {
                    var rankString = i.ToString();
                    var rankCount = results.Count(n => n[2] == rankString);
                    cumaltive += rankCount;
                    sw.WriteLine(rankString + "\t" + rankCount + "\t" + cumaltive + "\t" +
                        Math.Round((double)cumaltive / (double)count * 100.0, 2));
                }
                sw.WriteLine();

                //ontology ranking
                cumaltive = 0;
                sw.WriteLine("Ontology");
                sw.WriteLine("Rank\tCount\tCumalative count\t%");

                for (int i = 1; i <= 100; i++) {
                    var rankString = i.ToString();
                    var rankCount = results.Count(n => n[4] == rankString);
                    cumaltive += rankCount;
                    sw.WriteLine(rankString + "\t" + rankCount + "\t" + cumaltive + "\t" +
                        Math.Round((double)cumaltive / (double)count * 100.0, 2));
                }
                sw.WriteLine();


                //structure ranking
                cumaltive = 0;
                sw.WriteLine("Structure");
                sw.WriteLine("Rank\tCount\tCumalative count\t%");

                for (int i = 1; i <= 100; i++) {
                    var rankString = i.ToString();
                    var rankCount = results.Count(n => n[6] == rankString);
                    cumaltive += rankCount;
                    sw.WriteLine(rankString + "\t" + rankCount + "\t" + cumaltive + "\t" +
                        Math.Round((double)cumaltive / (double)count * 100.0, 2));
                }
                sw.WriteLine();
            }
        }

    }
}
