using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PlottingControls.GraphAxis
{
    static class CategoryHorizontalAxisPainter
    {
        static readonly Pen axisLine = new Pen(Brushes.Black, 2);

        static public void Draw(
            DrawingContext drawingContext,
            Point point, Vector vector,
            double xmin, double xmax,
            IReadOnlyList<double> positions,
            IReadOnlyList<string> labels,
            double tick, double rotate
        )
        {
            if (xmax <= xmin) return;
            double mapX(double x) => vector.X / (xmax - xmin) * (x - xmin) + point.X;

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

            var sin = Math.Sin(rotate * Math.PI / 180d);
            var cos = Math.Cos(rotate * Math.PI / 180d);
            var trans = new Matrix(cos, sin, -sin, cos, 0, 0);

            var textalign = TextAlignment.Right;
            var maxwidth = mapX(xmax) - mapX(xmin);
            var offsetFactor = 1d;
            if (Math.Abs(sin) < 1e-7)
            {
                textalign = TextAlignment.Center;
                offsetFactor = 0.5;
                var pos = positions.Select(p => mapX(p)).OrderBy(p => p).ToArray();
                for(int i = 1; i < pos.Length; ++i)
                    maxwidth = Math.Min(maxwidth, pos[i] - pos[i - 1]);
            }

            drawingContext.PushTransform(new RotateTransform(-rotate));
            foreach ((double pos, string label) in positions.Zip(labels, Tuple.Create))
            {
                if (pos < xmin || pos > xmax) continue;
                var p = new Point(mapX(pos), tick);
                var formattedText = new FormattedText(
                    label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                    new Typeface("Calibli"), 12, Brushes.Black
                ){ TextAlignment = textalign };
                var width = (vector.Y - tick - formattedText.Height * cos) / sin;
                width = Math.Min(width, maxwidth);
                if(width > 0)
                {
                    formattedText.MaxTextWidth = width;
                    trans.OffsetX = -width * offsetFactor;
                    drawingContext.DrawText(formattedText, p * trans);
                }
            }
            drawingContext.Pop();
        }
    }
}
