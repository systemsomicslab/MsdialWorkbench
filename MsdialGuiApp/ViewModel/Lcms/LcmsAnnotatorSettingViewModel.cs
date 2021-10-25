using CompMs.App.Msdial.Model.Lcms;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    public interface ILcmsAnnotatorSettingViewModel : IDisposable
    {
        ILcmsAnnotatorSettingModel Model { get; }
        ReactiveProperty<string> AnnotatorID { get; }
        ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }

    public sealed class LcmsMspAnnotatorSettingViewModel : ViewModelBase, ILcmsAnnotatorSettingViewModel
    {
        private readonly LcmsMspAnnotatorSettingModel model;

        public LcmsMspAnnotatorSettingViewModel(LcmsMspAnnotatorSettingModel model) {
            this.model = model;
            AnnotatorID = this.model.ToReactivePropertyAsSynchronized(m => m.AnnotatorID)
                .SetValidateAttribute(() => AnnotatorID)
                .AddTo(Disposables);
            ParameterViewModel = new MsRefSearchParameterBaseViewModel(this.model.SearchParameter).AddTo(Disposables);
            ObserveHasErrors = new[]
            {
                AnnotatorID.ObserveHasErrors,
                ParameterViewModel.Ms1Tolerance.ObserveHasErrors,
                ParameterViewModel.Ms2Tolerance.ObserveHasErrors,
                ParameterViewModel.RtTolerance.ObserveHasErrors,
                ParameterViewModel.RelativeAmpCutoff.ObserveHasErrors,
                ParameterViewModel.AbsoluteAmpCutoff.ObserveHasErrors,
                ParameterViewModel.MassRangeBegin.ObserveHasErrors,
                ParameterViewModel.MassRangeEnd.ObserveHasErrors,
                ParameterViewModel.SimpleDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.WeightedDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.ReverseDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.MatchedPeaksPercentageCutOff.ObserveHasErrors,
                ParameterViewModel.MinimumSpectrumMatch.ObserveHasErrors,
                ParameterViewModel.IsUseTimeForAnnotationFiltering.ObserveHasErrors,
                ParameterViewModel.IsUseTimeForAnnotationScoring.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        ILcmsAnnotatorSettingModel ILcmsAnnotatorSettingViewModel.Model => model;
        public MsRefSearchParameterBaseViewModel ParameterViewModel { get; }
        [Required(ErrorMessage = "Annotator id is required.")]
        public ReactiveProperty<string> AnnotatorID { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }

    public sealed class LcmsTextDBAnnotatorSettingViewModel : ViewModelBase, ILcmsAnnotatorSettingViewModel
    {
        private readonly LcmsTextDBAnnotatorSettingModel model;

        public LcmsTextDBAnnotatorSettingViewModel(LcmsTextDBAnnotatorSettingModel model) {
            this.model = model;
            AnnotatorID = this.model.ToReactivePropertyAsSynchronized(m => m.AnnotatorID)
                .SetValidateAttribute(() => AnnotatorID)
                .AddTo(Disposables);
            ParameterViewModel = new MsRefSearchParameterBaseViewModel(this.model.SearchParameter).AddTo(Disposables);
            ObserveHasErrors = new[]
            {
                AnnotatorID.ObserveHasErrors,
                ParameterViewModel.Ms1Tolerance.ObserveHasErrors,
                ParameterViewModel.RtTolerance.ObserveHasErrors,
                ParameterViewModel.IsUseTimeForAnnotationFiltering.ObserveHasErrors,
                ParameterViewModel.IsUseTimeForAnnotationScoring.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        ILcmsAnnotatorSettingModel ILcmsAnnotatorSettingViewModel.Model => model;
        public MsRefSearchParameterBaseViewModel ParameterViewModel { get; }
        [Required(ErrorMessage = "Annotator id is required.")]
        public ReactiveProperty<string> AnnotatorID { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }

    public class LcmsProteomicsAnnotatorSettingViewModel : ViewModelBase, ILcmsAnnotatorSettingViewModel
    {
        private readonly LcmsProteomicsAnnotatorSettingModel model;

        public LcmsProteomicsAnnotatorSettingViewModel(LcmsProteomicsAnnotatorSettingModel model) {
            this.model = model;
            AnnotatorID = this.model.ToReactivePropertyAsSynchronized(m => m.AnnotatorID)
                .SetValidateAttribute(() => AnnotatorID)
                .AddTo(Disposables);
            ParameterViewModel = new MsRefSearchParameterBaseViewModel(this.model.SearchParameter).AddTo(Disposables);
            ProteomicsParameterVM = new ProteomicsParameterVM(this.model.ProteomicsParameter).AddTo(Disposables);
            ObserveHasErrors = new[]
            {
                AnnotatorID.ObserveHasErrors,
                ParameterViewModel.Ms1Tolerance.ObserveHasErrors,
                ParameterViewModel.Ms2Tolerance.ObserveHasErrors,
                ParameterViewModel.RtTolerance.ObserveHasErrors,
                ParameterViewModel.RelativeAmpCutoff.ObserveHasErrors,
                ParameterViewModel.AbsoluteAmpCutoff.ObserveHasErrors,
                ParameterViewModel.MassRangeBegin.ObserveHasErrors,
                ParameterViewModel.MassRangeEnd.ObserveHasErrors,
                ParameterViewModel.SimpleDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.WeightedDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.ReverseDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.MatchedPeaksPercentageCutOff.ObserveHasErrors,
                ParameterViewModel.MinimumSpectrumMatch.ObserveHasErrors,
                ParameterViewModel.TotalScoreCutoff.ObserveHasErrors,
                ProteomicsParameterVM.AndromedaDelta.ObserveHasErrors,
                ProteomicsParameterVM.AndromedaMaxPeaks.ObserveHasErrors,
                ProteomicsParameterVM.FalseDiscoveryRateForPeptide.ObserveHasErrors,
                ProteomicsParameterVM.FalseDiscoveryRateForProtein.ObserveHasErrors
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        ILcmsAnnotatorSettingModel ILcmsAnnotatorSettingViewModel.Model => model;
        [Required(ErrorMessage = "Annotator id is required.")]
        public ReactiveProperty<string> AnnotatorID { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public MsRefSearchParameterBaseViewModel ParameterViewModel { get; }
        public ProteomicsParameterVM ProteomicsParameterVM { get; }
    }

    public class LcmsAnnotatorSettingViewModelFactory
    {
        public ILcmsAnnotatorSettingViewModel Create(ILcmsAnnotatorSettingModel model) {
            switch (model) {
                case LcmsMspAnnotatorSettingModel mspModel:
                    return new LcmsMspAnnotatorSettingViewModel(mspModel);
                case LcmsTextDBAnnotatorSettingModel textModel:
                    return new LcmsTextDBAnnotatorSettingViewModel(textModel);
                case LcmsProteomicsAnnotatorSettingModel proteomicsModel:
                    return new LcmsProteomicsAnnotatorSettingViewModel(proteomicsModel);
                default:
                    throw new NotSupportedException(model.GetType().Name);
            }
        }
    }
}
