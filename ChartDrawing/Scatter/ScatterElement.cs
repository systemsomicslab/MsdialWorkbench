using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Scatter
{
    class ScatterElement : DrawingElementGroup
    {
        public ScatterElement(
            IReadOnlyList<double> xPositions,
            IReadOnlyList<double> yPositions,
            double size
            )
        {
            foreach ((double x, double y) in xPositions.Zip(yPositions, Tuple.Create))
            {
                Add(new CirclePointElement(new Point(x, y), size));
            }
        }
    }
}
