using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

        private ObservableCollection<DataPoint> series;

        public ScatterVM2()
        {
            var xs = Enumerable.Range(0, 6000).Select(x => new DataPoint() { X = x / 1000d, Y = Math.Sin(x / 1000d), Type = x / 2000});
            Series = new ObservableCollection<DataPoint>(xs);
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
