using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;

namespace CompMs.MsdialDimsCore.DataObj
{
    public class DimsPeakComparer : PeakComparer
    {
        private double _mztol;

        public DimsPeakComparer(double mztol) {
            _mztol = mztol;
        }

        public override int Compare(IMSScanProperty x, IMSScanProperty y) {
            return x.PrecursorMz.CompareTo(y.PrecursorMz);
        }

        public override bool Equals(IMSScanProperty x, IMSScanProperty y) {
            return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol;
        }

        public override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2));
        }

        public override ChromXs GetCenter(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            return new ChromXs(chromFeatures.Select(n => n.ChromXsTop).Average(p => p.Mz.Value), ChromXType.Mz, ChromXUnit.Mz);
        }

        public override double GetAveragePeakWidth(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            return chromFeatures.Max(n => n.PeakWidth(ChromXType.Mz));
        }
    }
}
