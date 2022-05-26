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
using CompMs.MsdialLcImMsApi.Parameter;

namespace CompMs.MsdialLcImMsApi.Algorithm.Alignment
{
    public class LcimmsGapFiller : GapFiller3D
    {
        protected readonly double rtTol, mzTol, dtTol;
        protected readonly MsdialLcImMsParameter param;
        protected override double AxTol => AxTolFirst;
        protected override double AxTolFirst => rtTol;
        protected override double AxTolSecond => dtTol;

        public LcimmsGapFiller(MsdialLcImMsParameter param) : base(param) {
            rtTol = param.AccumulatedRtRange;
            mzTol = param.CentroidMs1Tolerance;
            dtTol = param.DriftTimeAlignmentTolerance;
            this.param = param;
        }

        protected override ChromXs GetCenterFirst(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return new ChromXs(peaks.Average(peak => peak.ChromXsTop.RT.Value), ChromXType.RT, ChromXUnit.Min)
            {
                Mz = new MzValue(peaks.Argmax(peak => peak.PeakHeightTop).Mass),
            };
        }
        protected override ChromXs GetCenterSecond(IEnumerable<AlignmentChromPeakFeature> peaks, AlignmentSpotProperty parent) {
            return new ChromXs(peaks.Average(peak => peak.ChromXsTop.Drift.Value), ChromXType.Drift, ChromXUnit.Msec)
            {
                RT = new RetentionTime(parent.TimesCenter.Value),
                Mz = new MzValue(parent.MassCenter),
            };
        }

        protected override double GetAveragePeakWidthFirst(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return peaks.Max(peak => peak.PeakWidth(ChromXType.RT));
        }
        protected override double GetAveragePeakWidthSecond(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return peaks.Max(peak => peak.PeakWidth(ChromXType.Drift));
        }

        protected override List<ChromatogramPeak> GetPeaksFirst(IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            var mzTol = Math.Max(this.mzTol, 0.005f);
            peakWidth = Math.Max(peakWidth, 0.2f);

            var rawSpectra = new RawSpectra(spectrum, param.IonMode, param.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(center.RT.Value - peakWidth * 1.5, center.RT.Value + peakWidth * 1.5, ChromXType.RT, ChromXUnit.Min);
            var peaklist = rawSpectra.GetMs1ExtractedChromatogram(center.Mz.Value, mzTol, chromatogramRange);
            return peaklist.Smoothing(smoothingMethod, smoothingLevel);
        }

        protected override List<ChromatogramPeak> GetPeaksSecond(IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            var mzTol = Math.Max(this.mzTol, 0.005);
            peakWidth = Math.Max(peakWidth, 0.2f);
            var peaklist = DataAccess.GetDriftChromatogramByRtMz(
                spectrum, (float)center.RT.Value, (float)rtTol,
                (float)center.Mz.Value, (float)mzTol,
                (float)(center.Drift.Value - peakWidth * 1.5), (float)(center.Drift.Value + peakWidth * 1.5));
            return new Chromatogram(peaklist, ChromXType.Drift, ChromXUnit.Msec).Smoothing(smoothingMethod, smoothingLevel);
        }
    }
}
