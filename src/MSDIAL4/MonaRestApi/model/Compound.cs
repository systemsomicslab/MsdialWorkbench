using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class Compound : ObservableObject {
		private string _inchi = string.Empty;
		private string _inchiKey = string.Empty;
		private string _molFile = string.Empty;
		private ObservableCollection<MetaData> _metaData = new ObservableCollection<MetaData>();
		private ObservableCollection<Name> _names = new ObservableCollection<Name>();
		private ObservableCollection<Tag> _tags = new ObservableCollection<Tag>();

        public Compound() {
            _metaData.CollectionChanged += MetaData_CollectionChanged;
        }

		[DataMember(Name = "inchi")]
		public string inchi {
			get { return _inchi; }
			set {
				if (value == _inchi) { return; }
				_inchi = value;
				OnPropertyChanged("inchi");
			}
		}
		[DataMember(Name = "inchiKey")]
		public string inchiKey {
			get { return _inchiKey; }
			set {
				if(value == _inchiKey) { return; }
				_inchiKey = value;
				OnPropertyChanged("inchiKey");
			}
		}
		[DataMember(Name = "metaData")]
		public ObservableCollection<MetaData> metaData {
			get { return _metaData; }
			set { if (value == _metaData) { return; }
                _metaData.CollectionChanged -= MetaData_CollectionChanged;
                _metaData = value;
                _metaData.CollectionChanged += MetaData_CollectionChanged;
                OnPropertyChanged("metaData");
			}
		}
		[DataMember(Name = "molFile")]
		public string molFile {
			get { return _molFile; }
			set {
				if (value == _molFile) { return; }
				_molFile = value;
				OnPropertyChanged("molFile");
			}
		}
		[DataMember(Name = "names")]
		public ObservableCollection<Name> names {
			get { return _names; }
			set {
				if (value == _names) { return; }
				_names = value;
				OnPropertyChanged("names");
			}
		}
		[DataMember(Name = "tags")]
		public ObservableCollection<Tag> tags {
			get { return _tags; }
			set {
				if (value == _tags) { return; }
				_tags = value;
				OnPropertyChanged("tags");
			}
		}
		[DataMember(Name = "computed"), IgnoreDataMember]
		public bool computed { get; set; }

		public override string ToString() {
			return string.Format("InChiKey: {0}\nInChi Code: {1}\nNames ({5}):\n\t{2}\nMetadata ({6}):\n\t{3}\nTags ({7}):\n\t{4}", 
				inchiKey, inchi, 
				string.Join("\n\t", names), string.Join("\n\t", metaData), string.Join("\n\t", tags), 
				names.Count, metaData.Count, tags.Count);
		}

        public void AddIsStandardMetadata() {
            MetaData stdmd = new MetaData() { Name = "kind", Value = "standard" };
            if (!metaData.Contains(stdmd)) {
                metaData.Add(stdmd);
            }
        }

        public void RemoveIsStandardMetadata() {
            MetaData stdmd = new MetaData() { Name = "kind", Value = "standard" };
            if (metaData.Contains(stdmd)) {
                metaData.Remove(stdmd);
            }
        }

        void MetaData_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            OnPropertyChanged("metaData");
        }
    }
}
