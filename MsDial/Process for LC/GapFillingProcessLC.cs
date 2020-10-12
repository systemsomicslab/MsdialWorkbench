using Msdial.Lcms.DataProcess;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This class describes how MS-DIAL program performs the interpolation for missing values existed in the alignment result.
    /// The interpolation process (Gap-filling) is performed as ansynchronous process.
    /// </summary>
    public class GapFillingProcessLC
    {
        private ProgressBarWin pbw;
        private string progressHeader = "Gap filling: ";
        private string progressFileMax;

        public async Task<AlignmentResultBean> GapFilling(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            Initialize(mainWindow);
            alignmentResult = await MainTaskAsync(mainWindow, alignmentResult);
            return alignmentResult;
        }

        private void Initialize(MainWindow mainWindow)
        {
            mainWindow.IsEnabled = false;
            this.progressFileMax = mainWindow.AnalysisFiles.Count.ToString();

            this.pbw = new ProgressBarWin();
            this.pbw.Owner = mainWindow;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + "0/" + this.progressFileMax;
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Maximum = mainWindow.AnalysisFiles.Count;
            this.pbw.ProgressView.Value = 0;
            this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.pbw.Show();
        }

        private async Task<AlignmentResultBean> MainTaskAsync(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            var projectProperty = mainWindow.ProjectProperty;
            var rdamProperty = mainWindow.RdamProperty;
            var analysisFiles = mainWindow.AnalysisFiles;
            var param = mainWindow.AnalysisParamForLC;
            var alignmentFile = mainWindow.AlignmentFiles[mainWindow.AlignmentFiles.Count - 1];
            var iupacRef = mainWindow.IupacReference;

            try
            {
                await Task.Run(() =>
                {
                    ProcessGapFilling.Execute(projectProperty, rdamProperty, analysisFiles, alignmentFile, param, iupacRef, alignmentResult, progress => ReportProgress(progress));
                });
            }
            catch
            {
                MessageBox.Show("Error is occured in GapFilling.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            this.pbw.Close();
            return alignmentResult;
        }

        private void ReportProgress(int progress)
        {
            this.pbw.Dispatcher.Invoke((Action)(() =>
            {
                this.pbw.ProgressView.Value = progress;
                this.pbw.ProgressBar_Label.Content = this.progressHeader + progress + "/" + this.progressFileMax;
            }));
        }


        /* Old codes should be removed;
         *         private BackgroundWorker bgWorker;
        private ProgressBarWin pbw;
        private string progressHeader = "Gap filling: ";
        private string progressFileMax;

        public void GapFilling(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            bgWorkerInitialize(mainWindow);
            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, alignmentResult });
        }

        private void bgWorkerInitialize(MainWindow mainWindow)
        {
            mainWindow.IsEnabled = false;
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

            MainWindow mainWindow = (MainWindow)arg[0];
            var projectProperty = mainWindow.ProjectProperty;
            var rdamProperty = mainWindow.RdamProperty;
            var analysisFiles = mainWindow.AnalysisFiles;
            var param = mainWindow.AnalysisParamForLC;
            var alignmentResult = (AlignmentResultBean)arg[1];
            var alignmentFile = mainWindow.AlignmentFiles[mainWindow.AlignmentFiles.Count - 1];

            ProcessGapFilling.Execute(projectProperty, rdamProperty, analysisFiles, alignmentFile, param, alignmentResult, this.bgWorker);

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

            if (e.Error != null) {
                MessageBox.Show("Error is occured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else {
                object[] arg = (object[])e.Result;

                var mainWindow = (MainWindow)arg[0];
                var alignmentResult = (AlignmentResultBean)arg[1];

                new AlignmentFinalizeProcessLC().Finalize(mainWindow, alignmentResult);
            }
        }
         */
    }
}
