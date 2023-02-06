using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using CompMs.Common.DataStructure;

namespace ChartDrawingUiTest.Dendrogram
{
    internal class DendrogramVM4 : INotifyPropertyChanged
    {
        #region Properties
        public DirectedTree Dendrogram { get; }

        public ObservableCollection<Node> Nodes
        {
            get => nodes;
            set => SetProperty(ref nodes, value);
        }

        public double MinY
        {
            get => minY;
            set => SetProperty(ref minY, minY);
        }
        public double MaxY
        {
            get => maxY;
            set => SetProperty(ref maxY, value);
        }

        public ObservableCollection<string> Xs
        {
            get => xs;
            set => SetProperty(ref xs, value);
        }

        #endregion

        private double minY;
        private double maxY;
        private ObservableCollection<Node> nodes;
        private ObservableCollection<string> xs;

        public class Node
        {
            public string X { get; set; }
            public int ID { get; set; }
            public int Order { get; set; }
        }

        public DendrogramVM4()
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

            var ys = Enumerable.Repeat(0, Dendrogram.Count).Select(e => (double)e).ToArray();
            Dendrogram.PreOrder(e => ys[e.To] = ys[e.From] + e.Distance);
            MinY = ys.Min();
            MaxY = ys.Max();
            var cnt = 0;
            var order = Enumerable.Repeat(0, Dendrogram.Leaves.Count).ToArray();
            Dendrogram.PreOrder(e =>
            {
                if (Dendrogram.Leaves.Contains(e.To))
                    order[e.To] = cnt++;
            });

            Nodes = new ObservableCollection<Node>
            {
                new Node(){ X = "A", ID = 0, Order = order[0]},
                new Node(){ X = "B", ID = 1, Order = order[1]},
                new Node(){ X = "C", ID = 2, Order = order[2]},
                new Node(){ X = "D", ID = 3, Order = order[3]},
                new Node(){ X = "E", ID = 4, Order = order[4]},
                new Node(){ X = "F", ID = 5, Order = order[5]},
            };
            Xs = new ObservableCollection<string>(Nodes.Select(n => n.X).Distinct());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void RaiseProerptyChanged(string propertyname) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyname = "")
        {
            if (value.Equals(property)) return false;
            property = value;
            RaiseProerptyChanged(propertyname);
            return true;
        }
    }
}
