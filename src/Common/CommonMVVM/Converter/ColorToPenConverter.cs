using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.CommonMVVM.Converter
{
    [ValueConversion(typeof(Color), typeof(Pen))]
    public class ColorToPenConverter : IValueConverter
    {
        public double Thickness { get; set; } = 2d;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is Color color) {
                var brush = new SolidColorBrush(color);
                var pen = new Pen(brush, Thickness);
                pen.Freeze();
                return pen;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }
    }
}
