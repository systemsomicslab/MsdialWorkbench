using edu.ucdavis.fiehnlab.MonaRestApi.model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace edu.ucdavis.fiehnlab.MonaExport.UtilClasses {
	public class ValueToMetadataConverter : IMultiValueConverter {
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			return new MetaData { Name = (string)values[0], Value = (string)values[1] };
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
			return new string[] { ((MetaData)value).Name, ((MetaData)value).Value as string };
		}
	}
}
