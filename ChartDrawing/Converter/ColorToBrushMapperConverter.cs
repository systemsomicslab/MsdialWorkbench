using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.Graphics.Converter
{
    [ValueConversion(typeof(Color), typeof(IBrushMapper))]
    public class ColorToBrushMapperConverter : IValueConverter
    {
        public double Opacity { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is Color color) {
                var brush = new SolidColorBrush(color) { Opacity = Opacity, };
                brush.Freeze();
                return new ConstantBrushMapper(brush);
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is ConstantBrushMapper mapper) {
                if (mapper.Brush is SolidColorBrush brush) {
                    return brush.Color;
                }
            }
            return Binding.DoNothing;
        }
    }
}
