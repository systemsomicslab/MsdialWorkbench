using System.Collections.Generic;
using System.Linq;
using System.Windows;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Heatmap
{
    class HeatmapElement : DrawingElementGroup
    {
        public HeatmapElement(IEnumerable<Rect> areas)
        {
            foreach (var area in areas)
                Add(new AreaElement(area));
        }
    }
}
