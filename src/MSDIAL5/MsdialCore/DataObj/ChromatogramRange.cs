using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using System;

namespace CompMs.MsdialCore.DataObj
{
    /// <summary>
    /// Represents a chromatogram range with a specified start and end time, type, and unit.
    /// Provides methods for extending, restricting, and combining chromatogram ranges.
    /// </summary>
    public sealed class ChromatogramRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChromatogramRange"/> class.
        /// </summary>
        /// <param name="begin">The start value of the chromatogram range.</param>
        /// <param name="end">The end value of the chromatogram range.</param>
        /// <param name="type">The chromatographic type of the range.</param>
        /// <param name="unit">The unit of the chromatographic values.</param>
        /// <exception cref="ArgumentException">Thrown when begin is greater than end.</exception>
        public ChromatogramRange(double begin, double end, ChromXType type, ChromXUnit unit) {
            if (begin > end) {
                throw new ArgumentException($"begin argument should be smaller than end argument.");
            }
            Begin = begin;
            End = end;
            Type = type;
            Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromatogramRange"/> class from a chromatogram peak feature.
        /// </summary>
        /// <param name="peak">The chromatogram peak feature from which to create the range.</param>
        /// <param name="type">The chromatographic type of the range.</param>
        /// <param name="unit">The unit of the chromatographic values.</param>
        public ChromatogramRange(IChromatogramPeakFeature peak, ChromXType type, ChromXUnit unit) 
            : this(peak.ChromXsLeft.GetChromByType(type).Value, peak.ChromXsRight.GetChromByType(type).Value, type, unit) {
        }

        /// <summary>
        /// Gets the start value of the chromatogram range.
        /// </summary>
        public double Begin { get; }

        /// <summary>
        /// Gets the end value of the chromatogram range.
        /// </summary>
        public double End { get; }

        /// <summary>
        /// Gets the chromatographic type of the range.
        /// </summary>
        public ChromXType Type { get; }

        /// <summary>
        /// Gets the unit of the chromatographic values.
        /// </summary>
        public ChromXUnit Unit { get; }

        /// <summary>
        /// Gets the width of the chromatogram range.
        /// </summary>
        public double Width => End - Begin;

        /// <summary>
        /// Extends the chromatogram range by a relative rate.
        /// </summary>
        /// <param name="rate">The rate by which to extend the range. Must be greater than -0.5.</param>
        /// <returns>A new <see cref="ChromatogramRange"/> extended by the specified rate.</returns>
        /// <exception cref="ArgumentException">Thrown when rate is less than -0.5.</exception>
        public ChromatogramRange ExtendRelative(double rate) {
            if (rate < -.5d) {
                throw new ArgumentException("rate argument should be larger than `-0.5`.");
            }
            var extendWidth = Width * rate;
            return new ChromatogramRange(Begin - extendWidth, End + extendWidth, Type, Unit);
        }

        /// <summary>
        /// Extends the chromatogram range by a fixed value.
        /// </summary>
        /// <param name="value">The value by which to extend the range. Must be greater than -Width / 2.</param>
        /// <returns>A new <see cref="ChromatogramRange"/> extended by the specified value.</returns>
        /// <exception cref="ArgumentException">Thrown when value is less than -Width / 2.</exception>
        public ChromatogramRange ExtendWith(double value) {
            if (value < - Width / 2d) {
                throw new ArgumentException("value argument should be larger than `- Width / 2`.");
            }
            return new ChromatogramRange(Begin - value, End + value, Type, Unit);
        }

        /// <summary>
        /// Restricts the chromatogram range within specified limits.
        /// </summary>
        /// <param name="limitLow">The lower limit of the range.</param>
        /// <param name="limitHigh">The upper limit of the range.</param>
        /// <returns>A new <see cref="ChromatogramRange"/> restricted within the specified limits.</returns>
        /// <exception cref="ArgumentException">Thrown when limitLow is greater than limitHigh.</exception>
        public ChromatogramRange RestrictBy(double limitLow, double limitHigh) {
            if (limitLow > limitHigh) {
                throw new ArgumentException("limitHigh argument should be larger than limitLow argument.");
            }
            if (limitLow > End) {
                return new ChromatogramRange(End, End, Type, Unit);
            }
            if (limitHigh < Begin) {
                return new ChromatogramRange(Begin, Begin, Type, Unit);
            }
            return new ChromatogramRange(Math.Max(limitLow, Begin), Math.Min(limitHigh, End), Type, Unit);
        }

        /// <summary>
        /// Combines two chromatogram ranges into their union.
        /// </summary>
        /// <param name="other">The other chromatogram range to merge.</param>
        /// <returns>A new <see cref="ChromatogramRange"/> that spans both input ranges.</returns>
        /// <exception cref="ArgumentException">Thrown when Type or Unit do not match.</exception>
        public ChromatogramRange Union(ChromatogramRange other) {
            if (other is null) {
                return this;
            }
            if (other.Type != Type || other.Unit != Unit) {
                throw new ArgumentException("Type and Unit should be same.");
            }
            return new ChromatogramRange(Math.Min(other.Begin, Begin), Math.Max(other.End, End), Type, Unit);
        }

        public static ChromatogramRange FromTimes<T>(T center, double width) where T: IChromX {
            return new ChromatogramRange(center.Value - width / 2d, center.Value + width / 2d, center.Type, center.Unit);
        }

        /// <summary>
        /// Creates a new chromatogram range from two chromatographic time points.
        /// </summary>
        /// <typeparam name="T">A type implementing <see cref="IChromX"/>.</typeparam>
        /// <param name="begin">The start chromatographic time point.</param>
        /// <param name="end">The end chromatographic time point.</param>
        /// <returns>A new <see cref="ChromatogramRange"/> created from the specified time points.</returns>
        public static ChromatogramRange FromTimes<T>(T begin, T end) where T: IChromX {
            return new ChromatogramRange(begin.Value, end.Value, begin.Type, begin.Unit);
        }
    }
}

