using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialDimsCore.DataObj
{
    public class DimsPeakJoiner : PeakJoiner
    {
        private readonly double _mztol, _mzfactor;

        public DimsPeakJoiner(double mztol, double mzfactor) {
            _mztol = mztol;
            _mzfactor = mzfactor;
        }

        public DimsPeakJoiner(double mztol) : this(mztol, 1) { }

        protected override bool Equals(IMSScanProperty x, IMSScanProperty y) {
            return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol;
        }

        protected override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return _mzfactor * Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2));
        }
    }
}
