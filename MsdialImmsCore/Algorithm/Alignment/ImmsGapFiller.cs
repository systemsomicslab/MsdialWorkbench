using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        protected override ChromXs GetCenter(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return new ChromXs(peaks.Average(peak => peak.ChromXsTop.Drift.Value), ChromXType.Drift, ChromXUnit.Msec)
            {
                Mz = new MzValue(peaks.Argmax(peak => peak.PeakHeightTop).Mass),
            };
        }

        protected override List<ChromatogramPeak> GetPeaks(IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            var mzTol = this.mzTol;

            var peaklist = DataAccess.GetMs1Peaklist(
                spectrum, center.Mz.Value, mzTol, ionMode,
                ChromXType.Drift, ChromXUnit.Msec, center.Drift.Value - peakWidth * 1.5, center.Drift.Value + peakWidth * 1.5);
            return DataAccess.GetSmoothedPeaklist(peaklist, smoothingMethod, smoothingLevel);
        }

        protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return peaks.Max(peak => peak.PeakWidth(ChromXType.Drift));
        }
    }
}
