using Microsoft.Win32;
using Msdial.Lcms.Dataprocess.Algorithm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv {

    public class AdductDiffVM : ViewModelBase {
        private AdductQuery posAdduct;
        private AdductQuery negAdduct;
        private double diff;

        public AdductDiffVM(ObservableCollection<AdductQuery> posAdductQueries, ObservableCollection<AdductQuery> negAdductQueries, 
            int selectedPosAdductID, int selectedNegAdductID) {

            if (selectedPosAdductID > 0 && selectedNegAdductID > 0 && 
                selectedPosAdductID < posAdductQueries.Count - 1 && selectedNegAdductID < negAdductQueries.Count - 1) {

                this.posAdduct = posAdductQueries[selectedPosAdductID];
                this.negAdduct = negAdductQueries[selectedNegAdductID];

                var selectedPosAdductString = posAdductQueries[selectedPosAdductID].AdductName;
                var selectedNegAdductString = negAdductQueries[selectedNegAdductID].AdductName;

                var posAdductBean = AdductIonParcer.GetAdductIonBean(selectedPosAdductString);
                var negAdductBean = AdductIonParcer.GetAdductIonBean(selectedNegAdductString);

                this.diff = posAdductBean.AdductIonAccurateMass - negAdductBean.AdductIonAccurateMass;
            }
            else {
                this.posAdduct = null;
                this.negAdduct = null;
                this.diff = 0;
            }
        }

        public AdductDiffVM(ObservableCollection<AdductQuery> posAdductQueries, ObservableCollection<AdductQuery> negAdductQueries, AdductDiff adductDiff) {

            if (adductDiff == null || adductDiff.PosAdduct == null || adductDiff.NegAdduct == null || adductDiff.Diff == 0) return;

            var posAdductString = adductDiff.PosAdduct.AdductIonName;
            var negAdductString = adductDiff.NegAdduct.AdductIonName;

            var posID = -1;
            var negID = -1;
            for (int i = 0; i < posAdductQueries.Count; i++) {
                if (posAdductQueries[i].AdductName == posAdductString) {
                    posID = i + 1;
                    break;
                }
            }

            for (int i = 0; i < negAdductQueries.Count; i++) {
                if (negAdductQueries[i].AdductName == negAdductString) {
                    negID = i + 1;
                    break;
                }
            }


            if (posID > 0 && negID > 0 &&
                posID < posAdductQueries.Count - 1 && negID < negAdductQueries.Count - 1) {

                this.posAdduct = posAdductQueries[posID];
                this.negAdduct = negAdductQueries[negID];

                var selectedPosAdductString = posAdductQueries[posID].AdductName;
                var selectedNegAdductString = negAdductQueries[negID].AdductName;

                var posAdductBean = AdductIonParcer.GetAdductIonBean(selectedPosAdductString);
                var negAdductBean = AdductIonParcer.GetAdductIonBean(selectedNegAdductString);

                this.diff = posAdductBean.AdductIonAccurateMass - negAdductBean.AdductIonAccurateMass;
            }
            else {
                return;
            }

         
        }

        public void UpdateVM() {
            OnPropertyChanged("PosAdduct");
            OnPropertyChanged("NegAdduct");
            OnPropertyChanged("Diff");
        }

        private void updateDiff() {
          
            if (posAdduct != null && posAdduct.AdductName != null && posAdduct.AdductName != string.Empty && 
                negAdduct != null && negAdduct.AdductName != null && negAdduct.AdductName != string.Empty) {
                var posAdductBean = AdductIonParcer.GetAdductIonBean(posAdduct.AdductName);
                var negAdductBean = AdductIonParcer.GetAdductIonBean(negAdduct.AdductName);

                this.diff = posAdductBean.AdductIonAccurateMass - negAdductBean.AdductIonAccurateMass;

            }
            else {
                this.diff = 0.0;
            }
            OnPropertyChanged("Diff");

        }

        #region // properties
        public AdductQuery PosAdduct {
            get {
                return posAdduct;
            }

            set {
                posAdduct = value; OnPropertyChanged("PosAdduct");
                updateDiff();
            }
        }

        
        public AdductQuery NegAdduct {
            get {
                return negAdduct;
            }

            set {
                negAdduct = value; OnPropertyChanged("NegAdduct");
                updateDiff();
            }
        }

        public double Diff {
            get {
                return diff;
            }

            set {
                diff = value; OnPropertyChanged("Diff");
            }
        }
        #endregion
    }

    public class AdductQuery : ViewModelBase {
        private string adductName;
        private int adductID;

        public string AdductName {
            get {
                return adductName;
            }

            set {
                adductName = value; OnPropertyChanged("AdductName");
            }
        }

        public int AdductID {
            get {
                return adductID;
            }

            set {
                adductID = value; OnPropertyChanged("AdductID");
            }
        }
    }

    public class PosNegAmalgamatorSetVM : ViewModelBase {
        private ObservableCollection<AdductDiffVM> adductDiffVMs;
        private ObservableCollection<AdductQuery> posAdductQueries;
        private ObservableCollection<AdductQuery> negAdductQueries;
        private AnalysisParametersBean param;
        private MainWindow mainWindow;

        private float rtTolerance;
        private float mzTolerance;

        private bool isPeakSpots;
        private bool isAlignmentSpots;

        private string diffentPolarityPeaklistFile;

        /// <summary>
        /// Sets up the view model for the FragmentQuerySet window
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }

        private void Window_Loaded(object obj) {
            var view = (PosNegAmalgamatorSetWin)obj;
            this.mainWindow = (MainWindow)view.Owner;
            this.param = this.mainWindow.AnalysisParamForLC;
            this.rtTolerance = this.param.RetentionTimeAlignmentTolerance;
            this.mzTolerance = this.param.Ms1AlignmentTolerance;

            var posAdductlist = new ObservableCollection<string>() {
                "", "[M+H]+", "[M+NH4]+", "[M+Na]+", "[M+CH3OH+H]+", "[M+K]+", "[M+Li]+", "[M+ACN+H]+", "[M+H-H2O]+", "[M+H-2H2O]+", "[M+2Na-H]+", "[M+IsoProp+H]+",
                "[M+ACN+Na]+", "[M+2K-H]+", "[M+DMSO+H]+", "[M+2ACN+H]+","[M+IsoProp+Na+H]+", "[M-C6H10O4+H]+", "[M-C6H10O5+H]+","[M-C6H8O6+H]+"
            };
            var negAdductlist = new ObservableCollection<string>() {
                 "", "[M-H]-", "[M-H2O-H]-", "[M+Na-2H]-", "[M+Cl]-", "[M+K-2H]-", "[M+FA-H]-", "[M+Hac-H]-", "[M+Br]-", "[M+TFA-H]-", "[M-C6H10O4-H]-", "[M-C6H10O5-H]-", "[M-C6H8O6-H]-"
            };

            this.posAdductQueries = new ObservableCollection<AdductQuery>();
            for (int i = 0; i < posAdductlist.Count; i++) {
                var query = new AdductQuery() { AdductID = i, AdductName = posAdductlist[i] };
                this.posAdductQueries.Add(query);
            }

            this.negAdductQueries = new ObservableCollection<AdductQuery>();
            for (int i = 0; i < negAdductlist.Count; i++) {
                var query = new AdductQuery() { AdductID = i, AdductName = negAdductlist[i] };
                this.negAdductQueries.Add(query);
            }

            if (this.mainWindow.TabItem_RtMzPairwisePlotAlignmentView.IsSelected)
                this.IsAlignmentSpots = true;
            else
                this.IsPeakSpots = true;

            var counter = 0;
            this.adductDiffVMs = new ObservableCollection<AdductDiffVM>();
            if (this.param.AddictDiffQueries != null && this.param.AddictDiffQueries.Count > 0) {
                for (int i = 0; i < this.param.AddictDiffQueries.Count; i++) {
                    var adductdiffVM = new AdductDiffVM(this.posAdductQueries, this.negAdductQueries, this.param.AddictDiffQueries[i]);
                    if (adductdiffVM.Diff == 0) continue;
                    this.adductDiffVMs.Add(adductdiffVM);
                    counter++;
                }
            }
            for (int i = counter; i < 100; i++) {
                if (i == 0)
                    this.adductDiffVMs.Add(new AdductDiffVM(this.posAdductQueries, this.negAdductQueries, 1, 1));
                else
                    this.adductDiffVMs.Add(new AdductDiffVM(this.posAdductQueries, this.negAdductQueries, 0, 0));
            }

            updateVM();
        }

        /// <summary>
		/// Opens the file select browser to import the peak list of differnt ion mode
		/// </summary>
		private DelegateCommand browse;
        public DelegateCommand Browse {
            get {
                return browse ?? (browse = new DelegateCommand(obj => {

                    var ofd = new OpenFileDialog();
                    ofd.Filter = "Text file(*.txt)|*.txt;";
                    ofd.Title = "Import a peak list of different polarity";
                    ofd.RestoreDirectory = true;
                    ofd.Multiselect = false;

                    if (ofd.ShowDialog() == true) {
                        this.DiffentPolarityPeaklistFile = ofd.FileName;
                    }
                    OnPropertyChanged("DiffentPolarityPeaklistFile");

                }, obj => { return true; }));
            }
        }

        /// <summary>
        /// Run
        /// </summary>
        private DelegateCommand run;
        public DelegateCommand Run {
            get {
                return run ?? (run = new DelegateCommand(winobj => {

                    var view = (PosNegAmalgamatorSetWin)winobj;

                    if (!System.IO.File.Exists(this.diffentPolarityPeaklistFile)) {
                        MessageBox.Show("Your imported file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var queries = this.adductDiffVMs;
                    var cQueries = new List<AdductDiff>();

                    foreach (var query in queries) {
                        if (query.Diff == 0) continue;
                        if (query.NegAdduct == null || query.NegAdduct.AdductName == null || query.NegAdduct.AdductName == string.Empty) continue;
                        if (query.PosAdduct == null || query.PosAdduct.AdductName == null || query.PosAdduct.AdductName == string.Empty) continue;

                        var posAdduct = AdductIonParcer.GetAdductIonBean(query.PosAdduct.AdductName);
                        var negAdduct = AdductIonParcer.GetAdductIonBean(query.NegAdduct.AdductName);
                        cQueries.Add(new AdductDiff(posAdduct, negAdduct));
                    }

                    if (cQueries.Count == 0) {
                        MessageBox.Show("Enter at least one query", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    this.param.AddictDiffQueries = new List<AdductDiff>();
                    foreach (var query in cQueries) {
                        this.param.AddictDiffQueries.Add(query);
                    }

                    Mouse.OverrideCursor = Cursors.Wait;
                    if (this.isPeakSpots) {
                        var fileid = this.mainWindow.FocusedFileID;
                        var peakSpots = this.mainWindow.AnalysisFiles[fileid].PeakAreaBeanCollection;
                        PosNegAmalgamator.AmalgamateDifferentPolarityDataListToDetectedSpots(new List<PeakAreaBean>(peakSpots), diffentPolarityPeaklistFile, cQueries, this.rtTolerance, this.mzTolerance, this.mainWindow.ProjectProperty.IonMode);

                        //int focusedFileID = mainWindow.FocusedFileID;
                        //int focusedPeakID = mainWindow.FocusedPeakID;

                        //mainWindow.PeakViewDataAccessRefresh();
                        //mainWindow.IsEnabled = false;

                        //PosNegAmalgamator.AmalgamatorPrivateTest(mainWindow.ProjectProperty, mainWindow.AnalysisFiles, mainWindow.AnalysisParamForLC);

                        //mainWindow.PeakViewerForLcRefresh(focusedFileID);
                        //((PairwisePlotPeakViewUI)mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;
                        //mainWindow.IsEnabled = true;
                    }
                    else if (this.isAlignmentSpots) {
                        var alignedSpots = this.mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;
                        PosNegAmalgamator.AmalgamateDifferentPolarityDataListToDetectedSpots(new List<AlignmentPropertyBean>(alignedSpots), diffentPolarityPeaklistFile, cQueries, this.rtTolerance, this.mzTolerance, this.mainWindow.ProjectProperty.IonMode);
                    }
                    Mouse.OverrideCursor = null;
                    view.Close();
                }, CanRun));
            }
        }

        /// <summary>
        /// Checks whether the fragment search command can be executed or not
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CanRun(object obj) {

            if (this.mainWindow == null) return true;

            var isSelectedFile = this.mainWindow.FocusedFileID;
            var isSelectedAlignmentFile = this.mainWindow.FocusedAlignmentFileID;

            if (this.isPeakSpots && isSelectedFile < 0) return false;
            if (this.isAlignmentSpots && isSelectedAlignmentFile < 0) return false;

            if (this.diffentPolarityPeaklistFile == null || this.diffentPolarityPeaklistFile == string.Empty) return false;

            return true;
        }

        /// <summary>
        /// Closes the window (on Cancel)
        /// </summary>
        private DelegateCommand cancel;
        public DelegateCommand Cancel {
            get {
                return cancel ?? (cancel = new DelegateCommand(obj => {
                    Window view = (Window)obj;
                    view.Close();
                }, obj => { return true; }));
            }
        }

        private void updateVM() {

            OnPropertyChanged("IsPeakSpots");
            OnPropertyChanged("IsAlignmentSpots");
            OnPropertyChanged("AdductDiffVMs");
            OnPropertyChanged("PosAdductQueries");
            OnPropertyChanged("NegAdductQueries");
            OnPropertyChanged("RtTolerance");
            OnPropertyChanged("MzTolerance");
            OnPropertyChanged("DiffentPolarityPeaklistFile");
        }

        public ObservableCollection<AdductQuery> PosAdductQueries {
            get {
                return posAdductQueries;
            }

            set {
                posAdductQueries = value;
                OnPropertyChanged("PosAdductQueries");
            }
        }

        public ObservableCollection<AdductQuery> NegAdductQueries {
            get {
                return negAdductQueries;
            }

            set {
                negAdductQueries = value;
                OnPropertyChanged("NegAdductQueries");
            }
        }

        public bool IsPeakSpots {
            get {
                return isPeakSpots;
            }

            set {
                if (isPeakSpots == value) return;
                isPeakSpots = value;
                OnPropertyChanged("IsPeakSpots");
            }
        }

        public bool IsAlignmentSpots {
            get {
                return isAlignmentSpots;
            }

            set {
                if (isAlignmentSpots == value) return;
                isAlignmentSpots = value;
                OnPropertyChanged("IsAlignmentSpots");
            }
        }

        public ObservableCollection<AdductDiffVM> AdductDiffVMs {
            get {
                return adductDiffVMs;
            }

            set {
                adductDiffVMs = value;
                OnPropertyChanged("AdductDiffVMs");
            }
        }

        public string DiffentPolarityPeaklistFile {
            get {
                return diffentPolarityPeaklistFile;
            }

            set {
                diffentPolarityPeaklistFile = value;
                OnPropertyChanged("DiffentPolarityPeaklistFile");
            }
        }

        public float RtTolerance {
            get {
                return rtTolerance;
            }

            set {
                rtTolerance = value;
                OnPropertyChanged("RtTolerance");
            }
        }

        public float MzTolerance {
            get {
                return mzTolerance;
            }

            set {
                mzTolerance = value;
                OnPropertyChanged("MzTolerance");
            }
        }
    }
}
