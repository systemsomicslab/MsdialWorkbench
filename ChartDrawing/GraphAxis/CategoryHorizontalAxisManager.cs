using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    class CategoryHorizontalAxisManager : IChartManager
    {
        public CategoryHorizontalAxisManager(IReadOnlyList<double> xPositions, IReadOnlyList<string> labels, int lim)
        {
            axis = new CategoryHorizontalAxisTickElement(xPositions, lim);
            label = new CategoryHorizontalAxisLabelElement(xPositions, labels, lim);
        }

        CategoryHorizontalAxisTickElement axis;
        CategoryHorizontalAxisLabelElement label;
        public Rect ChartArea => axis.ElementArea;

        public Drawing CreateChart(Rect rect, Size size)
        {
            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(
                new GeometryDrawing(Brushes.Black, new Pen(Brushes.Black, 2), axis.GetGeometry(rect, size))
                );
            var labelgeometry = label.GetGeometry(rect, new Size(size.Width, Math.Max(1, size.Height - axis.Ticksize - 3)));
            labelgeometry.Transform = new TranslateTransform(0, axis.Ticksize + 3);
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.Black, null,labelgeometry));
            return drawingGroup;
        }

        public Point Translate(Point point, Rect area, Size size)
        {
            return new Point(point.X / size.Width * area.Width + area.X, 0);
        }
        public Vector Translate(Vector vector, Rect area, Size size)
        {
            return new Vector(vector.X / size.Width * area.Width, 0);
        }
        public Rect Translate(Rect rect, Rect area, Size size)
        {
            return new Rect(Translate(rect.TopLeft, area, size),
                            Translate(rect.BottomRight, area, size));
        }
    }
}
