using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Setting;

internal sealed class ProductIonSelectingViewModel : SettingDialogViewModel
{
    private readonly ProductIonSettingModel _model;

    public ProductIonSelectingViewModel(ProductIonSettingModel model) {
        _model = model;
        SettingIons = [..model.SettingIons];

        FinishCommand = new ReactiveCommand().WithSubscribe(ReflectSettingIons).AddTo(Disposables);
    }

    public ObservableCollection<SettingIon> SettingIons { get; }

    private void ReflectSettingIons() {
        _model.SettingIons = [.. SettingIons];
    }

    public override ICommand? FinishCommand { get; }
}
