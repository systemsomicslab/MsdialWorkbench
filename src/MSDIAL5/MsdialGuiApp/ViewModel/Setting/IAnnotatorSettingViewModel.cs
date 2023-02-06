using CompMs.App.Msdial.Model.Setting;
using Reactive.Bindings;
using System;
using System.ComponentModel;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public interface IAnnotatorSettingViewModel : IDisposable, INotifyPropertyChanged
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
