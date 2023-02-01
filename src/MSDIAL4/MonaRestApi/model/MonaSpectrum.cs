using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
	[DataContract]
	public class MonaSpectrum : ObservableObject {
		private string _spectrum = string.Empty;
		private Splash _splash = new Splash();
		private Submitter _submitter = new Submitter();
		private Library _library = new Library();
		private ObservableCollection<MetaData> _metaData = new ObservableCollection<MetaData>();
		private ObservableCollection<Compound> _compounds = new ObservableCollection<Compound>();
		private ObservableCollection<Tag> _tags = new ObservableCollection<Tag>();

        public MonaSpectrum() {
            compounds.CollectionChanged += Compounds_CollectionChanged;
            metaData.CollectionChanged += MetaData_CollectionChanged;
        }

        private void Compounds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            OnPropertyChanged("compounds");
        }

        private void MetaData_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            OnPropertyChanged("metaData");
        }

        [DataMember(Name = "spectrum")]
		public string spectrum {
			get { return _spectrum; }
			set {
				if (value == _spectrum) { return; }
				_spectrum = value;
				OnPropertyChanged("spectrum");
			}
		}
		[DataMember(Name = "splash")]
		public Splash splash {
			get { return _splash; }
			set {
				if (value == _splash) { return; }
				_splash = value;
				OnPropertyChanged("splash");
			}
		}
		[DataMember(Name = "submitter")]
		public Submitter submitter {
			get { return _submitter; }
			set {
				if (value == _submitter) { return; }
				_submitter = value;
				OnPropertyChanged("submitter");
			}
		}
		[DataMember(Name = "compound")]
		public ObservableCollection<Compound> compounds {
			get { return _compounds; }
			set {
				if (value == _compounds) { return; }
                compounds.CollectionChanged -= Compounds_CollectionChanged;
                _compounds = value;
                compounds.CollectionChanged += Compounds_CollectionChanged;
                OnPropertyChanged("compounds");
			}
		}
		[DataMember(Name = "metaData")]
		public ObservableCollection<MetaData> metaData {
			get { return _metaData; }
			set {
				if (value == _metaData) { return; }
				_metaData = value;
				OnPropertyChanged("metaData");
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

        public void AddSpecie(string value) {
            foreach(MetaData md in metaData) {
                if (md.Name.ToLower().Equals("specie")) {
                    md.Value = value;
                    return;
                }
            }
            metaData.Add(new MetaData() { Name = "specie", Value = value });
        }

        public void AddOrgan(string value) {
            foreach (MetaData md in metaData) {
                if (md.Name.ToLower().Equals("organ")) {
                    md.Value = value;
                    return;
                }
            }
            metaData.Add(new MetaData() { Name = "organ", Value = value });
        }

        //[DataMember(Name = "library"), IgnoreDataMember]
        //public Library library { get; set; }
        [DataMember(Name = "id"), IgnoreDataMember]
		public string id { get; set; }
		[DataMember(Name = "lastUpdated"), IgnoreDataMember]
		public string lastUpdated { get; set; }
		[DataMember(Name = "score"), IgnoreDataMember]
		public Score score { get; set; }

		public override string ToString() {
			return string.Format("Splash: {0}\nSubmitter: {2}\nMetadata:\n\t{3},\nTags:\n\t{4}\nPeak list:\n\t[{1}]\nCompound:\n\t{5}", splash, spectrum, submitter, string.Join("\n\t", metaData), string.Join("\n\t", tags), string.Join("\n\t", compounds));
		}

        public bool ContainsMetaDataName(string name) {
            foreach(MetaData md in metaData) {
                if (md.Name.Equals(name))
                    return true;
            }
            return false;
        }
	}
}
