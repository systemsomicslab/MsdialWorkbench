using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Msdial.Heatmap
{
    public class HeatmapBean : INotifyPropertyChanged
    {
        #region Properties
        public double[,] DataMatrix { get; }
        public double ValueMin
        {
            get => valueMin;
            set
            {
                if (valueMin == value) return;
                valueMin = value;
                OnPropertyChanged();
            }
        }
        public double ValueMax
        {
            get => valueMax;
            set
            {
                if (valueMax == value) return;
                valueMax = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<double> XPositions { get; }
        public ObservableCollection<double> YPositions { get; }
        public ObservableCollection<double> XBorders { get; }
        public ObservableCollection<double> YBorders { get; }
        public double XDisplayMin {
            get => xDisplayMin;
            set
            {
                if (xDisplayMin == value) return;
                xDisplayMin = value;
                OnPropertyChanged();
            }
        }
        public double XDisplayMax {
            get => xDisplayMax;
            set
            {
                if (xDisplayMax == value) return;
                xDisplayMax = value;
                OnPropertyChanged();
            }
        }
        public double YDisplayMin {
            get => yDisplayMin;
            set
            {
                if (yDisplayMin == value) return;
                yDisplayMin = value;
                OnPropertyChanged();
            }
        }
        public double YDisplayMax {
            get => yDisplayMax;
            set
            {
                if (yDisplayMax == value) return;
                yDisplayMax = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<string> XLabels { get; set; }
        public ObservableCollection<string> YLabels { get; set; }
        public Color LowColor
        {
            get => lowColor;
            set
            {
                if (lowColor == value) return;
                lowColor = value;
                OnPropertyChanged();
            }
        }
        public Color HighColor
        {
            get => highColor;
            set
            {
                if (highColor == value) return;
                highColor = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public HeatmapBean(double[,] dataMatrix, IEnumerable<string> xlabels, IEnumerable<string> ylabels)
        {
            DataMatrix = dataMatrix;

            var valuemin = DataMatrix.Cast<double>().Min();
            var valuemax = DataMatrix.Cast<double>().Max();
            (double rmin, double rmax) = roundValues(valuemin, valuemax);
            valueMin = rmin;
            valueMax = rmax;

            XPositions = new ObservableCollection<double>(
                Enumerable.Range(0, DataMatrix.GetLength(0)).Select(i => i + 0.5)
            );
            YPositions = new ObservableCollection<double>(
                Enumerable.Range(0, DataMatrix.GetLength(1)).Select(i => i + 0.5)
            );
            XBorders = new ObservableCollection<double>(
                Enumerable.Range(0, XPositions.Count + 1).Select(e => (double)e)
            );
            YBorders = new ObservableCollection<double>(
                Enumerable.Range(0, YPositions.Count + 1).Select(e => (double)e)
            );
            XDisplayMin = XBorders.Min();
            XDisplayMax = XBorders.Max();
            YDisplayMin = YBorders.Min();
            YDisplayMax = YBorders.Max();

            XLabels = new ObservableCollection<string>(xlabels);
            YLabels = new ObservableCollection<string>(ylabels);
        }

        static private (double, double) roundValues(double min, double max)
        {
            var d = Math.Pow(10, Math.Floor(Math.Log10(max - min)));
            return (Math.Floor(min / d) * d, Math.Ceiling(max / d) * d);
        }

        #region fields
        double valueMin;
        double valueMax;
        double xDisplayMin;
        double xDisplayMax;
        double yDisplayMin;
        double yDisplayMax;
        Color lowColor;
        Color highColor;
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName]string propertyName= null) =>
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) =>
            PropertyChanged?.Invoke(this, e);
        #endregion
    }
}
