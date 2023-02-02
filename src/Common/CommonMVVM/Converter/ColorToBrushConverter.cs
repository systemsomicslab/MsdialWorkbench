using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.CommonMVVM.Converter
{
    [ValueConversion(typeof(Color), typeof(SolidColorBrush))]
    public class ColorToBrushConverter : IValueConverter
    {
        public double Opacity { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is Color color) {
                var brush = new SolidColorBrush(color) { Opacity = Opacity, };
                brush.Freeze();
                return brush;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is SolidColorBrush brush) {
                return brush.Color;
            }
            return Binding.DoNothing;
        }
    }
}
