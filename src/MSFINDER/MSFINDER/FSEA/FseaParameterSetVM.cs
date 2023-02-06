using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.MsfinderCommon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Rfx.Riken.OsakaUniv {
    public class FseaParameterSetVM : ViewModelBase {
        private MsfinderQueryStorage msfinderQueryStorage;
        private MainWindow mainWindow;

        private double relativeAbundanceCutoff;

        private bool isOntologySpace;
        private bool isReverseSpectrum;
        private bool isLowAbundanceIons;

        private double pvalueCutOff;

        #region
        public double RelativeAbundanceCutoff {
            get {
                return relativeAbundanceCutoff;
            }

            set {
                relativeAbundanceCutoff = value;
                OnPropertyChanged("RelativeAbundanceCutoff");
            }
        }

        public bool IsOntologySpace {
            get {
                return isOntologySpace;
            }

            set {
                isOntologySpace = value;
                OnPropertyChanged("IsOntologySpace");
            }
        }

        public bool IsReverseSpectrum {
            get {
                return isReverseSpectrum;
            }

            set {
                isReverseSpectrum = value;
                OnPropertyChanged("IsReverseSpectrum");
            }
        }

        public bool IsLowAbundanceIons {
            get {
                return isLowAbundanceIons;
            }

            set {
                isLowAbundanceIons = value;
                OnPropertyChanged("IsLowAbundanceIons");
            }
        }

        public double PvalueCutOff {
            get {
                return pvalueCutOff;
            }

            set {
                pvalueCutOff = value;
                OnPropertyChanged("PvalueCutOff");
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
            var view = (FseaParamerSetWin)obj;
            this.mainWindow = (MainWindow)view.Owner;
            this.msfinderQueryStorage = this.mainWindow.MainWindowVM.DataStorageBean;

            var param = this.msfinderQueryStorage.AnalysisParameter;
            if (param.FseaPvalueCutOff < 0.000001) {
                this.RelativeAbundanceCutoff = 5.0;
                this.IsOntologySpace = true;
                this.PvalueCutOff = 5.0;
            }
            else {

                this.RelativeAbundanceCutoff = param.FseaRelativeAbundanceCutOff;
                if (param.FseanonsignificantDef == FseaNonsignificantDef.OntologySpace)
                    this.IsOntologySpace = true;
                else if (param.FseanonsignificantDef == FseaNonsignificantDef.LowAbundantIons)
                    this.IsLowAbundanceIons = true;
                else
                    this.IsReverseSpectrum = true;
                this.PvalueCutOff = param.FseaPvalueCutOff;
            }

            OnPropertyChanged("FseaParameterSetView");
        }

        /// <summary>
        /// Searching fragment queries
        /// </summary>
        private DelegateCommand set;
        public DelegateCommand Set {
            get {
                return set ?? (set = new DelegateCommand(winobj => {

                    var view = (FseaParamerSetWin)winobj;
                    var param = this.msfinderQueryStorage.AnalysisParameter;

                    // file save
                    param.FseaPvalueCutOff = this.PvalueCutOff;
                    param.FseaRelativeAbundanceCutOff = this.RelativeAbundanceCutoff;
                    if (this.IsOntologySpace)
                        param.FseanonsignificantDef = FseaNonsignificantDef.OntologySpace;
                    else if (this.IsLowAbundanceIons)
                        param.FseanonsignificantDef = FseaNonsignificantDef.LowAbundantIons;
                    else
                        param.FseanonsignificantDef = FseaNonsignificantDef.ReverseSpectrum;

                    MsFinderIniParcer.Write(param);

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
