using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.CommonSourceGenerator.MVVM;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.DataObj
{
    [BufferedBindableType(typeof(AnalysisParamOfMsfinder))]
    internal partial class MsfinderParameterSetting {

        public FormulaFinderAdductIonSettingModel FormulaFinderAdductIonSetting { get; }

        public bool IsCreateNewProject {
            get => _isCreateNewProject;
            set
            {
                if (_isCreateNewProject != value)
                {
                    _isCreateNewProject = value;
                    RaisePropertyChanged(nameof(IsCreateNewProject));
                }
            }
        }
        private bool _isCreateNewProject;

        public bool IsUseAutoDefinedFolderName {
            get => _isUseAutoDefinedFolderName;
            set
            {
                if (_isUseAutoDefinedFolderName != value)
                {
                    _isUseAutoDefinedFolderName = value;
                    RaisePropertyChanged(nameof(IsUseAutoDefinedFolderName));
                }
            }
        }
        private bool _isUseAutoDefinedFolderName;

        public string UserDefinedProjectFolderName {
            get => _userDefinedProjectFolderName;
            set
            {
                if (_userDefinedProjectFolderName != value)
                {
                    _userDefinedProjectFolderName = value;
                    RaisePropertyChanged(nameof(UserDefinedProjectFolderName));
                }
            }
        }
        private string _userDefinedProjectFolderName;

        public string ExistProjectPath {
            get => _existProjectPath;
            set
            {
                if (_existProjectPath != value)
                {
                    _existProjectPath = value;
                    RaisePropertyChanged(nameof(ExistProjectPath));
                }
            }
        }
        private string _existProjectPath;

        internal AnalysisParamOfMsfinder AnalysisParameter => _innerModel;

        private MsfinderParameterSetting(ProjectBaseParameter projectParameter, AnalysisParamOfMsfinder analysisParameter) : this(analysisParameter) {
            FormulaFinderAdductIonSetting = new FormulaFinderAdductIonSettingModel(analysisParameter, projectParameter.IonMode);

            IsCreateNewProject = true;
            IsUseAutoDefinedFolderName = true;
            UserDefinedProjectFolderName = "";
            ExistProjectPath = projectParameter.ProjectFolderPath;
        }

        public static MsfinderParameterSetting CreateSetting(ProjectBaseParameter projectParameter)
        {
            var analysisParameter = new AnalysisParamOfMsfinder
            {
                MS1PositiveAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Positive),
                MS1NegativeAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Negative),
                MS2PositiveAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Positive),
                MS2NegativeAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Negative)
            };
            analysisParameter.MS2PositiveAdductIonList[0].IsIncluded = true;
            analysisParameter.MS2NegativeAdductIonList[0].IsIncluded = true;
            return new MsfinderParameterSetting(projectParameter, analysisParameter);
        }

        partial void Committed() {
            FormulaFinderAdductIonSetting.Commit();
        }

        public void Cancel() {
            var analysisParameter = _innerModel;
            MassTolType = analysisParameter.MassTolType;
            Mass1Tolerance = analysisParameter.Mass1Tolerance;
            Mass2Tolerance = analysisParameter.Mass2Tolerance;
            IsotopicAbundanceTolerance = analysisParameter.IsotopicAbundanceTolerance;
            MassRangeMin = analysisParameter.MassRangeMin;
            MassRangeMax = analysisParameter.MassRangeMax;
            RelativeAbundanceCutOff = analysisParameter.RelativeAbundanceCutOff;
            SolventType = analysisParameter.SolventType;
            RetentionType = analysisParameter.RetentionType;
            CoverRange = analysisParameter.CoverRange;
            IsLewisAndSeniorCheck = analysisParameter.IsLewisAndSeniorCheck;
            IsElementProbabilityCheck = analysisParameter.IsElementProbabilityCheck;
            IsOcheck = analysisParameter.IsOcheck;
            IsNcheck = analysisParameter.IsNcheck;
            IsPcheck = analysisParameter.IsPcheck;
            IsScheck = analysisParameter.IsScheck;
            IsFcheck = analysisParameter.IsFcheck;
            IsClCheck = analysisParameter.IsClCheck;
            IsBrCheck = analysisParameter.IsBrCheck;
            IsIcheck = analysisParameter.IsIcheck;
            IsSiCheck = analysisParameter.IsSiCheck;
            IsNitrogenRule = analysisParameter.IsNitrogenRule;
            FormulaScoreCutOff = analysisParameter.FormulaScoreCutOff;
            CanExcuteMS1AdductSearch = analysisParameter.CanExcuteMS1AdductSearch;
            CanExcuteMS2AdductSearch = analysisParameter.CanExcuteMS2AdductSearch;
            FormulaMaximumReportNumber = analysisParameter.FormulaMaximumReportNumber;
            IsNeutralLossCheck = analysisParameter.IsNeutralLossCheck;
            TreeDepth = analysisParameter.TreeDepth;
            StructureScoreCutOff = analysisParameter.StructureScoreCutOff;
            StructureMaximumReportNumber = analysisParameter.StructureMaximumReportNumber;
            IsUserDefinedDB = analysisParameter.IsUserDefinedDB;
            UserDefinedDbFilePath = analysisParameter.UserDefinedDbFilePath;
            IsAllProcess = analysisParameter.IsAllProcess;
            IsUseEiFragmentDB = analysisParameter.IsUseEiFragmentDB;
            TryTopNmolecularFormulaSearch = analysisParameter.TryTopNmolecularFormulaSearch;
            IsFormulaFinder = analysisParameter.IsFormulaFinder;
            IsStructureFinder = analysisParameter.IsStructureFinder;
            DatabaseQuery = analysisParameter.DatabaseQuery;
            IsPubChemNeverUse = analysisParameter.IsPubChemNeverUse;
            IsPubChemOnlyUseForNecessary = analysisParameter.IsPubChemOnlyUseForNecessary;
            IsPubChemAllTime = analysisParameter.IsPubChemAllTime;
            IsMinesNeverUse = analysisParameter.IsMinesNeverUse;
            IsMinesOnlyUseForNecessary = analysisParameter.IsMinesOnlyUseForNecessary;
            IsMinesAllTime = analysisParameter.IsMinesAllTime;
            CLabelMass = analysisParameter.CLabelMass;
            HLabelMass = analysisParameter.HLabelMass;
            NLabelMass = analysisParameter.NLabelMass;
            OLabelMass = analysisParameter.OLabelMass;
            PLabelMass = analysisParameter.PLabelMass;
            SLabelMass = analysisParameter.SLabelMass;
            FLabelMass = analysisParameter.FLabelMass;
            ClLabelMass = analysisParameter.ClLabelMass;
            BrLabelMass = analysisParameter.BrLabelMass;
            ILabelMass = analysisParameter.ILabelMass;
            SiLabelMass = analysisParameter.SiLabelMass;
            IsTmsMeoxDerivative = analysisParameter.IsTmsMeoxDerivative;
            MinimumTmsCount = analysisParameter.MinimumTmsCount;
            MinimumMeoxCount = analysisParameter.MinimumMeoxCount;
            IsRunSpectralDbSearch = analysisParameter.IsRunSpectralDbSearch;
            IsRunInSilicoFragmenterSearch = analysisParameter.IsRunInSilicoFragmenterSearch;
            IsPrecursorOrientedSearch = analysisParameter.IsPrecursorOrientedSearch;
            IsUseInternalExperimentalSpectralDb = analysisParameter.IsUseInternalExperimentalSpectralDb;
            IsUseInSilicoSpectralDbForLipids = analysisParameter.IsUseInSilicoSpectralDbForLipids;
            IsUseUserDefinedSpectralDb = analysisParameter.IsUseUserDefinedSpectralDb;
            UserDefinedSpectralDbFilePath = analysisParameter.UserDefinedSpectralDbFilePath;
            LipidQueryBean = analysisParameter.LipidQueryBean;
            ScoreCutOffForSpectralMatch = analysisParameter.ScoreCutOffForSpectralMatch;
            IsUsePredictedRtForStructureElucidation = analysisParameter.IsUsePredictedRtForStructureElucidation;
            RtSmilesDictionaryFilepath = analysisParameter.RtSmilesDictionaryFilepath;
            Coeff_RtPrediction = analysisParameter.Coeff_RtPrediction;
            Intercept_RtPrediction = analysisParameter.Intercept_RtPrediction;
            RtToleranceForStructureElucidation = analysisParameter.RtToleranceForStructureElucidation;
            IsUseRtInchikeyLibrary = analysisParameter.IsUseRtInchikeyLibrary;
            IsUseXlogpPrediction = analysisParameter.IsUseXlogpPrediction;
            RtInChIKeyDictionaryFilepath = analysisParameter.RtInChIKeyDictionaryFilepath;
            IsUseRtForFilteringCandidates = analysisParameter.IsUseRtForFilteringCandidates;
            IsUseExperimentalRtForSpectralSearching = analysisParameter.IsUseExperimentalRtForSpectralSearching;
            RtToleranceForSpectralSearching = analysisParameter.RtToleranceForSpectralSearching;
            RtPredictionSummaryReport = analysisParameter.RtPredictionSummaryReport;
            FseaRelativeAbundanceCutOff = analysisParameter.FseaRelativeAbundanceCutOff;
            FseanonsignificantDef = analysisParameter.FseanonsignificantDef;
            FseaPvalueCutOff = analysisParameter.FseaPvalueCutOff;
            IsMmnLocalCytoscape = analysisParameter.IsMmnLocalCytoscape;
            IsMmnMsdialOutput = analysisParameter.IsMmnMsdialOutput;
            IsMmnFormulaBioreaction = analysisParameter.IsMmnFormulaBioreaction;
            IsMmnRetentionRestrictionUsed = analysisParameter.IsMmnRetentionRestrictionUsed;
            IsMmnOntologySimilarityUsed = analysisParameter.IsMmnOntologySimilarityUsed;
            MmnMassTolerance = analysisParameter.MmnMassTolerance;
            MmnRelativeCutoff = analysisParameter.MmnRelativeCutoff;
            MmnMassSimilarityCutOff = analysisParameter.MmnMassSimilarityCutOff;
            MmnRtTolerance = analysisParameter.MmnRtTolerance;
            MmnOntologySimilarityCutOff = analysisParameter.MmnOntologySimilarityCutOff;
            MmnOutputFolderPath = analysisParameter.MmnOutputFolderPath;
            MmnRtToleranceForReaction = analysisParameter.MmnRtToleranceForReaction;
            IsMmnSelectedFileCentricProcess = analysisParameter.IsMmnSelectedFileCentricProcess;
            FormulaPredictionTimeOut = analysisParameter.FormulaPredictionTimeOut;
            StructurePredictionTimeOut = analysisParameter.StructurePredictionTimeOut;
            MS1PositiveAdductIonList = analysisParameter.MS1PositiveAdductIonList;
            MS2PositiveAdductIonList = analysisParameter.MS2PositiveAdductIonList;
            MS1NegativeAdductIonList = analysisParameter.MS1NegativeAdductIonList;
            MS2NegativeAdductIonList = analysisParameter.MS2NegativeAdductIonList;
            CcsToleranceForStructureElucidation = analysisParameter.CcsToleranceForStructureElucidation;
            IsUsePredictedCcsForStructureElucidation = analysisParameter.IsUsePredictedCcsForStructureElucidation;
            IsUseCcsInchikeyAdductLibrary = analysisParameter.IsUseCcsInchikeyAdductLibrary;
            CcsAdductInChIKeyDictionaryFilepath = analysisParameter.CcsAdductInChIKeyDictionaryFilepath;
            IsUseExperimentalCcsForSpectralSearching = analysisParameter.IsUseExperimentalCcsForSpectralSearching;
            CcsToleranceForSpectralSearching = analysisParameter.CcsToleranceForSpectralSearching;
            IsUseCcsForFilteringCandidates = analysisParameter.IsUseCcsForFilteringCandidates;
            IsCreateNewProject = true;
            IsUseAutoDefinedFolderName = true;
            UserDefinedProjectFolderName = "";
        }

    }
}
