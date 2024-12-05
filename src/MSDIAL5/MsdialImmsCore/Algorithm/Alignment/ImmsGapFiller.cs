using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm.Alignment
{
    public class ImmsGapFiller : GapFiller
    {
        private readonly double mzTol, driftTol;
        private readonly IonMode ionMode;
        protected override double AxTol => driftTol;

        public ImmsGapFiller(double driftTol, double mzTol, IonMode ionMode, SmoothingMethod smoothingMethod, int smoothingLevel, bool isForceInsert)
            : base(smoothingMethod, smoothingLevel, isForceInsert) {

            this.mzTol = mzTol;
            this.driftTol = driftTol;
            this.ionMode = ionMode;
        }

        protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
            return new ChromXs(peaks.Average(peak => peak.ChromXsTop.Drift.Value), ChromXType.Drift, ChromXUnit.Msec)
            {
                Mz = new MzValue(peaks.Argmax(peak => peak.PeakHeightTop).Mass),
            };
        }

        protected override List<ChromatogramPeak> GetPeaks(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            var chromatogramRange = new ChromatogramRange(center.Drift.Value - peakWidth * 1.5, center.Drift.Value + peakWidth * 1.5, ChromXType.Drift, ChromXUnit.Msec);
            var peaklist = ms1Spectra.GetMs1ExtractedChromatogram(center.Mz.Value, this.mzTol, chromatogramRange);
            return peaklist.ChromatogramSmoothing(smoothingMethod, smoothingLevel).AsPeakArray();
        }

        protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return peaks.Max(peak => peak.PeakWidth(ChromXType.Drift));
        }
    }
}
