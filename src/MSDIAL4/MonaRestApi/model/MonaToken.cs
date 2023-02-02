using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public sealed class MonaToken : ObservableObject {
		[DataMember(Name = "token")]
		public string Token { get; set; }
	}
}
