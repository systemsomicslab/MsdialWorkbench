using CompMs.Common.Components;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class Chromatogram
    {
        private readonly ChromXType _type;
        private readonly ChromXUnit _unit;

        public Chromatogram(List<PeakItem> peaks, List<PeakItem> peakArea, string class_, Color color, ChromXType type, ChromXUnit unit) {
            Peaks = peaks;
            PeakArea = peakArea;
            Class = class_;
            Color = color;
            _type = type;
            _unit = unit;
        }

        public List<PeakItem> Peaks { get; }
        public List<PeakItem> PeakArea { get; }
        public string Class { get; }
        public Color Color { get; }

        public CompMs.Common.Components.Chromatogram Convert() {
            return new CompMs.Common.Components.Chromatogram(Peaks.Select(peak => peak.Chrom).ToArray(), _type, _unit);
        }
    }
}
