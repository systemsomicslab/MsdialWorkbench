using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PlottingControls.GraphAxis
{
    public enum AxisDirection{
        Right,
        Left
    }

    static class ContinuousVerticalAxisPainter
    {
        static readonly Pen axisLine = new Pen(Brushes.Black, 2);

        static private double roundTick(double min, double max)
        {
            return Math.Pow(10, Math.Floor(Math.Log10(max - min)));
        }

        static public void Draw(
            DrawingContext drawingContext,
            Point point, Vector vector,
            double ymin, double ymax,
            double longtick, double shorttick,
            AxisDirection direction = AxisDirection.Right
        )
        {
            if (ymax == ymin) return;
            double mapY(double y) => vector.Y / (ymax - ymin) * (ymax - y) + point.Y;

            Matrix mat, mat2;
            TextAlignment align;
            switch (direction)
            {
                case AxisDirection.Left:
                    mat = new Matrix(1, 0, 0, 1, 0, 0);
                    mat2 = new Matrix(1, 0, 0, 1, longtick, 0);
                    align = TextAlignment.Left;
                    break;
                case AxisDirection.Right:
                default:
                    mat = new Matrix(-1, 0, 0, 1, point.X + vector.X, 0);
                    mat2 = new Matrix(1, 0, 0, 1, point.X + vector.X - longtick, 0);
                    align = TextAlignment.Right;
                    // throw new NotImplementedException("Right side axis is not implemented.");
                    break;
            }

            drawingContext.PushTransform(new MatrixTransform(mat));
            drawingContext.PushClip(new RectangleGeometry(new Rect(point*mat, vector*mat)));
            drawingContext.DrawLine(
                axisLine,
                new Point(axisLine.Thickness / 2, point.Y),
                new Point(axisLine.Thickness / 2, point.Y + vector.Y)
            );
            drawingContext.Pop();

            var d = roundTick(ymin, ymax);
            var lo = ymin;
            var hi = ymax;
            var ytick = Math.Floor(ymin / d) * d;

            while(ytick <= hi)
            {
                if (ytick >= lo)
                {
                    drawingContext.DrawLine(
                        axisLine,
                        new Point(0, mapY(ytick)),
                        new Point(longtick, mapY(ytick))
                    );
                }
                ytick += d;
            }

            ytick = Math.Floor(ymin / d) * d - d / 2;
            while (ytick <= hi)
            {
                if (ytick >= lo)
                {
                    drawingContext.DrawLine(
                        axisLine,
                        new Point(0, mapY(ytick)),
                        new Point(shorttick, mapY(ytick))
                    );
                }
                ytick += d;
            }

            drawingContext.Pop();

            drawingContext.PushTransform(new MatrixTransform(mat2));

            ytick = Math.Floor(ymin / d) * d;
            while(ytick <= hi)
            {
                if (ytick >= lo)
                {
                    var formattedText = new FormattedText(
                        ytick.ToString(), CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight, new Typeface("Calibri"),
                        12, Brushes.Black
                    )
                    {
                        TextAlignment = align
                    };
                    drawingContext.DrawText(
                        formattedText,
                        new Point(0, mapY(ytick) - formattedText.Height / 2)
                    );
                }
                ytick += d;
            }

            drawingContext.Pop();
        }
    }
}
