using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;

namespace CompMs.App.Msdial.Model.Chart
{
    public class AxisItemModel : BindableBase
    {
        public AxisItemModel(IAxisManager<double> axisManager, string label) {
            AxisManager = axisManager;
            Label = label;
        }

        public IAxisManager<double> AxisManager { get; }

        public string Label { get; }
    }
}
