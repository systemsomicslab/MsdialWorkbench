using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    public sealed class DimsMetabolomicsUseMs2AnnotatorSettingViewModel : ViewModelBase, IAnnotatorSettingViewModel
    {
        private readonly DimsMetabolomicsUseMs2AnnotatorSettingModel model;

        public DimsMetabolomicsUseMs2AnnotatorSettingViewModel(DimsMetabolomicsUseMs2AnnotatorSettingModel model) {
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
                ParameterViewModel.RelativeAmpCutoff.ObserveHasErrors,
                ParameterViewModel.AbsoluteAmpCutoff.ObserveHasErrors,
                ParameterViewModel.MassRangeBegin.ObserveHasErrors,
                ParameterViewModel.MassRangeEnd.ObserveHasErrors,
                ParameterViewModel.SimpleDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.WeightedDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.ReverseDotProductCutOff.ObserveHasErrors,
                ParameterViewModel.MatchedPeaksPercentageCutOff.ObserveHasErrors,
                ParameterViewModel.MinimumSpectrumMatch.ObserveHasErrors,
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

    public sealed class DimsMetabolomicsAnnotatorSettingViewModel : ViewModelBase, IAnnotatorSettingViewModel
    {
        private readonly DimsMetabolomicsAnnotatorSettingModel model;

        public DimsMetabolomicsAnnotatorSettingViewModel(DimsMetabolomicsAnnotatorSettingModel model) {
            this.model = model;
            AnnotatorID = this.model.ToReactivePropertyAsSynchronized(m => m.AnnotatorID)
                .SetValidateAttribute(() => AnnotatorID)
                .AddTo(Disposables);
            ParameterViewModel = new MsRefSearchParameterBaseViewModel(this.model.SearchParameter).AddTo(Disposables);
            ObserveHasErrors = new[]
            {
                AnnotatorID.ObserveHasErrors,
                ParameterViewModel.Ms1Tolerance.ObserveHasErrors,
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

    public sealed class DimsAnnotatorSettingViewModelFactory : IAnnotatorSettingViewModelFactory
    {
        public IAnnotatorSettingViewModel Create(IAnnotatorSettingModel model) {
            switch (model) {
                case DimsMetabolomicsUseMs2AnnotatorSettingModel withMs2Model:
                    return new DimsMetabolomicsUseMs2AnnotatorSettingViewModel(withMs2Model);
                case DimsMetabolomicsAnnotatorSettingModel withoutMs2Model:
                    return new DimsMetabolomicsAnnotatorSettingViewModel(withoutMs2Model);
                default:
                    throw new NotSupportedException(model.GetType().Name);
            }
        }
    }
}
