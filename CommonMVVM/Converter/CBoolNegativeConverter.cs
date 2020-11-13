using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Rfx.Riken.OsakaUniv {
    public class CBoolNegativeConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return !(value is bool && (bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return !(value is bool && (bool)value);
        }

    }
}

namespace CompMs.CommonMVVM.Converter
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class CBoolNegativeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return !(value is bool b && b);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return !(value is bool b && b);
        }
    }
}
