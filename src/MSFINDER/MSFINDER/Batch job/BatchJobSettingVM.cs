using Riken.Metabolomics.MsfinderCommon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class BatchJobSettingVM: ViewModelBase
    {
        private Window window;
        private MainWindowVM mainWindowVM;
        private AnalysisParamOfMsfinder param;
        private AnalysisParamOfMsfinder copyPram;

        public BatchJobSettingVM(Window window, MainWindowVM mainWindowVM) 
        { 
            this.window = window;
            this.mainWindowVM = mainWindowVM; 
            this.param = this.mainWindowVM.DataStorageBean.AnalysisParameter;

            this.copyPram = new AnalysisParamOfMsfinder();
            this.copyPram.IsAllProcess = this.param.IsAllProcess;
            this.copyPram.IsFormulaFinder = this.param.IsFormulaFinder;
            this.copyPram.IsStructureFinder = this.param.IsStructureFinder;
            this.copyPram.TryTopNmolecularFormulaSearch = this.param.TryTopNmolecularFormulaSearch;
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);
            var errorMessage = string.Empty;
            if (!FileStorageUtility.IsLibrariesImported(this.param, 
                this.mainWindowVM.ExistStructureDB, this.mainWindowVM.MineStructureDB, this.mainWindowVM.UserDefinedStructureDB, out errorMessage)) {
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.param.IsAllProcess = this.copyPram.IsAllProcess;
            this.param.IsFormulaFinder = this.copyPram.IsFormulaFinder;
            this.param.IsStructureFinder = this.copyPram.IsStructureFinder;
            this.param.TryTopNmolecularFormulaSearch = this.copyPram.TryTopNmolecularFormulaSearch;

            MsFinderIniParcer.Write(this.param);

            this.window.DialogResult = true;
            this.window.Close();
        }

        

        public bool IsAllProcess
        {
            get { return this.copyPram.IsAllProcess; }
            set 
            {
                if (this.copyPram.IsAllProcess == value) return; this.copyPram.IsAllProcess = value; OnPropertyChanged("IsAllProcess");

                if (value == true)
                {
                    IsFormulaFinder = true;
                    IsStructureFinder = true;
                }
            }
        }

        public bool IsFormulaFinder
        {
            get { return this.copyPram.IsFormulaFinder; }
            set 
            { 
                if (this.copyPram.IsFormulaFinder == value) return; this.copyPram.IsFormulaFinder = value; OnPropertyChanged("IsFormulaFinder");

                if (value == false)
                {
                    IsAllProcess = false;
                }
            }
        }

        public bool IsStructureFinder
        {
            get { return this.copyPram.IsStructureFinder; }
            set 
            { 
                if (this.copyPram.IsStructureFinder == value) return; this.copyPram.IsStructureFinder = value; OnPropertyChanged("IsStructureFinder");

                if (value == false)
                {
                    IsAllProcess = false;
                }
            }
        }

        public int TopN
        {
            get { return this.copyPram.TryTopNmolecularFormulaSearch; }
            set { if (this.copyPram.TryTopNmolecularFormulaSearch == value) return; this.copyPram.TryTopNmolecularFormulaSearch = value; OnPropertyChanged("TopN"); }
        }
    }
}
