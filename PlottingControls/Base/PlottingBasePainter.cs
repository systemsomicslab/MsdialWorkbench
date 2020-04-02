using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PlottingControls.Base
{
    static class PlottingBasePainter
    {
        private static readonly Brush graphBackground = Brushes.WhiteSmoke;
        private static readonly Pen graphBorder = new Pen(Brushes.Black, 1);
        private static readonly Brush rubberForeground  = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.5 };
        private static readonly Pen rubberBorder  = new Pen(Brushes.DarkGray, 1);


        static public void DrawBackground(
            DrawingContext drawingContext,
            Point point, Vector vector
        )
        {
            drawingContext.PushClip(new RectangleGeometry(new Rect(point, vector)));
            drawingContext.DrawRectangle(graphBackground, graphBorder, new Rect(point, vector));
            drawingContext.Pop();
        }

        static public void DrawForegraound(
            DrawingContext drawingContext,
            Point point1, Point point2
        )
        {
            var rect = new Rect(point1, point2);
            drawingContext.PushClip(new RectangleGeometry(rect));
            drawingContext.DrawRectangle(rubberForeground, rubberBorder, rect);
            drawingContext.Pop();
        }
    }
}
