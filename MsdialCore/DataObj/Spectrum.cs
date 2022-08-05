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
            var start = elements.LowerBound((float)(mz - tolerance), (elem, target) => elem.Mz.CompareTo(target));
            //var end = elements.UpperBound((float)(mz + tolerance), (elem, target) => elem.Mz.CompareTo(target));
            var summedIntensity = 0d;
            var basepeakIntensity = 0d;
            var basepeakMz = 0d;
            for (int i = start; i < elements.Count; i++) {
                var peak = elements[i];
                if (peak.Mz > mz + tolerance) break;
                summedIntensity += peak.Intensity;
                if (basepeakIntensity < peak.Intensity) {
                    basepeakIntensity = peak.Intensity;
                    basepeakMz = peak.Mz;
                }
            }
            return (basepeakMz, basepeakIntensity, summedIntensity);
        }

        public (double BasePeakMz, double BasePeakIntensity, double SummedIntensity) RetrieveTotalIntensity() {
            var summedIntensity = 0d;
            var basepeakIntensity = 0d;
            var basepeakMz = 0d;
            foreach (var peak in _elements) {
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
