using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    class CategoryVerticalAxisManager : IChartManager
    {
        public CategoryVerticalAxisManager(IReadOnlyList<double> yPositions, IReadOnlyList<string> labels, int lim)
        {
            axis = new CategoryVerticalAxisTickElement(yPositions, lim);
            label = new CategoryVerticalAxisLabelElement(yPositions, labels, lim);
        }

        CategoryVerticalAxisTickElement axis;
        CategoryVerticalAxisLabelElement label;
        public Rect ChartArea => axis.ElementArea;

        public Drawing CreateChart(Rect rect, Size size)
        {
            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(
                new GeometryDrawing(Brushes.Black, new Pen(Brushes.Black, 2), axis.GetGeometry(rect, size))
                );
            drawingGroup.Children.Add(
                new GeometryDrawing(
                    Brushes.Black, null,
                    label.GetGeometry(rect, new Size(Math.Max(1, size.Width - axis.Ticksize - 3), size.Height)))
                );
            return drawingGroup;
        }

        public Point Translate(Point point, Rect area, Size size)
        {
            return new Point(
                (size.Width - point.X) / size.Width * area.Width + area.X,
                point.Y / size.Height * area.Height + area.Y
                );
        }
        public Vector Translate(Vector vector, Rect area, Size size)
        {
            return new Vector(
                - vector.X / size.Width * area.Width,
                vector.Y / size.Height * area.Height
                );
        }
        public Rect Translate(Rect rect, Rect area, Size size)
        {
            return new Rect(Translate(rect.TopLeft, area, size),
                            Translate(rect.BottomRight, area, size));
        }
        public Point Inverse(Point point, Rect area, Size size){
            return new Point(
                (1 - (point.X - area.X) / area.Width) * size.Width,
                (point.Y - area.Y) / area.Height * size.Height
                );
        }
        public Vector Inverse(Vector vector, Rect area, Size size){
            return new Vector(
                - vector.X / area.Width * size.Width,
                vector.Y / area.Height * size.Height
                );
        }
        public Rect Inverse(Rect rect, Rect area, Size size){
            return new Rect(Inverse(rect.TopLeft, area, size),
                            Inverse(rect.BottomRight, area, size));
        }
    }
}
