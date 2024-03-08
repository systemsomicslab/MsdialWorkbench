using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class DisplayChromatogram : BindableBase {
        private readonly Chromatogram? _chromatogram;

        public DisplayChromatogram(List<PeakItem> peaks, Pen? linePen = null, string name = "na") {
            ChromatogramPeaks = peaks ?? throw new System.ArgumentNullException(nameof(peaks));
            if (linePen is null) {
                linePen = new Pen(Brushes.Black, 1.0);
                linePen.Freeze();
            }
            _linePen = linePen;
            _name = name;
        }

        public DisplayChromatogram(IEnumerable<ChromatogramPeak> peaks, Pen? linePen = null, string name = "na")
            : this(peaks.Select(peak => new PeakItem(peak)).ToList(), linePen, name) {

        }

        public DisplayChromatogram(Chromatogram chromatogram, Pen? linePen = null, string name = "na") : this(chromatogram.AsPeakArray(), linePen, name) {
            _chromatogram = chromatogram;
        }

        public List<PeakItem> ChromatogramPeaks { get; }

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        private string _name;

        public bool Visible {
            get => _visible;
            set => SetProperty(ref _visible, value);
        }
        private bool _visible = true;

        public Pen LinePen {
            get => _linePen;
            set {
                if (SetProperty(ref _linePen, value)) {
                    OnPropertyChanged(nameof(LineBrush));
                }
            }
        }
        private Pen _linePen;

        public Brush LineBrush => LinePen.Brush;

        public double MaxIntensity => ChromatogramPeaks.DefaultIfEmpty().Max(chromatogramPeak => chromatogramPeak?.Intensity) ?? 0d;
        public double MaxChromX => ChromatogramPeaks.DefaultIfEmpty().Max(chromatogramPeak => chromatogramPeak?.Time) ?? 1d;
        public double MinChromX => ChromatogramPeaks.DefaultIfEmpty().Min(chromatogramPeak => chromatogramPeak?.Time) ?? 0d;

        public AxisRange ChromXRange => new AxisRange(MinChromX, MaxChromX);
    }
}
