using CompMs.Common.DataObj;
using CompMs.Common.Utility;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Spectrum
{
    internal class Spectrum
    {
        private readonly List<RawPeakElement> _elements;

        public Spectrum(IReadOnlyList<RawPeakElement> elements) {
            _elements = elements.OrderBy(element => element.Mz).ToList();
        }

        public (double, double, double) RetrieveBin(double mz, double tolerance) {
            var start = _elements.LowerBound(mz - tolerance, (elem, target) => elem.Mz.CompareTo(target));
            var end = _elements.UpperBound(mz + tolerance, (elem, target) => elem.Mz.CompareTo(target));
            var summedIntensity = 0d;
            var basepeakIntensity = 0d;
            var basepeakMz = 0d;
            for (int i = start; i < end; i++) {
                var peak = _elements[i];
                summedIntensity += peak.Intensity;
                if (basepeakIntensity < peak.Intensity) {
                    basepeakIntensity = peak.Intensity;
                    basepeakMz = peak.Mz;
                }
            }
            return (basepeakMz, basepeakIntensity, summedIntensity);
        }
    }
}
