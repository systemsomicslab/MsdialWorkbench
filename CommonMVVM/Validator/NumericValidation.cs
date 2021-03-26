using System.Globalization;
using System.Windows.Controls;

namespace CompMs.CommonMVVM.Validator
{
    public class DoubleNumericValidation : ValidationRule {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (string.IsNullOrEmpty((string)value)) {
                return new ValidationResult(false, "Enter a value");
            }
            if (!double.TryParse(value.ToString(), out _)) {
                return new ValidationResult(false, "Double value is required.");
            }
            return new ValidationResult(true, null);
        }
    }

    public class DoubleNumericOrNullValidation : DoubleNumericValidation
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (string.IsNullOrEmpty((string)value)) {
                return new ValidationResult(true, null);
            }
            return base.Validate(value, cultureInfo);
        }
    }
}
