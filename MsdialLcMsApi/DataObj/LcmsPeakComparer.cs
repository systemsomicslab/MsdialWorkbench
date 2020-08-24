using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;

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

        public override int Compare(IChromatogramPeakFeature x, IChromatogramPeakFeature y) {
            return (x.Mass, x.ChromXsTop.RT.Value).CompareTo((y.Mass, y.ChromXsTop.RT.Value));
        }

        public override bool Equals(IChromatogramPeakFeature x, IChromatogramPeakFeature y) {
            return Math.Abs(x.Mass - y.Mass) <= _mztol && Math.Abs(x.ChromXsTop.RT.Value - y.ChromXsTop.RT.Value) <= _rttol;
        }

        public override double GetSimilality(IChromatogramPeakFeature x, IChromatogramPeakFeature y) {
            return Math.Exp(-.5 * Math.Pow((x.Mass - y.Mass) / _mztol, 2))
                + _factor * Math.Exp(-0.5 * Math.Pow((x.ChromXsTop.RT.Value - y.ChromXsTop.RT.Value) / _rttol, 2));
        }

        public override ChromXs GetCenter(IEnumerable<ChromXs> points) {
            return new ChromXs {
                RT = new RetentionTime(points.Average(p => p.RT.Value)),
                Mz = new MzValue(points.Average(p => p.Mz.Value)),
            };
        }
    }
}
