using CompMs.App.Msdial.Model.Setting;
using Reactive.Bindings;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal interface IAnnotationSettingViewModel
    {
        ReadOnlyReactivePropertySlim<string> Label { get; }
        IAnnotationSettingModel Model { get; }
    }
}