using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using Common.DataStructure;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Dendrogram
{
    public class DendrogramFactory : IChartFactory
    {
        private static readonly Pen graphLine  = new Pen(Brushes.Black, 1);

        public Rect ElementArea { get; set; }
        public Transform TransformElement { get; set; } = Transform.Identity;
        DrawingElementGroup elements = new DrawingElementGroup();

        public DendrogramFactory(Size size, DirectedTree tree, IReadOnlyList<double> xPositions, IReadOnlyList<double> yPositions)
        {
            void drawDfs(int v)
            {
                foreach (var e in tree[v])
                {
                    drawDfs(e.To);
                    var frompoint = new Point(xPositions[v], yPositions[v]);
                    var midpoint = new Point(xPositions[e.To], yPositions[v]);
                    var topoint = new Point(xPositions[e.To], yPositions[e.To]);
                    elements.Add(new LineElement(frompoint, midpoint));
                    elements.Add(new LineElement(midpoint, topoint));
                }
            }
            drawDfs(tree.Root);
        }

        public Drawing CreateChart(Size size)
        {
            return new GeometryDrawing();
        }

        public Vector Move(Vector vector)
        {
            throw new NotImplementedException();
        }

        public Rect Reset()
        {
            throw new NotImplementedException();
        }

        public Rect UpdateRange(Rect rect)
        {
            throw new NotImplementedException();
        }

        public void SizeChanged(Size size)
        {
            throw new NotImplementedException();
        }
    }
}
