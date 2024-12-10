using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Setting;

internal sealed class MolecularNetworkingSendingToCytoscapeJsSettingViewModel : ViewModelBase {
    private readonly MolecularNetworkingSettingModel _model;

    public MolecularNetworkingSendingToCytoscapeJsSettingViewModel(MolecularNetworkingSettingViewModel viewmodel, MolecularNetworkingSettingModel model) {
        _model = model ?? throw new ArgumentNullException(nameof(model));

        SettingViewModel = viewmodel;

        CytoscapeApiUrl = model.ToReactivePropertySlimAsSynchronized(m => m.CytoscapeUrl).AddTo(Disposables);
        ViewType = model.ToReactivePropertySlimAsSynchronized(m => m.NetworkPresentationType).AddTo(Disposables);

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

    public ReactivePropertySlim<NetworkVisualizationType> ViewType { get; }
    public ReactivePropertySlim<string> CytoscapeApiUrl { get; }

    public MolecularNetworkingSettingViewModel SettingViewModel { get; }

    public AsyncReactiveCommand MolecularNetworkingAsyncCommand { get; }

    public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
}
