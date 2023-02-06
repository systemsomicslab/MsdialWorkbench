using Riken.Metabolomics.MsfinderCommon.Utility;
using Riken.Metabolomics.StructureFinder.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class AnalysisParameterSettingVM : ViewModelBase
    {
        private Window window;
        private MainWindowVM mainWindowVM;
        private AnalysisParamOfMsfinder param;
        private AnalysisParamOfMsfinder copyParam;

        public AnalysisParameterSettingVM(Window window, MainWindowVM mainWindowVM) 
        { 
            this.window = window; 
            this.mainWindowVM = mainWindowVM; 
            this.param = this.mainWindowVM.DataStorageBean.AnalysisParameter;
            this.copyParam = getCopyParam(this.param);
        }

        private AnalysisParamOfMsfinder getCopyParam(AnalysisParamOfMsfinder param)
        {
            var copyParam = new AnalysisParamOfMsfinder() {
                IsLewisAndSeniorCheck = param.IsLewisAndSeniorCheck,
                MassTolType = param.MassTolType,
                Mass1Tolerance = param.Mass1Tolerance,
                Mass2Tolerance = param.Mass2Tolerance,
                IsotopicAbundanceTolerance = param.IsotopicAbundanceTolerance,
                CoverRange = param.CoverRange,
                IsElementProbabilityCheck = param.IsElementProbabilityCheck,
                IsOcheck = param.IsOcheck,
                IsNcheck = param.IsNcheck,
                IsPcheck = param.IsPcheck,
                IsScheck = param.IsScheck,
                IsFcheck = param.IsFcheck,
                IsClCheck = param.IsClCheck,
                IsBrCheck = param.IsBrCheck,
                IsIcheck = param.IsIcheck,
                IsSiCheck = param.IsSiCheck,
                IsNitrogenRule = param.IsNitrogenRule,

                CanExcuteMS2AdductSearch = param.CanExcuteMS2AdductSearch,
                CanExcuteMS1AdductSearch = param.CanExcuteMS1AdductSearch,
                MS1PositiveAdductIonList = param.MS1PositiveAdductIonList,
                MS2PositiveAdductIonList = param.MS2PositiveAdductIonList,
                MS1NegativeAdductIonList = param.MS1NegativeAdductIonList,
                MS2NegativeAdductIonList = param.MS2NegativeAdductIonList,
                IsTmsMeoxDerivative = param.IsTmsMeoxDerivative,
                MinimumTmsCount = param.MinimumTmsCount,
                MinimumMeoxCount = param.MinimumMeoxCount,

                FormulaScoreCutOff = param.FormulaScoreCutOff,
                FormulaMaximumReportNumber = param.FormulaMaximumReportNumber,

                IsNeutralLossCheck = param.IsNeutralLossCheck,

                TreeDepth = param.TreeDepth,
                RelativeAbundanceCutOff = param.RelativeAbundanceCutOff,

                StructureScoreCutOff = param.StructureScoreCutOff,
                ScoreCutOffForSpectralMatch = param.ScoreCutOffForSpectralMatch,
                StructureMaximumReportNumber = param.StructureMaximumReportNumber,

                IsAllProcess = param.IsAllProcess,
                IsFormulaFinder = param.IsFormulaFinder,
                IsStructureFinder = param.IsStructureFinder,
                TryTopNmolecularFormulaSearch = param.TryTopNmolecularFormulaSearch,

                IsPubChemAllTime = param.IsPubChemAllTime,
                IsPubChemNeverUse = param.IsPubChemNeverUse,
                IsPubChemOnlyUseForNecessary = param.IsPubChemOnlyUseForNecessary,
                
                IsMinesAllTime = param.IsMinesAllTime,
                IsMinesOnlyUseForNecessary = param.IsMinesOnlyUseForNecessary,
                IsMinesNeverUse = param.IsMinesNeverUse,

                IsUseEiFragmentDB = param.IsUseEiFragmentDB,
                
                IsRunSpectralDbSearch = param.IsRunSpectralDbSearch,
                IsRunInSilicoFragmenterSearch = param.IsRunInSilicoFragmenterSearch,
                IsPrecursorOrientedSearch = param.IsPrecursorOrientedSearch,
                IsUseInternalExperimentalSpectralDb = param.IsUseInternalExperimentalSpectralDb,
                IsUseInSilicoSpectralDbForLipids = param.IsUseInSilicoSpectralDbForLipids,
                IsUseUserDefinedSpectralDb = param.IsUseUserDefinedSpectralDb,
                UserDefinedSpectralDbFilePath = param.UserDefinedSpectralDbFilePath,
                SolventType = param.SolventType,

                DatabaseQuery =
                {
                    Chebi = param.DatabaseQuery.Chebi,
                    Hmdb = param.DatabaseQuery.Hmdb,
                    Pubchem = param.DatabaseQuery.Pubchem,
                    Smpdb = param.DatabaseQuery.Smpdb,
                    Unpd = param.DatabaseQuery.Unpd,
                    Ymdb = param.DatabaseQuery.Ymdb,
                    Plantcyc = param.DatabaseQuery.Plantcyc,
                    Knapsack = param.DatabaseQuery.Knapsack, 
                    Bmdb = param.DatabaseQuery.Bmdb,
                    Drugbank = param.DatabaseQuery.Drugbank,
                    Ecmdb = param.DatabaseQuery.Ecmdb,
                    Foodb = param.DatabaseQuery.Foodb,
                    T3db = param.DatabaseQuery.T3db,
                    Stoff = param.DatabaseQuery.Stoff,
                    Nanpdb = param.DatabaseQuery.Nanpdb,
                    Lipidmaps = param.DatabaseQuery.Lipidmaps,
                    Feces = param.DatabaseQuery.Feces, 
                    Saliva = param.DatabaseQuery.Saliva,
                    Serum = param.DatabaseQuery.Serum,
                    Urine = param.DatabaseQuery.Urine, 
                    Csf = param.DatabaseQuery.Csf,
                    Blexp = param.DatabaseQuery.Blexp,
                    Npa = param.DatabaseQuery.Npa,
                    Coconut = param.DatabaseQuery.Coconut
                },

                LipidQueryBean = param.LipidQueryBean,

                IsUserDefinedDB = param.IsUserDefinedDB,
                UserDefinedDbFilePath = param.UserDefinedDbFilePath, 
                MassRangeMin = param.MassRangeMin, 
                MassRangeMax = param.MassRangeMax,

                IsUsePredictedRtForStructureElucidation = param.IsUsePredictedRtForStructureElucidation,
                IsUseRtInchikeyLibrary = param.IsUseRtInchikeyLibrary,
                IsUseXlogpPrediction = param.IsUseXlogpPrediction,
                RtInChIKeyDictionaryFilepath = param.RtInChIKeyDictionaryFilepath,
                RtSmilesDictionaryFilepath = param.RtSmilesDictionaryFilepath,
                Coeff_RtPrediction = param.Coeff_RtPrediction,
                Intercept_RtPrediction = param.Intercept_RtPrediction,
                RtToleranceForStructureElucidation = param.RtToleranceForStructureElucidation,
                RtPredictionSummaryReport = param.RtPredictionSummaryReport,
                
                CcsAdductInChIKeyDictionaryFilepath = param.CcsAdductInChIKeyDictionaryFilepath,
                CcsToleranceForSpectralSearching = param.CcsToleranceForSpectralSearching,
                CcsToleranceForStructureElucidation = param.CcsToleranceForStructureElucidation,
                IsUseCcsForFilteringCandidates = param.IsUseCcsForFilteringCandidates,
                IsUseCcsInchikeyAdductLibrary = param.IsUseCcsInchikeyAdductLibrary,
                IsUseExperimentalCcsForSpectralSearching = param.IsUseExperimentalCcsForSpectralSearching,
                IsUsePredictedCcsForStructureElucidation = param.IsUsePredictedCcsForStructureElucidation,

                IsUseExperimentalRtForSpectralSearching = param.IsUseExperimentalRtForSpectralSearching,
                RetentionType = param.RetentionType,
                RtToleranceForSpectralSearching = param.RtToleranceForSpectralSearching,
                IsUseRtForFilteringCandidates = param.IsUseRtForFilteringCandidates,
                FormulaPredictionTimeOut = param.FormulaPredictionTimeOut,
                StructurePredictionTimeOut = param.StructurePredictionTimeOut,


            };

            return copyParam;
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);
            closingMethod();
            this.window.Close();
        }

        private void closingMethod()
        {
            if (this.copyParam.IsRunInSilicoFragmenterSearch == false && this.copyParam.IsRunSpectralDbSearch == false)
            {
                MessageBox.Show("Do either (1) check 'Run spectral database search' or (2) check 'Run in silico fragmenter search' in Spectral DB search tab.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.copyParam.IsRunSpectralDbSearch == true 
                && this.copyParam.IsUseInSilicoSpectralDbForLipids == true
                && this.copyParam.IsTmsMeoxDerivative == true) {

                MessageBox.Show("GC/MS searching cannot use in silico lipid spectral database. Do either (1) Formula finder -> uncheck TMS-MeOX derivative compounds or (2) Method -> uncheck Use the in silico spectra for lipids." 
                    , "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

          
            if (flgCheck(this.param))
            {
                Mouse.OverrideCursor = Cursors.Wait;
                //this.mainWindowVM.QuickFormulaDB = FileStorageUtility.GetKyusyuUnivFormulaDB(this.copyParam);
                Mouse.OverrideCursor = null;
            }

            if (this.copyParam.IsUserDefinedDB)
            {
                var userDefinedDbFilePath = this.copyParam.UserDefinedDbFilePath;
                if (userDefinedDbFilePath == null || userDefinedDbFilePath == string.Empty)
                {
                    MessageBox.Show("Select your own structure database, or uncheck the user-defined database option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!File.Exists(userDefinedDbFilePath))
                {
                    MessageBox.Show(userDefinedDbFilePath + " file is not existed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var userDefinedDb = ExistStructureDbParcer.ReadExistStructureDB(this.copyParam.UserDefinedDbFilePath);
                if (userDefinedDb == null || userDefinedDb.Count == 0)
                {
                    MessageBox.Show("Your own structure DB does not have the queries or the data format is not correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ExistStructureDbParcer.SetExistStructureDbInfoToUserDefinedDB(mainWindowVM.ExistStructureDB, userDefinedDb);
                this.mainWindowVM.UserDefinedStructureDB = userDefinedDb;
            }
            else
                this.mainWindowVM.UserDefinedStructureDB = null;

            var errorMessage = string.Empty;
            if (!FileStorageUtility.IsLibrariesImported(this.copyParam,
                this.mainWindowVM.ExistStructureDB, this.mainWindowVM.MineStructureDB, this.mainWindowVM.UserDefinedStructureDB, out errorMessage)) {
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.copyParam.IsUseEiFragmentDB && (this.mainWindowVM.EiFragmentDB == null || this.mainWindowVM.EiFragmentDB.Count == 0)){
                this.mainWindowVM.EiFragmentDB = FileStorageUtility.GetEiFragmentDB();
            }

            if (flgSpectrumDbCheck(this.param, this.copyParam)) //parse msp files
            { 
                Mouse.OverrideCursor = Cursors.Wait;
                var mspDB = FileStorageUtility.GetMspDB(this.copyParam, out errorMessage);
                Mouse.OverrideCursor = null;
                if (errorMessage != string.Empty) {
                    MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                //if (mspDB == null) return;
                this.mainWindowVM.MspDB = mspDB;
            }

            if (this.copyParam.IsPrecursorOrientedSearch && this.mainWindowVM.MspDB != null && this.mainWindowVM.MspDB.Count != 0)
                this.mainWindowVM.MspDB = this.mainWindowVM.MspDB.OrderBy(n => n.PrecursorMz).ToList();

            if (this.copyParam.IsTmsMeoxDerivative) this.copyParam.IsSiCheck = true;
            this.mainWindowVM.DataStorageBean.AnalysisParameter = this.copyParam;
            RefreshUtility.RawDataFileRefresh(this.mainWindowVM, this.mainWindowVM.SelectedRawFileId);
            MsFinderIniParcer.Write(this.copyParam);
        }

        private bool flgSpectrumDbCheck(AnalysisParamOfMsfinder originParam, AnalysisParamOfMsfinder copyParam)
        {
            if (originParam.IsRunSpectralDbSearch == false && copyParam.IsRunSpectralDbSearch == true) return true;
            if (copyParam.IsRunSpectralDbSearch == false) return false;

            if (originParam.IsRunSpectralDbSearch == true && copyParam.IsRunSpectralDbSearch == true)
            {
                if (originParam.IsTmsMeoxDerivative != copyParam.IsTmsMeoxDerivative) return true;
                if (originParam.IsUseInSilicoSpectralDbForLipids != copyParam.IsUseInSilicoSpectralDbForLipids || 
                    originParam.IsUseInternalExperimentalSpectralDb != copyParam.IsUseInternalExperimentalSpectralDb ||
                    originParam.IsUseUserDefinedSpectralDb != copyParam.IsUseUserDefinedSpectralDb)
                {
                    return true;
                }
                if (originParam.IsUseUserDefinedSpectralDb == copyParam.IsUseUserDefinedSpectralDb && originParam.UserDefinedSpectralDbFilePath != copyParam.UserDefinedSpectralDbFilePath) return true;
            }
            return false;
        }

        private bool flgCheck(AnalysisParamOfMsfinder param)
        {
            if (this.copyParam.CoverRange != param.CoverRange || this.copyParam.IsElementProbabilityCheck != param.IsElementProbabilityCheck || this.copyParam.IsLewisAndSeniorCheck != param.IsLewisAndSeniorCheck) return true;
            else return false;
        }

        public void OpenSetAdductTypeWindow() {
            var adductWindow = new SelectAdductType(this.copyParam);
            adductWindow.Owner = this.window;
            this.window.IsEnabled = false;
            adductWindow.Show();
            this.window.IsEnabled = true;
        }

        #region Properties
        public MassToleranceType MassToleranceType
        {
            get { return this.copyParam.MassTolType; }
            set { this.copyParam.MassTolType = value; OnPropertyChanged("MassTolType"); }
        }

        public bool IsLewisAndSeniorCheck
        {
            get { return this.copyParam.IsLewisAndSeniorCheck; }
            set { this.copyParam.IsLewisAndSeniorCheck = value; OnPropertyChanged("IsLewisAndSeniorCheck"); }
        }

        public double Mass1Tolerance
        {
            get { return this.copyParam.Mass1Tolerance; }
            set { this.copyParam.Mass1Tolerance = value; OnPropertyChanged("Mass1Tolerance"); }
        }

        public double Mass2Tolerance
        {
            get { return this.copyParam.Mass2Tolerance; }
            set { this.copyParam.Mass2Tolerance = value; OnPropertyChanged("Mass2Tolerance"); }
        }

        public double IsotopicAbundanceTolerance
        {
            get { return this.copyParam.IsotopicAbundanceTolerance; }
            set { this.copyParam.IsotopicAbundanceTolerance = value; OnPropertyChanged("IsotopicAbundanceTolerance"); }
        }


        public CoverRange ElementRatioCheck
        {
            get { return this.copyParam.CoverRange; }
            set { this.copyParam.CoverRange = value; OnPropertyChanged("ElementRatioCheck"); }
        }

        public bool IsElementProbabilityCheck
        {
            get { return this.copyParam.IsElementProbabilityCheck; }
            set { this.copyParam.IsElementProbabilityCheck = value; OnPropertyChanged("IsElementProbabilityCheck"); }
        }

        public bool IsNitrogenRule
        {
            get { return this.copyParam.IsNitrogenRule; }
            set { this.copyParam.IsNitrogenRule = value; OnPropertyChanged("IsNitrogenRule"); }
        }

        public bool IsOcheck
        {
            get { return this.copyParam.IsOcheck; }
            set { this.copyParam.IsOcheck = value; OnPropertyChanged("IsOcheck"); }
        }

        public bool IsNcheck
        {
            get { return this.copyParam.IsNcheck; }
            set { this.copyParam.IsNcheck = value; OnPropertyChanged("IsNcheck"); }
        }

        public bool IsPcheck
        {
            get { return this.copyParam.IsPcheck; }
            set { this.copyParam.IsPcheck = value; OnPropertyChanged("IsPcheck"); }
        }

        public bool IsScheck
        {
            get { return this.copyParam.IsScheck; }
            set { this.copyParam.IsScheck = value; OnPropertyChanged("IsScheck"); }
        }

        public bool IsFcheck
        {
            get { return this.copyParam.IsFcheck; }
            set { this.copyParam.IsFcheck = value; OnPropertyChanged("IsFcheck"); }
        }

        public bool IsIcheck
        {
            get { return this.copyParam.IsIcheck; }
            set { this.copyParam.IsIcheck = value; OnPropertyChanged("IsIcheck"); }
        }

        public bool IsClCheck
        {
            get { return this.copyParam.IsClCheck; }
            set { this.copyParam.IsClCheck = value; OnPropertyChanged("IsClCheck"); }
        }

        public bool IsBrCheck
        {
            get { return this.copyParam.IsBrCheck; }
            set { this.copyParam.IsBrCheck = value; OnPropertyChanged("IsBrCheck"); }
        }

        public bool IsSiCheck
        {
            get { return this.copyParam.IsSiCheck; }
            set { this.copyParam.IsSiCheck = value; OnPropertyChanged("IsSiCheck"); }
        }

        public bool IsTmsMeoxDerivative {
            get { return this.copyParam.IsTmsMeoxDerivative; }
            set { this.copyParam.IsTmsMeoxDerivative = value; OnPropertyChanged("IsTmsMeoxDerivative"); }
        }

        public int MinimumTmsCount {
            get { return this.copyParam.MinimumTmsCount; }
            set { this.copyParam.MinimumTmsCount = value; OnPropertyChanged("MinimumTmsCount"); }
        }

        public int MinimumMeoxCount {
            get { return this.copyParam.MinimumMeoxCount; }
            set { this.copyParam.MinimumMeoxCount = value; OnPropertyChanged("MinimumMeoxCount"); }
        }

        public double FormulaScoreCutOff
        {
            get { return this.copyParam.FormulaScoreCutOff; }
            set { this.copyParam.FormulaScoreCutOff = value; OnPropertyChanged("FormulaScoreCutOff"); }
        }

        public bool CanExcuteMS1AdductSearch {
            get { return this.copyParam.CanExcuteMS1AdductSearch; }
            set { this.copyParam.CanExcuteMS1AdductSearch = value; OnPropertyChanged("CanExcuteMS1AdductSearch"); }
        }

        public bool CanExcuteMS2AdductSearch {
            get { return this.copyParam.CanExcuteMS2AdductSearch; }
            set { this.copyParam.CanExcuteMS2AdductSearch = value; OnPropertyChanged("CanExcuteMS2AdductSearch"); }
        }

        public List<AdductIon> MS1PositiveAdductIonList {
            get { return this.copyParam.MS1PositiveAdductIonList; }
            set { this.copyParam.MS1PositiveAdductIonList = value; OnPropertyChanged("MS1PositiveAdductIonList"); }
        }

        public List<AdductIon> MS2PositiveAdductIonList {
            get { return this.copyParam.MS2PositiveAdductIonList; }
            set { this.copyParam.MS2PositiveAdductIonList = value; OnPropertyChanged("MS2PositiveAdductIonList"); }
        }

        public List<AdductIon> MS1NegativeAdductIonList {
            get { return this.copyParam.MS1NegativeAdductIonList; }
            set { this.copyParam.MS1NegativeAdductIonList = value; OnPropertyChanged("MS1NegativeAdductIonList"); }
        }

        public List<AdductIon> MS2NegativeAdductIonList {
            get { return this.copyParam.MS2NegativeAdductIonList; }
            set { this.copyParam.MS2NegativeAdductIonList = value; OnPropertyChanged("MS2NegativeAdductIonList"); }
        }


        public int FormulaMaximumReportNumber
        {
            get { return this.copyParam.FormulaMaximumReportNumber; }
            set { this.copyParam.FormulaMaximumReportNumber = value; OnPropertyChanged("FormulaMaximumReportNumber"); }
        }

        public bool IsNeutralLossCheck
        {
            get { return this.copyParam.IsNeutralLossCheck; }
            set { this.copyParam.IsNeutralLossCheck = value; OnPropertyChanged("IsNeutralLossCheck"); }
        }

        [Range(0,3,ErrorMessage="Please add 0-3 values as tree depth.")]
        public int TreeDepth
        {
            get { return this.copyParam.TreeDepth; }
            set { this.copyParam.TreeDepth = value; OnPropertyChanged("TreeDepth"); }
        }

        public double MassRangeMax
        {
            get { return this.copyParam.MassRangeMax; }
            set { this.copyParam.MassRangeMax = value; OnPropertyChanged("MassRangeMax"); }
        }

        public double MassRangeMin
        {
            get { return this.copyParam.MassRangeMin; }
            set { this.copyParam.MassRangeMin = value; OnPropertyChanged("MassRangeMin"); }
        }

        public double RelativeAbundanceCutOff
        {
            get { return this.copyParam.RelativeAbundanceCutOff; }
            set { this.copyParam.RelativeAbundanceCutOff = value; OnPropertyChanged("RelativeAbundanceCutOff"); }
        }

        public double StructureScoreCutOff
        {
            get { return this.copyParam.StructureScoreCutOff; }
            set { this.copyParam.StructureScoreCutOff = value; OnPropertyChanged("StructureScoreCutOff"); }
        }

        public double ScoreCutOffForSpectralMatch {
            get { return this.copyParam.ScoreCutOffForSpectralMatch; }
            set { this.copyParam.ScoreCutOffForSpectralMatch = value; OnPropertyChanged("ScoreCutOffForSpectralMatch"); }
        }

        public int StructureMaximumReportNumber
        {
            get { return this.copyParam.StructureMaximumReportNumber; }
            set { this.copyParam.StructureMaximumReportNumber = value; OnPropertyChanged("StructureMaximumReportNumber"); }
        }

        public bool IsUseEiFragmentDB
        {
            get { return this.copyParam.IsUseEiFragmentDB; }
            set { this.copyParam.IsUseEiFragmentDB = value; OnPropertyChanged("IsUseEiFragmentDB"); }
        }

        public bool IsMinesAllTime
        {
            get { return this.copyParam.IsMinesAllTime; }
            set { this.copyParam.IsMinesAllTime = value; OnPropertyChanged("IsMinesAllTime"); }
        }

        public bool IsMinesNeverUse
        {
            get { return this.copyParam.IsMinesNeverUse; }
            set { this.copyParam.IsMinesNeverUse = value; OnPropertyChanged("IsMinesNeverUse"); }
        }

        public bool IsMinesOnlyUseForNecessary
        {
            get { return this.copyParam.IsMinesOnlyUseForNecessary; }
            set { this.copyParam.IsMinesOnlyUseForNecessary = value; OnPropertyChanged("IsMinesOnlyUseForNecessary"); }
        }

        public bool IsPubChemAllTime
        {
            get { return this.copyParam.IsPubChemAllTime; }
            set { this.copyParam.IsPubChemAllTime = value; OnPropertyChanged("IsPubChemAllTime"); }
        }

        public bool IsPubChemNeverUse
        {
            get { return this.copyParam.IsPubChemNeverUse; }
            set { this.copyParam.IsPubChemNeverUse = value; OnPropertyChanged("IsPubChemNeverUse"); }
        }

        public bool IsPubChemOnlyUseForNecessary
        {
            get { return this.copyParam.IsPubChemOnlyUseForNecessary; }
            set { this.copyParam.IsPubChemOnlyUseForNecessary = value; OnPropertyChanged("IsPubChemOnlyUseForNecessary"); }
        }

        public bool Chebi
        {
            get { return this.copyParam.DatabaseQuery.Chebi; }
            set { this.copyParam.DatabaseQuery.Chebi = value; OnPropertyChanged("Chebi"); }
        }

        public bool Hmdb
        {
            get { return this.copyParam.DatabaseQuery.Hmdb; }
            set { this.copyParam.DatabaseQuery.Hmdb = value; OnPropertyChanged("Hmdb"); }
        }

        public bool Pubchem
        {
            get { return this.copyParam.DatabaseQuery.Pubchem; }
            set { this.copyParam.DatabaseQuery.Pubchem = value; OnPropertyChanged("Pubchem"); }
        }

        public bool Smpdb
        {
            get { return this.copyParam.DatabaseQuery.Smpdb; }
            set { this.copyParam.DatabaseQuery.Smpdb = value; OnPropertyChanged("Smpdb"); }
        }

        public bool Unpd
        {
            get { return this.copyParam.DatabaseQuery.Unpd; }
            set { this.copyParam.DatabaseQuery.Unpd = value; OnPropertyChanged("Unpd"); }
        }

        public bool Ymdb
        {
            get { return this.copyParam.DatabaseQuery.Ymdb; }
            set { this.copyParam.DatabaseQuery.Ymdb = value; OnPropertyChanged("Ymdb"); }
        }

        public bool Plantcyc
        {
            get { return this.copyParam.DatabaseQuery.Plantcyc; }
            set { this.copyParam.DatabaseQuery.Plantcyc = value; OnPropertyChanged("Plantcyc"); }
        }

        public bool Knapsack
        {
            get { return this.copyParam.DatabaseQuery.Knapsack; }
            set { this.copyParam.DatabaseQuery.Knapsack = value; OnPropertyChanged("Knapsack"); }
        }

        public bool Stoff
        {
            get { return this.copyParam.DatabaseQuery.Stoff; }
            set { this.copyParam.DatabaseQuery.Stoff = value; OnPropertyChanged("Stoff"); }
        }

        public bool Nanpdb {
            get { return this.copyParam.DatabaseQuery.Nanpdb; }
            set { this.copyParam.DatabaseQuery.Nanpdb = value; OnPropertyChanged("Nanpdb"); }
        }

        public bool Bmdb
        {
            get { return this.copyParam.DatabaseQuery.Bmdb; }
            set { this.copyParam.DatabaseQuery.Bmdb = value; OnPropertyChanged("Bmdb"); }
        }

        public bool Drugbank
        {
            get { return this.copyParam.DatabaseQuery.Drugbank; }
            set { this.copyParam.DatabaseQuery.Drugbank = value; OnPropertyChanged("Drugbank"); }
        }

        public bool Ecmdb
        {
            get { return this.copyParam.DatabaseQuery.Ecmdb; }
            set { this.copyParam.DatabaseQuery.Ecmdb = value; OnPropertyChanged("Ecmdb"); }
        }

        public bool Foodb
        {
            get { return this.copyParam.DatabaseQuery.Foodb; }
            set { this.copyParam.DatabaseQuery.Foodb = value; OnPropertyChanged("Foodb"); }
        }

        public bool T3db
        {
            get { return this.copyParam.DatabaseQuery.T3db; }
            set { this.copyParam.DatabaseQuery.T3db = value; OnPropertyChanged("T3db"); }
        }

        public bool Lipidmaps {
            get { return this.copyParam.DatabaseQuery.Lipidmaps; }
            set { this.copyParam.DatabaseQuery.Lipidmaps = value; OnPropertyChanged("Lipidmaps"); }
        }

        public bool Urine {
            get { return this.copyParam.DatabaseQuery.Urine; }
            set { this.copyParam.DatabaseQuery.Urine = value; OnPropertyChanged("Urine"); }
        }

        public bool Saliva {
            get { return this.copyParam.DatabaseQuery.Saliva; }
            set { this.copyParam.DatabaseQuery.Saliva = value; OnPropertyChanged("Saliva"); }
        }

        public bool Feces {
            get { return this.copyParam.DatabaseQuery.Feces; }
            set { this.copyParam.DatabaseQuery.Feces = value; OnPropertyChanged("Feces"); }
        }

        public bool Serum {
            get { return this.copyParam.DatabaseQuery.Serum; }
            set { this.copyParam.DatabaseQuery.Serum = value; OnPropertyChanged("Serum"); }
        }

        public bool Csf
        {
            get { return this.copyParam.DatabaseQuery.Csf; }
            set { this.copyParam.DatabaseQuery.Csf = value; OnPropertyChanged("Csf"); }
        }

        public bool Blexp
        {
            get { return this.copyParam.DatabaseQuery.Blexp; }
            set { this.copyParam.DatabaseQuery.Blexp = value; OnPropertyChanged("Blexp"); }
        }

        public bool Npa
        {
            get { return this.copyParam.DatabaseQuery.Npa; }
            set { this.copyParam.DatabaseQuery.Npa = value; OnPropertyChanged("Npa"); }
        }

        public bool Coconut
        {
            get { return this.copyParam.DatabaseQuery.Coconut; }
            set { this.copyParam.DatabaseQuery.Coconut = value; OnPropertyChanged("Coconut"); }
        }


        public bool IsUserDefinedDB
        {
            get { return this.copyParam.IsUserDefinedDB; }
            set { this.copyParam.IsUserDefinedDB = value; OnPropertyChanged("IsUserDefinedDB"); }
        }

        public string UserDefinedDbFilePath
        {
            get { return this.copyParam.UserDefinedDbFilePath; }
            set { this.copyParam.UserDefinedDbFilePath = value; OnPropertyChanged("UserDefinedDbFilePath"); }
        }

        public bool IsRunSpectralDbSearch
        {
            get { return this.copyParam.IsRunSpectralDbSearch; }
            set { this.copyParam.IsRunSpectralDbSearch = value; OnPropertyChanged("IsRunSpectralDbSearch"); }
        }

        public bool IsRunInSilicoFragmenterSearch
        {
            get { return this.copyParam.IsRunInSilicoFragmenterSearch; }
            set { this.copyParam.IsRunInSilicoFragmenterSearch = value; OnPropertyChanged("IsRunInSilicoFragmenterSearch"); }
        }

        public bool IsPrecursorOrientedSearch
        {
            get { return this.copyParam.IsPrecursorOrientedSearch; }
            set { this.copyParam.IsPrecursorOrientedSearch = value; OnPropertyChanged("IsPrecursorOrientedSearch"); }
        }

        public bool IsUseInternalExperimentalSpectralDb
        {
            get { return this.copyParam.IsUseInternalExperimentalSpectralDb; }
            set { this.copyParam.IsUseInternalExperimentalSpectralDb = value; OnPropertyChanged("IsUseInternalExperimentalSpectralDb"); }
        }

        public bool IsUseInSilicoSpectralDbForLipids
        {
            get { return this.copyParam.IsUseInSilicoSpectralDbForLipids; }
            set { this.copyParam.IsUseInSilicoSpectralDbForLipids = value; OnPropertyChanged("IsUseInSilicoSpectralDbForLipids"); }
        }

        public bool IsUseUserDefinedSpectralDb
        {
            get { return this.copyParam.IsUseUserDefinedSpectralDb; }
            set { this.copyParam.IsUseUserDefinedSpectralDb = value; OnPropertyChanged("IsUseUserDefinedSpectralDb"); }
        }

        public bool IsUseRtForFilteringCandidates {
            get { return this.copyParam.IsUseRtForFilteringCandidates; }
            set { this.copyParam.IsUseRtForFilteringCandidates = value; OnPropertyChanged("IsUseRtForFilteringCandidates"); }
        }

        public string UserDefinedSpectralDbFilePath
        {
            get { return this.copyParam.UserDefinedSpectralDbFilePath; }
            set { this.copyParam.UserDefinedSpectralDbFilePath = value; OnPropertyChanged("UserDefinedSpectralDbFilePath"); }
        }

        public SolventType SolventType
        {
            get { return this.copyParam.SolventType; }
            set { this.copyParam.SolventType = value; OnPropertyChanged("SolventType"); }
        }

        public RetentionType RetentionType {
            get { return this.copyParam.RetentionType; }
            set { this.copyParam.RetentionType = value; OnPropertyChanged("RetentionType"); }
        }

        public double FormulaPredictionTimeOut {
            get { return this.copyParam.FormulaPredictionTimeOut; }
            set { this.copyParam.FormulaPredictionTimeOut = value; OnPropertyChanged("FormulaPredictionTimeOut"); }
        }

        public double StructurePredictionTimeOut {
            get { return this.copyParam.StructurePredictionTimeOut; }
            set { this.copyParam.StructurePredictionTimeOut = value; OnPropertyChanged("StructurePredictionTimeOut"); }
        }

        public bool IsUsePredictedRtForStructureElucidation {
            get {
                return this.copyParam.IsUsePredictedRtForStructureElucidation;
            }

            set {
                this.copyParam.IsUsePredictedRtForStructureElucidation = value;
                OnPropertyChanged("IsUsePredictedRtForStructureElucidation");
                OnPropertyChanged("IsRtInChIKeyActive");
                OnPropertyChanged("IsRtXlogpActive");
            }
        }

        public bool IsUseRtInchikeyLibrary {
            get {
                return this.copyParam.IsUseRtInchikeyLibrary;
            }

            set {
                this.copyParam.IsUseRtInchikeyLibrary = value;
                OnPropertyChanged("IsUseRtInchikeyLibrary");
                OnPropertyChanged("IsRtInChIKeyActive");
                OnPropertyChanged("IsRtXlogpActive");
            }
        }

        public bool IsUseXlogpPrediction {
            get {
                return this.copyParam.IsUseXlogpPrediction;
            }

            set {
                this.copyParam.IsUseXlogpPrediction = value;
                OnPropertyChanged("IsUseXlogpPrediction");
                OnPropertyChanged("IsRtInChIKeyActive");
                OnPropertyChanged("IsRtXlogpActive");
            }
        }

        public bool IsRtInChIKeyActive {
            get {
                return this.copyParam.IsUseRtInchikeyLibrary && this.copyParam.IsUsePredictedRtForStructureElucidation;
            }

            set {
                OnPropertyChanged("IsRtInChIKeyActive");
            }
        }

        public bool IsRtXlogpActive {
            get {
                return this.copyParam.IsUseXlogpPrediction && this.copyParam.IsUsePredictedRtForStructureElucidation;
            }

            set {
                OnPropertyChanged("IsRtXlogpActive");
            }
        }

        public string RtInChIKeyDictionaryFilepath {
            get {
                return this.copyParam.RtInChIKeyDictionaryFilepath;
            }

            set {
                this.copyParam.RtInChIKeyDictionaryFilepath = value;
                OnPropertyChanged("RtInChIKeyDictionaryFilepath");
            }
        }

        public string RtSmilesDictionaryFilepath {
            get {
                return this.copyParam.RtSmilesDictionaryFilepath;
            }

            set {
                this.copyParam.RtSmilesDictionaryFilepath = value;
                OnPropertyChanged("RtSmilesDictionaryFilepath");
            }
        }

        public double Coeff_RtPrediction {
            get {
                return this.copyParam.Coeff_RtPrediction;
            }

            set {
                this.copyParam.Coeff_RtPrediction = value;
                OnPropertyChanged("Coeff_RtPrediction");
            }
        }

        public double Intercept_RtPrediction {
            get {
                return this.copyParam.Intercept_RtPrediction;
            }

            set {
                this.copyParam.Intercept_RtPrediction = value;
                OnPropertyChanged("Intercept_RtPrediction");
            }
        }

        public double RtToleranceForStructureElucidation {
            get {
                return this.copyParam.RtToleranceForStructureElucidation;
            }

            set {
                this.copyParam.RtToleranceForStructureElucidation = value;
                OnPropertyChanged("RtToleranceForStructureElucidation");
            }
        }

        public bool IsUseExperimentalRtForSpectralSearching {
            get {
                return this.copyParam.IsUseExperimentalRtForSpectralSearching;
            }

            set {
                this.copyParam.IsUseExperimentalRtForSpectralSearching = value;
                OnPropertyChanged("IsUseExperimentalRtForSpectralSearching");
            }
        }

        public double RtToleranceForSpectralSearching {
            get {
                return this.copyParam.RtToleranceForSpectralSearching;
            }

            set {
                this.copyParam.RtToleranceForSpectralSearching = value;
                OnPropertyChanged("RtToleranceForSpectralSearching");
            }
        }

        public string RtPredictionSummaryReport {
            get {
                return this.copyParam.RtPredictionSummaryReport;
            }

            set {
                this.copyParam.RtPredictionSummaryReport = value;
                OnPropertyChanged("RtPredictionSummaryReport");
               // if (this.copyParam.RtPredictionSummaryReport != string.Empty)
            }
        }


        public double CcsToleranceForStructureElucidation {
            get {
                return this.copyParam.CcsToleranceForStructureElucidation;
            }

            set {
                this.copyParam.CcsToleranceForStructureElucidation = value;
                OnPropertyChanged("CcsToleranceForStructureElucidation");
            }
        }

        public LipidQueryBean LipidQueries {
            get {
                return this.copyParam.LipidQueryBean;
            }

            set {
                this.copyParam.LipidQueryBean = value;
                OnPropertyChanged("LipidQueries");
            }
        }

        public bool IsUsePredictedCcsForStructureElucidation {
            get {
                return this.copyParam.IsUsePredictedCcsForStructureElucidation;
            }

            set {
                this.copyParam.IsUsePredictedCcsForStructureElucidation = value;
                OnPropertyChanged("IsUsePredictedCcsForStructureElucidation");
            }
        }

        public bool IsUseCcsInchikeyAdductLibrary {
            get {
                return this.copyParam.IsUseCcsInchikeyAdductLibrary;
            }

            set {
                this.copyParam.IsUseCcsInchikeyAdductLibrary = value;
                OnPropertyChanged("IsUseCcsInchikeyAdductLibrary");
            }
        }

        public string CcsAdductInChIKeyDictionaryFilepath {
            get {
                return this.copyParam.CcsAdductInChIKeyDictionaryFilepath;
            }

            set {
                this.copyParam.CcsAdductInChIKeyDictionaryFilepath = value;
                OnPropertyChanged("CcsAdductInChIKeyDictionaryFilepath");
            }
        }

        public bool IsUseExperimentalCcsForSpectralSearching {
            get {
                return this.copyParam.IsUseExperimentalCcsForSpectralSearching;
            }

            set {
                this.copyParam.IsUseExperimentalCcsForSpectralSearching = value;
                OnPropertyChanged("IsUseExperimentalCcsForSpectralSearching");
            }
        }

        public double CcsToleranceForSpectralSearching {
            get {
                return this.copyParam.CcsToleranceForSpectralSearching;
            }

            set {
                this.copyParam.CcsToleranceForSpectralSearching = value;
                OnPropertyChanged("CcsToleranceForSpectralSearching");
            }
        }

        public bool IsUseCcsForFilteringCandidates {
            get {
                return this.copyParam.IsUseCcsForFilteringCandidates;
            }

            set {
                this.copyParam.IsUseCcsForFilteringCandidates = value;
                OnPropertyChanged("IsUseCcsForFilteringCandidates");
            }
        }
        #endregion
    }
}
