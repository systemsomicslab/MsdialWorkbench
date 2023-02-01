using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public sealed class TokenInfo : ObservableObject {
		[DataMember(Name = "username")]
		public string username { get; set; }

		[DataMember(Name = "validFrom")]
		public long validFrom { get; set; }

		[DataMember(Name = "validTo")]
		public long validTo { get; set; }

		[DataMember(Name = "roles")]
		List<string> roles { get; set; }
	}
}
