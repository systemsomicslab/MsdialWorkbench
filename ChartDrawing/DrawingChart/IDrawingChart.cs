using System.Windows.Media;
using System.Windows;

namespace CompMs.Graphics.Core.Base
{
    public interface IDrawingChart
    {
        void Draw(DrawingContext drawingContext, Point point, Size size, IChartData chartData);
        void DrawBackground(DrawingContext drawingContext, Point point, Size size);
        void DrawForeground(DrawingContext drawingContext, Point point, Size size);
    }


    public class DefaultDrawingChart : IDrawingChart
    {
        protected static readonly Brush graphBackground = Brushes.WhiteSmoke;
        protected static readonly Pen graphBorder = new Pen(Brushes.Black, 1);
        protected static readonly Brush rubberForeground = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.5 };
        protected static readonly Pen rubberBorder = new Pen(Brushes.DarkGray, 1);

        virtual public void Draw(DrawingContext drawingContext, Point point, Size size, IChartData chartData) { }
        virtual public void DrawBackGround(DrawingContext drawingContext, Point point, Size size)
        {
            var rect = new Rect(point, size);
            drawingContext.PushClip(new RectangleGeometry(rect));
            drawingContext.DrawRectangle(graphBackground, graphBorder, rect);
            drawingContext.Pop();
        }
        virtual public void DrawForeground(DrawingContext drawingContext, Point point, Size size)
        {
            var rect = new Rect(point, size);
            drawingContext.PushClip(new RectangleGeometry(rect));
            drawingContext.DrawRectangle(rubberForeground, rubberBorder, rect);
            drawingContext.Pop();
        }

        void IDrawingChart.Draw(DrawingContext drawingContext, Point point, Size size, IChartData chartData)
            => Draw(drawingContext, point, size, chartData);
        void IDrawingChart.DrawBackground(DrawingContext drawingContext, Point point, Size size)
            => DrawBackGround(drawingContext, point, size);
        void IDrawingChart.DrawForeground(DrawingContext drawingContext, Point point, Size size)
            => DrawForeground(drawingContext, point, size);
    }
}
