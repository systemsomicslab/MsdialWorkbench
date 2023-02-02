using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CompMs.Graphics.Converter
{
    [ValueConversion(typeof(AxisValue), typeof(double))]
    public class AxisValueToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is AxisValue axisvalue) {
                return axisvalue.Value;
            }
            return double.NaN;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is double realvalue) {
                return new AxisValue(realvalue);
            }
            return Binding.DoNothing;
        }
    }
}
