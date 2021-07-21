using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    class LcmsMspAnnotationSettingViewModel : ViewModelBase, IAnnotationSettingViewModel
    {
        public LcmsMspAnnotationSettingViewModel(DataBaseAnnotationSettingModelBase other) {
            model = new LcmsMspAnnotationSettingModel(other);
            ParameterVM = new MsRefSearchParameterBaseViewModel(other.Parameter).AddTo(Disposables);
            Label = Observable.Return("LcmsMspAnnotator").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        private readonly LcmsMspAnnotationSettingModel model;

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        public IAnnotationSettingModel Model => model;

        public ReadOnlyReactivePropertySlim<string> Label { get; }
    }
}
