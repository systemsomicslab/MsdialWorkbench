using System;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>
    /// The instrument class of free instrument-type text (design 4.9, after the P2 helper's <c>instrument_bin</c>). The
    /// reference for both languages is:
    /// <code>
    /// _UP = {c: c - 32 for c in range(0x61, 0x7B)}           # a-z only
    /// def instrument_class(value):
    ///     if value is None or value.strip(" \t\r\n") == "": return "unknown"
    ///     s = value.translate(_UP)
    ///     if "TOF" in s: return "QTOF-type"
    ///     if any(t in s for t in ("ORBITRAP", "FT", "Q EXACTIVE", "Q-EXACTIVE")): return "Orbitrap/FT"
    ///     return "other"
    /// </code>
    /// Only a-z are upper-cased, the blank test knows only SP, HT, CR and LF, and the search is ordinal: never
    /// <c>IsNullOrWhiteSpace</c>, <c>ToUpper</c> or a culture comparison.
    /// </summary>
    internal static class InstrumentClassifier
    {
        public const string Unknown = "unknown";
        public const string QtofType = "QTOF-type";
        public const string OrbitrapFt = "Orbitrap/FT";
        public const string Other = "other";

        private static readonly string[] OrbitrapTokens = { "ORBITRAP", "FT", "Q EXACTIVE", "Q-EXACTIVE" };

        public static string Classify(string? value) {
            if (value is null || IsBlank(value)) {
                return Unknown;
            }
            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++) {
                if (chars[i] >= 'a' && chars[i] <= 'z') {
                    chars[i] = (char)(chars[i] - 32);
                }
            }
            var s = new string(chars);
            if (s.IndexOf("TOF", StringComparison.Ordinal) >= 0) {
                return QtofType;
            }
            foreach (var token in OrbitrapTokens) {
                if (s.IndexOf(token, StringComparison.Ordinal) >= 0) {
                    return OrbitrapFt;
                }
            }
            return Other;
        }

        private static bool IsBlank(string value) {
            foreach (var c in value) {
                if (c != ' ' && c != '\t' && c != '\r' && c != '\n') {
                    return false;
                }
            }
            return true;
        }
    }
}
