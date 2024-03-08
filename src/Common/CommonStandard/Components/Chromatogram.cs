using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Components
{
    /// <summary>
    /// Represents a chromatogram, which is a graphical representation of detector response or ion intensity 
    /// as a function of retention time or another chromatographic run parameter.
    /// </summary>
    /// <remarks>
    /// The Chromatogram class encapsulates a collection of peaks represented by <see cref="ValuePeak"/> objects, 
    /// providing a unified structure for chromatographic data. It offers methods for accessing peak data, 
    /// calculating chromatographic metrics, identifying peaks within specific criteria, and smoothing chromatographic data.
    /// Implementing IDisposable, this class also ensures proper release of resources when no longer needed.
    /// </remarks>
    public class Chromatogram : IDisposable
    {
        protected ValuePeak[]? _peaks;
        protected readonly int _size;
        protected ArrayPool<ValuePeak>? _arrayPool;
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
            if (peaks is null) {
                throw new ArgumentNullException(nameof(peaks));
            }

            _peaks = peaks.Select(p => new ValuePeak(p.ID, p.ChromXs.GetChromByType(type).Value, p.Mass, p.Intensity)).ToArray();
            _size = _peaks.Length;
            _type = type;
            _unit = unit;
        }

        public Chromatogram(IEnumerable<ValuePeak> peaks, ChromXType type, ChromXUnit unit) {
            if (peaks is null) {
                throw new ArgumentNullException(nameof(peaks));
            }

            _peaks = peaks as ValuePeak[] ?? peaks.ToArray();
            _size = _peaks.Length;
            _type = type;
            _unit = unit;
        }

        internal Chromatogram(IEnumerable<ValuePeak> peaks, int size, ChromXType type, ChromXUnit unit, ArrayPool<ValuePeak> arrayPool) {
            if (peaks is null) {
                throw new ArgumentNullException(nameof(peaks));
            }

            _peaks = peaks as ValuePeak[] ?? peaks.ToArray();
            _size = size;
            _type = type;
            _unit = unit;
            _arrayPool = arrayPool;
        }

        /// <summary>
        /// Returns a read-only list of peaks in the chromatogram.
        /// </summary>
        /// <returns>A read-only list of <see cref="IChromatogramPeak"/> objects.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        public List<ChromatogramPeak> AsPeakArray() {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            }
            return _peaks.Select(p => p.ConvertToChromatogramPeak(_type, _unit)).ToList();
        }

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
        public bool IsEmpty => _size == 0;

        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        public Chromatogram ChromatogramSmoothing(SmoothingMethod method, int level) {
            return ChromatogramSmoothingCore(method, level);
        }

        protected virtual Chromatogram ChromatogramSmoothingCore(SmoothingMethod method, int level) {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            }
            ValuePeak[] peaks;
            if (method == SmoothingMethod.LinearWeightedMovingAverage || _peaks.Length == _size) {
                peaks = _peaks;
            }
            else {
                peaks = new ValuePeak[_size];
                Array.Copy(_peaks, peaks, _size);
            }

            switch (method) {
                case SmoothingMethod.SimpleMovingAverage:
                    return new Chromatogram(Algorithm.ChromSmoothing.Smoothing.SimpleMovingAverage(peaks, level), _type, _unit);
                case SmoothingMethod.SavitzkyGolayFilter:
                    return new Chromatogram(Algorithm.ChromSmoothing.Smoothing.SavitxkyGolayFilter(peaks, level), _type, _unit);
                case SmoothingMethod.BinomialFilter:
                    return new Chromatogram(Algorithm.ChromSmoothing.Smoothing.BinomialFilter(peaks, level), _type, _unit);
                case SmoothingMethod.LowessFilter:
                    return new Chromatogram(Algorithm.ChromSmoothing.Smoothing.LowessFilter(peaks, level), _type, _unit);
                case SmoothingMethod.LoessFilter:
                    return new Chromatogram(Algorithm.ChromSmoothing.Smoothing.LoessFilter(peaks, level), _type, _unit);
                case SmoothingMethod.LinearWeightedMovingAverage:
                default:
                    var arrayPool = _arrayPool ?? ArrayPool<ValuePeak>.Shared;
                    var smoothed = arrayPool.Rent(_size);
                    new Algorithm.ChromSmoothing.Smoothing().LinearWeightedMovingAverage(peaks, smoothed, _size, level);
                    return new Chromatogram(smoothed, _size, _type, _unit, arrayPool);
            }
        }

        /// <summary>
        /// Creates a <see cref="PeakOfChromatogram"/> object representing the peak identified by the specified top, left, and right indices within the chromatogram.
        /// </summary>
        /// <param name="topIndex">The index of the peak's highest intensity point.</param>
        /// <param name="leftIndex">The index of the peak's left boundary.</param>
        /// <param name="rightIndex">The index of the peak's right boundary.</param>
        /// <returns>A <see cref="PeakOfChromatogram"/> object encapsulating the peak information.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        /// <remarks>
        /// This method is used to construct a peak object from the chromatogram based on the provided indices. It is important to ensure that the indices are within the bounds of the chromatogram data.
        /// </remarks>
        public PeakOfChromatogram AsPeak(int topIndex, int leftIndex, int rightIndex) {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            }
            var copied = new ValuePeak[_size];
            Array.Copy(_peaks, copied, _size);
            return new PeakOfChromatogram(copied, _type, _unit, topIndex, leftIndex, rightIndex);
        }

        /// <summary>
        /// Creates a <see cref="PeakOfChromatogram"/> object representing a peak within the specified time range.
        /// </summary>
        /// <param name="timeLeft">The left boundary of the time range to search for the peak.</param>
        /// <param name="timeRight">The right boundary of the time range to search for the peak.</param>
        /// <returns>A <see cref="PeakOfChromatogram"/> object representing the identified peak within the specified time range, or null if no peak is found within the range.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="timeLeft"/> is greater than <paramref name="timeRight"/>, indicating an invalid time range.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        /// <remarks>
        /// This method identifies the peak within the specified time range by finding the segment of chromatogram peaks that fall within the range,
        /// then selecting the peak with the highest intensity as the top of the identified peak. It calculates the left and right boundaries based on the time range provided,
        /// ensuring that the identified peak falls entirely within this range. If no peaks are found within the specified time range, the method returns null.
        /// </remarks>
        public PeakOfChromatogram? AsPeak(double timeLeft, double timeRight) {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            }
            if (timeLeft > timeRight) {
                throw new ArgumentException($"The specified time boundaries are invalid: '{nameof(timeLeft)}' should be less than or equal to '{nameof(timeRight)}'.");
            }
            var leftIndex = _peaks.LowerBound(timeLeft, 0, _size, (p, l) => p.Time.CompareTo(l));
            if (leftIndex >= _size || _peaks[leftIndex].Time > timeRight) {
                return null;
            }
            var rightIndex = leftIndex;
            while (rightIndex < _size && _peaks[rightIndex].Time <= timeRight) {
                ++rightIndex;
            }
            rightIndex = Math.Max(leftIndex, rightIndex - 1);
            var topIndex = Enumerable.Range(leftIndex, rightIndex - leftIndex + 1).Argmax(i => _peaks[i].Intensity);
            var copied = new ValuePeak[_size];
            Array.Copy(_peaks, copied, _size);
            return new PeakOfChromatogram(copied, _type, _unit, topIndex, leftIndex, rightIndex);
        }

        /// <summary>
        /// Creates a <see cref="PeakOfChromatogram"/> object representing a peak within the specified time boundaries, including a specified top time.
        /// </summary>
        /// <param name="timeLeft">The left boundary of the time range to search for the peak.</param>
        /// <param name="timeTop">The time corresponding to the peak's highest intensity point within the range.</param>
        /// <param name="timeRight">The right boundary of the time range to search for the peak.</param>
        /// <returns>A <see cref="PeakOfChromatogram"/> object representing the identified peak within the specified time boundaries, or null if no peak is found within the range.</returns>
        /// <exception cref="ArgumentException">Thrown if the time boundaries are invalid, specifically if <paramref name="timeLeft"/> is greater than <paramref name="timeTop"/>, or <paramref name="timeTop"/> is greater than <paramref name="timeRight"/>.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        /// <remarks>
        /// This method identifies a peak within the specified time boundaries by locating the segment of chromatogram peaks that falls within the range,
        /// and selecting the peak that is closest to the specified top time as the peak's top point. The method ensures that the identified peak falls entirely within the provided time boundaries.
        /// </remarks>
        public PeakOfChromatogram? AsPeak(double timeLeft, double timeTop, double timeRight) {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            }
            if (timeLeft > timeTop || timeTop > timeRight) {
                throw new ArgumentException($"The specified time boundaries are invalid: '{nameof(timeLeft)}' should be less than or equal to '{nameof(timeTop)}', and '{nameof(timeTop)}' should be less than or equal to '{nameof(timeRight)}'.");
            }
            var leftIndex = _peaks.LowerBound(timeLeft, 0, _size, (p, l) => p.Time.CompareTo(l));
            if (leftIndex >= _size || _peaks[leftIndex].Time > timeRight) {
                return null;
            }
            var rightIndex = leftIndex;
            while (rightIndex < _size && _peaks[rightIndex].Time <= timeRight) {
                ++rightIndex;
            }
            rightIndex = Math.Max(leftIndex, rightIndex - 1);
            var topIndex = Enumerable.Range(leftIndex, rightIndex - leftIndex + 1).Argmin(i => Math.Abs(timeTop - _peaks[i].Time));
            var copied = new ValuePeak[_size];
            Array.Copy(_peaks, copied, _size);
            return new PeakOfChromatogram(copied, _type, _unit, topIndex, leftIndex, rightIndex);
        }

        /// <summary>
        /// Searches for a peak within the chromatogram that matches the specified feature criteria.
        /// </summary>
        /// <param name="minPoints">The minimum number of points that the peak must span.</param>
        /// <param name="width">The maximum time width of the peak.</param>
        /// <param name="peakFeature">An object implementing <see cref="IChromatogramPeakFeature"/> that specifies the peak feature criteria.</param>
        /// <returns>A <see cref="PeakOfChromatogram"/> object representing the found peak.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        /// <remarks>
        /// This method searches the chromatogram for a peak that meets the criteria defined by <paramref name="peakFeature"/>. The search is constrained by the number of points and the width specified.
        /// </remarks>
        public PeakOfChromatogram FindPeak(int minPoints, double width, IChromatogramPeakFeature peakFeature) {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            }
            var maxId = SearchPeakTop(peakFeature.ChromXsTop);
            var leftId = SearchLeftEdge(maxId, minPoints, width, peakFeature.ChromXsLeft);
            var rightId = SearchRightEdge(maxId, minPoints, width, peakFeature.ChromXsRight);
            var max = FindHighestIntensity(leftId, rightId + 1, maxId);
            return AsPeak(max, leftId, rightId);
        }

        private int SearchPeakTop(ChromXs top) {
            var center = SearchNearestPoint(top, _peaks.Take(_size));
            var maxID = center;
            var maxInt = double.MinValue;
            //finding local maximum within -2 ~ +2
            for (int i = center - 2; i <= center + 2; i++) {
                if (i <= 0) {
                    continue;
                }
                if (i + 1 >= _size) {
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
            var leftEdge = _peaks[top].Time - width;
            for (int i = top - minPoints; i >= 0; i--) {
                if (minLeftInt < _peaks[i].Intensity || leftEdge > _peaks[i].Time) {
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
            double rightEdge = _peaks[top].Time + width;
            for (int i = top + minPoints; i < _peaks.Length - 1; i++) {
                if (minRightInt < _peaks[i].Intensity || rightEdge < _peaks[i].Time) {
                    break;
                }
                minRightInt = _peaks[i].Intensity;
                minRightId = i;
            }
            if (minRightId.HasValue) {
                return minRightId.Value;
            }

            return top + SearchNearestPoint(right, _peaks.Take(_size).Skip(top));
        }

        private int SearchNearestPoint(ChromXs chrom, IEnumerable<ValuePeak> peaklist) {
            var target = chrom.GetChromByType(_type).Value;
            return peaklist
                .Select(peak => Math.Abs(peak.Time - target))
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

        /// <summary>
        /// Disposes the chromatogram, releasing or returning any resources (e.g., to an ArrayPool) as appropriate.
        /// </summary>
        /// <remarks>
        /// Call Dispose when you are finished using the Chromatogram. The Dispose method leaves the Chromatogram in an unusable state.
        /// After calling Dispose, you must release all references to the Chromatogram so the garbage collector can reclaim the memory that the Chromatogram was occupying.
        /// </remarks>
        public void Dispose() {
            if (_arrayPool is null) {
                return;
            }
            _arrayPool.Return((ValuePeak[])_peaks);
            _peaks = null;
            _arrayPool = null;
        }
    }
}
