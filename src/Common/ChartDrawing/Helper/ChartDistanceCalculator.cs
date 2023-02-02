using CompMs.Common.DataStructure;
using CompMs.Graphics.Core.Base;
using System;

namespace CompMs.Graphics.Helper
{
    internal sealed class ChartDistanceCalculator : IDistanceCalculator
    {
        public ChartDistanceCalculator(ChartBaseControl chart) {
            _chart = chart;
        }

        private readonly ChartBaseControl _chart;

        public double Distance(double[] xs, double[] ys) {
            return Math.Sqrt(
                Math.Pow((xs[0] - ys[0]) / _chart.HorizontalAxis.Range.Delta * _chart.ActualWidth, 2) +
                Math.Pow((xs[1] - ys[1]) / _chart.VerticalAxis.Range.Delta * _chart.ActualHeight, 2));
        }

        public double RoughDistance(double[] xs, double[] ys, int i) {
            if (i == 0) {
                return Math.Abs((xs[0] - ys[0]) / _chart.HorizontalAxis.Range.Delta * _chart.ActualWidth);
            }
            else {
                return Math.Abs((xs[1] - ys[1]) / _chart.VerticalAxis.Range.Delta * _chart.ActualHeight);
            }
        }
    }
}
