using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class AxisItemModel : AxisItemModel<double>
    {
        public AxisItemModel(string label, IAxisManager<double> axisManager, string property, string graphLabel) : base(label, axisManager, property, graphLabel) {
            
        }

        public AxisItemModel(string label, IAxisManager<double> axisManager, string property) : base(label, axisManager, property, string.Empty) {
            
        }
    }

    public class AxisItemModel<T> : BindableBase {
        public AxisItemModel(string label, IAxisManager<T> axisManager, string property, string graphLabel) {
            AxisManager = axisManager;
            Property = property;
            Label = label;
            GraphLabel = graphLabel;
        }

        public IAxisManager<T> AxisManager { get; }
        public string Property { get; }
        public string Label { get; }
        public string GraphLabel { get; }
    }
}
