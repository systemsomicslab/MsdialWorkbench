using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.DataObj {
    internal class MsfinderParameterSetting : DisposableModelBase {

        public FormulaFinderAdductIonSettingModel FormulaFinderAdductIonSetting { get; }
        public MassToleranceType MassTolType { get; set; }

        public double Mass1Tolerance {  get; set; }

        public double Mass2Tolerance { get; set; }

        public double IsotopicAbundanceTolerance { get; set; }

        public double MassRangeMin {  get; set; }

        public double MassRangeMax { get; set; }

        public double RelativeAbundanceCutOff { get; set; }

        public SolventType SolventType { get; set; }

        public RetentionType RetentionType { get; set; }

        public CoverRange CoverRange { get; set; }

        public bool IsLewisAndSeniorCheck { get; set; }

        public bool IsElementProbabilityCheck { get; set; }

        public bool IsOcheck {  get; set; }

        public bool IsNcheck { get; set; }

        public bool IsPcheck { get; set; }

        public bool IsScheck { get; set; }

        public bool IsFcheck { get; set; }

        public bool IsClCheck { get; set; }

        public bool IsBrCheck { get; set; }

        public bool IsIcheck { get; set; }

        public bool IsSiCheck { get; set; }

        public bool IsNitrogenRule { get; set; }

        public double FormulaScoreCutOff { get; set; }

        public bool CanExcuteMS1AdductSearch { get; set; }

        public bool CanExcuteMS2AdductSearch { get; set; }

        public int FormulaMaximumReportNumber { get; set; }

        public bool IsNeutralLossCheck { get; set; }

        public int TreeDepth { get; set; }

        public double StructureScoreCutOff { get; set; }

        public int StructureMaximumReportNumber { get; set; }

        public bool IsUserDefinedDB { get; set; }

        public string UserDefinedDbFilePath { get; set; }

        public bool IsAllProcess {  get; set; }

        public bool IsUseEiFragmentDB { get; set; }

        public int TryTopNmolecularFormulaSearch {  get; set; }

        public bool IsFormulaFinder {  get; set; }

        public bool IsStructureFinder { get; set; }

        public DatabaseQuery DatabaseQuery { get; set; }
        public bool IsPubChemNeverUse { get; set; }

        public bool IsPubChemOnlyUseForNecessary { get; set; }

        public bool IsPubChemAllTime {  get; set; }

        public bool IsMinesNeverUse { get; set; }

        public bool IsMinesOnlyUseForNecessary { get; set; }
        public bool IsMinesAllTime { get; set; }

        public double CLabelMass { get; set; }

        public double HLabelMass { get; set; }

        public double NLabelMass { get; set; }

        public double OLabelMass { get; set; }

        public double PLabelMass { get; set; }

        public double SLabelMass { get; set; }

        public double FLabelMass { get; set; }

        public double ClLabelMass { get; set; }

        public double BrLabelMass { get; set; }

        public double ILabelMass { get; set; }

        public double SiLabelMass { get; set; }

        public bool IsTmsMeoxDerivative { get; set; }

        public int MinimumTmsCount { get; set; }

        public int MinimumMeoxCount { get; set; }

        public bool IsRunSpectralDbSearch { get; set; }

        public bool IsRunInSilicoFragmenterSearch { get; set; }

        public bool IsPrecursorOrientedSearch { get; set; }

        public bool IsUseInternalExperimentalSpectralDb {  get; set; }

        public bool IsUseInSilicoSpectralDbForLipids { get; set; }

        public bool IsUseUserDefinedSpectralDb { get; set; }
        public string UserDefinedSpectralDbFilePath { get; set; }

        public LipidQueryBean LipidQueryBean { get; set; }

        public double ScoreCutOffForSpectralMatch { get; set; }

        public bool IsUsePredictedRtForStructureElucidation { get; set; }

        public string RtSmilesDictionaryFilepath { get; set; }

        public double Coeff_RtPrediction {  get; set; }

        public double Intercept_RtPrediction { get; set; }

        public double RtToleranceForStructureElucidation { get; set; }

        public bool IsUseRtInchikeyLibrary { get; set; }

        public bool IsUseXlogpPrediction { get; set; }

        public string RtInChIKeyDictionaryFilepath { get; set; }

        public bool IsUseRtForFilteringCandidates { get; set; }

        public bool IsUseExperimentalRtForSpectralSearching { get; set; }

        public double RtToleranceForSpectralSearching { get; set; }

        public string RtPredictionSummaryReport {  get; set; }

        public double FseaRelativeAbundanceCutOff {  get; set; }

        public FseaNonsignificantDef FseanonsignificantDef { get; set; }

        public double FseaPvalueCutOff { get; set; }

        public bool IsMmnLocalCytoscape { get; set; }

        public bool IsMmnMsdialOutput { get; set; }

        public bool IsMmnFormulaBioreaction { get; set; }
        public bool IsMmnRetentionRestrictionUsed { get; set; }

        public bool IsMmnOntologySimilarityUsed { get; set; }

        public double MmnMassTolerance { get; set; }

        public double MmnRelativeCutoff { get; set; }

        public double MmnMassSimilarityCutOff { get; set; }

        public double MmnRtTolerance { get; set; }

        public double MmnOntologySimilarityCutOff { get; set; }

        public string MmnOutputFolderPath { get; set; }

        public double MmnRtToleranceForReaction { get; set; }

        public bool IsMmnSelectedFileCentricProcess { get; set; }

        public double FormulaPredictionTimeOut { get; set; }

        public double StructurePredictionTimeOut { get; set; }

        public List<AdductIon> MS1PositiveAdductIonList { get; set; }

        public List<AdductIon> MS2PositiveAdductIonList { get; set; }

        public List<AdductIon> MS1NegativeAdductIonList { get; set; }

        public List<AdductIon> MS2NegativeAdductIonList { get; set; }

        public double CcsToleranceForStructureElucidation { get; set; }

        public bool IsUsePredictedCcsForStructureElucidation { get; set; }

        public bool IsUseCcsInchikeyAdductLibrary { get; set; }

        public string CcsAdductInChIKeyDictionaryFilepath { get; set; }

        public bool IsUseExperimentalCcsForSpectralSearching { get; set; }

        public double CcsToleranceForSpectralSearching { get; set; }

        public bool IsUseCcsForFilteringCandidates { get; set; }

        public bool IsCreateNewProject {  get; set; }

        public bool IsUseAutoDefinedFolderName { get; set; }

        public string UserDefinedProjectFolderName { get; set; }

        public string ExistProjectPath { get; set; }

        internal readonly AnalysisParamOfMsfinder analysisParameter;
        public MsfinderParameterSetting(ProjectBaseParameter projectParameter) {
            analysisParameter = new AnalysisParamOfMsfinder {
                MS1PositiveAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Positive),
                MS1NegativeAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Negative),
                MS2PositiveAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Positive),
                MS2NegativeAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Negative)
            };
            analysisParameter.MS2PositiveAdductIonList[0].IsIncluded = true;
            analysisParameter.MS2NegativeAdductIonList[0].IsIncluded = true;
            FormulaFinderAdductIonSetting = new FormulaFinderAdductIonSettingModel(analysisParameter, projectParameter.IonMode);

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
            ExistProjectPath = projectParameter.ProjectFolderPath;
            }

        public void Commit() {
            analysisParameter.MassTolType = MassTolType;
            analysisParameter.Mass1Tolerance = Mass1Tolerance;
            analysisParameter.Mass2Tolerance = Mass2Tolerance;
            analysisParameter.IsotopicAbundanceTolerance = IsotopicAbundanceTolerance;
            analysisParameter.MassRangeMin = MassRangeMin;
            analysisParameter.MassRangeMax = MassRangeMax;
            analysisParameter.RelativeAbundanceCutOff = RelativeAbundanceCutOff;
            analysisParameter.SolventType = SolventType;
            analysisParameter.RetentionType = RetentionType;
            analysisParameter.CoverRange = CoverRange;
            analysisParameter.IsLewisAndSeniorCheck = IsLewisAndSeniorCheck;
            analysisParameter.IsElementProbabilityCheck = IsElementProbabilityCheck;
            analysisParameter.IsOcheck = IsOcheck;
            analysisParameter.IsNcheck = IsNcheck;
            analysisParameter.IsPcheck = IsPcheck;
            analysisParameter.IsScheck = IsScheck;
            analysisParameter.IsFcheck = IsFcheck;
            analysisParameter.IsClCheck = IsClCheck;
            analysisParameter.IsBrCheck = IsBrCheck;
            analysisParameter.IsIcheck = IsIcheck;
            analysisParameter.IsSiCheck = IsSiCheck;
            analysisParameter.IsNitrogenRule = IsNitrogenRule;
            analysisParameter.FormulaScoreCutOff = FormulaScoreCutOff;
            analysisParameter.CanExcuteMS1AdductSearch = CanExcuteMS1AdductSearch;
            analysisParameter.CanExcuteMS2AdductSearch = CanExcuteMS2AdductSearch;
            analysisParameter.FormulaMaximumReportNumber = FormulaMaximumReportNumber;
            analysisParameter.IsNeutralLossCheck = IsNeutralLossCheck;
            analysisParameter.TreeDepth = TreeDepth;
            analysisParameter.StructureScoreCutOff = StructureScoreCutOff;
            analysisParameter.StructureMaximumReportNumber = StructureMaximumReportNumber;
            analysisParameter.IsUserDefinedDB = IsUserDefinedDB;
            analysisParameter.UserDefinedDbFilePath = UserDefinedDbFilePath;
            analysisParameter.IsAllProcess = IsAllProcess;
            analysisParameter.IsUseEiFragmentDB = IsUseEiFragmentDB;
            analysisParameter.TryTopNmolecularFormulaSearch = TryTopNmolecularFormulaSearch;
            analysisParameter.IsFormulaFinder = IsFormulaFinder;
            analysisParameter.IsStructureFinder = IsStructureFinder;
            analysisParameter.DatabaseQuery = DatabaseQuery;
            analysisParameter.IsPubChemNeverUse = IsPubChemNeverUse;
            analysisParameter.IsPubChemOnlyUseForNecessary = IsPubChemOnlyUseForNecessary;
            analysisParameter.IsPubChemAllTime = IsPubChemAllTime;
            analysisParameter.IsMinesNeverUse = IsMinesNeverUse;
            analysisParameter.IsMinesOnlyUseForNecessary = IsMinesOnlyUseForNecessary;
            analysisParameter.IsMinesAllTime = IsMinesAllTime;
            analysisParameter.CLabelMass = CLabelMass;
            analysisParameter.HLabelMass = HLabelMass;
            analysisParameter.NLabelMass = NLabelMass;
            analysisParameter.OLabelMass = OLabelMass;
            analysisParameter.PLabelMass = PLabelMass;
            analysisParameter.SLabelMass = SLabelMass;
            analysisParameter.FLabelMass = FLabelMass;
            analysisParameter.ClLabelMass = ClLabelMass;
            analysisParameter.BrLabelMass = BrLabelMass;
            analysisParameter.ILabelMass = ILabelMass;
            analysisParameter.SiLabelMass = SiLabelMass;
            analysisParameter.IsTmsMeoxDerivative = IsTmsMeoxDerivative;
            analysisParameter.MinimumTmsCount = MinimumTmsCount;
            analysisParameter.MinimumMeoxCount = MinimumMeoxCount;
            analysisParameter.IsRunSpectralDbSearch = IsRunSpectralDbSearch;
            analysisParameter.IsRunInSilicoFragmenterSearch = IsRunInSilicoFragmenterSearch;
            analysisParameter.IsPrecursorOrientedSearch = IsPrecursorOrientedSearch;
            analysisParameter.IsUseInternalExperimentalSpectralDb = IsUseInternalExperimentalSpectralDb;
            analysisParameter.IsUseInSilicoSpectralDbForLipids = IsUseInSilicoSpectralDbForLipids;
            analysisParameter.IsUseUserDefinedSpectralDb = IsUseUserDefinedSpectralDb;
            analysisParameter.UserDefinedSpectralDbFilePath = UserDefinedSpectralDbFilePath;
            analysisParameter.LipidQueryBean = LipidQueryBean;
            analysisParameter.ScoreCutOffForSpectralMatch = ScoreCutOffForSpectralMatch;
            analysisParameter.IsUsePredictedRtForStructureElucidation = IsUsePredictedRtForStructureElucidation;
            analysisParameter.RtSmilesDictionaryFilepath = RtSmilesDictionaryFilepath;
            analysisParameter.Coeff_RtPrediction = Coeff_RtPrediction;
            analysisParameter.Intercept_RtPrediction = Intercept_RtPrediction;
            analysisParameter.RtToleranceForStructureElucidation = RtToleranceForStructureElucidation;
            analysisParameter.IsUseRtInchikeyLibrary = IsUseRtInchikeyLibrary;
            analysisParameter.IsUseXlogpPrediction = IsUseXlogpPrediction;
            analysisParameter.RtInChIKeyDictionaryFilepath = RtInChIKeyDictionaryFilepath;
            analysisParameter.IsUseRtForFilteringCandidates = IsUseRtForFilteringCandidates;
            analysisParameter.IsUseExperimentalRtForSpectralSearching = IsUseExperimentalRtForSpectralSearching;
            analysisParameter.RtToleranceForSpectralSearching = RtToleranceForSpectralSearching;
            analysisParameter.RtPredictionSummaryReport = RtPredictionSummaryReport;
            analysisParameter.FseaRelativeAbundanceCutOff = FseaRelativeAbundanceCutOff;
            analysisParameter.FseanonsignificantDef = FseanonsignificantDef;
            analysisParameter.FseaPvalueCutOff = FseaPvalueCutOff;
            analysisParameter.IsMmnLocalCytoscape = IsMmnLocalCytoscape;
            analysisParameter.IsMmnMsdialOutput = IsMmnMsdialOutput;
            analysisParameter.IsMmnFormulaBioreaction = IsMmnFormulaBioreaction;
            analysisParameter.IsMmnRetentionRestrictionUsed = IsMmnRetentionRestrictionUsed;
            analysisParameter.IsMmnOntologySimilarityUsed = IsMmnOntologySimilarityUsed;
            analysisParameter.MmnMassTolerance = MmnMassTolerance;
            analysisParameter.MmnRelativeCutoff = MmnRelativeCutoff;
            analysisParameter.MmnMassSimilarityCutOff = MmnMassSimilarityCutOff;
            analysisParameter.MmnRtTolerance = MmnRtTolerance;
            analysisParameter.MmnOntologySimilarityCutOff = MmnOntologySimilarityCutOff;
            analysisParameter.MmnOutputFolderPath = MmnOutputFolderPath;
            analysisParameter.MmnRtToleranceForReaction = MmnRtToleranceForReaction;
            analysisParameter.IsMmnSelectedFileCentricProcess = IsMmnSelectedFileCentricProcess;
            analysisParameter.FormulaPredictionTimeOut = FormulaPredictionTimeOut;
            analysisParameter.StructurePredictionTimeOut = StructurePredictionTimeOut;
            analysisParameter.MS1PositiveAdductIonList = MS1PositiveAdductIonList;
            analysisParameter.MS2PositiveAdductIonList = MS2PositiveAdductIonList;
            analysisParameter.MS1NegativeAdductIonList = MS1NegativeAdductIonList;
            analysisParameter.MS2NegativeAdductIonList = MS2NegativeAdductIonList;
            analysisParameter.CcsToleranceForStructureElucidation = CcsToleranceForStructureElucidation;
            analysisParameter.IsUsePredictedCcsForStructureElucidation = IsUsePredictedCcsForStructureElucidation;
            analysisParameter.IsUseCcsInchikeyAdductLibrary = IsUseCcsInchikeyAdductLibrary;
            analysisParameter.CcsAdductInChIKeyDictionaryFilepath = CcsAdductInChIKeyDictionaryFilepath;
            analysisParameter.IsUseExperimentalCcsForSpectralSearching = IsUseExperimentalCcsForSpectralSearching;
            analysisParameter.CcsToleranceForSpectralSearching = CcsToleranceForSpectralSearching;
            analysisParameter.IsUseCcsForFilteringCandidates = IsUseCcsForFilteringCandidates;

            FormulaFinderAdductIonSetting.Commit();
        }

        public void Cancel() {
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
