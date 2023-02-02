using CompMs.Common.MessagePack;
using Msdial.Gcms.Dataprocess;
using Msdial.Gcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class AnalysisFileProcessForGC
    {
        #region // members
        private BackgroundWorker bgWorker;
        private ProgressBarWin pbw;
        private string progressHeader = "File progress: ";
        private string progressFileMax;
        private int currentProgress;
        #endregion

        #region // data processing method summary
        public void Process(MainWindow mainWindow)
        {
            bgWorkerInitialize(mainWindow);

            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_Process_DoWork);
            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow });
        }

        private void bgWorkerInitialize(MainWindow mainWindow)
        {
            mainWindow.IsEnabled = false;
            if (mainWindow.MspDB != null && mainWindow.MspDB.Count > 0) {
                if (mainWindow.AnalysisParamForGC.RetentionType == RetentionType.RT)
                    mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.RetentionTime).ToList();
                else
                    mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.RetentionIndex).ToList();
            }

            this.progressFileMax = mainWindow.AnalysisFiles.Count.ToString();
            this.currentProgress = 0;

            this.pbw = new ProgressBarWin();
            this.pbw.Owner = mainWindow;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + "0/" + this.progressFileMax;
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Maximum = 100;
            this.pbw.ProgressView.Value = 0;
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
        private void bgWorker_Process_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] arg = (object[])e.Argument;

            MainWindow mainWindow = (MainWindow)arg[0];

            var projectProperty = mainWindow.ProjectProperty;
            var rdamPropertyBean = mainWindow.RdamProperty;
            var files = mainWindow.AnalysisFiles;
            var param = mainWindow.AnalysisParamForGC;
            var mspDB = mainWindow.MspDB;
            var alignmentFiles = mainWindow.AlignmentFiles;

			for (int i = 0; i < files.Count; i++) {
				try {
                    var error = string.Empty;
					ProcessFile.Execute(projectProperty, rdamPropertyBean, files[i], param, mspDB, null, out error);
                    if (error != string.Empty) {
                        MessageBox.Show(error);
                    }
					this.currentProgress++;
			} catch (Exception ex) {
				MessageBox.Show("error processing file: " + files[i].AnalysisFilePropertyBean.AnalysisFileName + 
					"\nMessage: " + ex.Message + "\n" + ex.Source +
					"\n" + ex.TargetSite +
					"\n" + ex.StackTrace);
			}
		}

		e.Result = new object[] { mainWindow };
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.pbw.ProgressView.Value = e.ProgressPercentage;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + this.currentProgress + "/" + this.progressFileMax;
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.pbw.Close();

            object[] arg = (object[])e.Result;

            MainWindow mainWindow = (MainWindow)arg[0];

            if (mainWindow.MspDB != null && mainWindow.MspDB.Count > 0) {
                mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.Id).ToList();
            }

            if (mainWindow.AnalysisFiles.Count != 1 && mainWindow.AnalysisParamForGC.TogetherWithAlignment)
            {
                new JointAlignerProcessGC().Execute(mainWindow); return;
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

        #endregion
    }
}
