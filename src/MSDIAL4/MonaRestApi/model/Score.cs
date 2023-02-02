using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class Score : ObservableObject {
		[DataMember(Name = "impacts"), IgnoreDataMember]
		public ObservableCollection<Impact> impacts { get; set; }
		[DataMember(Name = "relativeScore"), IgnoreDataMember]
		public double relativeScore { get; set; }
		[DataMember(Name = "scaledScore"), IgnoreDataMember]
		public double scaledScore { get; set; }
		[DataMember(Name = "score"), IgnoreDataMember]
		public double score { get; set; }
	}
}
