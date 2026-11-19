using System.Numerics;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Json
{
    /// <summary>
    /// The number subset of rule files (design 3.1), decided on the lexeme, with no culture and no double.Parse.
    /// </summary>
    /// <remarks>
    /// The Python reference is <c>_pi</c> and <c>_pf</c> of <c>rule_format.py</c>: an integer lexeme of more than
    /// 16 digits, or above 2^53 - 1, is number_range and "-0" is negative_zero; a float lexeme with an exponent
    /// is number_form, more than 15 significant digits is float_precision, a zero written with a minus sign is
    /// negative_zero, and a value outside 0 and [1e-4, 1e15) is number_range. A float is converted as
    /// (double)D / 10^k with D &lt; 10^15 and k &lt;= 18, one correctly rounded IEEE division, so it is
    /// bit-identical to Python's float(). Python's float() also rounds a positive value below 2^-1075 to 0.0,
    /// which its range test then accepts as zero; the reader does the same.
    /// </remarks>
    internal static class DecimalLexeme
    {
        internal const long MaxSafeInteger = 9007199254740991L; // 2^53 - 1

        private static readonly double[] Pow10 = {
            1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11,
            1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22,
        };

        /// <summary>An integer lexeme <c>-?(0|[1-9][0-9]*)</c>.</summary>
        internal static long ParseInteger(string lexeme) {
            if (lexeme == "-0") {
                throw new StrictJsonException(RuleErrorCodes.NegativeZero);
            }
            var negative = lexeme.Length > 0 && lexeme[0] == '-';
            var digits = negative ? lexeme.Substring(1) : lexeme;
            if (digits.Length > 16) {
                throw new StrictJsonException(RuleErrorCodes.NumberRange);
            }
            long v = 0;
            foreach (var ch in digits) {
                v = v * 10 + (ch - '0');
            }
            if (v > MaxSafeInteger) {
                throw new StrictJsonException(RuleErrorCodes.NumberRange);
            }
            return negative ? -v : v;
        }

        /// <summary>A float lexeme as the scanner delimits it: <c>-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][-+]?[0-9]+)?</c>.</summary>
        internal static JsonFloat ParseFloat(string lexeme) {
            if (!IsPlainFloat(lexeme)) {
                throw new StrictJsonException(RuleErrorCodes.NumberForm);
            }
            var negative = lexeme[0] == '-';
            var body = negative ? lexeme.Substring(1) : lexeme;
            var dot = body.IndexOf('.');
            var intPart = body.Substring(0, dot);
            var fracPart = body.Substring(dot + 1);
            var significant = (intPart + fracPart).TrimStart('0').TrimEnd('0');
            if (significant.Length > 15) {
                throw new StrictJsonException(RuleErrorCodes.FloatPrecision);
            }
            var frac = fracPart.TrimEnd('0');
            bool isZero;
            if (intPart != "0") {
                if (intPart.Length > 15) {
                    throw new StrictJsonException(RuleErrorCodes.NumberRange);
                }
                isZero = false;
            }
            else if (frac.Length == 0) {
                isZero = true;
            }
            else {
                var firstNonZero = 0;
                while (frac[firstNonZero] == '0') {
                    firstNonZero++;
                }
                if (firstNonZero < 4) {
                    isZero = false;
                }
                else if (UnderflowsToZero(frac)) {
                    isZero = true;
                }
                else {
                    throw new StrictJsonException(RuleErrorCodes.NumberRange);
                }
            }
            if (isZero) {
                if (negative) {
                    throw new StrictJsonException(RuleErrorCodes.NegativeZero);
                }
                return new JsonFloat(0.0, "0.0", 0, 0);
            }
            long d = 0;
            foreach (var ch in intPart + frac) {
                d = d * 10 + (ch - '0');
            }
            var k = frac.Length;
            var x = (double)d / Pow10[k];
            var canonical = (negative ? "-" : "") + intPart + "." + (k == 0 ? "0" : frac);
            return new JsonFloat(negative ? -x : x, canonical, negative ? -d : d, k);
        }

        /// <summary>Exact pico-daltons of a float (design 3.4): more than 12 fraction digits is mass_precision.</summary>
        internal static BigInteger ToPda(JsonFloat value) {
            if (value.Scale > 12) {
                throw new StrictJsonException(RuleErrorCodes.MassPrecision);
            }
            return value.Mantissa * BigInteger.Pow(10, 12 - value.Scale);
        }

        private static bool IsPlainFloat(string lexeme) {
            // Python's _FLT: -?(?:0|[1-9][0-9]*)\.[0-9]+ to the end. The scanner already guarantees the integer
            // part, so this only refuses an exponent or a missing fraction.
            var dot = lexeme.IndexOf('.');
            if (dot < 0 || dot == lexeme.Length - 1) {
                return false;
            }
            for (var i = dot + 1; i < lexeme.Length; i++) {
                if (lexeme[i] < '0' || lexeme[i] > '9') {
                    return false;
                }
            }
            return true;
        }

        /// <summary>0.frac &lt; 2^-1075 (half the smallest subnormal; an exact tie cannot occur here).</summary>
        private static bool UnderflowsToZero(string frac) {
            // 2^-1075 lies in [1e-324, 1e-323), so only a first non-zero digit at place 324 needs the exact
            // comparison, and 2^-1075 has 1075 decimal places, so the first 1076 places decide it. This keeps a
            // lexeme with very many zeros linear instead of building a BigInteger the size of the lexeme.
            var leadingZeros = 0;
            while (leadingZeros < frac.Length && frac[leadingZeros] == '0') {
                leadingZeros++;
            }
            if (leadingZeros >= 324) {
                return true;
            }
            if (leadingZeros < 323) {
                return false;
            }
            if (frac.Length > 1076) {
                frac = frac.Substring(0, 1076);
            }
            var d = BigInteger.Zero;
            foreach (var ch in frac) {
                d = d * 10 + (ch - '0');
            }
            return d * BigInteger.Pow(2, 1075) < BigInteger.Pow(10, frac.Length);
        }
    }
}
