using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using CompMs.Graphics.Core.LineChart;
using Rfx.Riken.OsakaUniv;

namespace ChartDrawingUiTest.LineChart
{
    internal class LineChartVM : ViewModelBase
    {
        public DrawingLineChart DrawingLineChart
        {
            get => drawingLineChart;
            set
            {
                var tmp = drawingLineChart;
                if (SetProperty(ref drawingLineChart, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged("DrawingLineChart");
                    if (drawingLineChart != null)
                        drawingLineChart.PropertyChanged += (s, e) => OnPropertyChanged("DrawingLineChart");
                }
            }
        }
        private DrawingLineChart drawingLineChart;

        public LineChartVM()
        {
            var xs = Enumerable.Range(0, 20).Select(x => (double)x).ToArray();
            var ys = xs.Select(x => Math.Pow(10-x, 2) + 10).ToArray();
            DrawingLineChart = new DrawingLineChart()
            {
                XPositions = xs,
                YPositions = ys,
            };
        }

        bool SetProperty<T, U>(ref U property, T value, [CallerMemberName] string propertyname = null) where T : U
        {
            if (value.Equals(property)) return false;
            property = value;
            OnPropertyChanged(propertyname);
            return true;
        }       
    }
}
