using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class LowessSpanSetVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;
        private AnalysisParametersBean paramLC;
        private AnalysisParamOfMsdialGcms paramGC;
        private double lowessSpan;
        private double minOptSize;
        private bool isEnabled;
        private int qcNum;

        public LowessSpanSetVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;
            this.paramLC = mainWindow.AnalysisParamForLC;
            this.paramGC = mainWindow.AnalysisParamForGC;

            if (mainWindow.ProjectProperty.Ionization == Ionization.ESI)
                this.lowessSpan = this.paramLC.LowessSpan;
            else
                this.lowessSpan = this.paramGC.LowessSpan;

            setMinOptSize(mainWindow.AnalysisFiles);
            setIsEnabled(this.lowessSpan);
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI)
                this.paramLC.LowessSpan = this.lowessSpan;
            else
                this.paramGC.LowessSpan = this.lowessSpan;
            
            this.window.DialogResult = true;
            this.window.Close();
        }

        private void setIsEnabled(double span)
        {
            if (span >= minOptSize - 0.0001 && span <= 1.0)
            {
                this.isEnabled = true;
            }
            else
            {
                this.isEnabled = false;
            }
        }

        private void setMinOptSize(ObservableCollection<AnalysisFileBean> files)
        {
            int qcNum = 0;

            foreach (var file in files)
            {
                if (file.AnalysisFilePropertyBean.AnalysisFileType == AnalysisFileType.QC && file.AnalysisFilePropertyBean.AnalysisFileIncluded) qcNum++;
            }

            this.qcNum = qcNum;
            this.minOptSize = SmootherMathematics.GetMinimumLowessSpan(this.qcNum);
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { isEnabled = value; OnPropertyChanged("IsEnabled"); }
        }

        public int QcNum
        {
            get { return qcNum; }
            set { qcNum = value; OnPropertyChanged("QcNum"); }
        }

        public double LowessSpan
        {
            get { return lowessSpan; }
            set { lowessSpan = value; OnPropertyChanged("LowessSpan"); setIsEnabled(lowessSpan); OnPropertyChanged("IsEnabled"); }
        }

        public double MinOptSize
        {
            get { return Math.Round(minOptSize, 3); }
            set { minOptSize = value; OnPropertyChanged("MinOptSize"); }
        }

    }
}
