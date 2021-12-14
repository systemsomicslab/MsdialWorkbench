using CompMs.Common.Components;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.DataObj {
    public class DisplayChromatogram : BindableBase {

        public DisplayChromatogram(List<ChromatogramPeak> peaks, string title = "na") {
            if (peaks is null) {
                Chromatograms = new List<ChromatogramPeakWrapper>();
            }
            else {
                Chromatograms = peaks.Select(n => new ChromatogramPeakWrapper(n)).ToList();
            }
            Name = title;
        }

        public DisplayChromatogram(List<ChromatogramPeakWrapper> peaks, string title = "na") {
            Chromatograms = peaks;
            Name = title;
        }

        public List<ChromatogramPeakWrapper> Chromatograms { get; } = new List<ChromatogramPeakWrapper>();
        public string Name { get; }

        public bool Visible {
            get => visible;
            set => SetProperty(ref visible, value);
        }
        private bool visible = true;

        public double MaxIntensity { get => Chromatograms.Any() ? Chromatograms.Max(n => n.Intensity) : 0.0; }
        public double MaxChromX { get => (double)(Chromatograms.Any() ? Chromatograms.Max(n => n.ChromXValue) : 1.0); }
        public double MinChromX { get => (double)(Chromatograms.Any() ? Chromatograms.Min(n => n.ChromXValue) : 0.0); }
    }
}
