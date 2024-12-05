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
        private readonly Chromatogram _chromatogram;
        private readonly PeakOfChromatogram? _peakOfChromatogram;

        public PeakChromatogram(Chromatogram chromatogram, PeakOfChromatogram? peak, string class_, Color color, string description = "") {
            _chromatogram = chromatogram;
            _peakOfChromatogram = peak;

            var peaks = chromatogram.AsPeakArray();
            var items = peaks.Select(v => new PeakItem(v)).ToList();

            Peaks = items;
            Class = class_;
            Color = color;
            Description = description;

            if (peak is null) {
                PeakArea = new List<PeakItem>(0);
                PeakTop = null;
                return;
            }

            var ids = peaks.Select(p => p.ID).ToArray();
            var left = ids.IndexOf(peak.GetLeft().ID);
            var right = ids.IndexOf(peak.GetRight().ID);
            if (left >= 0 && right >= 0) {
                PeakArea = items.GetRange(left, right - left + 1);
            }
            else {
                PeakArea = peak.SlicePeakArea().Select(v => new PeakItem(v)).ToList();
            }

            var top = ids.IndexOf(peak.GetTop().ID);
            if (top >= 0) {
                PeakTop = items[top];
            }
        }

        public List<PeakItem> Peaks { get; }
        public List<PeakItem> PeakArea { get; }
        public PeakItem? PeakTop { get; }
        public string Class { get; }
        public Color Color { get; }
        public string Description { get; }

        public AxisRange GetTimeRange() {
            if (Peaks.IsEmptyOrNull()) {
                return new AxisRange(0d, 1d);
            }
            return new AxisRange(Peaks.Min(peak => peak.Time), Peaks.Max(peak => peak.Time));
        }

        public AxisRange GetAbundanceRange() {
            if (PeakArea.IsEmptyOrNull()) {
                return new AxisRange(0d, 1d);
            }
            return new AxisRange(0d, Peaks.Max(peak => peak.Intensity));
        }

        public Chromatogram Convert() => _chromatogram;

        public DisplayChromatogram ConvertToDisplayChromatogram() {
            var pen = new Pen(new SolidColorBrush(Color), 1d);
            pen.Freeze();
            return new DisplayChromatogram(_chromatogram, pen, Class);
        }
    }
}
