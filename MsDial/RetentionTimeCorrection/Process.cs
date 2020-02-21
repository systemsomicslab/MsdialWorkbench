using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Msdial.Lcms.DataProcess;
using Msdial.Lcms.Dataprocess.Utility;

namespace Rfx.Riken.OsakaUniv.RetentionTimeCorrection
{

    public class RtCorrectionProcessForLC
    {
        #region // members
        private BackgroundWorker bgWorker;
        private ProgressBarWin pbw;
        private string progressHeader = "File progress: ";
        private string progressFileMax;
        private int currentProgress;
        private RetentionTimeCorrectionWin rtCorrectionWin;
        #endregion

        #region // data processing method summary
        public void Process(MainWindow mainWindow, RetentionTimeCorrectionWin rtCorrectionWin) {
            this.rtCorrectionWin = rtCorrectionWin;
            bgWorkerInitialize(mainWindow);

            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_Process_DoWork);
            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, rtCorrectionWin.VM.RtCorrectionCommon.StandardLibrary.Where(x => x.IsTarget).ToList(), rtCorrectionWin.VM.RtCorrectionParam });
        }

        private void bgWorkerInitialize(MainWindow mainWindow) {
            this.rtCorrectionWin.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            this.progressFileMax = mainWindow.AnalysisFiles.Count.ToString();
            this.currentProgress = 0;

            this.pbw = new ProgressBarWin();
            this.pbw.Owner = this.rtCorrectionWin;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + "0/" + this.progressFileMax;
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Maximum = mainWindow.AnalysisFiles.Count;
            this.pbw.ProgressView.Value = 0;
            this.pbw.Title = "RT correction";
            this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.pbw.Show();

            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
        }
        #endregion

        #region // background workers
        private void bgWorker_Process_DoWork(object sender, DoWorkEventArgs e) {
            object[] arg = (object[])e.Argument;

            MainWindow mainWindow = (MainWindow)arg[0];
            var iStandardLibrary = (List<TextFormatCompoundInformationBean>)arg[1];
            var rtParam = (RetentionTimeCorrectionParam)arg[2];

            var tmp_originalSettings = mainWindow.AnalysisParamForLC.MinimumAmplitude;
            mainWindow.AnalysisParamForLC.MinimumAmplitude = iStandardLibrary.Min(y => y.MinimumPeakHeight);

            System.Threading.Tasks.ParallelOptions parallelOptions = new System.Threading.Tasks.ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = mainWindow.AnalysisParamForLC.NumThreads;
            System.Threading.Tasks.Parallel.ForEach(mainWindow.AnalysisFiles, parallelOptions, f => {
                Msdial.Lcms.Dataprocess.Algorithm.RetentionTimeCorrection.Execute(mainWindow.ProjectProperty, mainWindow.RdamProperty, f, mainWindow.AnalysisParamForLC, iStandardLibrary, rtParam);
                this.bgWorker.ReportProgress(1);
            });

            mainWindow.AnalysisParamForLC.MinimumAmplitude = tmp_originalSettings;
            e.Result = new object[] { mainWindow };
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            this.pbw.ProgressView.Value = this.pbw.ProgressView.Value + e.ProgressPercentage;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + this.pbw.ProgressView.Value + "/" + this.progressFileMax;
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.pbw.ProgressBar_Label.Content = "Visualizing & Exporting PDF";
            object[] arg = (object[])e.Result;
            var mainWindow = (MainWindow)arg[0];
            this.pbw.Close();

            this.rtCorrectionWin.VM.RtCorrectionResUpdate();


//            RtCorrection.RtCorrectionUtility.PlotRtCorrectionRes(mainWindow);
//            RtCorrection.RtCorrectionUtility.ExportRtCorrectionRes(mainWindow);
//            this.rtCorrectionWin.RunFinished();
            Mouse.OverrideCursor = null;
            this.rtCorrectionWin.IsEnabled = true;
        }

        #endregion
    }
}
