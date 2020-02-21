using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
