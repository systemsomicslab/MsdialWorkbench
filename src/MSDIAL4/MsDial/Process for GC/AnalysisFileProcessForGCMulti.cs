using Msdial.Gcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CompMs.Common.MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public class AnalysisFileProcessForGCMulti
    {
        #region // members
        private ProgressBarMultiContainerWin pbm;
        private string progressHeader = "File progress: ";
        private int progressFileMax;
        private int currentProgress;
        private int totalThreads;
        private int currentId;
        private List<ProgressBarEach> pblist;
        private MainWindow mainWindow;
        private List<Task> tasks = new List<Task>();
        public CancellationTokenSource CancellationTokenSource;
        public CancellationToken CancellationToken;

        #endregion

        public async Task Process(MainWindow mainWindow) {
            initialize(mainWindow);
            var numThreads = 1;
            if (mainWindow.AnalysisParamForGC.NumThreads > 1) {
                var lp = Environment.ProcessorCount;
                if (mainWindow.AnalysisParamForGC.NumThreads > lp + 1) {
                    mainWindow.AnalysisParamForGC.NumThreads = lp;
                }
                numThreads = mainWindow.AnalysisParamForGC.NumThreads;
            }

            var numMaxThreads = Math.Min(numThreads, progressFileMax);
            for (var i = 0; i < numMaxThreads; i++) {
                tasks.Add(RunEachProcess(i, mainWindow));
                await Task.Delay(1000);
            }
            await Task.WhenAll(tasks);
            await FinalizeAllProcess(this.mainWindow);
        }

        private void initialize(MainWindow mainWindow) {
            mainWindow.IsEnabled = false;
            this.mainWindow = mainWindow;
            // Init CancellationToken
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            if (mainWindow.MspDB != null && mainWindow.MspDB.Count > 0) {
                if (mainWindow.AnalysisParamForGC.RetentionType == RetentionType.RT)
                    mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.RetentionTime).ToList();
                else
                    mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.RetentionIndex).ToList();
            }
            this.progressFileMax = mainWindow.AnalysisFiles.Count;
            this.currentProgress = 0;
            this.totalThreads = mainWindow.AnalysisFiles.Count;
            this.currentId = 0;

            this.pbm = new ProgressBarMultiContainerWin();
            this.pbm.Owner = mainWindow;
            this.pbm.ProgressBar_Label.Content = this.progressHeader + "0/" + this.progressFileMax;
            this.pbm.ProgressView.Minimum = 0;
            this.pbm.ProgressView.Maximum = mainWindow.AnalysisFiles.Count;
            this.pbm.ProgressView.Value = 0;
            this.pbm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.pblist = new List<ProgressBarEach>();
            for (var j = 0; j < mainWindow.AnalysisFiles.Count; j++) {
                var text = mainWindow.AnalysisFiles[j].AnalysisFilePropertyBean.AnalysisFileName;
                this.pblist.Add(new ProgressBarEach(text, mainWindow.ProjectProperty.Ionization, CancellationToken));
            }
            this.pbm.SetProgressBar(pblist);
            this.pbm.Show();
        }

        private async Task RunEachProcess(int i, MainWindow mainWindow) {
            currentId++;
            totalThreads--;
            await this.pblist[i].StartGcProcess(mainWindow.ProjectProperty, mainWindow.RdamProperty, mainWindow.AnalysisFiles[i], mainWindow.AnalysisParamForGC, mainWindow.MspDB);
            await FinalizeOneProcess();
        }

        private async Task FinalizeOneProcess() {
            //Increment the overall progress bar
            this.pbm.ProgressView.Value++;
            this.currentProgress++;
            this.pbm.ProgressBar_Label.Content = this.progressHeader + this.currentProgress + "/" + this.progressFileMax;

            if (this.totalThreads > 0)
                await RunEachProcess(this.currentId, this.mainWindow);
            if (this.currentProgress == this.progressFileMax)
                return;
        }

        private async Task FinalizeAllProcess(MainWindow mainWindow) {
            this.pbm.Close();
            if (mainWindow.MspDB != null && mainWindow.MspDB.Count > 0) {
                mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.Id).ToList();
            }

            if (mainWindow.AnalysisFiles.Count != 1 && mainWindow.AnalysisParamForGC.TogetherWithAlignment) {
                await new JointAlignerProcessGC().Execute(mainWindow); return;
            }

            var projectProperty = mainWindow.ProjectProperty;
            var analysisFiles = mainWindow.AnalysisFiles;

            mainWindow.IsEnabled = true;

            Mouse.OverrideCursor = Cursors.Wait;

            mainWindow.SaveProperty = DataStorageGcUtility.GetSavePropertyBean(mainWindow.ProjectProperty, mainWindow.RdamProperty, mainWindow.MspDB,
                mainWindow.IupacReference, mainWindow.AnalysisParamForGC, mainWindow.AnalysisFiles, mainWindow.AlignmentFiles);
            MessagePackHandler.SaveToFile<SavePropertyBean>(mainWindow.SaveProperty, projectProperty.ProjectFilePath);
            //DataStorageGcUtility.SaveToXmlFile(mainWindow.SaveProperty, projectProperty.ProjectFilePath, typeof(SavePropertyBean));

            mainWindow.FileNavigatorUserControlsRefresh(analysisFiles);
            mainWindow.PeakViewerForGcRefresh(0);

            Mouse.OverrideCursor = null;
        }
    }
}
