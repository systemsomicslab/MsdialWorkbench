using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    public class DrawingCategoryVerticalAxis : DrawingChartBase
    {
        public IReadOnlyList<double> YPositions
        {
            get => yPositions;
            set => SetProperty(ref yPositions, value as List<double> ?? new List<double>(value));
        }
        private List<double> yPositions;

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

        public DrawingCategoryVerticalAxis()
        {
            axisPen.Freeze();
        }

        void OnAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "YPositions":
                case "NLabel":
                    axis = null;
                    label = null;
                    break;
                case "Labels":
                    label = null;
                    break;
            }

        }

        CategoryVerticalAxisTickElement axis;
        CategoryVerticalAxisLabelElement label;

        public override Drawing CreateChart()
        {
            var drawingGroup = new DrawingGroup();
            if (axis == null)
            {
                axis = new CategoryVerticalAxisTickElement(YPositions, NLabel);
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
                label = new CategoryVerticalAxisLabelElement(YPositions, Labels, NLabel);
                if (label == null) return drawingGroup;
            }
            var labelgeometry = label.GetGeometry(ChartArea, new Size(Math.Max(1, RenderSize.Width - axis.Ticksize - 3), RenderSize.Height));
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.Black, null,labelgeometry));
            return drawingGroup;
        }

        public override Point RealToImagine(Point point)
        {
            var p = base.RealToImagine(point);
            return new Point(0, p.Y);
        }
        public override Point ImagineToReal(Point point)
        {
            var p = base.ImagineToReal(point);
            return new Point(0, p.Y);
        }
    }
}
