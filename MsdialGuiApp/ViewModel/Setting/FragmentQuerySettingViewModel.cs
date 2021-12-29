using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Setting {
    class FragmentQuerySettingViewModel : ViewModelBase {
        public FragmentQuerySettingViewModel(FragmentQuerySettingModel model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }
            Model = model;
            FragmentQuerySettingValues = Model.FragmentQuerySettingValues
                .ToReadOnlyReactiveCollection(x => new PeakFeatureSearchValueViewModel(x))
                .AddTo(Disposables);

            IsAlignSpotViewSelected = Model.IsAlignSpotViewSelected
                .ToReactiveProperty()
                .AddTo(Disposables);
            CommitAsObservable
              .WithLatestFrom(IsAlignSpotViewSelected, (_, x) => x)
              .Subscribe(x => model.IsAlignSpotViewSelected.Value = x)
              .AddTo(Disposables);

            SearchOption = Model.SearchOption
                .ToReactiveProperty()
                .AddTo(Disposables);
            CommitAsObservable
               .WithLatestFrom(SearchOption, (_, x) => x)
               .Subscribe(x => model.SearchOption.Value = x)
               .AddTo(Disposables);

            FragmentQuerySettingValues = Model.FragmentQuerySettingValues
                .ToReadOnlyReactiveCollection(x => new PeakFeatureSearchValueViewModel(x))
                .AddTo(Disposables);

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

        public FragmentQuerySettingModel Model { get; }
        public ReadOnlyReactiveCollection<PeakFeatureSearchValueViewModel> FragmentQuerySettingValues { get; }
        public ReactiveProperty<bool> IsAlignSpotViewSelected { get; }
        public ReactiveProperty<AndOr> SearchOption { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public Subject<Unit> CommitTrigger { get; } = new Subject<Unit>();

        public ReadOnlyReactivePropertySlim<bool> HasErrors { get; }

        public IObservable<Unit> CommitAsObservable => CommitTrigger.Where(_ => !ObserveHasErrors.Value).ToUnit();

        public ReactiveCommand ApplyCommand {
            get;
        }
        public void Commit() {
            foreach (var value in FragmentQuerySettingValues) {
                value.Commit();
            }
            CommitTrigger.OnNext(Unit.Default);
        }

        public DelegateCommand ClearList {
            get {
                return clearList ??
                  (clearList = new DelegateCommand(Model.ClearListMethod));
            }
        }
        private DelegateCommand clearList;
    }
}
