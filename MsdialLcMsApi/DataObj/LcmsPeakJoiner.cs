using System;
using System.Collections.Generic;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;

namespace CompMs.MsdialLcMsApi.DataObj
{
    public class LcmsPeakJoiner : PeakJoiner
    {
        private readonly double _mztol;
        private readonly double _rttol;
        private readonly double _mzfactor;
        private readonly double _rtfactor;

        public LcmsPeakJoiner(double rttol, double rtfactor, double mztol, double mzfactor) {
            _rttol = rttol;
            _rtfactor = rtfactor;
            _mztol = mztol;
            _mzfactor = mzfactor;
        }

        public LcmsPeakJoiner(double rttol, double mztol) : this(rttol, 1, mztol, 1) { }

        protected override bool Equals(IMSScanProperty x, IMSScanProperty y) {
            return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol && Math.Abs(x.ChromXs.RT.Value - y.ChromXs.RT.Value) <= _rttol;
        }

        protected override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return _mzfactor * Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2))
                 + _rtfactor * Math.Exp(-0.5 * Math.Pow((x.ChromXs.RT.Value - y.ChromXs.RT.Value) / _rttol, 2));
        }
    }
}
