using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class MetaData : ObservableObject {
		private string name = "";
		private string value = "";

		[DataMember(Name = "name"), Required(ErrorMessage = "The metadata name is required")]
		public string Name {
			get { return name; }
			set { if (value == null) { value = ""; }
                if (value.ToLower() == name) { return; }
				name = value.ToLower();
				OnPropertyChanged("Name");
			}
		}
		[DataMember(Name = "value"), Required(ErrorMessage = "The value for the metadata is required")]
		public string Value {
			get { return value; }
			set {
				if (value == this.value) { return; }
                this.value = value.ToString();
				OnPropertyChanged("Value");
			}
		}
		[DataMember(Name = "category")]
		public string Category { get; set; }
		[DataMember(Name = "computed"), IgnoreDataMember]
		public bool Computed { get; set; }
		[DataMember(Name = "hidden"), IgnoreDataMember]
		public bool Hidden { get; set; }
		[DataMember(Name = "url"), IgnoreDataMember]
		public string Url { get; set; }

		public override string ToString() {
			return string.Format("{0}: {1}", Name, Value as string);
		}

        public override bool Equals(object obj) {
            // If parameter is null return false.
            if (obj == null) {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            MetaData p = obj as MetaData;
            if (p == null) { return false; }

            // Return true if the fields match:
            return (Name == p.Name) && (Value == p.Value);
        }

        public override int GetHashCode() {
            return (Name + Value + Url).GetHashCode();
        }
    }
}
