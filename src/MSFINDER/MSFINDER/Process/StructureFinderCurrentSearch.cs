using Riken.Metabolomics.MsfinderCommon.Process;
using Riken.Metabolomics.MsfinderCommon.Utility;
using Riken.Metabolomics.StructureFinder.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class StructureFinderCurrentSearch
    {
        private ProgressBarWindow prb;
        private BackgroundWorker bgWorker;

        public void Process(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            mainWindow.IsEnabled = false;
            initilization(mainWindow);

            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, mainWindowVM });
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var arg = (object[])e.Argument;
            var mainwindow = (MainWindow)arg[0];
            var mainWindowVM = (MainWindowVM)arg[1];

            var fileID = mainWindowVM.SelectedRawFileId;
            var structureFolderPath = mainWindowVM.DataStorageBean.QueryFiles[fileID].StructureFolderPath;

            var structureFiles = System.IO.Directory.GetFiles(structureFolderPath, "*.sfd");
            if (structureFiles.Length > 0) FileStorageUtility.DeleteSfdFiles(structureFiles); 

            if (mainWindowVM.FormulaResultVMs == null || mainWindowVM.FormulaResultVMs.Count == 0) return;
            var formulaCandidates = mainWindowVM.FormulaResultVMs.Where(formulaVM => formulaVM.IsSelected).ToList();
            if (formulaCandidates == null || formulaCandidates.Count == 0) return;

            List<FragmentLibrary> fragmentDB = null;
            if (mainWindowVM.DataStorageBean.AnalysisParameter.IsUseEiFragmentDB && mainWindowVM.EiFragmentDB != null && mainWindowVM.EiFragmentDB.Count > 0) fragmentDB = mainWindowVM.EiFragmentDB;

            var fragmentOntologies = mainWindowVM.FragmentOntologyDB.Where(n => n.Frequency >= 0.2).ToList();

            int stepSize = (int)(100.0 / (double)formulaCandidates.Count);
            foreach (var formula in formulaCandidates)
            {
                var finder = new MsfinderStructureFinder();
                var exportFilePath = FileStorageUtility.GetStructureDataFilePath(structureFolderPath, formula.Formula);
                finder.StructureFinderMainProcess(mainWindowVM.DataStorageBean.RawData, formula.FormulaResult, 
                    mainWindowVM.DataStorageBean.AnalysisParameter, 
                    exportFilePath, mainWindowVM.ExistStructureDB, mainWindowVM.UserDefinedStructureDB, 
                    mainWindowVM.MineStructureDB, fragmentDB, fragmentOntologies, mainWindowVM.MspDB);
                this.bgWorker.ReportProgress(stepSize);
            }
            e.Result = new object[] { mainwindow, mainWindowVM };
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.prb.Close();

            var arg = (object[])e.Result;
            var mainWindow = (MainWindow)arg[0];
            var mainWindowVM = (MainWindowVM)arg[1];

            RefreshUtility.RefreshAll(mainWindow, mainWindowVM);
            mainWindow.IsEnabled = true;
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.prb.ProgressView.Value += e.ProgressPercentage;
        }

        private void initilization(MainWindow mainWindow)
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
        }
    }
}
