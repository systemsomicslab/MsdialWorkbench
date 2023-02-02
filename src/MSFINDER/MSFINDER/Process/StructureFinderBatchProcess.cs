using Riken.Metabolomics.MsfinderCommon.Process;
using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.MsfinderCommon.Utility;
using Riken.Metabolomics.StructureFinder.Parser;
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
    public class StructureFinderBatchProcess
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

            var progress = 0;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var errorString = string.Empty;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                if (param.IsPubChemAllTime || param.IsPubChemOnlyUseForNecessary) { // currently difficult to execute this program as parallel when we use pubchem rest service
                    foreach (var file in analysisFiles) {
                        var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, param);
                        if (rawData == null || rawData.Name == null) continue;
                        SingleSearchOfStructureFinder(file, rawData, mainWindowVM);

                        progress++;
                        this.bgWorker.ReportProgress(progress);
                    }
                }
                else { // use parallel mode unless pubchem rest is used.
                    var syncObj = new object();
                    Parallel.ForEach(analysisFiles, file => {

                        var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, param);
                        if (rawData == null || rawData.Name == null) return;
                        SingleSearchOfStructureFinder(file, rawData, mainWindowVM);

                        lock (syncObj) {
                            progress++;
                            this.bgWorker.ReportProgress(progress);
                        }
                    });
                }
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

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var progress = e.ProgressPercentage;
            this.prb.ProgressView.Value = (int)((double)progress * 100.0 / this.maxFileNum);
            this.prb.ProgressBar_Label.Content = "Structure finder: " + progress + "/" + ((int)this.maxFileNum).ToString();
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

            RefreshUtility.RefreshAll(mainWindow, mainWindowVM);
            mainWindow.IsEnabled = true;
            MessageBox.Show("Batch job finished! total time(min): " + minutes, "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SingleSearchOfStructureFinder(MsfinderQueryFile file, RawData rawData, MainWindowVM mainWindowVM)
        {
            var structureFiles = System.IO.Directory.GetFiles(file.StructureFolderPath, "*.sfd");
            if (structureFiles.Length > 0) FileStorageUtility.DeleteSfdFiles(structureFiles); 

            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var existStructureDB = mainWindowVM.ExistStructureDB;
            var userDefinedDB = mainWindowVM.UserDefinedStructureDB;
            var mineStructureDB = mainWindowVM.MineStructureDB;
            var fragmentOntologies = mainWindowVM.FragmentOntologyDB.Where(n => n.Frequency >= 0.2).ToList();
            var mspDB = mainWindowVM.MspDB;
            var error = string.Empty;
            
            List<FragmentLibrary> fragmentDB = null; 
            if (param.IsUseEiFragmentDB && mainWindowVM.EiFragmentDB != null && mainWindowVM.EiFragmentDB.Count > 0) fragmentDB = mainWindowVM.EiFragmentDB;

            if (!System.IO.File.Exists(file.FormulaFilePath)) return;
            var formulaResults = FormulaResultParcer.FormulaResultReader(file.FormulaFilePath, out error);
            if (error != string.Empty) {
                Console.WriteLine(error);
            }

            if (formulaResults == null || formulaResults.Count == 0) return;

            foreach (var formula in formulaResults.Where(f => f.IsSelected).ToList()) {
                var exportFilePath = FileStorageUtility.GetStructureDataFilePath(file.StructureFolderPath, formula.Formula.FormulaString);
                var finder = new MsfinderStructureFinder();
                finder.StructureFinderMainProcess(rawData, formula, param, exportFilePath, existStructureDB, 
                    userDefinedDB, mineStructureDB, fragmentDB, fragmentOntologies, mspDB);
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
            this.prb.ProgressBar_Label.Content = "Structure finder";
            this.prb.Show();

            var files = mainWindowVM.DataStorageBean.QueryFiles;
            if (files != null) this.maxFileNum = files.Count; else this.maxFileNum = 0;
        }

    }
}
