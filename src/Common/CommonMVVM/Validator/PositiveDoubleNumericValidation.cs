using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CompMs.CommonMVVM.Validator
{
    public class PositiveDoubleNumericValidation : ValidationRule {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (string.IsNullOrEmpty((string)value)) {
                return new ValidationResult(false, "Enter a value");
            }
            if (!double.TryParse(value.ToString(), out double val)) {
                return new ValidationResult(false, "Double value is required.");
            }
            if (val < 0) {
                return new ValidationResult(false, "Positive value is required.");
            }
            return new ValidationResult(true, null);
        }
    }

    public class PositiveDoubleNumericOrNullValidation : PositiveDoubleNumericValidation
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (string.IsNullOrEmpty((string)value)) {
                return new ValidationResult(true, null);
            }
            return base.Validate(value, cultureInfo);
        }
    }
}


namespace Rfx.Riken.OsakaUniv
{
    public class PositiveDoubleNumericValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo) {
            double val;
            if (!string.IsNullOrEmpty((string)value)) {
                if (!double.TryParse(value.ToString(), out val)) {
                    return new ValidationResult(false, "Double value is required.");
                }
                else {
                    if (val < 0) {
                        return new ValidationResult(false, "Positive value is required.");
                    }
                }
                return new ValidationResult(true, null);
            }
            else {
                return new ValidationResult(false, "Enter a value.");
            }
        }
    }

    public class PositiveDoubleNumericOrNullValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo) {
            double val;
            if (!string.IsNullOrEmpty((string)value)) {
                if (!double.TryParse(value.ToString(), out val)) {
                    return new ValidationResult(false, "Double value is required.");
                }
                else {
                    if (val < 0) {
                        return new ValidationResult(false, "Positive value is required.");
                    }
                }
                return new ValidationResult(true, null);
            }
            else {
                return new ValidationResult(true, null);
            }
        }
    }
}
