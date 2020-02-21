using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Msdial.AxisControl
{
    static class CategoryHorizontalAxisPainter
    {
        static readonly Pen axisLine = new Pen(Brushes.Black, 2);

        static public void Draw(
            DrawingContext drawingContext,
            Point point, Vector vector,
            double xmin, double xmax,
            IReadOnlyCollection<double> positions,
            IEnumerable<string> labels,
            double tick, double rotate = 45
        )
        {
            if (xmax == xmin) return;
            double mapX(double x) => vector.X / (xmax - xmin) * (x - xmin) + point.X;

            var sin = Math.Sin(rotate * Math.PI / 180d);
            var cos = Math.Cos(rotate * Math.PI / 180d);
            var trans = new Matrix(cos, sin, -sin, cos, 0, 0);

            drawingContext.PushClip(new RectangleGeometry(new Rect(point, vector)));
            drawingContext.DrawLine(
                axisLine,
                new Point(point.X, axisLine.Thickness / 2),
                new Point(point.X + vector.X, axisLine.Thickness / 2)
            );

            foreach (var pos in positions)
            {
                var p = new Point(mapX(pos), point.Y + tick);
                drawingContext.DrawLine(
                    axisLine, new Point(mapX(pos), point.Y), p
                );
            }
            drawingContext.Pop();

            drawingContext.PushTransform(new RotateTransform(-rotate));
            foreach ((double pos, string label) in positions.Zip(labels, Tuple.Create))
            {
                if (pos < xmin || pos > xmax) continue;
                var p = new Point(mapX(pos), tick);
                var formattedText = new FormattedText(
                    label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                    new Typeface("Calibli"), 12, Brushes.Black
                )
                { TextAlignment = TextAlignment.Right };
                var width = (vector.Y - tick - formattedText.Height * cos) / sin;
                if(width > 0)
                {
                    formattedText.MaxTextWidth = width;
                    trans.OffsetX = -width;
                    drawingContext.DrawText(formattedText, p * trans);
                }
            }
            drawingContext.Pop();
        }
    }
}
