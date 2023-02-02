using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace CompMs.CommonMVVM.Converter
{
    [ValueConversion(typeof(IConvertible), typeof(IConvertible))]
    public class ToPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return System.Convert.ToSingle(value) * 100;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return System.Convert.ToSingle(value) / 100;
        }
    }
}
