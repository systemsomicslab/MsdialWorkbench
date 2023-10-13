using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.StructureFinder.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class RefreshUtility
    {
        private RefreshUtility() { }

        public static void RefreshAll(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            RawDataFileRefresh(mainWindowVM, mainWindowVM.SelectedRawFileId);
            FormulaDataFileRefresh(mainWindow, mainWindowVM, mainWindowVM.SelectedRawFileId);
            StructureDataFileRefresh(mainWindow, mainWindowVM, mainWindowVM.SelectedRawFileId);
            RawDataMassSpecUiRefresh(mainWindow, mainWindowVM);
        }

        public static void RawDataFileRefresh(MainWindowVM mainWindowVM, int fileID)
        {
            if (fileID < 0) return;

            Mouse.OverrideCursor = Cursors.Wait;

            var filePath = mainWindowVM.DataStorageBean.QueryFiles[fileID].RawDataFilePath;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;

            mainWindowVM.DataStorageBean.RawData = RawDataParcer.RawDataFileReader(filePath, param);
            mainWindowVM.RawDataVM = new RawDataVM(mainWindowVM.DataStorageBean.RawData, mainWindowVM.AdductPositiveResources, mainWindowVM.AdductNegativeResources);
            if (mainWindowVM.DataStorageBean.RawData == null || mainWindowVM.DataStorageBean.RawData.Name == null) {
                MessageBox.Show("Error in parsing your msp/mat file, and please check the format.", "Error", MessageBoxButton.OK);
                Mouse.OverrideCursor = null;
                return;
            }

            Mouse.OverrideCursor = null;
        }

        public static void RawDataMassSpecUiRefresh(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            RefreshMS1Spectrum(mainWindow, mainWindowVM);
            RefrechMS2Spectrum(mainWindow, mainWindowVM);
        }

        public static void FormulaDataFileRefresh(MainWindow mainWindow, MainWindowVM mainWindowVM, int fileID)
        {
            if (fileID < 0) return;

            Mouse.OverrideCursor = Cursors.Wait;

            var dataStorageBean = mainWindowVM.DataStorageBean;
            var rawData = mainWindowVM.RawDataVM.RawData;
            var filePath = dataStorageBean.QueryFiles[fileID].FormulaFilePath;
            var error = string.Empty;

            if (System.IO.File.Exists(filePath))
            {
                dataStorageBean.FormualResults = FormulaResultParcer.FormulaResultReader(filePath, out error);
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }

                mainWindowVM.FormulaResultVMs = DataAccessUtility.GetFormulaVmList(dataStorageBean.FormualResults, rawData);
                if (mainWindowVM.FormulaResultVMs != null && mainWindowVM.FormulaResultVMs.Count != 0) mainWindowVM.SelectedFormulaVM = mainWindowVM.FormulaResultVMs[0];
            }
            else
            {
                dataStorageBean.FormualResults = null;
                mainWindowVM.FormulaResultVMs = null;
                mainWindowVM.SelectedFormulaVM = null;
            }
            
            mainWindow.DataGrid_FormulaResult.UpdateLayout();

            Mouse.OverrideCursor = null;
        }

        public static void StructureDataFileRefresh(MainWindow mainWindow, MainWindowVM mainWindowVM, int fileID)
        {
            if (fileID < 0) return;

            var formulaVM = mainWindowVM.SelectedFormulaVM;
            if (formulaVM == null) return;
            
            Mouse.OverrideCursor = Cursors.Wait;

            var dataStorageBean = mainWindowVM.DataStorageBean;
            var file = mainWindowVM.DataStorageBean.QueryFiles[fileID];
            var filePath = getStructureDataFilePath(file, formulaVM.Formula);

            if (System.IO.File.Exists(filePath))
            {
                dataStorageBean.FragmenterResults = FragmenterResultParcer.FragmenterResultReader(filePath);

                mainWindowVM.FragmenterResultVMs = DataAccessUtility.GetFragmenterResultVMs(dataStorageBean.FragmenterResults);
                if (mainWindowVM.FragmenterResultVMs != null && mainWindowVM.FragmenterResultVMs.Count != 0)
                {
                    mainWindowVM.SelectedFragmenterVM = mainWindowVM.FragmenterResultVMs[0];
                }
            }
            else
            {
                dataStorageBean.FragmenterResults = null;
                mainWindowVM.FragmenterResultVMs = null;
                mainWindowVM.SelectedFragmenterVM = null;
            }

            ActualMsMsVsInSilicoMsMsUiRefresh(mainWindow, mainWindowVM);
            mainWindow.DataGrid_FragmenterResult.UpdateLayout();

            Mouse.OverrideCursor = null;
        }

        public static void ActualMsMsVsInSilicoMsMsUiRefresh(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            var rawDataVM = mainWindowVM.RawDataVM;
            var fragmenterResultVM = mainWindowVM.SelectedFragmenterVM;

            mainWindow.ActuralVsTheoreticalSpectrumUI.Content = new MassSpectrogramWithReferenceUI(UiAccessUtility.GetActuralMsMsVsInSilicoMsMs(mainWindowVM));
        }

        private static string getStructureDataFilePath(MsfinderQueryFile file, string formula)
        {
            return Path.Combine(file.StructureFolderPath, formula + "." + SaveFileFormat.sfd);
        }


        public static void RefrechMS2Spectrum(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            if (mainWindow.ToggleButton_ShowMs2RawSpectrum.IsChecked == true)
            {
                mainWindow.Ms2RawSpectrumUI.Content = new MassSpectrogramUI(UiAccessUtility.GetMs2MassSpectrogramVM(mainWindowVM));
            }
            else if (mainWindow.ToggleButton_ShowProductIonSpectrum.IsChecked == true)
            {
                mainWindow.Ms2RawSpectrumUI.Content = new MassSpectrogramUI(UiAccessUtility.GetProductIonSpectrumVM(mainWindowVM));
            }
            else if (mainWindow.ToggleButton_ShowNeutralLossSpectrum.IsChecked == true)
            {
                mainWindow.Ms2RawSpectrumUI.Content = new MassSpectrogramUI(UiAccessUtility.GetNeutralLossSpectrumVM(mainWindowVM));
            }
        }

        public static void RefreshMS1Spectrum(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            if (mainWindow.ToggleButton_ShowMs1RawSpectrum.IsChecked == true)
            {
                mainWindow.Ms1RawSpectrumUI.Content = new MassSpectrogramUI(UiAccessUtility.GetMs1MassSpectrogramVM(mainWindowVM));
            }
            else if (mainWindow.ToggleButton_ShowIsotopeSpectrum.IsChecked == true)
            {
                mainWindow.Ms1RawSpectrumUI.Content = new MassSpectrogramUI(UiAccessUtility.GetIsotopeSpectrumVM(mainWindowVM));
            }
        }

        public static void UpdateRawDataFile(string filePath, RawDataVM rawDataVM, RawData rawData) {
            if (rawDataVM == null || rawData == null || rawData.Name == null) {
                //MessageBox.Show("Error in parsing your msp/mat file, and please check the format.", "Error", MessageBoxButton.OK);
                return;
            }
            if (!rawDataVM.Name.Equals(rawData.Name)) return;

            if (rawDataVM.IonMode != rawData.IonMode || rawDataVM.PrecursorMz != rawData.PrecursorMz || 
                rawDataVM.PrecursorType != rawData.PrecursorType || rawDataVM.SpectrumType != rawData.SpectrumType ||
                rawDataVM.CollisionEnergy != rawData.CollisionEnergy || rawDataVM.Formula != rawData.Formula || rawDataVM.Ontology != rawData.Ontology ||
                rawDataVM.Smiles != rawData.Smiles || rawDataVM.InchiKey != rawData.InchiKey || rawDataVM.IsMarked != rawData.IsMarked || rawDataVM.Comment != rawData.Comment) {
                rawData.IonMode = rawDataVM.IonMode;
                rawData.PrecursorMz = rawDataVM.PrecursorMz;
                rawData.PrecursorType = rawDataVM.PrecursorType;
                rawData.SpectrumType = rawDataVM.SpectrumType;
                rawData.CollisionEnergy = rawDataVM.CollisionEnergy;
                rawData.Formula = rawDataVM.Formula;
                rawData.Ontology = rawDataVM.Ontology;
                rawData.Smiles = rawDataVM.Smiles;
                rawData.InchiKey = rawDataVM.InchiKey;
                rawData.Comment = rawDataVM.Comment;
                rawData.IsMarked = rawDataVM.IsMarked;
                RawDataParcer.RawDataFileWriter(filePath, rawData);
            }
        }

        public static void UpdateFormulaDataFile(string filePath, List<FormulaVM> formulaVMs, List<FormulaResult> formulaResults) {
            if (formulaVMs == null || formulaResults == null || formulaVMs.Count == 0 || formulaResults.Count == 0) return;
            if (formulaVMs.Count != formulaResults.Count) return;

            bool flg = false;
            for (int i = 0; i < formulaVMs.Count; i++) {
                if (formulaVMs[i].Formula == null || formulaResults[i].Formula == null) return;
                if (!formulaVMs[i].Formula.Equals(formulaResults[i].Formula.FormulaString)) return;
                if (formulaVMs[i].IsSelected != formulaResults[i].IsSelected) {
                    formulaResults[i].IsSelected = formulaVMs[i].IsSelected;
                    flg = true;
                }
            }

            if (flg) {
                FormulaResultParcer.FormulaResultsWriter(filePath, formulaResults);
            }
        }

        public static void UpdataFiles(MainWindowVM mainWindowVM, int fileID) {
            var files = mainWindowVM.DataStorageBean.QueryFiles;

            if (files == null || fileID < 0 || fileID > files.Count - 1) return;

            var rawFilePath = files[fileID].RawDataFilePath;
            var rawDataVM = mainWindowVM.RawDataVM;
            var rawData = mainWindowVM.DataStorageBean.RawData;


            UpdateRawDataFile(rawFilePath, rawDataVM, rawData);

            var formulaFilePath = files[fileID].FormulaFilePath;
            var formulaResultVMs = mainWindowVM.FormulaResultVMs;
            var formulaResults = mainWindowVM.DataStorageBean.FormualResults;

            UpdateFormulaDataFile(formulaFilePath, formulaResultVMs, formulaResults);
        }

      
    }
}
