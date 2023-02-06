using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.MsfinderCommon.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Rfx.Riken.OsakaUniv {
    public class MolecularNetworkingSettingVM : ViewModelBase {
        private MsfinderQueryStorage msfinderQueryStorage;
        private MainWindow mainWindow;

        private bool isSelectedFile;
        private bool isAllFiles;
        private bool isOntologyNetwork;
        private bool isBioreactionNetwork;

        private double massTolerance;
        private double relativeAbundanceCutoff;
        private double msmsSimilarityCutoff;
        private double rtTolerance;
        private double ontologySimilarityCutoff;
        private double retentionTimeToleranceForReaction;

        #region // property
        public bool IsSelectedFile {
            get {
                return isSelectedFile;
            }

            set {
                isSelectedFile = value;
                OnPropertyChanged("IsSelectedFile");
            }
        }

        public bool IsAllFiles {
            get {
                return isAllFiles;
            }

            set {
                isAllFiles = value;
                OnPropertyChanged("IsAllFiles");
            }
        }

        public bool IsOntologyNetwork {
            get {
                return isOntologyNetwork;
            }

            set {
                isOntologyNetwork = value;
                OnPropertyChanged("IsOntologyNetwork");
            }
        }

        public bool IsBioreactionNetwork {
            get {
                return isBioreactionNetwork;
            }

            set {
                isBioreactionNetwork = value;
                OnPropertyChanged("IsBioreactionNetwork");
            }
        }

        public double MassTolerance {
            get {
                return massTolerance;
            }

            set {
                massTolerance = value;
                OnPropertyChanged("MassTolerance");
            }
        }

        public double RelativeAbundanceCutoff {
            get {
                return relativeAbundanceCutoff;
            }

            set {
                relativeAbundanceCutoff = value;
                OnPropertyChanged("RelativeAbundanceCutoff");
            }
        }

        public double MsmsSimilarityCutoff {
            get {
                return msmsSimilarityCutoff;
            }

            set {
                msmsSimilarityCutoff = value;
                OnPropertyChanged("MsmsSimilarityCutoff");
            }
        }

        public double RtTolerance {
            get {
                return rtTolerance;
            }

            set {
                rtTolerance = value;
                OnPropertyChanged("RtTolerance");
            }
        }

        public double OntologySimilarityCutoff {
            get {
                return ontologySimilarityCutoff;
            }

            set {
                ontologySimilarityCutoff = value;
                OnPropertyChanged("OntologySimilarityCutoff");
            }
        }

        public double RetentionTimeToleranceForReaction {
            get {
                return retentionTimeToleranceForReaction;
            }

            set {
                retentionTimeToleranceForReaction = value;
                OnPropertyChanged("RetentionTimeToleranceForReaction");
            }
        }
        #endregion

        /// <summary>
        /// Sets up the view model for the MolecularNetworkSettingWin window
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }

        private void Window_Loaded(object obj) {
            var view = (MolecularNetworkingSettingWin)obj;
            this.mainWindow = (MainWindow)view.Owner;
            this.msfinderQueryStorage = this.mainWindow.MainWindowVM.DataStorageBean;

            var param = this.msfinderQueryStorage.AnalysisParameter;
            if (param.MmnMassTolerance < 0.000001) {
                this.IsAllFiles = true;
                this.MassTolerance = 0.025;
                this.RelativeAbundanceCutoff = 1;
                this.MsmsSimilarityCutoff = 90;
                this.RtTolerance = 100;
                this.OntologySimilarityCutoff = 95;
                this.RetentionTimeToleranceForReaction = 0.5;

                this.IsOntologyNetwork = true;
                this.IsBioreactionNetwork = true;
            }
            else {
                if (param.IsMmnSelectedFileCentricProcess)
                    this.IsSelectedFile = true;
                else
                    this.IsAllFiles = true;

                this.MassTolerance = param.MmnMassTolerance;
                this.RelativeAbundanceCutoff = param.MmnRelativeCutoff;
                this.MsmsSimilarityCutoff = param.MmnMassSimilarityCutOff;
                this.RtTolerance = param.MmnRtTolerance;
                this.OntologySimilarityCutoff = param.MmnOntologySimilarityCutOff;
                this.RetentionTimeToleranceForReaction = param.MmnRtToleranceForReaction;

                this.IsOntologyNetwork = param.IsMmnOntologySimilarityUsed;
                this.IsBioreactionNetwork = param.IsMmnFormulaBioreaction;
            }

            OnPropertyChanged("MolecularNetwokringSetView");
        }

        /// <summary>
        /// Searching fragment queries
        /// </summary>
        private DelegateCommand run;
        public DelegateCommand Run {
            get {
                return run ?? (run = new DelegateCommand(winobj => {

                    var view = (MolecularNetworkingSettingWin)winobj;
                    var param = this.msfinderQueryStorage.AnalysisParameter;

                    // checking formula reaction db exist
                    var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var appDirectory = System.IO.Path.GetDirectoryName(appPath);
                    var reactionFiles = System.IO.Directory.GetFiles(Path.Combine(appDirectory, "Resources"), "*.fbt", System.IO.SearchOption.TopDirectoryOnly);

                    if (this.IsOntologyNetwork == true && (reactionFiles == null || reactionFiles.Length == 0)) {
                        MessageBox.Show("No formula reaction file. Please put *.fbt file on Resources folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // file save
                    param.IsMmnSelectedFileCentricProcess = this.IsSelectedFile;
                    param.MmnMassTolerance = this.MassTolerance;
                    param.MmnRelativeCutoff = this.RelativeAbundanceCutoff;
                    param.MmnMassSimilarityCutOff = this.MsmsSimilarityCutoff;
                    param.MmnRtTolerance = this.RtTolerance;
                    param.MmnOntologySimilarityCutOff = this.OntologySimilarityCutoff;
                    param.MmnRtToleranceForReaction = this.RetentionTimeToleranceForReaction;

                    param.IsMmnOntologySimilarityUsed = this.IsOntologyNetwork;
                    param.IsMmnFormulaBioreaction = this.IsBioreactionNetwork;

                    MsFinderIniParcer.Write(param);

                    new MsfinderMolecularNetworking().CytoscapeVisualizationProcess(this.mainWindow, this.msfinderQueryStorage);
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
    }
}
