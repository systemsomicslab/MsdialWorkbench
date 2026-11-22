using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.IO;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// Reads and writes the diagnostic-ion rule settings in MSFINDER.INI (design 5.1).
    /// </summary>
    /// <remarks>
    /// The settings reader offers every line to <see cref="TryRead"/> before its legacy patterns, so a
    /// <c>DiagnosticRule*</c> line never reaches them. The writer calls <see cref="Write"/> after the legacy
    /// bytes, and it writes nothing while every setting is at its default, so a default settings file keeps
    /// its bytes. Numbers are read with <see cref="DecimalText"/> and written with <see cref="ExactDecimal"/>,
    /// so neither the culture nor the target framework changes the text.
    /// </remarks>
    public static class DiagnosticRuleIni
    {
        public const string BlockHeader = "#Diagnostic-ion rules (experimental)";

        public const string EvaluationKey = "DiagnosticRuleEvaluation";
        public const string RankingWeightKey = "DiagnosticRuleRankingWeight";
        public const string RankingScopeKey = "DiagnosticRuleRankingScope";
        public const string RuleSetDirectoryKey = "DiagnosticRuleSetDirectory";
        public const string InstrumentTypeKey = "DiagnosticRuleInstrumentType";
        public const string StrictKey = "DiagnosticRuleStrict";

        /// <summary>The number of decimals the weight can have, and is written with.</summary>
        public const int RankingWeightDecimals = 6;

        private const string Prefix = "DiagnosticRule";

        /// <summary>
        /// Reads one settings-file line if it matches <c>^DiagnosticRule[A-Za-z]*=</c> (ASCII letters, case
        /// ignored), and returns true; returns false for any other line, which the legacy reader then handles.
        /// An unknown key or a bad value is added to <see cref="MsfinderDiagnosticRuleSettings.Errors"/> and
        /// leaves the setting unchanged.
        /// </summary>
        public static bool TryRead(string line, MsfinderDiagnosticRuleSettings settings) {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (line is null || !AsciiStartsWithIgnoreCase(line, Prefix)) return false;
            var end = Prefix.Length;
            while (end < line.Length && IsAsciiLetter(line[end])) end++;
            if (end >= line.Length || line[end] != '=') return false;

            var key = line.Substring(0, end);
            var value = line.Substring(end + 1).Trim();

            if (AsciiEqualsIgnoreCase(key, EvaluationKey)) {
                if (TryReadBool(value, out var b)) settings.Evaluation = b;
                else settings.Errors.Add(BadValue(key, value, "expected True or False"));
            }
            else if (AsciiEqualsIgnoreCase(key, RankingWeightKey)) {
                if (TryReadWeight(value, out var w)) settings.RankingWeight = w;
                else settings.Errors.Add(BadValue(key, value, "expected a finite number >= 0 with at most 6 decimals"));
            }
            else if (AsciiEqualsIgnoreCase(key, RankingScopeKey)) {
                if (AsciiEqualsIgnoreCase(value, "Reviewed")) settings.RankingScope = DiagnosticRuleRankingScope.Reviewed;
                else if (AsciiEqualsIgnoreCase(value, "Measured")) settings.RankingScope = DiagnosticRuleRankingScope.Measured;
                else settings.Errors.Add(BadValue(key, value, "expected Reviewed or Measured"));
            }
            else if (AsciiEqualsIgnoreCase(key, RuleSetDirectoryKey)) {
                settings.RuleSetDirectory = value;
            }
            else if (AsciiEqualsIgnoreCase(key, InstrumentTypeKey)) {
                settings.InstrumentType = value;
            }
            else if (AsciiEqualsIgnoreCase(key, StrictKey)) {
                if (TryReadBool(value, out var b)) settings.Strict = b;
                else settings.Errors.Add(BadValue(key, value, "expected True or False"));
            }
            else {
                settings.Errors.Add("unknown settings key " + key);
            }
            return true;
        }

        /// <summary>
        /// Writes the block of all six settings, in table order, unless every setting is at its default; then it
        /// writes nothing. <see cref="MsfinderDiagnosticRuleSettings.Errors"/> is never written.
        /// </summary>
        public static void Write(TextWriter writer, MsfinderDiagnosticRuleSettings settings) {
            if (writer is null) throw new ArgumentNullException(nameof(writer));
            if (settings is null || settings.IsDefault) return;

            writer.WriteLine(BlockHeader);
            writer.WriteLine(EvaluationKey + "=" + BoolText(settings.Evaluation));
            writer.WriteLine(RankingWeightKey + "=" + ExactDecimal.Format(settings.RankingWeight, RankingWeightDecimals));
            writer.WriteLine(RankingScopeKey + "=" + (settings.RankingScope == DiagnosticRuleRankingScope.Measured ? "Measured" : "Reviewed"));
            writer.WriteLine(RuleSetDirectoryKey + "=" + (settings.RuleSetDirectory ?? string.Empty));
            writer.WriteLine(InstrumentTypeKey + "=" + (settings.InstrumentType ?? string.Empty));
            writer.WriteLine(StrictKey + "=" + BoolText(settings.Strict));
            writer.WriteLine();
        }

        /// <summary>
        /// The weight rule of design 5.1: <see cref="DecimalText"/> text of a finite value of at least 0 that
        /// survives the round trip through its six-decimal text, so <see cref="Write"/> and
        /// <see cref="TryRead"/> are inverse. -0 is read as 0.
        /// </summary>
        public static bool TryReadWeight(string value, out double weight) {
            weight = 0.0;
            if (!DecimalText.TryParse(value, out double w)) return false;
            return TryAcceptWeight(w, out weight);
        }

        /// <summary>
        /// The numeric half of <see cref="TryReadWeight"/>, for a weight that arrives as a number (the GUI
        /// parameter window): finite, at least 0, and equal to its own six-decimal text, so the weight that
        /// ranks is the weight the INI, the context table and the run manifest record. -0 is accepted as 0.
        /// </summary>
        public static bool TryAcceptWeight(double value, out double weight) {
            weight = 0.0;
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value >= 0.0)) return false;
            if (!DecimalText.TryParse(ExactDecimal.Format(value, RankingWeightDecimals), out double back) || back != value) return false;
            weight = value == 0.0 ? 0.0 : value;
            return true;
        }

        private static bool TryReadBool(string value, out bool result) {
            result = false;
            if (AsciiEqualsIgnoreCase(value, "True")) { result = true; return true; }
            return AsciiEqualsIgnoreCase(value, "False");
        }

        private static string BoolText(bool value) => value ? "True" : "False";

        private static string BadValue(string key, string value, string expected) {
            return "bad value for " + key + " (" + expected + "): '" + value + "'";
        }

        private static bool IsAsciiLetter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

        private static char AsciiLower(char c) => c >= 'A' && c <= 'Z' ? (char)(c + ('a' - 'A')) : c;

        private static bool AsciiStartsWithIgnoreCase(string text, string prefix) {
            if (text.Length < prefix.Length) return false;
            for (var i = 0; i < prefix.Length; i++) {
                if (AsciiLower(text[i]) != AsciiLower(prefix[i])) return false;
            }
            return true;
        }

        private static bool AsciiEqualsIgnoreCase(string a, string b) {
            return a.Length == b.Length && AsciiStartsWithIgnoreCase(a, b);
        }
    }
}
