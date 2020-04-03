using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

using Rfx.Riken.OsakaUniv;
using Common.DataStructure;
using System.ComponentModel;

namespace PlottingControlsTests.Dendrogram
{
    class DendrogramTestVM : ViewModelBase
    {
        #region Properties
        public Graph Dendrogram { get; }
        public int Root { get; }

        public double XDisplayMin
        {
            get => xDisplayMin;
            set
            {
                if (xDisplayMin == value) return;
                xDisplayMin = value;
                // Console.WriteLine("X min changed.");
                OnPropertyChanged();
            }
        }
        public double XDisplayMax
        {
            get => xDisplayMax;
            set
            {
                if (xDisplayMax == value) return;
                xDisplayMax = value;
                OnPropertyChanged();
            }
        }
        public double YDisplayMin
        {
            get => yDisplayMin;
            set
            {
                if (yDisplayMin == value) return;
                yDisplayMin = value;
                OnPropertyChanged();
            }
        }
        public double YDisplayMax
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

        public DendrogramTestVM(Graph dendrogram, int root, IReadOnlyCollection<string> labels)
        {
            var rank = postOrdering(dendrogram, root);
            Root = rank[root];
            Labels = rank.Zip(labels, Tuple.Create).OrderBy(p => p.Item1).Select(p => p.Item2).ToArray();
            Dendrogram = new Graph(dendrogram.Select(es =>
                new Edges(es.Select(e =>
                    new Edge(rank[e.From], rank[e.To], e.Distance)
                ))
            ).Zip(rank, Tuple.Create).OrderBy(p => p.Item2).Select(p => p.Item1));

            var leafIdxs = new List<int>();
            for(int i = 0; i < dendrogram.Count; ++i)
                if(Dendrogram[i].Count() == 1)
                    leafIdxs.Add(i);
            (IEnumerable<double> xs, IEnumerable<double> ys) = getNodePosition(Dendrogram, Root, leafIdxs);
            XPositions = xs.ToArray();
            YPositions = ys.ToArray();
            XLabelPositions = leafIdxs.Select(i => XPositions[i]).ToArray();

            var xValueMin =XPositions.Min();
            var xValueMax =XPositions.Max();
            var yValueMin =YPositions.Min();
            var yValueMax =YPositions.Max();

            XDisplayMin = xValueMin - (xValueMax - xValueMin) * 0.05;
            XDisplayMax = xValueMax + (xValueMax - xValueMin) * 0.05;
            YDisplayMin = yValueMin;
            YDisplayMax = yValueMax + (yValueMax - yValueMin) * 0.05;
        }

        public DendrogramTestVM(Graph dendrogram, int root)
            : this(dendrogram, root, new string[] { }) {}

        private static int[] postOrdering(Graph dendrogram, int root)
        {
            var rank = Enumerable.Repeat(-1, dendrogram.Count).ToArray();
            var counter = 0;
            void dfs(int v, int p)
            {
                foreach (var e in dendrogram[v])
                {
                    if (e.To != p)
                    {
                        dfs(e.To, v);
                    }
                }
                rank[v] = counter++;
            }

            dfs(root, -1);
            return rank;
        }

        private static (List<double>, List<double>) getNodePosition(Graph dendrogram, int root, IReadOnlyList<int> leafIdxs)
        {
            var n = leafIdxs.Count;
            var xPosition = Enumerable.Repeat(0.0, n * 2 - 1).ToList();
            var yPosition = Enumerable.Repeat(0.0, n * 2 - 1).ToList();
            for(int j = 0; j < n; ++j)
            {
                xPosition[leafIdxs[j]] = (j * 2 + 1) / (n * 2.0);
            }
            double dfs(int v, int p, double y)
            {
                yPosition[v] = y;
                var xs = new List<double>();
                foreach (var e in dendrogram[v])
                {
                    if (e.To != p)
                    {
                        xs.Add(dfs(e.To, v, y + e.Distance));
                    }
                }
                if (xs.Count != 0)
                {
                    xPosition[v] = xs.Average();
                }
                return xPosition[v];
            }

            dfs(root, -1, 0);
            var yMax = yPosition.Max();
            yPosition = yPosition.Select(y => yMax - y).ToList();
            return (xPosition, yPosition);
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
