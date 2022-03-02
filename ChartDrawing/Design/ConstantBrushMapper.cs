using CompMs.Graphics.Base;
using System.Windows.Media;

namespace CompMs.Graphics.Design
{
    public class ConstantBrushMapper<T> : IBrushMapper<T>
    {
        private readonly Brush brush;

        public ConstantBrushMapper(Brush brush) {
            this.brush = brush;
        }

        public ConstantBrushMapper(Color color) {
            brush = new SolidColorBrush(color);
            brush.Freeze();
        }

        public Brush Map(T key) => brush;

        public Brush Map(object key) => brush;

        public ConstantBrushMapper<U> As<U>() => new ConstantBrushMapper<U>(brush);
    }
    
    public class ConstantBrushMapper : IBrushMapper
    {
        public Brush Brush { get; set; }

        public Brush Map(object key) => Brush;

        public ConstantBrushMapper<T> As<T>() => new ConstantBrushMapper<T>(Brush);
    }
}
