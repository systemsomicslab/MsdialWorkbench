using CompMs.Common.Components;
using System;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics
{
    class SpectrumEqualityComparer :  IEqualityComparer<SpectrumPeak>
    {
        private static readonly double EPS = 1e6;
        public bool Equals(SpectrumPeak x, SpectrumPeak y) {
            return Math.Abs(x.Mass - y.Mass) <= EPS;
        }

        public int GetHashCode(SpectrumPeak obj) {
            return Math.Round(obj.Mass, 6).GetHashCode();
        }
    }
}
