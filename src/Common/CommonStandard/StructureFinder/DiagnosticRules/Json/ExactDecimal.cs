using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Json
{
    /// <summary>
    /// Fixed-point text of a double (design 4.11): the exact binary value rounded half-even to a number of
    /// decimals, through <see cref="BigInteger"/>, so the text never depends on the culture or on the .NET
    /// Framework versus .NET Core double formatting.
    /// </summary>
    /// <remarks>
    /// This is Python's <c>f"{x:.{d}f}"</c>, with one change: text whose digits are all zero is written without a
    /// sign (-0.0, and -1e-9 at 6 decimals, both give "0.000000"). A non-finite value has no text:
    /// <see cref="Format"/> throws and <see cref="FormatOrNull"/> returns null, which result JSON writes as null.
    /// </remarks>
    public static class ExactDecimal
    {
        public static string Format(double value, int decimals) {
            if (double.IsNaN(value) || double.IsInfinity(value)) {
                throw new ArgumentException("ExactDecimal.Format needs a finite value", nameof(value));
            }
            if (decimals < 0) {
                throw new ArgumentOutOfRangeException(nameof(decimals));
            }
            var bits = BitConverter.DoubleToInt64Bits(value);
            var negative = bits < 0;
            var exponentField = (int)((bits >> 52) & 0x7FF);
            var fraction = bits & 0xFFFFFFFFFFFFFL;
            BigInteger mantissa;
            int exponent;
            if (exponentField == 0) {
                mantissa = fraction;
                exponent = -1074;
            }
            else {
                mantissa = fraction | (1L << 52);
                exponent = exponentField - 1075;
            }
            // |value| * 10^decimals = mantissa * 2^exponent * 10^decimals, rounded half-even to an integer.
            var scaled = mantissa * BigInteger.Pow(10, decimals);
            BigInteger q;
            if (exponent >= 0) {
                q = scaled << exponent;
            }
            else {
                var denominator = BigInteger.One << -exponent;
                q = BigInteger.DivRem(scaled, denominator, out var remainder);
                var twice = remainder * 2;
                if (twice > denominator || (twice == denominator && !q.IsEven)) {
                    q += 1;
                }
            }
            var digits = q.ToString(CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            if (negative && !q.IsZero) {
                sb.Append('-');
            }
            if (decimals == 0) {
                sb.Append(digits);
                return sb.ToString();
            }
            if (digits.Length <= decimals) {
                digits = new string('0', decimals + 1 - digits.Length) + digits;
            }
            sb.Append(digits, 0, digits.Length - decimals);
            sb.Append('.');
            sb.Append(digits, digits.Length - decimals, decimals);
            return sb.ToString();
        }

        /// <summary>The text of a finite value, or null for NaN and the infinities.</summary>
        public static string? FormatOrNull(double value, int decimals) =>
            double.IsNaN(value) || double.IsInfinity(value) ? null : Format(value, decimals);
    }

    /// <summary>
    /// The raw IEEE-754 pattern of a double, big-endian, as 16 lowercase hex digits, with every NaN written as
    /// 7ff8000000000000 (design 4.11; .NET's double.NaN is fff8000000000000, Python's nan 7ff8000000000000).
    /// -0.0 keeps its own pattern.
    /// </summary>
    public static class IeeeBits
    {
        public const string CanonicalNaN = "7ff8000000000000";

        public static string Of(double value) {
            if (double.IsNaN(value)) {
                return CanonicalNaN;
            }
            return ((ulong)BitConverter.DoubleToInt64Bits(value)).ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
