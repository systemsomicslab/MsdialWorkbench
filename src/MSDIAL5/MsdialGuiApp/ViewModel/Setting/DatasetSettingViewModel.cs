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

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class DatasetSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public DatasetSettingViewModel(DatasetSettingModel model, IObservable<bool> isEnabled) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            IsReadOnly = Model.IsReadOnlyDatasetParameter;
            SettingViewModels = new ObservableCollection<ISettingViewModel>(
                new ISettingViewModel[]
                {
                    new DatasetFileSettingViewModel(model.DatasetFileSettingModel, isEnabled).AddTo(Disposables),
                    new DatasetParameterSettingViewModel(model.DatasetParameterSettingModel, isEnabled).AddTo(Disposables),
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

            var methodIsEnabled = new[]
            {
                isEnabled,
                ObserveChangeAfterDecision.Inverse(),
            }.CombineLatestValuesAreAllTrue();
            MethodSettingViewModel = Model.ObserveProperty(m => m.MethodSettingModel)
                .Select(m => m is null ? null : new MethodSettingViewModel(m, methodIsEnabled))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public DatasetSettingModel Model { get; }

        public ReadOnlyReactivePropertySlim<MethodSettingViewModel?> MethodSettingViewModel { get; }
        public ObservableCollection<ISettingViewModel> SettingViewModels { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }
        public bool IsReadOnly { get; }

        public IObservable<Unit> ObserveChanges { get; }

        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ISettingViewModel? Next(ISettingViewModel selected) {
            var current = SettingViewModels.IndexOf(selected);
            if (current >= 0) {
                for (int i = 0; i <= current; i++) {
                    SettingViewModels[i].Next(selected);
                }
                var next = current + 1;
                if (next < SettingViewModels.Count) {
                    return SettingViewModels[next];
                }
            }
            return null;
        }
    }
}
