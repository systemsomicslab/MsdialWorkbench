using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    public class DrawingContinuousVerticalAxis : DrawingChartBase
    {
        public double MinY
        {
            get => minY;
            set => SetProperty(ref minY, value);
        }
        private double minY;
        public double MaxY
        {
            get => maxY;
            set => SetProperty(ref maxY, value);
        }
        private double maxY;

        readonly Pen axisPen = new Pen(Brushes.Black, 2);

        public DrawingContinuousVerticalAxis()
        {
            axisPen.Freeze();

            PropertyChanged += OnAxisPropertyChanged;
        }

        void OnAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "MinY":
                case "MaxY":
                    axis = null;
                    label = null;
                    break;
            }

        }

        ContinuousVerticalAxisTickElement axis;
        ContinuousVerticalAxisLabelElement label;

        public override Drawing CreateChart()
        {
            var drawingGroup = new DrawingGroup();
            if (axis == null)
            {
                axis = new ContinuousVerticalAxisTickElement(MinY, MaxY);
                if (axis == null) return drawingGroup;
                InitialArea = axis.ElementArea;
            }
            if (ChartArea == default)
                ChartArea = InitialArea;
            drawingGroup.Children.Add(
                new GeometryDrawing(Brushes.Black, axisPen, axis.GetGeometry(ChartArea, RenderSize))
                );
            if (label == null)
            {
                label = new ContinuousVerticalAxisLabelElement(MinY, MaxY);
                if (label == null) return drawingGroup;
            }
            var labelgeometry = label.GetGeometry(ChartArea, new Size(Math.Max(1, RenderSize.Width - axis.LongTicksize - 3), RenderSize.Height));
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.Black, null,labelgeometry));
            return drawingGroup;
        }

        public override Point RealToImagine(Point point)
        {
            return new Point(0, (ChartArea.Bottom - point.Y / RenderSize.Height * ChartArea.Height));
        }
        public override Point ImagineToReal(Point point)
        {
            return new Point(0, (ChartArea.Bottom - point.Y) / ChartArea.Height * RenderSize.Height);
        }
    }
}
