using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class Library : ObservableObject {
		private string _description = "";

		[DataMember(Name = "description")]
		public string description {
			get { return _description; }
			set { if(value == _description) { return; }
				_description = value;
				OnPropertyChanged("description");
			}
		}
	}
}
