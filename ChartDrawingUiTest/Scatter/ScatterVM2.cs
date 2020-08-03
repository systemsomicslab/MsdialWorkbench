using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Scatter;

namespace ChartDrawingUiTest.Scatter
{
    internal class ScatterVM2 : INotifyPropertyChanged
    {
        public ObservableCollection<DataPoint> Series
        {
            get => series;
            set => SetProperty(ref series, value);
        }

        public Rect ScatterArea
        {
            get => scatterArea;
            set => SetProperty(ref scatterArea, value);
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

        public Rect XAxisArea
        {
            get => xAxisArea;
            set => SetProperty(ref xAxisArea, value);
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

        public Rect YAxisArea
        {
            get => yAxisArea;
            set => SetProperty(ref yAxisArea, value);
        }

        private ObservableCollection<DataPoint> series;
        private Rect scatterArea;
        private double minX;
        private double maxX;
        private Rect xAxisArea;
        private double minY;
        private double maxY;
        private Rect yAxisArea;

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ScatterArea":
                    XAxisArea = new Rect(ScatterArea.X, XAxisArea.Y, ScatterArea.Width, XAxisArea.Height);
                    YAxisArea = new Rect(YAxisArea.X, ScatterArea.Y, YAxisArea.Width, ScatterArea.Height);
                    break;
                case "XAxisArea":
                    ScatterArea = new Rect(XAxisArea.X, ScatterArea.Y, XAxisArea.Width, ScatterArea.Height);
                    break;
                case "YAxisArea":
                    ScatterArea = new Rect(ScatterArea.X, YAxisArea.Y, ScatterArea.Width, YAxisArea.Height);
                    break;
            }
        }

        public ScatterVM2()
        {
            var xs = Enumerable.Range(0, 6000).Select(x => new DataPoint() { X = x / 1000d, Y = Math.Sin(x / 1000d), Type = x / 2000});
            Series = new ObservableCollection<DataPoint>(xs);
            MinX = xs.Min(dp => dp.X);
            MaxX = xs.Max(dp => dp.X);
            MinY = xs.Min(dp => dp.Y);
            MaxY = xs.Max(dp => dp.Y);

            PropertyChanged += OnPropertyChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void RaiseProerptyChanged(string propertyname) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyname = "")
        {
            if (value.Equals(property)) return false;
            property = value;
            RaiseProerptyChanged(propertyname);
            return true;
        }
    }
}
