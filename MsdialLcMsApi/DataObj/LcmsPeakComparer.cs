using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;

namespace CompMs.MsdialLcMsApi.DataObj
{
    public class LcmsPeakComparer : PeakComparer
    {
        private double _mztol;
        private double _rttol;
        private double _factor;

        public LcmsPeakComparer(double mztol, double rttol, double factor) {
            _mztol = mztol;
            _rttol = rttol;
            _factor = factor;
        }

        public LcmsPeakComparer(double mztol, double rttol) : this(mztol, rttol, 1) { }

        public override int Compare(IMSScanProperty x, IMSScanProperty y) {
            return (x.PrecursorMz, x.ChromXs.RT.Value).CompareTo((y.PrecursorMz, y.ChromXs.RT.Value));
        }

        public override bool Equals(IMSScanProperty x, IMSScanProperty y) {
            return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol && Math.Abs(x.ChromXs.RT.Value - y.ChromXs.RT.Value) <= _rttol;
        }

        public override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2))
                + _factor * Math.Exp(-0.5 * Math.Pow((x.ChromXs.RT.Value - y.ChromXs.RT.Value) / _rttol, 2));
        }

        public override ChromXs GetCenter(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            return new ChromXs {
                RI = new RetentionIndex(chromFeatures.Argmax(n => n.PeakHeightTop).Mass),
                RT = new RetentionTime(chromFeatures.Select(n => n.ChromXsTop).Average(p => p.RT.Value))
            };
        }

        public override double GetAveragePeakWidth(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            return chromFeatures.Max(n => n.PeakWidth(ChromXType.RT));
        }
    }

    public class LcmsAlignmentProcessFactory : AlignmentProcessFactory {
        private MsdialLcmsParameter Param;
        private List<AnalysisFileBean> Files;

        public LcmsAlignmentProcessFactory(List<AnalysisFileBean> files, MsdialLcmsParameter param) {
            this.Files = files;
            this.Param = param;
            this.IonMode = param.IonMode;
            this.SmoothingMethod = param.SmoothingMethod;
            this.SmoothingLevel = param.SmoothingLevel;
            this.MzTol = param.Ms1AlignmentTolerance;
            this.RtTol = param.RetentionTimeAlignmentTolerance;
            this.IsForceInsert = param.IsForceInsertForGapFilling;
        }

        public override List<ChromatogramPeak> PeaklistOnChromCenter(ChromXs center, double peakWidth, List<RawSpectrum> spectrumList, int fileID = -1) {

            var mzTol = Param.Ms1AlignmentTolerance;
            mzTol = Math.Max(mzTol, 0.005f);
            peakWidth = Math.Max(peakWidth, 0.2f);

            var peaklist = DataAccess.GetMs1Peaklist(spectrumList, center.Mz.Value, mzTol, Param.IonMode, 
                ChromXType.RT, ChromXUnit.Min, center.RT.Value - peakWidth * 1.5, center.RT.Value + peakWidth * 1.5);
            return peaklist;
        }

    }
}
