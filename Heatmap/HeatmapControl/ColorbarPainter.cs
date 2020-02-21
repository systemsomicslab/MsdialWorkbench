using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Msdial.Heatmap
{
    static class ColorbarPainter
    {
        static public void Draw(
            DrawingContext drawingContext,
            Point point, Vector vector,
            // Color ca, Color cb
            LinearGradientBrush gbrush
        )
        {
            drawingContext.PushClip(new RectangleGeometry(new Rect(point, vector)));
            drawingContext.PushTransform(new MatrixTransform(1, 0, 0, -1, 0, vector.Y + point.Y * 2));
            drawingContext.DrawRectangle(gbrush, null, new Rect(point, vector));
            drawingContext.Pop();
            drawingContext.Pop();
        }
    }
}
