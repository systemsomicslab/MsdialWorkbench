using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class AdvancedLibraryOptionSettingVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;

        private bool onlyReportTopHitForPostAnnotation;
        private float relativeAbundanceCutOff;

        public AdvancedLibraryOptionSettingVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;

            this.onlyReportTopHitForPostAnnotation = this.mainWindow.AnalysisParamForLC.OnlyReportTopHitForPostAnnotation;
            this.relativeAbundanceCutOff = this.mainWindow.AnalysisParamForLC.RelativeAbundanceCutOff;
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);
           
            Mouse.OverrideCursor = Cursors.Wait;
            if (ClosingMethod() == true)
            {
                window.DialogResult = true;
                window.Close();
            }
            Mouse.OverrideCursor = null;

        }

        private bool ClosingMethod()
        {
            if (this.relativeAbundanceCutOff < 0 || this.relativeAbundanceCutOff > 100)
            {
                MessageBox.Show("Add 0-100 value for the relative abundance cut off.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            this.mainWindow.AnalysisParamForLC.OnlyReportTopHitForPostAnnotation = this.onlyReportTopHitForPostAnnotation;
            this.mainWindow.AnalysisParamForLC.RelativeAbundanceCutOff = this.relativeAbundanceCutOff;

            return true;
        }

        #region properties
        public bool OnlyReportTopHitForPostAnnotation
        {
            get { return onlyReportTopHitForPostAnnotation; }
            set { if (onlyReportTopHitForPostAnnotation == value) return; onlyReportTopHitForPostAnnotation = value; OnPropertyChanged("OnlyReportTopHitForPostAnnotation"); }
        }

        public float RelativeAbundanceCutOff
        {
            get { return relativeAbundanceCutOff; }
            set { if (relativeAbundanceCutOff == value) return; relativeAbundanceCutOff = value; OnPropertyChanged("RelativeAbundanceCutOff"); }
        }
        #endregion
    }
}
