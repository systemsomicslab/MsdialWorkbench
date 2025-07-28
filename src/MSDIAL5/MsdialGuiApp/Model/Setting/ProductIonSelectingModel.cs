using CompMs.App.Msdial.Model.DataObj;
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

    public double LeftRt {
        get => _leftRt;
        set => SetProperty(ref _leftRt, value);
    }
    private double _leftRt = 0d;

    public double RightRt {
        get => _rightRt;
        set => SetProperty(ref _rightRt, value);
    }
    private double _rightRt = 0d;

    public IObservable<(double Left, double Right)> GetRtRangeAsObservable() {
        return new[] { this.ObserveProperty(m => m.LeftRt), this.ObserveProperty(m => m.RightRt) }
            .CombineLatest(range => (Left: range[0], Right: range[1]));
    }

    public void SelectPeak(ChromatogramPeakFeatureModel peak) {
        LeftRt = peak.ChromXLeftValue;
        RightRt = peak.ChromXRightValue;
    }

    public IObservable<double[]> GetRequiredProductIonsAsObservable() {
        return this.ObserveProperty(m => m.SettingIons)
            .Select(ions => ions is not null ? ions.Select(ion => ion.Mz).ToArray() : []);
    }
}

internal sealed class SettingIon : ValidatableBase
{
    public double Mz { get; set; } = 0d;
}

