using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Scatter;
using CompMs.Graphics.Core.GraphAxis;
using Rfx.Riken.OsakaUniv;

namespace ChartDrawingUiTest.Scatter
{
    internal class ScatterVM : ViewModelBase
    {
        public DrawingScatter DrawingScatter
        {
            get => drawingScatter;
            set
            {
                var tmp = drawingScatter;
                if (SetProperty(ref drawingScatter, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged("DrawingScatter");
                    if (drawingScatter != null)
                        drawingScatter.PropertyChanged += (s, e) => OnPropertyChanged("DrawingScatter");
                }
            }
        }
        private DrawingScatter drawingScatter;

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
                case "DrawingScatter":
                    X = DrawingScatter.ChartArea.X;
                    Y = DrawingScatter.ChartArea.Y;
                    Width = DrawingScatter.ChartArea.Width;
                    Height = DrawingScatter.ChartArea.Height;
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
                    if (DrawingScatter != null)
                    {
                        area = DrawingScatter.ChartArea;
                        area.X = X;
                        DrawingScatter.ChartArea = area;
                    }
                    if (DrawingYAxis != null)
                    {
                        area = DrawingXAxis.ChartArea;
                        area.X = X;
                        DrawingXAxis.ChartArea = area;
                    }
                    break;
                case "Y":
                    if (DrawingScatter != null)
                    {
                        area = DrawingScatter.ChartArea;
                        area.Y = Y;
                        DrawingScatter.ChartArea = area;
                    }
                    if (DrawingYAxis != null)
                    {
                        area = DrawingYAxis.ChartArea;
                        area.Y = Y;
                        DrawingYAxis.ChartArea = area;
                    }
                    break;
                case "Width":
                    if (DrawingScatter != null)
                    {
                        area = DrawingScatter.ChartArea;
                        area.Width = Width;
                        DrawingScatter.ChartArea = area;
                    }
                    if (DrawingXAxis != null)
                    {
                        area = DrawingXAxis.ChartArea;
                        area.Width = Width;
                        DrawingXAxis.ChartArea = area;
                    }
                    break;
                case "Height":
                    if (DrawingScatter != null)
                    {
                        area = DrawingScatter.ChartArea;
                        area.Height = Height;
                        DrawingScatter.ChartArea = area;
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


        public ScatterVM()
        {
            var xs = Enumerable.Range(0, 20).Select(x => (double)x).ToArray();
            var ys = xs.Select(x => Math.Pow(10-x, 5)).ToArray();
            DrawingScatter = new DrawingScatter()
            {
                XPositions = xs,
                YPositions = ys,
                PointBrush = Brushes.DeepPink
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
