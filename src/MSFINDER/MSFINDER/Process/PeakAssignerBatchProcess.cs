using Riken.Metabolomics.MsfinderCommon.Process;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class PeakAssignerBatchProcess
    {
        private ProgressBarWindow prb;
        private BackgroundWorker bgWorker;
        private double maxFileNum;

        public void Process(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            var selectedRawFile = mainWindowVM.SelectedRawFileId;

            RefreshUtility.RawDataFileRefresh(mainWindowVM, selectedRawFile);
            RefreshUtility.FormulaDataFileRefresh(mainWindow, mainWindowVM, selectedRawFile);
            RefreshUtility.StructureDataFileRefresh(mainWindow, mainWindowVM, selectedRawFile);

            mainWindow.IsEnabled = false;
            initilization(mainWindow, mainWindowVM);

            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, mainWindowVM });
        }

        private void initilization(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);

            this.prb = new ProgressBarWindow();
            this.prb.Owner = mainWindow;
            this.prb.ProgressView.Maximum = 100;
            this.prb.ProgressView.Minimum = 0;
            this.prb.ProgressView.Value = 0;
            this.prb.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            this.prb.ProgressBar_Label.Content = "Fragment assigner";
            this.prb.Show();

            var files = mainWindowVM.DataStorageBean.QueryFiles;
            if (files != null) this.maxFileNum = files.Count; else this.maxFileNum = 0;
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var arg = (object[])e.Argument;
            var mainwindow = (MainWindow)arg[0];
            var mainWindowVM = (MainWindowVM)arg[1];

            var analysisFiles = mainWindowVM.DataStorageBean.QueryFiles;
            if (analysisFiles == null || analysisFiles.Count == 0) return;

            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var productIonDB = mainWindowVM.ProductIonDB;
            var neutralLossDB = mainWindowVM.NeutralLossDB;
            var existFormulaDB = mainWindowVM.ExistFormulaDB;
            var fragmentDB = mainWindowVM.EiFragmentDB;
            var fragmentOntologies = mainWindowVM.FragmentOntologyDB;

            var syncObj = new object();
            var progress = 0;

            Parallel.ForEach(analysisFiles, file => { 
                var rawDataFilePath = file.RawDataFilePath;
                var formulaFilePath = file.FormulaFilePath;
                var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, param);

                if (rawData.Formula == null || rawData.Formula == string.Empty || rawData.Smiles == null || rawData.Smiles == string.Empty) return;

                if (param.IsUseEiFragmentDB)
                    PeakAssigner.Process(file, rawData, param, productIonDB, neutralLossDB, existFormulaDB, fragmentDB, fragmentOntologies);
                else
                    PeakAssigner.Process(file, rawData, param, productIonDB, neutralLossDB, existFormulaDB, null, fragmentOntologies);

                lock (syncObj) {
                    progress++;
                    this.bgWorker.ReportProgress(progress);
                }
            });
            e.Result = new object[] { mainwindow, mainWindowVM };
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var progress = e.ProgressPercentage;
            this.prb.ProgressView.Value = (int)((double)progress * 100.0 / this.maxFileNum);
            this.prb.ProgressBar_Label.Content = "Fragment assigner: " + progress + "/" + ((int)this.maxFileNum).ToString();
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.prb.Close();

            var arg = (object[])e.Result;
            var mainWindow = (MainWindow)arg[0];
            var mainWindowVM = (MainWindowVM)arg[1];

            
            mainWindow.IsEnabled = true;
            MessageBox.Show("Batch job finished!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
