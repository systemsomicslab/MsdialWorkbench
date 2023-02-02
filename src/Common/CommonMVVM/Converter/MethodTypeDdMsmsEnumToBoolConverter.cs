using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Rfx.Riken.OsakaUniv
{
    [ValueConversion(typeof(MethodType), typeof(bool))]
    public class MethodTypeDdMsmsEnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            MethodType methodType = (MethodType)value;
            if (methodType == MethodType.ddMSMS) return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
