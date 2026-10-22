using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.RawDataViewer.Model
{
    /// <summary>
    /// The base-2 logarithmic axis the viewer's histograms are drawn on, whose bins start at powers of two.
    /// </summary>
    internal static class Log2HistogramAxis
    {
        public static LogScaleAxisManager<double> Create(IEnumerable<double> binStarts, IChartMargin margin) {
            // LogScaleAxisManager takes the logarithm of the smallest and largest value it is given, and of 0 when it is
            // given none, so a histogram with no bins (the MS2 of an MS1-only file, or the peak spots of a GC-MS file,
            // which this viewer does not detect) had an axis from -Infinity to -Infinity. Such a chart has no bar to
            // place, so it gets the range of the bin 2^0 to have finite ticks to draw.
            var values = binStarts.Where(x => x > 0d && !double.IsInfinity(x)).ToArray();
            if (values.Length == 0) {
                return new LogScaleAxisManager<double>(1d, 2d, margin, base_: 2);
            }
            return new LogScaleAxisManager<double>(values, margin, base_: 2);
        }
    }
}
