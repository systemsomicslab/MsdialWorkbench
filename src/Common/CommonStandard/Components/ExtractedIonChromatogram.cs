using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Enum;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace CompMs.Common.Components
{
    public sealed class ExtractedIonChromatogram : Chromatogram {
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
        public ExtractedIonChromatogram(IEnumerable<ValuePeak> peaks, ChromXType type, ChromXUnit unit, double extractedMz) : base(peaks, type, unit) {
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
        public ExtractedIonChromatogram(ValuePeak[] peaks, int size, ChromXType type, ChromXUnit unit, double extractedMz, ArrayPool<ValuePeak> arrayPool) : base(peaks, size, type, unit, arrayPool) {
            _smoother = new Algorithm.ChromSmoothing.Smoothing();
            ExtractedMz = extractedMz;
        }

        public double ExtractedMz { get; }

        /// <summary>
        /// Creates and returns a copy of the peak array.
        /// </summary>
        /// <returns>An array of <see cref="ValuePeak"/> representing the chromatographic peaks.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the <see cref="ExtractedIonChromatogram"/> has been disposed and the peak data is no longer accessible.</exception>
        /// <remarks>
        /// This method ensures the integrity of the chromatographic data by returning a copy of the internal peak array, 
        /// allowing safe read-only access to the peak data.
        /// </remarks>
        public new ValuePeak[] AsPeakArray() {
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
        /// <exception cref="ObjectDisposedException">Thrown if the chromatogram has been disposed.</exception>
        /// <remarks>
        /// This method facilitates various smoothing techniques, each with their own strengths in different analytical contexts. For instance, simple moving averages are effective for general noise reduction, while more sophisticated methods like Savitzky-Golay filtering can preserve peak shape and height better. The choice of method and level should be tailored to the specific needs of the chromatographic analysis.
        /// </remarks>
        public new ExtractedIonChromatogram ChromatogramSmoothing(SmoothingMethod method, int level) {
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
                    return new ExtractedIonChromatogram(Algorithm.ChromSmoothing.Smoothing.SimpleMovingAverage(peaks, level), _type, _unit, ExtractedMz);
                case SmoothingMethod.SavitzkyGolayFilter:
                    return new ExtractedIonChromatogram(Algorithm.ChromSmoothing.Smoothing.SavitxkyGolayFilter(peaks, level), _type, _unit, ExtractedMz);
                case SmoothingMethod.BinomialFilter:
                    return new ExtractedIonChromatogram(Algorithm.ChromSmoothing.Smoothing.BinomialFilter(peaks, level), _type, _unit, ExtractedMz);
                case SmoothingMethod.LowessFilter:
                    return new ExtractedIonChromatogram(Algorithm.ChromSmoothing.Smoothing.LowessFilter(peaks, level), _type, _unit, ExtractedMz);
                case SmoothingMethod.LoessFilter:
                    return new ExtractedIonChromatogram(Algorithm.ChromSmoothing.Smoothing.LoessFilter(peaks, level), _type, _unit, ExtractedMz);
                case SmoothingMethod.TimeBasedLinearWeightedMovingAverage: {
                        var arrayPool = _arrayPool ?? ArrayPool<ValuePeak>.Shared;
                        var smoothed = arrayPool.Rent(_size);
                        _smoother.TimeBasedLinearWeightedMovingAverage(peaks, smoothed, _size, level);
                        return new ExtractedIonChromatogram(smoothed, _size, _type, _unit, ExtractedMz, arrayPool);
                    }
                case SmoothingMethod.LinearWeightedMovingAverage:
                default: {
                        var arrayPool = _arrayPool ?? ArrayPool<ValuePeak>.Shared;
                        var smoothed = arrayPool.Rent(_size);
                        _smoother.LinearWeightedMovingAverage(peaks, smoothed, _size, level);
                        return new ExtractedIonChromatogram(smoothed, _size, _type, _unit, ExtractedMz, arrayPool);
                    }
            }
        }

        protected override Chromatogram ChromatogramSmoothingCore(SmoothingMethod method, int level) {
            return ChromatogramSmoothing(method, level);
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
        public PeakDetectionResult? GetPeakDetectionResultFromRange(int startID, int endID) {
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
            var result = PeakDetection.GetPeakDetectionResult(datapoints, datapointsPeakTopIndex);
            if (result is null) {
                return null;
            }
            using var sChromatogram = ChromatogramSmoothing(SmoothingMethod.LinearWeightedMovingAverage, 1);
            using var ssChromatogram = sChromatogram.ChromatogramSmoothing(SmoothingMethod.LinearWeightedMovingAverage, 1);
            using var baselineChromatogram = ChromatogramSmoothing(SmoothingMethod.LinearWeightedMovingAverage, 20);
            using var baselineCorrectedChromatogram = ssChromatogram.Difference(baselineChromatogram);
            var parameter = NoiseEstimateParameter.GlobalParameter;
            var noise = baselineCorrectedChromatogram.GetMinimumNoiseLevel(parameter);
            result.EstimatedNoise = Math.Max(1f, (float)noise);
            result.SignalToNoise = (float)(result.IntensityAtPeakTop / result.EstimatedNoise);
            return result;
        }
    }
}
