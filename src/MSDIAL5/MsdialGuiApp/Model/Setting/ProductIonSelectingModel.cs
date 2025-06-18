using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Setting;

internal sealed class ProductIonSelectingModel : DisposableModelBase
{
    public SettingIon[]? SettingIons {
        get => _settingIons;
        set {
            SetProperty(ref _settingIons, value);
        }
    }
    private SettingIon[]? _settingIons = [];

    public IObservable<double[]> GetRequiredProductIonsAsObservable() {
        return this.ObserveProperty(m => m.SettingIons)
            .Select(ions => ions is not null ? ions.Select(ion => ion.Mz).ToArray() : []);
    }
}

internal sealed class SettingIon : ValidatableBase
{
    public double Mz { get; set; } = 0d;
}

