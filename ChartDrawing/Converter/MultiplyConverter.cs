using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CompMs.Graphics.Converter
{
    [ValueConversion(typeof(IConvertible), typeof(IConvertible))]
    public class MultiplyConverter : IValueConverter
    {
        public double By { get; set; } = 1;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return System.Convert.ToDouble(value) * By;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return System.Convert.ToDouble(value) / By;
        }
    }
}
