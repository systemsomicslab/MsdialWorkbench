using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CompMs.CommonMVVM.Validator
{
    public class PositiveIntNumericValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (string.IsNullOrEmpty((string)value)) {
                return new ValidationResult(false, "Enter a value.");
            }
            if (!int.TryParse(value.ToString(), out int val)) {
                return new ValidationResult(false, "Int value is required.");
            }
            if (val < 0) {
                return new ValidationResult(false, "Positive value is required.");
            }
            return new ValidationResult(true, null);
        }
    }
}

namespace Rfx.Riken.OsakaUniv
{
    public class PositiveIntNumericValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            int val;
            if (!string.IsNullOrEmpty((string)value))
            {
                if (!int.TryParse(value.ToString(), out val))
                {
                    return new ValidationResult(false, "Int value is required.");
                }
                else
                {
                    if (val < 0)
                    {
                        return new ValidationResult(false, "Positive value is required.");
                    }
                }
                return new ValidationResult(true, null);
            }
            else
            {
                return new ValidationResult(false, "Enter a value.");
            }
        }
    }
}
