using CompMs.Common.DataObj.Property;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CompMs.CommonMVVM.Converter
{
    [ValueConversion(typeof(AdductIon), typeof(string))]
    public sealed class AdductIonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AdductIon adduct) {
                return adduct.AdductIonName;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string adduct) {
                return AdductIon.GetAdductIon(adduct);
            }
            return AdductIon.Default;
        }
    }
}
