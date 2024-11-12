using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Mathematics.Basic;
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
        protected readonly ChromXType _type;
        protected readonly ChromXUnit _unit;

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
            var result = new List<ChromatogramPeak>(_size);
            for (int i = 0; i < _size; i++) {
                result.Add(_peaks[i].ConvertToChromatogramPeak(_type, _unit));
            }
            return result;
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

        public int Length => _size;

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
                case SmoothingMethod.TimeBasedLinearWeightedMovingAverage: {
                        var arrayPool = _arrayPool ?? ArrayPool<ValuePeak>.Shared;
                        var smoothed = arrayPool.Rent(_size);
                        new Algorithm.ChromSmoothing.Smoothing().TimeBasedLinearWeightedMovingAverage(peaks, smoothed, _size, level);
                        return new Chromatogram(smoothed, _size, _type, _unit, arrayPool);
                    }
                case SmoothingMethod.LinearWeightedMovingAverage:
                default: {
                        var arrayPool = _arrayPool ?? ArrayPool<ValuePeak>.Shared;
                        var smoothed = arrayPool.Rent(_size);
                        new Algorithm.ChromSmoothing.Smoothing().LinearWeightedMovingAverage(peaks, smoothed, _size, level);
                        return new Chromatogram(smoothed, _size, _type, _unit, arrayPool);
                    }
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
            for (int i = top + minPoints; i < _size - 1; i++) {
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
        /// Calculates the median intensity value of the peaks within the chromatogram.
        /// </summary>
        /// <returns>The median intensity value as a <see cref="double"/>.</returns>
        /// <remarks>
        /// This method computes the median of the intensity values of all peaks present in the chromatogram, providing a measure of the central tendency of peak intensities. It is useful for understanding the overall intensity distribution and for identifying the middle intensity value when peaks are sorted by intensity. This can be particularly helpful in noise reduction algorithms or when setting intensity thresholds for peak detection.
        /// </remarks>
        public double GetIntensityMedian() {
            return BasicMathematics.InplaceSortMedian(_peaks.Take(_size).Select(peak => peak.Intensity).ToArray());
        }

        /// <summary>
        /// Calculates the maximum intensity value among all peaks within the chromatogram.
        /// </summary>
        /// <returns>The maximum intensity value as a <see cref="double"/>.</returns>
        /// <remarks>
        /// This method finds the peak with the highest intensity in the chromatogram, which can be particularly useful for identifying the most prominent peak or for setting intensity thresholds for peak detection algorithms. The maximum intensity value serves as a reference for the dynamic range of the chromatogram and can be used in various data normalization and analysis tasks.
        /// </remarks>
        public double GetMaximumIntensity() {
            return _peaks.Take(_size).Select(peak => peak.Intensity).DefaultIfEmpty().Max();
        }

        /// <summary>
        /// Calculates the minimum intensity value among all peaks within the chromatogram.
        /// </summary>
        /// <returns>The minimum intensity value as a <see cref="double"/>.</returns>
        /// <remarks>
        /// This method identifies the peak with the lowest intensity in the chromatogram, useful for baseline correction, noise reduction, and setting intensity thresholds for peak detection. The minimum intensity value provides insight into the noise level or the least significant peaks within the chromatogram, aiding in various preprocessing and analysis steps.
        /// </remarks>
        public double GetMinimumIntensity() {
            return _peaks.Take(_size).Select(peak => peak.Intensity).DefaultIfEmpty().Min();
        }

        /// <summary>
        /// Estimates the minimum noise level within a specified range of the chromatogram by analyzing the intensity variations across the peaks.
        /// </summary>
        /// <param name="noiseParameter">An instance of <see cref="NoiseEstimateParameter"/> containing configuration values such as bin size, minimum window size, and minimum noise level for the calculation.</param>
        /// <returns>The estimated minimum noise level as a <see cref="double"/>.</returns>
        /// <remarks>
        /// This method provides a mechanism for estimating the background noise level in chromatographic data by analyzing the variation in peak intensities across different sections of the chromatogram.
        /// The method utilizes a binning approach, grouping peaks based on intensity to evaluate noise across the chromatogram. If the number of valid bins is smaller than the specified minimum window size, a default minimum noise level is returned. This is useful for distinguishing true signals from noise, improving peak detection algorithms, and setting baseline intensity thresholds.
        /// </remarks>
        public double GetMinimumNoiseLevel(NoiseEstimateParameter noiseParameter) {
            var binSize = noiseParameter.NoiseEstimateBin;
            var minWindowSize = noiseParameter.MinimumNoiseWindowSize;
            var minNoiseLevel = noiseParameter.MinimumNoiseLevel;
            var buffer = ArrayPool<double>.Shared.Rent((_size + binSize - 1) / binSize);
            try {
                var size = 0;
                int i = 0;
                while(i < _size) {
                    var (min, max) = (double.MaxValue, double.MinValue);
                    for (int j = 0; j < binSize; j++) {
                        min = Math.Min(min, _peaks[i].Intensity);
                        max = Math.Max(max, _peaks[i].Intensity);
                        if (++i == _size) {
                            break;
                        }
                    }
                    if (min < max) {
                        buffer[size++] = max - min;
                    }
                }
                return size >= minWindowSize
                    ? BasicMathematics.InplaceSortMedian(buffer, size)
                    : minNoiseLevel;
            }
            finally {
                ArrayPool<double>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Calculates the difference between this chromatogram and another, based on the intensity of peaks.
        /// </summary>
        /// <param name="other">The <see cref="ExtractedIonChromatogram"/> to compare with.</param>
        /// <returns>A new <see cref="ExtractedIonChromatogram"/> representing the difference in intensity between the two chromatograms.</returns>
        /// <remarks>
        /// This method generates a new chromatogram where each peak's intensity is the result of subtracting the intensity of the corresponding peak in the 'other' chromatogram from this chromatogram's peak intensity. Negative intensity values are floored at zero, as negative intensities are not physically meaningful in this context. This can be useful for background subtraction, noise reduction, or identifying significant changes in peak intensities between experiments.
        /// </remarks>
        public Chromatogram Difference(Chromatogram other) {
            System.Diagnostics.Debug.Assert(_type == other._type);
            System.Diagnostics.Debug.Assert(_unit == other._unit);
            System.Diagnostics.Debug.Assert(_size == other._size);
            var arrayPool = _arrayPool ?? ArrayPool<ValuePeak>.Shared;
            var peaks = arrayPool.Rent(_size);
            for (int i = 0; i < _size; i++) {
                peaks[i] = new ValuePeak(_peaks[i].Id, _peaks[i].Time, _peaks[i].Mz, Math.Max(0, _peaks[i].Intensity - other._peaks[i].Intensity));
            }
            return new Chromatogram(peaks, _size, _type, _unit, arrayPool);
        }

        /// <summary>
        /// Determines if the peak at the specified index is a peak top based on its intensity compared to its immediate neighbors.
        /// </summary>
        /// <param name="topId">The zero-based index of the peak to evaluate.</param>
        /// <returns><c>true</c> if the specified peak is higher in intensity than its immediate neighbors; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// A peak is considered a peak top if its intensity is not less than the intensities of the peaks immediately before and after it.
        /// This method does not consider the broader context of the chromatogram, such as whether the peak is part of a larger peak structure.
        /// </remarks>
        public bool IsPeakTop(int topId) {
            return topId >= 1 && topId < _size - 1
                && _peaks[topId - 1].Intensity <= _peaks[topId].Intensity
                && _peaks[topId].Intensity >= _peaks[topId + 1].Intensity;
        }

        /// <summary>
        /// Determines if the peak at the specified index is a large peak top, considering its intensity relative to its immediate and next-outer neighbors.
        /// </summary>
        /// <param name="topId">The zero-based index of the peak to evaluate.</param>
        /// <returns><c>true</c> if the specified peak's intensity is not less than its immediate neighbors and those neighbors' intensities are not less than their respective next-outer neighbors; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// A large peak top is defined as a peak that is higher than its immediate neighbors, which in turn are higher than their next-outer neighbors.
        /// </remarks>
        public bool IsLargePeakTop(int topId) {
            return topId - 2 >= 0 && topId + 2 < _size
                && _peaks[topId - 2].Intensity <= _peaks[topId - 1].Intensity
                && IsPeakTop(topId)
                && _peaks[topId + 1].Intensity >= _peaks[topId + 2].Intensity;
        }

        /// <summary>
        /// Determines if the peak at the specified index qualifies as a broad peak top, taking into account its intensity compared to both its immediate and next-outer neighbors.
        /// </summary>
        /// <param name="topId">The zero-based index of the peak to evaluate.</param>
        /// <returns><c>true</c> if the specified peak is a broad peak top, considering its intensity relative to its neighbors; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// A broad peak top is identified not only by its intensity being no less than its immediate neighbors but also if at least one set of next-outer neighbors maintains or increases intensity towards the peak. This method is useful for identifying peaks that might represent broader chromatographic features, potentially indicative of complex or co-eluting compounds.
        /// </remarks>
        public bool IsBroadPeakTop(int topId) {
            return IsPeakTop(topId) &&
                (topId - 2 >= 0 && _peaks[topId - 2].Intensity <= _peaks[topId - 1].Intensity ||
                topId + 2 < _size && _peaks[topId + 1].Intensity >= _peaks[topId + 2].Intensity);
        }

        /// <summary>
        /// Determines if the segment centered at the specified index exhibits flat characteristics based on intensity compared to its immediate neighbors.
        /// </summary>
        /// <param name="centerId">The zero-based index of the center point of the segment to evaluate.</param>
        /// <param name="amplitudeNoise">The maximum allowed difference in intensity between the center point and its neighbors for the segment to be considered flat.</param>
        /// <returns><c>true</c> if the difference in intensity between the center point and its immediate neighbors is less than or equal to the specified amplitude noise threshold; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// A flat segment is indicative of a plateau or a very stable region in the chromatogram where there is minimal variation in intensity. This method can be used to identify such regions, which may be significant for certain analyses, such as baseline correction or noise reduction.
        /// </remarks>
        public bool IsFlat(int centerId, double amplitudeNoise) {
            return centerId - 1 >= 0 && centerId + 1 < _size
                && Math.Abs(_peaks[centerId - 1].Intensity - _peaks[centerId].Intensity) < amplitudeNoise
                && Math.Abs(_peaks[centerId].Intensity - _peaks[centerId + 1].Intensity) < amplitudeNoise;
        }

        /// <summary>
        /// Determines if the peak at the specified index represents a bottom based on its intensity compared to its immediate neighbors.
        /// </summary>
        /// <param name="bottomId">The zero-based index of the peak to evaluate.</param>
        /// <returns><c>true</c> if the specified peak's intensity is lower than that of its immediate neighbors; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// A bottom is defined as a point in the chromatogram where the intensity is lower than the intensities of the peaks immediately before and after it.
        /// Identifying bottoms is crucial for understanding the valleys in chromatographic data, which can be important for separating closely adjacent peaks
        /// or for noise reduction and baseline correction strategies.
        /// </remarks>
        public bool IsBottom(int bottomId) {
            return bottomId - 1 >= 0 && bottomId + 1 < _size
                && _peaks[bottomId - 1].Intensity >= _peaks[bottomId].Intensity
                && _peaks[bottomId].Intensity <= _peaks[bottomId + 1].Intensity;
        }

        /// <summary>
        /// Determines if the peak at the specified index is a large bottom, considering its intensity relative to its immediate and next-outer neighbors.
        /// </summary>
        /// <param name="bottomId">The zero-based index of the peak to evaluate.</param>
        /// <returns><c>true</c> if the specified peak's intensity is lower than its immediate neighbors' intensities, which in turn are lower than their respective next-outer neighbors; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// A large bottom is defined as a valley that is deeper than the immediate surrounding by at least two levels of neighbors on each side. This method is useful for identifying significant valleys or dips in the chromatogram that may indicate distinct separations between peaks. It helps in distinguishing between minor variations in intensity and more significant troughs that could be relevant for peak detection algorithms.
        /// </remarks>
        public bool IsLargeBottom(int bottomId) {
            return bottomId - 2 >= 0 && bottomId + 2 < _size
                && _peaks[bottomId - 2].Intensity >= _peaks[bottomId - 1].Intensity
                && IsBottom(bottomId)
                && _peaks[bottomId + 1].Intensity <= _peaks[bottomId + 2].Intensity;
        }

        /// <summary>
        /// Determines if the peak at the specified index represents a broad bottom, taking into account its intensity relative to its immediate neighbors and allowing for a more flexible definition compared to a large bottom.
        /// </summary>
        /// <param name="bottomId">The zero-based index of the peak to evaluate.</param>
        /// <returns><c>true</c> if the specified peak's intensity is lower than its immediate neighbors or exhibits a broad valley characteristic by not being strictly higher than its next-outer neighbors; otherwise, <c>false</c>.</returns>
        public bool IsBroadBottom(int bottomId) {
            return IsBottom(bottomId)
                && (bottomId - 2 >= 0 && _peaks[bottomId - 2].Intensity >= _peaks[bottomId - 1].Intensity
                || bottomId + 2 < _size && _peaks[bottomId + 1].Intensity <= _peaks[bottomId + 2].Intensity);
        }

        /// <summary>
        /// Retrieves the intensity of the peak at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the peak.</param>
        /// <returns>The intensity value of the specified peak.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        public double Intensity(int index) {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            } 
            return _peaks[index].Intensity;
        }

        /// <summary>
        /// Calculates the difference in intensity between two peaks identified by their indices.
        /// </summary>
        /// <param name="i">The index of the first peak.</param>
        /// <param name="j">The index of the second peak.</param>
        /// <returns>The difference in intensity between the first and second peak. A positive value indicates the first peak has a higher intensity, while a negative value indicates the second peak has a higher intensity.</returns>
        /// <remarks>
        /// This method provides a straightforward way to compare the intensity of two peaks, which can be useful in peak detection and analysis algorithms.
        /// </remarks>
        public double IntensityDifference(int i, int j) {
            return _peaks[i].Intensity - _peaks[j].Intensity;
        }

        public ChromatogramGlobalProperty_temp2 GetProperty(NoiseEstimateParameter parameter) {
            using var sChromatogram = ChromatogramSmoothing(SmoothingMethod.LinearWeightedMovingAverage, 1);
            var ssChromatogram = sChromatogram.ChromatogramSmoothing(SmoothingMethod.LinearWeightedMovingAverage, 1);
            var baselineChromatogram = ChromatogramSmoothing(SmoothingMethod.LinearWeightedMovingAverage, 20);
            var baselineCorrectedChromatogram = ssChromatogram.Difference(baselineChromatogram);
            var noise = baselineCorrectedChromatogram.GetMinimumNoiseLevel(parameter) * parameter.NoiseFactor;

            // checking chromatogram properties
            var baselineMedian = GetIntensityMedian();
            var maxChromIntensity = GetMaximumIntensity();
            var minChromIntensity = GetMinimumIntensity();
            var isHighBaseline = baselineMedian > (maxChromIntensity + minChromIntensity) * 0.5;
            return new ChromatogramGlobalProperty_temp2(maxChromIntensity, minChromIntensity, baselineMedian, noise, isHighBaseline, ssChromatogram, baselineChromatogram, baselineCorrectedChromatogram);
        }

        /// <summary>
        /// Determines if the start or end boundary of a peak range has an intensity below a specified threshold.
        /// </summary>
        /// <param name="start">The zero-based start index of the peak range, inclusive.</param>
        /// <param name="end">The zero-based end index of the peak range, exclusive.</param>
        /// <param name="threshold">The intensity threshold for evaluating the peak boundaries.</param>
        /// <returns><c>true</c> if either the start or the end boundary (exclusive) of the peak has an intensity below the specified threshold; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method checks the intensities at the start and just before the end index within the specified range against a threshold. It is useful for identifying peaks that may not be significant or peaks that are close to the baseline noise level.
        /// </remarks>
        public bool HasBoundaryBelowThreshold(int start, int end, double threshold) {
            return Math.Min(_peaks[start].Intensity, _peaks[end - 1].Intensity) < threshold;
        }

        /// <summary>
        /// Calculates the minimum and maximum height of a peak from its boundaries relative to the peak top.
        /// </summary>
        /// <param name="start">The start index of the peak range.</param>
        /// <param name="end">The end index of the peak range, exclusive. The peak at this index is not included in the range.</param>
        /// <param name="top">The index of the peak top within the range.</param>
        /// <returns>A tuple containing the minimum and maximum height of the peak relative to its left and right boundaries.</returns>
        /// <remarks>
        /// This method assesses the peak's prominence by calculating its height from the baseline established by its boundaries.
        /// </remarks>
        public (double MinHeight, double MaxHeight) PeakHeightFromBounds(int start, int end, int top) {
            var topIntensity = _peaks[top].Intensity;
            var leftIntensity = _peaks[start].Intensity;
            var rightIntensity = _peaks[end - 1].Intensity;
            return (topIntensity - Math.Max(leftIntensity, rightIntensity), topIntensity - Math.Min(leftIntensity, rightIntensity));
        }

        /// <summary>
        /// Calculates the difference in time (or chromatographic position) between two peaks identified by their indices.
        /// </summary>
        /// <param name="i">The index of the first peak.</param>
        /// <param name="j">The index of the second peak.</param>
        /// <returns>The time difference between the two peaks. A positive value indicates the first peak occurs later in the chromatogram, while a negative value indicates the second peak occurs later.</returns>
        public double TimeDifference(int i, int j) {
            return _peaks[i].Time - _peaks[j].Time;
        }

        /// <summary>
        /// Calculates the area between two peaks using the trapezoidal rule.
        /// </summary>
        /// <param name="i">The index of the first peak in the chromatogram.</param>
        /// <param name="j">The index of the second peak in the chromatogram.</param>
        /// <returns>The calculated area between the two peaks based on their intensities and time difference.</returns>
        /// <remarks>
        /// This method estimates the area under the curve delineated by two points on the chromatogram, which can be useful for quantitative analysis.
        /// </remarks>
        public double CalculateArea(int i, int j) {
            return (_peaks[i].Intensity + _peaks[j].Intensity) * Math.Abs(_peaks[i].Time - _peaks[j].Time) / 2;
        }

        /// <summary>
        /// Retrieves the time of the peak at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the peak.</param>
        /// <returns>The time value of the specified peak.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        public double Time(int index) {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            } 
            return _peaks[index].Time;
        }

        /// <summary>
        /// Adjusts the peak range to more accurately define the peak boundaries based on the intensity profile.
        /// </summary>
        /// <param name="start">The zero-based initial start index of the peak range.</param>
        /// <param name="end">The zero-based initial end index of the peak range, exclusive. The peak at this index is not included in the range.</param>
        /// <param name="averagePeakWidth">An estimate of the average width of peaks to guide the shrinking process.</param>
        /// <returns>A tuple containing the new start index, peak top index, and new end index (exclusive) of the adjusted peak range.</returns>
        /// <remarks>
        /// This method locates the peak top within the specified range and then adjusts the start and end indices to more
        /// tightly encompass the peak, based on changes in intensity that suggest the actual bounds of the peak. The end index in the returned tuple
        /// is exclusive, indicating that the peak at this index is not included in the identified range.
        /// Start indices are inclusive and end indices are exclusive.
        /// </remarks>
        public (int, int, int) ShrinkPeakRange(int start, int end, int averagePeakWidth) {
            var peakTopId = GetPeakTopId(start, end);

            var newStart = start;
            for (int j = peakTopId - averagePeakWidth; j >= start; j--) {
                if (j - 1 < start) {
                    break;
                }
                if (_peaks[j - 1].Intensity >= _peaks[j].Intensity) {
                    newStart = j;
                    break;
                }
            }

            var newEnd = end;
            for (int j = peakTopId + averagePeakWidth; j < end; j++) {
                if (j + 1 >= end) {
                    break;
                }
                if (_peaks[j].Intensity <= _peaks[j + 1].Intensity) {
                    newEnd = j + 1;
                    break;
                }
            }

            return (newStart, peakTopId, newEnd);
        }

        /// <summary>
        /// Determines the index of the peak with the highest intensity within a specified range.
        /// </summary>
        /// <param name="start">The zero-based start index of the rangeto search for the peak top.</param>
        /// <param name="end">The zero-based end index of the range to search for the peak top, exclusive.</param>
        /// <returns>The zero-based index of the peak top with the highest intensity within the specified range.</returns>
        /// <remarks>
        /// This method scans the specified range of peaks and identifies the peak top by locating the index with the highest intensity value.
        /// It's particularly useful for analyzing subsections of a chromatogram to locate significant peaks.
        /// </remarks>
        public int GetPeakTopId(int start, int end) {
            var peakTopIntensity = double.MinValue;
            var peakTopId = start;
            for (int i = start; i < end; i++) {
                if (peakTopIntensity < _peaks[i].Intensity) {
                    peakTopIntensity = _peaks[i].Intensity;
                    peakTopId = i;
                }
            }
            return peakTopId;
        }

        /// <summary>
        /// Retrieves the m/z of the peak at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the peak.</param>
        /// <returns>The m/z value of the specified peak.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        public double Mz(int index) {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            } 
            return _peaks[index].Mz;
        }


        /// <summary>
        /// Retrieves the ID of the peak at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the peak.</param>
        /// <returns>The ID of the specified peak.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        public int Id(int index) {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            } 
            return _peaks[index].Id;
        }

        /// <summary>
        /// Determines whether a specified peak is a valid peak top based on its intensity and position.
        /// </summary>
        /// <param name="topId">The index of the peak to evaluate as the potential peak top.</param>
        /// <returns><c>true</c> if the peak at the specified index is a valid peak top; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// A peak is considered a valid peak top if it is not on the boundary of the chromatogram and both its adjacent points have positive intensities.
        /// </remarks>
        public bool IsValidPeakTop(int topId) {
            return topId - 1 >= 0 && topId + 1 <= _size - 1
                && _peaks[topId - 1].Intensity > 0 && _peaks[topId + 1].Intensity > 0;
        }

        /// <summary>
        /// Counts the number of significant spikes in intensity between the specified indices of the peak array, using a specified threshold.
        /// </summary>
        /// <param name="leftId">The zero-based start index of the range within which to count spikes.</param>
        /// <param name="rightId">The zero-based end index of the range within which to count spikes.</param>
        /// <param name="threshold">The intensity difference threshold above which a spike is considered significant.</param>
        /// <returns>The number of spikes within the specified range that have an intensity difference exceeding the specified threshold.</returns>
        /// <remarks>
        /// This method identifies spikes by comparing the intensity differences between peaks. A spike is considered significant if the difference
        /// in intensity between its peak and the adjacent minimum exceeds the specified threshold.
        /// </remarks>
        public int CountSpikes(int leftId, int rightId, double threshold) {
            var leftBound = Math.Max(leftId, 1);
            var rightBound = Math.Min(rightId, _size - 2);

            var counter = 0;
            double? spikeMax = null, spikeMin = null;
            for (int i = leftBound; i <= rightBound; i++) {
                if (IsPeakTop(i)) {
                    spikeMax = _peaks[i].Intensity;
                }
                else if (IsBottom(i)) {
                    spikeMin = _peaks[i].Intensity;
                }
                if (spikeMax.HasValue && spikeMin.HasValue) {
                    var noise = Math.Abs(spikeMax.Value - spikeMin.Value) / 2;
                    if (noise > threshold) {
                        counter++;
                    }
                    spikeMax = null; spikeMin = null;
                }
            }
            return counter;
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
