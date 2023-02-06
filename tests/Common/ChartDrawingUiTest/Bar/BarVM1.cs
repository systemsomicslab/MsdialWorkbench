using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ChartDrawingUiTest.Bar
{
    internal class BarVM1 : INotifyPropertyChanged
    {
        public class SampleData
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        public ObservableCollection<SampleData> Series
        {
            get => series;
            set => SetProperty(ref series, value);
        }

        public ObservableCollection<double> Xs
        {
            get => xs;
            set => SetProperty(ref xs, value);
        }

        public ObservableCollection<double> Ys
        {
            get => ys;
            set => SetProperty(ref ys, value);
        }

        public IBrushMapper<SampleData> Mapper { get; }

        private ObservableCollection<SampleData> series;
        private ObservableCollection<double> xs, ys;

        public BarVM1()
        {
            var ss = new List<SampleData>();
            ss.Add(new SampleData() { X = 1.0, Y = 1.0, });
            ss.Add(new SampleData() { X = 1.5, Y = 0.9, });
            ss.Add(new SampleData() { X = 2.0, Y = -.1, });
            ss.Add(new SampleData() { X = 4.0, Y = 1.5, });

            Series = new ObservableCollection<SampleData>(ss);
            Mapper = new KeyBrushMapper<SampleData>(
                ss.Zip(new[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Black, }, Tuple.Create)
                    .ToDictionary(p => p.Item1, p => (Brush)p.Item2));
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
