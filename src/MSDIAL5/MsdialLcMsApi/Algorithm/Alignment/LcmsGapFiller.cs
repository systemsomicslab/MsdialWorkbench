using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment
{
    public class LcmsGapFiller : GapFiller
    {
        private readonly double mzTol, rtTol;
        private readonly IonMode ionMode;
        protected override double AxTol => rtTol;

        public LcmsGapFiller(double rtTol, double mzTol, IonMode ionMode, SmoothingMethod smoothingMethod, int smoothingLevel, bool isForceInsert)
            : base(smoothingMethod, smoothingLevel, isForceInsert) {
            this.rtTol = rtTol;
            this.mzTol = mzTol;
            this.ionMode = ionMode;
        }

        public LcmsGapFiller(MsdialLcmsParameter param)
            : this(param.RetentionTimeAlignmentTolerance, param.CentroidMs1Tolerance, param.IonMode,
                  param.SmoothingMethod, param.SmoothingLevel, param.IsForceInsertForGapFilling) { }

        protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
            return new ChromXs(peaks.Average(peak => peak.ChromXsTop.RT.Value), ChromXType.RT, ChromXUnit.Min)
            {
                Mz = new MzValue(peaks.Argmax(peak => peak.PeakHeightTop).Mass),
            };
        }

        protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return peaks.Max(peak => peak.PeakWidth(ChromXType.RT));
        }

        protected override List<ChromatogramPeak> GetPeaks(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            peakWidth = Math.Max(peakWidth, 0.2f);

            var chromatogramRange = new ChromatogramRange(center.RT.Value - peakWidth * 1.5, center.RT.Value + peakWidth * 1.5, ChromXType.RT, ChromXUnit.Min);
            var peaklist = ms1Spectra.GetMs1ExtractedChromatogram(center.Mz.Value, this.mzTol, chromatogramRange);
            return peaklist.ChromatogramSmoothing(smoothingMethod, smoothingLevel).AsPeakArray();
        }
    }
}
