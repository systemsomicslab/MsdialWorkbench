using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class IdentifySettingViewModel : ViewModelBase, ISettingViewModel
    {
        public IdentifySettingViewModel(IdentifySettingModel model, IAnnotatorSettingViewModelFactory annotatorFactory, IObservable<bool> isEnabled) {
            this.model = model;
            IsReadOnly = model.IsReadOnly;

            var selectedAnnotator = new Subject<IAnnotatorSettingViewModel?>();
            var selectedDataBase = new Subject<IDataBaseSettingViewModel>();

            AnnotatorViewModels = this.model.AnnotatorModels.ToReadOnlyReactiveCollection(annotatorFactory.Create).AddTo(Disposables);
            var addedAnnotator = AnnotatorViewModels.ObserveAddChanged();
            var removedAnnotator = AnnotatorViewModels.ObserveRemoveChanged();
            AnnotatorViewModel = new[]
            {
                addedAnnotator,
                removedAnnotator.Select(avm => AnnotatorViewModels.LastOrDefault()),
                selectedDataBase.Where(dvm => dvm is not null).Select(dvm => AnnotatorViewModels.LastOrDefault(avm => avm.Model.DataBaseSettingModel == dvm.Model)),
            }.Merge()
            .ToReactiveProperty((IAnnotatorSettingViewModel?)null).AddTo(Disposables);
            AnnotatorViewModel.Subscribe(selectedAnnotator);

            var dataBaseFactory = new DataBaseSettingViewModelFactory(
                removedAnnotator.Select(dvm => dvm.Model.DataBaseSettingModel)
                    .Where(dm => AnnotatorViewModels.All(avm => avm.Model.DataBaseSettingModel != dm)),
                addedAnnotator.Select(dvm => dvm.Model.DataBaseSettingModel)
                    .Where(dm => AnnotatorViewModels.Any(avm => avm.Model.DataBaseSettingModel == dm)));
            DataBaseViewModels = this.model.DataBaseModels.ToReadOnlyReactiveCollection(dataBaseFactory.Create).AddTo(Disposables);
            var addedDataBase = DataBaseViewModels.ObserveAddChanged();
            var removedDataBase = DataBaseViewModels.ObserveRemoveChanged();
            DataBaseViewModel = new[]
            {
                addedDataBase,
                removedDataBase.Select(dvm => DataBaseViewModels.LastOrDefault()),
                selectedAnnotator.Where(avm => avm is not null).Select(avm => DataBaseViewModels.FirstOrDefault(dvm => dvm.Model == avm!.Model.DataBaseSettingModel)),
            }.Merge()
            .ToReactiveProperty((DataBaseSettingViewModel?)null).AddTo(Disposables);
            DataBaseViewModel.Subscribe(selectedDataBase);

            // DataBase errors
            var dataBaseIDDuplicate = new[]
            {
                addedDataBase.ToUnit(),
                removedDataBase.ToUnit(),
                DataBaseViewModels.ObserveElementObservableProperty(vm => vm.DataBaseID).ToUnit(),
            }.Merge()
            .Select(_ => DataBaseViewModels.Select(vm => vm.DataBaseID.Value).Distinct().Count() != DataBaseViewModels.Count)
            .StartWith(false);
            dataBaseIDDuplicate.Subscribe(hasError => {
                if (hasError) {
                    AddError(nameof(DataBaseViewModels), DataBaseIDDuplicateErrorMessage);
                }
                else {
                    RemoveError(nameof(DataBaseViewModels), DataBaseIDDuplicateErrorMessage);
                }
            }).AddTo(Disposables);
            var dataBaseHasError = new[]
            {
                addedDataBase.ToUnit(),
                removedDataBase.ToUnit(),
                DataBaseViewModels.ObserveElementObservableProperty(vm => vm.ObserveHasErrors).ToUnit(),
            }.Merge()
            .Select(_ => DataBaseViewModels.Any(vm => vm.ObserveHasErrors.Value))
            .StartWith(false);
            var dataBasesDoesnotHaveError = new[]
            {
                dataBaseIDDuplicate,
                dataBaseHasError,
                this.ErrorsChangedAsObservable().Select(_ => ContainsError(nameof(DataBaseViewModels))).StartWith(false),
            }.CombineLatestValuesAreAllFalse();

            // Annotator errors
            var annotatorIDDuplicate = new[]
            {
                addedAnnotator.ToUnit(),
                removedAnnotator.ToUnit(),
                AnnotatorViewModels.ObserveElementObservableProperty(vm => vm.AnnotatorID).ToUnit(),
            }.Merge().StartWith(Unit.Default)
            .Select(_ => AnnotatorViewModels.Select(vm => vm.AnnotatorID.Value).Distinct().Count() != AnnotatorViewModels.Count);
            annotatorIDDuplicate.Subscribe(hasError => {
                if (hasError) {
                    AddError(nameof(AnnotatorViewModels), AnnotatorIDDuplicateErrorMessage);
                }
                else {
                    RemoveError(nameof(AnnotatorViewModels), AnnotatorIDDuplicateErrorMessage);
                }
            }).AddTo(Disposables);
            var annotatorHasError = new[]
            {
                addedAnnotator.ToUnit(),
                removedAnnotator.ToUnit(),
                AnnotatorViewModels.ObserveElementObservableProperty(vm => vm.ObserveHasErrors).ToUnit(),
            }.Merge().StartWith(Unit.Default)
            .Select(_ => AnnotatorViewModels.Any(vm => vm.ObserveHasErrors.Value));
            var annotatorsDoesnotHaveError = new[]
            {
                annotatorIDDuplicate,
                annotatorHasError,
                this.ErrorsChangedAsObservable().Select(_ => ContainsError(nameof(AnnotatorViewModels))).StartWith(false),
            }.CombineLatestValuesAreAllFalse();

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                dataBasesDoesnotHaveError,
                annotatorsDoesnotHaveError,
            }.CombineLatestValuesAreAllTrue()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                DataBaseViewModels.ObserveUntilRemove(item => ((INotifyPropertyChanged)item).PropertyChangedAsObservable()).ToUnit(),
                AnnotatorViewModels.ObserveUntilRemove(item => ((INotifyPropertyChanged)item).PropertyChangedAsObservable()).ToUnit(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            var changes = ObserveChanges.TakeFirstAfterEach(decide);
            ObserveChangeAfterDecision = new[]
            {
                changes.ToConstant(true),
                decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            // Commands
            AddDataBaseCommand = dataBasesDoesnotHaveError.ToReactiveCommand()
                .WithSubscribe(() => this.model.AddDataBase())
                .AddTo(Disposables);
            var dbIsNotNull = DataBaseViewModel.Select(m => !(m is null));
            RemoveDataBaseCommand = dbIsNotNull
                .ToReactiveCommand()
                .WithSubscribe(() => this.model.RemoveDataBase(DataBaseViewModel.Value.Model))
                .AddTo(Disposables);
            AddAnnotatorCommand = new[]{
                dbIsNotNull,
                DataBaseViewModel.Where(vm => vm != null).SelectSwitch(vm => vm.ObserveHasErrors).Inverse(),
                dataBasesDoesnotHaveError,
                annotatorsDoesnotHaveError,
            }.CombineLatestValuesAreAllTrue()
                .ToReactiveCommand()
                .WithSubscribe(() => this.model.AddAnnotator(DataBaseViewModel.Value.Model))
                .AddTo(Disposables);
            var annotatorIsNotNull = AnnotatorViewModel.Select(m => m is not null);
            RemoveAnnotatorCommand = annotatorIsNotNull
                .ToReactiveCommand()
                .WithSubscribe(() => this.model.RemoveAnnotator(AnnotatorViewModel.Value?.Model))
                .AddTo(Disposables);
            var moveUpTrigger = new Subject<Unit>();
            var moveDownTrigger = new Subject<Unit>();
            MoveUpAnnotatorCommand = new[]{
                moveUpTrigger,
                moveDownTrigger,
                AnnotatorViewModel.ToUnit()
            }.Merge()
            .WithLatestFrom(new[] { 
                annotatorIsNotNull,
                annotatorsDoesnotHaveError,
                AnnotatorViewModel.ToUnit().Merge(moveUpTrigger).Select(_ => AnnotatorViewModels.FirstOrDefault() != AnnotatorViewModel.Value),
            }.CombineLatestValuesAreAllTrue(),
            (fst, snd) => snd)
            .ToReactiveCommand()
            .WithSubscribe(() => this.model.MoveUpAnnotator(AnnotatorViewModel.Value?.Model))
            .WithSubscribe(() => moveUpTrigger.OnNext(Unit.Default))
            .AddTo(Disposables);
            MoveDownAnnotatorCommand = new[]{
                moveUpTrigger,
                moveDownTrigger,
                AnnotatorViewModel.ToUnit()
            }.Merge()
            .WithLatestFrom(new[] { 
                annotatorIsNotNull,
                annotatorsDoesnotHaveError,
                AnnotatorViewModel.ToUnit().Merge(moveDownTrigger).Select(_ => AnnotatorViewModels.LastOrDefault() != AnnotatorViewModel.Value),
            }.CombineLatestValuesAreAllTrue(),
            (fst, snd) => snd)
            .ToReactiveCommand()
            .WithSubscribe(() => this.model.MoveDownAnnotator(AnnotatorViewModel.Value?.Model))
            .WithSubscribe(() => moveDownTrigger.OnNext(Unit.Default))
            .AddTo(Disposables);

            DataBaseViewModels.ToObservable()
                .Zip(Observable.Interval(TimeSpan.FromMilliseconds(100)), (a, _) => a)
                .Concat(addedDataBase)
                .Where(vm => vm is not null)
                .SelectMany(vm => dataBasesDoesnotHaveError
                    .TakeUntil(removedDataBase.Where(x => x == vm))
                    .Where(x => x)
                    .Where(_ => AnnotatorViewModels.All(avm => avm.Model.DataBaseSettingModel != vm.Model))
                    .Take(1)
                    .SelectMany(_ => Observable.Interval(TimeSpan.FromMilliseconds(100)))
                    .Do(_ => this.model.AddAnnotator(vm.Model))
                    .Retry(5)
                    .Take(1)
                    .Catch((Exception ex) => Observable.Empty<long>()))
                .Subscribe()
                .AddTo(Disposables);
        }

        private readonly IdentifySettingModel model;

        public bool IsReadOnly { get; }

        public ReadOnlyReactiveCollection<IDataBaseSettingViewModel> DataBaseViewModels { get; }
        public ReactiveProperty<IDataBaseSettingViewModel> DataBaseViewModel { get; }

        public ReadOnlyReactiveCollection<IAnnotatorSettingViewModel> AnnotatorViewModels { get; }

        public ReactiveProperty<IAnnotatorSettingViewModel?> AnnotatorViewModel { get; }

        public ReactiveCommand AddDataBaseCommand { get; }
        public ReactiveCommand RemoveDataBaseCommand { get; }
        public ReactiveCommand AddAnnotatorCommand { get; }
        public ReactiveCommand RemoveAnnotatorCommand { get; }
        public ReactiveCommand MoveUpAnnotatorCommand { get; }
        public ReactiveCommand MoveDownAnnotatorCommand { get; }

        private static readonly string DataBaseIDDuplicateErrorMessage = "DataBase name is duplicated.";
        private static readonly string AnnotatorIDDuplicateErrorMessage = "Annotation method name is duplicated.";

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;
        public IObservable<Unit> ObserveChanges { get; }

        private readonly Subject<Unit> decide;
        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }
        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ISettingViewModel? Next(ISettingViewModel selected) {
            decide.OnNext(Unit.Default);
            return null;
        }
    }
}