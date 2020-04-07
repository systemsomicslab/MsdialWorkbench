using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Common.DataStructure;

namespace Msdial.Dendrogram
{
    public class DendrogramPlotBean : INotifyPropertyChanged
    {
        #region Properties
        public string GraphTitle { get; }
        public ObservableCollection<string> LabelCollection { get; }
        public DirectedTree Dendrogram { get; }
        public int Root { get; }
        public IReadOnlyList<int> LeafIdxs => leafIdxs;
        public IReadOnlyList<double> XLeafPositions => LeafIdxs.Select(idx => XPositions[idx]).ToArray();
        public IReadOnlyList<double> YLeafPositions => LeafIdxs.Select(idx => YPositions[idx]).ToArray();

        public double ValueMinX { get; }
        public double ValueMaxX { get; }
        public double ValueMinY { get; }
        public double ValueMaxY { get; }

        public double DisplayMinX
        {
            get => displayMinX;
            set
            {
                if (displayMinX == value) return;
                displayMinX = value;
                OnPropertyChanged();
            }
        }
        public double DisplayMaxX
        {
            get => displayMaxX;
            set
            {
                if (displayMaxX == value) return;
                displayMaxX = value;
                OnPropertyChanged();
            }
        }
        public double DisplayMinY
        {
            get => displayMinY;
            set
            {
                if (displayMinY == value) return;
                displayMinY = value;
                OnPropertyChanged();
            }
        }
        public double DisplayMaxY
        {
            get => displayMaxY;
            set
            {
                if (displayMaxY == value) return;
                displayMaxY = value;
                OnPropertyChanged();
            }
        }
        public int SelectedIdx
        {
            get => selectedIdx;
            set
            {
                if (selectedIdx == value) return;
                selectedIdx = value;
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<double> XPositions { get; }
        public IReadOnlyList<double> YPositions { get; }
        public IReadOnlyList<int> Rank { get; }
        #endregion

        public DendrogramPlotBean()
        {
            this.GraphTitle = "Drawing dendrogram";
            this.LabelCollection = new ObservableCollection<string>();
            this.Dendrogram = new DirectedTree(0);
        }
        public DendrogramPlotBean(string graphTitle, IEnumerable<string> labelCollection, DirectedTree dendrogram, int root)
        {
            this.GraphTitle = graphTitle;

            var rank = postOrdering(dendrogram, root);
            this.Rank = rank;
            this.Root = rank[root];
            this.LabelCollection = new ObservableCollection<string>(
                rank.Zip(labelCollection, Tuple.Create).OrderBy(p => p.Item1).Select(p => p.Item2)
            );
            this.Dendrogram = new DirectedTree(dendrogram.Select(es =>
                new Edges(es.Select(e =>
                    new Edge(rank[e.From], rank[e.To], e.Distance)
                ))
            ).Zip(rank, Tuple.Create).OrderBy(p => p.Item2).Select(p => p.Item1));

            this.leafIdxs = new List<int>();
            for(int i = 0; i < dendrogram.Count; ++i)
                if(this.Dendrogram[i].Count() == 1)
                    leafIdxs.Add(i);
            (IEnumerable<double> xs, IEnumerable<double> ys) = getNodePosition(this.Dendrogram, this.Root, this.LeafIdxs);
            this.XPositions = xs.ToArray();
            this.YPositions = ys.ToArray();

            this.ValueMinX =this.XPositions.Min();
            this.ValueMaxX =this.XPositions.Max();
            this.ValueMinY =this.YPositions.Min();
            this.ValueMaxY =this.YPositions.Max();

            this.DisplayMinX = this.ValueMinX - (this.ValueMaxX - this.ValueMinX) * 0.05;
            this.DisplayMaxX = this.ValueMaxX + (this.ValueMaxX - this.ValueMinX) * 0.05;
            this.DisplayMinY = this.ValueMinY;
            this.DisplayMaxY = this.ValueMaxY + (this.ValueMaxY - this.ValueMinY) * 0.05;
        }

        private static int[] postOrdering(DirectedTree dendrogram, int root)
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

        private static (List<double>, List<double>) getNodePosition(DirectedTree dendrogram, int root, IReadOnlyList<int> leafIdxs)
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
        private readonly List<int> leafIdxs;

        private double displayMinX;
        private double displayMaxX;
        private double displayMinY;
        private double displayMaxY;

        private int selectedIdx = -1;
        #endregion

        #region // Required Methods for INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]string propertyName = null) =>
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        // protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        // {
        //     this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        //     Console.WriteLine(propertyName + " changed.");
        // }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) =>
            this.PropertyChanged?.Invoke(this, e);
        #endregion
    }
}
