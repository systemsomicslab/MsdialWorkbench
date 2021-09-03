using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    public sealed class DimsMspAnnotationSettingViewModel : ViewModelBase, IAnnotationSettingViewModel
    {
        private readonly DimsMspAnnotationSettingModel model;

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }
        public ReactivePropertySlim<string> AnnotatorID { get; }
        public ReadOnlyReactivePropertySlim<string> Label { get; }

        private readonly ReadOnlyReactivePropertySlim<bool> hasErrors;
        ReadOnlyReactivePropertySlim<bool> IAnnotationSettingViewModel.ObserveHasErrors => hasErrors;

        public IAnnotationSettingModel Model => model;

        public DimsMspAnnotationSettingViewModel(DataBaseAnnotationSettingModelBase other) {
            model = new DimsMspAnnotationSettingModel(other);
            ParameterVM = new MsRefSearchParameterBaseViewModel(other.Parameter).AddTo(Disposables);
            AnnotatorID = model.ToReactivePropertySlimAsSynchronized(m => m.AnnotatorID).AddTo(Disposables);
            Label = Observable.Return("DimsMspAnnotator").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            hasErrors = new[]
            {
                ParameterVM.Ms1Tolerance.ObserveHasErrors,
                ParameterVM.Ms2Tolerance.ObserveHasErrors,
                ParameterVM.RelativeAmpCutoff.ObserveHasErrors,
                ParameterVM.AbsoluteAmpCutoff.ObserveHasErrors,
                ParameterVM.MassRangeBegin.ObserveHasErrors,
                ParameterVM.MassRangeEnd.ObserveHasErrors,
                ParameterVM.SimpleDotProductCutOff.ObserveHasErrors,
                ParameterVM.WeightedDotProductCutOff.ObserveHasErrors,
                ParameterVM.ReverseDotProductCutOff.ObserveHasErrors,
                ParameterVM.MatchedPeaksPercentageCutOff.ObserveHasErrors,
                ParameterVM.MinimumSpectrumMatch.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }
    }
}
