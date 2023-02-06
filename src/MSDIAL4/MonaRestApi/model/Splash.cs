using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class Splash : ObservableObject {
		private string _splash = "";

		[DataMember(Name = "block1")]
		public string block1 { get; set; }
		[DataMember(Name = "block2")]
		public string block2 { get; set; }
		[DataMember(Name = "block3")]
		public string block3 { get; set; }
		[DataMember(Name = "block4")]
		public string block4 { get; set; }
		[DataMember(Name = "splash")]
		public string splash {
			get { return _splash; }
			set { if (value == _splash) { return; }
				_splash = value;
				OnPropertyChanged("splash");
			}
		}

		public override string ToString() {
			return splash;
		}
	}
}
