using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class LipoqualityDatabaseFormatExportVM : ViewModelBase
    {
        private string exportFolderPath;
        private AlignmentFileBean selectedAlignmentFile;
        private ObservableCollection<AlignmentFileBean> alignmentFiles;

        #region
        [Required(ErrorMessage = "Choose a folder for the exported file.")]
        public string ExportFolderPath
        {
            get { return exportFolderPath; }
            set { if (exportFolderPath == value) return; exportFolderPath = value; OnPropertyChanged("ExportFolderPath"); }
        }

        public AlignmentFileBean SelectedAlignmentFile
        {
            get { return selectedAlignmentFile; }
            set { if (selectedAlignmentFile == value) return; selectedAlignmentFile = value; OnPropertyChanged("SelectedAlignmentFile"); }
        }

        public ObservableCollection<AlignmentFileBean> AlignmentFiles
        {
            get { return alignmentFiles; }
            set { if (alignmentFiles == value) return; alignmentFiles = value; OnPropertyChanged("AlignmentFiles"); }
        }
        #endregion

        /// <summary>
        /// Sets up the view model for the MrmprobsExport window in InvokeCommandAction
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded
        {
            get
            {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }

        private void Window_Loaded(object obj)
        {
            var view = (LipoqualityDatabaseFormatExportWin)obj;
            var mainWindow = (MainWindow)view.Owner;
            AlignmentFiles = mainWindow.AlignmentFiles;

            if (this.alignmentFiles == null || this.alignmentFiles.Count == 0) return;
            SelectedAlignmentFile = this.alignmentFiles[0];

            var alignmentResultID = mainWindow.FocusedAlignmentFileID;
            if (alignmentResultID < 0) return;
            if (alignmentResultID > this.alignmentFiles.Count - 1) return;

            SelectedAlignmentFile = this.alignmentFiles[alignmentResultID];
        }

        /// <summary>
        /// Opens the folder selection dialog and sets the value to the ExportFolderPath field
        /// </summary>
        private DelegateCommand selectDestinationFolder;
        public DelegateCommand SelectDestinationFolder
        {
            get
            {
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

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                ExportFolderPath = fbd.SelectedPath;
            }
            ExportResultAsLipoqualityFormat.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Closes the window (on Cancel)
        /// </summary>
        private DelegateCommand closeExportWindow;
        public DelegateCommand CloseExportWindow
        {
            get
            {
                return closeExportWindow ?? (closeExportWindow = new DelegateCommand(obj => {
                    Window view = (Window)obj;
                    view.Close();
                }, obj => { return true; }));
            }
        }

        /// <summary>
        /// Saves the Peak list and closes the window
        /// </summary>
        private DelegateCommand exportResultAsLipoqualityFormat;
        public DelegateCommand ExportResultAsLipoqualityFormat
        {
            get
            {
                return exportResultAsLipoqualityFormat ?? (exportResultAsLipoqualityFormat = new DelegateCommand(winobj => {

                    var view = (LipoqualityDatabaseFormatExportWin)winobj;
                    var mainWindow = (MainWindow)view.Owner;
                    var selectedAlignmentFileID = this.selectedAlignmentFile.FileID;

                    //DataExportLcUtility.ExportAsLipoqualityDatabaseFormat(this.exportFolderPath, mainWindow, selectedAlignmentFileID);
                    DataExportLcUtility.ExportAsLipoqualityDatabaseFormatVS2(this.exportFolderPath, mainWindow, selectedAlignmentFileID);

                    view.Close();
                }, CanExportLipoqualityFormat));
            }
        }

        /// <summary>
        /// Checks whether the exportMrmprobsRef command can be executed or not
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CanExportLipoqualityFormat(object arg)
        {
            if (this.HasViewError) return false;
            else return true;
        }
    }
}
