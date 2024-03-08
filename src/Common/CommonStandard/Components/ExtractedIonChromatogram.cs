using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Enum;
using CompMs.Common.Mathematics.Basic;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Components
{
    public sealed class ExtractedIonChromatogram : IDisposable {
        private IReadOnlyList<ValuePeak>? _peaks;
        private readonly int _size;
        private readonly ChromXType _type;
        private readonly ChromXUnit _unit;
        private ArrayPool<ValuePeak>? _arrayPool;
        private readonly Algorithm.ChromSmoothing.Smoothing _smoother;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractedIonChromatogram"/> class using a collection of <see cref="ValuePeak"/> objects, chromatogram type, unit, and the extracted m/z.
        /// </summary>
        /// <param name="peaks">An <see cref="IEnumerable{ValuePeak}"/> representing the peaks in the chromatogram.</param>
        /// <param name="type">The type of chromatogram, represented by the <see cref="ChromXType"/> enumeration.</param>
        /// <param name="unit">The unit of measurement for the chromatogram, represented by the <see cref="ChromXUnit"/> enumeration.</param>
        /// <param name="extractedMz">The extracted m/z for which the chromatogram is generated.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="peaks"/> collection is null.</exception>
        /// <remarks>
        /// This constructor is suitable for cases where the peaks are provided in a collection that may not be initially indexed. It ensures that the peaks are stored internally as a read-only list, facilitating efficient access while preserving immutability.
        /// </remarks>
        public ExtractedIonChromatogram(IEnumerable<ValuePeak> peaks, ChromXType type, ChromXUnit unit, double extractedMz) {
            _peaks = peaks as IReadOnlyList<ValuePeak> ?? peaks?.ToArray() ?? throw new ArgumentNullException(nameof(peaks));
            _size = _peaks.Count;
            _type = type;
            _unit = unit;
            _smoother = new Algorithm.ChromSmoothing.Smoothing();
            ExtractedMz = extractedMz;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractedIonChromatogram"/> class using an array of <see cref="ValuePeak"/> objects rented from an <see cref="ArrayPool{ValuePeak}"/>, size of the peak array, chromatogram type, unit, the extracted mass-to-charge ratio (m/z), and a specific <see cref="ArrayPool{ValuePeak}"/>.
        /// </summary>
        /// <param name="peaks">An array of <see cref="ValuePeak"/> representing the peaks in the chromatogram. This array should be rented from an <see cref="ArrayPool{ValuePeak}"/> to optimize memory usage.</param>
        /// <param name="size">The size of the peak array, indicating the number of peaks included.</param>
        /// <param name="type">The type of chromatogram, represented by the <see cref="ChromXType"/> enumeration.</param>
        /// <param name="unit">The unit of measurement for the chromatogram, represented by the <see cref="ChromXUnit"/> enumeration.</param>
        /// <param name="extractedMz">The extracted mass-to-charge ratio (m/z) for which the chromatogram is generated.</param>
        /// <param name="arrayPool">The <see cref="ArrayPool{ValuePeak}"/> from which the peaks array was rented. It is used for recycling the peak array after use, optimizing memory usage.</param>
        /// <remarks>
        /// This constructor is optimized for scenarios where memory efficiency is crucial, utilizing an <see cref="ArrayPool{ValuePeak}"/> for the peak array. This approach is particularly useful in high-throughput environments or when processing large datasets. Ensure to call <see cref="Dispose"/> when done using the instance to release the array back to the specified <see cref="ArrayPool{ValuePeak}"/>, preventing memory leaks and ensuring the array can be reused. It is essential that the provided `peaks` array is rented from an array pool to align with the disposal pattern and memory management practices.
        /// </remarks>
        public ExtractedIonChromatogram(ValuePeak[] peaks, int size, ChromXType type, ChromXUnit unit, double extractedMz, ArrayPool<ValuePeak> arrayPool) {
            _peaks = peaks;
            _size = size;
            _type = type;
            _unit = unit;
            _arrayPool = arrayPool;
            _smoother = new Algorithm.ChromSmoothing.Smoothing();
            ExtractedMz = extractedMz;
        }

        public double ExtractedMz { get; }

        public bool IsEmpty => _size == 0;
        public int Length => _size;

        /// <summary>
        /// Creates and returns a copy of the peak array.
        /// </summary>
        /// <returns>An array of <see cref="ValuePeak"/> representing the chromatographic peaks.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the <see cref="ExtractedIonChromatogram"/> has been disposed and the peak data is no longer accessible.</exception>
        /// <remarks>
        /// This method ensures the integrity of the chromatographic data by returning a copy of the internal peak array, 
        /// allowing safe read-only access to the peak data.
        /// </remarks>
        public ValuePeak[] AsPeakArray() {
            if (_peaks is null) {
                throw new ObjectDisposedException(nameof(_peaks));
            } 
            var dest = new ValuePeak[_size];
            if (_peaks is ValuePeak[] array) {
                Array.Copy(array, dest, _size);
                return dest;
            }
            for (int i = 0; i < dest.Length; i++) {
                dest[i] = _peaks[i];
            }
            return dest;
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
        /// Creates a <see cref="ChromXs"/> object representing a chromatographic position with an associated m/z.
        /// </summary>
        /// <param name="chromValue">The chromatographic value (e.g., retention time).</param>
        /// <param name="mz">The m/z to associate with the chromatographic position.</param>
        /// <returns>A <see cref="ChromXs"/> object encapsulating the specified chromatographic position and m/z value.</returns>
        /// <remarks>
        /// This method facilitates the creation of a <see cref="ChromXs"/> object for cases where the chromatographic type
        /// does not inherently involve m/z values, allowing for the explicit association of an m/z value with a given chromatographic measurement.
        /// </remarks>
        public ChromXs PeakChromXs(double chromValue, double mz) {
            var result = new ChromXs(chromValue, _type, _unit);
            if (_type != ChromXType.Mz) {
                result.Mz = new MzValue(mz);
            }
            return result;
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
        /// Extracts a subset of peaks from the chromatogram, creating a new array of peaks between the specified indices, inclusive of the start index and the end index.
        /// </summary>
        /// <param name="left">The zero-based start index for the subset of peaks to extract.</param>
        /// <param name="right">The zero-based end index for the subset of peaks to extract.</param>
        /// <returns>An array of <see cref="ValuePeak"/> containing the extracted subset of peaks.</returns>
        /// <remarks>
        /// This method is useful for isolating a specific section of the chromatogram for detailed analysis or for operations that require a focus on a particular segment of the data. It effectively allows for the zooming into a region of interest by trimming the peak data to just the relevant part.
        /// Note that the method assumes the indices are within the bounds of the peak array and does not perform bounds checking; it is the caller's responsibility to ensure the indices are valid.
        /// </remarks>
        public ValuePeak[] TrimPeaks(int left, int right) {
            var result = new ValuePeak[right - left + 1];
            if (_peaks is ValuePeak[] array) {
                Array.Copy(array, left, result, 0, result.Length);
                return result;
            }
            for (int i = 0; i < result.Length; i++) {
                result[i] = _peaks[i + left];
            }
            return result;
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
        /// <param name="binSize">The size of the bin to group peaks for noise calculation. Larger bins provide a broader analysis at the risk of smoothing over smaller noise variations.</param>
        /// <param name="minWindowSize">The minimum window size required to consider the analysis valid. If the number of bins calculated is less than this, the method returns a default or specified minimum noise level.</param>
        /// <param name="minNoiseLevel">The minimum noise level to return if the calculated noise level is below this value or if there are not enough bins to make a valid estimation.</param>
        /// <returns>The estimated minimum noise level as a <see cref="double"/>.</returns>
        /// <remarks>
        /// This method provides a basic mechanism for estimating the background noise level in chromatographic data by analyzing the variation in peak intensities across different sections of the chromatogram. It's useful for setting baseline intensity thresholds, distinguishing between true signal and noise, and improving the accuracy of peak detection algorithms.
        /// </remarks>
        public double GetMinimumNoiseLevel(int binSize, int minWindowSize, double minNoiseLevel) {
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
        public ExtractedIonChromatogram Difference(ExtractedIonChromatogram other) {
            System.Diagnostics.Debug.Assert(_type == other._type);
            System.Diagnostics.Debug.Assert(_unit == other._unit);
            System.Diagnostics.Debug.Assert(_size == other._size);
            var arrayPool = _arrayPool ?? ArrayPool<ValuePeak>.Shared;
            var peaks = arrayPool.Rent(_size);
            for (int i = 0; i < _size; i++) {
                peaks[i] = new ValuePeak(_peaks[i].Id, _peaks[i].Time, _peaks[i].Mz, Math.Max(0, _peaks[i].Intensity - other._peaks[i].Intensity));
            }
            return new ExtractedIonChromatogram(peaks, _size, _type, _unit, ExtractedMz, arrayPool);
        }

        /// <summary>
        /// Applies a specified smoothing algorithm to the chromatogram, aiming to reduce noise and enhance peak detection.
        /// </summary>
        /// <param name="method">The smoothing method to apply, defined by the <see cref="SmoothingMethod"/> enumeration.</param>
        /// <param name="level">The level of smoothing to apply, which may affect the degree of noise reduction and peak preservation based on the chosen method.</param>
        /// <returns>A new <see cref="ExtractedIonChromatogram"/> instance representing the smoothed chromatogram.</returns>
        /// <remarks>
        /// This method facilitates various smoothing techniques, each with their own strengths in different analytical contexts. For instance, simple moving averages are effective for general noise reduction, while more sophisticated methods like Savitzky-Golay filtering can preserve peak shape and height better. The choice of method and level should be tailored to the specific needs of the chromatographic analysis.
        /// </remarks>
        public ExtractedIonChromatogram ChromatogramSmoothing(SmoothingMethod method, int level) {
            ValuePeak[] peaks = null;
            if (method == SmoothingMethod.LinearWeightedMovingAverage) {
                peaks = _peaks as ValuePeak[];
            }
            if (peaks is null) {
                peaks = _peaks.Take(_size).ToArray();
            }

            switch (method) {
                case SmoothingMethod.SimpleMovingAverage:
                    return new ExtractedIonChromatogram(Algorithm.ChromSmoothing.Smoothing.SimpleMovingAverage(peaks, level), _type, _unit, ExtractedMz);
                case SmoothingMethod.SavitzkyGolayFilter:
                    return new ExtractedIonChromatogram(Algorithm.ChromSmoothing.Smoothing.SavitxkyGolayFilter(peaks, level), _type, _unit, ExtractedMz);
                case SmoothingMethod.BinomialFilter:
                    return new ExtractedIonChromatogram(Algorithm.ChromSmoothing.Smoothing.BinomialFilter(peaks, level), _type, _unit, ExtractedMz);
                case SmoothingMethod.LowessFilter:
                    return new ExtractedIonChromatogram(Algorithm.ChromSmoothing.Smoothing.LowessFilter(peaks, level), _type, _unit, ExtractedMz);
                case SmoothingMethod.LoessFilter:
                    return new ExtractedIonChromatogram(Algorithm.ChromSmoothing.Smoothing.LoessFilter(peaks, level), _type, _unit, ExtractedMz);
                case SmoothingMethod.LinearWeightedMovingAverage:
                default:
                    var arrayPool = _arrayPool ?? ArrayPool<ValuePeak>.Shared;
                    var smoothed = arrayPool.Rent(_size);
                    _smoother.LinearWeightedMovingAverage(peaks, smoothed, _size, level);
                    return new ExtractedIonChromatogram(smoothed, _size, _type, _unit, ExtractedMz, arrayPool);
            }
        }

        /// <summary>
        /// Analyzes a specified range within the chromatogram to detect peaks and evaluate their characteristics.
        /// </summary>
        /// <param name="startID">The start index of the range to analyze for peak detection.</param>
        /// <param name="endID">The end index of the range to analyze for peak detection.</param>
        /// <returns>A <see cref="PeakDetectionResult"/> object containing details about detected peaks and their properties within the specified range.</returns>
        /// <remarks>
        /// This method is designed to focus peak detection efforts on a specific segment of the chromatogram, allowing for targeted analysis of areas of interest.
        /// </remarks>
        public PeakDetectionResult GetPeakDetectionResultFromRange(int startID, int endID) {
            var datapoints = new List<double[]>();
            var datapointsPeakTopIndex = 0;
            var peaktopIntensity = double.MinValue;
            for (int i = 0; i < _size; i++) {
                var peak = _peaks[i];
                if (peak.Id >= startID && peak.Id <= endID) {
                    datapoints.Add(new double[] { i, peak.Time, peak.Mz, peak.Intensity });
                    if (peak.Intensity > peaktopIntensity) {
                        peaktopIntensity = peak.Intensity;
                        datapointsPeakTopIndex = datapoints.Count - 1;
                    }
                }
            }
            return PeakDetection.GetPeakDetectionResult(datapoints, datapointsPeakTopIndex);
        }

        public ChromatogramGlobalProperty_temp2 GetProperty(int noiseEstimateBin, int minNoiseWindowSize, double minNoiseLevel, double noiseFactor) {
            using var sChromatogram = ChromatogramSmoothing(SmoothingMethod.LinearWeightedMovingAverage, 1);
            var ssChromatogram = sChromatogram.ChromatogramSmoothing(SmoothingMethod.LinearWeightedMovingAverage, 1);
            var baselineChromatogram = ChromatogramSmoothing(SmoothingMethod.LinearWeightedMovingAverage, 20);
            var baselineCorrectedChromatogram = ssChromatogram.Difference(baselineChromatogram);
            var noise = baselineCorrectedChromatogram.GetMinimumNoiseLevel(noiseEstimateBin, minNoiseWindowSize, minNoiseLevel) * noiseFactor;

            // checking chromatogram properties
            var baselineMedian = GetIntensityMedian();
            var maxChromIntensity = GetMaximumIntensity();
            var minChromIntensity = GetMinimumIntensity();
            var isHighBaseline = baselineMedian > (maxChromIntensity + minChromIntensity) * 0.5;
            return new ChromatogramGlobalProperty_temp2(maxChromIntensity, minChromIntensity, baselineMedian, noise, isHighBaseline, ssChromatogram, baselineChromatogram, baselineCorrectedChromatogram);
        }

        internal ChroChroChromatogram GetChroChroChromatogram(int noiseEstimateBin, int minNoiseWindowSize, double minNoiseLevel, double noiseFactor) {
            // 'chromatogram' properties
            var globalProperty = GetProperty(noiseEstimateBin, minNoiseWindowSize, minNoiseLevel, noiseFactor);

            // differential factors
            var differencialCoefficients = globalProperty.GenerateDifferencialCoefficients();

            // slope noises
            var noises = globalProperty.CalculateSlopeNoises(differencialCoefficients);

            return new ChroChroChromatogram(this, globalProperty, differencialCoefficients, noises);
        }

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
