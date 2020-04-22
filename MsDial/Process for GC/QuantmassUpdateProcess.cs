using CompMs.Common.MessagePack;
using Msdial.Gcms.Dataprocess.Algorithm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class QuantmassUpdateProcess
    {
        private BackgroundWorker bgWorker;
        private ProgressBarWin pbw;
        private string progressHeader = "Progress: ";
        private string progressFileMax;

        public void Execute(MainWindow mainWindow, AlignmentFileBean alignmentFile)
        {
            bgWorkerInitialize(mainWindow, alignmentFile);

            //var focusedFileID = mainWindow.FocusedFileID;
            //var focusedAlignmentFileID = mainWindow.FocusedAlignmentFileID;

            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, alignmentFile, mainWindow.FocusedAlignmentResult });
        }

        private void bgWorkerInitialize(MainWindow mainWindow, AlignmentFileBean alignmentFile)
        {
            MessagePackHandler.SaveToFile<AlignmentResultBean>(mainWindow.FocusedAlignmentResult, alignmentFile.FilePath);

            mainWindow.PeakViewDataAccessRefresh();
            mainWindow.AlignmentViewDataAccessRefresh();

            mainWindow.IsEnabled = false;
            mainWindow.FocusedAlignmentResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(alignmentFile.FilePath);

            this.progressFileMax = mainWindow.AnalysisFiles.Count.ToString();

            this.pbw = new ProgressBarWin();
            this.pbw.Owner = mainWindow;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + "0/" + this.progressFileMax;
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Maximum = mainWindow.AnalysisFiles.Count;
            this.pbw.ProgressView.Value = 0;
            this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.pbw.Show();

            //background worker
            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] arg = (object[])e.Argument;

            //var focusedFileID = (int)arg[0];
            //var focusedAlignmentFileID = (int)arg[1];

            MainWindow mainWindow = (MainWindow)arg[0];

            var rdamProperty = mainWindow.RdamProperty;
            var analysisFiles = mainWindow.AnalysisFiles;
            var param = mainWindow.AnalysisParamForGC;
            var projectProp = mainWindow.ProjectProperty;
            var mspDB = mainWindow.MspDB;

            var alignmentFile = (AlignmentFileBean)arg[1];
            var alignmentResult = (AlignmentResultBean)arg[2];

            PeakAlignment.QuantmassUpdate(rdamProperty, analysisFiles, param,
                alignmentResult, alignmentFile, projectProp, mspDB, this.bgWorker);

            e.Result = new object[] { mainWindow, alignmentResult };
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.pbw.ProgressView.Value = e.ProgressPercentage;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + e.ProgressPercentage + "/" + this.progressFileMax;
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.pbw.Close();

            object[] arg = (object[])e.Result;

            var mainWindow = (MainWindow)arg[0];
            var alignmentResult = (AlignmentResultBean)arg[1];

            new AlignedMS1DecStorageProcess().Finalize(mainWindow, alignmentResult);
        }
    }
}
