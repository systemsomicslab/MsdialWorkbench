using System.Collections.Generic;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class Chromatogram
    {
        public Chromatogram(
            List<PeakItem> peaks,
            List<PeakItem> peakArea,
            string class_,
            Color color) {
            Peaks = peaks;
            PeakArea = peakArea;
            Class = class_;
            Color = color;
        }

        public List<PeakItem> Peaks { get; }
        public List<PeakItem> PeakArea { get; }
        public string Class { get; }
        public Color Color { get; }
    }
}
