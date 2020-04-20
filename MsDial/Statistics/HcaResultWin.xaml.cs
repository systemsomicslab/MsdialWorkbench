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

        public IReadOnlyList<string> XLabels { get; private set; }
        public IReadOnlyList<string> YLabels { get; private set; }

        public int XNumberOfData
        {
            get => xNumberOfData;
            set
            {
                if (xNumberOfData == value) return;
                xNumberOfData = value;
                SetDataLimit(Math.Max, XNumberOfData, YNumberOfData);
                OnPropertyChanged(String.Empty);
            }
        }
        public int YNumberOfData
        {
            get => yNumberOfData;
            set
            {
                if (yNumberOfData == value) return;
                yNumberOfData = value;
                SetDataLimit(Math.Max, XNumberOfData, YNumberOfData);
                OnPropertyChanged(String.Empty);
            }
        }

        public int XLabelLimit { get; set; } = 20;
        public int YLabelLimit { get; set; } = 20;

        public HcaResultVM(MultivariateAnalysisResult result_)
        {
            result = result_;
            DataMatrix = transpose(result.StatisticsObject.XDataMatrix);
            XDendrogram = result.XDendrogram;
            YDendrogram = result.YDendrogram;
            XLabels = result.StatisticsObject.YLabels;
            YLabels = result.StatisticsObject.XLabels;
        }

        public void SetDataLimit( Func<double, double, double> agg, int x_ = -1, int y_ = -1)
        {
            var m = result.StatisticsObject.XDataMatrix.GetLength(0);
            var n = result.StatisticsObject.XDataMatrix.GetLength(1);
            var x = x_ == -1 ? m : Math.Min(x_, m);
            var y = y_ == -1 ? n : Math.Min(y_, n);

            var xleaves = new HashSet<int>();
            var yleaves = new HashSet<int>();
            var xroot = result.XDendrogram.Root;
            xleaves.Add(xroot);
            result.XDendrogram.BfsNode(xroot, v =>
            {
                if (xleaves.Count >= x) return;
                xleaves.Remove(v);
                foreach (var e in result.XDendrogram[v])
                    xleaves.Add(e.To);
            });
            var yroot = result.YDendrogram.Root;
            yleaves.Add(yroot);
            result.YDendrogram.BfsNode(yroot, v =>
            {
                if (yleaves.Count >= y) return;
                yleaves.Remove(v);
                foreach (var e in result.YDendrogram[v])
                    yleaves.Add(e.To);
            });

            var xgroups = Enumerable.Range(0, result.XDendrogram.Count).ToArray();
            var ygroups = Enumerable.Range(0, result.YDendrogram.Count).ToArray();

            foreach (var v in xleaves)
                result.XDendrogram.PreOrder(v, e => xgroups[e.To] = v);
            foreach (var v in yleaves)
                result.YDendrogram.PreOrder(v, e => ygroups[e.To] = v);

            var xmap = new Dictionary<int, int>(x);
            var ymap = new Dictionary<int, int>(y);

            {
                var next = 0;
                foreach(var group in xgroups)
                {
                    if (xmap.ContainsKey(group)) continue;
                    xmap[group] = next++;
                }
            }
            {
                var next = 0;
                foreach(var group in ygroups)
                {
                    if (ymap.ContainsKey(group)) continue;
                    ymap[group] = next++;
                }
            }

            var xdendrogram = new DirectedTree(x * 2 - 1);  // Premise: binary tree
            var ydendrogram = new DirectedTree(y * 2 - 1);  // Premise: binary tree

            result.XDendrogram.PreOrder(e =>
            {
                if (e.To != xgroups[e.To]) return;
                xdendrogram.AddEdge(xmap[e.From], xmap[e.To], e.Distance);
            });
            result.YDendrogram.PreOrder(e =>
            {
                if (e.To != ygroups[e.To]) return;
                ydendrogram.AddEdge(ymap[e.From], ymap[e.To], e.Distance);
            });

            var groupedmatrix = new List<double>[y, x];
            for (int i = 0; i < x; ++i) for (int j = 0; j < y; ++j)
                    groupedmatrix[j, i] = new List<double>();
            for (int i = 0; i < m; ++i) for (int j = 0; j < n; ++j)
                    groupedmatrix[ymap[ygroups[j]], xmap[xgroups[i]]].Add(result.StatisticsObject.XDataMatrix[i, j]);
            var aggedmatrix = new double[y, x];
            for (int i = 0; i < x; ++i) for (int j = 0; j < y; ++j)
                    aggedmatrix[j, i] = groupedmatrix[j, i].Aggregate((acc, e) => agg(acc, e));

            XDendrogram = xdendrogram;
            YDendrogram = ydendrogram;
            DataMatrix = aggedmatrix;
            if (x == m)
                XLabels = result.StatisticsObject.YLabels;
            else
                XLabels = new String[] { };
            if (y == n)
                YLabels = result.StatisticsObject.XLabels;
            else
                YLabels = new String[] { };
        }

        private double[,] transpose(double[,] matrix)
        {
            var n = matrix.GetLength(1);
            var m = matrix.GetLength(0);
            var transposeMatrix = new double[n, m];
            for (int i = 0; i < m; ++i)
                for (int j = 0; j < n; ++j)
                    transposeMatrix[j, i] = matrix[i, j];
            return transposeMatrix;
        }

        private DirectedTree xDendrogram;
        private DirectedTree yDendrogram;
        private double[,] dataMatrix;
        private MultivariateAnalysisResult result;
        private int xNumberOfData = -1;
        private int yNumberOfData = -1;
    }
}
