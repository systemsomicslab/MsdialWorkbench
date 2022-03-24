using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Converter
{
    class SpotToBarItemConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length >= 2 &&
                values[0] is AlignmentSpotPropertyModel spot &&
                values[1] is IBarItemsLoader loader) {
                return loader.LoadBarItems(spot);
            }
            return new List<BarItem>(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
