using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Heatmap
{
    public class DrawingHeatmap : DrawingChartBase
    {
        public double[,] DataMatrix
        {
            get => dataMatrix;
            set => SetProperty(ref dataMatrix, value);
        }
        double[,] dataMatrix;

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

        public GradientStopCollection Gsc
        {
            get => gsc;
            set => SetProperty(ref gsc, value);
        }
        GradientStopCollection gsc;

        List<(Color color, IDrawingElement element)> areas;
        Dictionary<Color, Brush> brushMemo;

        public DrawingHeatmap()
        {
            Gsc = new GradientStopCollection();
            Gsc.Add(new GradientStop(Colors.Blue, 0));
            Gsc.Add(new GradientStop(Colors.Red, 1));
        }

        public override Drawing CreateChart()
        {
            if (areas == null)
            {
                createElements();
                if (areas == null) return new GeometryDrawing();
                InitialArea = areas.Select(e => e.element.ElementArea).Aggregate((acc, next) => Rect.Union(acc, next));
            }
            var drawingGroup = new DrawingGroup();
            if (areas == null) return drawingGroup;
            if (ChartArea == default)
            {
                ChartArea = InitialArea;
            }
            foreach(var area in areas)
            {
                var drawing = new GeometryDrawing(brushMemo[area.color], null, area.element.GetGeometry(ChartArea, RenderSize));
                drawingGroup.Children.Add(drawing);
            }
            return drawingGroup;
        }

        private void createElements()
        {
            if (DataMatrix == null || DataMatrix.Length == 0) return;
            if (XPositions == null || DataMatrix.GetLength(1) != XPositions.Count) return;
            if (YPositions == null || DataMatrix.GetLength(0) != YPositions.Count) return;
            if (Gsc == null) return;

            double[] XBounds, YBounds;

            var xidcs = Enumerable.Range(0, XPositions.Count).OrderBy(i => XPositions[i]).ToArray();
            XBounds = new double[XPositions.Count + 1];
            for(int i = 1; i<xidcs.Length; ++i)
                XBounds[i] = (XPositions[xidcs[i - 1]] + XPositions[xidcs[i]]) / 2;
            if (XBounds.Length != 2)
            {
                XBounds[0] = XPositions[xidcs[0]] * 2 - XBounds[1];
                XBounds[XBounds.Length - 1] = XPositions[xidcs[xidcs.Length - 1]] * 2 - XBounds[XBounds.Length - 2];
            }
            else
            {
                XBounds[0] = XPositions[xidcs[0]] - 1;
                XBounds[1] = XPositions[xidcs[0]] + 1;
            }

            var yidcs = Enumerable.Range(0, YPositions.Count).OrderBy(i => YPositions[i]).ToArray();
            YBounds = new double[YPositions.Count + 1];
            for(int i = 1; i<yidcs.Length; ++i)
                YBounds[i] = (YPositions[yidcs[i - 1]] + YPositions[yidcs[i]]) / 2;
            if (YBounds.Length != 2)
            {
                YBounds[0] = YPositions[yidcs[0]] * 2 - YBounds[1];
                YBounds[YBounds.Length - 1] = YPositions[yidcs[yidcs.Length - 1]] * 2 - YBounds[YBounds.Length - 2];
            }
            else
            {
                YBounds[0] = YPositions[yidcs[0]] - 1;
                YBounds[1] = YPositions[yidcs[0]] + 1;
            }

            var matrixAreas = new List<(double value, Rect area)>(DataMatrix.Length);
            for(int i = 0; i<DataMatrix.GetLength(1); ++i)
                for(int j = 0; j<DataMatrix.GetLength(0); ++j)
                    matrixAreas.Add((
                            DataMatrix[yidcs[j], xidcs[i]],
                            new Rect(new Point(XBounds[i], YBounds[j]), new Point(XBounds[i + 1], YBounds[j + 1]))
                        ));
            matrixAreas.Sort((a, b) => a.value.CompareTo(b.value));
            (double rmin, double rmax) = roundValues(matrixAreas[0].value, matrixAreas[matrixAreas.Count - 1].value);

            areas = new List<(Color color, IDrawingElement element)>();
            brushMemo = new Dictionary<Color, Brush>();
            {
                Color cur;
                int i = 0, j = 1;
                while (i < matrixAreas.Count)
                {
                    cur = getGradientColor(gsc, matrixAreas[i].value, rmin, rmax);
                    if (!brushMemo.ContainsKey(cur))
                    {
                        brushMemo[cur] = new SolidColorBrush(cur);
                        brushMemo[cur].Freeze();
                    }

                    while (j < matrixAreas.Count && Color.AreClose(cur, getGradientColor(gsc, matrixAreas[j].value, rmin, rmax)))
                        ++j;
                    areas.Add((cur, new HeatmapElement(matrixAreas.GetRange(i, j - i).Select(e => e.area))));
                    i = j++;
                }

            }
        }

        static (double, double) roundValues(double min, double max)
        {
            var d = Math.Pow(10, Math.Floor(Math.Log10(max - min)));
            return (Math.Floor(min / d) * d, Math.Ceiling(max / d) * d);
        }

        static Color blendColors(Color ca, Color cb, double factor)
        {
            var f = (float)factor;
            return cb * f + ca * (1 - f);
        }

        static Color getGradientColor(GradientStopCollection gsc, double offset)
        {
            var lowers = gsc.Where(gs => gs.Offset <= offset).ToArray();
            var highers = gsc.Where(gs => gs.Offset > offset).ToArray();
            if (offset < 0) return highers.Min(gs => (gs.Offset, gs.Color)).Color;
            if (offset >= 1) return lowers.Max(gs => (gs.Offset, gs.Color)).Color; 

            var lo = lowers.Max(gs => (gs.Offset, gs.Color)); 
            var hi = highers.Min(gs => (gs.Offset, gs.Color));
            var o = ((offset - lo.Offset) / (hi.Offset - lo.Offset));
            return blendColors(lo.Color, hi.Color, o);
        }

        static Color getGradientColor(GradientStopCollection gsc, double value, double min, double max)
            => getGradientColor(gsc, (value - min) / (max - min));
    }
}
