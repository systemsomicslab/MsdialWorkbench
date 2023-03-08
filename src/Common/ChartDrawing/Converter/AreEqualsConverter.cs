using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace CompMs.Graphics.Converter
{
    public class AreEqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return false;
            var rep = values[0];
            foreach (var item in values.Skip(1))
                if (!Equals(rep, item))
                    return false;
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
