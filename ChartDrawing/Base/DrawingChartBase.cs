using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public class DrawingChartBase : IDrawingChart, INotifyPropertyChanged
    {
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
        public Rect InitialArea
        {
            get => initialArea;
            set => SetProperty(ref initialArea, value);
        }
        Rect initialArea;

        public virtual Drawing CreateChart()
        {
            throw new NotImplementedException("CreateChart mehod is not implemented.");
        }

        public virtual Point RealToImagine(Point point)
        {
            return new Point(
                (point.X / RenderSize.Width * ChartArea.Width + ChartArea.X),
                (point.Y / RenderSize.Height * ChartArea.Height + ChartArea.Y)
                );
        }

        public virtual Point ImagineToReal(Point point)
        {
            return new Point(
                (point.X - ChartArea.X) / ChartArea.Width * RenderSize.Width,
                (point.Y - ChartArea.Y) / ChartArea.Height * RenderSize.Height
                );
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
            => PropertyChanged?.Invoke(this, e);

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyname = null)
            => OnPropertyChanged(new PropertyChangedEventArgs(propertyname));

        protected bool SetProperty<T, U>(ref U property, T value, [CallerMemberName] string propertyname = null) where T : U
        {
            if (value.Equals(property)) return false;
            property = value;
            OnPropertyChanged(propertyname);
            return true;
        }       
    }
}
