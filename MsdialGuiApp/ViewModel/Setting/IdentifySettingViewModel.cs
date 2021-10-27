using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class IdentifySettingViewModel : ViewModelBase
    {
        public IdentifySettingViewModel(IdentifySettingModel model, IAnnotatorSettingViewModelFactory annotatorFactory) {
            this.model = model;

            var selectedAnnotator = new Subject<IAnnotatorSettingViewModel>();
            var selectedDataBase = new Subject<IDataBaseSettingViewModel>();

            AnnotatorViewModels = this.model.AnnotatorModels.ToReadOnlyReactiveCollection(annotatorFactory.Create).AddTo(Disposables);
            var addedAnnotator = AnnotatorViewModels.ObserveAddChanged();
            var removedAnnotator = AnnotatorViewModels.ObserveRemoveChanged();
            AnnotatorViewModel = new[]
            {
                addedAnnotator,
                removedAnnotator.Select(avm => AnnotatorViewModels.LastOrDefault()),
                selectedDataBase.Where(dvm => !(dvm is null)).Select(dvm => AnnotatorViewModels.LastOrDefault(avm => avm.Model.DataBaseSettingModel == dvm.Model)),
            }.Merge().StartWith((IAnnotatorSettingViewModel)null)
            .ToReactiveProperty().AddTo(Disposables);
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
                selectedAnnotator.Where(avm => !(avm is null)).Select(avm => DataBaseViewModels.FirstOrDefault(dvm => dvm.Model == avm.Model.DataBaseSettingModel)),
            }.Merge().StartWith((DataBaseSettingViewModel)null)
            .ToReactiveProperty().AddTo(Disposables);
            DataBaseViewModel.Subscribe(selectedDataBase);

            // DataBase errors
            var dataBaseIDDuplicate = new[]
            {
                addedDataBase.ToUnit(),
                removedDataBase.ToUnit(),
                DataBaseViewModels.ObserveElementObservableProperty(vm => vm.DataBaseID).ToUnit(),
            }.Merge().StartWith(Unit.Default)
            .Select(_ => DataBaseViewModels.Select(vm => vm.DataBaseID.Value).Distinct().Count() != DataBaseViewModels.Count);
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
            }.Merge().StartWith(Unit.Default)
            .Select(_ => DataBaseViewModels.Any(vm => vm.ObserveHasErrors.Value));
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

            ObserveHasErrors = new[]
            {
                dataBasesDoesnotHaveError,
                annotatorsDoesnotHaveError,
            }.CombineLatestValuesAreAllTrue()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            // Commands
            var notifier = new BusyNotifier();
            AddDataBaseCommand = dataBasesDoesnotHaveError.ToReactiveCommand()
                .WithSubscribe(() => this.model.AddDataBaseZZZ())
                .AddTo(Disposables);
            var dbIsNotNull = DataBaseViewModel.Select(m => !(m is null));
            RemoveDataBaseCommand = dbIsNotNull
                .ToReactiveCommand()
                .WithSubscribe(() => this.model.RemoveDataBaseZZZ(DataBaseViewModel.Value.Model))
                .AddTo(Disposables);
            AddAnnotatorCommand = new[]{
                dbIsNotNull,
                DataBaseViewModel.Where(vm => !(vm is null)).Select(vm => vm.ObserveHasErrors).Switch().Inverse(),
                dataBasesDoesnotHaveError,
                annotatorsDoesnotHaveError,
            }.CombineLatestValuesAreAllTrue()
                .ToReactiveCommand()
                .WithSubscribe(() => this.model.AddAnnotatorZZZ(DataBaseViewModel.Value.Model))
                .AddTo(Disposables);
            var annotatorIsNotNull = AnnotatorViewModel.Select(m => !(m is null));
            RemoveAnnotatorCommand = annotatorIsNotNull
                .ToReactiveCommand()
                .WithSubscribe(() => this.model.RemoveAnnotatorZZZ(AnnotatorViewModel.Value.Model))
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
            .WithSubscribe(() => this.model.MoveUpAnnotatorZZZ(AnnotatorViewModel.Value.Model))
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
            .WithSubscribe(() => this.model.MoveDownAnnotatorZZZ(AnnotatorViewModel.Value.Model))
            .WithSubscribe(() => moveDownTrigger.OnNext(Unit.Default))
            .AddTo(Disposables);

            DataBaseViewModels.ToObservable()
                .Concat(addedDataBase)
                .Where(vm => !(vm is null))
                .SelectMany(vm => dataBasesDoesnotHaveError
                    .TakeUntil(removedDataBase.Where(x => x == vm))
                    .Where(x => x)
                    .Where(_ => DataBaseViewModel.Value == vm)
                    .Where(_ => AnnotatorViewModels.All(avm => avm.Model.DataBaseSettingModel != vm.Model))
                    .Take(1)
                    .SelectMany(_ => Observable.Interval(TimeSpan.FromMilliseconds(100)))
                    .Do(_ => this.model.AddAnnotatorZZZ(DataBaseViewModel.Value.Model))
                    .Retry(5)
                    .Take(1)
                    .Catch((Exception ex) => Observable.Empty<long>()))
                .Subscribe();
        }

        private readonly IdentifySettingModel model;

        public ReadOnlyReactiveCollection<IDataBaseSettingViewModel> DataBaseViewModels { get; }
        public ReactiveProperty<IDataBaseSettingViewModel> DataBaseViewModel { get; }

        public ReadOnlyReactiveCollection<IAnnotatorSettingViewModel> AnnotatorViewModels { get; }

        public ReactiveProperty<IAnnotatorSettingViewModel> AnnotatorViewModel { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        public ReactiveCommand AddDataBaseCommand { get; }
        public ReactiveCommand RemoveDataBaseCommand { get; }
        public ReactiveCommand AddAnnotatorCommand { get; }
        public ReactiveCommand RemoveAnnotatorCommand { get; }
        public ReactiveCommand MoveUpAnnotatorCommand { get; }
        public ReactiveCommand MoveDownAnnotatorCommand { get; }

        private static readonly string DataBaseIDDuplicateErrorMessage = "DataBase name is duplicated.";
        private static readonly string AnnotatorIDDuplicateErrorMessage = "Annotation method name is duplicated.";
    }
}