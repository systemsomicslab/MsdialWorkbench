using CompMs.Common.Enum;
using System;
using System.Collections.Generic;

namespace CompMs.Common.Components {
    public class Chromatogram_temp2 {
        private readonly IReadOnlyList<ValuePeak> _peaks;
        private readonly ChromXType _type;
        private readonly ChromXUnit _unit;
        private readonly Algorithm.ChromSmoothing.Smoothing _smoother;

        public Chromatogram_temp2(IReadOnlyList<ValuePeak> peaks, ChromXType type, ChromXUnit unit) {
            _peaks = peaks ?? throw new ArgumentNullException(nameof(peaks));
            _type = type;
            _unit = unit;
            _smoother = new Algorithm.ChromSmoothing.Smoothing();
        }

        public IReadOnlyList<ValuePeak> Peaks => _peaks;
        public bool IsEmpty => _peaks.Count == 0;

        public ChromXs PeakChromXs(double chromValue, double mz) {
            var result = new ChromXs(chromValue, _type, _unit);
            if (_type != ChromXType.Mz) {
                result.Mz = new MzValue(mz);
            }
            return result;
        }

        public bool IsValidPeakTop(int topId) {
            return topId - 1 >= 0 && topId + 1 <= _peaks.Count - 1
                && _peaks[topId - 1].Intensity > 0 && _peaks[topId + 1].Intensity > 0;
        }

        public bool IsPeakTop(int topId) {
            return _peaks[topId - 1].Intensity <= _peaks[topId].Intensity
                && _peaks[topId].Intensity >= _peaks[topId + 1].Intensity;
        }

        public bool IsBottom(int bottomId) {
            return _peaks[bottomId - 1].Intensity >= _peaks[bottomId].Intensity
                && _peaks[bottomId].Intensity <= _peaks[bottomId + 1].Intensity;
        }

        public ValuePeak[] Smoothing(SmoothingMethod method, int level) {
            switch (method) {
                case SmoothingMethod.SimpleMovingAverage:
                    return Algorithm.ChromSmoothing.Smoothing.SimpleMovingAverage(_peaks, level).ToArray();
                case SmoothingMethod.SavitzkyGolayFilter:
                    return Algorithm.ChromSmoothing.Smoothing.SavitxkyGolayFilter(_peaks, level).ToArray();
                case SmoothingMethod.BinomialFilter:
                    return Algorithm.ChromSmoothing.Smoothing.BinomialFilter(_peaks, level).ToArray();
                case SmoothingMethod.LowessFilter:
                    return Algorithm.ChromSmoothing.Smoothing.LowessFilter(_peaks, level).ToArray();
                case SmoothingMethod.LoessFilter:
                    return Algorithm.ChromSmoothing.Smoothing.LoessFilter(_peaks, level).ToArray();
                case SmoothingMethod.LinearWeightedMovingAverage:
                default:
                    return _smoother.LinearWeightedMovingAverage(_peaks, level);
            }
        }
    }
}
