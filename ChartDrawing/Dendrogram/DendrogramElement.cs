using System.Collections.Generic;
using System.Linq;
using System.Windows;

using CompMs.Common.DataStructure;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Dendrogram
{
    class DendrogramElement : DrawingElementGroup
    {
        public DendrogramElement(
            DirectedTree tree,
            IReadOnlyList<double> xPositions,
            IReadOnlyList<double> yPositions
            )
        {
            tree.PostOrder(e =>
            {
                var parent = new Point(xPositions[e.From], yPositions[e.From]);
                var child = new Point(xPositions[e.To], yPositions[e.To]);
                var mid = new Point(child.X, parent.Y);
                Add(new LineElement(parent, mid));
                Add(new LineElement(mid, child));
            });
        }
    }
}
