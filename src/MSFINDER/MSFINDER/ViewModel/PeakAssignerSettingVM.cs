using Riken.Metabolomics.MsfinderCommon.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class PeakAssignerSettingVM : ViewModelBase
    {
        private Window window;
        private MainWindow mainWindow;
        private MainWindowVM mainWindowVM;
        private ObservableCollection<MsfinderQueryFile> analysisFiles;
        private RawDataVM rawDataVM;

        public PeakAssignerSettingVM(Window window, MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            this.window = window;
            this.mainWindow = mainWindow;
            this.mainWindowVM = mainWindowVM;
            this.analysisFiles = mainWindowVM.AnalysisFiles;

            if (mainWindowVM.SelectedRawFileId >= 0 && mainWindowVM.RawDataVM != null)
            {
                this.rawDataVM = this.mainWindowVM.RawDataVM;
            }
            else
            {
                selectionFileChanged(0);
            }
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            Mouse.OverrideCursor = Cursors.Wait;
            
            PeakAssignerCurrentSearch.Process(this.mainWindowVM);

            this.mainWindow.DataGrid_FormulaResult.UpdateLayout();
            this.mainWindow.DataGrid_FragmenterResult.UpdateLayout();
            
            Mouse.OverrideCursor = null;

            this.window.Close();
        }

        private void selectionFileChanged(int fileID)
        {
            this.mainWindowVM.SelectedRawFileId = fileID;
            RefreshUtility.RawDataFileRefresh(this.mainWindowVM, this.mainWindowVM.SelectedRawFileId);
            RawDataVM = this.mainWindowVM.RawDataVM;
        }

        public RawDataVM RawDataVM
        {
            get { return this.rawDataVM; }
            set { this.rawDataVM = value; OnPropertyChanged("RawDataVM"); }
        }

        [Required]
        public string Formula
        {
            get { return this.rawDataVM.Formula; }
            set { this.rawDataVM.Formula = value; OnPropertyChanged("Formula"); }
        }

        [Required]
        public string Smiles
        {
            get { return this.rawDataVM.Smiles; }
            set { this.rawDataVM.Smiles = value; OnPropertyChanged("Smiles"); }
        }

        public int SelectedFileID
        {
            get { return this.mainWindowVM.SelectedRawFileId; }
            set { this.mainWindowVM.SelectedRawFileId = value; OnPropertyChanged("SelectedFileID"); selectionFileChanged(this.mainWindowVM.SelectedRawFileId); }
        }

        public ObservableCollection<MsfinderQueryFile> AnalysisFiles
        {
            get { return this.analysisFiles; }
        }
    }
}
