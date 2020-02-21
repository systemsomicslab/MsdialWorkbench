using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class Tag : ObservableObject {

		private string _text = "";

		public Tag(string text) {
			this.text = text;
		}

		public Tag() { }

		[DataMember(Name = "ruleBased"), IgnoreDataMember]
		public bool ruleBased { get; set; }
		[DataMember(Name = "text"), Required]
		public string text {
			get { return _text; }
			set { if (value == _text) { return; }
				_text = value;
				OnPropertyChanged("text");
			}
		}

        public override bool Equals(object obj) {
            // If parameter is null return false.
            if (obj == null) {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Tag p = obj as Tag;
            if ((System.Object)p == null) {
                return false;
            }

            // Return true if the fields match:
            return (text == p.text);
        }

        public override string ToString() {
			return text;
		}

        public override int GetHashCode() {
            return text.GetHashCode();
        }
    }
}
