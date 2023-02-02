using Riken.Metabolomics.MsfinderCommon.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class FormulaFinderBatchProcess
    {
        private ProgressBarWindow prb;
        private BackgroundWorker bgWorker;
        private double maxFileNum;

        public void Process(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            mainWindow.IsEnabled = false;
            initilization(mainWindow, mainWindowVM);

            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, mainWindowVM });
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var arg = (object[])e.Argument;
            var mainwindow = (MainWindow)arg[0];
            var mainWindowVM = (MainWindowVM)arg[1];

            var analysisFiles = mainWindowVM.DataStorageBean.QueryFiles;
            if (analysisFiles == null || analysisFiles.Count == 0) return;

            var syncObj = new object();
            var progress = 0;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var errorString = string.Empty;
            try {
                Parallel.ForEach(analysisFiles, file =>
                {
                    var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, param);
                    if (rawData == null || rawData.Name == null) return;
                    SingleSearchOfMolecularFormularFinder(file, rawData, mainWindowVM);

                    lock (syncObj) {
                        progress++;
                        this.bgWorker.ReportProgress(progress);
                    }
                });
            }
            catch (AggregateException ae) {
                var exceptions = ae.Flatten().InnerExceptions;
                foreach (var ex in exceptions) {
                    errorString += ex.GetType() + "\r\n";
                }
            }
            finally {
                var minutes = stopwatch.ElapsedMilliseconds * 0.001 / 60;
                e.Result = new object[] { mainwindow, mainWindowVM, minutes, errorString };
            }
        }


        public void SingleSearchOfMolecularFormularFinder(MsfinderQueryFile file, RawData rawData, MainWindowVM mainWindowVM)
        {
            var formulaFilePath = file.FormulaFilePath;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            //var formulaDB = mainWindowVM.QuickFormulaDB;
            var productIonDB = mainWindowVM.ProductIonDB;
            var neutralLossDB = mainWindowVM.NeutralLossDB;
            var existFormulaDB = mainWindowVM.ExistFormulaDB;
            var chemOntDB = mainWindowVM.ChemicalOntologies;

            var formulaResults = new List<FormulaResult>();
            if (param.IsRunInSilicoFragmenterSearch) {
                formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, param);
                ChemicalOntologyAnnotation.ProcessByOverRepresentationAnalysis(formulaResults, chemOntDB, 
                    rawData.IonMode, param, AdductIonParcer.GetAdductIonBean(rawData.PrecursorType), productIonDB, neutralLossDB);
            }
            if (param.IsRunSpectralDbSearch)
                formulaResults.Add(FormulaResultParcer.GetFormulaResultTemplateForSpectralDbSearch());

            FormulaResultParcer.FormulaResultsWriter(formulaFilePath, formulaResults);
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var progress = e.ProgressPercentage;
            this.prb.ProgressView.Value = (int)((double)progress * 100.0 / this.maxFileNum);
            this.prb.ProgressBar_Label.Content = "Formula finder: " + progress + "/" + ((int)this.maxFileNum).ToString();
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null) {
                MessageBox.Show("Batch job was desorbed by the following error: " 
                    + e.Error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                ((MainWindow)this.prb.Owner).IsEnabled = true;
                this.prb.Close();
                return;
            }

            this.prb.Close();
            var arg = (object[])e.Result;
            var mainWindow = (MainWindow)arg[0];
            var mainWindowVM = (MainWindowVM)arg[1];
            var minutes = (double)arg[2];
            var errorLog = (string)arg[3];
            if (errorLog != string.Empty) {
                RefreshUtility.RefreshAll(mainWindow, mainWindowVM);
                mainWindow.IsEnabled = true;
                MessageBox.Show(errorLog, "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (mainWindowVM.DataStorageBean.AnalysisParameter.IsStructureFinder)
            {
                var structureFinderBatchJob = new StructureFinderBatchProcess();
                structureFinderBatchJob.Process(mainWindow, mainWindowVM);
            }
            else
            {
                RefreshUtility.RefreshAll(mainWindow, mainWindowVM);
                mainWindow.IsEnabled = true;
                MessageBox.Show("Batch job finished! total time(min): " + minutes, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
            this.prb.ProgressBar_Label.Content = "Formula finder";
            this.prb.Show();

            var files = mainWindowVM.DataStorageBean.QueryFiles;
            if (files != null) this.maxFileNum = files.Count; else this.maxFileNum = 0;
        }

    }
}
