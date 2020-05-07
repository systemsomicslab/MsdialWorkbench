using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Runtime.CompilerServices;

using CompMs.Graphics.Core.Base;
using CompMs.Common.DataStructure;

namespace CompMs.Graphics.Core.Dendrogram
{
    public class DrawingDendrogram : IDrawingChart, INotifyPropertyChanged
    {
        public DirectedTree Tree
        {
            get => tree;
            set => SetProperty(ref tree, value);
        }
        DirectedTree tree;
        public IReadOnlyList<XY> Series
        {
            get => series;
            set => SetProperty(ref series, value as List<XY> ?? new List<XY>(value));
        }
        List<XY> series;

        public Size RenderSize
        {
            get => new Size(width, height);
            set
            {
                SetProperty(ref width, value.Width);
                SetProperty(ref height, value.Height);
                OnPropertyChanged("Height");
                OnPropertyChanged("Width");
            }
        }
        public double Height
        {
            get => height;
            set
            {
                SetProperty(ref height, value);
                OnPropertyChanged("RenderSize");
            }
        }
        double height;
        public double Width
        {
            get => width;
            set
            {
                SetProperty(ref width, value);
                OnPropertyChanged("RenderSize");
            }
        }
        double width;
        public Rect ChartArea
        {
            get => chartArea;
            set => SetProperty(ref chartArea, value);
        }
        Rect chartArea;

        DendrogramElement element = null;
        readonly Pen graphLine = new Pen(Brushes.Black, 1);

        public DrawingDendrogram() : base()
        {
            graphLine.Freeze();
        }

        public Drawing CreateChart()
        {
            if (element == null)
            {
                if (Tree == null || Series == null)
                    return new GeometryDrawing();
                element = new DendrogramElement(
                    Tree, Series.Select(xy => (double)xy.X).ToArray(), Series.Select(xy => (double)xy.Y).ToArray()
                    );
            }
            if (ChartArea == default)
            {
                ChartArea = element.ElementArea;
                ChartArea = new Rect(
                    ChartArea.X - ChartArea.Width * 0.05, ChartArea.Y,
                    ChartArea.Width * 1.1, ChartArea.Height * 1.05
                    );
            }
            var geometry = element.GetGeometry(ChartArea, RenderSize);
            geometry.Transform = new ScaleTransform(1, -1, 0, RenderSize.Height / 2);
            return new GeometryDrawing(Brushes.Black, graphLine, geometry);
        }

        public Point RealToImagine(Point point)
        {
            return new Point(
                (point.X / RenderSize.Width * ChartArea.Width + ChartArea.X),
                (ChartArea.Bottom - point.Y / RenderSize.Height * ChartArea.Height)
                );
        }

        public Point ImagineToReal(Point point)
        {
            return new Point(
                (point.X - ChartArea.X) / ChartArea.Width * RenderSize.Width,
                (ChartArea.Bottom - point.Y) / ChartArea.Height * RenderSize.Height
                );
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
            => PropertyChanged?.Invoke(this, e);

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyname = null)
            => OnPropertyChanged(new PropertyChangedEventArgs(propertyname));

        void SetProperty<T, U>(ref U property, T value, [CallerMemberName] string propertyname = null) where T : U
        {
            if (value.Equals(property)) return;
            property = value;
            OnPropertyChanged(propertyname);
        }       
    }
}
