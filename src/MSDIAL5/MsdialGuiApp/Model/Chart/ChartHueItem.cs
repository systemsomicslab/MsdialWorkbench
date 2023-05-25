using CompMs.Graphics.Base;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class ChartHueItem
    {
        public ChartHueItem(string property, IBrushMapper brush) {
            Property = property;
            Brush = brush;
        }

        public string Property { get; }
        public IBrushMapper Brush { get; }
    }
}
