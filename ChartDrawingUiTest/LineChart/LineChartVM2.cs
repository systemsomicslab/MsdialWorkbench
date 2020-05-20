using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

using CompMs.Graphics.Core.LineChart;
using CompMs.Graphics.Core.GraphAxis;
using Rfx.Riken.OsakaUniv;

namespace ChartDrawingUiTest.LineChart
{
    internal class LineChartVM2 : ViewModelBase
    {
        public DrawingLineChart DrawingLineChart { get; set; }

        public DrawingContinuousVerticalAxis DrawingYAxis { get; set; }

        public DrawingContinuousHorizontalAxis DrawingXAxis { get; set; }

        public LineChartVM2()
        {
            var xs = Enumerable.Range(0, 20).Select(x => (double)x).ToArray();
            var ys = xs.Select(x => Math.Pow(10-x, 5)).ToArray();
            DrawingLineChart = new DrawingLineChart()
            {
                XPositions = xs,
                YPositions = ys,
            };
            DrawingXAxis = new DrawingContinuousHorizontalAxis()
            {
                MinX = xs.Min(),
                MaxX = xs.Max(),
            };
            DrawingYAxis = new DrawingContinuousVerticalAxis()
            {
                MinY = ys.Min(),
                MaxY = ys.Max(),
            };
        }
    }
}
