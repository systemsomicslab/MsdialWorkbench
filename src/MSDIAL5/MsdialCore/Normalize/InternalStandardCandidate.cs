using System;
using System.Text.RegularExpressions;

namespace CompMs.MsdialCore.Normalize
{
    /// <summary>
    /// Tells whether an annotation looks like an isotope-labelled internal standard,
    /// e.g. "PC 15:0_18:1(d7)", "Leucine-d10", "[13C6]-Glucose" or a lipid class such as "PC_d5".
    /// </summary>
    public static class InternalStandardCandidate
    {
        // A label is deuterium, 13C or 15N with an optional count: d7, D9, 13C6, 15N2.
        private const string BracketLabel = @"(?:[dD]\d{1,2}|13C\d{0,2}|15N\d{0,2})";
        // Outside brackets only a lowercase "d" counts, so names such as "Vitamin-D3" stay out.
        private const string BareLabel = @"(?:d\d{1,2}|13C\d{0,2}|15N\d{0,2})";

        private static readonly Regex LabelledNameRegex = new Regex(
            // "(d7)", "[13C6]", "(13C6,15N2)"
            @"[\(\[]\s*" + BracketLabel + @"(?:\s*[,;/+]\s*" + BracketLabel + @")*\s*[\)\]]"
            // "Leucine-d10", "Glucose-13C6"; a sphingoid base such as "-d18:1" is not a label.
            + @"|-" + BareLabel + @"(?![A-Za-z0-9:])"
            // "d3-Creatinine", "13C6-Glucose"
            + @"|(?<![A-Za-z0-9])" + BareLabel + @"-(?=[A-Za-z0-9\(\[])",
            RegexOptions.CultureInvariant);

        // Labelled lipid classes of the lipid library (LbmClass): CE_d7, Cer_NS_d7, PC_d5, SM_d9, ...
        private static readonly Regex LabelledClassRegex = new Regex(@"_d\d+$", RegexOptions.CultureInvariant);

        /// <summary>True when the name carries an isotope label.</summary>
        public static bool IsLabelledName(string? name) {
            return !string.IsNullOrEmpty(name) && LabelledNameRegex.IsMatch(name);
        }

        /// <summary>True when the ontology is a labelled lipid class or a SPLASH standard.</summary>
        public static bool IsLabelledStandardClass(string? ontology) {
            if (string.IsNullOrEmpty(ontology)) {
                return false;
            }
            return LabelledClassRegex.IsMatch(ontology) || string.Equals(ontology, "SPLASH", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True when either the name or the ontology marks a labelled internal standard.</summary>
        public static bool IsCandidate(string? name, string? ontology) {
            return IsLabelledName(name) || IsLabelledStandardClass(ontology);
        }
    }
}
