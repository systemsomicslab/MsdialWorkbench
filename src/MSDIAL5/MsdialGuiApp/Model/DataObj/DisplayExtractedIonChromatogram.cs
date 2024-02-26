using System.Collections.Generic;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class DisplayExtractedIonChromatogram : DisplayChromatogram
    {
        public DisplayExtractedIonChromatogram(List<PeakItem> peaks, double mz, double tolerance, Pen? linePen = null, string title = "na") : base(peaks, linePen, title) {
            Mz = mz;
            Tolerance = tolerance;
        }

        public double Mz { get; }
        public double Tolerance { get; }
    }
}
