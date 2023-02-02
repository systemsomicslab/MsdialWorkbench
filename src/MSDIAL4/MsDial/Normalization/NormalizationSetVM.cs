using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class NormalizationSetVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private AnalysisParametersBean paramLC;
        private AnalysisParamOfMsdialGcms paramGC;
        private Window window;

        private bool isNone;
        private bool isIS;
        private bool isLowess;
        private bool isIsLowess;
        private bool isTIC;
        private bool isMTIC;
        private bool isSplash;

        public NormalizationSetVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;

            this.paramLC = mainWindow.AnalysisParamForLC;
            this.paramGC = mainWindow.AnalysisParamForGC;
            if (mainWindow.ProjectProperty.Ionization == Ionization.ESI)
            {
                this.isNone = this.paramLC.IsNormalizeNone;
                this.isIS = this.paramLC.IsNormalizeIS;
                this.isLowess = this.paramLC.IsNormalizeLowess;
                this.isIsLowess = this.paramLC.IsNormalizeIsLowess;
                this.isTIC = this.paramLC.IsNormalizeTic;
                this.isMTIC = this.paramLC.IsNormalizeMTic;
                this.isSplash = this.paramLC.IsNormalizeSplash;
            }
            else
            {
                this.isNone = this.paramGC.IsNormalizeNone;
                this.isIS = this.paramGC.IsNormalizeIS;
                this.isLowess = this.paramGC.IsNormalizeLowess;
                this.isIsLowess = this.paramGC.IsNormalizeIsLowess;
                this.isTIC = this.paramGC.IsNormalizeTic;
                this.isMTIC = this.paramGC.IsNormalizeMTic;
                this.isSplash = this.paramGC.IsNormalizeSplash;
            }

            if (this.isNone == false && this.isIS == false && this.isLowess == false && this.isIsLowess == false && this.isTIC == false && this.isMTIC == false && this.isSplash == false)
            {
                this.isNone = true;
            }
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI)
            {
                this.paramLC.IsNormalizeNone = this.isNone;
                this.paramLC.IsNormalizeIS = this.isIS;
                this.paramLC.IsNormalizeLowess = this.isLowess;
                this.paramLC.IsNormalizeIsLowess = this.isIsLowess;
                this.paramLC.IsNormalizeTic = this.isTIC;
                this.paramLC.IsNormalizeMTic = this.isMTIC;
                this.paramLC.IsNormalizeSplash = this.isSplash;
            }
            else
            {
                this.paramGC.IsNormalizeNone = this.isNone;
                this.paramGC.IsNormalizeIS = this.isIS;
                this.paramGC.IsNormalizeLowess = this.isLowess;
                this.paramGC.IsNormalizeIsLowess = this.isIsLowess;
                this.paramGC.IsNormalizeTic = this.isTIC;
                this.paramGC.IsNormalizeMTic = this.isMTIC;
                this.paramGC.IsNormalizeSplash = this.isSplash;
            }

            if (this.isSplash) {
                if (this.mainWindow.ProjectProperty.TargetOmics != TargetOmics.Lipidomics) {
                    MessageBox.Show("Currently, this normalization is available for lipidomics project.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!IsInjectionVolumeSetOk(this.mainWindow)) {
                    MessageBox.Show("Please set injection volumes for samples in Option->File property setting before SPLASH normalizations.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else {
                    var splashWin = new SplashSetWin(this.mainWindow);
                    splashWin.Owner = this.mainWindow;
                    splashWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    splashWin.Show();
                    this.window.Close();
                    return;
                }
            }
            else if (MsDialDataNormalization.NormalizationFormatCheck(this.mainWindow, this.window, this.isNone, this.isIS, this.isLowess, this.isIsLowess, this.isTIC, this.isMTIC))
            {
                double lowessSpan = 1; if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) lowessSpan = this.paramLC.LowessSpan; else lowessSpan = this.paramGC.LowessSpan;
                MsDialDataNormalization.MainProcess(this.mainWindow, this.isNone, this.isIS, this.isLowess, this.isIsLowess, this.isTIC, this.isMTIC, lowessSpan);
            }
            else
            {
                return;
            }

            this.window.DialogResult = true;
            this.window.Close();
        }

        private bool IsInjectionVolumeSetOk(MainWindow mainWindow) {
            var files = mainWindow.AnalysisFiles;
            foreach (var file in files) {
                var volume = file.AnalysisFilePropertyBean.InjectionVolume;
                if (volume <= 0) {
                    return false;
                }
            }
            return true;
        }

        public bool IsNone
        {
            get { return isNone; }
            set { isNone = value; OnPropertyChanged("IsNone"); }
        }

        public bool IsIS
        {
            get { return isIS; }
            set { isIS = value; OnPropertyChanged("IsIS"); }
        }

        public bool IsLowess
        {
            get { return isLowess; }
            set { isLowess = value; OnPropertyChanged("IsLowess"); }
        }

        public bool IsIsLowess
        {
            get { return isIsLowess; }
            set { isIsLowess = value; OnPropertyChanged("IsIsLowess"); }
        }

        public bool IsTIC
        {
            get { return isTIC; }
            set { isTIC = value; OnPropertyChanged("IsTIC"); }
        }

        public bool IsMTIC
        {
            get { return isMTIC; }
            set { isMTIC = value; OnPropertyChanged("IsMTIC"); }
        }

        public bool IsSplash {
            get { return isSplash; }
            set { isSplash = value; OnPropertyChanged("IsSplash"); }
        }
    }
}
