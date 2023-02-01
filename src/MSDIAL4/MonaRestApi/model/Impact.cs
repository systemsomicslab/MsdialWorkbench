using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class Impact : ObservableObject {
		[DataMember(Name = "impactValue"), IgnoreDataMember]
		public double impactValue { get; set; }
		[DataMember(Name = "reason"), IgnoreDataMember]
		public string reason { get; set; }
	}
}
