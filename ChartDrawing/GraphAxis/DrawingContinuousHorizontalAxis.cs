using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    public class DrawingContinuousHorizontalAxis : DrawingChartBase
    {
        public double MinX
        {
            get => minX;
            set => SetProperty(ref minX, value);
        }
        private double minX;
        public double MaxX
        {
            get => maxX;
            set => SetProperty(ref maxX, value);
        }
        private double maxX;

        readonly Pen axisPen = new Pen(Brushes.Black, 2);

        public DrawingContinuousHorizontalAxis()
        {
            axisPen.Freeze();

            PropertyChanged += OnAxisPropertyChanged;
        }

        void OnAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "MinX":
                case "MaxX":
                    axis = null;
                    label = null;
                    break;
            }

        }

        ContinuousHorizontalAxisTickElement axis;
        ContinuousHorizontalAxisLabelElement label;

        public override Drawing CreateChart()
        {
            var drawingGroup = new DrawingGroup();
            if (axis == null)
            {
                axis = new ContinuousHorizontalAxisTickElement(MinX, MaxX);
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
                label = new ContinuousHorizontalAxisLabelElement(MinX, MaxX);
                if (label == null) return drawingGroup;
            }
            var labelgeometry = label.GetGeometry(ChartArea, new Size(RenderSize.Width, Math.Max(1, RenderSize.Height - axis.LongTicksize - 3)));
            labelgeometry.Transform = new TranslateTransform(0, axis.LongTicksize + 3);
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.Black, null,labelgeometry));
            return drawingGroup;
        }

        public override Point RealToImagine(Point point)
        {
            var p = base.RealToImagine(point);
            return new Point(p.X, 0);
        }
        public override Point ImagineToReal(Point point)
        {
            var p = base.ImagineToReal(point);
            return new Point(p.X, 0);
        }
    }
}
