using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace CompMs.Common.Components
{
    public sealed class PeakOfChromatogram
    {
        private readonly IReadOnlyList<IChromatogramPeak> _chromatogram;
        private readonly ChromXType _type;
        private readonly int _left;
        private readonly int _top;
        private readonly int _right;

        internal PeakOfChromatogram(IReadOnlyList<IChromatogramPeak> chromatogram, ChromXType type, int top, int left, int right) {
            _chromatogram = chromatogram;
            _type = type;
            _left = left;
            _top = top;
            _right = right;

            Top = _chromatogram[top];
            Left = _chromatogram[left];
            Right = _chromatogram[right];
        }

        public IChromatogramPeak Top { get; }
        public IChromatogramPeak Left { get; }
        public IChromatogramPeak Right { get; }

        public double CalculateArea() {
            var result = 0d;
            var peaks = _chromatogram;
            for (int i = _left; i + 1 <= _right; i++) {
                result += (peaks[i + 1].ChromXs.GetChromByType(_type).Value - peaks[i].ChromXs.GetChromByType(_type).Value) * (peaks[i + 1].Intensity + peaks[i].Intensity) / 2;
            }
            return result;
        }

        public double CalculateBaseLineArea() {
            return (_chromatogram[_left].Intensity + _chromatogram[_right].Intensity) * (_chromatogram[_right].ChromXs.GetChromByType(_type).Value - _chromatogram[_left].ChromXs.GetChromByType(_type).Value) / 2;
        }

        public double CalculatePeakAmplitude() {
            return Top.Intensity - Math.Min(Left.Intensity, Right.Intensity);
        }

        public bool IsValid(double minimumAmplitude) {
            return Top.Intensity > Math.Max(Left.Intensity, Right.Intensity)
                && CalculatePeakAmplitude() >= minimumAmplitude;
        }
    }
}
