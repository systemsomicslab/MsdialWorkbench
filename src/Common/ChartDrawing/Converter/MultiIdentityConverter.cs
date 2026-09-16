using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace CompMs.Graphics.Converter
{
    public class MultiIdentityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            return values.ToArray();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}
