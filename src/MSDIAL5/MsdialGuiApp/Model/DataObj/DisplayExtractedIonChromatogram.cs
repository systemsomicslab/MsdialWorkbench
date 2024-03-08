using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Utility;
using System.Collections.Generic;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class DisplayExtractedIonChromatogram : DisplayChromatogram
    {
        private readonly IonMode _ionMode;

        public DisplayExtractedIonChromatogram(List<PeakItem> peaks, double mz, double tolerance, IonMode ionMode, Pen? linePen = null, string name = "na") : base(peaks, linePen, name) {
            Mz = mz;
            Tolerance = tolerance;
            _ionMode = ionMode;
        }

        public DisplayExtractedIonChromatogram(ExtractedIonChromatogram chromatogram, double tolerance, IonMode ionMode, Pen? linePen = null, string name = "na") : base(chromatogram, linePen, name) {
            Mz = chromatogram.ExtractedMz;
            Tolerance = tolerance;
            _ionMode = ionMode;
        }

        public double Mz { get; }
        public double Tolerance { get; }

        public MSIonProperty DetectPeak(double start, double end) {
            var ptr = ChromatogramPeaks.LowerBound(start, (peak, t) => peak.Time.CompareTo(t));
            var peakTop = ChromatogramPeaks[ptr];
            while (ptr < ChromatogramPeaks.Count && ChromatogramPeaks[ptr].Time < end) {
                if (peakTop.Intensity < ChromatogramPeaks[ptr].Intensity) {
                    peakTop = ChromatogramPeaks[ptr];
                }
                ++ptr;
            }
            return new MSIonProperty(peakTop.Chrom.Mass, peakTop.Chrom.ChromXs, _ionMode, AdductIon.Default, -1d);
        }
    }
}
