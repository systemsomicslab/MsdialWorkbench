using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Process;
using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.MsfinderCommon.Utility;
using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MsfinderConsoleApp.Process {
    public class MsSearchProcess {
        private ObservableCollection<MsfinderQueryFile> queryFiles;
        private AnalysisParamOfMsfinder param;
        private List<MspFormatCompoundInformationBean> mspDB;
        private string outputfile;

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
            this.param.IsRunInSilicoFragmenterSearch = false;
            this.param.IsRunSpectralDbSearch = true;

            //output file preparation
            var dt = DateTime.Now;
            var dateString = dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute;
            this.outputfile = Path.Combine(outputfolder, "MsSearch result-" + dateString + ".txt");

            //working space prep...
            workSpacePreparation();
            if (this.mspDB == null || this.mspDB.Count == 0) {
                Console.WriteLine("No information in MSP spectral DB.");
                return -1;
            }

            Console.WriteLine("Start processing..");
            return executeProcess();
        }

        private int executeProcess() {

            var syncObj = new object();
            var queryCount = this.queryFiles.Count;

            //formula predictions
            var progress = 0;
            var error = string.Empty;

            Parallel.ForEach(this.queryFiles, file => {
                var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, this.param);
                var formulaResults = new List<FormulaResult>() {
                    new FormulaResult(){
                            Formula = new Formula() { FormulaString = "Spectral DB search", Mass = -1 },
                            IsSelected = true,
                            TotalScore = 5.0
                    }
                };

                lock (syncObj) {
                    progress++;
                    FormulaResultParcer.FormulaResultsWriter(file.FormulaFilePath, formulaResults);
                    if (!Console.IsOutputRedirected) {
                        Console.Write("Working space preparation finished: {0} / {1}", progress, queryCount);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("Working space preparation finished: {0} / {1}", progress, queryCount);
                    }
                }
            });
            Console.WriteLine("Working space preparation completed.");

            //structure predictions
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
                            finder.StructureFinderMainProcess(rawData, formula, this.param, exportFilePath, 
                                null, null, null, null, null, this.mspDB);
                        }
                    }
                }

                lock (syncObj) {
                    progress++;
                    if (!Console.IsOutputRedirected) {
                        Console.Write("MS search finished: {0} / {1}", progress, queryCount);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("MS search finished: {0} / {1}", progress, queryCount);
                    }
                }
            });
            Console.WriteLine("MS search completed.");

            Console.WriteLine("Writing MS search results...");
            progress = 0;
            using (var sw = new StreamWriter(this.outputfile, false, Encoding.ASCII)) {
                sw.WriteLine("File path\tFile name\tMS1 count\tMSMS count\t" +
                    "Precursor mz\tPrecursor type\tRank\tInChIKey\tSMILES\tTotal score");

                foreach (var rawfile in this.queryFiles) {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }

                    formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                    var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                    var sfdResults = new List<FragmenterResult>();

                    foreach (var sfdFile in sfdFiles) {
                        var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                        sfdResultMerge(sfdResults, sfdResult);
                    }
                    sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();
                    writeStructureResult(sw, rawData, sfdResults);

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
                sw.Write(rawData.Ms1PeakNumber + "\t");
                sw.Write(rawData.Ms2PeakNumber + "\t");
                sw.Write(rawData.PrecursorMz + "\t");
                sw.Write(rawData.PrecursorType + "\t");

                sw.WriteLine(counter + "\t" +
                    result.Inchikey + "\t" +
                    result.Smiles + "\t" +
                    result.TotalScore);

                counter++;
            }
        }

        private static void sfdResultMerge(List<FragmenterResult> mergedList, List<FragmenterResult> results) {
            if (results == null || results.Count == 0) return;

            foreach (var result in results) {
                mergedList.Add(result);
            }
        }

        private void workSpacePreparation() {
            var errorMessage = string.Empty;
            this.mspDB = FileStorageUtility.GetMspDB(param, out errorMessage);
            if (errorMessage != string.Empty)
                Console.WriteLine(errorMessage);
        }

        private ObservableCollection<MsfinderQueryFile> getQueryFilesFromMultipleFolders(string input) {
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
                foreach (var query in queries)
                    masterQueries.Add(query);
            }

            return masterQueries;
        }
    }
}
