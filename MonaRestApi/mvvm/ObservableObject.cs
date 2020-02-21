using Microsoft.Practices.EnterpriseLibrary.Validation;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace edu.ucdavis.fiehnlab.MonaRestApi.mvvm {

	public abstract class ObservableObject : INotifyPropertyChanged, IDataErrorInfo {
		#region INPC implementation
		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "") {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion

		#region IDEI implementation
		string IDataErrorInfo.Error {
			get { return string.Empty; }
		}

		string IDataErrorInfo.this[string columnName] {
			get {
				var prop = this.GetType().GetProperty(columnName);
				return this.GetErrorInfo(prop);
			}
		}

		private string GetErrorInfo(PropertyInfo prop) {
			var validator = this.GetPropertyValidator(prop);

			if (validator != null) {
				var results = validator.Validate(this);

				if (!results.IsValid) {
					return string.Join(" ", results.Select(r => r.Message).ToArray());
				}
			}

			return string.Empty;
		}

		private Validator GetPropertyValidator(PropertyInfo prop) {
			string ruleset = string.Empty;
			var source = ValidationSpecificationSource.All;
			var builder = new ReflectionMemberValueAccessBuilder();
			return PropertyValidationFactory.GetPropertyValidator(
				this.GetType(), prop, ruleset, source, builder);
		}
		#endregion
	}
}
