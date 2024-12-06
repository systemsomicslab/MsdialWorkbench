using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.Common.Enum;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Setting {
    internal sealed class InternalMsfinderSettingViewModel : SettingDialogViewModel {
        public InternalMsfinderSettingViewModel(MsfinderParameterSetting model, IMessageBroker broker) {
            MassTolType = model.ToReactivePropertySlimAsSynchronized(m => m.MassTolType).AddTo(Disposables);

            Mass1Tolerance = model.ToReactivePropertyAsSynchronized(
                m => m.Mass1Tolerance,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => Mass1Tolerance).AddTo(Disposables);

            Mass2Tolerance = model.ToReactivePropertyAsSynchronized(
                m => m.Mass2Tolerance,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => Mass2Tolerance).AddTo(Disposables);

            IsotopicAbundanceTolerance = model.ToReactivePropertyAsSynchronized(
                m => m.IsotopicAbundanceTolerance,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => IsotopicAbundanceTolerance).AddTo(Disposables);

            MassRangeMin = model.ToReactivePropertyAsSynchronized(
                m => m.MassRangeMin,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => MassRangeMin).AddTo(Disposables);

            MassRangeMax = model.ToReactivePropertyAsSynchronized(
                m => m.MassRangeMax,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => MassRangeMax).AddTo(Disposables);

            RelativeAbundanceCutOff = model.ToReactivePropertyAsSynchronized(
                m => m.RelativeAbundanceCutOff,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => RelativeAbundanceCutOff).AddTo(Disposables);

            SolventType = model.ToReactivePropertySlimAsSynchronized(m => m.SolventType).AddTo(Disposables);

            RetentionType = model.ToReactivePropertySlimAsSynchronized(m => m.RetentionType).AddTo(Disposables);

            CoverRange = model.ToReactivePropertySlimAsSynchronized(m => m.CoverRange).AddTo(Disposables);

            IsLewisAndSeniorCheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsLewisAndSeniorCheck).AddTo(Disposables);

            IsElementProbabilityCheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsElementProbabilityCheck).AddTo(Disposables);

            IsOcheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsOcheck).AddTo(Disposables);

            IsNcheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsNcheck).AddTo(Disposables);

            IsPcheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsPcheck).AddTo(Disposables);

            IsScheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsScheck).AddTo(Disposables);

            IsFcheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsFcheck).AddTo(Disposables);

            IsClCheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsClCheck).AddTo(Disposables);

            IsBrCheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsBrCheck).AddTo(Disposables);

            IsIcheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsIcheck).AddTo(Disposables);

            IsSiCheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsSiCheck).AddTo(Disposables);

            IsNitrogenRule = model.ToReactivePropertySlimAsSynchronized(m => m.IsNitrogenRule).AddTo(Disposables);

            FormulaScoreCutOff = model.ToReactivePropertyAsSynchronized(
                m => m.FormulaScoreCutOff,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => FormulaScoreCutOff).AddTo(Disposables);

            CanExcuteMS1AdductSearch = model.ToReactivePropertySlimAsSynchronized(m => m.CanExcuteMS1AdductSearch).AddTo(Disposables);

            CanExcuteMS2AdductSearch = model.ToReactivePropertySlimAsSynchronized(m => m.CanExcuteMS2AdductSearch).AddTo(Disposables);

            FormulaMaximumReportNumber = model.ToReactivePropertyAsSynchronized(
                m => m.FormulaMaximumReportNumber,
                m => m.ToString(),
                vm => int.Parse(vm)
            ).SetValidateAttribute(() => FormulaMaximumReportNumber).AddTo(Disposables);

            IsNeutralLossCheck = model.ToReactivePropertySlimAsSynchronized(m => m.IsNeutralLossCheck).AddTo(Disposables);

            TreeDepth = model.ToReactivePropertyAsSynchronized(
                m => m.TreeDepth,
                m => m.ToString(),
                vm => int.Parse(vm)
            ).SetValidateAttribute(() => TreeDepth).AddTo(Disposables);

            StructureScoreCutOff = model.ToReactivePropertyAsSynchronized(
                m => m.StructureScoreCutOff,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => StructureScoreCutOff).AddTo(Disposables);

            StructureMaximumReportNumber = model.ToReactivePropertyAsSynchronized(
                m => m.StructureMaximumReportNumber,
                m => m.ToString(),
                vm => int.Parse(vm)
            ).SetValidateAttribute(() => StructureMaximumReportNumber).AddTo(Disposables);

            IsUserDefinedDB = model.ToReactivePropertySlimAsSynchronized(m => m.IsUserDefinedDB).AddTo(Disposables);

            UserDefinedDbFilePath = model.ToReactivePropertyAsSynchronized(m => m.UserDefinedDbFilePath).AddTo(Disposables);

            IsAllProcess = model.ToReactivePropertySlimAsSynchronized(m => m.IsAllProcess).AddTo(Disposables);

            IsUseEiFragmentDB = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseEiFragmentDB).AddTo(Disposables);

            Chebi = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Chebi).AddTo(Disposables);

            Hmdb = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Hmdb).AddTo(Disposables);

            Pubchem = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Pubchem).AddTo(Disposables);

            Smpdb = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Smpdb).AddTo(Disposables);

            Unpd = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Unpd).AddTo(Disposables);

            Ymdb = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Ymdb).AddTo(Disposables);

            Plantcyc = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Plantcyc).AddTo(Disposables);

            Knapsack = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Knapsack).AddTo(Disposables);

            Bmdb = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Bmdb).AddTo(Disposables);

            Drugbank = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Drugbank).AddTo(Disposables);

            Ecmdb = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Ecmdb).AddTo(Disposables);

            Foodb = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Foodb).AddTo(Disposables);

            T3db = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.T3db).AddTo(Disposables);

            Stoff = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Stoff).AddTo(Disposables);

            Nanpdb = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Nanpdb).AddTo(Disposables);

            Lipidmaps = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Lipidmaps).AddTo(Disposables);

            Feces = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Feces).AddTo(Disposables);

            Saliva = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Saliva).AddTo(Disposables);

            Serum = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Serum).AddTo(Disposables);

            Urine = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Urine).AddTo(Disposables);

            Csf = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Csf).AddTo(Disposables);

            Blexp = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Blexp).AddTo(Disposables);

            Npa = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Npa).AddTo(Disposables);

            Coconut = model.ToReactivePropertySlimAsSynchronized(m => m.DatabaseQuery.Coconut).AddTo(Disposables);

            IsPubChemNeverUse = model.ToReactivePropertySlimAsSynchronized(m => m.IsPubChemNeverUse).AddTo(Disposables);

            IsPubChemOnlyUseForNecessary = model.ToReactivePropertySlimAsSynchronized(m => m.IsPubChemOnlyUseForNecessary).AddTo(Disposables);

            IsPubChemAllTime = model.ToReactivePropertySlimAsSynchronized(m => m.IsPubChemAllTime).AddTo(Disposables);

            IsMinesNeverUse = model.ToReactivePropertySlimAsSynchronized(m => m.IsMinesNeverUse).AddTo(Disposables);

            IsMinesOnlyUseForNecessary = model.ToReactivePropertySlimAsSynchronized(m => m.IsMinesOnlyUseForNecessary).AddTo(Disposables);

            IsMinesAllTime = model.ToReactivePropertySlimAsSynchronized(m => m.IsMinesAllTime).AddTo(Disposables);

            CLabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.CLabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => CLabelMass).AddTo(Disposables);

            HLabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.HLabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => HLabelMass).AddTo(Disposables);

            NLabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.NLabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => NLabelMass).AddTo(Disposables);

            OLabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.OLabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => OLabelMass).AddTo(Disposables);

            PLabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.PLabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => PLabelMass).AddTo(Disposables);

            SLabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.SLabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => SLabelMass).AddTo(Disposables);

            FLabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.FLabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => FLabelMass).AddTo(Disposables);

            ClLabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.ClLabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => ClLabelMass).AddTo(Disposables);

            BrLabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.BrLabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => BrLabelMass).AddTo(Disposables);

            ILabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.ILabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => ILabelMass).AddTo(Disposables);

            SiLabelMass = model.ToReactivePropertyAsSynchronized(
                m => m.SiLabelMass,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => SiLabelMass).AddTo(Disposables);

            IsTmsMeoxDerivative = model.ToReactivePropertySlimAsSynchronized(m => m.IsTmsMeoxDerivative).AddTo(Disposables);

            MinimumTmsCount = model.ToReactivePropertyAsSynchronized(
                m => m.MinimumTmsCount,
                m => m.ToString(),
                vm => int.Parse(vm)
            ).SetValidateAttribute(() => MinimumTmsCount).AddTo(Disposables);

            MinimumMeoxCount = model.ToReactivePropertyAsSynchronized(
                m => m.MinimumMeoxCount,
                m => m.ToString(),
                vm => int.Parse(vm)
            ).SetValidateAttribute(() => MinimumMeoxCount).AddTo(Disposables);

            IsRunSpectralDbSearch = model.ToReactivePropertySlimAsSynchronized(m => m.IsRunSpectralDbSearch).AddTo(Disposables);

            IsRunInSilicoFragmenterSearch = model.ToReactivePropertySlimAsSynchronized(m => m.IsRunInSilicoFragmenterSearch).AddTo(Disposables);

            IsPrecursorOrientedSearch = model.ToReactivePropertySlimAsSynchronized(m => m.IsPrecursorOrientedSearch).AddTo(Disposables);

            IsUseInternalExperimentalSpectralDb = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseInternalExperimentalSpectralDb).AddTo(Disposables);

            IsUseInSilicoSpectralDbForLipids = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseInSilicoSpectralDbForLipids).AddTo(Disposables);

            IsUseUserDefinedSpectralDb = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseUserDefinedSpectralDb).AddTo(Disposables);

            UserDefinedSpectralDbFilePath = model.ToReactivePropertyAsSynchronized(m => m.UserDefinedSpectralDbFilePath).AddTo(Disposables);

            ScoreCutOffForSpectralMatch = model.ToReactivePropertyAsSynchronized(
                m => m.ScoreCutOffForSpectralMatch,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => ScoreCutOffForSpectralMatch).AddTo(Disposables);

            IsUsePredictedRtForStructureElucidation = model.ToReactivePropertySlimAsSynchronized(m => m.IsUsePredictedRtForStructureElucidation).AddTo(Disposables);

            RtSmilesDictionaryFilepath = model.ToReactivePropertyAsSynchronized(m => m.RtSmilesDictionaryFilepath).AddTo(Disposables);

            Coeff_RtPrediction = model.ToReactivePropertyAsSynchronized(
                m => m.Coeff_RtPrediction,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => Coeff_RtPrediction).AddTo(Disposables);

            Intercept_RtPrediction = model.ToReactivePropertyAsSynchronized(
                m => m.Intercept_RtPrediction,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => Intercept_RtPrediction).AddTo(Disposables);

            RtToleranceForStructureElucidation = model.ToReactivePropertyAsSynchronized(
                m => m.RtToleranceForStructureElucidation,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => RtToleranceForStructureElucidation).AddTo(Disposables);

            IsUseRtInchikeyLibrary = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseRtInchikeyLibrary).AddTo(Disposables);

            IsUseXlogpPrediction = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseXlogpPrediction).AddTo(Disposables);

            RtInChIKeyDictionaryFilepath = model.ToReactivePropertyAsSynchronized(m => m.RtInChIKeyDictionaryFilepath).AddTo(Disposables);

            IsUseRtForFilteringCandidates = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseRtForFilteringCandidates).AddTo(Disposables);

            IsUseExperimentalRtForSpectralSearching = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseExperimentalRtForSpectralSearching).AddTo(Disposables);

            RtToleranceForSpectralSearching = model.ToReactivePropertyAsSynchronized(
                m => m.RtToleranceForSpectralSearching,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => RtToleranceForSpectralSearching).AddTo(Disposables);

            RtPredictionSummaryReport = model.ToReactivePropertyAsSynchronized(m => m.RtPredictionSummaryReport).AddTo(Disposables);

            FseaRelativeAbundanceCutOff = model.ToReactivePropertyAsSynchronized(
                m => m.FseaRelativeAbundanceCutOff,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => FseaRelativeAbundanceCutOff).AddTo(Disposables);

            FseaPvalueCutOff = model.ToReactivePropertyAsSynchronized(
                m => m.FseaPvalueCutOff,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => FseaPvalueCutOff).AddTo(Disposables);

            IsMmnLocalCytoscape = model.ToReactivePropertySlimAsSynchronized(m => m.IsMmnLocalCytoscape).AddTo(Disposables);

            IsMmnMsdialOutput = model.ToReactivePropertySlimAsSynchronized(m => m.IsMmnMsdialOutput).AddTo(Disposables);

            IsMmnFormulaBioreaction = model.ToReactivePropertySlimAsSynchronized(m => m.IsMmnFormulaBioreaction).AddTo(Disposables);

            IsMmnRetentionRestrictionUsed = model.ToReactivePropertySlimAsSynchronized(m => m.IsMmnRetentionRestrictionUsed).AddTo(Disposables);

            IsMmnOntologySimilarityUsed = model.ToReactivePropertySlimAsSynchronized(m => m.IsMmnOntologySimilarityUsed).AddTo(Disposables);

            MmnMassTolerance = model.ToReactivePropertyAsSynchronized(
                m => m.MmnMassTolerance,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => MmnMassTolerance).AddTo(Disposables);

            MmnRelativeCutoff = model.ToReactivePropertyAsSynchronized(
                m => m.MmnRelativeCutoff,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => MmnRelativeCutoff).AddTo(Disposables);

            MmnMassSimilarityCutOff = model.ToReactivePropertyAsSynchronized(
                m => m.MmnMassSimilarityCutOff,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => MmnMassSimilarityCutOff).AddTo(Disposables);

            MmnRtTolerance = model.ToReactivePropertyAsSynchronized(
                m => m.MmnRtTolerance,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => MmnRtTolerance).AddTo(Disposables);

            MmnOntologySimilarityCutOff = model.ToReactivePropertyAsSynchronized(
                m => m.MmnOntologySimilarityCutOff,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => MmnOntologySimilarityCutOff).AddTo(Disposables);

            MmnOutputFolderPath = model.ToReactivePropertyAsSynchronized(m => m.MmnOutputFolderPath).AddTo(Disposables);

            MmnRtToleranceForReaction = model.ToReactivePropertyAsSynchronized(
                m => m.MmnRtToleranceForReaction,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => MmnRtToleranceForReaction).AddTo(Disposables);

            IsMmnSelectedFileCentricProcess = model.ToReactivePropertySlimAsSynchronized(m => m.IsMmnSelectedFileCentricProcess).AddTo(Disposables);

            FormulaPredictionTimeOut = model.ToReactivePropertyAsSynchronized(
                m => m.FormulaPredictionTimeOut,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => FormulaPredictionTimeOut).AddTo(Disposables);

            StructurePredictionTimeOut = model.ToReactivePropertyAsSynchronized(
                m => m.StructurePredictionTimeOut,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => StructurePredictionTimeOut).AddTo(Disposables);

            CcsToleranceForStructureElucidation = model.ToReactivePropertyAsSynchronized(
                m => m.CcsToleranceForStructureElucidation,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => CcsToleranceForStructureElucidation).AddTo(Disposables);

            IsUsePredictedCcsForStructureElucidation = model.ToReactivePropertySlimAsSynchronized(m => m.IsUsePredictedCcsForStructureElucidation).AddTo(Disposables);

            IsUseCcsInchikeyAdductLibrary = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseCcsInchikeyAdductLibrary).AddTo(Disposables);

            CcsAdductInChIKeyDictionaryFilepath = model.ToReactivePropertyAsSynchronized(m => m.CcsAdductInChIKeyDictionaryFilepath).AddTo(Disposables);

            IsUseExperimentalCcsForSpectralSearching = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseExperimentalCcsForSpectralSearching).AddTo(Disposables);

            CcsToleranceForSpectralSearching = model.ToReactivePropertyAsSynchronized(
                m => m.CcsToleranceForSpectralSearching,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => CcsToleranceForSpectralSearching).AddTo(Disposables);

            IsUseCcsForFilteringCandidates = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseCcsForFilteringCandidates).AddTo(Disposables);

            FormulaFinderAdductIonSettingViewModel = new FormulaFinderAdductIonSettingViewModel(model.FormulaFinderAdductIonSetting).AddTo(Disposables);
            OpenSetAdductTypeWindow = new ReactiveCommand()
                .WithSubscribe(() =>
                {
                    broker.Publish(FormulaFinderAdductIonSettingViewModel);
                });

            Save = new ReactiveCommand().WithSubscribe(() => {
                model.Commit();
            }).AddTo(Disposables);

            Cancel = new ReactiveCommand().WithSubscribe(() => {
                model.Cancel();
            }).AddTo(Disposables);
        }

        public InternalMsFinderViewModel InternalMsFinderViewModel { get; set; }

        public FormulaFinderAdductIonSettingViewModel FormulaFinderAdductIonSettingViewModel { get; }

        public ReactivePropertySlim<MassToleranceType> MassTolType { get; }

        public ReactiveProperty<string> Mass1Tolerance { get; }

        public ReactiveProperty<string> Mass2Tolerance { get; }

        public ReactiveProperty<string> IsotopicAbundanceTolerance { get; }

        public ReactiveProperty<string> MassRangeMin { get; }

        public ReactiveProperty<string> MassRangeMax { get; }

        public ReactiveProperty<string> RelativeAbundanceCutOff { get; }

        public ReactivePropertySlim<SolventType> SolventType { get; }

        public ReactivePropertySlim<RetentionType> RetentionType { get; }

        public ReactivePropertySlim<CoverRange> CoverRange { get; }

        public ReactivePropertySlim<bool> IsLewisAndSeniorCheck { get; }

        public ReactivePropertySlim<bool> IsElementProbabilityCheck { get; }

        public ReactivePropertySlim<bool> IsOcheck { get; }

        public ReactivePropertySlim<bool> IsNcheck { get; }

        public ReactivePropertySlim<bool> IsPcheck { get; }

        public ReactivePropertySlim<bool> IsScheck { get; }

        public ReactivePropertySlim<bool> IsFcheck { get; }

        public ReactivePropertySlim<bool> IsClCheck { get; }

        public ReactivePropertySlim<bool> IsBrCheck { get; }

        public ReactivePropertySlim<bool> IsIcheck { get; }

        public ReactivePropertySlim<bool> IsSiCheck { get; }

        public ReactivePropertySlim<bool> IsNitrogenRule { get; }

        public ReactiveProperty<string> FormulaScoreCutOff { get; }

        public ReactivePropertySlim<bool> CanExcuteMS1AdductSearch { get; }

        public ReactivePropertySlim<bool> CanExcuteMS2AdductSearch { get; }

        public ReactiveProperty<string> FormulaMaximumReportNumber { get; }

        public ReactivePropertySlim<bool> IsNeutralLossCheck { get; }

        public ReactiveProperty<string> TreeDepth { get; }

        public ReactiveProperty<string> StructureScoreCutOff { get; }

        public ReactiveProperty<string> StructureMaximumReportNumber { get; }

        public ReactivePropertySlim<bool> IsUserDefinedDB { get; }

        public ReactiveProperty<string> UserDefinedDbFilePath { get; }

        public ReactivePropertySlim<bool> IsAllProcess { get; set; }

        public ReactivePropertySlim<bool> IsUseEiFragmentDB { get; }

        // Check
        public ReactivePropertySlim<bool> Chebi { get; }

        public ReactivePropertySlim<bool> Hmdb { get; }

        public ReactivePropertySlim<bool> Pubchem { get; }

        public ReactivePropertySlim<bool> Smpdb { get; }

        public ReactivePropertySlim<bool> Unpd { get; }

        public ReactivePropertySlim<bool> Ymdb { get; }

        public ReactivePropertySlim<bool> Plantcyc { get; }

        public ReactivePropertySlim<bool> Knapsack { get; }

        public ReactivePropertySlim<bool> Bmdb { get; }

        public ReactivePropertySlim<bool> Drugbank { get; }

        public ReactivePropertySlim<bool> Ecmdb { get; }

        public ReactivePropertySlim<bool> Foodb { get; }

        public ReactivePropertySlim<bool> T3db { get; }

        public ReactivePropertySlim<bool> Stoff { get; }

        public ReactivePropertySlim<bool> Nanpdb { get; }

        public ReactivePropertySlim<bool> Lipidmaps { get; }

        public ReactivePropertySlim<bool> Feces { get; }

        public ReactivePropertySlim<bool> Saliva { get; }

        public ReactivePropertySlim<bool> Serum { get; }

        public ReactivePropertySlim<bool> Urine { get; }

        public ReactivePropertySlim<bool> Csf { get; }

        public ReactivePropertySlim<bool> Blexp { get; }

        public ReactivePropertySlim<bool> Npa { get; }

        public ReactivePropertySlim<bool> Coconut { get; }

        public ReactivePropertySlim<bool> IsPubChemNeverUse { get; }

        public ReactivePropertySlim<bool> IsPubChemOnlyUseForNecessary { get; }

        public ReactivePropertySlim<bool> IsPubChemAllTime { get; }

        public ReactivePropertySlim<bool> IsMinesNeverUse { get; }

        public ReactivePropertySlim<bool> IsMinesOnlyUseForNecessary { get; }

        public ReactivePropertySlim<bool> IsMinesAllTime { get; }

        public ReactiveProperty<string> CLabelMass { get; }

        public ReactiveProperty<string> HLabelMass { get; }

        public ReactiveProperty<string> NLabelMass { get; }

        public ReactiveProperty<string> OLabelMass { get; }

        public ReactiveProperty<string> PLabelMass { get; }

        public ReactiveProperty<string> SLabelMass { get; }

        public ReactiveProperty<string> FLabelMass { get; }

        public ReactiveProperty<string> ClLabelMass { get; }

        public ReactiveProperty<string> BrLabelMass { get; }

        public ReactiveProperty<string> ILabelMass { get; }

        public ReactiveProperty<string> SiLabelMass { get; }

        public ReactivePropertySlim<bool> IsTmsMeoxDerivative { get; }

        public ReactiveProperty<string> MinimumTmsCount { get; }

        public ReactiveProperty<string> MinimumMeoxCount { get; }

        public ReactivePropertySlim<bool> IsRunSpectralDbSearch { get; }

        public ReactivePropertySlim<bool> IsRunInSilicoFragmenterSearch { get; }

        public ReactivePropertySlim<bool> IsPrecursorOrientedSearch { get; }

        public ReactivePropertySlim<bool> IsUseInternalExperimentalSpectralDb { get; }

        public ReactivePropertySlim<bool> IsUseInSilicoSpectralDbForLipids { get; }

        public ReactivePropertySlim<bool> IsUseUserDefinedSpectralDb { get; }

        public ReactiveProperty<string> UserDefinedSpectralDbFilePath { get; }

        //public ReactiveProperty<string> LipidQueryBean { get; }

        public ReactiveProperty<string> ScoreCutOffForSpectralMatch { get; }

        public ReactivePropertySlim<bool> IsUsePredictedRtForStructureElucidation { get; }

        public ReactiveProperty<string> RtSmilesDictionaryFilepath { get; }

        public ReactiveProperty<string> Coeff_RtPrediction { get; }

        public ReactiveProperty<string> Intercept_RtPrediction { get; }

        public ReactiveProperty<string> RtToleranceForStructureElucidation { get; }

        public ReactivePropertySlim<bool> IsUseRtInchikeyLibrary { get; }

        public ReactivePropertySlim<bool> IsUseXlogpPrediction { get; }

        public ReactiveProperty<string> RtInChIKeyDictionaryFilepath { get; }

        public ReactivePropertySlim<bool> IsUseRtForFilteringCandidates { get; }

        public ReactivePropertySlim<bool> IsUseExperimentalRtForSpectralSearching { get; }

        public ReactiveProperty<string> RtToleranceForSpectralSearching { get; }

        public ReactiveProperty<string> RtPredictionSummaryReport { get; }

        public ReactiveProperty<string> FseaRelativeAbundanceCutOff { get; }

        //public ReactiveProperty<string> FseanonsignificantDef { get; }

        public ReactiveProperty<string> FseaPvalueCutOff { get; }

        public ReactivePropertySlim<bool> IsMmnLocalCytoscape { get; }

        public ReactivePropertySlim<bool> IsMmnMsdialOutput { get; }

        public ReactivePropertySlim<bool> IsMmnFormulaBioreaction { get; }

        public ReactivePropertySlim<bool> IsMmnRetentionRestrictionUsed { get; }

        public ReactivePropertySlim<bool> IsMmnOntologySimilarityUsed { get; }

        public ReactiveProperty<string> MmnMassTolerance { get; }

        public ReactiveProperty<string> MmnRelativeCutoff { get; }

        public ReactiveProperty<string> MmnMassSimilarityCutOff { get; }

        public ReactiveProperty<string> MmnRtTolerance { get; }

        public ReactiveProperty<string> MmnOntologySimilarityCutOff { get; }

        public ReactiveProperty<string> MmnOutputFolderPath { get; }

        public ReactiveProperty<string> MmnRtToleranceForReaction { get; }

        public ReactivePropertySlim<bool> IsMmnSelectedFileCentricProcess { get; }

        public ReactiveProperty<string> FormulaPredictionTimeOut { get; }

        public ReactiveProperty<string> StructurePredictionTimeOut { get; }

        public ReactiveProperty<string> CcsToleranceForStructureElucidation { get; }

        public ReactivePropertySlim<bool> IsUsePredictedCcsForStructureElucidation { get; }

        public ReactivePropertySlim<bool> IsUseCcsInchikeyAdductLibrary { get; }

        public ReactiveProperty<string> CcsAdductInChIKeyDictionaryFilepath { get; }

        public ReactivePropertySlim<bool> IsUseExperimentalCcsForSpectralSearching { get; }

        public ReactiveProperty<string> CcsToleranceForSpectralSearching { get; }

        public ReactivePropertySlim<bool> IsUseCcsForFilteringCandidates { get; }

        public ReactiveCommand OpenSetAdductTypeWindow { get; }

        public ReactiveCommand Save { get; }

        public ReactiveCommand Cancel { get; }

        public override ICommand? FinishCommand => Save;
        public override ICommand? CancelCommand => Cancel;
    }
}
