using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.DataStructure;
using CompMs.Graphics.Compound;
using Rfx.Riken.OsakaUniv;

namespace ChartDrawingUiTest.Compound
{
    public class ClusterVM2 : ViewModelBase
    {
        public double [,] DataMatrix { get; set; }
        public DirectedTree XDendrogram { get; set; }
        public DirectedTree YDendrogram { get; set; }
        public IReadOnlyList<string> XLabels { get; set; }
        public IReadOnlyList<string> YLabels { get; set; }
        public int XLabelLimit { get; set; } = -1;
        public int YLabelLimit { get; set; } = -1;

        public ClusterVM2()
        {
            DataMatrix = new double[,]
            {
                {1, 2, 3, 4, 5 },
                {6, 7, 8, 9, 10 },
                {11, 12, 13, 14, 15 },
                {16, 17, 18, 19, 20 },
            };

            XDendrogram = new DirectedTree(9);
            XDendrogram.AddEdge(5, 4, 3);
            XDendrogram.AddEdge(5, 2, 2);
            XDendrogram.AddEdge(6, 0, 2);
            XDendrogram.AddEdge(6, 1, 1);
            XDendrogram.AddEdge(7, 3, 4);
            XDendrogram.AddEdge(7, 6, 2);
            XDendrogram.AddEdge(8, 5, 6);
            XDendrogram.AddEdge(8, 7, 5);

            YDendrogram = new DirectedTree(7);
            YDendrogram.AddEdge(4, 2, 3);
            YDendrogram.AddEdge(4, 1, 2);
            YDendrogram.AddEdge(4, 0, 1);
            YDendrogram.AddEdge(5, 3, 4);
            YDendrogram.AddEdge(5, 4, 5);
            YDendrogram.AddEdge(6, 5, 6);

            XLabels = new string[]
            {
                "A", "B", "C", "D", "E"
            };
            YLabels = new string[]
            {
                "a", "b", "c", "d"
            };
        }
    }
}
