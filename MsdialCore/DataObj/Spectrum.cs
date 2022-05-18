using CompMs.Common.DataObj;
using CompMs.Common.Utility;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    public class Spectrum
    {
        private readonly IReadOnlyList<RawPeakElement> _elements;

        public Spectrum(IReadOnlyList<RawPeakElement> elements) {
            _elements = elements;
        }

        public (double BasePeakMz, double BasePeakIntensity, double SummedIntensity) RetrieveBin(double mz, double tolerance) {
            var elements = _elements;
            var start = elements.LowerBound(mz - tolerance, (elem, target) => elem.Mz.CompareTo(target));
            var end = elements.UpperBound(mz + tolerance, (elem, target) => elem.Mz.CompareTo(target));
            var summedIntensity = 0d;
            var basepeakIntensity = 0d;
            var basepeakMz = 0d;
            for (int i = start; i < end; i++) {
                var peak = elements[i];
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
