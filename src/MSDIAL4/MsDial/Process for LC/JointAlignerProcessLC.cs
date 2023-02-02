using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Lcms.DataProcess;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This class describes how MS-DIAL program performs the peak alignment. See also PeakAlignmet.cs.
    /// The peak alignemnt process is based on the modified joint aligner described in the previous article.
    /// Katajamaa, M. & Oresic, M. Processing methods for differential analysis of LC/MS profile data. BMC Bioinformatics 6, 179 (2005).
    /// </summary>
    public class JointAlignerProcessLC
    {
        private ProgressBarWin pbw;
        private string progressHeader = "Alignment: ";
        private string progressFileMax;

        public async Task<AlignmentResultBean> JointAligner(MainWindow mainWindow)
        {
            Initialize(mainWindow);
            var alignmentResult = await MainTaskAsync(mainWindow);
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

        private async Task<AlignmentResultBean> MainTaskAsync(MainWindow mainWindow)
        {
            var projectPropertyBean = mainWindow.ProjectProperty;
            var rdamPropertyBean = mainWindow.RdamProperty;
            var analysisFileBeanCollection = mainWindow.AnalysisFiles;
            var analysisParametersBean = mainWindow.AnalysisParamForLC;
            var alignmentFileBeanCollection = mainWindow.AlignmentFiles;
            var alignmentResultBean = new AlignmentResultBean();

            try
            {
                await Task.Run(() => ProcessJointAligner.Execute(rdamPropertyBean, projectPropertyBean, analysisFileBeanCollection,
                    analysisParametersBean, alignmentResultBean, progress => ReportProgress(progress)));
            }
            catch
            {
                MessageBox.Show("Error occured in JointAligner.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            this.pbw.Close();
            return alignmentResultBean;
        }

        private void ReportProgress(int progress)
        {
            this.pbw.Dispatcher.BeginInvoke((Action)(() => {
                this.pbw.ProgressView.Value = progress;
                this.pbw.ProgressBar_Label.Content = this.progressHeader + progress + "/" + this.progressFileMax;
            }));
        }

        // Old codes should be removed
        /*
        private BackgroundWorker bgWorker;
        private ProgressBarWin pbw;
        private string progressHeader = "Alignment: ";
        private string progressFileMax;

        public void JointAligner(MainWindow mainWindow)
        {
            bgWorkerInitialize(mainWindow);
            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow });
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
            var projectPropertyBean = mainWindow.ProjectProperty;
            var rdamPropertyBean = mainWindow.RdamProperty;
            var analysisFileBeanCollection = mainWindow.AnalysisFiles;
            var analysisParametersBean = mainWindow.AnalysisParamForLC;
            var alignmentFileBeanCollection = mainWindow.AlignmentFiles;
            var alignmentResultBean = new AlignmentResultBean();

            ProcessJointAligner.Execute(rdamPropertyBean, projectPropertyBean, analysisFileBeanCollection, 
                analysisParametersBean, alignmentResultBean, bgWorker);

            e.Result = new object[] { mainWindow, alignmentResultBean };
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
            object[] arg = (object[])e.Result;

            var mainWindow = (MainWindow)arg[0];
            var alignmentResult = (AlignmentResultBean)arg[1];
            if (alignmentResult.AlignmentPropertyBeanCollection == null || alignmentResult.AlignmentPropertyBeanCollection.Count == 0) {
                MessageBox.Show("There is no peak information for peak alignment. Please check your ion mode setting.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                mainWindow.IsEnabled = true;

                Mouse.OverrideCursor = Cursors.Wait;
                mainWindow.SaveProperty = DataStorageLcUtility.GetSavePropertyBean(mainWindow.ProjectProperty, mainWindow.RdamProperty, mainWindow.MspDB
                , mainWindow.IupacReference, mainWindow.AnalysisParamForLC, mainWindow.AnalysisFiles, mainWindow.AlignmentFiles
                , mainWindow.PostIdentificationTxtDB, mainWindow.TargetFormulaLibrary);

                MessagePackHandler.SaveToFile<SavePropertyBean>(mainWindow.SaveProperty, mainWindow.ProjectProperty.ProjectFilePath);
                
                //DataStorageLcUtility.SaveToXmlFile(mainWindow.SaveProperty, 
                //    mainWindow.ProjectProperty.ProjectFilePath, 
                //    typeof(SavePropertyBean));

                mainWindow.FileNavigatorUserControlsRefresh(mainWindow.AnalysisFiles);
                mainWindow.PeakViewerForLcRefresh(0);
                Mouse.OverrideCursor = null;

                return;
            }

            new GapFillingProcessLC().GapFilling(mainWindow, alignmentResult);
        }
        */
    }
}
