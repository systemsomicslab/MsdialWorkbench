using System;

namespace CompMs.Common.Algorithm.PeakPick
{
    internal sealed class ChromatogramBaseline
    {
        private readonly double _intensityMedian;
        private readonly double _noise;
        private readonly double _noiseFactor;
        private readonly bool _isHighBaseline;

        public ChromatogramBaseline(double intensityMedian, bool isHighBaseline, double noise, double noiseFactor) {
            _intensityMedian = intensityMedian;
            _noise = noise;
            _noiseFactor = noiseFactor;
            _isHighBaseline = isHighBaseline;
        }

        public float EstimateNoise => Math.Max(1f, (float)(_noise / _noiseFactor));

        public bool IsNoise(double peakHeight) {
            return peakHeight < _noise;
        }

        public bool IsNoiseIfHighBaseline(double intensity) {
            return _isHighBaseline && intensity < _intensityMedian;
        }
    }
}
