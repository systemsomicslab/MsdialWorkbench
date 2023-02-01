using System;
using System.Windows;
using System.Windows.Data;

namespace edu.ucdavis.fiehnlab.MonaExport.UtilClasses
{
	public class TrimmedTextBlockVisibilityConverter : IValueConverter {


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value == null) return Visibility.Collapsed;

            FrameworkElement textBlock = (FrameworkElement)value;

            textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

            if (((FrameworkElement)value).ActualWidth < ((FrameworkElement)value).DesiredSize.Width)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
