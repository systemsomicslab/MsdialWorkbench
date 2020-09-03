using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.MsdialCore.DataObj
{
    public abstract class PeakComparer : IEqualityComparer<IChromatogramPeakFeature>, IComparer<IChromatogramPeakFeature>
    {
        public abstract int Compare(IChromatogramPeakFeature x, IChromatogramPeakFeature y);
        public abstract bool Equals(IChromatogramPeakFeature x, IChromatogramPeakFeature y);
        public abstract double GetSimilality(IChromatogramPeakFeature x, IChromatogramPeakFeature y);

        public virtual ChromXs GetCenter(IEnumerable<ChromXs> points) {
            return new ChromXs(points.Average(p => p.Value), ChromXType.RT, ChromXUnit.Min);
        }

        public virtual int GetHashCode(IChromatogramPeakFeature obj) {
            return obj.GetHashCode();
        }
    }
}
