using System;

namespace CompMs.Common.DataObj
{
    /// <summary>
    /// Represents a range of m/z with a specified central value and tolerance.
    /// This class is used to define a specific m/z range for filtering or identifying ions in mass spectrometry data.
    /// </summary>
    public sealed class MzRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MzRange"/> class with a specified m/z value and tolerance.
        /// </summary>
        /// <param name="mz">The central m/z value of the range.</param>
        /// <param name="tolerance">The tolerance around the central m/z value, defining the width of the range.</param>
        public MzRange(double mz, double tolerance)
        {
            Mz = mz;
            Tolerance = tolerance;
        }

        /// <summary>
        /// Gets the central m/z value of the range.
        /// </summary>
        public double Mz { get; }

        /// <summary>
        /// Gets the tolerance around the central m/z value, defining the width of the range.
        /// </summary>
        public double Tolerance { get; }

        /// <summary>
        /// Gets the left boundary of the m/z range.
        /// Equivalent to <c>Mz - Tolerance</c>.
        /// </summary>
        public double Left => Mz - Tolerance;

        /// <summary>
        /// Gets the right boundary of the m/z range.
        /// Equivalent to <c>Mz + Tolerance</c>.
        /// </summary>
        public double Right => Mz + Tolerance;

        /// <summary>
        /// Determines whether the given m/z value falls within the range represented by this <see cref="MzRange"/>.
        /// </summary>
        /// <param name="mz">The m/z value to check.</param>
        /// <returns><c>true</c> if the m/z value is within the range; otherwise, <c>false</c>.</returns>
        public bool Includes(double mz) {
            return Mz - Tolerance <= mz && mz <= Mz + Tolerance;
        }

        /// <summary>
        /// Creates a new <see cref="MzRange"/> instance based on a specified left and right boundary.
        /// </summary>
        /// <param name="left">The left boundary of the m/z range.</param>
        /// <param name="right">The right boundary of the m/z range.</param>
        /// <returns>A new <see cref="MzRange"/> instance representing the specified range.</returns>
        public static MzRange FromRange(double left, double right) {
            return new MzRange((left + right) / 2, Math.Abs(right - left) / 2);
        }

        /// <summary>
        /// Creates a new <see cref="MzRange"/> instance from the given range tuple.
        /// </summary>
        /// <param name="range">A tuple containing the left and right boundaries of the m/z range.</param>
        /// <returns>A new <see cref="MzRange"/> instance representing the specified range.</returns>
        public static MzRange FromRange((double left, double right) range) {
            return FromRange(range.left, range.right);
        }
    }
}
