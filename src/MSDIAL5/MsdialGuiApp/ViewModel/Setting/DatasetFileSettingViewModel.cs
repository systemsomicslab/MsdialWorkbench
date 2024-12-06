using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal class DatasetFileSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public DatasetFileSettingViewModel(DatasetFileSettingModel model, IObservable<bool> isEnabled) {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            IsReadOnly = model.IsReadOnly;

            AnalysisFilePropertyCollection = Model.FileModels.AnalysisFiles
                .ToReadOnlyReactiveCollection(v => new AnalysisFileBeanViewModel(v))
                .AddTo(Disposables);

            var analysisFileHasError = new[]
            {
                AnalysisFilePropertyCollection.ObserveAddChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveRemoveChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveResetChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveElementObservableProperty(vm => vm.HasErrors).ToUnit(),
            }.Merge()
            .Select(_ => AnalysisFilePropertyCollection.Any(vm => vm.HasErrors.Value));

            var analysisFileNameDuplicate = new[]
            {
                AnalysisFilePropertyCollection.ObserveAddChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveRemoveChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveResetChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveElementObservableProperty(vm => vm.AnalysisFileName).ToUnit(),
            }.Merge()
            .Select(_ => AnalysisFilePropertyCollection.Select(vm => vm.AnalysisFileName.Value).Distinct().Count() != AnalysisFilePropertyCollection.Count);
            analysisFileNameDuplicate.Subscribe(hasError => {
                if (hasError) {
                    AddError(nameof(AnalysisFilePropertyCollection), FileNameDuplicateErrorMessage);
                }
                else {
                    RemoveError(nameof(AnalysisFilePropertyCollection), FileNameDuplicateErrorMessage);
                }
            }).AddTo(Disposables);

            var analysisFileIsEmpty = AnalysisFilePropertyCollection.CollectionChangedAsObservable().ToUnit()
                .StartWith(Unit.Default)
                .Select(_ => AnalysisFilePropertyCollection.Count <= 0);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            SelectedAcquisitionType = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedAcquisitionType).AddTo(Disposables);
            SetSelectedAcquisitionTypeCommand = new[]
            {
                Observable.Return(!model.IsReadOnly),
                SelectedAcquisitionType.Select(x => Enum.IsDefined(typeof(AcquisitionType), x)),
            }.CombineLatestValuesAreAllTrue()
                .ToReactiveCommand()
                .WithSubscribe(SetSelectedAquisitionTypeToAll)
                .AddTo(Disposables);

            SupportingMessage = model.ObserveProperty(m => m.SupportingMessage)
                .ToReadOnlyReactivePropertySlim(initialValue: string.Empty)
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                analysisFileHasError,
                analysisFileNameDuplicate,
                analysisFileIsEmpty,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim(true)
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                AnalysisFilePropertyCollection.CollectionChangedAsObservable().ToUnit(),
                AnalysisFilePropertyCollection.ObserveElementObservableProperty(vm => vm.ObserveChanges).ToUnit(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            var change = ObserveChanges.TakeFirstAfterEach(decide);
            ObserveChangeAfterDecision = new[]
            {
                change.ToConstant(true),
                decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            MessageBroker.Default
                .Subscribe<SelectedAnalysisFileQuery>(ImportFiles)
                .AddTo(Disposables);
        }

        public DatasetFileSettingModel Model { get; }

        public ReactivePropertySlim<AcquisitionType> SelectedAcquisitionType { get; }
        public ReactiveCommand SetSelectedAcquisitionTypeCommand { get; }

        public ReadOnlyReactivePropertySlim<string> SupportingMessage { get; }

        public bool IsReadOnly { get; }

        private readonly Subject<Unit> decide;

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }

        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public IObservable<Unit> ObserveChanges { get; }

        public ReadOnlyReactiveCollection<AnalysisFileBeanViewModel> AnalysisFilePropertyCollection { get; }

        private void ImportFiles(SelectedAnalysisFileQuery query) {
            var files = query.AnalysisFilesPath;
            if (files.IsEmptyOrNull()) {
                return;
            }

            if (files.Length > 0) {
                Model.SetFiles(files);
            }
        }

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public ISettingViewModel? Next(ISettingViewModel selected) {
            foreach (var file in AnalysisFilePropertyCollection) {
                file.Commit();
            }
            decide.OnNext(Unit.Default);
            return null;
        }

        private static readonly string FileNameDuplicateErrorMessage = "File name duplicated.";

        private void SetSelectedAquisitionTypeToAll() {
            if (IsReadOnly) {
                return;
            }
            foreach (var file in AnalysisFilePropertyCollection) {
                file.AcquisitionType.Value = SelectedAcquisitionType.Value;
            }
        }
    }

    public class SelectedAnalysisFileQuery
    {
        public SelectedAnalysisFileQuery(params string[] analysisFilesPath) {
            AnalysisFilesPath = analysisFilesPath;
        }

        public string[] AnalysisFilesPath { get; }
    }
}
