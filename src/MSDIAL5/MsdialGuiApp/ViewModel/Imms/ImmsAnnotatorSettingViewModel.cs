using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    public sealed class ImmsMspAnnotatorSettingViewModel : ViewModelBase, IAnnotatorSettingViewModel
    {
        private readonly ImmsMspAnnotatorSettingModel model;

        public ImmsMspAnnotatorSettingViewModel(ImmsMspAnnotatorSettingModel model) {
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
                ParameterViewModel.CcsTolerance.ObserveHasErrors,
                ParameterViewModel.RelativeAmpCutoff.ObserveHasErrors,
                ParameterViewModel.AbsoluteAmpCutoff.ObserveHasErrors,
                ParameterViewModel.MassRangeBegin.ObserveHasErrors,
                ParameterViewModel.MassRangeEnd.ObserveHasErrors,
                ParameterViewModel.SimpleDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.WeightedDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.ReverseDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.MatchedPeaksPercentageCutOff.ObserveHasErrors,
                ParameterViewModel.MinimumSpectrumMatch.ObserveHasErrors,
                ParameterViewModel.IsUseCcsForAnnotationFiltering.ObserveHasErrors,
                ParameterViewModel.IsUseCcsForAnnotationScoring.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        IAnnotatorSettingModel IAnnotatorSettingViewModel.Model => model;
        public MsRefSearchParameterBaseViewModel ParameterViewModel { get; }
        [Required(ErrorMessage = "Annotator id is required.")]
        public ReactiveProperty<string> AnnotatorID { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }

    public sealed class ImmsTextDBAnnotatorSettingViewModel : ViewModelBase, IAnnotatorSettingViewModel
    {
        private readonly ImmsTextDBAnnotatorSettingModel model;

        public ImmsTextDBAnnotatorSettingViewModel(ImmsTextDBAnnotatorSettingModel model) {
            this.model = model;
            AnnotatorID = this.model.ToReactivePropertyAsSynchronized(m => m.AnnotatorID)
                .SetValidateAttribute(() => AnnotatorID)
                .AddTo(Disposables);
            ParameterViewModel = new MsRefSearchParameterBaseViewModel(this.model.SearchParameter).AddTo(Disposables);
            ObserveHasErrors = new[]
            {
                AnnotatorID.ObserveHasErrors,
                ParameterViewModel.Ms1Tolerance.ObserveHasErrors,
                ParameterViewModel.CcsTolerance.ObserveHasErrors,
                ParameterViewModel.IsUseCcsForAnnotationFiltering.ObserveHasErrors,
                ParameterViewModel.IsUseCcsForAnnotationScoring.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        IAnnotatorSettingModel IAnnotatorSettingViewModel.Model => model;
        public MsRefSearchParameterBaseViewModel ParameterViewModel { get; }
        [Required(ErrorMessage = "Annotator id is required.")]
        public ReactiveProperty<string> AnnotatorID { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }

    public class ImmsEadLipidAnnotatorSettingViewModel : ViewModelBase, IAnnotatorSettingViewModel
    {
        public ImmsEadLipidAnnotatorSettingViewModel(ImmsEadLipidAnnotatorSettingModel model) {
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
                ParameterViewModel.CcsTolerance.ObserveHasErrors,
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
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        private readonly ImmsEadLipidAnnotatorSettingModel model;
        public IAnnotatorSettingModel Model => model;

        [Required(ErrorMessage = "Annotator id is required.")]
        public ReactiveProperty<string> AnnotatorID { get; }

        public MsRefSearchParameterBaseViewModel ParameterViewModel { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }

    public class ImmsAnnotatorSettingViewModelFactory : IAnnotatorSettingViewModelFactory
    {
        public IAnnotatorSettingViewModel Create(IAnnotatorSettingModel model) {
            switch (model) {
                case ImmsMspAnnotatorSettingModel mspModel:
                    return new ImmsMspAnnotatorSettingViewModel(mspModel);
                case ImmsTextDBAnnotatorSettingModel textModel:
                    return new ImmsTextDBAnnotatorSettingViewModel(textModel);
                case ImmsEadLipidAnnotatorSettingModel eadLipidModel:
                    return new ImmsEadLipidAnnotatorSettingViewModel(eadLipidModel);
                default:
                    throw new NotSupportedException(model.GetType().Name);
            }
        }
    }
}
