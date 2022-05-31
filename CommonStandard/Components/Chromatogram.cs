using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System.Collections.Generic;

namespace CompMs.Common.Components
{
    public class Chromatogram
    {
        private readonly IReadOnlyList<IChromatogramPeak> _peaks;
        private readonly ChromXType _type;
        private readonly ChromXUnit _unit;

        public Chromatogram(IReadOnlyList<IChromatogramPeak> peaks, ChromXType type, ChromXUnit unit) {
            _peaks = peaks ?? throw new System.ArgumentNullException(nameof(peaks));
            _type = type;
            _unit = unit;
        }

        public IReadOnlyList<IChromatogramPeak> Peaks => _peaks;

        public ChromXs PeakChromXs(double chromValue, double mz) {
            var result = new ChromXs(chromValue, _type, _unit);
            if (_type != ChromXType.Mz) {
                result.Mz = new MzValue(mz);
            }
            return result;
        }

        public bool IsEmpty => _peaks.Count == 0;

        public List<ChromatogramPeak> Smoothing(SmoothingMethod method, int level) {
            switch (method) {
                case SmoothingMethod.SimpleMovingAverage:
                    return Algorithm.ChromSmoothing.Smoothing.SimpleMovingAverage(_peaks, level);
                case SmoothingMethod.SavitzkyGolayFilter:
                    return Algorithm.ChromSmoothing.Smoothing.SavitxkyGolayFilter(_peaks, level);
                case SmoothingMethod.BinomialFilter:
                    return Algorithm.ChromSmoothing.Smoothing.BinomialFilter(_peaks, level);
                case SmoothingMethod.LowessFilter:
                    return Algorithm.ChromSmoothing.Smoothing.LowessFilter(_peaks, level);
                case SmoothingMethod.LoessFilter:
                    return Algorithm.ChromSmoothing.Smoothing.LoessFilter(_peaks, level);
                case SmoothingMethod.LinearWeightedMovingAverage:
                default:
                    return Algorithm.ChromSmoothing.Smoothing.LinearWeightedMovingAverage(_peaks, level);
            }
        }
    }
}
