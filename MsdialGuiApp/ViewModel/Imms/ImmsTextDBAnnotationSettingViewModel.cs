using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    public sealed class ImmsTextDBAnnotationSettingViewModel : ViewModelBase, IAnnotationSettingViewModel
    {
        public ImmsTextDBAnnotationSettingViewModel(DataBaseAnnotationSettingModelBase other) {
            model = new ImmsTextDBAnnotationSettingModel(other);
            ParameterVM = new MsRefSearchParameterBaseViewModel(other.Parameter).AddTo(Disposables);
            AnnotatorID = model.ToReactivePropertySlimAsSynchronized(m => m.AnnotatorID).AddTo(Disposables);
            Label = Observable.Return("ImmsTextDBAnnotator").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            hasErrors = new[]
            {
                ParameterVM.Ms1Tolerance.ObserveHasErrors,
                ParameterVM.CcsTolerance.ObserveHasErrors,
                ParameterVM.TotalScoreCutoff.ObserveHasErrors,
                ParameterVM.IsUseCcsForAnnotationFiltering.ObserveHasErrors,
                ParameterVM.IsUseCcsForAnnotationScoring.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        private readonly ImmsTextDBAnnotationSettingModel model;
        private readonly ReadOnlyReactivePropertySlim<bool> hasErrors;

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        public ReactivePropertySlim<string> AnnotatorID { get; }

        public IAnnotationSettingModel Model => model;

        ReadOnlyReactivePropertySlim<bool> IAnnotationSettingViewModel.ObserveHasErrors => hasErrors;

        public ReadOnlyReactivePropertySlim<string> Label { get; }
    }
}
