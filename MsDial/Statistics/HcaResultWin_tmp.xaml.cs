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

using CompMs.Common.DataStructure;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Interaction logic for HcaResultWin.xaml
    /// </summary>
    public partial class HcaResultWin_tmp : Window
    {
        public HcaResultWin_tmp(MultivariateAnalysisResult result)
        {
            InitializeComponent();
            this.DataContext = new HcaResultVM_tmp(result);
        }
    }

    class HcaResultVM_tmp : ViewModelBase
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
                SetDataLimit(XNumberOfData, YNumberOfData);
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
                SetDataLimit(XNumberOfData, YNumberOfData);
                OnPropertyChanged(String.Empty);
            }
        }

        public int XLabelLimit { get; set; } = 20;
        public int YLabelLimit { get; set; } = 20;

        public HcaResultVM_tmp(MultivariateAnalysisResult result_)
        {
            result = result_;
            DataMatrix = transpose(result.StatisticsObject.XDataMatrix);
            XDendrogram = result.XDendrogram;
            YDendrogram = result.YDendrogram;
            XLabels = result.StatisticsObject.YLabels;
            YLabels = result.StatisticsObject.XLabels;
        }

        public void SetDataLimit(int x_ = -1, int y_ = -1)
        {
            var datamatrix = result.StatisticsObject.XDataMatrix;
            var transposed = transpose(datamatrix);
            var scaled = MinMaxScaling(transposed);
            var m = scaled.GetLength(1);
            var n = scaled.GetLength(0);
            var x = x_ == -1 ? m : Math.Min(x_, m);
            var y = y_ == -1 ? n : Math.Min(y_, n);

            var xvars = new double[m];
            for (int i = 0; i < m; i++)
            {
                double z = 0, z2 = 0;
                for (int j = 0; j < n; j++)
                {
                    z += scaled[j, i];
                    z2 += scaled[j, i] * scaled[j, i];
                }
                xvars[i] = z2 / n - (z / n) * (z / n);
            }
            var xleaves = xvars.Select((v, i) => (v, i))
                               .OrderBy(p => -p.v)
                               .Take(x)
                               .Select(p => p.i)
                               .ToHashSet();
            var xcontains = new bool[result.XDendrogram.Count];
            var xroot = result.XDendrogram.Root;
            result.XDendrogram.PostOrder(xroot, e =>
                xcontains[e.To] = xleaves.Contains(e.To) ||
                                  result.XDendrogram[e.To]
                                        .Any(e_ => xcontains[e_.To])
            );
            xcontains[xroot] = xleaves.Contains(xroot) ||
                               result.XDendrogram[xroot]
                                     .Any(e_ => xcontains[e_.To]);
            var xmap = new int[result.XDendrogram.Count];
            var cnt = 0;
            for (int i = 0; i < result.XDendrogram.Count; i++)
            {
                if (xcontains[i])
                {
                    xmap[i] = cnt++;
                }
            }
            var xdendrogram = new DirectedTree(cnt);
            result.XDendrogram.PreOrder(xroot, e =>
            {
                if (xcontains[e.To])
                {
                    xdendrogram.AddEdge(xmap[e.From], xmap[e.To], e.Distance);
                }
            });

            var yvars = new double[n];
            for (int i = 0; i < n; i++)
            {
                double z = 0, z2 = 0;
                for (int j = 0; j < m; j++)
                {
                    z += scaled[i, j];
                    z2 += scaled[i, j] * scaled[i, j];
                }
                yvars[i] = z2 / m - (z / m) * (z / m);
            }
            var yleaves = yvars.Select((v, i) => (v, i))
                               .OrderBy(p => -p.v)
                               .Take(y)
                               .Select(p => p.i)
                               .ToHashSet();
            var ycontains = new bool[result.YDendrogram.Count];
            var yroot = result.YDendrogram.Root;
            result.YDendrogram.PostOrder(yroot, e =>
                ycontains[e.To] = yleaves.Contains(e.To) ||
                                  result.YDendrogram[e.To]
                                        .Any(e_ => ycontains[e_.To])
            );
            ycontains[yroot] = yleaves.Contains(yroot) ||
                               result.YDendrogram[yroot]
                                     .Any(e_ => ycontains[e_.To]);
            var ymap = new int[result.YDendrogram.Count];
            cnt = 0;
            for (int i = 0; i < result.YDendrogram.Count; i++)
            {
                if (ycontains[i])
                {
                    ymap[i] = cnt++;
                }
            }
            var ydendrogram = new DirectedTree(cnt);
            result.YDendrogram.PreOrder(yroot, e =>
            {
                if (ycontains[e.To])
                {
                    ydendrogram.AddEdge(ymap[e.From], ymap[e.To], e.Distance);
                }
            });

            var mappedmatrix = new double[y, x];
            for (int i = 0; i < m; i++)
            {
                if (!xcontains[i])
                {
                    continue;
                }
                for (int j = 0; j < n; j++)
                {
                    if (!ycontains[j])
                    {
                        continue;
                    }
                    mappedmatrix[ymap[j], xmap[i]] = scaled[j, i];
                }
            }

            var xLabels = new string[x];
            for (int i = 0; i < result.StatisticsObject.YLabels.Count; i++)
            {
                if (xcontains[i])
                {
                    xLabels[xmap[i]] = result.StatisticsObject.YLabels[i];
                }
            }
            var yLabels = new string[y];
            for (int i = 0; i < result.StatisticsObject.XLabels.Count; i++)
            {
                if (ycontains[i])
                {
                    yLabels[ymap[i]] = result.StatisticsObject.XLabels[i];
                }
            }


            XDendrogram = xdendrogram;
            YDendrogram = ydendrogram;
            DataMatrix = mappedmatrix;
            XLabels = xLabels;
            YLabels = yLabels;
        }

        /*
        public void SetDataLimit( Func<IEnumerable<double>, double> agg, int x_ = -1, int y_ = -1)
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

            var transposedmatrix = transpose(result.StatisticsObject.XDataMatrix);
            var scaledmatrix = MinMaxScaling(transposedmatrix);
            var groupedmatrix = new List<double>[y, x];
            for (int i = 0; i < x; ++i) for (int j = 0; j < y; ++j)
                    groupedmatrix[j, i] = new List<double>();
            for (int i = 0; i < m; ++i) for (int j = 0; j < n; ++j)
                    groupedmatrix[ymap[ygroups[j]], xmap[xgroups[i]]].Add(scaledmatrix[j, i]);
            var aggedmatrix = new double[y, x];
            for (int i = 0; i < x; ++i) for (int j = 0; j < y; ++j)
                    aggedmatrix[j, i] = agg(groupedmatrix[j, i]);

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
        */

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

        private double[,] MinMaxScaling(double[,] matrix)
        {
            var m = matrix.GetLength(0);
            var n = matrix.GetLength(1);

            var result = new double[m, n];
            for(int i = 0; i < m; ++i)
            {
                var max = double.MinValue;
                var min = double.MaxValue;
                for (int j = 0; j < n; ++j)
                {
                    max = Math.Max(max, matrix[i, j]);
                    min = Math.Min(min, matrix[i, j]);
                }
                if (min == max)
                    for (int j = 0; j < n; ++j)
                        result[i, j] = 0;
                else
                    for (int j = 0; j < n; ++j)
                        result[i, j] = (matrix[i, j] - min) / (max - min);
            }

            return result;
        }

        private DirectedTree xDendrogram;
        private DirectedTree yDendrogram;
        private double[,] dataMatrix;
        private MultivariateAnalysisResult result;
        private int xNumberOfData = -1;
        private int yNumberOfData = -1;
    }
}
