using Riken.Metabolomics.MsfinderCommon.Utility;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
	public sealed class WorkingSpacePreparation
    {
        private BackgroundWorker bgWorker;
        private ShortMessageWindow window;

        public void Process(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            mainWindow.IsEnabled = false;
            initilization(mainWindow);
            var errors = WorkSpaceCheck();

            if (errors.Count == 0) {
                this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, mainWindowVM });
            } else {
                MessageBox.Show(string.Join("\n", errors));
                Application.Current.Shutdown();
            }
        }

        public List<string> WorkSpaceCheck()
        {
            var errors = new List<string>();

            ResourceSet resourceSet = Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            foreach (DictionaryEntry entry in resourceSet) {
                string k = entry.Key.ToString();
                if (k.EndsWith("Lib")) {
                    string v = entry.Value.ToString();
                    string file = FileStorageUtility.GetResourcesPath(k);
                    if (!new FileInfo(file).Exists) {
                        errors.Add(string.Format("Missing library file: {0}", file));
                    }
                }
            }

            return errors;
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var arg = (object[])e.Argument;
            var mainwindow = (MainWindow)arg[0];
            var mainWindowVM = (MainWindowVM)arg[1];
            //var options = new ParallelOptions() { MaxDegreeOfParallelism = 4 };

            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            Parallel.Invoke(() =>
             {
                 //mainWindowVM.QuickFormulaDB = FileStorageUtility.GetKyusyuUnivFormulaDB(param);
             },
             () =>
             {
                 mainWindowVM.NeutralLossDB = FileStorageUtility.GetNeutralLossDB();
                 mainWindowVM.ProductIonDB = FileStorageUtility.GetProductIonDB();
                 mainWindowVM.FragmentOntologyDB = FileStorageUtility.GetUniqueFragmentDB();
                 mainWindowVM.ChemicalOntologies = FileStorageUtility.GetChemicalOntologyDB();

                 if (mainWindowVM.FragmentOntologyDB != null && mainWindowVM.ProductIonDB != null)
                     ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(mainWindowVM.ProductIonDB, mainWindowVM.FragmentOntologyDB);

                 if (mainWindowVM.FragmentOntologyDB != null && mainWindowVM.NeutralLossDB != null)
                     ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(mainWindowVM.NeutralLossDB, mainWindowVM.FragmentOntologyDB);

                 if (mainWindowVM.FragmentOntologyDB != null && mainWindowVM.ChemicalOntologies != null)
                     ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(mainWindowVM.ChemicalOntologies, mainWindowVM.FragmentOntologyDB);
             },
             () =>
             {
                 mainWindowVM.ExistFormulaDB = FileStorageUtility.GetExistFormulaDB();
             },
             () =>
             {
                 mainWindowVM.ExistStructureDB = FileStorageUtility.GetExistStructureDB();
             },
             () => {
                 if(param.IsUseEiFragmentDB)
                     mainWindowVM.EiFragmentDB = FileStorageUtility.GetEiFragmentDB();
             },
             () => {
                 mainWindowVM.MineStructureDB = FileStorageUtility.GetMinesStructureDB();
             }
             ,
             () => {
                 if (param.IsRunSpectralDbSearch == true) {
                     string errorMessage = string.Empty;
                     mainWindowVM.MspDB = FileStorageUtility.GetMspDB(param, out errorMessage);
                     if (errorMessage != string.Empty) {
                         MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                     }
                 }
             }
             //,
             //() =>
             //{
             //    MoleculeImage.TryClassLoad();
             //}
             );
            e.Result = new object[] { mainwindow, mainWindowVM };
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.window.Close();

            var arg = (object[])e.Result;
            var mainWindow = (MainWindow)arg[0];
            var mainWindowVM = (MainWindowVM)arg[1];

            mainWindow.IsEnabled = true;

            //if (mainWindowVM.ExistFormulaDB == null || mainWindowVM.QuickFormulaDB == null || mainWindowVM.NeutralLossDB == null) {
            if (mainWindowVM.ExistFormulaDB == null || mainWindowVM.NeutralLossDB == null) {
                    Application.Current.Shutdown();
            }

            if (mainWindowVM.DataStorageBean.ImportFolderPath != string.Empty)
                mainWindow.MainWindowVM.Refresh_ImportFolder(mainWindowVM.DataStorageBean.ImportFolderPath);
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        private void initilization(MainWindow mainWindow)
        {
            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);

            this.window = new ShortMessageWindow();
            this.window.Owner = mainWindow;
            this.window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.window.Show();
        }

    }
}
