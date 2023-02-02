using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CompMs.CommonMVVM.Converter
{
    // reference https://ponyoth.hateblo.jp/entry/2016/07/26/173603
    [ValueConversion(typeof(Enum), typeof(bool))]
    public class BooleanToEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || parameter == null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || parameter == null) return Binding.DoNothing;
            if ((bool)value) {
                return Enum.Parse(targetType, parameter.ToString());
            }
            return Binding.DoNothing;
        }
    }
}
