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
        #region Property
        public Size RenderSize
        {
            get => renderSize;
            set => SetProperty(ref renderSize, value);
        }
        
        public Rect ChartArea
        {
            get => chartArea;
            set => SetProperty(ref chartArea, value);
        }

        public double ChartX
        {
            get => ChartArea.X;
            set
            {
                if (ChartArea.X == value) return;
                ChartArea = new Rect(value, ChartArea.Y, ChartArea.Width, ChartArea.Height);
                OnPropertyChanged("ChartX");
                OnPropertyChanged("ChartArea");
            }
        }

        public double ChartY
        {
            get => ChartArea.Y;
            set
            {
                if (ChartArea.Y == value) return;
                ChartArea = new Rect(ChartArea.X, value, ChartArea.Width, ChartArea.Height);
                OnPropertyChanged("ChartY");
                OnPropertyChanged("ChartArea");
            }
        }

        public double ChartWidth
        {
            get => ChartArea.Width;
            set
            {
                if (ChartArea.Width == value) return;
                ChartArea = new Rect(ChartArea.X, ChartArea.Y, value, ChartArea.Height);
                OnPropertyChanged("ChartWidth");
                OnPropertyChanged("ChartArea");
            }
        }

        public double ChartHeight
        {
            get => ChartArea.Height;
            set
            {
                if (ChartArea.Height == value) return;
                ChartArea = new Rect(ChartArea.X, ChartArea.Y, ChartArea.Width, value);
                OnPropertyChanged("ChartHeight");
                OnPropertyChanged("ChartArea");
            }
        }

        public Rect InitialArea
        {
            get => initialArea;
            set => SetProperty(ref initialArea, value);
        }

        public double InitialX
        {
            get => InitialArea.X;
            set
            {
                if (InitialArea.X == value) return;
                InitialArea = new Rect(value, InitialArea.Y, InitialArea.Width, InitialArea.Height);
                OnPropertyChanged("InitialX");
                OnPropertyChanged("InitialArea");
            }
        }

        public double InitialY
        {
            get => InitialArea.Y;
            set
            {
                if (InitialArea.Y == value) return;
                InitialArea = new Rect(InitialArea.X, value, InitialArea.Width, InitialArea.Height);
                OnPropertyChanged("InitialY");
                OnPropertyChanged("InitialArea");
            }
        }

        public double InitialWidth
        {
            get => InitialArea.Width;
            set
            {
                if (InitialArea.Width == value) return;
                InitialArea = new Rect(InitialArea.X, InitialArea.Y, value, InitialArea.Height);
                OnPropertyChanged("InitialWidth");
                OnPropertyChanged("InitialArea");
            }
        }

        public double InitialHeight
        {
            get => InitialArea.Height;
            set
            {
                if (InitialArea.Height == value) return;
                InitialArea = new Rect(InitialArea.X, InitialArea.Y, InitialArea.Width, value);
                OnPropertyChanged("InitialHeight");
                OnPropertyChanged("InitialArea");
            }
        }
        #endregion

        #region field
        Size renderSize;
        Rect chartArea;
        Rect initialArea;
        #endregion

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

        #region notify property change
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
            => PropertyChanged?.Invoke(this, e);

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyname = null)
            => OnPropertyChanged(new PropertyChangedEventArgs(propertyname));

        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyname = null)
        {
            if (value.Equals(property)) return false;
            property = value;
            OnPropertyChanged(propertyname);
            return true;
        }
        #endregion
    }
}
