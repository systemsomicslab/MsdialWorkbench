using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;

namespace CompMs.MsdialDimsCore.DataObj
{
    public class DimsPeakComparer : PeakComparer
    {
        private double _mztol;

        public DimsPeakComparer(double mztol) {
            _mztol = mztol;
        }

        public override int Compare(IChromatogramPeakFeature x, IChromatogramPeakFeature y) {
            return x.Mass.CompareTo(y.Mass);
        }

        public override bool Equals(IChromatogramPeakFeature x, IChromatogramPeakFeature y) {
            return Math.Abs(x.Mass - y.Mass) <= _mztol;
        }

        public override double GetSimilality(IChromatogramPeakFeature x, IChromatogramPeakFeature y) {
            return Math.Exp(-.5 * Math.Pow((x.Mass - y.Mass) / _mztol, 2));
        }

        public override ChromXs GetCenter(IEnumerable<ChromXs> points) {
            return new ChromXs(points.Average(p => p.Mz.Value), ChromXType.Mz, ChromXUnit.Mz);
        }
    }
}
