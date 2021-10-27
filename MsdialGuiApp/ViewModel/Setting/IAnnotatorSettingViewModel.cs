using CompMs.App.Msdial.Model.Setting;
using Reactive.Bindings;
using System;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public interface IAnnotatorSettingViewModel : IDisposable
    {
        IAnnotatorSettingModel Model { get; }
        ReactiveProperty<string> AnnotatorID { get; }
        ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }

    public interface IAnnotatorSettingViewModelFactory
    {
        IAnnotatorSettingViewModel Create(IAnnotatorSettingModel model);
    }
}
