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
        private DrawingContinuousVerticalAxis drawingYAxis;

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
        private DrawingContinuousHorizontalAxis drawingXAxis;

        public double X
        {
            get => x;
            set => SetProperty(ref x, value);
        }
        private double x;
        public double Y
        {
            get => y;
            set => SetProperty(ref y, value);
        }
        private double y;
        public double Width
        {
            get => width;
            set => SetProperty(ref width, value);
        }
        private double width;
        public double Height
        {
            get => height;
            set => SetProperty(ref height, value);
        }
        private double height;

        void OnChartPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Rect area;
            switch (e.PropertyName)
            {
                case "DrawingLineChart":
                    X = DrawingLineChart.ChartArea.X;
                    Y = DrawingLineChart.ChartArea.Y;
                    Width = DrawingLineChart.ChartArea.Width;
                    Height = DrawingLineChart.ChartArea.Height;
                    break;
                case "DrawingXAxis":
                    X = DrawingXAxis.ChartArea.X;
                    Width = DrawingXAxis.ChartArea.Width;
                    break;
                case "DrawingYAxis":
                    Y = DrawingYAxis.ChartArea.Y;
                    Height = DrawingYAxis.ChartArea.Height;
                    break;
                case "X":
                    if (DrawingLineChart != null)
                    {
                        area = DrawingLineChart.ChartArea;
                        area.X = X;
                        DrawingLineChart.ChartArea = area;
                    }
                    if (DrawingYAxis != null)
                    {
                        area = DrawingXAxis.ChartArea;
                        area.X = X;
                        DrawingXAxis.ChartArea = area;
                    }
                    break;
                case "Y":
                    if (DrawingLineChart != null)
                    {
                        area = DrawingLineChart.ChartArea;
                        area.Y = Y;
                        DrawingLineChart.ChartArea = area;
                    }
                    if (DrawingYAxis != null)
                    {
                        area = DrawingYAxis.ChartArea;
                        area.Y = Y;
                        DrawingYAxis.ChartArea = area;
                    }
                    break;
                case "Width":
                    if (DrawingLineChart != null)
                    {
                        area = DrawingLineChart.ChartArea;
                        area.Width = Width;
                        DrawingLineChart.ChartArea = area;
                    }
                    if (DrawingXAxis != null)
                    {
                        area = DrawingXAxis.ChartArea;
                        area.Width = Width;
                        DrawingXAxis.ChartArea = area;
                    }
                    break;
                case "Height":
                    if (DrawingLineChart != null)
                    {
                        area = DrawingLineChart.ChartArea;
                        area.Height = Height;
                        DrawingLineChart.ChartArea = area;
                    }
                    if (DrawingYAxis != null)
                    {
                        area = DrawingYAxis.ChartArea;
                        area.Height = Height;
                        DrawingYAxis.ChartArea = area;
                    }
                    break;
            }
        }


        public LineChartVM()
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

            PropertyChanged += OnChartPropertyChanged;
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
