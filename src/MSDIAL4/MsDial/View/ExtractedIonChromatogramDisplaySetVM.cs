using CompMs.Common.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class EicDisplayQueryVM : ViewModelBase {
        private string eicName;
        private double? exactMass;
        private double? massTolerance;

        public EicDisplayQueryVM() {
            this.eicName = string.Empty;
        }

        public EicDisplayQueryVM(ExtractedIonChromatogramDisplaySettingBean query) {
            this.eicName = query.EicName;
            this.exactMass = query.ExactMass;
            this.massTolerance = query.MassTolerance;
        }

        public string EicName {
            get { return eicName; }
            set { if (eicName == value) return; eicName = value; OnPropertyChanged("EicName"); }
        }

        public double? ExactMass {
            get { return exactMass; }
            set {
                if (exactMass == value) return;
                exactMass = value;
                OnPropertyChanged("ExactMass");
            }
        }

        public double? MassTolerance {
            get { return massTolerance; }
            set {
                if (massTolerance == value) return;
                massTolerance = value;
                OnPropertyChanged("MassTolerance");
            }
        }
    }



	public class ExtractedIonChromatogramDisplaySetVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;
        private ObservableCollection<EicDisplayQueryVM> eicDisplaySettingBeanCollection;
        private ChromatogramTicEicViewModel chromatogramTicEicViewModel;

        private ObservableCollection<RawSpectrum> lcmsSpectrumCollection;
        private List<RawSpectrum> gcmsSpectrumList;
        private ProjectPropertyBean projectPropertyBean;
        private List<SolidColorBrush> solidColorBrushList;
        
        private AnalysisParametersBean paramLC;
        private AnalysisParamOfMsdialGcms paramGC;
        private AnalysisFileBean analysisFileBean;
        
        private bool eicFrag;
        private bool bpcFrag;

        public ExtractedIonChromatogramDisplaySetVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.eicFrag = true; this.bpcFrag = false;
            this.eicDisplaySettingBeanCollection = new ObservableCollection<EicDisplayQueryVM>();

            for (int i = 0; i < 100; i++) this.eicDisplaySettingBeanCollection.Add(new EicDisplayQueryVM());

            this.window = window;
            this.analysisFileBean = mainWindow.AnalysisFiles[mainWindow.FocusedFileID];
            this.paramLC = mainWindow.AnalysisParamForLC;
            this.paramGC = mainWindow.AnalysisParamForGC;
            this.projectPropertyBean = mainWindow.ProjectProperty;
            this.gcmsSpectrumList = mainWindow.GcmsSpectrumList;
            if (this.projectPropertyBean.SeparationType == SeparationType.IonMobility)
                this.lcmsSpectrumCollection = mainWindow.AccumulatedMs1Specra;
            else
                this.lcmsSpectrumCollection = mainWindow.LcmsSpectrumCollection;

            this.solidColorBrushList = mainWindow.SolidColorBrushList;

            if (this.projectPropertyBean.Ionization == Ionization.ESI) {
                if (this.paramLC.EicDisplayQueries == null) return;
                for (int i = 0; i < 100; i++) {
                    if (this.paramLC.EicDisplayQueries.Count - 1 < i) break;
                    this.eicDisplaySettingBeanCollection[i].EicName = this.paramLC.EicDisplayQueries[i].EicName;
                    this.eicDisplaySettingBeanCollection[i].ExactMass = this.paramLC.EicDisplayQueries[i].ExactMass;
                    this.eicDisplaySettingBeanCollection[i].MassTolerance = this.paramLC.EicDisplayQueries[i].MassTolerance;

                }
            }
            else {
                if (this.paramGC.EicDisplayQueries == null) return;
                for (int i = 0; i < 100; i++) {
                    if (this.paramGC.EicDisplayQueries.Count - 1 < i) break;
                    this.eicDisplaySettingBeanCollection[i].EicName = this.paramGC.EicDisplayQueries[i].EicName;
                    this.eicDisplaySettingBeanCollection[i].ExactMass = this.paramGC.EicDisplayQueries[i].ExactMass;
                    this.eicDisplaySettingBeanCollection[i].MassTolerance = this.paramGC.EicDisplayQueries[i].MassTolerance;
                }
            }

        }


        public ObservableCollection<EicDisplayQueryVM> EicDisplaySettingBeanCollection
        {
            get { return eicDisplaySettingBeanCollection; }
            set { if (eicDisplaySettingBeanCollection == value) return; eicDisplaySettingBeanCollection = value; OnPropertyChanged("EicDisplaySettingBeanCollection"); }
        }


        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            if (ClosingMethod() == true)
            {
                window.Close();
                var displayWindow = new ExtractedIonChromatogramDisplayWin(this.chromatogramTicEicViewModel);
                displayWindow.Owner = this.mainWindow;
                displayWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                displayWindow.Show();
            }
        }

        private bool ClosingMethod()
        {
            var finalEicDisplaySettingBeanCollection = new ObservableCollection<ExtractedIonChromatogramDisplaySettingBean>();
            var fragEIC = true; if (this.bpcFrag == true) fragEIC = false;

            for (int i = 0; i < this.eicDisplaySettingBeanCollection.Count; i++)
            {
                if (this.eicDisplaySettingBeanCollection[i].ExactMass == null || this.eicDisplaySettingBeanCollection[i].MassTolerance == null) continue;
                if (this.eicDisplaySettingBeanCollection[i].ExactMass <= 0 || this.eicDisplaySettingBeanCollection[i].MassTolerance <= 0) continue;
                //finalEicDisplaySettingBeanCollection.Add(this.eicDisplaySettingBeanCollection[i]);
                finalEicDisplaySettingBeanCollection.Add(new ExtractedIonChromatogramDisplaySettingBean() {
                    EicName = this.eicDisplaySettingBeanCollection[i].EicName,
                    ExactMass = this.eicDisplaySettingBeanCollection[i].ExactMass,
                    MassTolerance = this.eicDisplaySettingBeanCollection[i].MassTolerance
                });
                
            }

            if (finalEicDisplaySettingBeanCollection.Count == 0)
            {
                MessageBox.Show("Enter at least one extracted ion information", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            //save
            if (this.projectPropertyBean.Ionization == Ionization.ESI) {
                this.paramLC.EicDisplayQueries = new List<ExtractedIonChromatogramDisplaySettingBean>();
                for (int i = 0; i < this.eicDisplaySettingBeanCollection.Count; i++) {
                    this.paramLC.EicDisplayQueries.Add(new ExtractedIonChromatogramDisplaySettingBean() {
                        EicName = this.eicDisplaySettingBeanCollection[i].EicName,
                        ExactMass = this.eicDisplaySettingBeanCollection[i].ExactMass,
                        MassTolerance = this.eicDisplaySettingBeanCollection[i].MassTolerance
                    });
                }
            }
            else {
                this.paramGC.EicDisplayQueries = new List<ExtractedIonChromatogramDisplaySettingBean>();
                for (int i = 0; i < this.eicDisplaySettingBeanCollection.Count; i++) {
                    this.paramGC.EicDisplayQueries.Add(new ExtractedIonChromatogramDisplaySettingBean() {
                        EicName = this.eicDisplaySettingBeanCollection[i].EicName,
                        ExactMass = this.eicDisplaySettingBeanCollection[i].ExactMass,
                        MassTolerance = this.eicDisplaySettingBeanCollection[i].MassTolerance
                    });
                }
            }

            // Display
            if (this.projectPropertyBean.Ionization == Ionization.ESI)
            {
                if (fragEIC)
                    this.chromatogramTicEicViewModel = UiAccessLcUtility.GetChromatogramEicViewModel(finalEicDisplaySettingBeanCollection, this.projectPropertyBean, this.analysisFileBean, this.paramLC, this.solidColorBrushList, this.lcmsSpectrumCollection);
                else
                    this.chromatogramTicEicViewModel = UiAccessLcUtility.GetChromatogramBpcViewModel(finalEicDisplaySettingBeanCollection, this.projectPropertyBean, this.analysisFileBean, this.paramLC, this.solidColorBrushList, this.lcmsSpectrumCollection);
            }
            else
            {
                if (fragEIC)
                    this.chromatogramTicEicViewModel = UiAccessGcUtility.GetChromatogramEicViewModel(finalEicDisplaySettingBeanCollection, this.analysisFileBean, this.paramGC, this.solidColorBrushList, this.gcmsSpectrumList);
                else
                    this.chromatogramTicEicViewModel = UiAccessGcUtility.GetChromatogramBpcViewModel(finalEicDisplaySettingBeanCollection, this.analysisFileBean, this.paramGC, this.solidColorBrushList, this.gcmsSpectrumList);
            }
            return true;
        }


        public bool EicFrag
        {
            get { return eicFrag; }
            set { eicFrag = value; OnPropertyChanged("EicFrag"); }
        }

        public bool BpcFrag
        {
            get { return bpcFrag; }
            set { bpcFrag = value; OnPropertyChanged("BpcFrag"); }
        }
    }
}
