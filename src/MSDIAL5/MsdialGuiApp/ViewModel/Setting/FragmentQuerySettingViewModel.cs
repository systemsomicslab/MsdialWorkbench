using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting {
    internal sealed class FragmentQuerySettingViewModel : ViewModelBase {
        private readonly FragmentQuerySettingModel _model;
        private Subject<Unit> _commitTrigger;

        public FragmentQuerySettingViewModel(FragmentQuerySettingModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _commitTrigger = new Subject<Unit>().AddTo(Disposables);

            FragmentQuerySettingValues = model.FragmentQuerySettingValueModels
                .ToReadOnlyReactiveCollection(x => new PeakFeatureSearchValueViewModel(x))
                .AddTo(Disposables);

            IsAlignSpotViewSelected = model.IsAlignSpotViewSelected.ToReactivePropertyWithCommit(m => m.Value, CommitAsObservable).AddTo(Disposables);
            SearchOption = model.SearchOption.ToReactivePropertyWithCommit(m => m.Value, CommitAsObservable).AddTo(Disposables);

            var settingHasError = new[] {
                FragmentQuerySettingValues.ObserveAddChanged().ToUnit(),
                FragmentQuerySettingValues.ObserveRemoveChanged().ToUnit(),
                FragmentQuerySettingValues.ObserveElementObservableProperty(vm => vm.HasErrors).ToUnit(),
            }.Merge()
            .Select(_ => FragmentQuerySettingValues.Any(vm => vm.HasErrors.Value));

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

        public ReadOnlyReactiveCollection<PeakFeatureSearchValueViewModel> FragmentQuerySettingValues { get; }
        public ReactiveProperty<bool> IsAlignSpotViewSelected { get; }
        public ReactiveProperty<AndOr> SearchOption { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        public ReactiveCommand ApplyCommand { get; }

        public IObservable<Unit> CommitAsObservable => _commitTrigger.Where(_ => !ObserveHasErrors.Value).ToUnit();

        private void Commit() {
            foreach (var value in FragmentQuerySettingValues) {
                value.Commit();
            }
            _commitTrigger.OnNext(Unit.Default);
        }

        public DelegateCommand ClearList => _clearList ??= new DelegateCommand(_model.ClearListMethod);
        private DelegateCommand? _clearList;
    }
}
