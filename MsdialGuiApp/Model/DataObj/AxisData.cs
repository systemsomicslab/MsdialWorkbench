using CompMs.Graphics.Core.Base;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class AxisData
    {
        public AxisData(IAxisManager axis, string property, string title) {
            Axis = axis;
            Property = property;
            Title = title;
        }

        public IAxisManager Axis { get; }

        public string Property { get; }

        public string Title { get; }
    }
}
