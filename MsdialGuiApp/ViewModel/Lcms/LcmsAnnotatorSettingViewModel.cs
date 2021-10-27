using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    public sealed class LcmsMspAnnotatorSettingViewModel : ViewModelBase, IAnnotatorSettingViewModel
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

        IAnnotatorSettingModel IAnnotatorSettingViewModel.Model => model;
        public MsRefSearchParameterBaseViewModel ParameterViewModel { get; }
        [Required(ErrorMessage = "Annotator id is required.")]
        public ReactiveProperty<string> AnnotatorID { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }

    public sealed class LcmsTextDBAnnotatorSettingViewModel : ViewModelBase, IAnnotatorSettingViewModel
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

        IAnnotatorSettingModel IAnnotatorSettingViewModel.Model => model;
        public MsRefSearchParameterBaseViewModel ParameterViewModel { get; }
        [Required(ErrorMessage = "Annotator id is required.")]
        public ReactiveProperty<string> AnnotatorID { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }

    public class LcmsProteomicsAnnotatorSettingViewModel : ViewModelBase, IAnnotatorSettingViewModel
    {
        private readonly LcmsProteomicsAnnotatorSettingModel model;

        public LcmsProteomicsAnnotatorSettingViewModel(LcmsProteomicsAnnotatorSettingModel model) {
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
                ParameterViewModel.TotalScoreCutoff.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        IAnnotatorSettingModel IAnnotatorSettingViewModel.Model => model;
        [Required(ErrorMessage = "Annotator id is required.")]
        public ReactiveProperty<string> AnnotatorID { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public MsRefSearchParameterBaseViewModel ParameterViewModel { get; }
        public ProteomicsParameterVM ProteomicsParameterVM { get; }
    }

    public class LcmsAnnotatorSettingViewModelFactory : IAnnotatorSettingViewModelFactory
    {
        public IAnnotatorSettingViewModel Create(IAnnotatorSettingModel model) {
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
