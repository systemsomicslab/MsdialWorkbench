using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Converter
{
    class PeakToChromatogramConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length >= 2 &&
                values[0] is ChromatogramPeakFeatureModel feature &&
                values[1] is EicLoader loader) {
                (var eic, var area, var peak) = loader.LoadEic(feature);
                return new { Eic = eic, EicArea = area, EicPeak = peak };
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
