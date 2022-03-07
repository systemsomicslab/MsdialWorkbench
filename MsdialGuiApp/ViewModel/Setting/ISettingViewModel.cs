using System;
using System.Reactive;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public interface ISettingViewModel
    {
        void Next();
        // void Run();

        IObservable<bool> ObserveHasErrors { get; }
        IObservable<bool> ObserveChangeAfterDecision { get; }
        IObservable<Unit> ObserveChanges { get; }
    }
}
