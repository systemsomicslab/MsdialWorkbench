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
    public class AnnotateProcess {

        private string outputFormulaPath;
        private string outputStructurePath;
        private ObservableCollection<MsfinderQueryFile> queryFiles;
        private AnalysisParamOfMsfinder param;
        private List<AdductIon> adductPositiveResources;
        private List<AdductIon> adductNegativeResources;
        private List<ProductIon> productIonDB;
        private List<NeutralLoss> neutralLossDB;
        private List<FragmentOntology> fragmentOntologyDB;
        private List<ExistFormulaQuery> existFormulaDB;
        private List<FragmentLibrary> eiFragmentDB;
        private List<ChemicalOntology> chemicalOntologies;

        public int Run(string input, string methodFile) {

            Console.WriteLine("Loading library files..");

            var utility = new PredictProcess();

            //read query files and param
            if (File.Exists(input))
                this.queryFiles = utility.getQueryFilesFromMultipleFolders(input);
            else if (Directory.Exists(input))
                this.queryFiles = FileStorageUtility.GetAnalysisFileBeanCollection(input);

            if (this.queryFiles == null || this.queryFiles.Count == 0) {
                Console.WriteLine("The program cannot find any query file in your input folder.");
                return -1;
            }
            this.param = MsFinderIniParcer.Read(methodFile);
            this.param.IsRunInSilicoFragmenterSearch = true;
            this.param.IsRunSpectralDbSearch = false;

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
            if (this.eiFragmentDB == null) {
                Console.WriteLine("EI fragment DB is missing.");
                return -1;
            }
            if (this.chemicalOntologies == null) {
                Console.WriteLine("Chemical ontologies DB is missing.");
                return -1;
            }
            #endregion

            Console.WriteLine("Start processing..");
            return executeProcess();
        }

        private int executeProcess() {
            var syncObj = new object();
            var queryCount = this.queryFiles.Count;

            var progress = 0;

            Parallel.ForEach(this.queryFiles, file => {
                var rawDataFilePath = file.RawDataFilePath;
                var formulaFilePath = file.FormulaFilePath;
                var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, param);

                if (rawData.Formula == null || rawData.Formula == string.Empty || rawData.Smiles == null || rawData.Smiles == string.Empty) return;

                if (param.IsUseEiFragmentDB)
                    PeakAssigner.Process(file, rawData, param, productIonDB, neutralLossDB, existFormulaDB, eiFragmentDB, fragmentOntologyDB);
                else
                    PeakAssigner.Process(file, rawData, param, productIonDB, neutralLossDB, existFormulaDB, null, fragmentOntologyDB);

                lock (syncObj) {
                    progress++;
                    if (!Console.IsOutputRedirected) {
                        Console.Write("Fragment annotation finished: {0} / {1}", progress, queryCount);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("Fragment annotation finished: {0} / {1}", progress, queryCount);
                    }
                }
            });
            return 1;
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
                this.eiFragmentDB = FileStorageUtility.GetEiFragmentDB();
            }
            //,
            //() => {
            //    MoleculeImage.TryClassLoad();
            //}
            #endregion
            );
        }
    }
}
