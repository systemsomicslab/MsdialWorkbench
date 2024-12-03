using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.FormulaGenerator;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.Common.StructureFinder.Parser;
using CompMs.Common.Utility;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace CompMs.App.Msdial.Model.Setting
{
    internal class InternalMsfinderSettingModel : BindableBase
    {
        internal readonly MsfinderParameterSetting parameter;
        internal readonly AnalysisParamOfMsfinder analysisParam;
        internal readonly AlignmentSpectraExportGroupModel exporter;
        internal readonly ReadOnlyReactivePropertySlim<IAlignmentModel?> CurrentAlignmentModel;

        public InternalMsfinderSettingModel(MsfinderParameterSetting projectParameter, AlignmentSpectraExportGroupModel alignmentExporter, ReadOnlyReactivePropertySlim<IAlignmentModel?> currentAlignmentModel) {
            parameter = projectParameter;
            exporter = alignmentExporter;
            CurrentAlignmentModel = currentAlignmentModel;
            analysisParam = projectParameter.AnalysisParameter;
        }

        private readonly List<ProductIon> productIonDB = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetProductIonDB(
            @"Resources\msfinderLibrary\ProductIonLib_vs1.pid", out string _);
        private readonly List<NeutralLoss> neutralLossDB = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetNeutralLossDB(
            @"Resources\msfinderLibrary\NeutralLossDB_vs2.ndb", out string _);
        private readonly List<ExistFormulaQuery> existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(
            @"Resources\msfinderLibrary\MsfinderFormulaDB-VS13.efd", out string _);
        private readonly List<ExistStructureQuery> existStructureDB = FileStorageUtility.GetExistStructureDB();

        private readonly List<ExistStructureQuery> mineStructureDB = FileStorageUtility.GetMinesStructureDB();
        private readonly List<FragmentOntology> fragmentOntologyDB = FileStorageUtility.GetUniqueFragmentDB();
        private readonly List<MoleculeMsReference> mspDB = [];
        private List<ExistStructureQuery> userDefinedStructureDB = [];
        private readonly List<FragmentLibrary>  eiFragmentDB = FileStorageUtility.GetEiFragmentDB();


        public InternalMsFinder? Process() {
            if (CurrentAlignmentModel.Value is null) {
                return null;
            }
            SetUserDefinedStructureDB();

            string fullpath;
            var dt = DateTime.Now;
            if (parameter.IsCreateNewProject) {
                var directory = Path.GetDirectoryName(CurrentAlignmentModel.Value.AlignmentFile.FilePath); // project folder
                string foldername;
                if (parameter.IsUseAutoDefinedFolderName) {
                    foldername = $"{CurrentAlignmentModel.Value.AlignmentFile.FileName}_{dt:yyyyMMddHHmmss}";
                }else{
                    foldername = parameter.UserDefinedProjectFolderName;
                }
                fullpath = Path.Combine(directory, foldername); // export folder
                if (!Directory.Exists(fullpath)) {
                    Directory.CreateDirectory(fullpath);
                }
                exporter.Export(CurrentAlignmentModel.Value.AlignmentFile, fullpath, null);
            }else{
                fullpath = parameter.ExistProjectPath;
            }

            var matFilePaths = Directory.GetFiles(fullpath, "*.mat");
            var msfinderQueryFiles = new List<MsfinderQueryFile>(matFilePaths.Length);
            foreach (var matFilePath in matFilePaths)
            {
                var msfinderQueryFile = new MsfinderQueryFile(matFilePath);
                if (!Directory.Exists(msfinderQueryFile.StructureFolderPath))
                {
                    Directory.CreateDirectory(msfinderQueryFile.StructureFolderPath);
                }
                msfinderQueryFiles.Add(msfinderQueryFile);
            }

            if (parameter.IsFormulaFinder) {
                var paramfile = Path.Combine(fullpath, $"batchparam-{dt:yyyy_MM_dd_HH_mm_ss}.txt");
                MsFinderIniParser.Write(analysisParam, paramfile);

                foreach (var msfinderQueryFile in msfinderQueryFiles) {
                    var rawData = RawDataParcer.RawDataFileReader(msfinderQueryFile.RawDataFilePath, analysisParam);
                    var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, analysisParam);
                    FormulaResultParcer.FormulaResultsWriter(msfinderQueryFile.FormulaFilePath, formulaResults);
                }
            }
            if (parameter.IsStructureFinder) {                
                var finder = new StructureFinderBatchProcess();
                finder.Process(msfinderQueryFiles, analysisParam, existStructureDB, userDefinedStructureDB, mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);
            }

            if (CurrentAlignmentModel.Value.AlignmentSpotSource.Spots is null) {
                return null;
            }
            return new InternalMsFinder(msfinderQueryFiles, analysisParam, userDefinedStructureDB); 
        }

        private void SetUserDefinedStructureDB() {
            if (parameter.IsUserDefinedDB) {
                var userDefinedDbFilePath = parameter.UserDefinedDbFilePath;
                if (userDefinedDbFilePath == null || userDefinedDbFilePath == string.Empty) {
                    MessageBox.Show("Select your own structure database, or uncheck the user-defined database option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!File.Exists(userDefinedDbFilePath)) {
                    MessageBox.Show(userDefinedDbFilePath + " file is not existed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var userDefinedDb = ExistStructureDbParser.ReadExistStructureDB(parameter.UserDefinedDbFilePath);
                if (userDefinedDb == null || userDefinedDb.Count == 0) {
                    MessageBox.Show("Your own structure DB does not have the queries or the data format is not correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ExistStructureDbParser.SetExistStructureDbInfoToUserDefinedDB(existStructureDB, userDefinedDb);
                userDefinedStructureDB = userDefinedDb;
            }
            else
                userDefinedStructureDB = [];
        }

    }
}
