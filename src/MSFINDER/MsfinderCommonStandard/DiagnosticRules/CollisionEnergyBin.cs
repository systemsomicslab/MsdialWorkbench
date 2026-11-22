using System;
using System.Text.RegularExpressions;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// The report-only stratum <c>collision_energy_bin</c> of the diagrules benchmark (design section 8 PR 5): an ASCII
    /// port of the P2 <c>energy_bin</c> function. It is never passed to the evaluator.
    /// </summary>
    /// <remarks>
    /// The original strips and lowercases the value, then fully matches
    /// <c>(?:nce\s*[=:]?\s*)?(\d+(?:\.\d+)?)\s*(ev|v|%|nce)?</c>. The unit is <c>NCE</c> when the value contains
    /// <c>%</c> or <c>nce</c>, <c>eV</c> for <c>ev</c> or <c>v</c>, else <c>unspecified</c>; the number is binned as
    /// <c>&lt;20</c>, <c>20-&lt;40</c> or <c>&gt;=40</c>. This port reads <c>\d</c> as <c>[0-9]</c> and <c>\s</c> (and
    /// the strip) as space or horizontal tab only, and lowercases ASCII letters only, so a value with any other digit or
    /// white space is <c>unparseable</c>. The number is not parsed: it is compared exactly with the points where a
    /// binary64 conversion (Python's <c>float</c>) rounds to 20 and to 40, so the bin equals the original's for any digit
    /// string, under net48 and net8.0 alike.
    /// </remarks>
    public static class CollisionEnergyBin
    {
        public const string Unparseable = "unparseable";

        private static readonly Regex Pattern = new Regex(@"^(?:nce[ \t]*[=:]?[ \t]*)?([0-9]+(?:\.[0-9]+)?)[ \t]*(ev|v|%|nce)?\z",
            RegexOptions.CultureInvariant);

        // The halfway points below 20 and 40 (20 - 2^-49, 40 - 2^-48): a decimal at or above one rounds to 20 (or 40),
        // because both are even in the last mantissa bit.
        private const string Below20 = "19.9999999999999982236431605997495353221893310546875";
        private const string Below40 = "39.999999999999996447286321199499070644378662109375";

        /// <summary>The bin of a COLLISIONENERGY value (null or absent: <see cref="Unparseable"/>, as the original's
        /// empty default).</summary>
        public static string Of(string value) {
            var s = DiagrulesMspReader.AsciiLower(DiagrulesMspReader.TrimSpaceTab(value ?? string.Empty));
            var m = Pattern.Match(s);
            if (!m.Success) {
                return Unparseable;
            }
            string unit;
            if (s.IndexOf('%') >= 0 || s.IndexOf("nce", StringComparison.Ordinal) >= 0) {
                unit = "NCE";
            }
            else if (m.Groups[2].Value == "ev" || m.Groups[2].Value == "v") {
                unit = "eV";
            }
            else {
                unit = "unspecified";
            }
            var number = m.Groups[1].Value;
            return unit + (Less(number, Below20) ? ":<20" : Less(number, Below40) ? ":20-<40" : ":>=40");
        }

        /// <summary>Whether the decimal <paramref name="a"/> (<c>[0-9]+(\.[0-9]+)?</c>) is below the decimal
        /// <paramref name="b"/> (same form), compared exactly.</summary>
        internal static bool Less(string a, string b) {
            Split(a, out var ai, out var af);
            Split(b, out var bi, out var bf);
            if (ai.Length != bi.Length) {
                return ai.Length < bi.Length;
            }
            var c = string.CompareOrdinal(ai, bi);
            if (c != 0) {
                return c < 0;
            }
            var n = Math.Max(af.Length, bf.Length);
            for (var k = 0; k < n; k++) {
                var x = k < af.Length ? af[k] : '0';
                var y = k < bf.Length ? bf[k] : '0';
                if (x != y) {
                    return x < y;
                }
            }
            return false;
        }

        private static void Split(string d, out string integral, out string fraction) {
            var dot = d.IndexOf('.');
            integral = (dot < 0 ? d : d.Substring(0, dot)).TrimStart('0');
            fraction = dot < 0 ? string.Empty : d.Substring(dot + 1);
        }
    }
}
