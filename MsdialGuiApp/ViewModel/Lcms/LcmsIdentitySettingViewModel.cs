using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    public class LcmsIdentitySettingViewModel : ViewModelBase
    {
        private readonly LcmsIdentitySettingModel model;
        private readonly LcmsAnnotatorSettingViewModelFactory annotatorFactory;

        public LcmsIdentitySettingViewModel(LcmsIdentitySettingModel model) {
            this.model = model;
            annotatorFactory = new LcmsAnnotatorSettingViewModelFactory();

            DataBaseViewModels = this.model.DataBaseModels.ToReadOnlyReactiveCollection(m => new DataBaseSettingViewModel(m)).AddTo(Disposables);
            AnnotatorViewModels = this.model.AnnotatorModels.ToReadOnlyReactiveCollection(m => annotatorFactory.Create(m)).AddTo(Disposables);
            DataBaseViewModel = new ReactivePropertySlim<DataBaseSettingViewModel>(null).AddTo(Disposables);
            DataBaseViewModel.Subscribe(vm => this.model.DataBaseModel = vm.Model).AddTo(Disposables);
            AnnotatorViewModel = new ReactivePropertySlim<ILcmsAnnotatorSettingViewModel>(null).AddTo(Disposables);
            AnnotatorViewModel.Subscribe(vm => this.model.AnnotatorModel = vm.Model).AddTo(Disposables);

            var addedDataBase = DataBaseViewModels.ObserveAddChanged();
            var removedDataBase = DataBaseViewModels.ObserveRemoveChanged();
            var dataBaseIDDuplicate = new[]
            {
                addedDataBase.ToUnit(),
                removedDataBase.ToUnit(),
                DataBaseViewModels.ObserveElementObservableProperty(vm => vm.DataBaseID).ToUnit(),
            }.Merge()
            .SelectMany(_ => Observable.Defer(() => Observable.Return(
                DataBaseViewModels.Select(vm => vm.DataBaseID.Value).Distinct().Count() != DataBaseViewModels.Count)));
            var dataBaseHasError = new[]
            {
                addedDataBase.ToUnit(),
                removedDataBase.ToUnit(),
                DataBaseViewModels.ObserveElementObservableProperty(vm => vm.ObserveHasErrors).ToUnit(),
            }.Merge()
            .SelectMany(_ => Observable.Defer(() =>
                DataBaseViewModels.Select(vm => vm.ObserveHasErrors)
                    .CombineLatestValuesAreAllFalse()
                    .Inverse()));

            var addedAnnotator = AnnotatorViewModels.ObserveAddChanged();
            var removedAnnotator = AnnotatorViewModels.ObserveRemoveChanged();
            var annotatorIDDuplicate = new[]
            {
                addedAnnotator.ToUnit(),
                removedAnnotator.ToUnit(),
                AnnotatorViewModels.ObserveElementObservableProperty(vm => vm.AnnotatorID).ToUnit(),
            }.Merge()
            .SelectMany(_ => Observable.Defer(() => Observable.Return(
                AnnotatorViewModels.Select(vm => vm.AnnotatorID.Value).Distinct().Count() != AnnotatorViewModels.Count)));
            var annotatorHasError = new[]
            {
                addedAnnotator.ToUnit(),
                removedAnnotator.ToUnit(),
                AnnotatorViewModels.ObserveElementObservableProperty(vm => vm.ObserveHasErrors).ToUnit(),
            }.Merge()
            .SelectMany(_ => Observable.Defer(() =>
                AnnotatorViewModels.Select(vm => vm.ObserveHasErrors)
                    .CombineLatestValuesAreAllFalse()
                    .Inverse()));

            ObserveHasErrors = new[]
            {
                dataBaseIDDuplicate,
                dataBaseHasError,
                annotatorIDDuplicate,
                annotatorHasError,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            var dbIsNotNull = DataBaseViewModel.Select(m => !(m is null));
            var annotatorIsNotNull = AnnotatorViewModel.Select(m => !(m is null));
            AddDataBaseCommand = new ReactiveCommand()
                .WithSubscribe(this.model.AddDataBase)
                .AddTo(Disposables);
            RemoveDataBaseCommand = dbIsNotNull
                .ToReactiveCommand()
                .WithSubscribe(this.model.RemoveDataBase)
                .AddTo(Disposables);
            AddAnnotatorCommand = new[]{
                dbIsNotNull.Inverse(),
                DataBaseViewModel.SelectMany(vm => vm.ObserveHasErrors)
            }.CombineLatestValuesAreAllFalse()
                .ToReactiveCommand()
                .WithSubscribe(this.model.AddAnnotator)
                .AddTo(Disposables);
            RemoveAnnotatorCommand = annotatorIsNotNull
                .ToReactiveCommand()
                .WithSubscribe(this.model.RemoveAnnotator)
                .AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<DataBaseSettingViewModel> DataBaseViewModels { get; }

        public ReadOnlyReactiveCollection<ILcmsAnnotatorSettingViewModel> AnnotatorViewModels { get; }

        public ReactivePropertySlim<DataBaseSettingViewModel> DataBaseViewModel { get; }

        public ReactivePropertySlim<ILcmsAnnotatorSettingViewModel> AnnotatorViewModel { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReactiveCommand AddDataBaseCommand { get; }
        public ReactiveCommand RemoveDataBaseCommand { get; }
        public ReactiveCommand AddAnnotatorCommand { get; }
        public ReactiveCommand RemoveAnnotatorCommand { get; }
    }
}
