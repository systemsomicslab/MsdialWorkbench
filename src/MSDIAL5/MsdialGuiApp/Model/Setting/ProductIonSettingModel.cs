using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting;

internal sealed class ProductIonSettingModel : BindableBase
{
    public SettingIon[]? SettingIons {
        get => _settingIons;
        set => SetProperty(ref _settingIons, value);
    }
    private SettingIon[]? _settingIons = [];
}

internal sealed class SettingIon : ValidatableBase
{
    public double Mz { get; set; } = 0d;
}

