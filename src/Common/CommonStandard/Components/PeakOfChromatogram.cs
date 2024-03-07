using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace CompMs.Common.Components
{
    /// <summary>
    /// Represents a specific peak within a chromatogram, detailing the peak's highest point (Top), 
    /// and its boundaries (Left and Right).
    /// </summary>
    /// <remarks>
    /// This class provides functionality to analyze various attributes of a peak such as its area, baseline area, 
    /// amplitude, and validity based on a minimum amplitude criterion. It encapsulates the chromatographic data points 
    /// that constitute the peak and allows for the calculation of metrics important for chromatographic analysis.
    /// </remarks>
    public sealed class PeakOfChromatogram
    {
        private readonly IReadOnlyList<IChromatogramPeak> _chromatogram;
        private readonly ChromXType _type;
        private readonly int _left;
        private readonly int _top;
        private readonly int _right;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeakOfChromatogram"/> class.
        /// </summary>
        /// <param name="chromatogram">The list of peaks from which to derive the peak.</param>
        /// <param name="type">The type of chromatographic measurement (e.g., retention time).</param>
        /// <param name="top">The index of the peak's highest intensity within the chromatogram.</param>
        /// <param name="left">The index marking the start of the peak within the chromatogram.</param>
        /// <param name="right">The index marking the end of the peak within the chromatogram.</param>
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

        /// <summary>
        /// Extracts a segment of the chromatogram that corresponds to the peak area.
        /// </summary>
        /// <returns>An array of <see cref="IChromatogramPeak"/> representing the extracted segment of the chromatogram within the peak boundaries.</returns>
        /// <remarks>
        /// This method returns the portion of the chromatogram that spans from the left to the right boundary of the peak, inclusive. It's useful for further analysis or visualization of the peak's chromatographic profile.
        /// </remarks>
        public IChromatogramPeak[] SlicePeakArea() {
            var chromatogram = new IChromatogramPeak[_right - _left + 1];
            for (int i = _left; i <= _right; i++) {
                chromatogram[i - _left] = _chromatogram[i];
            }
            return chromatogram;
        }

        /// <summary>
        /// Calculates the area under the peak using the trapezoidal rule.
        /// </summary>
        /// <returns>The calculated area under the peak.</returns>
        /// <remarks>
        /// This method integrates the area under the peak between the left and right boundaries,
        /// providing a measure of the peak's quantity.
        /// </remarks>
        public double CalculateArea() {
            var result = 0d;
            var peaks = _chromatogram;
            for (int i = _left; i + 1 <= _right; i++) {
                result += (peaks[i + 1].ChromXs.GetChromByType(_type).Value - peaks[i].ChromXs.GetChromByType(_type).Value) * (peaks[i + 1].Intensity + peaks[i].Intensity) / 2;
            }
            return result;
        }

        /// <summary>
        /// Calculates the baseline area of the peak.
        /// </summary>
        /// <returns>The area under the baseline, calculated between the left and right peak boundaries.</returns>
        /// <remarks>
        /// This calculation assumes a straight line connecting the left and right boundaries of the peak
        /// and integrates the area under this line.
        /// </remarks>
        public double CalculateBaseLineArea() {
            return (_chromatogram[_left].Intensity + _chromatogram[_right].Intensity) * (_chromatogram[_right].ChromXs.GetChromByType(_type).Value - _chromatogram[_left].ChromXs.GetChromByType(_type).Value) / 2;
        }

        /// <summary>
        /// Calculates the amplitude of the peak.
        /// </summary>
        /// <returns>The difference in intensity between the peak's top and its lowest boundary.</returns>
        /// <remarks>
        /// The amplitude is a measure of the peak's signal strength, determined by subtracting the
        /// minimum boundary intensity from the peak's top intensity.
        /// </remarks>
        public double CalculatePeakAmplitude() {
            return Top.Intensity - Math.Min(Left.Intensity, Right.Intensity);
        }

        /// <summary>
        /// Determines whether the peak meets a specified minimum amplitude criterion.
        /// </summary>
        /// <param name="minimumAmplitude">The minimum amplitude required for a peak to be considered valid.</param>
        /// <returns><c>true</c> if the peak's amplitude is equal to or greater than the minimum; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method is useful for filtering out noise and ensuring that only significant peaks are analyzed.
        /// </remarks>
        public bool IsValid(double minimumAmplitude) {
            return Top.Intensity > Math.Max(Left.Intensity, Right.Intensity)
                && CalculatePeakAmplitude() >= minimumAmplitude;
        }
    }
}
