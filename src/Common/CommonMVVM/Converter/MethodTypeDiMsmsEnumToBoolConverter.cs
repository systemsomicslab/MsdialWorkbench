using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Rfx.Riken.OsakaUniv
{
    [ValueConversion(typeof(MethodType), typeof(bool))]
    public class MethodTypeDiMsmsEnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            MethodType methodType = (MethodType)value;
            if (methodType == MethodType.diMSMS) return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class MethodTypeDiMsmsEnumToBoolConverterMulti : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            MethodType methodType = (MethodType)value[0];
            bool check = (bool)value[1];
            if (methodType == MethodType.diMSMS && !check) return true;
            else return false;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) {
            return new object[]{ Binding.DoNothing, Binding.DoNothing};
        }
    }

    public class MethodTypeDiAifMsmsEnumToBoolConverterMulti : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            MethodType methodType = (MethodType)value[0];
            bool check = (bool)value[1];
            if (methodType == MethodType.diMSMS && check) return true;
            else return false;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }

}
