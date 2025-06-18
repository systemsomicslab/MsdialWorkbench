using CompMs.App.Msdial.Model.Setting;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Setting;

internal sealed class ProductIonSelectingViewModel : SettingDialogViewModel
{
    private readonly ProductIonSelectingModel _model;

    public ProductIonSelectingViewModel(ProductIonSelectingModel model) {
        _model = model;

        SettingIons = [..model.SettingIons];
        ClearSettingsCommand = new ReactiveCommand()
            .WithSubscribe(() => SettingIons.Clear())
            .AddTo(Disposables);

        FinishCommand = new ReactiveCommand().WithSubscribe(ReflectSettingIons).AddTo(Disposables);
    }

    public ObservableCollection<SettingIon> SettingIons { get; }

    public ReactiveCommand ClearSettingsCommand { get; }

    private void ReflectSettingIons() {
        _model.SettingIons = [.. SettingIons];
    }

    public override ICommand? FinishCommand { get; }
}
