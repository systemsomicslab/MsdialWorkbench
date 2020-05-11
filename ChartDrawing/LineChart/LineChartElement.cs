using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;


namespace CompMs.Graphics.Core.LineChart
{
    public class LineChartElement : IDrawingElement
    {
        public LineChartElement(
            IReadOnlyList<double> xPositions,
            IReadOnlyList<double> yPositions
            )
        {
            lineChart = new PathGeometry();
            var path = new PathFigure();
            if (xPositions.Count != 0 && yPositions.Count != 0)
            {
                path.StartPoint = new Point(xPositions[0], yPositions[0]);
                foreach((double x, double y) in xPositions.Zip(yPositions, Tuple.Create).Skip(1))
                {
                    path.Segments.Add(new LineSegment() { Point = new Point(x, y) });
                }
            }
            path.Freeze();
            lineChart.Figures = new PathFigureCollection { path };
        }


        PathGeometry lineChart;

        public Rect ElementArea => lineChart.Bounds;

        public Geometry GetGeometry(Rect rect, Size size)
        {
            PathGeometry element = lineChart;
            var transforms = new TransformGroup();
            transforms.Children.Add(new TranslateTransform(-rect.Left, -rect.Top));
            transforms.Children.Add(new ScaleTransform(size.Width / rect.Width, size.Height / rect.Height));
            element.Transform = transforms;
            return element;
        }
    }
}
