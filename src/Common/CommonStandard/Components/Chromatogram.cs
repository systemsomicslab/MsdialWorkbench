using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Components
{
    /// <summary>
    /// Represents a chromatogram, a graphical representation of detector response, ion intensity or other measure of detector signal, as a function of retention time or another chromatographic run parameter.
    /// </summary>
    /// <remarks>
    /// The Chromatogram class encapsulates a collection of peaks (as <see cref="IChromatogramPeak"/> objects) along with their chromatographic type (e.g., retention time) and unit. It provides methods to access peak data, calculate chromatographic metrics, and identify peaks based on specified criteria.
    /// </remarks>
    public sealed class Chromatogram
    {
        private readonly IReadOnlyList<IChromatogramPeak> _peaks;
        private readonly ChromXType _type;
        private readonly ChromXUnit _unit;

        /// <summary>
        /// Initializes a new instance of the <see cref="Chromatogram"/> class with a specified collection of peaks, chromatographic type, and unit.
        /// </summary>
        /// <param name="peaks">A collection of peaks constituting the chromatogram.</param>
        /// <param name="type">The type of chromatographic data (e.g., retention time).</param>
        /// <param name="unit">The unit of measurement for the chromatographic data.</param>
        /// <exception cref="ArgumentNullException">Thrown when the peaks collection is null.</exception>
        public Chromatogram(IReadOnlyList<IChromatogramPeak> peaks, ChromXType type, ChromXUnit unit) {
            _peaks = peaks ?? throw new ArgumentNullException(nameof(peaks));
            _type = type;
            _unit = unit;
        }

        /// <summary>
        /// Returns a read-only list of peaks in the chromatogram.
        /// </summary>
        /// <returns>A read-only list of <see cref="IChromatogramPeak"/> objects.</returns>
        public IReadOnlyList<IChromatogramPeak> AsPeakArray() => _peaks;

        /// <summary>
        /// Generates a <see cref="ChromXs"/> object representing the chromatographic position and mass (m/z) for a given chromatographic value.
        /// </summary>
        /// <param name="chromValue">The chromatographic value (e.g., retention time, retention index, drift time).</param>
        /// <param name="mz">The m/z value associated with the chromatographic position.</param>
        /// <returns>A <see cref="ChromXs"/> object encapsulating the chromatographic position and m/z value.</returns>
        /// <remarks>
        /// This method is useful for converting raw chromatographic values and mass-to-charge ratios into a <see cref="ChromXs"/> object, which standardizes the representation of these values within the system.
        /// </remarks>
        public ChromXs PeakChromXs(double chromValue, double mz) {
            var result = new ChromXs(chromValue, _type, _unit);
            if (_type != ChromXType.Mz) {
                result.Mz = new MzValue(mz);
            }
            return result;
        }

        /// <summary>
        /// Determines if the chromatogram is empty, meaning it contains no peaks.
        /// </summary>
        /// <value>True if the chromatogram contains no peaks; otherwise, false.</value>
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

        public Chromatogram SmoothedChromatogram(SmoothingMethod method, int level) {
            return new Chromatogram(Smoothing(method, level), _type, _unit);
        }

        /// <summary>
        /// Creates a <see cref="PeakOfChromatogram"/> object representing the peak identified by the specified top, left, and right indices within the chromatogram.
        /// </summary>
        /// <param name="topIndex">The index of the peak's highest intensity point.</param>
        /// <param name="leftIndex">The index of the peak's left boundary.</param>
        /// <param name="rightIndex">The index of the peak's right boundary.</param>
        /// <returns>A <see cref="PeakOfChromatogram"/> object encapsulating the peak information.</returns>
        /// <remarks>
        /// This method is used to construct a peak object from the chromatogram based on the provided indices. It is important to ensure that the indices are within the bounds of the chromatogram data.
        /// </remarks>
        public PeakOfChromatogram AsPeak(int topIndex, int leftIndex, int rightIndex) {
            return new PeakOfChromatogram(_peaks, _type, topIndex, leftIndex, rightIndex);
        }

        /// <summary>
        /// Creates a <see cref="PeakOfChromatogram"/> object representing a peak within the specified time range.
        /// </summary>
        /// <param name="timeLeft">The left boundary of the time range to search for the peak.</param>
        /// <param name="timeRight">The right boundary of the time range to search for the peak.</param>
        /// <returns>A <see cref="PeakOfChromatogram"/> object representing the identified peak within the specified time range, or null if no peak is found within the range.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="timeLeft"/> is greater than <paramref name="timeRight"/>, indicating an invalid time range.</exception>
        /// <remarks>
        /// This method identifies the peak within the specified time range by finding the segment of chromatogram peaks that fall within the range,
        /// then selecting the peak with the highest intensity as the top of the identified peak. It calculates the left and right boundaries based on the time range provided,
        /// ensuring that the identified peak falls entirely within this range. If no peaks are found within the specified time range, the method returns null.
        /// </remarks>
        public PeakOfChromatogram? AsPeak(double timeLeft, double timeRight) {
            if (timeLeft > timeRight) {
                throw new ArgumentException($"The specified time boundaries are invalid: '{nameof(timeLeft)}' should be less than or equal to '{nameof(timeRight)}'.");
            }
            var leftIndex = _peaks.LowerBound(timeLeft, (p, l) => p.ChromXs.GetChromByType(_type).Value.CompareTo(l));
            if (leftIndex >= _peaks.Count || _peaks[leftIndex].ChromXs.GetChromByType(_type).Value > timeRight) {
                return null;
            }
            var rightIndex = leftIndex;
            while (rightIndex < _peaks.Count && _peaks[rightIndex].ChromXs.GetChromByType(_type).Value <= timeRight) {
                ++rightIndex;
            }
            rightIndex = Math.Max(leftIndex, rightIndex - 1);
            var topIndex = Enumerable.Range(leftIndex, rightIndex - leftIndex + 1).Argmax(i => _peaks[i].Intensity);
            return new PeakOfChromatogram(_peaks, _type, topIndex, leftIndex, rightIndex);
        }

        /// <summary>
        /// Creates a <see cref="PeakOfChromatogram"/> object representing a peak within the specified time boundaries, including a specified top time.
        /// </summary>
        /// <param name="timeLeft">The left boundary of the time range to search for the peak.</param>
        /// <param name="timeTop">The time corresponding to the peak's highest intensity point within the range.</param>
        /// <param name="timeRight">The right boundary of the time range to search for the peak.</param>
        /// <returns>A <see cref="PeakOfChromatogram"/> object representing the identified peak within the specified time boundaries, or null if no peak is found within the range.</returns>
        /// <exception cref="ArgumentException">Thrown if the time boundaries are invalid, specifically if <paramref name="timeLeft"/> is greater than <paramref name="timeTop"/>, or <paramref name="timeTop"/> is greater than <paramref name="timeRight"/>.</exception>
        /// <remarks>
        /// This method identifies a peak within the specified time boundaries by locating the segment of chromatogram peaks that falls within the range,
        /// and selecting the peak that is closest to the specified top time as the peak's top point. The method ensures that the identified peak falls entirely within the provided time boundaries.
        /// </remarks>
        public PeakOfChromatogram? AsPeak(double timeLeft, double timeTop, double timeRight) {
            if (timeLeft > timeTop || timeTop > timeRight) {
                throw new ArgumentException($"The specified time boundaries are invalid: '{nameof(timeLeft)}' should be less than or equal to '{nameof(timeTop)}', and '{nameof(timeTop)}' should be less than or equal to '{nameof(timeRight)}'.");
            }
            var leftIndex = _peaks.LowerBound(timeLeft, (p, l) => p.ChromXs.GetChromByType(_type).Value.CompareTo(l));
            if (leftIndex >= _peaks.Count || _peaks[leftIndex].ChromXs.GetChromByType(_type).Value > timeRight) {
                return null;
            }
            var rightIndex = leftIndex;
            while (rightIndex < _peaks.Count && _peaks[rightIndex].ChromXs.GetChromByType(_type).Value <= timeRight) {
                ++rightIndex;
            }
            rightIndex = Math.Max(leftIndex, rightIndex - 1);
            var topIndex = Enumerable.Range(leftIndex, rightIndex - leftIndex + 1).Argmin(i => Math.Abs(timeTop - _peaks[i].ChromXs.GetChromByType(_type).Value));
            return new PeakOfChromatogram(_peaks, _type, topIndex, leftIndex, rightIndex);
        }

        /// <summary>
        /// Searches for a peak within the chromatogram that matches the specified feature criteria.
        /// </summary>
        /// <param name="minPoints">The minimum number of points that the peak must span.</param>
        /// <param name="width">The maximum time width of the peak.</param>
        /// <param name="peakFeature">An object implementing <see cref="IChromatogramPeakFeature"/> that specifies the peak feature criteria.</param>
        /// <returns>A <see cref="PeakOfChromatogram"/> object representing the found peak.</returns>
        /// <remarks>
        /// This method searches the chromatogram for a peak that meets the criteria defined by <paramref name="peakFeature"/>. The search is constrained by the number of points and the width specified.
        /// </remarks>
        public PeakOfChromatogram FindPeak(int minPoints, double width, IChromatogramPeakFeature peakFeature) {
            var maxId = SearchPeakTop(peakFeature.ChromXsTop);
            var leftId = SearchLeftEdge(maxId, minPoints, width, peakFeature.ChromXsLeft);
            var rightId = SearchRightEdge(maxId, minPoints, width, peakFeature.ChromXsRight);
            var max = FindHighestIntensity(leftId, rightId + 1, maxId);
            return AsPeak(max, leftId, rightId);
        }

        private int SearchPeakTop(ChromXs top) {
            var center = SearchNearestPoint(top, _peaks);
            var maxID = center;
            var maxInt = double.MinValue;
            //finding local maximum within -2 ~ +2
            for (int i = center - 2; i <= center + 2; i++) {
                if (i <= 0) {
                    continue;
                }
                if (i + 1 >= _peaks.Count) {
                    break;
                }

                if (_peaks[i].Intensity > maxInt && _peaks[i - 1].Intensity <= _peaks[i].Intensity && _peaks[i].Intensity <= _peaks[i + 1].Intensity) {
                    maxInt = _peaks[i].Intensity;
                    maxID = i;
                }
            }
            return maxID;
        }

        private int SearchLeftEdge(int top, int minPoints, double width, ChromXs left) {
            //finding left edge;
            int? minLeftId = null;
            var minLeftInt = _peaks[top].Intensity;
            var leftEdge = _peaks[top].ChromXs.GetChromByType(_type).Value - width;
            for (int i = top - minPoints; i >= 0; i--) {
                if (minLeftInt < _peaks[i].Intensity || leftEdge > _peaks[i].ChromXs.GetChromByType(_type).Value) {
                    break;
                }

                minLeftInt = _peaks[i].Intensity;
                minLeftId = i;
            }
            if (minLeftId.HasValue) {
                return minLeftId.Value;
            }

            return SearchNearestPoint(left, _peaks.Take(top + 1));
        }

        private int SearchRightEdge(int top, int minPoints, double width, ChromXs right) {
            //finding right edge;
            int? minRightId = null;
            var minRightInt = _peaks[top].Intensity;
            double rightEdge = _peaks[top].ChromXs.GetChromByType(_type).Value + width;
            for (int i = top + minPoints; i < _peaks.Count - 1; i++) {
                if (minRightInt < _peaks[i].Intensity || rightEdge < _peaks[i].ChromXs.GetChromByType(_type).Value) {
                    break;
                }
                minRightInt = _peaks[i].Intensity;
                minRightId = i;
            }
            if (minRightId.HasValue) {
                return minRightId.Value;
            }

            return top + SearchNearestPoint(right, _peaks.Skip(top));
        }

        private int SearchNearestPoint(ChromXs chrom, IEnumerable<IChromatogramPeak> peaklist) {
            var target = chrom.GetChromByType(_type).Value;
            return peaklist
                .Select(peak => Math.Abs(peak.ChromXs.GetChromByType(_type).Value - target))
                .Argmin();
        }


        private int FindHighestIntensity(int start, int end, int defaultId) {
            var realMaxInt = double.MinValue;
            var realMaxID = defaultId;
            for (int i = start; i < end; i++) {
                if (realMaxInt < _peaks[i].Intensity) {
                    realMaxInt = _peaks[i].Intensity;
                    realMaxID = i;
                }
            }
            return realMaxID;
        }
    }
}
