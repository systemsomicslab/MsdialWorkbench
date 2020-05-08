using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Runtime.CompilerServices;

using CompMs.Graphics.Core.Base;
using CompMs.Common.DataStructure;

namespace CompMs.Graphics.Core.Dendrogram
{
    public class DrawingDendrogram : DrawingChartBase
    {
        public DirectedTree Tree
        {
            get => tree;
            set => SetProperty(ref tree, value);
        }
        DirectedTree tree;
        public IReadOnlyList<XY> Series
        {
            get => series;
            set => SetProperty(ref series, value as List<XY> ?? new List<XY>(value));
        }
        List<XY> series;

        DendrogramElement element = null;
        readonly Pen graphLine = new Pen(Brushes.Black, 1);

        public DrawingDendrogram() : base()
        {
            graphLine.Freeze();
        }

        public override Drawing CreateChart()
        {
            if (element == null)
            {
                if (Tree == null || Series == null)
                    return new GeometryDrawing();
                element = new DendrogramElement(
                    Tree, Series.Select(xy => (double)xy.X).ToArray(), Series.Select(xy => (double)xy.Y).ToArray()
                    );
                var area = element.ElementArea;
                InitialArea = new Rect(
                    area.X - area.Width * 0.05, area.Y,
                    area.Width * 1.1, area.Height * 1.05
                    );
            }
            if (ChartArea == default)
            {
                ChartArea = InitialArea;
            }
            var geometry = element.GetGeometry(ChartArea, RenderSize);
            geometry.Transform = new ScaleTransform(1, -1, 0, RenderSize.Height / 2);
            return new GeometryDrawing(Brushes.Black, graphLine, geometry);
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
