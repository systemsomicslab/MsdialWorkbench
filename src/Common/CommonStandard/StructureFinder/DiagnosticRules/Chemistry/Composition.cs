using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Chemistry
{
    /// <summary>
    /// Formulas of rule files (design 3.5): strict Hill order (C, then H, then the rest alphabetically; with no C
    /// everything alphabetical), each element once with a count of 1 to 999, the count 1 omitted, no leading
    /// zeros. A parsed formula is a count vector over an <see cref="ElementTable"/>.
    /// </summary>
    public static class Composition
    {
        private const RegexOptions Options = RegexOptions.CultureInvariant;

        // Python: (?:[A-Z][a-z]?(?:[2-9]|[1-9][0-9]{1,2})?)+\Z
        private static readonly Regex FormulaPattern = new Regex(@"^(?:[A-Z][a-z]?(?:[2-9]|[1-9][0-9]{1,2})?)+\z", Options);
        private static readonly Regex TokenPattern = new Regex(@"([A-Z][a-z]?)([0-9]*)", Options);
        private static readonly Regex LoosePartPattern = new Regex(@"^(?:[A-Z][a-z]?(?:[1-9][0-9]*)?)+\z", Options);

        /// <summary>
        /// Canonical adduct names, as <c>_ADDUCT_RE</c>: [kM(+|-)(n)Part...]z(+|-), for example [M+H]+, [M-H]-,
        /// [2M+Na]+, [M+H-2H2O]+.
        /// </summary>
        internal static readonly Regex AdductPattern = new Regex(
            @"^\[([1-9][0-9]*)?M((?:[+-](?:[1-9][0-9]*)?[A-Z][A-Za-z0-9]*)*)\]([1-9][0-9]*)?([+-])\z", Options);

        private static readonly Regex AdductPartPattern = new Regex(@"([+-])([1-9][0-9]*)?([A-Z][A-Za-z0-9]*)", Options);

        /// <summary>Parses a strict Hill formula; throws <see cref="StrictJsonException"/> with bad_formula,
        /// unknown_element or formula_not_hill, checked in that order.</summary>
        public static long[] Parse(string text, ElementTable elements) {
            if (text is null || !FormulaPattern.IsMatch(text)) {
                throw new StrictJsonException(RuleErrorCodes.BadFormula);
            }
            var symbols = new List<string>();
            var counts = new List<long>();
            foreach (Match m in TokenPattern.Matches(text)) {
                symbols.Add(m.Groups[1].Value);
                counts.Add(m.Groups[2].Length == 0 ? 1 : ParseDigits(m.Groups[2].Value));
            }
            foreach (var s in symbols) {
                if (!elements.Contains(s)) {
                    throw new StrictJsonException(RuleErrorCodes.UnknownElement);
                }
            }
            var distinct = new HashSet<string>(symbols, StringComparer.Ordinal);
            if (distinct.Count != symbols.Count || !SequenceEqual(symbols, HillOrder(symbols))) {
                throw new StrictJsonException(RuleErrorCodes.FormulaNotHill);
            }
            var vector = new long[elements.Count];
            for (var i = 0; i < symbols.Count; i++) {
                elements.TryGetIndex(symbols[i], out var index);
                vector[index] = counts[i];
            }
            return vector;
        }

        /// <summary>The element symbols of a formula text, as Python's <c>_TOKEN_RE.findall</c> yields them.</summary>
        internal static IEnumerable<string> Symbols(string text) {
            foreach (Match m in TokenPattern.Matches(text)) {
                yield return m.Groups[1].Value;
            }
        }

        /// <summary>Hill order of a set of symbols: C first, then H, then the rest in code-point order; with no C
        /// everything in code-point order.</summary>
        public static List<string> HillOrder(IEnumerable<string> symbols) {
            var set = new HashSet<string>(symbols, StringComparer.Ordinal);
            var rest = new List<string>();
            foreach (var s in set) {
                if (!set.Contains("C") || (s != "C" && s != "H")) {
                    rest.Add(s);
                }
            }
            rest.Sort(CanonicalJson.CodePointOrder);
            if (!set.Contains("C")) {
                return rest;
            }
            var ordered = new List<string> { "C" };
            if (set.Contains("H")) {
                ordered.Add("H");
            }
            ordered.AddRange(rest);
            return ordered;
        }

        /// <summary>Hill text of a non-negative composition (zero counts omitted, count 1 not written).</summary>
        public static string Format(long[] counts, ElementTable elements) {
            var present = new List<string>();
            for (var i = 0; i < counts.Length; i++) {
                if (counts[i] < 0) {
                    throw new ArgumentException("negative count", nameof(counts));
                }
                if (counts[i] != 0) {
                    present.Add(elements.Symbols[i]);
                }
            }
            var sb = new StringBuilder();
            foreach (var s in HillOrder(present)) {
                elements.TryGetIndex(s, out var index);
                sb.Append(s);
                if (counts[index] != 1) {
                    sb.Append(counts[index].ToString(CultureInfo.InvariantCulture));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// The net composition a canonical adduct name states (Python <c>adduct_name_net</c>): multiplier, charge
        /// count, sign and net element counts with zeros dropped; null when the name is not formula-based (a part
        /// outside the loose formula grammar or an element missing from the table).
        /// </summary>
        internal static AdductNameParts? ParseAdductName(string name, ElementTable elements) {
            var m = AdductPattern.Match(name);
            if (!m.Success) {
                return null;
            }
            var net = new Dictionary<string, BigInteger>(StringComparer.Ordinal);
            foreach (Match part in AdductPartPattern.Matches(m.Groups[2].Value)) {
                var sign = part.Groups[1].Value == "+" ? 1 : -1;
                var count = part.Groups[2].Length == 0 ? BigInteger.One : ParseBigDigits(part.Groups[2].Value);
                var formula = part.Groups[3].Value;
                if (!LoosePartPattern.IsMatch(formula)) {
                    return null;
                }
                foreach (Match token in TokenPattern.Matches(formula)) {
                    var symbol = token.Groups[1].Value;
                    if (!elements.Contains(symbol)) {
                        return null;
                    }
                    var n = token.Groups[2].Length == 0 ? BigInteger.One : ParseBigDigits(token.Groups[2].Value);
                    net.TryGetValue(symbol, out var current);
                    net[symbol] = current + sign * count * n;
                }
            }
            var nonZero = new Dictionary<string, BigInteger>(StringComparer.Ordinal);
            foreach (var kv in net) {
                if (!kv.Value.IsZero) {
                    nonZero.Add(kv.Key, kv.Value);
                }
            }
            var multiplier = m.Groups[1].Length == 0 ? BigInteger.One : ParseBigDigits(m.Groups[1].Value);
            var chargeCount = m.Groups[3].Length == 0 ? BigInteger.One : ParseBigDigits(m.Groups[3].Value);
            return new AdductNameParts(multiplier, chargeCount, m.Groups[4].Value == "+" ? 1 : -1, nonZero);
        }

        internal static long ParseDigits(string digits) {
            long v = 0;
            foreach (var ch in digits) {
                v = checked(v * 10 + (ch - '0'));
            }
            return v;
        }

        internal static BigInteger ParseBigDigits(string digits) {
            var v = BigInteger.Zero;
            foreach (var ch in digits) {
                v = v * 10 + (ch - '0');
            }
            return v;
        }

        private static bool SequenceEqual(List<string> a, List<string> b) {
            if (a.Count != b.Count) {
                return false;
            }
            for (var i = 0; i < a.Count; i++) {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) {
                    return false;
                }
            }
            return true;
        }
    }

    internal sealed class AdductNameParts
    {
        public AdductNameParts(BigInteger multiplier, BigInteger chargeCount, int sign, Dictionary<string, BigInteger> net) {
            Multiplier = multiplier;
            ChargeCount = chargeCount;
            Sign = sign;
            Net = net;
        }

        public BigInteger Multiplier { get; }
        public BigInteger ChargeCount { get; }
        public int Sign { get; }
        public Dictionary<string, BigInteger> Net { get; }
    }
}
