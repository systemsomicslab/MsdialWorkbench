using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CompMs.CommonMVVM.Validator
{
    public class FileNameCharacterValidation : ValidationRule
    {
        private static readonly char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (!(value is string path)) {
                return new ValidationResult(false, "Enter a value");
            }
            if (path.IndexOfAny(invalidChars) >= 0) {
                return new ValidationResult(false, "Invalid character contains.");
            }
            return new ValidationResult(true, null);
        }
    }
}
