using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Setting;

internal sealed class MolecularNetworkingExportSettingViewModel : ViewModelBase {
    private readonly MolecularNetworkingSettingModel _model;

    public MolecularNetworkingExportSettingViewModel(MolecularNetworkingSettingModel model) {
        _model = model ?? throw new ArgumentNullException(nameof(model));

        SettingViewModel = new MolecularNetworkingSettingViewModel(model).AddTo(Disposables);

        ExportFolderPath = model.ToReactivePropertyAsSynchronized(m => m.ExportFolderPath, ignoreValidationErrorValue: true)
            .SetValidateAttribute(() => ExportFolderPath).AddTo(Disposables);
        ValidateProperty(nameof(ExportFolderPath), ExportFolderPath);

        ObserveHasErrors = new[]
        {
            SettingViewModel.ObserveHasErrors,
            ExportFolderPath.ObserveHasErrors,
        }.CombineLatestValuesAreAllFalse()
        .Inverse()
        .ToReadOnlyReactivePropertySlim()
        .AddTo(Disposables);

        MolecularNetworkingAsyncCommand = ObserveHasErrors.Inverse().ToAsyncReactiveCommand()
            .WithSubscribe(model.RunMolecularNetworkingAsync).AddTo(Disposables);
    }

    public MolecularNetworkingSettingViewModel SettingViewModel { get; }

    [Required(ErrorMessage = "Please enter the folder which the results will be exported.")]
    [PathExists(ErrorMessage = "This folder does not exist.", IsDirectory = true)]
    public ReactiveProperty<string> ExportFolderPath { get; }

    public AsyncReactiveCommand MolecularNetworkingAsyncCommand { get; }

    public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
}
