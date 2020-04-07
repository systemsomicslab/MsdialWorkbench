using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PlottingControls.GraphAxis
{
    static class CategoryVerticalAxisPainter
    {
        static readonly Pen axisLine = new Pen(Brushes.Black, 2);

        static public void Draw(
            DrawingContext drawingContext,
            Point point, Vector vector,
            double yMin, double yMax,
            IReadOnlyCollection<double> positions,
            IEnumerable<string> labels,
            double tick, double rotate = 45
        )
        {
            double mapY(double y) => vector.Y / (yMax - yMin) * (y - yMin) + point.Y;

            var sin = Math.Sin(rotate * Math.PI / 180d);
            var cos = Math.Cos(rotate * Math.PI / 180d);
            var trans = new Matrix(cos, sin, -sin, cos, 0, 0);
            var coordinateSystem = new Matrix(1, 0, 0, 1, 0, - point.Y * 2 - vector.Y);
            var inv_coordinateSystem = new Matrix(1, 0, 0, 1, 0, point.Y * 2 + vector.Y);

            drawingContext.PushTransform(new MatrixTransform(coordinateSystem));
            drawingContext.PushClip(new RectangleGeometry(new Rect(point * inv_coordinateSystem, vector * inv_coordinateSystem)));
            drawingContext.DrawLine(
                axisLine,
                new Point(point.X + vector.X - axisLine.Thickness / 2, point.Y) * inv_coordinateSystem,
                new Point(point.X + vector.X - axisLine.Thickness / 2, point.Y + vector.Y) * inv_coordinateSystem
            );

            foreach (var pos in positions)
            {
                drawingContext.DrawLine(
                    axisLine,
                    new Point(point.X + vector.X, mapY(pos)) * inv_coordinateSystem,
                    new Point(point.X + vector.X - tick, mapY(pos)) * inv_coordinateSystem
                );
            }
            drawingContext.Pop();

            drawingContext.PushTransform(new RotateTransform(-rotate));
            foreach ((double pos, string label) in positions.Zip(labels, Tuple.Create))
            {
                if (pos < yMin || pos > yMax) continue;
                var formattedText = new FormattedText(
                    label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                    new Typeface("Calibli"), 12, Brushes.Black
                )
                { TextAlignment = TextAlignment.Right };
                var p = new Point(
                    point.X + vector.X - tick - formattedText.Height * sin,
                    mapY(pos) - formattedText.Height * cos / 2
                );
                var width = (vector.X - tick - formattedText.Height * sin) / cos - formattedText.Height * sin;
                if(width > 0)
                {
                    formattedText.MaxTextWidth = width;
                    trans.OffsetX = -width;
                    drawingContext.DrawText(formattedText, p * trans * inv_coordinateSystem);
                }
            }
            drawingContext.Pop();
            drawingContext.Pop();
        }
    }
}
