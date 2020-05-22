using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Base;
using CompMs.Graphics.Core.GraphAxis;
using CompMs.Graphics.Core.LineChart;
using CompMs.Graphics.Core.Scatter;
using Rfx.Riken.OsakaUniv;

namespace ChartDrawingUiTest.Compound
{
    internal class LineAndScatterVM2 : ViewModelBase
    {
        public DrawingChartGroup DrawingChart
        {
            get => drawingChart;
            set
            {
                var tmp = drawingChart;
                if (SetProperty(ref drawingChart, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(DrawingChart));
                    if (drawingChart != null)
                        drawingChart.PropertyChanged += (s, e) => OnPropertyChanged(nameof(DrawingChart));
                }
            }

        }

        public DrawingContinuousVerticalAxis DrawingYAxis
        {
            get => drawingYAxis;
            set
            {
                var tmp = drawingYAxis;
                if (SetProperty(ref drawingYAxis, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(DrawingYAxis));
                    if (drawingYAxis != null)
                        drawingYAxis.PropertyChanged += (s, e) => OnPropertyChanged(nameof(DrawingYAxis));
                }
            }
        }

        public DrawingContinuousHorizontalAxis DrawingXAxis
        {
            get => drawingXAxis;
            set
            {
                var tmp = drawingXAxis;
                if (SetProperty(ref drawingXAxis, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(DrawingXAxis));
                    if (drawingXAxis != null)
                        drawingXAxis.PropertyChanged += (s, e) => OnPropertyChanged(nameof(DrawingXAxis));
                }
            }
        }

        private DrawingChartGroup drawingChart;
        private DrawingContinuousVerticalAxis drawingYAxis;
        private DrawingContinuousHorizontalAxis drawingXAxis;

        public LineAndScatterVM2()
        {
            var xs = Enumerable.Range(0, 20).Select(x => (double)x).ToArray();
            var ys = xs.Select(x => Math.Pow(10-x, 5)).ToArray();
            DrawingChart = new DrawingChartGroup()
            {
                Children = new System.Collections.ObjectModel.ObservableCollection<CompMs.Graphics.Core.Base.DrawingChartBase>()
                {
                    new DrawingLineChart()
                    {
                        XPositions = xs,
                        YPositions = ys,
                    },
                     new DrawingScatter()
                    {
                        XPositions = xs,
                        YPositions = ys,
                    },
                }
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

        bool SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyname = null)
        {
            if (value.Equals(property)) return false;
            property = value;
            OnPropertyChanged(propertyname);
            return true;
        }       
    }
}
