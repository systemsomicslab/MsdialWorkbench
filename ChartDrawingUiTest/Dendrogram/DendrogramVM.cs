using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Common.DataStructure;
using Rfx.Riken.OsakaUniv;

namespace ChartDrawingUiTest.Dendrogram
{
    public class DendrogramVM : ViewModelBase
    {
        #region Properties
        public DirectedTree Dendrogram { get; }

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
        public IReadOnlyList<string> Labels { get; }
        public IReadOnlyList<double> XLabelPositions { get; }
        #endregion

        public DendrogramVM(DirectedTree dendrogram, IReadOnlyList<string> labels)
        {
            Dendrogram = dendrogram;
            Labels = labels;
        }

        public DendrogramVM(DirectedTree dendrogram)
            : this(dendrogram, new string[] { }) {}

        public DendrogramVM() // sample
        {
            Dendrogram = new DirectedTree(11);
            Dendrogram.AddEdge(6, 0, 3);
            Dendrogram.AddEdge(6, 2, 3);
            Dendrogram.AddEdge(7, 4, 5);
            Dendrogram.AddEdge(7, 3, 5);
            Dendrogram.AddEdge(8, 6, 4);
            Dendrogram.AddEdge(8, 7, 2);
            Dendrogram.AddEdge(9, 1, 9);
            Dendrogram.AddEdge(9, 8, 2);
            Dendrogram.AddEdge(9, 5, 9);
            Dendrogram.AddEdge(10, 9, 3);
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
