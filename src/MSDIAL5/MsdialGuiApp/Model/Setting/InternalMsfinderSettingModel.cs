using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.Common.StructureFinder.Parser;
using CompMs.Common.Utility;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using FragmentDbParser = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser;

namespace CompMs.App.Msdial.Model.Setting
{
    internal class InternalMsfinderSettingModel : BindableBase
    {

        internal readonly AnalysisParamOfMsfinder parameter;
        internal readonly AlignmentSpectraExportGroupModel exporter;

        public InternalMsfinderSettingModel(ProjectBaseParameter projectParameter, AlignmentSpectraExportGroupModel exporter, ReadOnlyReactivePropertySlim<IAlignmentModel?> currentAlignmentModel) {
            this.exporter = exporter;
            this.CurrentAlignmentModel = currentAlignmentModel;
            parameter = new AnalysisParamOfMsfinder();
            parameter.MS1PositiveAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Positive);
            parameter.MS1NegativeAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Negative);
            parameter.MS2PositiveAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Positive);
            parameter.MS2NegativeAdductIonList = AdductResourceParser.GetAdductIonInformationList(IonMode.Negative);
            parameter.MS2PositiveAdductIonList[0].IsIncluded = true;
            parameter.MS2NegativeAdductIonList[0].IsIncluded = true;
            FormulaFinderAdductIonSetting = new FormulaFinderAdductIonSettingModel(parameter, projectParameter.IonMode);

            massTolType = parameter.MassTolType;
            mass1Tolerance = parameter.Mass1Tolerance;
            mass2Tolerance = parameter.Mass2Tolerance;
            isotopicAbundanceTolerance = parameter.IsotopicAbundanceTolerance;
            massRangeMin = parameter.MassRangeMin;
            massRangeMax = parameter.MassRangeMax;
            relativeAbundanceCutOff = parameter.RelativeAbundanceCutOff;
            solventType = parameter.SolventType;
            retentionType = parameter.RetentionType;
            coverRange = parameter.CoverRange;
            isLewisAndSeniorCheck = parameter.IsLewisAndSeniorCheck;
            isElementProbabilityCheck = parameter.IsElementProbabilityCheck;
            isOcheck = parameter.IsOcheck;
            isNcheck = parameter.IsNcheck;
            isPcheck = parameter.IsPcheck;
            isScheck = parameter.IsScheck;
            isFcheck = parameter.IsFcheck;
            isClCheck = parameter.IsClCheck;
            isBrCheck = parameter.IsBrCheck;
            isIcheck = parameter.IsIcheck;
            isSiCheck = parameter.IsSiCheck;
            isNitrogenRule = parameter.IsNitrogenRule;
            formulaScoreCutOff = parameter.FormulaScoreCutOff;
            canExcuteMS1AdductSearch = parameter.CanExcuteMS1AdductSearch;
            canExcuteMS2AdductSearch = parameter.CanExcuteMS2AdductSearch;
            formulaMaximumReportNumber = parameter.FormulaMaximumReportNumber;
            isNeutralLossCheck = parameter.IsNeutralLossCheck;
            treeDepth = parameter.TreeDepth;
            structureScoreCutOff = parameter.StructureScoreCutOff;
            structureMaximumReportNumber = parameter.StructureMaximumReportNumber;
            isUserDefinedDB = parameter.IsUserDefinedDB;
            userDefinedDbFilePath = parameter.UserDefinedDbFilePath;
            isAllProcess = parameter.IsAllProcess;
            isUseEiFragmentDB = parameter.IsUseEiFragmentDB;
            tryTopNmolecularFormulaSearch = parameter.TryTopNmolecularFormulaSearch;
            isFormulaFinder = parameter.IsFormulaFinder;
            isStructureFinder = parameter.IsStructureFinder;
            databaseQuery = parameter.DatabaseQuery;
            isPubChemNeverUse = parameter.IsPubChemNeverUse;
            isPubChemOnlyUseForNecessary = parameter.IsPubChemOnlyUseForNecessary;
            isPubChemAllTime = parameter.IsPubChemAllTime;
            isMinesNeverUse = parameter.IsMinesNeverUse;
            isMinesOnlyUseForNecessary = parameter.IsMinesOnlyUseForNecessary;
            isMinesAllTime = parameter.IsMinesAllTime;
            cLabelMass = parameter.CLabelMass;
            hLabelMass = parameter.HLabelMass;
            nLabelMass = parameter.NLabelMass;
            oLabelMass = parameter.OLabelMass;
            pLabelMass = parameter.PLabelMass;
            sLabelMass = parameter.SLabelMass;
            fLabelMass = parameter.FLabelMass;
            clLabelMass = parameter.ClLabelMass;
            brLabelMass = parameter.BrLabelMass;
            iLabelMass = parameter.ILabelMass;
            siLabelMass = parameter.SiLabelMass;
            isTmsMeoxDerivative = parameter.IsTmsMeoxDerivative;
            minimumTmsCount = parameter.MinimumTmsCount;
            minimumMeoxCount = parameter.MinimumMeoxCount;
            isRunSpectralDbSearch = parameter.IsRunSpectralDbSearch;
            isRunInSilicoFragmenterSearch = parameter.IsRunInSilicoFragmenterSearch;
            isPrecursorOrientedSearch = parameter.IsPrecursorOrientedSearch;
            isUseInternalExperimentalSpectralDb = parameter.IsUseInternalExperimentalSpectralDb;
            isUseInSilicoSpectralDbForLipids = parameter.IsUseInSilicoSpectralDbForLipids;
            isUseUserDefinedSpectralDb = parameter.IsUseUserDefinedSpectralDb;
            userDefinedSpectralDbFilePath = parameter.UserDefinedSpectralDbFilePath;
            lipidQueryBean = parameter.LipidQueryBean;
            scoreCutOffForSpectralMatch = parameter.ScoreCutOffForSpectralMatch;
            isUsePredictedRtForStructureElucidation = parameter.IsUsePredictedRtForStructureElucidation;
            rtSmilesDictionaryFilepath = parameter.RtSmilesDictionaryFilepath;
            coeff_RtPrediction = parameter.Coeff_RtPrediction;
            intercept_RtPrediction = parameter.Intercept_RtPrediction;
            rtToleranceForStructureElucidation = parameter.RtToleranceForStructureElucidation;
            isUseRtInchikeyLibrary = parameter.IsUseRtInchikeyLibrary;
            isUseXlogpPrediction = parameter.IsUseXlogpPrediction;
            rtInChIKeyDictionaryFilepath = parameter.RtInChIKeyDictionaryFilepath;
            isUseRtForFilteringCandidates = parameter.IsUseRtForFilteringCandidates;
            isUseExperimentalRtForSpectralSearching = parameter.IsUseExperimentalRtForSpectralSearching;
            rtToleranceForSpectralSearching = parameter.RtToleranceForSpectralSearching;
            rtPredictionSummaryReport = parameter.RtPredictionSummaryReport;
            fseaRelativeAbundanceCutOff = parameter.FseaRelativeAbundanceCutOff;
            fseanonsignificantDef = parameter.FseanonsignificantDef;
            fseaPvalueCutOff = parameter.FseaPvalueCutOff;
            isMmnLocalCytoscape = parameter.IsMmnLocalCytoscape;
            isMmnMsdialOutput = parameter.IsMmnMsdialOutput;
            isMmnFormulaBioreaction = parameter.IsMmnFormulaBioreaction;
            isMmnRetentionRestrictionUsed = parameter.IsMmnRetentionRestrictionUsed;
            isMmnOntologySimilarityUsed = parameter.IsMmnOntologySimilarityUsed;
            mmnMassTolerance = parameter.MmnMassTolerance;
            mmnRelativeCutoff = parameter.MmnRelativeCutoff;
            mmnMassSimilarityCutOff = parameter.MmnMassSimilarityCutOff;
            mmnRtTolerance = parameter.MmnRtTolerance;
            mmnOntologySimilarityCutOff = parameter.MmnOntologySimilarityCutOff;
            mmnOutputFolderPath = parameter.MmnOutputFolderPath;
            mmnRtToleranceForReaction = parameter.MmnRtToleranceForReaction;
            isMmnSelectedFileCentricProcess = parameter.IsMmnSelectedFileCentricProcess;
            formulaPredictionTimeOut = parameter.FormulaPredictionTimeOut;
            structurePredictionTimeOut = parameter.StructurePredictionTimeOut;
            mS1PositiveAdductIonList = parameter.MS1PositiveAdductIonList;
            mS2PositiveAdductIonList = parameter.MS2PositiveAdductIonList;
            mS1NegativeAdductIonList = parameter.MS1NegativeAdductIonList;
            mS2NegativeAdductIonList = parameter.MS2NegativeAdductIonList;
            ccsToleranceForStructureElucidation = parameter.CcsToleranceForStructureElucidation;
            isUsePredictedCcsForStructureElucidation = parameter.IsUsePredictedCcsForStructureElucidation;
            isUseCcsInchikeyAdductLibrary = parameter.IsUseCcsInchikeyAdductLibrary;
            ccsAdductInChIKeyDictionaryFilepath = parameter.CcsAdductInChIKeyDictionaryFilepath;
            isUseExperimentalCcsForSpectralSearching = parameter.IsUseExperimentalCcsForSpectralSearching;
            ccsToleranceForSpectralSearching = parameter.CcsToleranceForSpectralSearching;
            isUseCcsForFilteringCandidates = parameter.IsUseCcsForFilteringCandidates;
            isCreateNewProject = true;
            isUseAutoDefinedFolderName = true;
            userDefinedProjectFolderName = "";
            existProjectPath = projectParameter.ProjectFolderPath;
        }

        public void Commit()
        {
            parameter.MassTolType = MassTolType;
            parameter.Mass1Tolerance = Mass1Tolerance;
            parameter.Mass2Tolerance = Mass2Tolerance;
            parameter.IsotopicAbundanceTolerance = IsotopicAbundanceTolerance;
            parameter.MassRangeMin = MassRangeMin;
            parameter.MassRangeMax = MassRangeMax;
            parameter.RelativeAbundanceCutOff = RelativeAbundanceCutOff;
            parameter.SolventType = SolventType;
            parameter.RetentionType = RetentionType;
            parameter.CoverRange = CoverRange;
            parameter.IsLewisAndSeniorCheck = IsLewisAndSeniorCheck;
            parameter.IsElementProbabilityCheck = IsElementProbabilityCheck;
            parameter.IsOcheck = IsOcheck;
            parameter.IsNcheck = IsNcheck;
            parameter.IsPcheck = IsPcheck;
            parameter.IsScheck = IsScheck;
            parameter.IsFcheck = IsFcheck;
            parameter.IsClCheck = IsClCheck;
            parameter.IsBrCheck = IsBrCheck;
            parameter.IsIcheck = IsIcheck;
            parameter.IsSiCheck = IsSiCheck;
            parameter.IsNitrogenRule = IsNitrogenRule;
            parameter.FormulaScoreCutOff = FormulaScoreCutOff;
            parameter.CanExcuteMS1AdductSearch = CanExcuteMS1AdductSearch;
            parameter.CanExcuteMS2AdductSearch = CanExcuteMS2AdductSearch;
            parameter.FormulaMaximumReportNumber = FormulaMaximumReportNumber;
            parameter.IsNeutralLossCheck = IsNeutralLossCheck;
            parameter.TreeDepth = TreeDepth;
            parameter.StructureScoreCutOff = StructureScoreCutOff;
            parameter.StructureMaximumReportNumber = StructureMaximumReportNumber;
            parameter.IsUserDefinedDB = IsUserDefinedDB;
            parameter.UserDefinedDbFilePath = UserDefinedDbFilePath;
            parameter.IsAllProcess = IsAllProcess;
            parameter.IsUseEiFragmentDB = IsUseEiFragmentDB;
            parameter.TryTopNmolecularFormulaSearch = TryTopNmolecularFormulaSearch;
            parameter.IsFormulaFinder = IsFormulaFinder;
            parameter.IsStructureFinder = IsStructureFinder;
            parameter.DatabaseQuery = DatabaseQuery;
            parameter.IsPubChemNeverUse = IsPubChemNeverUse;
            parameter.IsPubChemOnlyUseForNecessary = IsPubChemOnlyUseForNecessary;
            parameter.IsPubChemAllTime = IsPubChemAllTime;
            parameter.IsMinesNeverUse = IsMinesNeverUse;
            parameter.IsMinesOnlyUseForNecessary = IsMinesOnlyUseForNecessary;
            parameter.IsMinesAllTime = IsMinesAllTime;
            parameter.CLabelMass = CLabelMass;
            parameter.HLabelMass = HLabelMass;
            parameter.NLabelMass = NLabelMass;
            parameter.OLabelMass = OLabelMass;
            parameter.PLabelMass = PLabelMass;
            parameter.SLabelMass = SLabelMass;
            parameter.FLabelMass = FLabelMass;
            parameter.ClLabelMass = ClLabelMass;
            parameter.BrLabelMass = BrLabelMass;
            parameter.ILabelMass = ILabelMass;
            parameter.SiLabelMass = SiLabelMass;
            parameter.IsTmsMeoxDerivative = IsTmsMeoxDerivative;
            parameter.MinimumTmsCount = MinimumTmsCount;
            parameter.MinimumMeoxCount = MinimumMeoxCount;
            parameter.IsRunSpectralDbSearch = IsRunSpectralDbSearch;
            parameter.IsRunInSilicoFragmenterSearch = IsRunInSilicoFragmenterSearch;
            parameter.IsPrecursorOrientedSearch = IsPrecursorOrientedSearch;
            parameter.IsUseInternalExperimentalSpectralDb = IsUseInternalExperimentalSpectralDb;
            parameter.IsUseInSilicoSpectralDbForLipids = IsUseInSilicoSpectralDbForLipids;
            parameter.IsUseUserDefinedSpectralDb = IsUseUserDefinedSpectralDb;
            parameter.UserDefinedSpectralDbFilePath = UserDefinedSpectralDbFilePath;
            parameter.LipidQueryBean = LipidQueryBean;
            parameter.ScoreCutOffForSpectralMatch = ScoreCutOffForSpectralMatch;
            parameter.IsUsePredictedRtForStructureElucidation = IsUsePredictedRtForStructureElucidation;
            parameter.RtSmilesDictionaryFilepath = RtSmilesDictionaryFilepath;
            parameter.Coeff_RtPrediction = Coeff_RtPrediction;
            parameter.Intercept_RtPrediction = Intercept_RtPrediction;
            parameter.RtToleranceForStructureElucidation = RtToleranceForStructureElucidation;
            parameter.IsUseRtInchikeyLibrary = IsUseRtInchikeyLibrary;
            parameter.IsUseXlogpPrediction = IsUseXlogpPrediction;
            parameter.RtInChIKeyDictionaryFilepath = RtInChIKeyDictionaryFilepath;
            parameter.IsUseRtForFilteringCandidates = IsUseRtForFilteringCandidates;
            parameter.IsUseExperimentalRtForSpectralSearching = IsUseExperimentalRtForSpectralSearching;
            parameter.RtToleranceForSpectralSearching = RtToleranceForSpectralSearching;
            parameter.RtPredictionSummaryReport = RtPredictionSummaryReport;
            parameter.FseaRelativeAbundanceCutOff = FseaRelativeAbundanceCutOff;
            parameter.FseanonsignificantDef = FseanonsignificantDef;
            parameter.FseaPvalueCutOff = FseaPvalueCutOff;
            parameter.IsMmnLocalCytoscape = IsMmnLocalCytoscape;
            parameter.IsMmnMsdialOutput = IsMmnMsdialOutput;
            parameter.IsMmnFormulaBioreaction = IsMmnFormulaBioreaction;
            parameter.IsMmnRetentionRestrictionUsed = IsMmnRetentionRestrictionUsed;
            parameter.IsMmnOntologySimilarityUsed = IsMmnOntologySimilarityUsed;
            parameter.MmnMassTolerance = MmnMassTolerance;
            parameter.MmnRelativeCutoff = MmnRelativeCutoff;
            parameter.MmnMassSimilarityCutOff = MmnMassSimilarityCutOff;
            parameter.MmnRtTolerance = MmnRtTolerance;
            parameter.MmnOntologySimilarityCutOff = MmnOntologySimilarityCutOff;
            parameter.MmnOutputFolderPath = MmnOutputFolderPath;
            parameter.MmnRtToleranceForReaction = MmnRtToleranceForReaction;
            parameter.IsMmnSelectedFileCentricProcess = IsMmnSelectedFileCentricProcess;
            parameter.FormulaPredictionTimeOut = FormulaPredictionTimeOut;
            parameter.StructurePredictionTimeOut = StructurePredictionTimeOut;
            parameter.MS1PositiveAdductIonList = MS1PositiveAdductIonList;
            parameter.MS2PositiveAdductIonList = MS2PositiveAdductIonList;
            parameter.MS1NegativeAdductIonList = MS1NegativeAdductIonList;
            parameter.MS2NegativeAdductIonList = MS2NegativeAdductIonList;
            parameter.CcsToleranceForStructureElucidation = CcsToleranceForStructureElucidation;
            parameter.IsUsePredictedCcsForStructureElucidation = IsUsePredictedCcsForStructureElucidation;
            parameter.IsUseCcsInchikeyAdductLibrary = IsUseCcsInchikeyAdductLibrary;
            parameter.CcsAdductInChIKeyDictionaryFilepath = CcsAdductInChIKeyDictionaryFilepath;
            parameter.IsUseExperimentalCcsForSpectralSearching = IsUseExperimentalCcsForSpectralSearching;
            parameter.CcsToleranceForSpectralSearching = CcsToleranceForSpectralSearching;
            parameter.IsUseCcsForFilteringCandidates = IsUseCcsForFilteringCandidates;

            FormulaFinderAdductIonSetting.Commit();
        }

        public void Cancel() {
            MassTolType = parameter.MassTolType;
            Mass1Tolerance = parameter.Mass1Tolerance;
            Mass2Tolerance = parameter.Mass2Tolerance;
            IsotopicAbundanceTolerance = parameter.IsotopicAbundanceTolerance;
            MassRangeMin = parameter.MassRangeMin;
            massRangeMax = parameter.MassRangeMax;
            relativeAbundanceCutOff = parameter.RelativeAbundanceCutOff;
            solventType = parameter.SolventType;
            retentionType = parameter.RetentionType;
            coverRange = parameter.CoverRange;
            isLewisAndSeniorCheck = parameter.IsLewisAndSeniorCheck;
            isElementProbabilityCheck = parameter.IsElementProbabilityCheck;
            isOcheck = parameter.IsOcheck;
            isNcheck = parameter.IsNcheck;
            isPcheck = parameter.IsPcheck;
            isScheck = parameter.IsScheck;
            isFcheck = parameter.IsFcheck;
            isClCheck = parameter.IsClCheck;
            isBrCheck = parameter.IsBrCheck;
            isIcheck = parameter.IsIcheck;
            isSiCheck = parameter.IsSiCheck;
            isNitrogenRule = parameter.IsNitrogenRule;
            formulaScoreCutOff = parameter.FormulaScoreCutOff;
            canExcuteMS1AdductSearch = parameter.CanExcuteMS1AdductSearch;
            canExcuteMS2AdductSearch = parameter.CanExcuteMS2AdductSearch;
            formulaMaximumReportNumber = parameter.FormulaMaximumReportNumber;
            isNeutralLossCheck = parameter.IsNeutralLossCheck;
            treeDepth = parameter.TreeDepth;
            structureScoreCutOff = parameter.StructureScoreCutOff;
            structureMaximumReportNumber = parameter.StructureMaximumReportNumber;
            isUserDefinedDB = parameter.IsUserDefinedDB;
            userDefinedDbFilePath = parameter.UserDefinedDbFilePath;
            isAllProcess = parameter.IsAllProcess;
            isUseEiFragmentDB = parameter.IsUseEiFragmentDB;
            tryTopNmolecularFormulaSearch = parameter.TryTopNmolecularFormulaSearch;
            isFormulaFinder = parameter.IsFormulaFinder;
            isStructureFinder = parameter.IsStructureFinder;
            databaseQuery = parameter.DatabaseQuery;
            isPubChemNeverUse = parameter.IsPubChemNeverUse;
            isPubChemOnlyUseForNecessary = parameter.IsPubChemOnlyUseForNecessary;
            isPubChemAllTime = parameter.IsPubChemAllTime;
            isMinesNeverUse = parameter.IsMinesNeverUse;
            isMinesOnlyUseForNecessary = parameter.IsMinesOnlyUseForNecessary;
            isMinesAllTime = parameter.IsMinesAllTime;
            cLabelMass = parameter.CLabelMass;
            hLabelMass = parameter.HLabelMass;
            nLabelMass = parameter.NLabelMass;
            oLabelMass = parameter.OLabelMass;
            pLabelMass = parameter.PLabelMass;
            sLabelMass = parameter.SLabelMass;
            fLabelMass = parameter.FLabelMass;
            clLabelMass = parameter.ClLabelMass;
            brLabelMass = parameter.BrLabelMass;
            iLabelMass = parameter.ILabelMass;
            siLabelMass = parameter.SiLabelMass;
            isTmsMeoxDerivative = parameter.IsTmsMeoxDerivative;
            minimumTmsCount = parameter.MinimumTmsCount;
            minimumMeoxCount = parameter.MinimumMeoxCount;
            isRunSpectralDbSearch = parameter.IsRunSpectralDbSearch;
            isRunInSilicoFragmenterSearch = parameter.IsRunInSilicoFragmenterSearch;
            isPrecursorOrientedSearch = parameter.IsPrecursorOrientedSearch;
            isUseInternalExperimentalSpectralDb = parameter.IsUseInternalExperimentalSpectralDb;
            isUseInSilicoSpectralDbForLipids = parameter.IsUseInSilicoSpectralDbForLipids;
            isUseUserDefinedSpectralDb = parameter.IsUseUserDefinedSpectralDb;
            userDefinedSpectralDbFilePath = parameter.UserDefinedSpectralDbFilePath;
            lipidQueryBean = parameter.LipidQueryBean;
            scoreCutOffForSpectralMatch = parameter.ScoreCutOffForSpectralMatch;
            isUsePredictedRtForStructureElucidation = parameter.IsUsePredictedRtForStructureElucidation;
            rtSmilesDictionaryFilepath = parameter.RtSmilesDictionaryFilepath;
            coeff_RtPrediction = parameter.Coeff_RtPrediction;
            intercept_RtPrediction = parameter.Intercept_RtPrediction;
            rtToleranceForStructureElucidation = parameter.RtToleranceForStructureElucidation;
            isUseRtInchikeyLibrary = parameter.IsUseRtInchikeyLibrary;
            isUseXlogpPrediction = parameter.IsUseXlogpPrediction;
            rtInChIKeyDictionaryFilepath = parameter.RtInChIKeyDictionaryFilepath;
            isUseRtForFilteringCandidates = parameter.IsUseRtForFilteringCandidates;
            isUseExperimentalRtForSpectralSearching = parameter.IsUseExperimentalRtForSpectralSearching;
            rtToleranceForSpectralSearching = parameter.RtToleranceForSpectralSearching;
            rtPredictionSummaryReport = parameter.RtPredictionSummaryReport;
            fseaRelativeAbundanceCutOff = parameter.FseaRelativeAbundanceCutOff;
            fseanonsignificantDef = parameter.FseanonsignificantDef;
            fseaPvalueCutOff = parameter.FseaPvalueCutOff;
            isMmnLocalCytoscape = parameter.IsMmnLocalCytoscape;
            isMmnMsdialOutput = parameter.IsMmnMsdialOutput;
            isMmnFormulaBioreaction = parameter.IsMmnFormulaBioreaction;
            isMmnRetentionRestrictionUsed = parameter.IsMmnRetentionRestrictionUsed;
            isMmnOntologySimilarityUsed = parameter.IsMmnOntologySimilarityUsed;
            mmnMassTolerance = parameter.MmnMassTolerance;
            mmnRelativeCutoff = parameter.MmnRelativeCutoff;
            mmnMassSimilarityCutOff = parameter.MmnMassSimilarityCutOff;
            mmnRtTolerance = parameter.MmnRtTolerance;
            mmnOntologySimilarityCutOff = parameter.MmnOntologySimilarityCutOff;
            mmnOutputFolderPath = parameter.MmnOutputFolderPath;
            mmnRtToleranceForReaction = parameter.MmnRtToleranceForReaction;
            isMmnSelectedFileCentricProcess = parameter.IsMmnSelectedFileCentricProcess;
            formulaPredictionTimeOut = parameter.FormulaPredictionTimeOut;
            structurePredictionTimeOut = parameter.StructurePredictionTimeOut;
            mS1PositiveAdductIonList = parameter.MS1PositiveAdductIonList;
            mS2PositiveAdductIonList = parameter.MS2PositiveAdductIonList;
            mS1NegativeAdductIonList = parameter.MS1NegativeAdductIonList;
            mS2NegativeAdductIonList = parameter.MS2NegativeAdductIonList;
            ccsToleranceForStructureElucidation = parameter.CcsToleranceForStructureElucidation;
            isUsePredictedCcsForStructureElucidation = parameter.IsUsePredictedCcsForStructureElucidation;
            isUseCcsInchikeyAdductLibrary = parameter.IsUseCcsInchikeyAdductLibrary;
            ccsAdductInChIKeyDictionaryFilepath = parameter.CcsAdductInChIKeyDictionaryFilepath;
            isUseExperimentalCcsForSpectralSearching = parameter.IsUseExperimentalCcsForSpectralSearching;
            ccsToleranceForSpectralSearching = parameter.CcsToleranceForSpectralSearching;
            isUseCcsForFilteringCandidates = parameter.IsUseCcsForFilteringCandidates;

            isCreateNewProject = true;
            isUseAutoDefinedFolderName = true;
            userDefinedProjectFolderName = "";
        }

        public ReadOnlyReactivePropertySlim<IAlignmentModel?> CurrentAlignmentModel { get; }

        public FormulaFinderAdductIonSettingModel FormulaFinderAdductIonSetting { get;  }
        public MassToleranceType MassTolType
        {
            get => massTolType;
            set => SetProperty(ref massTolType, value);
        }
        private MassToleranceType massTolType;

        public double Mass1Tolerance
        {
            get => mass1Tolerance;
            set => SetProperty(ref mass1Tolerance, value);
        }
        private double mass1Tolerance;

        public double Mass2Tolerance
        {
            get => mass2Tolerance;
            set => SetProperty(ref mass2Tolerance, value);
        }
        private double mass2Tolerance;

        public double IsotopicAbundanceTolerance
        {
            get => isotopicAbundanceTolerance;
            set => SetProperty(ref isotopicAbundanceTolerance, value);
        }
        private double isotopicAbundanceTolerance;

        public double MassRangeMin
        {
            get => massRangeMin;
            set => SetProperty(ref massRangeMin, value);
        }
        private double massRangeMin;

        public double MassRangeMax
        {
            get => massRangeMax;
            set => SetProperty(ref massRangeMax, value);
        }
        private double massRangeMax;

        public double RelativeAbundanceCutOff
        {
            get => relativeAbundanceCutOff;
            set => SetProperty(ref relativeAbundanceCutOff, value);
        }
        private double relativeAbundanceCutOff;

        public SolventType SolventType
        {
            get => solventType;
            set => SetProperty(ref solventType, value);
        }
        private SolventType solventType;

        public RetentionType RetentionType
        {
            get => retentionType;
            set => SetProperty(ref retentionType, value);
        }
        private RetentionType retentionType;

        public CoverRange CoverRange
        {
            get => coverRange;
            set => SetProperty(ref coverRange, value);
        }
        private CoverRange coverRange;

        public bool IsLewisAndSeniorCheck
        {
            get => isLewisAndSeniorCheck;
            set => SetProperty(ref isLewisAndSeniorCheck, value);
        }
        private bool isLewisAndSeniorCheck;

        public bool IsElementProbabilityCheck
        {
            get => isElementProbabilityCheck;
            set => SetProperty(ref isElementProbabilityCheck, value);
        }
        private bool isElementProbabilityCheck;

        public bool IsOcheck
        {
            get => isOcheck;
            set => SetProperty(ref isOcheck, value);
        }
        private bool isOcheck;

        public bool IsNcheck
        {
            get => isNcheck;
            set => SetProperty(ref isNcheck, value);
        }
        private bool isNcheck;

        public bool IsPcheck
        {
            get => isPcheck;
            set => SetProperty(ref isPcheck, value);
        }
        private bool isPcheck;

        public bool IsScheck
        {
            get => isScheck;
            set => SetProperty(ref isScheck, value);
        }
        private bool isScheck;

        public bool IsFcheck
        {
            get => isFcheck;
            set => SetProperty(ref isFcheck, value);
        }
        private bool isFcheck;

        public bool IsClCheck
        {
            get => isClCheck;
            set => SetProperty(ref isClCheck, value);
        }
        private bool isClCheck;

        public bool IsBrCheck
        {
            get => isBrCheck;
            set => SetProperty(ref isBrCheck, value);
        }
        private bool isBrCheck;

        public bool IsIcheck
        {
            get => isIcheck;
            set => SetProperty(ref isIcheck, value);
        }
        private bool isIcheck;

        public bool IsSiCheck
        {
            get => isSiCheck;
            set => SetProperty(ref isSiCheck, value);
        }
        private bool isSiCheck;

        public bool IsNitrogenRule
        {
            get => isNitrogenRule;
            set => SetProperty(ref isNitrogenRule, value);
        }
        private bool isNitrogenRule;

        public double FormulaScoreCutOff
        {
            get => formulaScoreCutOff;
            set => SetProperty(ref formulaScoreCutOff, value);
        }
        private double formulaScoreCutOff;

        public bool CanExcuteMS1AdductSearch
        {
            get => canExcuteMS1AdductSearch;
            set => SetProperty(ref canExcuteMS1AdductSearch, value);
        }
        private bool canExcuteMS1AdductSearch;

        public bool CanExcuteMS2AdductSearch
        {
            get => canExcuteMS2AdductSearch;
            set => SetProperty(ref canExcuteMS2AdductSearch, value);
        }
        private bool canExcuteMS2AdductSearch;

        public int FormulaMaximumReportNumber
        {
            get => formulaMaximumReportNumber;
            set => SetProperty(ref formulaMaximumReportNumber, value);
        }
        private int formulaMaximumReportNumber;

        public bool IsNeutralLossCheck
        {
            get => isNeutralLossCheck;
            set => SetProperty(ref isNeutralLossCheck, value);
        }
        private bool isNeutralLossCheck;

        public int TreeDepth
        {
            get => treeDepth;
            set => SetProperty(ref treeDepth, value);
        }
        private int treeDepth;

        public double StructureScoreCutOff
        {
            get => structureScoreCutOff;
            set => SetProperty(ref structureScoreCutOff, value);
        }
        private double structureScoreCutOff;

        public int StructureMaximumReportNumber
        {
            get => structureMaximumReportNumber;
            set => SetProperty(ref structureMaximumReportNumber, value);
        }
        private int structureMaximumReportNumber;

        public bool IsUserDefinedDB
        {
            get => isUserDefinedDB;
            set => SetProperty(ref isUserDefinedDB, value);
        }
        private bool isUserDefinedDB;

        public string UserDefinedDbFilePath
        {
            get => userDefinedDbFilePath;
            set => SetProperty(ref userDefinedDbFilePath, value);
        }
        private string userDefinedDbFilePath;

        public bool IsAllProcess
        {
            get => isAllProcess;
            set => SetProperty(ref isAllProcess, value);
        }
        private bool isAllProcess;

        public bool IsUseEiFragmentDB
        {
            get => isUseEiFragmentDB;
            set => SetProperty(ref isUseEiFragmentDB, value);
        }
        private bool isUseEiFragmentDB;

        public int TryTopNmolecularFormulaSearch
        {
            get => tryTopNmolecularFormulaSearch;
            set => SetProperty(ref tryTopNmolecularFormulaSearch, value);
        }
        private int tryTopNmolecularFormulaSearch;

        public bool IsFormulaFinder
        {
            get => isFormulaFinder;
            set => SetProperty(ref isFormulaFinder, value);
        }
        private bool isFormulaFinder;

        public bool IsStructureFinder
        {
            get => isStructureFinder;
            set => SetProperty(ref isStructureFinder, value);
        }
        private bool isStructureFinder;

        public DatabaseQuery DatabaseQuery
        {
            get => databaseQuery;
            set => SetProperty(ref databaseQuery, value);
        }
        private DatabaseQuery databaseQuery;

        public bool IsPubChemNeverUse
        {
            get => isPubChemNeverUse;
            set => SetProperty(ref isPubChemNeverUse, value);
        }
        private bool isPubChemNeverUse;

        public bool IsPubChemOnlyUseForNecessary
        {
            get => isPubChemOnlyUseForNecessary;
            set => SetProperty(ref isPubChemOnlyUseForNecessary, value);
        }
        private bool isPubChemOnlyUseForNecessary;

        public bool IsPubChemAllTime
        {
            get => isPubChemAllTime;
            set => SetProperty(ref isPubChemAllTime, value);
        }
        private bool isPubChemAllTime;

        public bool IsMinesNeverUse
        {
            get => isMinesNeverUse;
            set => SetProperty(ref isMinesNeverUse, value);
        }
        private bool isMinesNeverUse;

        public bool IsMinesOnlyUseForNecessary
        {
            get => isMinesOnlyUseForNecessary;
            set => SetProperty(ref isMinesOnlyUseForNecessary, value);
        }
        private bool isMinesOnlyUseForNecessary;

        public bool IsMinesAllTime
        {
            get => isMinesAllTime;
            set => SetProperty(ref isMinesAllTime, value);
        }
        private bool isMinesAllTime;

        public double CLabelMass
        {
            get => cLabelMass;
            set => SetProperty(ref cLabelMass, value);
        }
        private double cLabelMass;

        public double HLabelMass
        {
            get => hLabelMass;
            set => SetProperty(ref hLabelMass, value);
        }
        private double hLabelMass;

        public double NLabelMass
        {
            get => nLabelMass;
            set => SetProperty(ref nLabelMass, value);
        }
        private double nLabelMass;

        public double OLabelMass
        {
            get => oLabelMass;
            set => SetProperty(ref oLabelMass, value);
        }
        private double oLabelMass;

        public double PLabelMass
        {
            get => pLabelMass;
            set => SetProperty(ref pLabelMass, value);
        }
        private double pLabelMass;

        public double SLabelMass
        {
            get => sLabelMass;
            set => SetProperty(ref sLabelMass, value);
        }
        private double sLabelMass;

        public double FLabelMass
        {
            get => fLabelMass;
            set => SetProperty(ref fLabelMass, value);
        }
        private double fLabelMass;

        public double ClLabelMass
        {
            get => clLabelMass;
            set => SetProperty(ref clLabelMass, value);
        }
        private double clLabelMass;

        public double BrLabelMass
        {
            get => brLabelMass;
            set => SetProperty(ref brLabelMass, value);
        }
        private double brLabelMass;

        public double ILabelMass
        {
            get => iLabelMass;
            set => SetProperty(ref iLabelMass, value);
        }
        private double iLabelMass;

        public double SiLabelMass
        {
            get => siLabelMass;
            set => SetProperty(ref siLabelMass, value);
        }
        private double siLabelMass;

        public bool IsTmsMeoxDerivative
        {
            get => isTmsMeoxDerivative;
            set => SetProperty(ref isTmsMeoxDerivative, value);
        }
        private bool isTmsMeoxDerivative;

        public int MinimumTmsCount
        {
            get => minimumTmsCount;
            set => SetProperty(ref minimumTmsCount, value);
        }
        private int minimumTmsCount;

        public int MinimumMeoxCount
        {
            get => minimumMeoxCount;
            set => SetProperty(ref minimumMeoxCount, value);
        }
        private int minimumMeoxCount;

        public bool IsRunSpectralDbSearch
        {
            get => isRunSpectralDbSearch;
            set => SetProperty(ref isRunSpectralDbSearch, value);
        }
        private bool isRunSpectralDbSearch;

        public bool IsRunInSilicoFragmenterSearch
        {
            get => isRunInSilicoFragmenterSearch;
            set => SetProperty(ref isRunInSilicoFragmenterSearch, value);
        }
        private bool isRunInSilicoFragmenterSearch;

        public bool IsPrecursorOrientedSearch
        {
            get => isPrecursorOrientedSearch;
            set => SetProperty(ref isPrecursorOrientedSearch, value);
        }
        private bool isPrecursorOrientedSearch;

        public bool IsUseInternalExperimentalSpectralDb
        {
            get => isUseInternalExperimentalSpectralDb;
            set => SetProperty(ref isUseInternalExperimentalSpectralDb, value);
        }
        private bool isUseInternalExperimentalSpectralDb;

        public bool IsUseInSilicoSpectralDbForLipids
        {
            get => isUseInSilicoSpectralDbForLipids;
            set => SetProperty(ref isUseInSilicoSpectralDbForLipids, value);
        }
        private bool isUseInSilicoSpectralDbForLipids;

        public bool IsUseUserDefinedSpectralDb
        {
            get => isUseUserDefinedSpectralDb;
            set => SetProperty(ref isUseUserDefinedSpectralDb, value);
        }
        private bool isUseUserDefinedSpectralDb;

        public string UserDefinedSpectralDbFilePath
        {
            get => userDefinedSpectralDbFilePath;
            set => SetProperty(ref userDefinedSpectralDbFilePath, value);
        }
        private string userDefinedSpectralDbFilePath;

        public LipidQueryBean LipidQueryBean
        {
            get => lipidQueryBean;
            set => SetProperty(ref lipidQueryBean, value);
        }
        private LipidQueryBean lipidQueryBean;

        public double ScoreCutOffForSpectralMatch
        {
            get => scoreCutOffForSpectralMatch;
            set => SetProperty(ref scoreCutOffForSpectralMatch, value);
        }
        private double scoreCutOffForSpectralMatch;

        public bool IsUsePredictedRtForStructureElucidation
        {
            get => isUsePredictedRtForStructureElucidation;
            set => SetProperty(ref isUsePredictedRtForStructureElucidation, value);
        }
        private bool isUsePredictedRtForStructureElucidation;

        public string RtSmilesDictionaryFilepath
        {
            get => rtSmilesDictionaryFilepath;
            set => SetProperty(ref rtSmilesDictionaryFilepath, value);
        }
        private string rtSmilesDictionaryFilepath;

        public double Coeff_RtPrediction
        {
            get => coeff_RtPrediction;
            set => SetProperty(ref coeff_RtPrediction, value);
        }
        private double coeff_RtPrediction;

        public double Intercept_RtPrediction
        {
            get => intercept_RtPrediction;
            set => SetProperty(ref intercept_RtPrediction, value);
        }
        private double intercept_RtPrediction;

        public double RtToleranceForStructureElucidation
        {
            get => rtToleranceForStructureElucidation;
            set => SetProperty(ref rtToleranceForStructureElucidation, value);
        }
        private double rtToleranceForStructureElucidation;

        public bool IsUseRtInchikeyLibrary
        {
            get => isUseRtInchikeyLibrary;
            set => SetProperty(ref isUseRtInchikeyLibrary, value);
        }
        private bool isUseRtInchikeyLibrary;

        public bool IsUseXlogpPrediction
        {
            get => isUseXlogpPrediction;
            set => SetProperty(ref isUseXlogpPrediction, value);
        }
        private bool isUseXlogpPrediction;

        public string RtInChIKeyDictionaryFilepath
        {
            get => rtInChIKeyDictionaryFilepath;
            set => SetProperty(ref rtInChIKeyDictionaryFilepath, value);
        }
        private string rtInChIKeyDictionaryFilepath;

        public bool IsUseRtForFilteringCandidates
        {
            get => isUseRtForFilteringCandidates;
            set => SetProperty(ref isUseRtForFilteringCandidates, value);
        }
        private bool isUseRtForFilteringCandidates;

        public bool IsUseExperimentalRtForSpectralSearching
        {
            get => isUseExperimentalRtForSpectralSearching;
            set => SetProperty(ref isUseExperimentalRtForSpectralSearching, value);
        }
        private bool isUseExperimentalRtForSpectralSearching;

        public double RtToleranceForSpectralSearching
        {
            get => rtToleranceForSpectralSearching;
            set => SetProperty(ref rtToleranceForSpectralSearching, value);
        }
        private double rtToleranceForSpectralSearching;

        public string RtPredictionSummaryReport
        {
            get => rtPredictionSummaryReport;
            set => SetProperty(ref rtPredictionSummaryReport, value);
        }
        private string rtPredictionSummaryReport;

        public double FseaRelativeAbundanceCutOff
        {
            get => fseaRelativeAbundanceCutOff;
            set => SetProperty(ref fseaRelativeAbundanceCutOff, value);
        }
        private double fseaRelativeAbundanceCutOff;

        public FseaNonsignificantDef FseanonsignificantDef
        {
            get => fseanonsignificantDef;
            set => SetProperty(ref fseanonsignificantDef, value);
        }
        private FseaNonsignificantDef fseanonsignificantDef;

        public double FseaPvalueCutOff
        {
            get => fseaPvalueCutOff;
            set => SetProperty(ref fseaPvalueCutOff, value);
        }
        private double fseaPvalueCutOff;

        public bool IsMmnLocalCytoscape
        {
            get => isMmnLocalCytoscape;
            set => SetProperty(ref isMmnLocalCytoscape, value);
        }
        private bool isMmnLocalCytoscape;

        public bool IsMmnMsdialOutput
        {
            get => isMmnMsdialOutput;
            set => SetProperty(ref isMmnMsdialOutput, value);
        }
        private bool isMmnMsdialOutput;

        public bool IsMmnFormulaBioreaction
        {
            get => isMmnFormulaBioreaction;
            set => SetProperty(ref isMmnFormulaBioreaction, value);
        }
        private bool isMmnFormulaBioreaction;

        public bool IsMmnRetentionRestrictionUsed
        {
            get => isMmnRetentionRestrictionUsed;
            set => SetProperty(ref isMmnRetentionRestrictionUsed, value);
        }
        private bool isMmnRetentionRestrictionUsed;

        public bool IsMmnOntologySimilarityUsed
        {
            get => isMmnOntologySimilarityUsed;
            set => SetProperty(ref isMmnOntologySimilarityUsed, value);
        }
        private bool isMmnOntologySimilarityUsed;

        public double MmnMassTolerance
        {
            get => mmnMassTolerance;
            set => SetProperty(ref mmnMassTolerance, value);
        }
        private double mmnMassTolerance;

        public double MmnRelativeCutoff
        {
            get => mmnRelativeCutoff;
            set => SetProperty(ref mmnRelativeCutoff, value);
        }
        private double mmnRelativeCutoff;

        public double MmnMassSimilarityCutOff
        {
            get => mmnMassSimilarityCutOff;
            set => SetProperty(ref mmnMassSimilarityCutOff, value);
        }
        private double mmnMassSimilarityCutOff;

        public double MmnRtTolerance
        {
            get => mmnRtTolerance;
            set => SetProperty(ref mmnRtTolerance, value);
        }
        private double mmnRtTolerance;

        public double MmnOntologySimilarityCutOff
        {
            get => mmnOntologySimilarityCutOff;
            set => SetProperty(ref mmnOntologySimilarityCutOff, value);
        }
        private double mmnOntologySimilarityCutOff;

        public string MmnOutputFolderPath
        {
            get => mmnOutputFolderPath;
            set => SetProperty(ref mmnOutputFolderPath, value);
        }
        private string mmnOutputFolderPath;

        public double MmnRtToleranceForReaction
        {
            get => mmnRtToleranceForReaction;
            set => SetProperty(ref mmnRtToleranceForReaction, value);
        }
        private double mmnRtToleranceForReaction;

        public bool IsMmnSelectedFileCentricProcess
        {
            get => isMmnSelectedFileCentricProcess;
            set => SetProperty(ref isMmnSelectedFileCentricProcess, value);
        }
        private bool isMmnSelectedFileCentricProcess;

        public double FormulaPredictionTimeOut
        {
            get => formulaPredictionTimeOut;
            set => SetProperty(ref formulaPredictionTimeOut, value);
        }
        private double formulaPredictionTimeOut;

        public double StructurePredictionTimeOut
        {
            get => structurePredictionTimeOut;
            set => SetProperty(ref structurePredictionTimeOut, value);
        }
        private double structurePredictionTimeOut;

        public List<AdductIon> MS1PositiveAdductIonList
        {
            get => mS1PositiveAdductIonList;
            set => SetProperty(ref mS1PositiveAdductIonList, value);
        }
        private List<AdductIon> mS1PositiveAdductIonList;

        public List<AdductIon> MS2PositiveAdductIonList
        {
            get => mS2PositiveAdductIonList;
            set => SetProperty(ref mS2PositiveAdductIonList, value);
        }
        private List<AdductIon> mS2PositiveAdductIonList;

        public List<AdductIon> MS1NegativeAdductIonList
        {
            get => mS1NegativeAdductIonList;
            set => SetProperty(ref mS1NegativeAdductIonList, value);
        }
        private List<AdductIon> mS1NegativeAdductIonList;

        public List<AdductIon> MS2NegativeAdductIonList
        {
            get => mS2NegativeAdductIonList;
            set => SetProperty(ref mS2NegativeAdductIonList, value);
        }
        private List<AdductIon> mS2NegativeAdductIonList;

        public double CcsToleranceForStructureElucidation
        {
            get => ccsToleranceForStructureElucidation;
            set => SetProperty(ref ccsToleranceForStructureElucidation, value);
        }
        private double ccsToleranceForStructureElucidation;

        public bool IsUsePredictedCcsForStructureElucidation
        {
            get => isUsePredictedCcsForStructureElucidation;
            set => SetProperty(ref isUsePredictedCcsForStructureElucidation, value);
        }
        private bool isUsePredictedCcsForStructureElucidation;

        public bool IsUseCcsInchikeyAdductLibrary
        {
            get => isUseCcsInchikeyAdductLibrary;
            set => SetProperty(ref isUseCcsInchikeyAdductLibrary, value);
        }
        private bool isUseCcsInchikeyAdductLibrary;

        public string CcsAdductInChIKeyDictionaryFilepath
        {
            get => ccsAdductInChIKeyDictionaryFilepath;
            set => SetProperty(ref ccsAdductInChIKeyDictionaryFilepath, value);
        }
        private string ccsAdductInChIKeyDictionaryFilepath;

        public bool IsUseExperimentalCcsForSpectralSearching
        {
            get => isUseExperimentalCcsForSpectralSearching;
            set => SetProperty(ref isUseExperimentalCcsForSpectralSearching, value);
        }
        private bool isUseExperimentalCcsForSpectralSearching;

        public double CcsToleranceForSpectralSearching
        {
            get => ccsToleranceForSpectralSearching;
            set => SetProperty(ref ccsToleranceForSpectralSearching, value);
        }
        private double ccsToleranceForSpectralSearching;

        public bool IsUseCcsForFilteringCandidates
        {
            get => isUseCcsForFilteringCandidates;
            set => SetProperty(ref isUseCcsForFilteringCandidates, value);
        }
        private bool isUseCcsForFilteringCandidates;

        public bool IsCreateNewProject
        {
            get => isCreateNewProject;
            set => SetProperty(ref isCreateNewProject, value);
        }
        private bool isCreateNewProject;

        public bool IsUseAutoDefinedFolderName
        {
            get => isUseAutoDefinedFolderName;
            set => SetProperty(ref isUseAutoDefinedFolderName, value);
        }
        private bool isUseAutoDefinedFolderName;

        public string UserDefinedProjectFolderName
        {
            get => userDefinedProjectFolderName;
            set => SetProperty(ref userDefinedProjectFolderName, value);
        }
        private string userDefinedProjectFolderName;

        public string ExistProjectPath
        {
            get => existProjectPath;
            set => SetProperty(ref existProjectPath, value);
        }
        private string existProjectPath;

        private readonly List<ProductIon> productIonDB = FragmentDbParser.GetProductIonDB(
            @"Resources\msfinderLibrary\ProductIonLib_vs1.pid", out string _);
        private readonly List<NeutralLoss> neutralLossDB = FragmentDbParser.GetNeutralLossDB(
            @"Resources\msfinderLibrary\NeutralLossDB_vs2.ndb", out string _);
        private readonly List<ExistFormulaQuery> existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(
            @"Resources\msfinderLibrary\MsfinderFormulaDB-VS13.efd", out string _);
        private readonly List<ExistStructureQuery> existStructureDB = FileStorageUtility.GetExistStructureDB();

        private readonly List<ExistStructureQuery> mineStructureDB = FileStorageUtility.GetMinesStructureDB();
        private readonly List<FragmentOntology> fragmentOntologyDB = FileStorageUtility.GetUniqueFragmentDB();
        private List<MoleculeMsReference> mspDB = new List<MoleculeMsReference>();
        private List<ExistStructureQuery> userDefinedStructureDB;
        private readonly List<FragmentLibrary>  eiFragmentDB = FileStorageUtility.GetEiFragmentDB();


        public InternalMsFinder? Process() {
            if (CurrentAlignmentModel.Value is null) {
                return null;
            }
            SetUserDefinedStructureDB();

            string fullpath;
            var dt = DateTime.Now;
            if (IsCreateNewProject) {
                var directory = Path.GetDirectoryName(CurrentAlignmentModel.Value.AlignmentFile.FilePath); // project folder
                string foldername;
                if (IsUseAutoDefinedFolderName) {
                    foldername = $"{CurrentAlignmentModel.Value.AlignmentFile.FileName}_{dt:yyyyMMddHHmmss}";
                }else{
                    foldername = UserDefinedProjectFolderName;
                }
                fullpath = Path.Combine(directory, foldername); // export folder
                if (!Directory.Exists(fullpath)) {
                    Directory.CreateDirectory(fullpath);
                }
                exporter.Export(CurrentAlignmentModel.Value.AlignmentFile, fullpath, null);
            }else{
                fullpath = ExistProjectPath;
            }
            
            Commit();
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
                MsFinderIniParser.Write(parameter, paramfile);

                foreach (var msfinderQueryFile in msfinderQueryFiles) {
                    var rawData = RawDataParcer.RawDataFileReader(msfinderQueryFile.RawDataFilePath, parameter);
                    var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, parameter);
                    FormulaResultParcer.FormulaResultsWriter(msfinderQueryFile.FormulaFilePath, formulaResults);
                }
            }
            if (parameter.IsStructureFinder) {                
                var finder = new StructureFinderBatchProcess();
                finder.Process(msfinderQueryFiles, parameter, existStructureDB, userDefinedStructureDB, mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);
            }

            if (CurrentAlignmentModel.Value.AlignmentSpotSource.Spots is null) {
                return null;
            }
            var metaboliteList = new InternalMsFinderMetaboliteList(msfinderQueryFiles, parameter, userDefinedStructureDB);
            var internalMsFinderModel = new InternalMsFinder(metaboliteList);
            return internalMsFinderModel;
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
                userDefinedStructureDB = null;
        }

    }
}
