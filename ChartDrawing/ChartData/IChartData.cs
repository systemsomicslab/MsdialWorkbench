using System;
using System.ComponentModel;
using System.Windows;
using System.Runtime.CompilerServices;

namespace CompMs.Graphics.Core.Base
{
    public interface IChartData : INotifyPropertyChanged
    {
        void UpdateGraphRange(Point p1, Point p2);
        void Reset();
    }


    public class DefaultChartData : IChartData
    {
        public double MinX
        {
            set => DataMinX = (DataMaxX - DataMinX) * value + DataMinX;
        }
        public double MaxX
        {
            set => DataMaxX = (DataMaxX - DataMinX) * value + DataMinX;
        }
        public double MinY
        {
            set => DataMinY = (DataMaxY - DataMinY) * value + DataMinY;
        }
        public double MaxY
        {
            set => DataMinY = (DataMaxY - DataMinY) * value + DataMinY;
        }

        public double LimitMinX { get; protected set; } = float.MinValue;
        public double LimitMaxX { get; protected set; } = float.MaxValue;
        public double LimitMinY { get; protected set; } = float.MinValue;
        public double LimitMaxY { get; protected set; } = float.MaxValue;
        public double LimitMinZ { get; protected set; } = float.MinValue;
        public double LimitMaxZ { get; protected set; } = float.MaxValue;

        public double DataMinX
        {
            get => dataMinX;
            set
            {
                if (value == dataMinX) return;
                dataMinX = Math.Max(value, LimitMinX);
                Console.WriteLine("DataMinX changed.");
                OnPropertyChanged();
            }
        }
        public double DataMaxX
        {
            get => dataMaxX;
            set
            {
                if (value == dataMaxX) return;
                dataMaxX = Math.Min(value, LimitMaxX);
                OnPropertyChanged();
            }
        }
        public double DataMinY
        {
            get => dataMinY;
            set
            {
                if (value == dataMinY) return;
                dataMinY = Math.Max(value, LimitMinY);
                OnPropertyChanged();
            }
        }
        public double DataMaxY
        {
            get => dataMaxY;
            set
            {
                if (value == dataMaxY) return;
                dataMaxY = Math.Min(value, LimitMaxY);
                OnPropertyChanged();
            }
        }
        public double DataMinZ
        {
            get => dataMinZ;
            set
            {
                if (value == dataMinZ) return;
                dataMinZ = Math.Max(value, LimitMinZ);
                OnPropertyChanged();
            }
        }
        public double DataMaxZ
        {
            get => dataMaxZ;
            set
            {
                if (value == dataMaxZ) return;
                dataMaxZ = Math.Min(value, LimitMaxZ);
                OnPropertyChanged();
            }
        }
        double dataMinX;
        double dataMaxX;
        double dataMinY;
        double dataMaxY;
        double dataMinZ;
        double dataMaxZ;

        public DefaultChartData()
        {
            DataMinX = LimitMinX; DataMaxX = LimitMaxX;
            DataMinY = LimitMinY; DataMaxY = LimitMaxY;
            DataMinZ = LimitMinZ; DataMaxZ = LimitMaxZ;
        }

        virtual public void Reset()
        {
            DataMinX = LimitMinX; DataMaxX = LimitMaxX;
            DataMinY = LimitMinY; DataMaxY = LimitMaxY;
            DataMinZ = LimitMinZ; DataMaxZ = LimitMaxZ;
        }
        virtual public void UpdateGraphRange(Point p1, Point p2)
        {
            MinX = Math.Min(p1.X, p2.X);
            MaxX = Math.Max(p1.X, p2.X);
            MinY = Math.Min(p1.Y, p2.Y);
            MaxY = Math.Max(p1.Y, p2.Y);
        }

        void IChartData.Reset() => Reset();
        void IChartData.UpdateGraphRange(Point p1, Point p2) => UpdateGraphRange(p1, p2);

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);
        void OnPropertyChanged([CallerMemberName]string propertyname = null) => OnPropertyChanged(new PropertyChangedEventArgs(propertyname));
    }
}
