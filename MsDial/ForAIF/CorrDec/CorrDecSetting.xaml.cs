using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv.ForAIF
{
    /// <summary>
    /// CorrDecSetting.xaml の相互作用ロジック
    /// </summary>
    public partial class CorrDecSetting : Window
    {
        public CorrDecSettingVM VM { get; set; }
        public CorrDecSetting()
        {
            InitializeComponent();
        }

        public CorrDecSetting(AnalysisParamOfMsdialCorrDec param, MainWindow mainWindow, AlignmentResultBean alignmentRes, bool isSingle = false) {
            InitializeComponent();
            this.VM = new CorrDecSettingVM(this, param, mainWindow, alignmentRes, isSingle);
            this.DataContext = VM;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }

    public class CorrDecSettingVM: ViewModelBase
    {
        public AnalysisParamOfMsdialCorrDec Param { get; set; }
        private MainWindow window;
        private AlignmentResultBean res;
        private CorrDecSetting settingWindow;
        private bool isSingle;
        public CorrDecSettingVM() { }
        public CorrDecSettingVM(CorrDecSetting setting, AnalysisParamOfMsdialCorrDec param, MainWindow mainWindow, AlignmentResultBean alignmentRes, bool isSingle) {
            settingWindow = setting; Param = param; window = mainWindow; res = alignmentRes;
            this.isSingle = isSingle;
        }

        protected override void executeCommand(object parameter) {
            base.executeCommand(parameter);
            if (isSingle)
            {
                _ = new CorrDecProcess().ProcessSingle(window, res, window.FocusedAlignmentPeakID);
            }
            else {
                _ = new CorrDecProcess().ProcessAll(window, res);
            }
            settingWindow.Close();
        }

        public int MinMS2Intensity {
            get { return Param.MinMS2Intensity; }
            set { if (value == Param.MinMS2Intensity) return;
                Param.MinMS2Intensity = value;
                OnPropertyChanged("MinMS2Intensity");
            }
        }
        public float MS2Tolerance {
            get { return Param.MS2Tolerance; }
            set {
                if (value == Param.MS2Tolerance) return;
                Param.MS2Tolerance = value;
                OnPropertyChanged("MS2Tolerance");
            }
        }

        public float MinCorr_MS1 {
            get { return Param.MinCorr_MS1; }
            set {
                if (value == Param.MinCorr_MS1) return;
                Param.MinCorr_MS1 = value;
                OnPropertyChanged("MinCorr_MS1");
            }
        }

        public float MinCorr_MS2 {
            get { return Param.MinCorr_MS2; }
            set {
                if (value == Param.MinCorr_MS2) return;
                Param.MinCorr_MS2 = value;
                OnPropertyChanged("MinCorr_MS2");
            }
        }

        public float CorrDiff_MS1 {
            get { return Param.CorrDiff_MS1; }
            set {
                if (value == Param.CorrDiff_MS1) return;
                Param.CorrDiff_MS1 = value;
                OnPropertyChanged("CorrDiff_MS1");
            }
        }

        public float CorrDiff_MS2 {
            get { return Param.CorrDiff_MS2; }
            set {
                if (value == Param.CorrDiff_MS2) return;
                Param.CorrDiff_MS2 = value;
                OnPropertyChanged("CorrDiff_MS2");
            }
        }

        public float MinDetectedPercentToVisualize {
            get { return Param.MinDetectedPercentToVisualize; }
            set {
                if (value == Param.MinDetectedPercentToVisualize) return;
                Param.MinDetectedPercentToVisualize = value;
                OnPropertyChanged("MinDetectedPercentToVisualize");
            }
        }

        public bool RemoveAfterPrecursor {
            get { return Param.RemoveAfterPrecursor; }
            set {
                if (value == Param.RemoveAfterPrecursor) return;
                Param.RemoveAfterPrecursor = value;
                OnPropertyChanged("RemoveAfterPrecursor");
            }
        }

        public int MinNumberOfSample {
            get { return Param.MinNumberOfSample; }
            set {
                if (value == Param.MinNumberOfSample) return;
                Param.MinNumberOfSample = value;
                OnPropertyChanged("MinNumberOfSample");
            }
        }

        public float MinMS2RelativeIntensity {
            get { return Param.MinMS2RelativeIntensity; }
            set {
                if (value == Param.MinMS2RelativeIntensity) return;
                Param.MinMS2RelativeIntensity = value;
                OnPropertyChanged("MinMS2RelativeIntensity");
            }
        }
    }
}
