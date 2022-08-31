using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class DisplayChromatogram : BindableBase {

        public DisplayChromatogram(IEnumerable<ChromatogramPeak> peaks, Pen linePen = null, string title = "na")
            : this(peaks?.Select(peak => new ChromatogramPeakWrapper(peak)).ToList() ?? new List<ChromatogramPeakWrapper>(), linePen, title) {

        }

        public DisplayChromatogram(List<ChromatogramPeakWrapper> peaks, Pen linePen = null, string title = "na") {
            ChromatogramPeaks = peaks ?? throw new System.ArgumentNullException(nameof(peaks));
            if (linePen is null) {
                linePen = new Pen(Brushes.Black, 1.0);
                linePen.Freeze();
            }
            LinePen = linePen;
            Name = title;
        }

        public List<ChromatogramPeakWrapper> ChromatogramPeaks { get; }

        public string Name { get; }

        public bool Visible {
            get => _visible;
            set => SetProperty(ref _visible, value);
        }
        private bool _visible = true;

        public Pen LinePen { get; }
        public Brush LineBrush => LinePen.Brush;

        public double MaxIntensity => ChromatogramPeaks.DefaultIfEmpty().Max(chromatogramPeak => chromatogramPeak?.Intensity) ?? 0d;
        public double MaxChromX => ChromatogramPeaks.DefaultIfEmpty().Max(chromatogramPeak => chromatogramPeak?.ChromXValue) ?? 1d;
        public double MinChromX => ChromatogramPeaks.DefaultIfEmpty().Min(chromatogramPeak => chromatogramPeak?.ChromXValue) ?? 0d;

        public Range ChromXRange => new Range(MinChromX, MaxChromX);
    }
}
