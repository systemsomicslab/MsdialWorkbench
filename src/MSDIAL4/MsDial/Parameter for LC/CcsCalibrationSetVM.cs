using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Rfx.Riken.OsakaUniv {

    public class CcsCalibrationInfoVS : ViewModelBase {
        private string filepath;
        private string filename;
        private int fileid;
        private double agilentBeta;
        private double agilentTFix;
        private double watersCoefficient;
        private double watersT0;
        private double watersExponent;

        public string Filepath {
            get {
                return filepath;
            }

            set { if (filepath == value) return; filepath = value; OnPropertyChanged("Filepath"); }
        }

        public int Fileid {
            get {
                return fileid;
            }

            set { if (fileid == value) return; fileid = value; OnPropertyChanged("Fileid"); }
        }


        public string Filename {
            get {
                return filename;
            }

            set { if (filename == value) return; filename = value; OnPropertyChanged("Filename"); }
        }

        public double AgilentBeta {
            get {
                return agilentBeta;
            }

            set { if (agilentBeta == value) return; agilentBeta = value; OnPropertyChanged("AgilentBeta"); }
        }

        public double AgilentTFix {
            get {
                return agilentTFix;
            }

            set { if (agilentTFix == value) return; agilentTFix = value; OnPropertyChanged("AgilentTFix"); }
        }

        public double WatersCoefficient {
            get {
                return watersCoefficient;
            }

            set { if (watersCoefficient == value) return; watersCoefficient = value; OnPropertyChanged("WatersCoefficient"); }
        }

        public double WatersT0 {
            get {
                return watersT0;
            }

            set { if (watersT0 == value) return; watersT0 = value; OnPropertyChanged("WatersT0"); }
        }

        public double WatersExponent {
            get {
                return watersExponent;
            }

            set { if (watersExponent == value) return; watersExponent = value; OnPropertyChanged("WatersExponent"); }
        }

    }

    public class CcsCalibrationSetVM : ViewModelBase {

        private AnalysisParametersBean param;
        private ObservableCollection<CcsCalibrationInfoVS> ccsCalibrationInfoVSs;
        private AnalysisParamSetForLcWin win;

        public ObservableCollection<CcsCalibrationInfoVS> CcsCalibrationInfoVSs {
            get { return ccsCalibrationInfoVSs; }
            set { ccsCalibrationInfoVSs = value; OnPropertyChanged("CcsCalibrationInfoVSs"); }
        }

        public AnalysisParametersBean Param {
            get { return param; }
            set { param = value; }
        }

        /// <summary>
        /// Sets up the view model in InvokeCommandAction
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }

        /// <summary>
        /// Action for the WindowLoaded command
        /// </summary>
        /// <param name="obj"></param>
        private void Window_Loaded(object obj) {

            MainWindow mainWindow;
            if (obj.GetType() == typeof(AgilentCcsCalibrationSetWin)) {
                var view = (AgilentCcsCalibrationSetWin)obj;
                this.win = (AnalysisParamSetForLcWin)view.Owner;
            }
            else {
                var view = (WatersCcsCalibrationSetWin)obj;
                this.win = (AnalysisParamSetForLcWin)view.Owner;
            }
            mainWindow = (MainWindow)this.win.Owner;

            Param = mainWindow.AnalysisParamForLC;
            var files = mainWindow.AnalysisFiles;
            CcsCalibrationInfoVSs = new ObservableCollection<CcsCalibrationInfoVS>();
            foreach (var file in files) {
                var fileprop = file.AnalysisFilePropertyBean;
                var fileid = fileprop.AnalysisFileId;
                var calinfo = Param.FileidToCcsCalibrantData[fileid];
                var vs = new CcsCalibrationInfoVS() {
                    Filename = fileprop.AnalysisFileName, Fileid = fileid, Filepath = fileprop.AnalysisFilePath,
                    AgilentBeta = calinfo.AgilentBeta, AgilentTFix = calinfo.AgilentTFix, WatersCoefficient = calinfo.WatersCoefficient,
                    WatersExponent = calinfo.WatersExponent, WatersT0 = calinfo.WatersT0
                };
                CcsCalibrationInfoVSs.Add(vs);
            }
        }

        /// <summary>
        /// Closes the window (on Cancel)
        /// </summary>
        private DelegateCommand closeWindow;
        public DelegateCommand CloseWindow {
            get {
                return closeWindow ?? (closeWindow = new DelegateCommand(obj => {
                    Window view = (Window)obj;
                    view.Close();
                }, obj => { return true; }));
            }
        }

        /// <summary>
        /// Set Ri dictionary
        /// </summary>
        private DelegateCommand calibrantSet;
        public DelegateCommand CalibrantSet {
            get {
                return calibrantSet ?? (calibrantSet = new DelegateCommand(executeCalibrantSet, canExcecuteCalibrantSet));
            }
        }

        private void executeCalibrantSet(object obj) {

            var mobilitytype = this.param.IonMobilityType;
            var isAllDataExist = true;

            foreach (var vs in this.ccsCalibrationInfoVSs) {
                if (mobilitytype == IonMobilityType.Dtims) {
                    if (vs.AgilentBeta == -1 || vs.AgilentTFix == -1) {
                        isAllDataExist = false;
                        break;
                    }
                }
                else {
                    if (vs.WatersCoefficient == -1 || vs.WatersExponent == -1 || vs.WatersT0 == -1) {
                        isAllDataExist = false;
                        break;
                    }
                }
            }

            if (!isAllDataExist) {
                var errorMessage = mobilitytype == IonMobilityType.Dtims
                    ? "For Agilent single fieled-based CCS calculation, you have to set the coefficients for all files. "
                    : "For Waters CCS calculation, you have to set the coefficients for all files. ";
                errorMessage += "Otherwise, the Mason–Schamp equation using gasweight=28.0134 and temperature=305.0 is used for CCS calculation for all data. ";
                errorMessage += "Do you continue the CCS parameter setting?";
                if (MessageBox.Show(errorMessage, "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes) {
                    return;
                }
            }

            foreach (var vs in this.ccsCalibrationInfoVSs) {
                var fileidToCcsCalInfo = this.param.FileidToCcsCalibrantData;
                if (mobilitytype == IonMobilityType.Dtims) {
                    fileidToCcsCalInfo[vs.Fileid].AgilentBeta = vs.AgilentBeta;
                    fileidToCcsCalInfo[vs.Fileid].AgilentTFix = vs.AgilentTFix;
                }
                else {
                    fileidToCcsCalInfo[vs.Fileid].WatersCoefficient = vs.WatersCoefficient;
                    fileidToCcsCalInfo[vs.Fileid].WatersExponent = vs.WatersExponent;
                    fileidToCcsCalInfo[vs.Fileid].WatersT0 = vs.WatersT0;
                }
            }


            if (isAllDataExist) {
                this.param.IsAllCalibrantDataImported = true;
                this.win.Label_CcsCalibrantImport.Content = "Status: imported";
            }
            else {
                this.param.IsAllCalibrantDataImported = false;
                this.win.Label_CcsCalibrantImport.Content = "Status: not imported yet";
            }

            if (obj.GetType() == typeof(AgilentCcsCalibrationSetWin)) {
                var view = (AgilentCcsCalibrationSetWin)obj;
                view.Close();
            }
            else {
                var view = (WatersCcsCalibrationSetWin)obj;
                view.Close();
            }
        }

        private bool canExcecuteCalibrantSet(object arg) {
            return true;
        }
    }
}
