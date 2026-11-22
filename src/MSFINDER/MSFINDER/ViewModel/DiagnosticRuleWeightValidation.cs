using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System.Globalization;
using System.Windows.Controls;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Validates the diagnostic-rule weight box with the INI reader's rule (design 5.1), so the window
    /// refuses a value that the INI and the run manifest could not record exactly.
    /// </summary>
    public class DiagnosticRuleWeightValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            var text = (value as string)?.Trim();
            if (string.IsNullOrEmpty(text)) return new ValidationResult(false, "Enter a value");
            // The binding converts the text with its own culture, so the two readings must agree
            // (a comma, for example, is a decimal mark to the INI reader and a group separator in en-US).
            if (!DiagnosticRuleIni.TryReadWeight(text, out var weight)
                || !double.TryParse(text, NumberStyles.Float, cultureInfo, out var converted)
                || converted != weight) {
                return new ValidationResult(false, "Enter a number of at least 0 with at most "
                    + DiagnosticRuleIni.RankingWeightDecimals + " decimals.");
            }
            return new ValidationResult(true, null);
        }
    }
}
