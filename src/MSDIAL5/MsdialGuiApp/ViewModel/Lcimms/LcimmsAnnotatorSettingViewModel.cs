using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    public sealed class LcimmsMspAnnotatorSettingViewModel : ViewModelBase, IAnnotatorSettingViewModel
    {
        private readonly LcimmsMspAnnotatorSettingModel model;

        public LcimmsMspAnnotatorSettingViewModel(LcimmsMspAnnotatorSettingModel model) {
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
                ParameterViewModel.IsUseTimeForAnnotationFiltering.ObserveHasErrors,
                ParameterViewModel.IsUseTimeForAnnotationScoring.ObserveHasErrors,
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

    public sealed class LcimmsTextDBAnnotatorSettingViewModel : ViewModelBase, IAnnotatorSettingViewModel
    {
        private readonly LcimmsTextDBAnnotatorSettingModel model;

        public LcimmsTextDBAnnotatorSettingViewModel(LcimmsTextDBAnnotatorSettingModel model) {
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
                ParameterViewModel.CcsTolerance.ObserveHasErrors,
                ParameterViewModel.IsUseTimeForAnnotationFiltering.ObserveHasErrors,
                ParameterViewModel.IsUseTimeForAnnotationScoring.ObserveHasErrors,
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

    public sealed class LcimmsAnnotatorSettingViewModelFactory : IAnnotatorSettingViewModelFactory
    {
        public IAnnotatorSettingViewModel Create(IAnnotatorSettingModel model) {
            switch (model) {
                case LcimmsMspAnnotatorSettingModel mspModel:
                    return new LcimmsMspAnnotatorSettingViewModel(mspModel);
                case LcimmsTextDBAnnotatorSettingModel textModel:
                    return new LcimmsTextDBAnnotatorSettingViewModel(textModel);
                default:
                    throw new NotSupportedException(model.GetType().Name);
            }
        }
    }
}
