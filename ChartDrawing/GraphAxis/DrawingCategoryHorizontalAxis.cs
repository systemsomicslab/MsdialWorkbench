using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    public class DrawingCategoryHorizontalAxis : DrawingChartBase
    {
        public IReadOnlyList<double> XPositions
        {
            get => xPositions;
            set => SetProperty(ref xPositions, value as List<double> ?? new List<double>(value));
        }
        private List<double> xPositions;

        public IReadOnlyList<string> Labels
        {
            get => labels;
            set => SetProperty(ref labels, value as List<string> ?? new List<string>(value));
        }
        private List<string> labels;

        public int NLabel
        {
            get => nLabel;
            set => SetProperty(ref nLabel, value);
        }
        private int nLabel = -1;

        readonly Pen axisPen = new Pen(Brushes.Black, 2);

        public DrawingCategoryHorizontalAxis()
        {
            axisPen.Freeze();

            PropertyChanged += OnAxisPropertyChanged;
        }

        void OnAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "XPositions":
                case "NLabel":
                    axis = null;
                    label = null;
                    break;
                case "Labels":
                    label = null;
                    break;
            }

        }

        CategoryHorizontalAxisTickElement axis;
        CategoryHorizontalAxisLabelElement label;

        public override Drawing CreateChart()
        {
            var drawingGroup = new DrawingGroup();
            if (axis == null)
            {
                axis = new CategoryHorizontalAxisTickElement(XPositions, NLabel);
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
                label = new CategoryHorizontalAxisLabelElement(XPositions, Labels, NLabel);
                if (label == null) return drawingGroup;
            }
            var labelgeometry = label.GetGeometry(ChartArea, new Size(RenderSize.Width, Math.Max(1, RenderSize.Height - axis.Ticksize - 3)));
            labelgeometry.Transform = new TranslateTransform(0, axis.Ticksize + 3);
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
