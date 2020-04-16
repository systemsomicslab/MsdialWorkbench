using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Common.DataStructure;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Dendrogram
{
    class DendrogramElement : DrawingElementGroup
    {
        public DendrogramElement(
            DirectedTree tree,
            IReadOnlyList<double> xPositions = null,
            IReadOnlyList<double> yPositions = null
            )
        {
            var root = tree.Root;
            var XPositions = xPositions?.ToArray();
            if (XPositions == null)
            {
                XPositions = new double[tree.Count];
                var leaves = tree.Leaves.ToHashSet();
                var leafId = 0;
                tree.PostOrder(root, e =>
                {
                    if (leaves.Contains(e.To))
                        XPositions[e.To] = leafId++;
                    else
                        XPositions[e.To] = tree[e.To].Average(z => XPositions[z.To]);
                });
                XPositions[root] = leaves.Contains(root) ? 0d : tree[root].Average(z => XPositions[z.To]);
            }

            var YPositions = yPositions?.ToArray();
            if(YPositions == null)
            {
                YPositions = new double[tree.Count];
                tree.PostOrder(root, e => YPositions[e.From] = YPositions[e.To] + e.Distance);
            }

            tree.PostOrder(root, e =>
            {
                var parent = new Point(XPositions[e.From], YPositions[e.From]);
                var child = new Point(XPositions[e.To], YPositions[e.To]);
                var mid = new Point(child.X, parent.Y);
                Add(new LineElement(parent, mid));
                Add(new LineElement(mid, child));
            });
        }
    }
}
