using CompMs.Graphics.Core.Base;

namespace CompMs.App.Msdial.Model.Chart;

public sealed class AxisItemModelWithValue<T>(string label, IAxisManager<T> axisManager, string graphLabel) : AxisItemModel<T>(label, axisManager, graphLabel)
{
    public string ValueLabel { get; set; } = string.Empty;

    public double Value {
        get => _value;
        set => SetProperty(ref _value, value);
    }
    private double _value;
}
