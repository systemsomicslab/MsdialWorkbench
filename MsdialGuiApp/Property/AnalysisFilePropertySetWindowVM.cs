using CompMs.App.Msdial.Common;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.Property
{
    class AnalysisFilePropertySetWindowVM : ViewModelBase
    {
        #region Property
        public MachineCategory MachineCategory { get; set; }
        public string ProjectFolderPath { get; set; }

        public ObservableCollection<AnalysisFileBean> AnalysisFilePropertyCollection {
            get => analysisFilePropertyCollection;
            set => SetProperty(ref analysisFilePropertyCollection, value);
        }


        #endregion

        #region Field
        private ObservableCollection<AnalysisFileBean> analysisFilePropertyCollection;
        #endregion

        public AnalysisFilePropertySetWindowVM() {
            AnalysisFilePropertyCollection = new ObservableCollection<AnalysisFileBean>();
            PropertyChanged += OnAnalysisFileChanged;
        }

        #region Command
        public DelegateCommand AnalysisFilesSelectCommand {
            get => analysisFilesSelectCommand ?? (analysisFilesSelectCommand = new DelegateCommand(AnalysisFilesSelect));
        }
        private DelegateCommand analysisFilesSelectCommand;

        private void AnalysisFilesSelect() {
            var ofd = new OpenFileDialog()
            {
                Title = "Import analysis files",
                InitialDirectory = ProjectFolderPath,
                RestoreDirectory = true,
                Multiselect = true,
            };
            if (MachineCategory == MachineCategory.LCIMMS || MachineCategory == MachineCategory.IMMS) {
                ofd.Filter = "IBF file(*.ibf)|*.ibf";
            }
            else {
                ofd.Filter = "ABF file(*.abf)|*.abf|mzML file(*.mzml)|*.mzml|netCDF file(*.cdf)|*.cdf|IBF file(*.ibf)|*.ibf";
            }

            if (ofd.ShowDialog() == true) {
                if (ofd.FileNames.Any(filename => Path.GetDirectoryName(filename) != ProjectFolderPath)) {
                    MessageBox.Show("The directory of analysis files should be where the project file is created.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;
                AnalysisFilePropertyCollection = ReadImportedFiles(ofd.FileNames);
                Mouse.OverrideCursor = null;
            }
        }

        private ObservableCollection<AnalysisFileBean> ReadImportedFiles(string[] filenames) {
            var dt = DateTime.Now;
            var analysisfiles = filenames.Select((filename, i) =>
                new AnalysisFileBean
                {
                    AnalysisFileAnalyticalOrder = i + 1,
                    AnalysisFileClass = "1",
                    AnalysisFileId = i,
                    AnalysisFileIncluded = true,
                    AnalysisFileName = Path.GetFileNameWithoutExtension(filename),
                    AnalysisFilePath = filename,
                    AnalysisFileType = AnalysisFileType.Sample,
                    AnalysisBatch = 1,
                    InjectionVolume = 1d,
                }
            ).ToList();
            foreach (var analysisfile in analysisfiles) {
                analysisfile.DeconvolutionFilePath = Path.Combine(ProjectFolderPath, analysisfile.AnalysisFileName + dt.ToString("_yyyyMMddHHmm")+ $".{MsdialDataStorageFormat.dcl}");
                analysisfile.PeakAreaBeanInformationFilePath = Path.Combine(ProjectFolderPath, analysisfile.AnalysisFileName + dt.ToString("_yyyyMMddHHmm") + $".{MsdialDataStorageFormat.pai}");
            }

            return new ObservableCollection<AnalysisFileBean>(analysisfiles);
        }

        public DelegateCommand<Window> ContinueProcessCommand {
            get => continueProcessCommand ?? (continueProcessCommand = new DelegateCommand<Window>(ContinueProcess, ValidateAnalysisFilePropertySetWindow));
        }
        private DelegateCommand<Window> continueProcessCommand;

        private void ContinueProcess(Window window) {
            window.DialogResult = true;
            window.Close();
        }

        private bool ValidateAnalysisFilePropertySetWindow(Window window) {
            if (HasViewError)
                return false;
            if (AnalysisFilePropertyCollection.IsEmptyOrNull())
                return false;
            var invalidChars = Path.GetInvalidFileNameChars();
            if (AnalysisFilePropertyCollection.Any(analysisfile => analysisfile.AnalysisFileName.IndexOfAny(invalidChars) >= 0))
                return false;
            return true;
        }

        public DelegateCommand<Window> CancelProcessCommand {
            get => cancelProcessCommand ?? (cancelProcessCommand = new DelegateCommand<Window>(CancelProcess));
        }
        private DelegateCommand<Window> cancelProcessCommand;

        private void CancelProcess(Window window) {
            window.DialogResult = false;
            window.Close();
        }
        #endregion

        #region Event
        private void OnAnalysisFileChanged(object sender, PropertyChangedEventArgs e) {
            ContinueProcessCommand.RaiseCanExecuteChanged();
        }
        #endregion

    }
}
