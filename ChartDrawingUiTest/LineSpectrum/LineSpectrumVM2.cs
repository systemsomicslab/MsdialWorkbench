using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChartDrawingUiTest.LineSpectrum
{
    internal class LineSpectrumVM2 : INotifyPropertyChanged
    {
        public ObservableCollection<DataPoint> Series
        {
            get => series;
            set => SetProperty(ref series, value);
        }
        private ObservableCollection<DataPoint> series;

        public LineSpectrumVM2()
        {
            Series = new ObservableCollection<DataPoint>(new List<DataPoint>
            {
                new DataPoint { X = 100, Y = 100},
            });
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
}
