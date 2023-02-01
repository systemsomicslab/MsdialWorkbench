using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class Name : ObservableObject {
		private string _name = "";

		[DataMember(Name = "computed"), IgnoreDataMember]
		public bool computed { get; set; }
		[DataMember(Name = "name")]
		public string name { get { return _name; }
			set { if(value == _name) { return; }
				_name = value;
				OnPropertyChanged("name");
			}	
		}
		[DataMember(Name = "score"), IgnoreDataMember]
		public double score { get; set; }
		[DataMember(Name = "source"), IgnoreDataMember]
		public string source { get; set; }

		public override string ToString() {
			return name;
		}
	}
}
