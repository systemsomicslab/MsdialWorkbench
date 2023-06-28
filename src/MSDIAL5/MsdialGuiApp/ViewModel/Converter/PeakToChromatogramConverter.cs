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
                values[1] is IChromatogramLoader<ChromatogramPeakFeatureModel> loader) {
                var chromatogram = loader.LoadChromatogramAsync(feature, default).Result;
                return chromatogram;
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
