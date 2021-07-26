using CompMs.App.Msdial.Model.Setting;
using Reactive.Bindings;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal interface IAnnotationSettingViewModel
    {
        IAnnotationSettingModel Model { get; }

        ReactivePropertySlim<string> AnnotatorID { get; }
        ReadOnlyReactivePropertySlim<string> Label { get; }
        ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }
}