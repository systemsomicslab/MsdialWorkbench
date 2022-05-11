using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Chart;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Converter
{
    public class BarItemCollectionToSmallClassBarChartViewModel : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is BarItemCollection collection) {
                return new SmallClassBarChartViewModel(collection);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }
    }
}
