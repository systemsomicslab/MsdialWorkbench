using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;

namespace CompMs.App.Msdial.Model.Chart;

public class AxisItemModel<T>(string label, IAxisManager<T> axisManager, string graphLabel) : BindableBase {
    public IAxisManager<T> AxisManager { get; } = axisManager;
    public string Label { get; } = label;
    public string GraphLabel { get; } = graphLabel;
}
