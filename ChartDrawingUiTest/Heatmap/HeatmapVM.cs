using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Rfx.Riken.OsakaUniv;

namespace ChartDrawingUiTest.Heatmap
{
    public class HeatmapVM : ViewModelBase
    {
        #region Properties
        public double[,] DataMatrix { get; }

        public double MinX
        {
            get => xDisplayMin;
            set
            {
                if (xDisplayMin == value) return;
                xDisplayMin = value;
                OnPropertyChanged();
            }
        }
        public double MaxX
        {
            get => xDisplayMax;
            set
            {
                if (xDisplayMax == value) return;
                xDisplayMax = value;
                OnPropertyChanged();
            }
        }
        public double MinY
        {
            get => yDisplayMin;
            set
            {
                if (yDisplayMin == value) return;
                yDisplayMin = value;
                OnPropertyChanged();
            }
        }
        public double MaxY
        {
            get => yDisplayMax;
            set
            {
                if (yDisplayMax == value) return;
                yDisplayMax = value;
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<double> XPositions { get; }
        public IReadOnlyList<double> YPositions { get; }
        public IReadOnlyList<string> XLabels { get; }
        public IReadOnlyList<string> YLabels { get; }
        #endregion

        public HeatmapVM(double[,] dataMatrix, IReadOnlyList<string> xlabels, IReadOnlyList<string> ylabels)
        {
            DataMatrix = dataMatrix;
            XLabels = xlabels;
            YLabels = ylabels;
        }

        public HeatmapVM(double[,] dataMatrix)
            : this(dataMatrix, new string[] { }, new string[] { }) {}

        public HeatmapVM() // sample
        {
            DataMatrix = new double[,]
            {
                {1, 2, 0, 4, 9},
                {4, 10, 6, 10, 1 },
                {17, 3, 9, 20, 2 },
                {10, 1, 12, 15, 9 }
            };
        }

        #region private member
        private double xDisplayMin;
        private double xDisplayMax;
        private double yDisplayMin;
        private double yDisplayMax;
        #endregion

        protected new void OnPropertyChanged([CallerMemberName]string propertyName = null) =>
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }
}
