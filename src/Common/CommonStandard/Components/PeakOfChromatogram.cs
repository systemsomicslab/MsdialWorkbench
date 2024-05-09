using CompMs.Common.Interfaces;
using System;

namespace CompMs.Common.Components
{
    /// <summary>
    /// Represents a specific peak within a chromatogram, detailing the peak's highest point (Top), 
    /// and its boundaries (Left and Right).
    /// </summary>
    /// <remarks>
    /// This class encapsulates the data for a chromatographic peak, providing functionality to analyze
    /// various attributes of the peak, such as its area, baseline area, amplitude, and validity based on
    /// a minimum amplitude criterion. It operates directly on an array of <see cref="ValuePeak"/> objects,
    /// simplifying the interaction with chromatographic data.
    /// </remarks>
    public sealed class PeakOfChromatogram
    {
        private readonly ValuePeak[] _chromatogram;
        private readonly ChromXType _type;
        private readonly ChromXUnit _unit;
        private readonly int _left;
        private readonly int _top;
        private readonly int _right;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeakOfChromatogram"/> class directly from an array of <see cref="ValuePeak"/> objects.
        /// </summary>
        /// <param name="chromatogram">The array of <see cref="ValuePeak"/> objects representing the chromatographic data from which to derive the peak.</param>
        /// <param name="type">The type of chromatographic measurement (e.g., retention time) applicable to all peaks in the array.</param>
        /// <param name="unit">The unit of measurement for the chromatographic data.</param>
        /// <param name="top">The index within the array of the peak's highest intensity.</param>
        /// <param name="left">The index within the array marking the start of the peak.</param>
        /// <param name="right">The index within the array marking the end of the peak.</param>
        /// <remarks>
        /// This constructor initializes a <see cref="PeakOfChromatogram"/> object directly from the provided <see cref="ValuePeak"/> array.
        /// It identifies the peak within the chromatogram based on the specified indices, outlining the peak's boundaries.
        /// </remarks>
        internal PeakOfChromatogram(ValuePeak[] chromatogram, ChromXType type, ChromXUnit unit, int top, int left, int right) {
            _chromatogram = chromatogram;
            _type = type;
            _unit = unit;
            _left = left;
            _top = top;
            _right = right;
        }

        /// <summary>
        /// Retrieves the center of peak as an <see cref="IChromatogramPeak"/>.
        /// </summary>
        /// <returns>The <see cref="IChromatogramPeak"/> at the center of the peak.</returns>
        public IChromatogramPeak GetTop() => _chromatogram[_top].ConvertToChromatogramPeak(_type, _unit);

        /// <summary>
        /// Retrieves the peak's left boundary as an <see cref="IChromatogramPeak"/>.
        /// </summary>
        /// <returns>The <see cref="IChromatogramPeak"/> at the left boundary of the peak.</returns>
        public IChromatogramPeak GetLeft() => _chromatogram[_left].ConvertToChromatogramPeak(_type, _unit);

        /// <summary>
        /// Retrieves the peak's right boundary as an <see cref="IChromatogramPeak"/>.
        /// </summary>
        /// <returns>The <see cref="IChromatogramPeak"/> at the right boundary of the peak.</returns>
        public IChromatogramPeak GetRight() => _chromatogram[_right].ConvertToChromatogramPeak(_type, _unit);

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
                chromatogram[i - _left] = _chromatogram[i].ConvertToChromatogramPeak(_type, _unit);
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
                result += (peaks[i + 1].Time - peaks[i].Time) * (peaks[i + 1].Intensity + peaks[i].Intensity) / 2;
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
            return (_chromatogram[_left].Intensity + _chromatogram[_right].Intensity) * (_chromatogram[_right].Time - _chromatogram[_left].Time) / 2;
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
            return _chromatogram[_top].Intensity - Math.Min(_chromatogram[_left].Intensity, _chromatogram[_right].Intensity);
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
            return _chromatogram[_top].Intensity > Math.Max(_chromatogram[_left].Intensity, _chromatogram[_right].Intensity)
                && CalculatePeakAmplitude() >= minimumAmplitude;
        }
    }
}
