using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.Graphics.Converter
{
    [ValueConversion(typeof(string), typeof(Color))]
    public sealed class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is Color color) {
                return color.ToString();
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string text) {
                try {
                    return ColorConverter.ConvertFromString(text);
                }
                catch (FormatException) {
                }
            }
            return Binding.DoNothing;
        }
    }
}
