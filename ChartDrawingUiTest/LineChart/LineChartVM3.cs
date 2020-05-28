using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using CompMs.Graphics.Core.Base;

namespace ChartDrawingUiTest.LineChart
{
    internal class LineChartVM3 : INotifyPropertyChanged
    {
        public ObservableCollection<DataSeries> Serieses
        {
            get => serieses;
            set => SetProperty(ref serieses, value);
        }

        private ObservableCollection<DataSeries> serieses;

        public LineChartVM3()
        {
            var xs = Enumerable.Range(0, 6000).Select(x => x / 1000d);
            var ss = new List<DataSeries>(6);
            ss.Add(new DataSeries() { Datas = new ObservableCollection<DataPoint>(xs.Select(x => new DataPoint() { X = x, Y = Math.Sin(x) })), Type = 0 });
            ss.Add(new DataSeries() { Datas = new ObservableCollection<DataPoint>(xs.Select(x => new DataPoint() { X = x, Y = Math.Cos(x) })), Type = 1 });
            ss.Add(new DataSeries() { Datas = new ObservableCollection<DataPoint>(xs.Select(x => new DataPoint() { X = x, Y = x * Math.Sin(x) })), Type = 2 });
            ss.Add(new DataSeries() { Datas = new ObservableCollection<DataPoint>(xs.Select(x => new DataPoint() { X = x, Y = x * Math.Cos(x) })), Type = 3 });
            ss.Add(new DataSeries() { Datas = new ObservableCollection<DataPoint>(xs.Select(x => new DataPoint() { X = x, Y = - x * Math.Sin(x) })), Type = 4 });
            ss.Add(new DataSeries() { Datas = new ObservableCollection<DataPoint>(xs.Select(x => new DataPoint() { X = x, Y = - x * Math.Cos(x) })), Type = 5 });
            Serieses = new ObservableCollection<DataSeries>(ss);
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
