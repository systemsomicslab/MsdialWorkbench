using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.LineChart
{
    public class DrawingLineChart : DrawingChartBase
    {
        public IReadOnlyList<double> XPositions
        {
            get => xPositions;
            set => SetProperty(ref xPositions, value as List<double> ?? new List<double>(value));

        }
        List<double> xPositions;
        public IReadOnlyList<double> YPositions
        {
            get => yPositions;
            set => SetProperty(ref yPositions, value as List<double> ?? new List<double>(value));

        }
        List<double> yPositions;

        LineChartElement element;
        readonly Pen graphLine = new Pen(Brushes.Black, 1);

        void OnChartPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "XPositions":
                case "YPositions":
                    element = null;
                    break;
            }
        }

        public DrawingLineChart() : base()
        {
            graphLine.Freeze();

            PropertyChanged += OnChartPropertyChanged;
        }

        public override Drawing CreateChart()
        {
            if (element == null)
            {
                if (XPositions == null || YPositions == null)
                    return new GeometryDrawing();
                element = new LineChartElement(XPositions, YPositions);
                var area = element.ElementArea;
                area.Inflate(0, area.Height * 0.05);
                InitialArea = area;
            }
            if (ChartArea == default)
            {
                ChartArea = InitialArea;
            }
            var geometry = element.GetGeometry(ChartArea, RenderSize);
            var transforms = new TransformGroup();
            transforms.Children.Add(geometry.Transform);
            transforms.Children.Add(new ScaleTransform(1, -1, 0, RenderSize.Height / 2));
            geometry.Transform = transforms;
            return new GeometryDrawing(null, graphLine, geometry);
        }

        public override Point RealToImagine(Point point)
        {
            return new Point(
                (point.X / RenderSize.Width * ChartArea.Width + ChartArea.X),
                (ChartArea.Bottom - point.Y / RenderSize.Height * ChartArea.Height)
                );
        }

        public override Point ImagineToReal(Point point)
        {
            return new Point(
                (point.X - ChartArea.X) / ChartArea.Width * RenderSize.Width,
                (ChartArea.Bottom - point.Y) / ChartArea.Height * RenderSize.Height
                );
        }
    }
}
