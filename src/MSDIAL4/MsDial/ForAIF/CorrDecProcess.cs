using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using CompMs.Common.MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public class CorrDecProcess
    {
        [Flags]
        public enum ProcessFlag { Grouping, Correltaion }
        public enum DecTarget { Single, All }

        private ProgressBarWin pbw;
        private string progressHeader = "";
        private int progressFileMax;
        private ProcessFlag Flag;
        private DecTarget Target;

        public CorrDecProcess() { }

        #region // data processing method summary
        public async Task ProcessAll(MainWindow mainWindow, AlignmentResultBean alignmentRes, ProcessFlag flag = ProcessFlag.Grouping | ProcessFlag.Correltaion) {
            this.Flag = flag;
            this.Target = DecTarget.All;
            Initialize(mainWindow, alignmentRes);
            if (this.Flag.HasFlag(ProcessFlag.Grouping))
                await CreateSpectraGroupAsync(mainWindow, alignmentRes);

            if (this.Flag.HasFlag(ProcessFlag.Correltaion))
                await MainCorrDecAsync(mainWindow, alignmentRes);
        
            Finalize(mainWindow, alignmentRes);
        }

        public async Task ProcessSingle(MainWindow mainWindow, AlignmentResultBean alignmentRes, int alignmentId)
        {
            SetProgressBar(mainWindow);
            this.progressHeader = "Single : ";
            this.pbw.ProgressBar_Label.Content = this.progressHeader + "0%";
            this.progressFileMax = mainWindow.AnalysisFiles.Count + 1;
            this.pbw.ProgressView.Maximum = progressFileMax;
            this.pbw.Show();

            List<CorrDecResult> results = new List<CorrDecResult>();
            await Task.Run(() =>
            {
                results = CorrDecBase.CorrDecSingleAlignmentSpot(mainWindow.ProjectProperty, mainWindow.RdamProperty, mainWindow.AnalysisFiles, mainWindow.AnalysisParamForLC, alignmentRes, alignmentId, ReportProgress);
            });
            this.pbw.Close();
            mainWindow.PeakViewerForLcRefresh(0);
            mainWindow.SetAlignmentFileForAifViewerController();
            var correlDecResMsViewer = new MsViewer.CorrelationDecResMsViewer(new AifViewControlCommonProperties(mainWindow.ProjectProperty, 
                mainWindow.AnalysisFiles, mainWindow.AnalysisParamForLC, mainWindow.LcmsSpectrumCollection, 
                mainWindow.MsdialIniField, mainWindow.AnalysisFiles[mainWindow.FocusedFileID], mainWindow.MspDB), 
                mainWindow.FocusedAlignmentResult, results, 
                mainWindow.ProjectProperty.ExperimentID_AnalystExperimentInformationBean.Values.Select(x => x.Name).ToList(), 
                mainWindow.FocusedAlignmentPeakID);
            correlDecResMsViewer.Owner = mainWindow;
            correlDecResMsViewer.Title = "Correlation-based deconvolution";
            correlDecResMsViewer.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            correlDecResMsViewer.Show();
            mainWindow.IsEnabled = true;
        }
        private void SetProgressBar(MainWindow mainWindow)
        {
            this.pbw = new ProgressBarWin();
            this.pbw.Owner = mainWindow;
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Value = 0;
            this.pbw.ProgressView.Maximum = 100;
            this.pbw.Title = "Progress of correlation based deconvolution;";
            this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void Initialize(MainWindow mainWindow, AlignmentResultBean alignmentRes) {
            mainWindow.IsEnabled = false;
            SetProgressBar(mainWindow);
            if (this.Flag.HasFlag(ProcessFlag.Grouping) && this.Flag.HasFlag(ProcessFlag.Correltaion))
                this.pbw.Title += " Grouping & Correlation";
            else if (this.Flag.HasFlag(ProcessFlag.Grouping))
                this.pbw.Title += " Grouping only";
            else if (this.Flag.HasFlag(ProcessFlag.Correltaion))
                this.pbw.Title += " Correlation only";
            this.pbw.Show();

            mainWindow.AlignmentViewDataAccessRefresh();
        }

        private void SetGroupingProgressBar()
        {
            this.progressHeader = "Grouping : ";
            this.pbw.ProgressBar_Label.Content = this.progressHeader + "0%";
            this.pbw.ProgressView.Maximum = progressFileMax;
        }

        private void SetCorrelationProgressBar()
        {
            this.progressHeader = "Correlation: ";
            this.pbw.ProgressView.Value = 0;
            this.pbw.ProgressView.Maximum = this.progressFileMax;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + "0%";
        }

        #endregion

        #region MainProcess
        private async Task CreateSpectraGroupAsync(MainWindow mainWindow, AlignmentResultBean alignmentResult) {
            var projectProperty = mainWindow.ProjectProperty;
            var rdamProperty = mainWindow.RdamProperty;
            var analysisFiles = mainWindow.AnalysisFiles;
            var param = mainWindow.AnalysisParamForLC;
            var alignmentFile = mainWindow.AlignmentFiles[mainWindow.AlignmentFiles.Count - 1];

            var numMs2Scan = mainWindow.ProjectProperty.Ms2LevelIdList.Count;
            if (numMs2Scan == 0) numMs2Scan = 1;
            this.progressFileMax = mainWindow.AnalysisFiles.Count + alignmentResult.AlignmentPropertyBeanCollection.Count * numMs2Scan;
            SetGroupingProgressBar();
            await Task.Run(() =>
            {
                CorrDecBase.GenerateTemporalMs2SpectraFiles(projectProperty, rdamProperty, analysisFiles, param, alignmentResult, alignmentFile, progress => ReportProgress(progress));
                CorrDecBase.CreateMs2SpectraGroup(projectProperty, alignmentFile, analysisFiles, alignmentResult, param, progress => ReportProgress(progress));
                CorrDecBase.TemporaryDirectoryHandler(projectProperty, isCreate: false, isRemove: true);
            });
        }

        private async Task MainCorrDecAsync(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            var projectProperty = mainWindow.ProjectProperty;
            var rdamProperty = mainWindow.RdamProperty;
            var analysisFiles = mainWindow.AnalysisFiles;
            var param = mainWindow.AnalysisParamForLC;
            var alignmentFile = mainWindow.AlignmentFiles[mainWindow.AlignmentFiles.Count - 1];
             
            var numMs2Scan = mainWindow.ProjectProperty.Ms2LevelIdList.Count;
            if (numMs2Scan == 0) numMs2Scan = 1;
            this.progressFileMax = 100 * numMs2Scan;
            SetCorrelationProgressBar();

            await Task.Run(() =>
            {
                for (var i = 0; i < projectProperty.Ms2LevelIdList.Count; i++)
                {
                    var decFilePath = projectProperty.ProjectFolderPath + "\\" + alignmentFile.FileName + "_MsGrouping_Raw_" + i + ".mfg";
                    var filePath = projectProperty.ProjectFolderPath + "\\" + alignmentFile.FileName + "_CorrelationBasedDecRes_Raw_" + i + ".cbd";
                    CorrDecHandler.WriteCorrelationDecRes(param.AnalysisParamOfMsdialCorrDec, projectProperty, alignmentResult.AlignmentPropertyBeanCollection, analysisFiles.Count, filePath, decFilePath, progress => ReportProgress(progress));
                }
                if(projectProperty.Ms2LevelIdList.Count == 0)
                {
                    var i = 0;
                    var decFilePath = projectProperty.ProjectFolderPath + "\\" + alignmentFile.FileName + "_MsGrouping_Raw_" + i + ".mfg";
                    var filePath = projectProperty.ProjectFolderPath + "\\" + alignmentFile.FileName + "_CorrelationBasedDecRes_Raw_" + i + ".cbd";
                    CorrDecHandler.WriteCorrelationDecRes(param.AnalysisParamOfMsdialCorrDec, projectProperty, alignmentResult.AlignmentPropertyBeanCollection, analysisFiles.Count, filePath, decFilePath, progress => ReportProgress(progress));
                }
            });
        }


        private void ReportProgress(float progress) {
            this.pbw.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.pbw.ProgressView.Value += progress;
                this.pbw.ProgressBar_Label.Content = this.progressHeader + ((this.pbw.ProgressView.Value / (double)this.progressFileMax) * 100).ToString("0.0") + "%";
            }));
        }
        #endregion

        #region Finalizer
        private void Finalize(MainWindow mainWindow, AlignmentResultBean alignmentResult) {
            this.pbw.Close();

            var projectPropertyBean = mainWindow.ProjectProperty;
            var analysisFileBeanCollection = mainWindow.AnalysisFiles;

            mainWindow.SaveProperty = DataStorageLcUtility.GetSavePropertyBean(mainWindow.ProjectProperty, mainWindow.RdamProperty, mainWindow.MspDB
                    , mainWindow.IupacReference, mainWindow.AnalysisParamForLC, mainWindow.AnalysisFiles, mainWindow.AlignmentFiles
                    , mainWindow.PostIdentificationTxtDB, mainWindow.TargetFormulaLibrary);
            MessagePackHandler.SaveToFile<SavePropertyBean>(mainWindow.SaveProperty, mainWindow.ProjectProperty.ProjectFilePath);

            mainWindow.FileNavigatorUserControlsRefresh(analysisFileBeanCollection);
            mainWindow.PeakViewerForLcRefresh(0);
            mainWindow.AlignmentViewerForLcRefresh(mainWindow.AlignmentFiles[mainWindow.FocusedAlignmentFileID].FileID);

            mainWindow.IsEnabled = true;
            Mouse.OverrideCursor = null;
        }
        #endregion
    }
}
