using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using CompMs.Common.MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public class CorrDecIdentificationProcess
    {
        private ProgressBarWin pbw;
        private string progressHeader = "Identify: ";

        public CorrDecIdentificationProcess() { }

        #region // data processing method summary
        public async Task Process(MainWindow mainWindow, AlignmentResultBean alignmentRes)
        {
            Initialize(mainWindow);
            alignmentRes = await MainTaskAsync(mainWindow, alignmentRes);
            Finalize(mainWindow, alignmentRes);
        }

        private void Initialize(MainWindow mainWindow)
        {
            mainWindow.IsEnabled = false;

            mainWindow.AlignmentViewDataAccessRefresh();
            if (mainWindow.MspDB != null && mainWindow.MspDB.Count >= 0)
                mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.PrecursorMz).ToList();

            this.pbw = new ProgressBarWin();
            this.pbw.Owner = mainWindow;
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Maximum = 100;
            this.pbw.ProgressView.Value = 0;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + "0%";
            this.pbw.Title = "Progress of CorrDec identification";
            this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.pbw.Show();
        }
        #endregion

        #region // background workers
        private async Task<AlignmentResultBean> MainTaskAsync(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            var projectProperty = mainWindow.ProjectProperty;
            var analysisFiles = mainWindow.AnalysisFiles;
            var param = mainWindow.AnalysisParamForLC;
            var alignmentFile = mainWindow.AlignmentFiles[mainWindow.FocusedAlignmentFileID];

            await Task.Run(() =>
            {
                IdentificationForAif.MainProcessForCorrDec(alignmentResult, alignmentFile, mainWindow.MspDB, param, projectProperty, progress => ReportProgress(progress));
            });
            return alignmentResult;
        }

        private void ReportProgress(int progress)
        {
            this.pbw.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.pbw.ProgressView.Value += progress;
                this.pbw.ProgressBar_Label.Content = this.progressHeader + this.pbw.ProgressView.Value + "%";
            }));
        }

        private void Finalize(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            this.pbw.Close();
            if (mainWindow.MspDB != null && mainWindow.MspDB.Count > 0)
                mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.Id).ToList();


            mainWindow.FocusedAlignmentResult = alignmentResult;
            var projectPropertyBean = mainWindow.ProjectProperty;
            var analysisFileBeanCollection = mainWindow.AnalysisFiles;

            mainWindow.SaveProperty = DataStorageLcUtility.GetSavePropertyBean(mainWindow.ProjectProperty, mainWindow.RdamProperty, mainWindow.MspDB
                    , mainWindow.IupacReference, mainWindow.AnalysisParamForLC, mainWindow.AnalysisFiles, mainWindow.AlignmentFiles
                    , mainWindow.PostIdentificationTxtDB, mainWindow.TargetFormulaLibrary);
            MessagePackHandler.SaveToFile<SavePropertyBean>(mainWindow.SaveProperty, mainWindow.ProjectProperty.ProjectFilePath);
            MessagePackHandler.SaveToFile<AlignmentResultBean>(mainWindow.FocusedAlignmentResult, mainWindow.AlignmentFiles[mainWindow.FocusedAlignmentFileID].FilePath);

            mainWindow.FileNavigatorUserControlsRefresh(analysisFileBeanCollection);
            mainWindow.PeakViewerForLcRefresh(0);
            mainWindow.AlignmentViewerForLcRefresh(mainWindow.AlignmentFiles[mainWindow.FocusedAlignmentFileID].FileID);

            mainWindow.IsEnabled = true;
            Mouse.OverrideCursor = null;
        }

        #endregion
    }
}
