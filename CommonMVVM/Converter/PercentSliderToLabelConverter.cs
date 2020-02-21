using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Rfx.Riken.OsakaUniv
{
    [ValueConversion(typeof(double), typeof(string))]
    public class PercentSliderToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double textValue = (double)value;
            return "Peak spots: " + (int)textValue + "%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
