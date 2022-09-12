using CompMs.Common.DataObj;
using CompMs.Common.Utility;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public class Spectrum
    {
        private readonly RawPeakElement[] _elements;

        public Spectrum(IReadOnlyList<RawPeakElement> elements) {
            switch (elements) {
                case RawPeakElement[] arr:
                    _elements = arr;
                    break;
                case List<RawPeakElement> lst:
                    _elements = lst.ToArray();
                    break;
                default:
                    _elements = elements.ToArray();
                    break;
            }
        }

        public (double BasePeakMz, double BasePeakIntensity, double SummedIntensity) RetrieveBin(double mz, double tolerance) {
            var elements = _elements;
            var start = elements.LowerBound((mz - tolerance), (elem, target) => elem.Mz.CompareTo(target));
            var summedIntensity = 0d;
            var basepeakIntensity = 0d;
            var basepeakMz = mz;
            for (int i = start; i < elements.Length; i++) {
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
