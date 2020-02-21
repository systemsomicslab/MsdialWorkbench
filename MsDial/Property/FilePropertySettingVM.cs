using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class FilePropertySettingVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;
        private ObservableCollection<AnalysisFilePropertyVM> analysisFilePropertyCollection;

        public ObservableCollection<AnalysisFilePropertyVM> AnalysisFilePropertyCollection
        {
            get { return analysisFilePropertyCollection; }
            set { analysisFilePropertyCollection = value; }
        }

        public FilePropertySettingVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;
            this.analysisFilePropertyCollection = new ObservableCollection<AnalysisFilePropertyVM>();
            for (int i = 0; i < mainWindow.AnalysisFiles.Count; i++) { analysisFilePropertyCollection.Add(new AnalysisFilePropertyVM() { AnalysisFilePropertyBean = mainWindow.AnalysisFiles[i].AnalysisFilePropertyBean }); }
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            ClosingMethod();
            window.DialogResult = true;
            window.Close();
        }

        private void ClosingMethod()
        {
            var isClassNameChanged = false;
            for (int i = 0; i < this.analysisFilePropertyCollection.Count; i++)
            {
                if (this.mainWindow.ProjectProperty.FileID_ClassName[this.analysisFilePropertyCollection[i].AnalysisFileId] != this.analysisFilePropertyCollection[i].AnalysisFileClass) {
                    isClassNameChanged = true;
                }
                this.mainWindow.ProjectProperty.FileID_AnalysisFileType[this.analysisFilePropertyCollection[i].AnalysisFileId] = this.analysisFilePropertyCollection[i].AnalysisFileType;
                this.mainWindow.ProjectProperty.FileID_ClassName[this.analysisFilePropertyCollection[i].AnalysisFileId] = this.analysisFilePropertyCollection[i].AnalysisFileClass;
            }
            if (isClassNameChanged)
                MsDialStatistics.ClassColorDictionaryInitialization(this.mainWindow.AnalysisFiles, this.mainWindow.ProjectProperty, this.mainWindow.SolidColorBrushList);
            if (this.mainWindow.FocusedAlignmentResult != null) {

                var project = this.mainWindow.ProjectProperty;
                if (project.Ionization == Ionization.EI)
                    TableViewerUtility.CalcStatisticsForAlignmentProperty(this.mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection, this.mainWindow.AnalysisFiles, false);
                else {
                    var param = this.mainWindow.AnalysisParamForLC;
                    TableViewerUtility.CalcStatisticsForAlignmentProperty(this.mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection, this.mainWindow.AnalysisFiles, param.IsIonMobility);
                }
            }
        }
    }
}
