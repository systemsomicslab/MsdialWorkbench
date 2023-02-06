using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;


namespace ChartDrawingUiTest.LineSpectrum
{
    internal class LineSpecrumVM1 : INotifyPropertyChanged
    {
        public ObservableCollection<DataPoint> Series
        {
            get => series;
            set => SetProperty(ref series, value);
        }

        public double MinX
        {
            get => minX;
            set => SetProperty(ref minX, value);
        }

        public double MaxX
        {
            get => maxX;
            set => SetProperty(ref maxX, value);
        }

        public double MinY
        {
            get => minY;
            set => SetProperty(ref minY, value);
        }

        public double MaxY
        {
            get => maxY;
            set => SetProperty(ref maxY, value);
        }

        private ObservableCollection<DataPoint> series;
        private double minX;
        private double maxX;
        private double minY;
        private double maxY;

        public LineSpecrumVM1()
        {
            var xs = Enumerable.Range(0, 628).Select(x => new DataPoint() { X = x / 100d, Y = Math.Sin(x / 100d) });
            Series = new ObservableCollection<DataPoint>(xs);
            MinX = xs.Min(dp => dp.X);
            MaxX = xs.Max(dp => dp.X);
            MinY = xs.Min(dp => dp.Y);
            MaxY = xs.Max(dp => dp.Y);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void RaiseProerptyChanged(string propertyname) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyname = "")
        {
            if (value == null && property == null || value.Equals(property)) return false;
            property = value;
            RaiseProerptyChanged(propertyname);
            return true;
        }
    }

    internal class DataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int Type { get; internal set; }
    }
}
