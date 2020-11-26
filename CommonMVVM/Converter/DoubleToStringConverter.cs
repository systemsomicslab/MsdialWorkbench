using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.CommonMVVM.Converter
{
    [System.Windows.Data.ValueConversion(typeof(double), typeof(string))]
    public class DoubleToStringConverter : System.Windows.Data.IValueConverter
    {
        private string convertBackString; //input string

        //double to string (used when displayed)
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            try {
                if (!(value is double)) return null;

                string format = (parameter as string) ?? string.Empty;
                if (!string.IsNullOrEmpty(convertBackString)) {
                    //divided formats to int and decimals
                    var formatParts = format.Split('.');
                    if (formatParts.Length >= 2) {
                        if (convertBackString.EndsWith(culture.NumberFormat.NumberDecimalSeparator)) {
                            format = formatParts[0] + @"\" + culture.NumberFormat.NumberDecimalSeparator;
                        }
                        else {
                            var pos = convertBackString.IndexOf(culture.NumberFormat.NumberDecimalSeparator);
                            if (pos >= 0) {
                                var digitLength = convertBackString.Length - pos - 1;
                                format = formatParts[0] + @"."
                                    + new string('0', digitLength)
                                    + formatParts[1].Substring(Math.Min(digitLength, formatParts[1].Length));
                            }
                        }
                    }
                }
                return ((double)value).ToString(format);
            }
            finally {
                convertBackString = null;
            }
        }

        //string to double
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            convertBackString = value as string;

            if (!string.IsNullOrEmpty(convertBackString)
              && double.TryParse(convertBackString, out double newValue)) {
                return newValue;
            }
            else {
                return null;
            }
        }
    }

}

namespace Rfx.Riken.OsakaUniv {
    //double string converter
    public class DoubleToStringConverter : System.Windows.Data.IValueConverter {
        private string convertBackString; //input string

        //double to string (used when displayed)
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            try {
                if (!(value is double)) return null;

                string format = (parameter as string) ?? string.Empty;
                if (!string.IsNullOrEmpty(convertBackString)) {
                    //divided formats to int and decimals
                    var formatParts = format.Split('.');
                    if (formatParts.Length >= 2) {
                        if (convertBackString.EndsWith(culture.NumberFormat.NumberDecimalSeparator)) {
                            format = formatParts[0] + @"\" + culture.NumberFormat.NumberDecimalSeparator;
                        }
                        else {
                            var pos = convertBackString.IndexOf(culture.NumberFormat.NumberDecimalSeparator);
                            if (pos >= 0) {
                                var digitLength = convertBackString.Length - pos - 1;
                                format = formatParts[0] + @"."
                                    + new string('0', digitLength)
                                    + formatParts[1].Substring(Math.Min(digitLength, formatParts[1].Length));
                            }
                        }
                    }
                }
                return ((double)value).ToString(format);
            }
            finally {
                convertBackString = null;
            }
        }

        //string to double
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            convertBackString = value as string;

            if (!string.IsNullOrEmpty(convertBackString)
              && double.TryParse(convertBackString, out double newValue)) {
                return newValue;
            }
            else {
                return null;
            }
        }
    }
}
