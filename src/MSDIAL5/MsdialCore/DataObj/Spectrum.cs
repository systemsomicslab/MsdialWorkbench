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

        internal IEnumerable<SummarizedSpectrum> RetrieveBins(IEnumerable<double> mzs, double tolerance) {
            var lo = 0;
            var hi = 0;
            foreach (var mz in mzs) {
                var inf = mz - tolerance;
                while (lo < _elements.Length && _elements[lo].Mz < inf) {
                    lo++;
                }
                if (hi < lo) {
                    hi = lo;
                }
                var sup = mz + tolerance;
                while (hi < _elements.Length && _elements[hi].Mz <= sup) {
                    hi++;
                }
                if (lo == hi) {
                    yield return new SummarizedSpectrum(mz, 0d, 0d);
                }
                else {
                    yield return SummarizeBin(lo, hi, mz);
                }
            }
        }

        private SummarizedSpectrum SummarizeBin(int lo, int hi, double mz) {
            var summedIntensity = 0d;
            var basePeakIntensity = 0d;
            var basePeakMz = mz;
            for (int i = lo; i < hi; i++) {
                summedIntensity += _elements[i].Intensity;
                if (basePeakIntensity < _elements[i].Intensity) {
                    basePeakIntensity = _elements[i].Intensity;
                    basePeakMz = _elements[i].Mz;
                }
            }
            return new SummarizedSpectrum(basePeakMz, basePeakIntensity, summedIntensity);
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

        internal readonly struct SummarizedSpectrum {
            public SummarizedSpectrum(double basePeakMz, double basePeakIntensity, double summedIntensity)
            {
                BasePeakMz = basePeakMz;
                BasePeakIntensity = basePeakIntensity;
                SummedIntensity = summedIntensity;
            }
            public double BasePeakMz { get; }
            public double BasePeakIntensity { get; }
            public double SummedIntensity { get; }
        }
    }
}
