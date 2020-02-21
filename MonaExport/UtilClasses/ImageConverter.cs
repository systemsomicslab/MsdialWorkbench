using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace edu.ucdavis.fiehnlab.MonaExport.UtilClasses {
	public sealed class ImageConverter : IValueConverter {
		public object Convert(object value, Type targetType,
							  object parameter, CultureInfo culture) {
			try {
				return new BitmapImage(new Uri((string)value));
			} catch {
				return new BitmapImage();
			}
		}

		public object ConvertBack(object value, Type targetType,
								  object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
