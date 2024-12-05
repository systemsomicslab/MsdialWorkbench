using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Chart;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Converter
{
    public sealed class BarItemCollectionToSmallClassBarChartViewModel : DependencyObject, IMultiValueConverter, IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is SpotBarItemCollection collection) {
                return new SmallClassBarChartViewModel(collection);
            }
            return null;
        }

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length >= 2 && values[0] is AlignmentSpotPropertyModel spot && values[1] is IObservable<IBarItemsLoader> loader) {
                if (values.Length >= 3 && values[2] is FileClassPropertiesModel fileClasses) {
                    return new SmallClassBarChartViewModel(SpotBarItemCollection.Create(spot, loader), fileClasses);
                }
                return new SmallClassBarChartViewModel(SpotBarItemCollection.Create(spot, loader));
            }
            if (values.Length >= 1 && values[0] is SpotBarItemCollection collection) {
                return new SmallClassBarChartViewModel(collection);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            return new[] { Binding.DoNothing };
        }
    }
}
