using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Common.DataStructure;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Interaction logic for HcaResultWin.xaml
    /// </summary>
    public partial class HcaResultWin : Window
    {
        public HcaResultWin(MultivariateAnalysisResult result)
        {
            InitializeComponent();
            this.DataContext = new HcaResultVM(result);
        }
    }

    class HcaResultVM : ViewModelBase
    {
        public DirectedTree XDendrogram
        {
            get => xDendrogram;
            set
            {
                if (xDendrogram == value) return;
                xDendrogram = value;
                OnPropertyChanged("XDendrogram");
            }
        }

        public DirectedTree YDendrogram
        {
            get => yDendrogram;
            set
            {
                if (yDendrogram == value) return;
                yDendrogram = value;
                OnPropertyChanged("YDendrogram");
            }
        }

        public double[,] DataMatrix
        {
            get => dataMatrix;
            set
            {
                if (dataMatrix == value) return;
                dataMatrix = value;
                OnPropertyChanged("DataMatrix");
            }
        }

        public IReadOnlyList<string> XLabels { get; }
        public IReadOnlyList<string> YLabels { get; }

        public HcaResultVM(MultivariateAnalysisResult result)
        {
            var transposeMatrix = new double[
                result.StatisticsObject.XDataMatrix.GetLength(1),
                result.StatisticsObject.XDataMatrix.GetLength(0)
                ];
            for (int i = 0; i < result.StatisticsObject.XDataMatrix.GetLength(0); ++i)
                for (int j = 0; j < result.StatisticsObject.XDataMatrix.GetLength(1); ++j)
                    transposeMatrix[j, i] = result.StatisticsObject.XDataMatrix[i, j];
            DataMatrix = transposeMatrix;
            XDendrogram = result.XDendrogram;
            YDendrogram = result.YDendrogram;
            XLabels = result.StatisticsObject.XLabels;
            YLabels = result.StatisticsObject.YLabels;
        }

        private DirectedTree xDendrogram;
        private DirectedTree yDendrogram;
        private double[,] dataMatrix;
    }
}
