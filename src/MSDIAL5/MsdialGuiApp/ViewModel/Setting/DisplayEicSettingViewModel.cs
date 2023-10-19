using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class DisplayEicSettingViewModel : ViewModelBase {
        private readonly DisplayEicSettingModel _model;

        public DisplayEicSettingViewModel(DisplayEicSettingModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            DiplayEicSettingValues = model.DisplayEicSettingValueModels
                .ToReadOnlyReactiveCollection(x => new PeakFeatureSearchValueViewModel(x))
                .AddTo(Disposables);

            var settingHasError = new[] {
                DiplayEicSettingValues.ObserveAddChanged().ToUnit(),
                DiplayEicSettingValues.ObserveRemoveChanged().ToUnit(),
                DiplayEicSettingValues.ObserveElementObservableProperty(vm => vm.HasErrors).ToUnit(),
            }.Merge()
            .Select(_ => DiplayEicSettingValues.Any(vm => vm.HasErrors.Value));

            ObserveHasErrors = new[]
            {
                settingHasError
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim(true)
            .AddTo(Disposables);

            ApplyCommand = ObserveHasErrors.Inverse()
               .ToReactiveCommand()
               .WithSubscribe(Commit)
               .AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<PeakFeatureSearchValueViewModel> DiplayEicSettingValues { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReactiveCommand ApplyCommand { get; }
        public bool DialogResult { get; private set; } = false;

        private void Commit() {
            foreach (var value in DiplayEicSettingValues) {
                value.Commit();
            }
            DialogResult = true;
        }
    }
}
