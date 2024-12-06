using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Setting;

internal sealed class MolecularNetworkingSendingToCytoscapeJsSettingViewModel : ViewModelBase {
    private readonly MolecularNetworkingSettingModel _model;

    public MolecularNetworkingSendingToCytoscapeJsSettingViewModel(MolecularNetworkingSettingModel model) {
        _model = model ?? throw new ArgumentNullException(nameof(model));

        SettingViewModel = new MolecularNetworkingSettingViewModel(model).AddTo(Disposables);

        ObserveHasErrors = new[]
        {
            SettingViewModel.ObserveHasErrors,
        }.CombineLatestValuesAreAllFalse()
        .Inverse()
        .ToReadOnlyReactivePropertySlim()
        .AddTo(Disposables);

        MolecularNetworkingAsyncCommand = ObserveHasErrors.Inverse().ToAsyncReactiveCommand()
            .WithSubscribe(model.SendMolecularNetworkingDataToCytoscapeJsAsync).AddTo(Disposables);
    }

    public MolecularNetworkingSettingViewModel SettingViewModel { get; }

    public AsyncReactiveCommand MolecularNetworkingAsyncCommand { get; }

    public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
}
