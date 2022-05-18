using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;

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

        protected override ChromXs GetCenter(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return new ChromXs(peaks.Average(peak => peak.ChromXsTop.RT.Value), ChromXType.RT, ChromXUnit.Min)
            {
                Mz = new MzValue(peaks.Argmax(peak => peak.PeakHeightTop).Mass),
            };
        }

        protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return peaks.Max(peak => peak.PeakWidth(ChromXType.RT));
        }

        protected override List<ChromatogramPeak> GetPeaks(IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            var mzTol = Math.Max(this.mzTol, 0.005f);
            peakWidth = Math.Max(peakWidth, 0.2f);

            var rawSpectra = new RawSpectra(spectrum, ChromXType.RT, ChromXUnit.Min, ionMode);
            var peaklist = rawSpectra.GetMs1Chromatogram(center.Mz.Value, mzTol, center.RT.Value - peakWidth * 1.5, center.RT.Value + peakWidth * 1.5);
            return peaklist.Smoothing(smoothingMethod, smoothingLevel);
        }
    }
}
