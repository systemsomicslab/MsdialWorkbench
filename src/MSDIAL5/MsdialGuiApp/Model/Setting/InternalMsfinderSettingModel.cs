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
        private readonly MsfinderParameterSetting _parameter;
        private readonly AnalysisParamOfMsfinder _analysisParam;
        private readonly AlignmentSpectraExportGroupModel _exporter;
        private readonly ReadOnlyReactivePropertySlim<IAlignmentModel?> _currentAlignmentModel;

        public InternalMsfinderSettingModel(MsfinderParameterSetting projectParameter, AlignmentSpectraExportGroupModel alignmentExporter, ReadOnlyReactivePropertySlim<IAlignmentModel?> currentAlignmentModel) {
            _parameter = projectParameter;
            _exporter = alignmentExporter;
            _currentAlignmentModel = currentAlignmentModel;
            _analysisParam = projectParameter.AnalysisParameter;
        }

        private List<ProductIon> ProductIonDB => _productIonDB ??= CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetProductIonDB(
            @"Resources\msfinderLibrary\ProductIonLib_vs1.pid", out string _);
        private List<ProductIon>? _productIonDB;
        private List<NeutralLoss> NeutralLossDB => _neutralLossDB ??= CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetNeutralLossDB(
            @"Resources\msfinderLibrary\NeutralLossDB_vs2.ndb", out string _);
        private List<NeutralLoss>? _neutralLossDB;
        private List<ExistFormulaQuery> ExistFormulaDB => _existFormulaDB ??= ExistFormulaDbParcer.ReadExistFormulaDB(
            @"Resources\msfinderLibrary\MsfinderFormulaDB-VS13.efd", out string _);
        private List<ExistFormulaQuery>? _existFormulaDB;
        private List<ExistStructureQuery> ExistStructureDB => _existStructureDB ??= FileStorageUtility.GetExistStructureDB();
        private List<ExistStructureQuery>? _existStructureDB;
        private List<ExistStructureQuery> MineStructureDB => _mineStructureDB ??= FileStorageUtility.GetMinesStructureDB();
        private List<ExistStructureQuery>? _mineStructureDB;
        private List<FragmentOntology> FragmentOntologyDB => _fragmentOntologyDB ??= FileStorageUtility.GetUniqueFragmentDB();
        private List<FragmentOntology>? _fragmentOntologyDB;
        private List<FragmentLibrary> EiFragmentDB => _eiFragmentDB ??= FileStorageUtility.GetEiFragmentDB();
        private List<FragmentLibrary>? _eiFragmentDB;

        private readonly List<MoleculeMsReference> mspDB = [];
        private List<ExistStructureQuery> userDefinedStructureDB = [];

        public InternalMsFinder? Process() {
            if (_currentAlignmentModel.Value is null) {
                return null;
            }
            SetUserDefinedStructureDB();

            string fullpath;
            var dt = DateTime.Now;
            if (_parameter.IsCreateNewProject) {
                var directory = Path.GetDirectoryName(_currentAlignmentModel.Value.AlignmentFile.FilePath); // project folder
                string foldername;
                if (_parameter.IsUseAutoDefinedFolderName) {
                    foldername = $"{_currentAlignmentModel.Value.AlignmentFile.FileName}_{dt:yyyyMMddHHmmss}";
                }else{
                    foldername = _parameter.UserDefinedProjectFolderName;
                }
                fullpath = Path.Combine(directory, foldername); // export folder
                if (!Directory.Exists(fullpath)) {
                    Directory.CreateDirectory(fullpath);
                }
                _exporter.Export(_currentAlignmentModel.Value.AlignmentFile, fullpath, null);
            }else{
                fullpath = _parameter.ExistProjectPath;
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

            if (_parameter.IsFormulaFinder) {
                var paramfile = Path.Combine(fullpath, $"batchparam-{dt:yyyy_MM_dd_HH_mm_ss}.txt");
                MsFinderIniParser.Write(_analysisParam, paramfile);

                foreach (var msfinderQueryFile in msfinderQueryFiles) {
                    var rawData = RawDataParcer.RawDataFileReader(msfinderQueryFile.RawDataFilePath, _analysisParam);
                    var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(ProductIonDB, NeutralLossDB, ExistFormulaDB, rawData, _analysisParam);
                    FormulaResultParcer.FormulaResultsWriter(msfinderQueryFile.FormulaFilePath, formulaResults);
                }
            }
            if (_parameter.IsStructureFinder) {                
                var finder = new StructureFinderBatchProcess();
                finder.Process(msfinderQueryFiles, _analysisParam, ExistStructureDB, userDefinedStructureDB, MineStructureDB, FragmentOntologyDB, mspDB, EiFragmentDB);
            }

            if (_currentAlignmentModel.Value.AlignmentSpotSource.Spots is null) {
                return null;
            }
            return new InternalMsFinder(msfinderQueryFiles, _analysisParam, userDefinedStructureDB); 
        }

        private void SetUserDefinedStructureDB() {
            if (_parameter.IsUserDefinedDB) {
                var userDefinedDbFilePath = _parameter.UserDefinedDbFilePath;
                if (userDefinedDbFilePath == null || userDefinedDbFilePath == string.Empty) {
                    MessageBox.Show("Select your own structure database, or uncheck the user-defined database option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!File.Exists(userDefinedDbFilePath)) {
                    MessageBox.Show(userDefinedDbFilePath + " file is not existed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var userDefinedDb = ExistStructureDbParser.ReadExistStructureDB(_parameter.UserDefinedDbFilePath);
                if (userDefinedDb == null || userDefinedDb.Count == 0) {
                    MessageBox.Show("Your own structure DB does not have the queries or the data format is not correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ExistStructureDbParser.SetExistStructureDbInfoToUserDefinedDB(ExistStructureDB, userDefinedDb);
                userDefinedStructureDB = userDefinedDb;
            }
            else
                userDefinedStructureDB = [];
        }

    }
}
