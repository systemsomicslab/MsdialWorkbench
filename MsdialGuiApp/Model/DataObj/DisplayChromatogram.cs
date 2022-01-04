using CompMs.Common.Components;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj {
    public class DisplayChromatogram : BindableBase {

        public DisplayChromatogram(List<ChromatogramPeak> peaks, Pen linePen = null, string title = "na") {
            if (peaks is null) {
                ChromatogramPeaks = new List<ChromatogramPeakWrapper>();
            }
            else {
                ChromatogramPeaks = peaks.Select(n => new ChromatogramPeakWrapper(n)).ToList();
            }
            if (linePen is null) {
                LinePen = new Pen(Brushes.Black, 1.0);
            }
            else {
                LinePen = linePen;
            }
            Name = title;
            LinePen.Freeze();
        }

        public DisplayChromatogram(List<ChromatogramPeakWrapper> peaks, Pen linePen = null, string title = "na") {
            ChromatogramPeaks = peaks;
            Name = title;
            if (linePen is null) {
                LinePen = new Pen(Brushes.Black, 1.0);
            }
            else {
                LinePen = linePen;
            }
            LinePen.Freeze();
        }

        public List<ChromatogramPeakWrapper> ChromatogramPeaks { get; } = new List<ChromatogramPeakWrapper>();
        public string Name { get; }

        public bool Visible {
            get => visible;
            set => SetProperty(ref visible, value);
        }
        private bool visible = true;

        public Pen LinePen { get; }
        public SolidColorBrush LineBrush { get => (SolidColorBrush)LinePen.Brush; }

        public double MaxIntensity { get => ChromatogramPeaks.Any() ? ChromatogramPeaks.Max(n => n.Intensity) : 0.0; }
        public double MaxChromX { get => (double)(ChromatogramPeaks.Any() ? ChromatogramPeaks.Max(n => n.ChromXValue) : 1.0); }
        public double MinChromX { get => (double)(ChromatogramPeaks.Any() ? ChromatogramPeaks.Min(n => n.ChromXValue) : 0.0); }
    }
}
