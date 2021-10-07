using CompMs.Graphics.Base;
using System.Windows.Media;

namespace CompMs.Graphics.Design
{
    public class ConstantBrushMapper : IBrushMapper
    {
        private readonly Brush brush;

        public ConstantBrushMapper(Brush brush) {
            this.brush = brush;
        }

        public ConstantBrushMapper(Color color) {
            brush = new SolidColorBrush(color);
            brush.Freeze();
        }

        public Brush Map(object key) {
            return brush;
        }
    }
}
