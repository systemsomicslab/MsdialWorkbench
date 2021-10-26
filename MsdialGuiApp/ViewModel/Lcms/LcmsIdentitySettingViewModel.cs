using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    public class LcmsIdentitySettingViewModel : ViewModelBase
    {
        private readonly LcmsIdentitySettingModel model;
        private readonly DataBaseSettingViewModelFactory dataBaseFactory;
        private readonly LcmsAnnotatorSettingViewModelFactory annotatorFactory;

        private static readonly string DataBaseIDDuplicateErrorMessage = "DataBase name is duplicated.";
        private static readonly string AnnotatorIDDuplicateErrorMessage = "Annotation method name is duplicated.";

        public LcmsIdentitySettingViewModel(LcmsIdentitySettingModel model) {
            this.model = model;
            dataBaseFactory = new DataBaseSettingViewModelFactory();
            annotatorFactory = new LcmsAnnotatorSettingViewModelFactory();

            DataBaseViewModels = this.model.DataBaseModels.ToReadOnlyReactiveCollection(dataBaseFactory.Create).AddTo(Disposables);
            AnnotatorViewModels = this.model.AnnotatorModels.ToReadOnlyReactiveCollection(annotatorFactory.Create).AddTo(Disposables);
            DataBaseViewModel = this.model.ToReactivePropertySlimAsSynchronized(
                m => m.DataBaseModel,
                dbm => DataBaseViewModels.FirstOrDefault(vm => vm.Model == dbm),
                dbvm => dbvm?.Model)
                .AddTo(Disposables);
            AnnotatorViewModel = this.model.ToReactivePropertySlimAsSynchronized(
                m => m.AnnotatorModel,
                am => AnnotatorViewModels.FirstOrDefault(vm => vm.Model == am),
                avm => avm?.Model)
                .AddTo(Disposables);
            SelectedViewModel = new IObservable<object>[]
            {
                DataBaseViewModel,
                AnnotatorViewModel,
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            IsDataBaseExpanded = new[]
            {
                DataBaseViewModel.Select(_ => true),
                AnnotatorViewModel.Select(_ => false),
            }.Merge()
            .ToReactiveProperty()
            .AddTo(Disposables);
            IsAnnotatorExpanded = new[]
            {
                DataBaseViewModel.Select(_ => false),
                AnnotatorViewModel.Select(_ => true),
            }.Merge()
            .ToReactiveProperty()
            .AddTo(Disposables);

            var addedDataBase = DataBaseViewModels.ObserveAddChanged();
            addedDataBase.Subscribe(vm => DataBaseViewModel.Value = vm).AddTo(Disposables);
            var removedDataBase = DataBaseViewModels.ObserveRemoveChanged();
            var dataBaseIDDuplicate = new[]
            {
                addedDataBase.ToUnit(),
                removedDataBase.ToUnit(),
                DataBaseViewModels.ObserveElementObservableProperty(vm => vm.DataBaseID).ToUnit(),
            }.Merge()
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
            }.Merge()
            .Select(_ => DataBaseViewModels.Any(vm => vm.ObserveHasErrors.Value));

            var addedAnnotator = AnnotatorViewModels.ObserveAddChanged();
            addedAnnotator.Subscribe(vm => AnnotatorViewModel.Value = vm).AddTo(Disposables);
            var removedAnnotator = AnnotatorViewModels.ObserveRemoveChanged();
            var annotatorIDDuplicate = new[]
            {
                addedAnnotator.ToUnit(),
                removedAnnotator.ToUnit(),
                AnnotatorViewModels.ObserveElementObservableProperty(vm => vm.AnnotatorID).ToUnit(),
            }.Merge()
            .Select(_ => Observable.Defer(() => Observable.Return(
                AnnotatorViewModels.Select(vm => vm.AnnotatorID.Value).Distinct().Count() != AnnotatorViewModels.Count)))
            .Switch();
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
            }.Merge()
            .Select(_ => Observable.Defer(() =>
                AnnotatorViewModels.Select(vm => vm.ObserveHasErrors)
                    .CombineLatestValuesAreAllFalse()
                    .Inverse()))
            .Switch();

            var dataBasesDoesnotHaveError = new[]
            {
                dataBaseIDDuplicate.Do(_ => Console.WriteLine("dataBaseIDDuplicate fire!")),
                dataBaseHasError.Do(_ => Console.WriteLine("dataBaseHasError fire!")),
                this.ErrorsChangedAsObservable().Select(_ => ContainsError(nameof(DataBaseViewModels))).StartWith(false).Do(_ => Console.WriteLine("ErrorChanged fire!")),
            }.CombineLatestValuesAreAllFalse().Do(_ => Console.WriteLine("dataBasesDoesnotHaveError fire!"));
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

            var dbIsNotNull = DataBaseViewModel.Select(m => !(m is null));
            var annotatorIsNotNull = AnnotatorViewModel.Select(m => !(m is null));
            AddDataBaseCommand = dataBasesDoesnotHaveError.ToReactiveCommand()
                .WithSubscribe(this.model.AddDataBase)
                .AddTo(Disposables);
            RemoveDataBaseCommand = dbIsNotNull
                .ToReactiveCommand()
                .WithSubscribe(this.model.RemoveDataBase)
                .AddTo(Disposables);
            
            AddAnnotatorCommand = new[]{
                dbIsNotNull,
                DataBaseViewModel.Where(vm => !(vm is null)).Select(vm => vm.ObserveHasErrors).Switch().Inverse(),
                dataBasesDoesnotHaveError,
                annotatorsDoesnotHaveError,
            }.CombineLatestValuesAreAllTrue()
                .ToReactiveCommand()
                .WithSubscribe(this.model.AddAnnotator)
                .AddTo(Disposables);

            DataBaseViewModels.ToObservable()
                .Concat(addedDataBase)
                .Where(vm => !(vm is null))
                .SelectMany(vm => dataBasesDoesnotHaveError
                    .Where(x => x)
                    .Where(_ => DataBaseViewModel.Value == vm)
                    .Where(_ => AnnotatorViewModels.All(avm => avm.Model.DataBaseSettingModel != vm.Model))
                    .TakeUntil(removedDataBase.Where(x => x == vm))
                    .Take(1)
                    .SelectMany(_ => Observable.Interval(TimeSpan.FromMilliseconds(100)))
                    .Do(_ => this.model.AddAnnotator())
                    .Retry(5)
                    .Take(1)
                    .Catch((Exception ex) => Observable.Empty<long>()))
                .Subscribe();

            RemoveAnnotatorCommand = annotatorIsNotNull
                .ToReactiveCommand()
                .WithSubscribe(this.model.RemoveAnnotator)
                .AddTo(Disposables);
            var moveUpTrigger = new Subject<Unit>();
            var moveDownTrigger = new Subject<Unit>();
            MoveUpAnnotatorCommand = new[]
            {
                annotatorIsNotNull,
                annotatorsDoesnotHaveError,
                moveDownTrigger.Merge(AnnotatorViewModel.ToUnit())
                    .Select(_ => AnnotatorViewModels.FirstOrDefault() != AnnotatorViewModel.Value),
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .WithSubscribe(this.model.MoveUpAnnotator)
            .WithSubscribe(() => moveUpTrigger.OnNext(Unit.Default))
            .AddTo(Disposables);
            MoveDownAnnotatorCommand = new[]
            {
                annotatorIsNotNull,
                annotatorsDoesnotHaveError,
                moveUpTrigger.Merge(AnnotatorViewModel.ToUnit())
                    .Select(_ => AnnotatorViewModels.LastOrDefault() != AnnotatorViewModel.Value),
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .WithSubscribe(this.model.MoveDownAnnotator)
            .WithSubscribe(() => moveDownTrigger.OnNext(Unit.Default))
            .AddTo(Disposables);

            var hasAnnotator = DataBaseViewModel.Select(vm => vm is null || AnnotatorViewModels.All(annotator => annotator.Model.DataBaseSettingModel != vm.Model));
            IsSourceTypeEnabled = new[]
            {
                AnnotatorViewModels.ObserveAddChanged().ToUnit(),
                AnnotatorViewModels.ObserveRemoveChanged().ToUnit(),
                DataBaseViewModel.ToUnit(),
            }.Merge()
            .Select(_ => DataBaseViewModel.Value is null || AnnotatorViewModels.All(annotator => annotator.Model.DataBaseSettingModel != DataBaseViewModel.Value.Model))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<IDataBaseSettingViewModel> DataBaseViewModels { get; }

        public ReadOnlyReactiveCollection<ILcmsAnnotatorSettingViewModel> AnnotatorViewModels { get; }

        public ReactivePropertySlim<IDataBaseSettingViewModel> DataBaseViewModel { get; }

        public ReactivePropertySlim<ILcmsAnnotatorSettingViewModel> AnnotatorViewModel { get; }

        public ReadOnlyReactivePropertySlim<object> SelectedViewModel { get; }
        public ReactiveProperty<bool> IsDataBaseExpanded { get; }
        public ReactiveProperty<bool> IsAnnotatorExpanded { get; }
        public ReadOnlyReactivePropertySlim<bool> IsSourceTypeEnabled { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReactiveCommand AddDataBaseCommand { get; }
        public ReactiveCommand RemoveDataBaseCommand { get; }
        public ReactiveCommand AddAnnotatorCommand { get; }
        public ReactiveCommand RemoveAnnotatorCommand { get; }
        public ReactiveCommand MoveUpAnnotatorCommand { get; }
        public ReactiveCommand MoveDownAnnotatorCommand { get; }
    }
}
