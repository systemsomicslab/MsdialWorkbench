using System;
using edu.ucdavis.fiehnlab.MonaRestApi.model;
using System.Diagnostics;
using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Collections.ObjectModel;

namespace edu.ucdavis.fiehnlab.MonaExport.DataObjects {
    /// <summary>
    /// this class holds the data to be exported to mona.
    /// its data is added to the mona entries when they are exported.
    /// </summary>
    [Serializable]
	public class MonaProjectData : ObservableObject {
		#region members
		private ObservableCollection<MonaSpectrum> spectra;
		private Submitter submitter;

		private string fileName;
		#endregion

		#region Properties
		public ObservableCollection<MonaSpectrum> Spectra {
			get { return spectra; }
			set {
				if (spectra == value) return;
				spectra = value;
				//OnPropertyChanged();
			}
		}

		public Submitter Submitter {
			get { return submitter; }
			set {
				if (submitter == value) return;
				submitter = value;
				//OnPropertyChanged();
			}
		}
		#endregion

		public MonaProjectData() {
			fileName = "MonaProject_" + DateTime.Now;
			Debug.WriteLine("filename: " + fileName);
			Spectra = new ObservableCollection<MonaSpectrum>();
			Submitter = new Submitter();
        }

		public void saveProject() {
			throw new NotImplementedException();
		}

		public void loadProject(string fileName) {
			throw new NotImplementedException();
		}

		public override string ToString() {
			return string.Format("Spectra:\n{0}", string.Join("\n\t", Spectra));
		}
	}
}
