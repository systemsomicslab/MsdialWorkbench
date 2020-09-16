using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;

namespace CompMs.MsdialLcImMsApi.DataObj
{
    public class LcimmsGapFiller : GapFiller
    {
        protected readonly double rtTol, mzTol, dtTol;
        protected override double AxTol => dtTol;

        public LcimmsGapFiller(MsdialLcImMsParameter param) : base(param) {
            rtTol = param.AccumulatedRtRagne;
            mzTol = param.CentroidMs1Tolerance;
            dtTol = param.DriftTimeAlignmentTolerance;
        }

        protected override ChromXs GetCenter(IEnumerable<AlignmentChromPeakFeature> peaks) {
            throw new NotImplementedException();
        }

        protected override double GetAveragePeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return peaks.Max(peak => peak.PeakWidth(ChromXType.Drift));
        }

        protected override List<ChromatogramPeak> GetPeaks(List<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            var mzTol = Math.Max(this.mzTol, 0.005);
            peakWidth = Math.Max(peakWidth, 0.2f);
            var peaklist = DataAccess.GetDriftChromatogramByRtMz(
                spectrum, (float)center.RT.Value, (float)rtTol,
                (float)center.Mz.Value, (float)mzTol,
                (float)(center.Drift.Value - peakWidth * 1.5), (float)(center.Drift.Value + peakWidth * 1.5));
            return DataAccess.GetSmoothedPeaklist(peaklist, smoothingMethod, smoothingLevel);
        }
    }
}
