using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class ProjectSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public ProjectSettingViewModel(ProjectSettingModel model) {
            Model = model;
            IsReadOnly = new ReadOnlyReactivePropertySlim<bool>(Observable.Return(model.IsReadOnlyProjectParameter)).AddTo(Disposables);
            SettingViewModels = new ObservableCollection<ISettingViewModel>(
                new ISettingViewModel[]
                {
                    new ProjectParameterSettingViewModel(model.ProjectParameterSettingModel).AddTo(Disposables),
                });

            ObserveChanges = SettingViewModels.ObserveElementObservableProperty(vm => vm.ObserveChanges).Select(pack => pack.Value);

            ObserveHasErrors = SettingViewModels.ObserveElementObservableProperty(vm => vm.ObserveHasErrors)
                .SelectSwitch(_ => SettingViewModels.Select(vm => vm.ObserveHasErrors).CombineLatestValuesAreAnyTrue())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveChangeAfterDecision = SettingViewModels.ObserveElementObservableProperty(vm => vm.ObserveChangeAfterDecision)
                .SelectSwitch(_ => SettingViewModels.Select(vm => vm.ObserveChangeAfterDecision).CombineLatestValuesAreAnyTrue())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            DatasetSettingViewModel = model
                .ObserveProperty(m => m.DatasetSettingModel)
                .Select(m => m is null ? null : new DatasetSettingViewModel(m, ObserveChangeAfterDecision.Inverse()))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public ProjectSettingModel Model { get; }

        public ReadOnlyReactivePropertySlim<DatasetSettingViewModel?> DatasetSettingViewModel { get; }

        public ObservableCollection<ISettingViewModel> SettingViewModels { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }

        public ReadOnlyReactivePropertySlim<bool> IsReadOnly { get; }

        public IObservable<Unit> ObserveChanges { get; }

        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ISettingViewModel? Next(ISettingViewModel selected) {
            var current = SettingViewModels.IndexOf(selected);
            if (current >= 0) {
                selected.Next(selected);
                var next = current + 1;
                if (next < SettingViewModels.Count) {
                    return SettingViewModels[next];
                }
            }
            return null;
        }

        public Task RunAsync() {
            return Model.RunAsync();
        }
    }
}
