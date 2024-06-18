using CompMs.Common.Algorithm.ChromSmoothing;
using CompMs.Common.Enum;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace CompMs.Common.Components
{
    /// <summary>
    /// Represents a specific experiment chromatogram.
    /// </summary>
    public sealed class SpecificExperimentChromatogram : Chromatogram
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificExperimentChromatogram"/> class.
        /// </summary>
        /// <param name="peaks">The collection of value peaks.</param>
        /// <param name="chromatogramType">The type of chromatogram (e.g., retention time, drift time).</param>
        /// <param name="chromatogramUnit">The unit of the chromatogram (e.g., seconds, milliseconds).</param>
        /// <param name="experimentID">The experiment ID.</param>
        public SpecificExperimentChromatogram(IEnumerable<ValuePeak> peaks, ChromXType chromatogramType, ChromXUnit chromatogramUnit, int experimentID) : base(peaks, chromatogramType, chromatogramUnit) {
            ExperimentID = experimentID;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificExperimentChromatogram"/> class.
        /// </summary>
        /// <param name="peaks">The collection of value peaks.</param>
        /// <param name="size">The size of the peak array, indicating the number of peaks included.</param>
        /// <param name="chromatogramType">The type of chromatogram (e.g., retention time, drift time).</param>
        /// <param name="chromatogramUnit">The unit of the chromatogram (e.g., seconds, milliseconds).</param>
        /// <param name="experimentID">The experiment ID.</param>
        /// <param name="arrayPool">The <see cref="ArrayPool{ValuePeak}"/> from which the peaks array was rented. It is used for recycling the peak array after use, optimizing memory usage.</param>
        /// <remarks>
        /// This constructor is optimized for scenarios where memory efficiency is crucial, utilizing an <see cref="ArrayPool{ValuePeak}"/> for the peak array. This approach is particularly useful in high-throughput environments or when processing large datasets. Ensure to call <see cref="Dispose"/> when done using the instance to release the array back to the specified <see cref="ArrayPool{ValuePeak}"/>, preventing memory leaks and ensuring the array can be reused. It is essential that the provided `peaks` array is rented from an array pool to align with the disposal pattern and memory management practices.
        /// </remarks>
        public SpecificExperimentChromatogram(ValuePeak[] peaks, int size, ChromXType chromatogramType, ChromXUnit chromatogramUnit, int experimentID, ArrayPool<ValuePeak> arrayPool) : base(peaks, size, chromatogramType, chromatogramUnit, arrayPool) {
            ExperimentID = experimentID;
        }

        /// <summary>
        /// Gets the experiment ID.
        /// </summary>
        public int ExperimentID { get; }

        public new SpecificExperimentChromatogram ChromatogramSmoothing(SmoothingMethod method, int level) {
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
                    return new SpecificExperimentChromatogram(Smoothing.SimpleMovingAverage(peaks, level), _type, _unit, ExperimentID);
                case SmoothingMethod.SavitzkyGolayFilter:
                    return new SpecificExperimentChromatogram(Smoothing.SavitxkyGolayFilter(peaks, level), _type, _unit, ExperimentID);
                case SmoothingMethod.BinomialFilter:
                    return new SpecificExperimentChromatogram(Smoothing.BinomialFilter(peaks, level), _type, _unit, ExperimentID);
                case SmoothingMethod.LowessFilter:
                    return new SpecificExperimentChromatogram(Smoothing.LowessFilter(peaks, level), _type, _unit, ExperimentID);
                case SmoothingMethod.LoessFilter:
                    return new SpecificExperimentChromatogram(Smoothing.LoessFilter(peaks, level), _type, _unit, ExperimentID);
                case SmoothingMethod.LinearWeightedMovingAverage:
                default:
                    var arrayPool = _arrayPool ?? ArrayPool<ValuePeak>.Shared;
                    var smoothed = arrayPool.Rent(_size);
                    new Smoothing().LinearWeightedMovingAverage(peaks, smoothed, _size, level);
                    return new SpecificExperimentChromatogram(smoothed, _size, _type, _unit, ExperimentID, arrayPool);
            }
        }

        protected override Chromatogram ChromatogramSmoothingCore(SmoothingMethod method, int level) {
            return ChromatogramSmoothing(method, level);
        }
    }
}
