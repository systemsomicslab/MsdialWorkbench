using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

using PlottingControls.Base;

namespace PlottingControls.Heatmap
{
    public class HeatmapControl : PlottingBase
    {
        #region Property
        public double[,] DataMatrix
        {
            get => (double[,])GetValue(DataMatrixProperty);
            set => SetValue(DataMatrixProperty, value);
        }
        public static readonly DependencyProperty DataMatrixProperty = DependencyProperty.Register(
            "DataMatrix", typeof(double[,]), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public LinearGradientBrush Brush
        {
            get => (LinearGradientBrush)GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            "Brush", typeof(LinearGradientBrush), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(
                new LinearGradientBrush(Colors.Blue, Colors.Red, new Point(0, 0), new Point(0, 1)),
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );
        public IReadOnlyList<double> XBorders => xBorders = xBorders ?? initBorders(XPositions);
        public IReadOnlyList<double> YBorders => yBorders = yBorders ?? initBorders(YPositions);
        List<double> xBorders;
        List<double> yBorders;

        #endregion

        public HeatmapControl() { }

        protected override void DrawChart(DrawingContext drawingContext)
        {
            if(DataMatrix != null)
                HeatmapPainter.Draw(
                    drawingContext,
                    DataMatrix,
                    XBorders, YBorders,
                    XDisplayMin, XDisplayMax,
                    YDisplayMin, YDisplayMax,
                    ZDisplayMin, ZDisplayMax,
                    new Point(0,0), new Vector(ActualWidth, ActualHeight),
                    Brush
                );
        }

        static private (double, double) roundValues(double min, double max)
        {
            var d = Math.Pow(10, Math.Floor(Math.Log10(max - min)));
            return (Math.Floor(min / d) * d, Math.Ceiling(max / d) * d);
        }

        private void initZRange()
        {
            var zmin = DataMatrix.Cast<double>().Min();
            var zmax = DataMatrix.Cast<double>().Max();
            (double zrmin, double zrmax) = roundValues(zmin, zmax);
            ZDisplayMin = zrmin;
            ZDisplayMax = zrmax;
        }
        
        private List<double> initBorders(IReadOnlyList<double> positions)
        {
            var borders = new List<double>(positions.Count + 1);
            for(int i= 1; i< positions.Count; ++i)
            {
                borders.Add((positions[i - 1] + positions[i]) / 2);
            }
            if (borders.Count >= 1)
            {
                borders.Add(positions.Last() * 2 - borders.Last());
                borders.Add(positions.First() * 2 - borders.First());
            }
            borders.Sort();
            return borders;
        }

        #region mouse event
        protected override void ResetDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                initZRange();
                XDisplayMin = XBorders.Min();
                XDisplayMax = XBorders.Max();
                YDisplayMin = YBorders.Min();
                YDisplayMax = YBorders.Max();
            }
        }
        #endregion
    }
}
