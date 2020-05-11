using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

using CompMs.Common.DataStructure;

namespace CompMs.Graphics.Core.Base
{
    public class Utility
    {
        #region XY axis setting in Draw Visual to set fixed max and min value
        public static void SetDrawingMinAndMaxXYConstValue(DrawVisual drawing, float minX = float.MinValue, float maxX = float.MinValue, float minY = float.MinValue, float maxY = float.MinValue) {
            if (minX > float.MinValue) drawing.MinX = minX;
            if (maxX > float.MinValue) drawing.MaxX = maxX;
            if (minY > float.MinValue) drawing.MinY = minY;
            if (maxY > float.MinValue) drawing.MaxY = maxY;
        }

        public static void SetDrawingMinXRatio(DrawVisual drawing, float ratio) {
            drawing.MinX -= (drawing.SeriesList.MaxX - drawing.SeriesList.MinX) * ratio;
        }

        public static void SetDrawingMinYRatio(DrawVisual drawing, float ratio) {
            drawing.MinY -= (drawing.SeriesList.MaxY - drawing.SeriesList.MinY) * ratio;
        }

        public static void SetDrawingMaxXRatio(DrawVisual drawing, float ratio) {
            drawing.MaxX += (drawing.SeriesList.MaxX - drawing.SeriesList.MinX) * ratio;
        }

        public static void SetDrawingMaxYRatio(DrawVisual drawing, float ratio) {
            drawing.MaxY += (drawing.SeriesList.MaxY - drawing.SeriesList.MinY) * ratio;
        }
        #endregion

        #region Get Area
        public static Area GetDefaultAreaV1(string xlabel = "Retention time (min)", string ylabel = "RT diff (Sample - Reference) (sec)") {
            var area = new Area() {
                AxisX = new AxisX() { AxisLabel = xlabel, Pen = new Pen(Brushes.Black, 0.5), FontSize = 12 },
                AxisY = new AxisY() { AxisLabel = ylabel, Pen = new Pen(Brushes.Black, 0.5), FontSize = 12 }
            };
            return area;
        }

        public static Area GetDefaultAreaV2(string xlabel = "Retention time (min)") {
            var area = new Area() {
                AxisX = new AxisX() { AxisLabel = xlabel, Pen = new Pen(Brushes.DarkGray, 0.5), FontSize = 12 },
                AxisY = new AxisY() { AxisLabel = "", Pen = new Pen(Brushes.DarkGray, 0.5), FontSize = 12 },
                Margin = new Margin(20, 30, 10, 40)
            };
            return area;
        }
        #endregion

        #region Get Title
        public static Title GetDefaultTitleV1(int fontsize = 13, string label = "Overview: Retention time correction") {
            return new Title() { FontSize = fontsize, Label = label };
        }
        #endregion

        #region Line charts
        public static SeriesList GetLineChartV1SeriesList(List<List<float[]>> targetListList, List<SolidColorBrush> brushes = null)
        {
            var slist = new SeriesList();
            for (var i = 0; i < targetListList.Count; i++)
            {
                var targetList = targetListList[i];
                var brush = Brushes.Blue;
                if (brushes != null && brushes[i] != null)
                {
                    brush = brushes[i];
                }
                var s = new Series()
                {
                    ChartType = ChartType.Line,
                    MarkerType = MarkerType.None,
                    MarkerSize = new System.Windows.Size(2, 2),
                    Brush = brush,
                    Pen = new Pen(brush, 1.0)
                };
                foreach (var value in targetList)
                {
                    s.AddPoint(value[0], value[1]);
                }
                if (s.Points.Count > 0) slist.Series.Add(s);
            }
            return slist;
        }

        public static DrawVisual GetLineChartV1(List<List<float[]>> targetListList) {
            var area = GetDefaultAreaV1();
            var title = GetDefaultTitleV1();
            var slist = GetLineChartV1SeriesList(targetListList);
            return new DrawVisual(area, title, slist);
        }
        #endregion

        #region Chromatogram
        public static Legend GetChromatogramLegend(string text)
        {
            var l = new Legend()
            {
                Text = text,
                IsVisible = true,
            };
            return l;
        }

        public static SeriesList GetChromatogramSeriesListV1(List<List<float[]>> targetListList, List<SolidColorBrush> brushes = null, List<string> legends = null)
        {
            var slist = new SeriesList();
            for (var i = 0; i < targetListList.Count; i++)
            {
                var targetList = targetListList[i];
                var brush = Brushes.Blue;
                if (brushes != null && brushes[i] != null)
                {
                    brush = brushes[i];
                }
                var s = new Series()
                {
                    ChartType = ChartType.Line,
                    MarkerType = MarkerType.None,
                    Brush = brush,
                    Pen = new Pen(brush, 1.0),
                };
                if(legends != null && legends[i] != null)
                {
                    s.Legend = GetChromatogramLegend(legends[i]);
                }
                foreach (var value in targetList)
                {
                    s.AddPoint(value[0], value[1]);
                }
                if (s.Points.Count > 0) slist.Series.Add(s);
            }
            return slist;
        }

        public static List<List<float[]>> SetDemoListList()
        {
            var s = new List<List<float[]>>();
            var s2 = new List<float[]>();
            for (var i = 0; i < 10; i++)
            {
                s2.Add(new float[2] { i, i });
            }
            s.Add(s2);
            return s;
        }

        public static DrawVisual GetSimpleChromatogramV1()
        {
            var area = GetDefaultAreaV1("Retention time (min)", "Intensity");
            var title = GetDefaultTitleV1(13, "MS2 chromatogram");
            var slist = GetChromatogramSeriesListV1(SetDemoListList());
            return new DrawVisual(area, title, slist);
        }

        public static DrawVisual GetSimpleChromatogramV1(List<List<float[]>> targetListList, string titleText = "MS2 chromatogram")
        {
            var area = GetDefaultAreaV1("Retention time (min)", "Intensity");
            var title = GetDefaultTitleV1(13, titleText);
            var slist = GetChromatogramSeriesListV1(targetListList);
            return new DrawVisual(area, title, slist);
        }

        public static DrawVisual GetChromatogramV1(List<List<float[]>> targetListList, string titleText = "MS2 chromatogram")
        {
            var area = GetDefaultAreaV1("Retention time (min)", "Intensity");
            var title = GetDefaultTitleV1(13, titleText);
            var slist = GetChromatogramSeriesListV1(targetListList);

            return new DrawVisual(area, title, slist);
        }

        #endregion


        #region Common Methods
        public static SolidColorBrush CombineAlphaAndColor(double opacity, SolidColorBrush baseBrush) {
            Color color = baseBrush.Color;
            SolidColorBrush returnSolidColorBrush;

            // Deal with )pacity
            if (opacity > 1.0)
                opacity = 1.0;

            if (opacity < 0.0)
                opacity = 0.0;

            // Get the Hex value of the Alpha Chanel (Opacity)
            byte a = (byte)(Convert.ToInt32(255 * opacity));

            try {
                byte r = color.R;
                byte g = color.G;
                byte b = color.B;

                returnSolidColorBrush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
            catch {
                returnSolidColorBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            }
            return returnSolidColorBrush;
        }

        public static double RoundUp(double dValue, int iDigits) {
            double dCoef = System.Math.Pow(10, iDigits);

            return dValue > 0 ? System.Math.Ceiling(dValue * dCoef) / dCoef :
                                System.Math.Floor(dValue * dCoef) / dCoef;
        }

        public static double RoundDown(double dValue, int iDigits) {
            double dCoef = System.Math.Pow(10, iDigits);

            return dValue > 0 ? System.Math.Floor(dValue * dCoef) / dCoef :
                                System.Math.Ceiling(dValue * dCoef) / dCoef;
        }
        #endregion

        #region Tree calculation
        public static List<XY> CalculateTreeCoordinate(DirectedTree tree)
        {
            if (tree.Count == 0) return new List<XY>();
            var xys = Enumerable.Range(0, tree.Count).Select(_ => new XY()).ToList();
            var root = tree.Root;
            var leaves = new HashSet<int>(tree.Leaves);
            var counter = 0;
            tree.PostOrder(root, e => 
                xys[e.To].X = tree[e.To].Count() == 0 ? counter++
                                                      : tree[e.To].Select(e_ => xys[e_.To].X).Average()
            );
            xys[root].X = tree[root].Count() == 0 ? counter++
                                                  : tree[root].Select(e_ => xys[e_.To].X).Average();
            tree.PreOrder(root, e => xys[e.To].Y = xys[e.From].Y + (float)e.Distance);
            var ymax = xys.Max(xy => xy.Y);
            foreach (var xy in xys)
            {
                xy.Y = ymax - xy.Y;
            }

            return xys;
        }
        #endregion
    }
}
