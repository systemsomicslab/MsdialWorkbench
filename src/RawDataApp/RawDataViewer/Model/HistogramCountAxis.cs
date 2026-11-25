using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.RawDataViewer.Model
{
    /// <summary>
    /// The linear axis the counts of the viewer's histograms are drawn on, which starts at 0.
    /// </summary>
    internal static class HistogramCountAxis
    {
        public static ContinuousAxisManager<double> Create(IEnumerable<double> counts, IChartMargin margin) {
            // ContinuousAxisManager gives an empty collection the range of 0 alone, and widens a range that has no width
            // by 1 on each side, so a histogram with no bins had a count axis from -1 to 1. No count is below 0, so such
            // a chart gets the range 0 to 1, which is only there for the ticks to have a width to be drawn over.
            var values = counts.ToArray();
            var bounds = new AxisRange(0d, 0d);
            if (values.Length == 0) {
                return new ContinuousAxisManager<double>(0d, 1d, margin, bounds);
            }
            return new ContinuousAxisManager<double>(values, margin, bounds);
        }
    }
}
