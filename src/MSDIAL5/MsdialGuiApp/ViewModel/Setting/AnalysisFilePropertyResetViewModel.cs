using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class AnalysisFilePropertyResetViewModel : SettingDialogViewModel
    {
        private static readonly string FILE_NAME_DUPLICATE_ERROR_MESSAGE = "File name duplicated.";

        private readonly AnalysisFilePropertyResetModel _model;

        public AnalysisFilePropertyResetViewModel(AnalysisFilePropertyResetModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            AnalysisFilePropertyCollection = _model.AnalysisFileModelCollection.AnalysisFiles
                .ToReadOnlyReactiveCollection(v => new AnalysisFileBeanViewModel(v))
                .AddTo(Disposables);

            var analysisFileHasError = new[]
            {
                AnalysisFilePropertyCollection.ObserveAddChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveRemoveChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveElementObservableProperty(vm => vm.HasErrors).ToUnit(),
            }.Merge()
            .Select(_ => AnalysisFilePropertyCollection.Any(vm => vm.HasErrors.Value));

            var analysisFileNameDuplicate = new[]
            {
                AnalysisFilePropertyCollection.ObserveAddChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveRemoveChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveElementObservableProperty(vm => vm.AnalysisFileName).ToUnit(),
            }.Merge()
            .Select(_ => AnalysisFilePropertyCollection.Select(vm => vm.AnalysisFileName.Value).Distinct().Count() != AnalysisFilePropertyCollection.Count);
            analysisFileNameDuplicate.Subscribe(hasError => {
                if (hasError) {
                    AddError(nameof(AnalysisFilePropertyCollection), FILE_NAME_DUPLICATE_ERROR_MESSAGE);
                }
                else {
                    RemoveError(nameof(AnalysisFilePropertyCollection), FILE_NAME_DUPLICATE_ERROR_MESSAGE);
                }
            }).AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                analysisFileHasError,
                analysisFileNameDuplicate,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim(true)
            .AddTo(Disposables);

            ContinueProcessCommand = ObserveHasErrors.Inverse()
                .ToReactiveCommand()
                .WithSubscribe(Commit)
                .AddTo(Disposables);
        }
        public ReadOnlyReactiveCollection<AnalysisFileBeanViewModel> AnalysisFilePropertyCollection { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReactiveCommand ContinueProcessCommand { get; }
        public override ICommand FinishCommand => ContinueProcessCommand;

        public void Commit() {
            foreach (var file in AnalysisFilePropertyCollection) {
                file.Commit();
            }
        }
    }
}
