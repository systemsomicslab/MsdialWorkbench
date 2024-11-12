using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Rfx.Riken.OsakaUniv
{
	public class PeaklistExportVM : ViewModelBase
    {
		private MainWindow mainWindow;
		private string exportFolderPath;
        private ObservableCollection<AnalysisFileBean> analysisFileBeanCollection;
        private ObservableCollection<AnalysisFileBean> selectedAnalysisFileBeanCollection;
        private ProjectPropertyBean projectPropertyBean;
        private ObservableCollection<ExportSpectraFileFormat> exportSpectraFileFormats;
        private ObservableCollection<ExportspectraType> exportSpectraTypes;
		private ExportSpectraFileFormat selectedFileFormat;
		private ExportspectraType selectedSpectraType;
        private float isotopeExportMax;

		private ObservableCollection<AnalysisFileBean> selectedFrom = new ObservableCollection<AnalysisFileBean>();
		private ObservableCollection<AnalysisFileBean> selectedTo = new ObservableCollection<AnalysisFileBean>();

		#region properties
		/// <summary>
		/// Folder where the export function will save the selected files
		/// </summary>
		[Required(ErrorMessage = "Choose a folder for the exported files.")]
		public string ExportFolderPath
		{
			get { return exportFolderPath; }
			set { if (exportFolderPath == value) return; exportFolderPath = value; OnPropertyChanged("ExportFolderPath"); }
		}

		/// <summary>
		/// Available files to be exported
		/// </summary>
		public ObservableCollection<AnalysisFileBean> AnalysisFileBeanCollection
		{
			get { return analysisFileBeanCollection ?? (analysisFileBeanCollection = new ObservableCollection<AnalysisFileBean>()); }
			set {
				if (analysisFileBeanCollection == value)
					return;

				analysisFileBeanCollection = value;
				OnPropertyChanged("AnalysisFileBeanCollection"); }
		}

		/// <summary>
		/// Files selected to be exported
		/// </summary>
		public ObservableCollection<AnalysisFileBean> SelectedAnalysisFileBeanCollection
		{
			get { return selectedAnalysisFileBeanCollection ?? (selectedAnalysisFileBeanCollection = new ObservableCollection<AnalysisFileBean>()); }
			set
			{
				if (selectedAnalysisFileBeanCollection == value)
					return;

				selectedAnalysisFileBeanCollection = value;
				OnPropertyChanged("SelectedAnalysisFileBeanCollection");
				exportPeakList.RaiseCanExecuteChanged();
			}
		}

		/// <summary>
		/// Files to be added to export listbox
		/// </summary>
		public ObservableCollection<AnalysisFileBean> SelectedFrom
		{
			get { return selectedFrom; }
			set
			{
				selectedFrom = value;
				OnPropertyChanged("SelectedFrom");
			}
		}

		/// <summary>
		/// Files to be removes from export listbox
		/// </summary>
		public ObservableCollection<AnalysisFileBean> SelectedTo
		{
			get { return selectedTo; }
			set
			{
				selectedTo = value;
				OnPropertyChanged("SelectedTo");
			}
		}

		public ObservableCollection<ExportspectraType> ExportSpectraTypes
		{
			get
			{
				return exportSpectraTypes ?? (exportSpectraTypes = new ObservableCollection<ExportspectraType>(Enum.GetValues(typeof(ExportspectraType)).Cast<ExportspectraType>()));
			}
			set
			{
				foreach (var item in value.ToList())
				{
					if (mainWindow.ProjectProperty.MethodType == MethodType.ddMSMS && item != ExportspectraType.deconvoluted)
					{
						exportSpectraTypes.Add(item);
					}
				}
				OnPropertyChanged("ExportSpectraTypes");
			}
		}

		public ObservableCollection<ExportSpectraFileFormat> ExportSpectraFileFormats
		{
			get { return exportSpectraFileFormats ?? (exportSpectraFileFormats = new ObservableCollection<ExportSpectraFileFormat>(Enum.GetValues(typeof(ExportSpectraFileFormat)).Cast<ExportSpectraFileFormat>())); }
			set
			{
				exportSpectraFileFormats = value; OnPropertyChanged("ExportSpectraFileFormats");
			}
		}

		public ExportSpectraFileFormat SelectedFileFormat
		{
			get { return selectedFileFormat; }
			set { if (selectedFileFormat == value) return; selectedFileFormat = value; OnPropertyChanged("SelectedFileFormat"); }
		}

		public ExportspectraType SelectedSpectraType
		{
			get { return selectedSpectraType; }
			set { if (selectedSpectraType == value) return; selectedSpectraType = value; OnPropertyChanged("SelectedSpectraType"); }
		}


        public float IsotopeExportMax {
            get {
                return isotopeExportMax;
            }

            set { if (isotopeExportMax == value) return; isotopeExportMax = value; OnPropertyChanged("IsotopeExportMax"); }
        }
        #endregion

        #region Commands
        /// <summary>
        /// Sets up the view model for the PeakListExport window
        /// </summary>
        private DelegateCommand windowLoaded;
		public DelegateCommand WindowLoaded
		{
			get
			{
				return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
			}
		}

		/// <summary>
		/// Action for the WindowLoaded command
		/// </summary>
		/// <param name="obj"></param>
		private void Window_Loaded(object obj)
		{
			var view = (PeaklistExportWin)obj;
			mainWindow = (MainWindow)view.Owner;
            IsotopeExportMax = 5.0F;
			projectPropertyBean = mainWindow.ProjectProperty;
			AnalysisFileBeanCollection = new ObservableCollection<AnalysisFileBean>(mainWindow.AnalysisFiles);
			AddAllItems.RaiseCanExecuteChanged();

			if (mainWindow.ProjectProperty.Ionization == Ionization.EI)
			{
                view.TextBox_IsotopeExportMax.IsEnabled = false;
				if (mainWindow.ProjectProperty.DataType == DataType.Centroid) {
					ExportSpectraTypes.Remove(ExportspectraType.profile);
					SelectedSpectraType = ExportspectraType.centroid;
				}
			} else if (mainWindow.ProjectProperty.Ionization == Ionization.ESI) {
				if (mainWindow.ProjectProperty.DataTypeMS2 == DataType.Centroid) {
					ExportSpectraTypes.Remove(ExportspectraType.profile);
					SelectedSpectraType = ExportspectraType.centroid;
				}
			}
		}

		/// <summary>
		/// Opens the folder selection dialog and sets the value to the ExportFolderPath field
		/// </summary>
		private DelegateCommand selectDestinationFolder;
		public DelegateCommand SelectDestinationFolder
		{
			get {
				return selectDestinationFolder ?? (selectDestinationFolder = new DelegateCommand(ShowFolderSelectionDialog, arguments => { return true; }));
			}
		}
		/// <summary>
		/// actual action for the SelectExportFolder command
		/// </summary>
		/// <param name="obj"></param>
		private void ShowFolderSelectionDialog(object obj)
		{
			System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
			fbd.RootFolder = Environment.SpecialFolder.Desktop;
			fbd.Description = "Choose a folder where to save the exported files.";
			fbd.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				ExportFolderPath = fbd.SelectedPath;
			}
			ExportPeakList.RaiseCanExecuteChanged();
		}

		/// <summary>
		/// Closes the window (on Cancel)
		/// </summary>
		private DelegateCommand closeExportWindow;
		public DelegateCommand CloseExportWindow
		{
			get {
				return closeExportWindow ?? (closeExportWindow = new DelegateCommand(obj => {
					Window view = (Window)obj;
					view.Close();
				}, obj => { return true; }));
			}
		}

		/// <summary>
		/// Saves the Peak list and closes the window
		/// </summary>
		private DelegateCommand exportPeakList;
		public DelegateCommand ExportPeakList
		{
			get {
				return exportPeakList ?? (exportPeakList = new DelegateCommand(winobj => {
					PeaklistExportWin view = (PeaklistExportWin)winobj;
					if (projectPropertyBean.Ionization == Ionization.ESI) {

						DataExportLcUtility.PeaklistExport((MainWindow)view.Owner, exportFolderPath, selectedAnalysisFileBeanCollection,
							SelectedFileFormat, SelectedSpectraType, isotopeExportMax);

						//DataExportLcUtility.PeaklistExportPrivate((MainWindow)view.Owner, exportFolderPath, selectedAnalysisFileBeanCollection,
						//    SelectedFileFormat, SelectedSpectraType, isotopeExportMax);
					}
					else {
						if (SelectedFileFormat == ExportSpectraFileFormat.ms) {
							MessageBox.Show("SIRIUS ms format is not supported for GC-MS project", "Error", MessageBoxButton.OK);
							return;
						}
						DataExportGcUtility.PeaklistExport((MainWindow)view.Owner, exportFolderPath, selectedAnalysisFileBeanCollection, SelectedFileFormat);
					}

					view.Close();
				}, CanExportPeaklist));
			}
		}
		
        /// <summary>
		/// Checks whether the exportPeaklist command can be executed or not
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		private bool CanExportPeaklist(object obj)
        {
            if (ExportFolderPath == null || ExportFolderPath == string.Empty) {
				Debug.WriteLine("Empty destination folder");
				return false;
			}
            if (SelectedAnalysisFileBeanCollection.Count == 0) {
				Debug.WriteLine("Nothing to export");
				return false;
			}

            return true;
        }

		/// <summary>
		/// Command to handle the selection of files in both listboxes
		/// </summary>
		public DelegateCommand SelectedCommand
		{
			get
			{
				return new DelegateCommand(SelectionChanged, obj => { return true; } );
			}
		}
		/// <summary>
		/// Action used in the SelectedCommand
		/// </summary>
		/// <param name="param"></param>
		private void SelectionChanged(object param)
		{
			var list = (ListBox)param;
			StringBuilder Msg = new StringBuilder();

			if (list.Name.Equals("AvailableFileList"))
			{
				Debug.WriteLine("clearing sel fr\nSelected item count: " + list.SelectedItems.Count);
				SelectedFrom = new ObservableCollection<AnalysisFileBean>();
			}
			else if (list.Name.Equals("ForExportFileList"))
			{
				Debug.WriteLine("clearing sel to\nSelected item count: " + list.SelectedItems.Count);
				SelectedTo = new ObservableCollection<AnalysisFileBean>();
			}
			foreach (AnalysisFileBean item in list.SelectedItems)
			{
				if (list.Name.Equals("AvailableFileList"))
				{
					Debug.WriteLine("added (" + item.AnalysisFilePropertyBean.AnalysisFileId + ") " + item.AnalysisFilePropertyBean.AnalysisFileName + " -> from");
					SelectedFrom.Add(item);
				}
				else if (list.Name.Equals("ForExportFileList"))
				{
					Debug.WriteLine("added (" + item.AnalysisFilePropertyBean.AnalysisFileId +") " + item.AnalysisFilePropertyBean.AnalysisFileName + " -> to");
					SelectedTo.Add(item);
				}
			}
			AddItems.RaiseCanExecuteChanged();
			DelItems.RaiseCanExecuteChanged();
		}

		/// <summary>
		/// Add the selected items to the list of files to be exported and removes them from the list of available files
		/// </summary>
		private DelegateCommand addItems;
		public DelegateCommand AddItems
		{
			get
			{
				return addItems ?? (addItems = new DelegateCommand(
				obj => {
					//SelectedAnalysisFileBeanCollection = new ObservableCollection<AnalysisFileBean>(SelectedAnalysisFileBeanCollection.Union(SelectedFrom, new AnalysisFileBeanComparer()));
					SelectedFrom.ToList().ForEach(delegate (AnalysisFileBean file) {
						if (!SelectedAnalysisFileBeanCollection.Contains(file))
						{
							SelectedAnalysisFileBeanCollection.Add(file);
							Debug.WriteLine("added file " + file.AnalysisFilePropertyBean.AnalysisFileName + " to selected");
						}
						Debug.WriteLine("removing file " + file.AnalysisFilePropertyBean.AnalysisFileName + " from available");
						AnalysisFileBeanCollection.Remove(file);
					});
					SelectedFrom = new ObservableCollection<AnalysisFileBean>();
					SelectedTo = new ObservableCollection<AnalysisFileBean>();
					SortDest();
				},
				selItms => {
					Debug.WriteLine("can add: " + (SelectedFrom.Count > 0 ? "yup " : "nope ") + SelectedFrom.Count.ToString());
					return (SelectedFrom.Count > 0);
				}));
			}
		}

		/// <summary>
		/// Add the selected items to the list of available files and removes them from the list of files to be exported
		/// </summary>
		private DelegateCommand delItems;
		public DelegateCommand DelItems
		{
			get
			{
				return delItems ?? (delItems = new DelegateCommand(
				obj => {
					//AnalysisFileBeanCollection = new ObservableCollection<AnalysisFileBean>(AnalysisFileBeanCollection.Union(SelectedTo, new AnalysisFileBeanComparer()));
					SelectedTo.ToList().ForEach(delegate (AnalysisFileBean file) {
						if (!AnalysisFileBeanCollection.Contains(file))
						{
							AnalysisFileBeanCollection.Add(file);
							Debug.WriteLine("added file " + file.AnalysisFilePropertyBean.AnalysisFileName + " to available selected");
						}
						Debug.WriteLine("removing file " + file.AnalysisFilePropertyBean.AnalysisFileName + " from selected");
						SelectedAnalysisFileBeanCollection.Remove(file);
					});
					SelectedFrom = new ObservableCollection<AnalysisFileBean>();
					SelectedTo = new ObservableCollection<AnalysisFileBean>();
					SortDest();
				},
				selItms => {
					Debug.WriteLine("can del: " + (SelectedTo.Count > 0 ? "yup " : "nope ") + SelectedTo.Count.ToString());
					return (SelectedTo.Count > 0);
				}));
			}
		}

		/// <summary>
		/// Add all the items to the list of files to be exported and removes them from the list of available files
		/// </summary>
		private DelegateCommand addAllItems;
		public DelegateCommand AddAllItems
		{
			get
			{
				return addAllItems ?? (addAllItems = new DelegateCommand(
				obj => {
					var list = (ListBox)obj;
					foreach(AnalysisFileBean item in list.Items)
					{
						SelectedAnalysisFileBeanCollection.Add(item);
					}
					AnalysisFileBeanCollection.Clear();
					//AddAllItems.RaiseCanExecuteChanged();
					DelAllItems.RaiseCanExecuteChanged();
                    ExportPeakList.RaiseCanExecuteChanged();
				},
				obj => {
					var list = (ListBox)obj;
					Debug.WriteLine(list.Name);
					return (list.Items.Count > 0);
				}));
			}
		}

		/// <summary>
		/// Add all the items to the list of available files and removes them from the list of files to be exported
		/// </summary>
		private DelegateCommand delAllItems;
		public DelegateCommand DelAllItems
		{
			get
			{
				return delAllItems ?? (delAllItems = new DelegateCommand(
				obj => {
					var list = (ListBox)obj;
					foreach (AnalysisFileBean item in list.Items)
					{
						AnalysisFileBeanCollection.Add(item);
					}
					SelectedAnalysisFileBeanCollection.Clear();
					AddAllItems.RaiseCanExecuteChanged();
					//DelAllItems.RaiseCanExecuteChanged();
                    ExportPeakList.RaiseCanExecuteChanged();
				},
				obj => {
					var list = (ListBox)obj;
					Debug.WriteLine(list.Name);
					return (list.Items.Count > 0);
				}));
			}
		}

        #endregion

        #region Helper Methods
        /// <summary>
        /// converts elements of Enum<T> in Array<string>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        private void GetEnumValues<T>(ref ObservableCollection<T> target) {

			FieldInfo[] info;

			Type type = typeof(T);

			info = type.GetFields(BindingFlags.Public | BindingFlags.Static);

			foreach (FieldInfo fi in info)
			{
				target.Add((T)Enum.Parse(type, fi.Name, true));
			}
		}

		/// <summary>
		/// helper method to sort the lists of files
		/// </summary>
		private void SortDest()
		{
			AnalysisFileBeanCollection = new ObservableCollection<AnalysisFileBean>(AnalysisFileBeanCollection.OrderBy(it => it.AnalysisFilePropertyBean.AnalysisFileName));
			SelectedAnalysisFileBeanCollection = new ObservableCollection<AnalysisFileBean>(SelectedAnalysisFileBeanCollection.OrderBy(it => it.AnalysisFilePropertyBean.AnalysisFileName));
		}
		#endregion
	}
}
