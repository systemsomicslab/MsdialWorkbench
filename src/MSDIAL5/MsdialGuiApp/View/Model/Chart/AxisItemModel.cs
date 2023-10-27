using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class AxisItemModel<T> : BindableBase {
        public AxisItemModel(string label, IAxisManager<T> axisManager, string graphLabel) {
            AxisManager = axisManager;
            Label = label;
            GraphLabel = graphLabel;
        }

        public IAxisManager<T> AxisManager { get; }
        public string Label { get; }
        public string GraphLabel { get; }
    }
}
