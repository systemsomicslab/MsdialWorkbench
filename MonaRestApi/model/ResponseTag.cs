using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class ResponseTag : ObservableObject {

		private string text = "";
		private long count = 0;

		[DataMember(Name = "text")]
		public string Text {
			get { return text; }
			set { if (value == text) { return; }
				text = value;
				OnPropertyChanged("Text");
			}
		}

        [DataMember(Name="category"), IgnoreDataMember]
        public string Category { get; set; }

        [DataMember(Name = "id"), IgnoreDataMember]
        public string Id { get; set; }

        [DataMember(Name = "count"), IgnoreDataMember]
		public long Count { get; set; }

		public override string ToString() {
			return text;
		}
	}
}
