using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using Rfx.Riken.OsakaUniv;

namespace PlottingControlsTests.Heatmap
{
    public class HeatmapTestVM : ViewModelBase
    {
        #region Properties
        public double[,] DataMatrix { get; }
        public ObservableCollection<double> XPositions { get; }
        public ObservableCollection<double> YPositions { get; }
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
        public double ZDisplayMin
        {
            get => zDisplayMin;
            set
            {
                if (zDisplayMin == value) return;
                zDisplayMin = value;
                OnPropertyChanged();
            }
        }
        public double ZDisplayMax
        {
            get => zDisplayMax;
            set
            {
                if (zDisplayMax == value) return;
                zDisplayMax = value;
                OnPropertyChanged();
            }
        }
        double zDisplayMin;
        double zDisplayMax;
        double xDisplayMin;
        double xDisplayMax;
        double yDisplayMin;
        double yDisplayMax;

        public ObservableCollection<string> XLabels { get; set; }
        public ObservableCollection<string> YLabels { get; set; }
        #endregion

        public HeatmapTestVM(double[,] dataMatrix, IReadOnlyList<string> xlabels, IReadOnlyList<string> ylabels)
        {
            DataMatrix = dataMatrix;
            XPositions = new ObservableCollection<double>(Enumerable.Range(0, DataMatrix.GetLength(0)).Select(i => (double)i));
            YPositions = new ObservableCollection<double>(Enumerable.Range(0, DataMatrix.GetLength(1)).Select(i => (double)i));

            XLabels = new ObservableCollection<string>(xlabels);
            YLabels = new ObservableCollection<string>(ylabels);
        }
        public HeatmapTestVM(double[,] dataMatrix)
            : this(dataMatrix, new string[] { }, new string[] { }) { }

        #region INotifyPropertyChanged
        protected new void OnPropertyChanged([CallerMemberName]string propertyName= null) =>
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        #endregion
    }
}
