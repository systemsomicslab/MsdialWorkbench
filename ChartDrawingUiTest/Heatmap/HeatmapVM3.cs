using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;

namespace ChartDrawingUiTest.Heatmap
{
    public class CategoryData
    {
        public string X { get; set; }
        public string Y { get; set; }
        public double Z { get; set; }
    }

    public class HeatmapVM3 : INotifyPropertyChanged
    {
        public ObservableCollection<CategoryData> Series
        {
            get => series;
            set => SetProperty(ref series, value);
        }

        public ObservableCollection<string> Xs
        {
            get => xs;
            set => SetProperty(ref xs, value);
        }

        public ObservableCollection<string> Ys
        {
            get => ys;
            set => SetProperty(ref ys, value);
        }

        public ObservableCollection<double> Zs
        {
            get => zs;
            set => SetProperty(ref zs, value);
        }

        public GradientBrushMapper<double> GradientBrush { get; }

        private ObservableCollection<CategoryData> series;
        private ObservableCollection<string> xs;
        private ObservableCollection<string> ys;
        private ObservableCollection<double> zs;

        public HeatmapVM3()
        {
            var s = new List<CategoryData>
            {
                new CategoryData(){ X = "A", Y = "x", Z = 1},
                new CategoryData(){ X = "B", Y = "x", Z = 2},
                new CategoryData(){ X = "C", Y = "x", Z = 0},
                new CategoryData(){ X = "D", Y = "x", Z = 4},
                new CategoryData(){ X = "E", Y = "x", Z = 9},
                new CategoryData(){ X = "A", Y = "y", Z = 4},
                new CategoryData(){ X = "B", Y = "y", Z = 10},
                new CategoryData(){ X = "C", Y = "y", Z = 6},
                new CategoryData(){ X = "D", Y = "y", Z = 10},
                new CategoryData(){ X = "E", Y = "y", Z = 1},
                new CategoryData(){ X = "A", Y = "z", Z = 17},
                new CategoryData(){ X = "B", Y = "z", Z = 3},
                new CategoryData(){ X = "C", Y = "z", Z = 9},
                new CategoryData(){ X = "D", Y = "z", Z = 20},
                new CategoryData(){ X = "E", Y = "z", Z = 2},
                new CategoryData(){ X = "A", Y = "w", Z = 10},
                new CategoryData(){ X = "B", Y = "w", Z = 1},
                new CategoryData(){ X = "C", Y = "w", Z = 12},
                new CategoryData(){ X = "D", Y = "w", Z = 15},
                new CategoryData(){ X = "E", Y = "w", Z = 9}
            };

            Series = new ObservableCollection<CategoryData>(s);
            Xs = new ObservableCollection<string>(new HashSet<string>(s.Select(d => d.X)));
            Ys = new ObservableCollection<string>(new HashSet<string>(s.Select(d => d.Y)));
            Zs = new ObservableCollection<double>(s.Select(d => d.Z));
            GradientBrush = new GradientBrushMapper<double>(Zs.Min(), Zs.Max(), new[]
            {
                new GradientStop(Colors.MediumPurple, 0d),
                new GradientStop(Colors.Yellow, 1d),
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void RaiseProerptyChanged([CallerMemberName]string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyname = "")
        {
            if (value == null && property == null || value.Equals(property)) return false;
            property = value;
            RaiseProerptyChanged(propertyname);
            return true;
        }
    }
}
