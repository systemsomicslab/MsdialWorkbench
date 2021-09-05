using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    public class DimsTextDBAnnotationSettingViewModel : ViewModelBase, IAnnotationSettingViewModel
    {
        public DimsTextDBAnnotationSettingViewModel(DataBaseAnnotationSettingModelBase other) {
            model = new DimsTextDBAnnotationSettingModel(other);
            ParameterVM = new MsRefSearchParameterBaseViewModel(other.Parameter).AddTo(Disposables);
            AnnotatorID = model.ToReactivePropertySlimAsSynchronized(m => m.AnnotatorID).AddTo(Disposables);
            Label = Observable.Return("DimsTextDBAnnotator").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            hasErrors = new[]
            {
                ParameterVM.Ms1Tolerance.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        private readonly DimsTextDBAnnotationSettingModel model;
        private readonly ReadOnlyReactivePropertySlim<bool> hasErrors;

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        public ReactivePropertySlim<string> AnnotatorID { get; }

        public IAnnotationSettingModel Model => model;

        ReadOnlyReactivePropertySlim<bool> IAnnotationSettingViewModel.ObserveHasErrors => hasErrors;

        public ReadOnlyReactivePropertySlim<string> Label { get; }
    }
}
