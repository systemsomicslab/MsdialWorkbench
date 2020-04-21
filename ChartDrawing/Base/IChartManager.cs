using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public interface IChartManager
    {
        // Rect ElementArea { get; set; }
        Rect ChartArea { get; }
        // Transform TransformElement { get; set; }
        // Drawing CreateChart(Size size);
        Drawing CreateChart(Rect rect, Size size);
        // Vector Move(Vector vector);
        // Rect UpdateRange(Rect rect);
        // Rect Reset();
        // void SizeChanged(Size size);
        Point Translate(Point point, Rect area, Size size);
        Vector Translate(Vector vector, Rect area, Size size);
        Rect Translate(Rect rect, Rect area, Size size);
    }


    public class BackgroundManager : IChartManager
    {
        public Brush graphBackground { get; }
        public Pen graphBorder { get; }

        IDrawingElement BackgroundElement;

        public Rect ChartArea { get; } = new Rect(0, 0, 100, 100);

        // public Rect ElementArea { get; set; }
        // public Transform TransformElement { get; set; }

        public BackgroundManager(): this(Brushes.WhiteSmoke, new Pen(Brushes.Black, 1)) { }
        public BackgroundManager(Brush brush, Pen border)
        {
            graphBackground = brush;
            graphBorder = border;
            BackgroundElement = new AreaElement(ChartArea);
            // TransformElement = new ScaleTransform(ChartArea.Width / size.Width, ChartArea.Height / size.Height);
            // ElementArea = BackgroundElement.ElementArea;
        }

        public Drawing CreateChart(Rect _, Size size)
        {
            var geometryDrawing = new GeometryDrawing
            {
                Geometry = BackgroundElement.GetGeometry(ChartArea, size),
                Brush = graphBackground,
                Pen = graphBorder
            };
            return geometryDrawing;
        }

        public Point Translate(Point point, Rect area, Size size)
        {
            return new Point(point.X / size.Width * area.Width + area.X,
                             point.Y / size.Height * area.Height + area.Y);
        }
        public Vector Translate(Vector vector, Rect area, Size size)
        {
            return new Vector(vector.X / size.Width * area.Width + area.X,
                              vector.Y / size.Height * area.Height + area.Y);
        }
        public Rect Translate(Rect rect, Rect area, Size size)
        {
            return new Rect(Translate(rect.TopLeft, area, size),
                            Translate(rect.BottomRight, area, size));
        }

        /*
        public Vector Move(Vector vector)
        {
            Vector vec = (Vector)TransformElement.Transform((Point)vector);
            ElementArea.Offset(vec);
            return vec;
        }
        public Rect UpdateRange(Rect rect) => ElementArea = rect;
        public Rect Reset() => ElementArea = ChartArea;
        public void SizeChanged(Size size)
            => TransformElement = new ScaleTransform(ChartArea.Width / size.Width, ChartArea.Height / size.Height);
        */
    }

    public class ZoomRubberManager : IChartManager
    {
        protected static readonly Brush rubberForeground = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.5 };
        protected static readonly Pen rubberBorder = new Pen(Brushes.DarkGray, 1);

        IDrawingElement ZoomRubberElement;

        // public Rect ElementArea { get; set; }
        // public Transform TransformElement { get; set; } = Transform.Identity;

        public Rect ChartArea { get; } = new Rect(0, 0, 1, 1);

        public ZoomRubberManager()
        {
            ZoomRubberElement = new AreaElement(ChartArea);
            // ElementArea = ZoomRubberElement.ElementArea;
        }
        public Drawing CreateChart(Rect _, Size size)
        {
            var geometryDrawing = new GeometryDrawing
            {
                Geometry = ZoomRubberElement.GetGeometry(new Rect(0, 0, 1, 1), size),
                Brush = rubberForeground,
                Pen = rubberBorder,
            };
            return geometryDrawing;
        }

        public Point Translate(Point point, Rect area, Size size)
        {
            return new Point(point.X / size.Width * area.Width + area.X,
                             point.Y / size.Height * area.Height + area.Y);
        }
        public Vector Translate(Vector vector, Rect area, Size size)
        {
            return new Vector(vector.X / size.Width * area.Width + area.X,
                              vector.Y / size.Height * area.Height + area.Y);
        }
        public Rect Translate(Rect rect, Rect area, Size size)
        {
            return new Rect(Translate(rect.TopLeft, area, size),
                            Translate(rect.BottomRight, area, size));
        }

        /*
        public Vector Move(Vector vector) { return new Vector(0, 0); }
        public Rect UpdateRange(Rect rect) { return new Rect(0, 0, 1, 1); }
        public Rect Reset() { return new Rect(0, 0, 1, 1); }
        public void SizeChanged(Size size) { }
        */
    }
}
