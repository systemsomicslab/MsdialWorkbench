using CompMs.App.Msdial.Model.Export;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class ExportMrmprobsViewModel : SettingDialogViewModel
    {
        private readonly ExportMrmprobsModel _model;
        private readonly AsyncReactiveCommand _exportCommand;

        public ExportMrmprobsViewModel(ExportMrmprobsModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            Copy = model.ObserveProperty(m => m.Copy).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            ExportFilePath = model.ToReactivePropertyAsSynchronized(m => m.ExportFilePath)
                .SetValidateNotifyError(f => !string.IsNullOrWhiteSpace(f) && Directory.GetParent(f).Exists ? null : "This folder does not exist")
                .AddTo(Disposables);
            ExportParameter = new MrmprobsExportParameterViewModel(model.ExportParameter).AddTo(Disposables);

            var ok = new[]
            {
                ExportParameter.ObserveHasErrors,
                Copy.Select(p => p ? Observable.Return(false) : ExportFilePath.ObserveHasErrors).Switch(),
            }.CombineLatestValuesAreAllFalse();

            _exportCommand = ok.ToAsyncReactiveCommand().WithSubscribe(() => model.ExportAsync(default)).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<bool> Copy { get; }

        public ReactiveProperty<string> ExportFilePath { get; }

        public MrmprobsExportParameterViewModel ExportParameter { get; }

        public IExportMrmprobsUsecase ExportUsecase => _model.ExportUsecase;

        public override ICommand FinishCommand => _exportCommand;
    }
}
