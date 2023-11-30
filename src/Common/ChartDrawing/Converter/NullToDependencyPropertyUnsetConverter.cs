using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CompMs.Graphics.Converter
{
    internal sealed class NullToDependencyPropertyUnsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value ?? DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
