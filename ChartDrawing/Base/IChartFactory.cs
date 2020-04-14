using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public interface IChartFactory
    {
        Rect ElementArea { get; set; }
        Transform TransformElement { get; set; }
        Drawing CreateChart(Size size);
        Vector Move(Vector vector);
        Rect UpdateRange(Rect rect);
        Rect Reset();
        void SizeChanged(Size size);
    }


    public class BackgroundChartFactory : IChartFactory
    {
        protected static readonly Brush graphBackground = Brushes.WhiteSmoke;
        protected static readonly Pen graphBorder = new Pen(Brushes.Black, 1);

        IDrawingElement BackgroundElement;

        public Rect ElementArea { get; set; }
        public Transform TransformElement { get; set; }

        public BackgroundChartFactory(Size size)
        {
            BackgroundElement = new AreaElement(new Rect(0, 0, 100, 100));
            TransformElement = new ScaleTransform(size.Width, size.Height);
            ElementArea = BackgroundElement.ElementArea;
        }

        public Drawing CreateChart(Size size)
        {
            var geometryDrawing = new GeometryDrawing
            {
                Geometry = BackgroundElement.GetGeometry(new Rect(0, 0, 100, 100), size),
                Brush = graphBackground,
                Pen = graphBorder
            };
            return geometryDrawing;
        }
        public Vector Move(Vector vector)
        {
            var inv = TransformElement.Inverse;
            if (inv == null) return new Vector(0, 0);
            Vector vec = (Vector)inv.Transform((Point)vector);
            ElementArea.Offset(vec);
            return vec;
        }
        public Rect UpdateRange(Rect rect) => ElementArea = rect;
        public Rect Reset() => ElementArea = new Rect(0, 0, 100, 100);
        public void SizeChanged(Size size) => TransformElement = new ScaleTransform(size.Width, size.Height);
    }

    public class ZoomRubberChartFactory : IChartFactory
    {
        protected static readonly Brush rubberForeground = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.5 };
        protected static readonly Pen rubberBorder = new Pen(Brushes.DarkGray, 1);

        IDrawingElement ZoomRubberElement;

        public Rect ElementArea { get; set; }
        public Transform TransformElement { get; set; } = Transform.Identity;

        public ZoomRubberChartFactory()
        {
            ZoomRubberElement = new AreaElement(new Rect(0, 0, 1, 1));
            ElementArea = ZoomRubberElement.ElementArea;
        }
        public Drawing CreateChart(Size size)
        {
            var geometryDrawing = new GeometryDrawing
            {
                Geometry = ZoomRubberElement.GetGeometry(new Rect(0, 0, 1, 1), size),
                Brush = rubberForeground,
                Pen = rubberBorder,
            };
            return geometryDrawing;
        }
        public Vector Move(Vector vector) { return new Vector(0, 0); }
        public Rect UpdateRange(Rect rect) { return new Rect(0, 0, 1, 1); }
        public Rect Reset() { return new Rect(0, 0, 1, 1); }
        public void SizeChanged(Size size) { }
    }
}
