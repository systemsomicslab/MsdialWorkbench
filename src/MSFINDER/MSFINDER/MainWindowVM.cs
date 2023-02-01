using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.MsfinderCommon.Utility;
using Riken.Metabolomics.StructureFinder.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv {
    public class MainWindowVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private MsfinderQueryStorage dataStorageBean;
        public DateTime BatchJobStartTimeStamp { get; set; }

		private List<AdductIon> adductPositiveResources;
		private List<AdductIon> adductNegativeResources;
		//private List<Formula> quickFormulaDB;
        private List<ProductIon> productIonDB;
        private List<NeutralLoss> neutralLossDB;
        private List<FragmentOntology> fragmentOntologyDB;
        private List<ExistFormulaQuery> existFormulaDB;
        private List<ExistStructureQuery> existStructureDB;
        private List<ExistStructureQuery> userDefinedStructureDB;
        private List<ExistStructureQuery> mineStructureDB;
        private List<FragmentLibrary> eiFragmentDB;
        private List<ChemicalOntology> chemicalOntologies;
        private List<MspFormatCompoundInformationBean> mspDB;

        private int selectedRawFileId;
        private int selectedFormulaFileId;
        private int selectedStructureFileId;

        private RawDataVM rawDataVM;

        private List<FormulaVM> formulaResultVMs;
        private FormulaVM selectedFormulaVM;

        private List<FragmenterResultVM> fragmenterResultVMs;
        private FragmenterResultVM selectedFragmenterVM;

        public MainWindowVM(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            this.dataStorageBean = new MsfinderQueryStorage();
            this.selectedRawFileId = -1;
            this.selectedFormulaFileId = -1;
            this.selectedStructureFileId = -1;
			this.adductPositiveResources = AdductListParcer.GetAdductPositiveResources();
			this.adductNegativeResources = AdductListParcer.GetAdductNegativeResources();
		}

		#region // Properties
		public MsfinderQueryStorage DataStorageBean
        {
            get { return dataStorageBean; }
            set { dataStorageBean = value; }
        }

        public ObservableCollection<MsfinderQueryFile> AnalysisFiles
        {
            get { return dataStorageBean.QueryFiles; }
            set { dataStorageBean.QueryFiles = value; OnPropertyChanged("AnalysisFiles"); }
        }

        //public List<Formula> QuickFormulaDB
        //{
        //    get { return quickFormulaDB; }
        //    set { quickFormulaDB = value; }
        //}

        public List<ProductIon> ProductIonDB
        {
            get { return productIonDB; }
            set { productIonDB = value; }
        }

        public List<NeutralLoss> NeutralLossDB
        {
            get { return neutralLossDB; }
            set { neutralLossDB = value; }
        }

        public List<FragmentOntology> FragmentOntologyDB
        {
            get { return fragmentOntologyDB; }
            set { fragmentOntologyDB = value; }
        }

        public List<ExistFormulaQuery> ExistFormulaDB
        {
            get { return existFormulaDB; }
            set { existFormulaDB = value; }
        }

        public List<ExistStructureQuery> ExistStructureDB
        {
            get { return existStructureDB; }
            set { existStructureDB = value; }
        }

        public List<ExistStructureQuery> UserDefinedStructureDB
        {
            get { return userDefinedStructureDB; }
            set { userDefinedStructureDB = value; }
        }

        public List<ExistStructureQuery> MineStructureDB
        {
            get { return mineStructureDB; }
            set { mineStructureDB = value; }
        }

        public List<FragmentLibrary> EiFragmentDB
        {
            get { return eiFragmentDB; }
            set { eiFragmentDB = value; }
        }

        public List<MspFormatCompoundInformationBean> MspDB
        {
            get { return mspDB; }
            set { mspDB = value; }
        }

        public int SelectedRawFileId
        {
            get { return selectedRawFileId; }
            set { if (selectedRawFileId == value) return; selectedRawFileId = value; OnPropertyChanged("SelectedRawFileId"); }
        }

        public int SelectedFormulaFileId
        {
            get { return selectedFormulaFileId; }
            set { if (selectedFormulaFileId == value) return; selectedFormulaFileId = value; OnPropertyChanged("SelectedFormulaFileId"); }
        }

        public int SelectedStructureFileId
        {
            get { return selectedStructureFileId; }
            set { if (selectedStructureFileId == value) return; selectedStructureFileId = value; OnPropertyChanged("SelectedStructureFileId"); }
        }

        public RawDataVM RawDataVM
        {
            get { return rawDataVM; }
            set { rawDataVM = value; OnPropertyChanged("RawDataVM"); }
        }

        public List<FormulaVM> FormulaResultVMs
        {
            get { return formulaResultVMs; }
            set { formulaResultVMs = value; OnPropertyChanged("FormulaResultVMs"); }
        }

        public FormulaVM SelectedFormulaVM
        {
            get { return selectedFormulaVM; }
            set { selectedFormulaVM = value; }
        }

        public List<FragmenterResultVM> FragmenterResultVMs
        {
            get { return fragmenterResultVMs; }
            set { fragmenterResultVMs = value; OnPropertyChanged("FragmenterResultVMs"); }
        }

        public FragmenterResultVM SelectedFragmenterVM
        {
            get { return selectedFragmenterVM; }
            set { selectedFragmenterVM = value; OnPropertyChanged("SelectedFragmenterVM"); }
        }

		public List<AdductIon> AdductPositiveResources
		{
			get { return adductPositiveResources; }
			set { adductPositiveResources = value; }
		}

		public List<AdductIon> AdductNegativeResources
		{
			get { return adductNegativeResources; }
			set { adductNegativeResources = value; }
		}

        public List<ChemicalOntology> ChemicalOntologies {
            get { return chemicalOntologies; }
            set { chemicalOntologies = value; }
        }
        #endregion

        #region // Refresh
        public void Refresh_ImportFolder(string folderPath)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            this.DataStorageBean = new MsfinderQueryStorage(folderPath, this.DataStorageBean.AnalysisParameter);
            if (ErrorHanling_ImportedFileChecker())
            {
                FileNavigatorRefresh(folderPath);
            }
            Mouse.OverrideCursor = null;
        }
        #endregion

        #region // Events
        public void FileNavigatorRefresh(string folderPath)
        {
            this.mainWindow.Title = this.mainWindow.MainWindowTitle + " " + folderPath;

            this.mainWindow.ListBox_FileName.PreviewMouseDoubleClick -= listBox_FileName_PreviewMouseDoubleClick;
            this.mainWindow.ListBox_FileName.SelectionChanged -= listBox_FileName_SelectionChanged;

            this.mainWindow.ListBox_FileName.PreviewMouseDoubleClick += listBox_FileName_PreviewMouseDoubleClick;
            this.mainWindow.ListBox_FileName.SelectionChanged += listBox_FileName_SelectionChanged;

            this.mainWindow.ListBox_FileName.DisplayMemberPath = "RawDataFileName";
            this.mainWindow.ListBox_FileName.ItemsSource = this.dataStorageBean.QueryFiles;

            this.mainWindow.ListBox_FileName.UpdateLayout();
        }

        private void listBox_FileName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshUtility.UpdataFiles(this, this.selectedRawFileId);
            this.selectedRawFileId = ((ListBox)sender).SelectedIndex;

            RefreshUtility.RawDataFileRefresh(this, this.selectedRawFileId);
            RefreshUtility.FormulaDataFileRefresh(this.mainWindow, this, this.selectedRawFileId);
            RefreshUtility.StructureDataFileRefresh(this.mainWindow, this, this.selectedRawFileId);
            RefreshUtility.RawDataMassSpecUiRefresh(this.mainWindow, this);
        }

        private void listBox_FileName_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            this.SelectedRawFileId = ((ListBox)sender).SelectedIndex;
            var param = this.dataStorageBean.AnalysisParameter;

            FormulaFinderCurrentSearch.Process(this);
            if (param.IsRunInSilicoFragmenterSearch == false && param.IsRunSpectralDbSearch == true)
            {
                var process = new StructureFinderCurrentSearch();
                process.Process(this.mainWindow, this);
            }

            this.mainWindow.DataGrid_FormulaResult.UpdateLayout();

            Mouse.OverrideCursor = null;
        }
        #endregion

        #region // Error handling methods
        public bool ErrorHanling_ImportedFileChecker()
        {
            if (!(this.dataStorageBean.QueryFiles.Count == 0))
            {
                return true;
            }
            else
            {
                MessageBox.Show("There is no file information in this folder. Please select the folder including MAT files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.dataStorageBean.QueryFiles = new ObservableCollection<MsfinderQueryFile>();
                this.dataStorageBean.ImportFolderPath = string.Empty;
                return false;
            }
        }
        #endregion

    }
}
