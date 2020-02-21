using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Msdial.Heatmap
{
    static class HeatmapPainter
    {
        static readonly Pen borderLine = new Pen(Brushes.Black, 2);

        static private Color blendColors(Color ca, Color cb, double factor)
        {
            var f = (Single)factor;
            return cb * f + ca * (1 - f);
        }

        static private Color getGradientColor(GradientStopCollection gsc, double offset)
        {
            var lowers = gsc.Where(gs => gs.Offset <= offset).ToArray();
            var highers = gsc.Where(gs => gs.Offset > offset).ToArray();
            if (offset < 0) return highers.Min(gs => (gs.Offset, gs.Color)).Color;
            if (offset >= 1) return lowers.Max(gs => (gs.Offset, gs.Color)).Color; 

            var lo = lowers.Max(gs => (gs.Offset, gs.Color)); 
            var hi = highers.Min(gs => (gs.Offset, gs.Color));
            var o = ((offset - lo.Offset) / (hi.Offset - lo.Offset));
            return blendColors(lo.Color, hi.Color, o);
        }

        static public void Draw(
            DrawingContext drawingContext,
            double[,] matrix,
            double valueMin, double valueMax,
            Point point, Vector vector,
            double xMin, double xMax,
            double yMin, double yMax,
            IReadOnlyList<double> xBorders, IReadOnlyList<double> yBorders,
            LinearGradientBrush gbrush
        )
        {
            var m = matrix.GetLength(0);
            var n = matrix.GetLength(1);

            double mapX(double x) => vector.X / (xMax - xMin) * (x - xMin) + point.X;
            double mapY(double y) => vector.Y / (yMax - yMin) * (y - yMin) + point.Y;

            drawingContext.PushClip(new RectangleGeometry(new Rect(point, vector)));
            if (valueMax - valueMin > Math.Pow(10, -10))
            {
                for(int i=0; i < m; ++i)
                {
                    for(int j = 0; j < n; ++j)
                    {
                        var c = getGradientColor(
                            gbrush.GradientStops,
                            (Math.Max(Math.Min(matrix[i, j], valueMax), valueMin) - valueMin) / (valueMax - valueMin)
                        );
                        var brush = new SolidColorBrush(c);
                        drawingContext.DrawRectangle(
                            brush, borderLine,
                            new Rect(
                                new Point(mapX(xBorders[i]), mapY(yBorders[j])),
                                new Point(mapX(xBorders[i+1]), mapY(yBorders[j+1])))
                        );
                    }
                }
            }
            drawingContext.Pop();
        }
    }
}
