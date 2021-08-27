using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    sealed class LcmsTextDBAnnotationSettingViewModel : ViewModelBase, IAnnotationSettingViewModel {
        public LcmsTextDBAnnotationSettingViewModel(DataBaseAnnotationSettingModelBase other) {
            model = new LcmsTextDBAnnotationSettingModel(other);
            ParameterVM = new MsRefSearchParameterBaseViewModel(other.Parameter).AddTo(Disposables);
            AnnotatorID = model.ToReactivePropertySlimAsSynchronized(m => m.AnnotatorID).AddTo(Disposables);
            Label = Observable.Return("LcmsTextDBAnnotator").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            hasErrors = new[]
            {
                ParameterVM.Ms1Tolerance.ObserveHasErrors,
                ParameterVM.RtTolerance.ObserveHasErrors,
                ParameterVM.TotalScoreCutoff.ObserveHasErrors,
                ParameterVM.IsUseTimeForAnnotationFiltering.ObserveHasErrors,
                ParameterVM.IsUseTimeForAnnotationScoring.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        private readonly LcmsTextDBAnnotationSettingModel model;
        private readonly ReadOnlyReactivePropertySlim<bool> hasErrors;

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        public ReactivePropertySlim<string> AnnotatorID { get; }

        public IAnnotationSettingModel Model => model;

        ReadOnlyReactivePropertySlim<bool> IAnnotationSettingViewModel.ObserveHasErrors => hasErrors;

        public ReadOnlyReactivePropertySlim<string> Label { get; }
    }
}
