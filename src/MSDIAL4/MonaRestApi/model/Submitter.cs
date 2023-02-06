using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class Submitter : ObservableObject {
		private string _firstName = "";
		private string _lastName = "";
		private string _emailAddress = "";
		private string _institution = "";

		[DataMember(Name = "id"), IgnoreDataMember]
		public string id { get; set; }
		[DataMember(Name = "emailAddress"), Required]
		public string emailAddress {
			get { return _emailAddress; }
			set { if(value == _emailAddress) { return; }
				_emailAddress = value;
				OnPropertyChanged("emailAddress");
			}
		}
		[DataMember(Name = "firstName"), Required]
		public string firstName {
			get { return _firstName; }
			set {
				if (value == _firstName) { return; }
				_firstName = value;
				OnPropertyChanged("firstName");
			}
		}
		[DataMember(Name = "institution"), Required]
		public string institution {
			get { return _institution; }
			set {
				if (value == _institution) { return; }
				_institution = value;
				OnPropertyChanged("institution");
			}
		}
		[DataMember(Name = "lastName"), Required]
		public string lastName {
			get { return _lastName; }
			set {
				if (value == _lastName) { return; }
				_lastName = value;
				OnPropertyChanged("lastName");
			}
		}

        public override string ToString() {
			return string.Format("{0}, {1} ({2})", lastName, firstName, emailAddress);
		}
	}
}
