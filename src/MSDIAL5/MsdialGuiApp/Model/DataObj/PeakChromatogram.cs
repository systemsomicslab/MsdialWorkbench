using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class PeakChromatogram
    {
        private readonly ChromXType _type;
        private readonly ChromXUnit _unit;

        public PeakChromatogram(List<PeakItem> peaks, List<PeakItem> peakArea, PeakItem peakTop, string class_, Color color, ChromXType type, ChromXUnit unit, string description = "") {
            Peaks = peaks;
            PeakArea = peakArea;
            PeakTop = peakTop;
            Class = class_;
            Color = color;
            _type = type;
            _unit = unit;
            Description = description;
        }

        public PeakChromatogram(List<PeakItem> peaks, List<PeakItem> peakArea, string class_, Color color, ChromXType type, ChromXUnit unit, string description = "")
            : this(peaks, peakArea, peakArea.DefaultIfEmpty().Argmax(item => item?.Intensity ?? 0d), class_, color, type, unit, description) {

        }

        public PeakChromatogram(List<PeakItem> peaks, string class_, Color color, ChromXType type, ChromXUnit unit)
            : this(peaks, peaks, peaks.DefaultIfEmpty().Argmax(item => item?.Intensity ?? 0d), class_, color, type, unit) {

        }

        public List<PeakItem> Peaks { get; }
        public List<PeakItem> PeakArea { get; }
        public PeakItem PeakTop { get; }
        public string Class { get; }
        public Color Color { get; }
        public string Description { get; }

        public Range GetTimeRange() {
            if (Peaks.IsEmptyOrNull()) {
                return new Range(0d, 1d);
            }
            return new Range(Peaks.Min(peak => peak.Time), Peaks.Max(peak => peak.Time));
        }

        public Range GetAbundanceRange() {
            if (PeakArea.IsEmptyOrNull()) {
                return new Range(0d, 1d);
            }
            return new Range(0d, Peaks.Max(peak => peak.Intensity));
        }

        public Chromatogram Convert() {
            return new Chromatogram(Peaks.Select(peak => peak.Chrom).ToArray(), _type, _unit);
        }

        public DisplayChromatogram ConvertToDisplayChromatogram() {
            var pen = new Pen(new SolidColorBrush(Color), 1d);
            pen.Freeze();
            return new DisplayChromatogram(Peaks, pen, Class);
        }
    }
}
