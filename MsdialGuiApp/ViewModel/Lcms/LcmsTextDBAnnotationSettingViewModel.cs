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
    class LcmsTextDBAnnotationSettingViewModel : ViewModelBase, IAnnotationSettingViewModel
    {
        public LcmsTextDBAnnotationSettingViewModel(DataBaseAnnotationSettingModelBase other) {
            model = new LcmsTextDBAnnotationSettingModel(other);
            ParameterVM = new MsRefSearchParameterBaseViewModel(other.Parameter).AddTo(Disposables);
            Label = Observable.Return("LcmsTextDBAnnotator").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        private readonly LcmsTextDBAnnotationSettingModel model;

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        public IAnnotationSettingModel Model => model;

        public ReadOnlyReactivePropertySlim<string> Label { get; }
    }
}
