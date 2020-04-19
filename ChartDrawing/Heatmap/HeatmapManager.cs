using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Heatmap
{
    class HeatmapManager : IChartManager
    {
        public Rect ChartArea { get; }
        public IReadOnlyList<double> XPositions { get; }
        public IReadOnlyList<double> YPositions { get; }
        public GradientStopCollection Gsc { get; }

        List<(Color color, IDrawingElement element)> areas;
        Dictionary<Color, Brush> brushMemo;

        public HeatmapManager(
            double[,] dataMatrix,
            IReadOnlyList<double> xPositions = null,
            IReadOnlyList<double> yPositions = null,
            GradientStopCollection gsc = null
            )
        {
            if (dataMatrix.Length == 0) return;

            double[] XBounds, YBounds;

            XPositions = xPositions?.ToArray();
            if (XPositions == null)
                XPositions = Enumerable.Range(0, dataMatrix.GetLength(1)).Select(e => (double)e).ToArray();
            var xidcs = Enumerable.Range(0, XPositions.Count).OrderBy(i => XPositions[i]).ToArray();
            XBounds = new double[dataMatrix.GetLength(1) + 1];
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

            YPositions = yPositions?.ToArray();
            if (YPositions == null)
                YPositions = Enumerable.Range(0, dataMatrix.GetLength(0)).Select(e => (double)e).ToArray();
            var yidcs = Enumerable.Range(0, YPositions.Count).OrderBy(i => YPositions[i]).ToArray();
            YBounds = new double[dataMatrix.GetLength(0) + 1];
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

            var matrixAreas = new List<(double value, Rect area)>(dataMatrix.Length);
            for(int i = 0; i<xidcs.Length; ++i)
                for(int j = 0; j<yidcs.Length; ++j)
                    matrixAreas.Add((
                            dataMatrix[yidcs[j], xidcs[i]],
                            new Rect(new Point(XBounds[i], YBounds[j]), new Point(XBounds[i + 1], YBounds[j + 1]))
                        ));
            matrixAreas.Sort((a, b) => a.value.CompareTo(b.value));
            (double rmin, double rmax) = roundValues(matrixAreas[0].value, matrixAreas[matrixAreas.Count - 1].value);

            if (gsc == null)
            {
                gsc = new GradientStopCollection(2);
                gsc.Add(new GradientStop(Colors.Blue, 0));
                gsc.Add(new GradientStop(Colors.Red, 1));
            }

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

            ChartArea = areas.Select(e => e.element.ElementArea).Aggregate((acc, next) => Rect.Union(acc, next));
        }

        public Drawing CreateChart(Rect rect, Size size)
        {
            var drawingGroup = new DrawingGroup();
            foreach(var area in areas)
            {
                var drawing = new GeometryDrawing(brushMemo[area.color], null, area.element.GetGeometry(rect, size));
                drawingGroup.Children.Add(drawing);
            }
            return drawingGroup;
        }

        public Point Translate(Point point, Rect area, Size size)
        {
            return new Point(point.X / size.Width * area.Width + area.X,
                             point.Y / size.Height * area.Height + area.Y);
        }
        public Vector Translate(Vector vector, Rect area, Size size)
        {
            return new Vector(vector.X / size.Width * area.Width,
                              vector.Y / size.Height * area.Height);
        }
        public Rect Translate(Rect rect, Rect area, Size size)
        {
            return new Rect(Translate(rect.TopLeft, area, size),
                            Translate(rect.BottomRight, area, size));
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
