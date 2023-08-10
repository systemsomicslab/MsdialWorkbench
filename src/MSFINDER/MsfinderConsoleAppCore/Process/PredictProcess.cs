using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Process;
using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.MsfinderCommon.Utility;
using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Result;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MsfinderConsoleApp.Process {
    public class PredictProcess {

        private string outputFormulaPath;
        private string outputStructurePath;
        private ObservableCollection<MsfinderQueryFile> queryFiles;
        private AnalysisParamOfMsfinder param;
        private List<AdductIon> adductPositiveResources;
        private List<AdductIon> adductNegativeResources;
        //private List<Formula> quickFormulaDB;
        private List<ProductIon> productIonDB;
        private List<NeutralLoss> neutralLossDB;
        private List<FragmentOntology> fragmentOntologyDB;
        private List<ExistFormulaQuery> existFormulaDB;
        private List<ExistStructureQuery> existStructureDB;
        private List<ExistStructureQuery> userDefinedStructureDB;
        private List<ExistStructureQuery> mineStructureDB;
        private List<FragmentLibrary> eiFragmentDB;
        private List<ChemicalOntology> chemicalOntologies;

        public int Run(string input, string methodFile, string outputfolder) {

            Console.WriteLine("Loading library files..");

            //read query files and param
            if (File.Exists(input))
                this.queryFiles = getQueryFilesFromMultipleFolders(input);
            else if (Directory.Exists(input))
                this.queryFiles = FileStorageUtility.GetAnalysisFileBeanCollection(input);

            if (this.queryFiles == null || this.queryFiles.Count == 0) {
                Console.WriteLine("The program cannot find any query file in your input folder.");
                return -1;
            }
            this.param = MsFinderIniParcer.Read(methodFile);
            this.param.IsRunInSilicoFragmenterSearch = true;
            this.param.IsRunSpectralDbSearch = false;


            //output file preparation
            if (!System.IO.Directory.Exists(outputfolder)) {
                Directory.CreateDirectory(outputfolder);
            }

            var dt = DateTime.Now;
            var dateString = dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute;
            this.outputFormulaPath = Path.Combine(outputfolder, "Formula result-" + dateString + ".txt");
            this.outputStructurePath = Path.Combine(outputfolder, "Structure result-" + dateString + ".txt");

            //working space prep...
            workSpacePreparation();
            #region error messages
            if (this.adductPositiveResources == null) {
                Console.WriteLine("Adduct positive resource is missing.");
                return -1;
            }
            if (this.adductNegativeResources == null) {
                Console.WriteLine("Adduct negative resource is missing.");
                return -1;
            }
            //if (this.quickFormulaDB == null) {
            //    Console.WriteLine("Quick formula DB is missing.");
            //    return -1;
            //}
            if (this.productIonDB == null) {
                Console.WriteLine("Product ion DB is missing.");
                return -1;
            }
            if (this.neutralLossDB == null) {
                Console.WriteLine("Neutral loss DB is missing.");
                return -1;
            }
            if (this.fragmentOntologyDB == null) {
                Console.WriteLine("Fragment ontology DB is missing.");
                return -1;
            }
            if (this.existFormulaDB == null) {
                Console.WriteLine("Exist formula DB is missing.");
                return -1;
            }
            if (this.existStructureDB == null) {
                Console.WriteLine("Exist structureDB DB is missing.");
                return -1;
            }
            if (this.mineStructureDB == null) {
                Console.WriteLine("Mine structure DB is missing.");
                return -1;
            }
            if (this.eiFragmentDB == null) {
                Console.WriteLine("EI fragment DB is missing.");
                return -1;
            }
            if (this.chemicalOntologies == null) {
                Console.WriteLine("Chemical ontologies DB is missing.");
                return -1;
            }
            #endregion

            //checking libraries
            var errorMessage = string.Empty;
            if (!FileStorageUtility.IsLibrariesImported(this.param,
                existStructureDB, mineStructureDB, userDefinedStructureDB, out errorMessage)) {
                Console.WriteLine(errorMessage);
                return -1;
            }

            Console.WriteLine("Start processing..");
            //return privateExecute();
            return executeProcess();
        }

        private int privateExecute() {
            this.param.IsPubChemOnlyUseForNecessary = true;
            this.param.IsPubChemAllTime = false;
            this.param.IsPubChemNeverUse = false;

            //structure predictions
            List<FragmentLibrary> fragmentDB = null;
            if (this.param.IsUseEiFragmentDB && this.eiFragmentDB != null && this.eiFragmentDB.Count > 0) fragmentDB = this.eiFragmentDB;
            var highFragOntologies = this.fragmentOntologyDB.Where(n => n.Frequency >= 0.2).ToList();

            var queryCount = this.queryFiles.Count;
            var progress = 0;
            var error = string.Empty;

            foreach (var file in this.queryFiles) {
                progress++;
                var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, param);
                var rawName = System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath);
                var formulaResults = FormulaResultParcer.FormulaResultReader(file.FormulaFilePath, out error);
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }

                if (rawData.Ms2PeakNumber > 0 && formulaResults != null && formulaResults.Count > 0) {
                    formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();

                    var sfdFiles = System.IO.Directory.GetFiles(file.StructureFolderPath);
                    var sfdResults = new List<FragmenterResult>();

                    foreach (var sfdFile in sfdFiles) {
                        var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                        sfdResultMerge(sfdResults, sfdResult);
                    }

                    var flg = false;
                    foreach (var result in sfdResults) {
                        if (result.Title != null && result.Title != string.Empty) {
                            flg = true;
                            break;
                        }
                    }

                    if (!flg) {
                        foreach (var formula in formulaResults.Where(f => f.IsSelected).ToList()) {

                            var exportFilePath = FileStorageUtility.GetStructureDataFilePath(file.StructureFolderPath, formula.Formula.FormulaString);
                            var finder = new MsfinderStructureFinder();
                            finder.StructureFinderMainProcess(rawData, formula, this.param, exportFilePath, this.existStructureDB,
                                this.userDefinedStructureDB, this.mineStructureDB, fragmentDB, highFragOntologies, null);
                        }

                        if (!Console.IsOutputRedirected) {
                            Console.Write("Structure prediction finished: {0} / {1}", progress, queryCount);
                            Console.SetCursorPosition(0, Console.CursorTop);
                        }
                        else {
                            Console.WriteLine("Structure prediction finished: {0} / {1}", progress, queryCount);
                        }
                    }
                }
            }
            return 1;
        }

        private int executeProcess() {

            var syncObj = new object();
            var queryCount = this.queryFiles.Count;
            var progress = 0;
            var error = string.Empty;

            if (this.param.IsFormulaFinder) {
                //formula predictions
                Parallel.ForEach(this.queryFiles, file => {
                    var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, this.param);
                    var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(this.productIonDB,
                        this.neutralLossDB, this.existFormulaDB, rawData, this.param);
                    //var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(this.quickFormulaDB, this.productIonDB,
                    //    this.neutralLossDB, this.existFormulaDB, rawData, this.param);

                    ChemicalOntologyAnnotation.ProcessByOverRepresentationAnalysis(formulaResults, this.chemicalOntologies,
                        rawData.IonMode, this.param, AdductIonParcer.GetAdductIonBean(rawData.PrecursorType), productIonDB, neutralLossDB);

                    lock (syncObj) {
                        progress++;
                        FormulaResultParcer.FormulaResultsWriter(file.FormulaFilePath, formulaResults);
                        if (!Console.IsOutputRedirected) {
                            Console.Write("Formula prediction finished: {0} / {1}", progress, queryCount);
                            Console.SetCursorPosition(0, Console.CursorTop);
                        }
                        else {
                            Console.WriteLine("Formula prediction finished: {0} / {1}", progress, queryCount);
                        }
                    }
                });
                Console.WriteLine("Formula prediction completed.");
            }
            else {
                Console.WriteLine("Formula prediction is skipped.");
            }

            //structure predictions
            List<FragmentLibrary> fragmentDB = null;
            if (this.param.IsUseEiFragmentDB && this.eiFragmentDB != null && this.eiFragmentDB.Count > 0) fragmentDB = this.eiFragmentDB;
            var highFragOntologies = this.fragmentOntologyDB.Where(n => n.Frequency >= 0.2).ToList();

            progress = 0;
            Parallel.ForEach(this.queryFiles, file => {

                var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, param);
                var structureFiles = System.IO.Directory.GetFiles(file.StructureFolderPath, "*.sfd");
                if (structureFiles.Length > 0) FileStorageUtility.DeleteSfdFiles(structureFiles);

                if (System.IO.File.Exists(file.FormulaFilePath)) {
                    var formulaResults = FormulaResultParcer.FormulaResultReader(file.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }

                    if (formulaResults != null && formulaResults.Count != 0) {
                        foreach (var formula in formulaResults.Where(f => f.IsSelected).ToList()) {

                            var exportFilePath = FileStorageUtility.GetStructureDataFilePath(file.StructureFolderPath, formula.Formula.FormulaString);
                            var finder = new MsfinderStructureFinder();
                            finder.StructureFinderMainProcess(rawData, formula, this.param, exportFilePath, this.existStructureDB,
                                this.userDefinedStructureDB, this.mineStructureDB, fragmentDB, highFragOntologies, null);
                        }
                    }
                }

                lock (syncObj) {
                    progress++;
                    if (!Console.IsOutputRedirected) {
                        Console.Write("Structure prediction finished: {0} / {1}", progress, queryCount);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("Structure prediction finished: {0} / {1}", progress, queryCount);
                    }
                }
            });
            Console.WriteLine("Structure prediction completed.");

            Console.WriteLine("Writing formula prediction results...");
            progress = 0;
            using (var sw = new StreamWriter(this.outputFormulaPath, false, Encoding.ASCII)) {

                sw.WriteLine("File path\tFile name\tTitle\tMS1 count\tMSMS count\t" +
                    "Precursor mz\tPrecursor type\tFormula\tTheoretical mass\t" +
                    "Mass error\tFormula score\tDatabases");

                foreach (var rawfile in this.queryFiles) {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    if (System.IO.File.Exists(rawfile.FormulaFilePath)) {
                        var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                        if (error != string.Empty) {
                            Console.WriteLine(error);
                        }

                        if (formulaResults != null) {
                            formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                            writeFormulaResult(sw, rawData, formulaResults);
                        }
                    }

                    progress++;
                    if (!Console.IsOutputRedirected) {
                        Console.Write("Writing formula prediction result finished: {0} / {1}", progress, queryCount);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("Writing formula prediction result finished: {0} / {1}", progress, queryCount);
                    }
                }
            }

            Console.WriteLine("Writing structure prediction results...");
            progress = 0;
            using (var sw = new StreamWriter(this.outputStructurePath, false, Encoding.ASCII)) {
                sw.WriteLine("File path\tFile name\tTitle\tMS1 count\tMSMS count\t" +
                    "Precursor mz\tPrecursor type\tStructure rank\tTotal score\tDatabases\tFormula\tOntology\tInChIKey\tSMILES");

                foreach (var rawfile in this.queryFiles) {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    if (System.IO.File.Exists(rawfile.FormulaFilePath)) {
                        var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                        if (error != string.Empty) {
                            Console.WriteLine(error);
                        }
                        if (formulaResults != null) {
                            formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                            var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                            var sfdResults = new List<FragmenterResult>();

                            foreach (var sfdFile in sfdFiles) {
                                var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                                var formulaString = System.IO.Path.GetFileNameWithoutExtension(sfdFile);
                                sfdResultMerge(sfdResults, sfdResult, formulaString);
                            }
                            sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();
                            writeStructureResult(sw, rawData, sfdResults);
                        }
                    }

                    progress++;
                    if (!Console.IsOutputRedirected) {
                        Console.Write("Writing structure prediction result finished: {0} / {1}", progress, queryCount);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("Writing structure prediction result finished: {0} / {1}", progress, queryCount);
                    }
                }
            }

            return 1;
        }

        private void writeStructureResult(StreamWriter sw, Rfx.Riken.OsakaUniv.RawData rawData, 
            List<FragmenterResult> sfdResults) {
            var filepath = rawData.RawdataFilePath;
            var filename = System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath);
            var counter = 1;

            foreach (var result in sfdResults) {

                if (result.TotalScore <= 0) continue;

                sw.Write(filepath + "\t");
                sw.Write(filename + "\t");
                sw.Write(rawData.Name + "\t");
                sw.Write(rawData.Ms1PeakNumber + "\t");
                sw.Write(rawData.Ms2PeakNumber + "\t");
                sw.Write(rawData.PrecursorMz + "\t");
                sw.Write(rawData.PrecursorType + "\t");

                sw.WriteLine(
                    result.Title + "\t" +
                    result.TotalScore + "\t" +
                    result.Resources + "\t" +
                    result.Formula + "\t" +
                    result.Ontology + "\t" +
                    result.Inchikey + "\t" +
                    result.Smiles);

                counter++;
            }
            
        }

        private static void sfdResultMerge(List<FragmenterResult> mergedList, List<FragmenterResult> results, string formulaString="") {
            if (results == null || results.Count == 0) return;

            foreach (var result in results) {
                result.Formula = formulaString;
                mergedList.Add(result);
            }
        }

        private void writeFormulaResult(StreamWriter sw, Rfx.Riken.OsakaUniv.RawData rawData, 
            List<FormulaResult> formulaResults) {
            var counter = 1;
            var filepath = rawData.RawdataFilePath;
            var filename = System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath);

            foreach (var result in formulaResults) {
                if (result.TotalScore <= 0) continue;
                sw.Write(filepath + "\t");
                sw.Write(filename + "\t");
                sw.Write(rawData.Name + "\t");
                sw.Write(rawData.Ms1PeakNumber + "\t");
                sw.Write(rawData.Ms2PeakNumber + "\t");
                sw.Write(rawData.PrecursorMz + "\t");
                sw.Write(rawData.PrecursorType + "\t");

                sw.WriteLine(
                    result.Formula.FormulaString + "\t" +
                    result.Formula.Mass + "\t" +
                    result.MassDiff + "\t" +
                    result.TotalScore + "\t" +
                    result.ResourceNames);

                counter++;
            }
        }

        public ObservableCollection<MsfinderQueryFile> getQueryFilesFromMultipleFolders(string input) {
            var folders = new List<string>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    folders.Add(line);
                }
            }

            var masterQueries = new ObservableCollection<MsfinderQueryFile>();
            foreach (var folder in folders) {
                var queries = FileStorageUtility.GetAnalysisFileBeanCollection(folder);
                if (queries == null || queries.Count == 0) continue;
                foreach (var query in queries)
                    masterQueries.Add(query);
            }

            return masterQueries;
        }

        private void workSpacePreparation() {
            this.adductPositiveResources = AdductListParcer.GetAdductPositiveResources();
            this.adductNegativeResources = AdductListParcer.GetAdductNegativeResources();

            Parallel.Invoke(
            #region invoke
            () => {
                this.neutralLossDB = FileStorageUtility.GetNeutralLossDB();
                this.productIonDB = FileStorageUtility.GetProductIonDB();
                this.fragmentOntologyDB = FileStorageUtility.GetUniqueFragmentDB();
                this.chemicalOntologies = FileStorageUtility.GetChemicalOntologyDB();

                if (this.fragmentOntologyDB != null && this.productIonDB != null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(this.productIonDB, this.fragmentOntologyDB);

                if (this.fragmentOntologyDB != null && this.neutralLossDB != null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(this.neutralLossDB, this.fragmentOntologyDB);

                if (this.fragmentOntologyDB != null && this.chemicalOntologies != null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(this.chemicalOntologies, this.fragmentOntologyDB);
            },
            () => {
                this.existFormulaDB = FileStorageUtility.GetExistFormulaDB();
            },
            () => {
                this.existStructureDB = FileStorageUtility.GetExistStructureDB();
            },
            () => {
                this.eiFragmentDB = FileStorageUtility.GetEiFragmentDB();
            },
            () => {
                this.mineStructureDB = FileStorageUtility.GetMinesStructureDB();
            }
            ,
            () => {
              
            }
            //,
            //() => {
            //    MoleculeImage.TryClassLoad();
            //}
            #endregion
            );

            if (this.param.UserDefinedDbFilePath != null && this.param.UserDefinedDbFilePath != string.Empty) {
                if (File.Exists(this.param.UserDefinedDbFilePath)) {
                    var userDefinedDb = ExistStructureDbParcer.ReadExistStructureDB(this.param.UserDefinedDbFilePath);
                    ExistStructureDbParcer.SetExistStructureDbInfoToUserDefinedDB(this.existStructureDB, userDefinedDb);
                    this.userDefinedStructureDB = userDefinedDb;
                }
            }
        }
    }
}
