using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CompMs.Common.Algorithm.Scoring
{
    /// <summary>
    /// The canonical identity string of a spectral-similarity kernel: <c>&lt;base&gt;?&lt;k&gt;=&lt;v&gt;&amp;…</c>.
    /// </summary>
    /// <remarks>
    /// <para>The grammar is the shared one of the MS-DIAL spectrum catalog's similarity module (contract
    /// C3), so that a score computed here and one computed in Python carry the same string when, and only
    /// when, they mean the same thing:</para>
    /// <list type="bullet">
    /// <item>keys in ordinal (ASCII) order, every key of the base always present;</item>
    /// <item>numbers in plain decimal, without exponent, with the shortest digits that round-trip,
    /// without trailing zeros or a trailing '.', -0 written as 0 and whole numbers as integers;</item>
    /// <item><c>prep=mn</c> brings every <c>prep.*</c> key; <c>prep=none</c> means "sort by m/z only".</item>
    /// </list>
    /// <para>The comparison semantics (strict for the msdial bases, inclusive for gnps) belong to the base,
    /// not to a key. Only the bases this assembly computes are parsed.</para>
    /// </remarks>
    public static class KernelIdentity
    {
        public const string ModDotLegacy = "msdial.moddot.legacy";
        public const string Bonanza = "msdial.bonanza.v1";
        public const string CosineMatchedLegacy = "msdial.cosine_matched.legacy";
        public const string ModifiedCosine = "gnps.modified_cosine";

        /// <summary>
        /// What the GUI writes for MS-DIAL's "Cosine" mode, which has no registered identity. It is
        /// never a valid identity: <see cref="Parse"/> refuses it.
        /// </summary>
        public const string UnregisteredSimpleDot = "msdial.simpledot (unregistered)";

        private static readonly string[] MnPrepKeys = {
            "prep.abs", "prep.above", "prep.frame", "prep.maxmz", "prep.minmz", "prep.pow",
            "prep.precwin", "prep.rel", "prep.scale", "prep.topk", "prep.topkwin",
        };

        private static readonly Dictionary<string, string[]> KernelKeys = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [ModDotLegacy] = new[] { "tol" },
            [Bonanza] = new[] { "tol" },
            [CosineMatchedLegacy] = new[] { "tol" },
            [ModifiedCosine] = new[] { "assign", "ipow", "mzpow", "tol" },
        };

        /// <summary>The bases that may be written bare, and what they expand to: the MS-DIAL MN defaults.</summary>
        private static readonly Dictionary<string, Func<string>> BareAliases = new Dictionary<string, Func<string>>(StringComparer.Ordinal)
        {
            [ModDotLegacy] = () => Format(ModDotLegacy, MnPreprocessing.LegacyDefaults.ToParameters(), Tol(0.025)),
            [Bonanza] = () => Format(Bonanza, MnPreprocessing.LegacyDefaults.ToParameters(), Tol(0.025)),
        };

        private static IEnumerable<KeyValuePair<string, string>> Tol(double tolerance) {
            yield return new KeyValuePair<string, string>("tol", FormatNumber(tolerance));
        }

        /// <summary>Whether this assembly computes <paramref name="baseName"/>.</summary>
        public static bool IsKnownBase(string baseName) => baseName != null && KernelKeys.ContainsKey(baseName);

        /// <summary>
        /// A number as the grammar writes it: plain decimal, shortest round-trip digits, no exponent.
        /// </summary>
        /// <remarks>
        /// Not <c>ToString("R")</c>: on .NET Framework it can write an exponent (1E-05) and is not
        /// guaranteed to be the shortest. The digits come from the shortest "E" precision whose text
        /// parses back to the same double, which is the correctly rounded, and therefore the closest,
        /// candidate of that length.
        /// </remarks>
        public static string FormatNumber(double value) {
            if (double.IsNaN(value) || double.IsInfinity(value)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, "A kernel identity number must be finite.");
            }
            if (value == 0d) {
                return "0";
            }
            string scientific = null;
            for (int precision = 0; precision < 17; precision++) {
                var candidate = value.ToString("E" + precision.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                if (double.Parse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture) == value) {
                    scientific = candidate;
                    break;
                }
            }
            scientific = scientific ?? value.ToString("E16", CultureInfo.InvariantCulture);

            var negative = scientific[0] == '-';
            var body = negative ? scientific.Substring(1) : scientific;
            var e = body.IndexOf('E');
            var digits = body.Substring(0, e).Replace(".", string.Empty).TrimEnd('0');
            var exponent = int.Parse(body.Substring(e + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            // value = 0.d1d2…dn × 10^(exponent + 1)
            var point = exponent + 1;
            string text;
            if (point <= 0) {
                text = "0." + new string('0', -point) + digits;
            }
            else if (point >= digits.Length) {
                text = digits + new string('0', point - digits.Length);
            }
            else {
                text = digits.Substring(0, point) + "." + digits.Substring(point);
            }
            return negative ? "-" + text : text;
        }

        /// <summary>
        /// Write an identity from already formatted values. Keys are put in ordinal order.
        /// </summary>
        public static string Format(string baseName, params IEnumerable<KeyValuePair<string, string>>[] parameterGroups) {
            var parameters = parameterGroups.SelectMany(group => group).OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList();
            var builder = new StringBuilder(baseName);
            for (int i = 0; i < parameters.Count; i++) {
                builder.Append(i == 0 ? '?' : '&').Append(parameters[i].Key).Append('=').Append(parameters[i].Value);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Read an identity of a base this assembly computes, checking every key and value, and
        /// return it with its values in canonical form.
        /// </summary>
        /// <exception cref="FormatException">An unknown base, a missing, unknown or repeated key, or a bad value.</exception>
        public static ParsedKernelIdentity Parse(string identity) {
            if (string.IsNullOrEmpty(identity)) {
                throw new FormatException("A kernel identity is empty.");
            }
            var question = identity.IndexOf('?');
            if (question < 0) {
                if (BareAliases.TryGetValue(identity, out var expand)) {
                    return Parse(expand());
                }
                throw new FormatException($"'{identity}' has no parameters and is not a bare alias.");
            }
            var baseName = identity.Substring(0, question);
            if (!KernelKeys.TryGetValue(baseName, out var kernelKeys)) {
                throw new FormatException($"Unknown kernel base '{baseName}'.");
            }

            var given = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var part in identity.Substring(question + 1).Split('&')) {
                var equals = part.IndexOf('=');
                if (equals <= 0) {
                    throw new FormatException($"'{part}' is not a key=value pair.");
                }
                var key = part.Substring(0, equals);
                if (given.ContainsKey(key)) {
                    throw new FormatException($"The key '{key}' is repeated.");
                }
                given[key] = part.Substring(equals + 1);
            }

            if (!given.TryGetValue("prep", out var prep)) {
                throw new FormatException("The key 'prep' is missing.");
            }
            var expected = new List<string>(kernelKeys) { "prep" };
            switch (prep) {
                case "mn":
                    expected.AddRange(MnPrepKeys);
                    break;
                case "none":
                    break;
                default:
                    throw new FormatException($"prep must be mn or none, not '{prep}'.");
            }
            foreach (var key in expected) {
                if (!given.ContainsKey(key)) {
                    throw new FormatException($"The key '{key}' is missing.");
                }
            }
            foreach (var key in given.Keys) {
                if (!expected.Contains(key)) {
                    throw new FormatException($"The key '{key}' does not belong to {baseName} with prep={prep}.");
                }
            }

            var canonical = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in given) {
                canonical[pair.Key] = CanonicalValue(pair.Key, pair.Value);
            }
            return new ParsedKernelIdentity(baseName, canonical);
        }

        /// <summary>The canonical form of an identity of a base this assembly computes.</summary>
        public static string Canonical(string identity) => Parse(identity).ToString();

        private static string CanonicalValue(string key, string value) {
            switch (key) {
                case "prep":
                    return value;
                case "assign":
                    if (value == "greedy" || value == "optimal") return value;
                    throw new FormatException($"assign must be greedy or optimal, not '{value}'.");
                case "prep.above":
                    return value == "none" ? value : FormatNumber(ReadNumber(key, value));
                case "prep.topk": {
                        var number = ReadNumber(key, value);
                        if (number < 0 || number != Math.Floor(number)) {
                            throw new FormatException($"prep.topk must be a whole number >= 0, not '{value}'.");
                        }
                        return FormatNumber(number);
                    }
                default:
                    return FormatNumber(ReadNumber(key, value));
            }
        }

        private static double ReadNumber(string key, string value) {
            if (value.Length == 0
                || char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])
                || !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                || double.IsNaN(number) || double.IsInfinity(number)) {
                throw new FormatException($"{key} must be a finite number, not '{value}'.");
            }
            return number;
        }
    }

    /// <summary>A parsed kernel identity: its base and its values in canonical text.</summary>
    public sealed class ParsedKernelIdentity
    {
        internal ParsedKernelIdentity(string baseName, IReadOnlyDictionary<string, string> parameters) {
            Base = baseName;
            Parameters = parameters;
        }

        public string Base { get; }
        public IReadOnlyDictionary<string, string> Parameters { get; }

        public string Text(string key) => Parameters[key];

        public double Number(string key) => double.Parse(Parameters[key], NumberStyles.Float, CultureInfo.InvariantCulture);

        /// <summary>The preprocessing the identity names, or null for <c>prep=none</c> (sort by m/z only).</summary>
        public MnPreprocessing Preprocessing() {
            if (Text("prep") == "none") {
                return null;
            }
            return new MnPreprocessing(
                relativeAbundanceCutOff: Number("prep.rel"),
                absoluteAbundanceCutOff: Number("prep.abs"),
                above: Text("prep.above") == "none" ? (double?)null : Number("prep.above"),
                minMz: Number("prep.minmz"),
                maxMz: Number("prep.maxmz"),
                frame: Number("prep.frame"),
                power: Number("prep.pow"),
                scale: Number("prep.scale"),
                precursorWindow: Number("prep.precwin"),
                topK: (int)Number("prep.topk"),
                topKWindow: Number("prep.topkwin"));
        }

        public override string ToString() => KernelIdentity.Format(Base, Parameters);
    }
}
