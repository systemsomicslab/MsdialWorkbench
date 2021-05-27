using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.CommonMVVM.Validator;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Export
{
    class AnalysisResultExportViewModel : ViewModelBase
    {
        public AnalysisResultExportViewModel(
            IEnumerable<AnalysisFileBean> files,
            IEnumerable<SpectraType> spectraTypes,
            IEnumerable<SpectraFormat> spectraFormats,
            IDataProviderFactory<AnalysisFileBean> providerFactory)
            : this(new AnalysisResultExportModel(files, spectraTypes, spectraFormats, providerFactory)) {

        }

        public AnalysisResultExportViewModel(AnalysisResultExportModel model) {

            Model = model;

            SelectedFrom = Model.UnSelectedFiles.ToMappedReadOnlyObservableCollection(file => new FileBeanSelection(file));
            Disposables.Add(SelectedFrom);
            SelectedTo = Model.SelectedFiles.ToMappedReadOnlyObservableCollection(file => new FileBeanSelection(file));
            Disposables.Add(SelectedTo);

            ExportSpectraTypes = new ReadOnlyObservableCollection<SpectraType>(Model.ExportSpectraTypes);
            ExportSpectraFileFormats = new ReadOnlyObservableCollection<SpectraFormat>(Model.ExportSpectraFileFormats);

            var notifir = new PropertyChangedNotifier(Model);
            Disposables.Add(notifir);
            notifir
                .SubscribeTo(nameof(Model.SelectedSpectraType), () => SelectedSpectraType = Model.SelectedSpectraType)
                .SubscribeTo(nameof(Model.SelectedFileFormat), () => SelectedFileFormat = Model.SelectedFileFormat)
                .SubscribeTo(nameof(Model.IsotopeExportMax), () => IsotopeExportMax = Model.IsotopeExportMax);

            SelectedSpectraType = Model.SelectedSpectraType;
            SelectedFileFormat = Model.SelectedFileFormat;
            DestinationFolder = Model.DestinationFolder;
        }

        public AnalysisResultExportModel Model { get; }

        public DelegateCommand ExportPeakCommand => exportPeakCommand ?? (exportPeakCommand = new DelegateCommand(ExportPeak, CanExportPeak));
        private DelegateCommand exportPeakCommand;

        private void ExportPeak() {
            result = Task.Run(() => Model.Export());
            ExportPeakCommand.RaiseCanExecuteChanged();
        }
        private Task result;

        private bool CanExportPeak() {
            return result?.Status != TaskStatus.Running && !HasErrors;
        }

        public MappedReadOnlyObservableCollection<AnalysisFileBean, FileBeanSelection> SelectedFrom { get; }

        public MappedReadOnlyObservableCollection<AnalysisFileBean, FileBeanSelection> SelectedTo { get; }

        public DelegateCommand AddItemsCommand => addItemsCommand ?? (addItemsCommand = new DelegateCommand(AddItems));
        private DelegateCommand addItemsCommand;

        private void AddItems() {
            Model.Selects(SelectedFrom.Where(file => file.IsChecked).Select(file => file.File).ToList());
        }

        public DelegateCommand AddAllItemsCommand => addAllItemsCommand ?? (addAllItemsCommand = new DelegateCommand(AddAllItems));
        private DelegateCommand addAllItemsCommand;

        private void AddAllItems() {
            Model.Selects(SelectedFrom.Select(file => file.File).ToList());
        }

        public DelegateCommand RemoveItemsCommand => removeItemsCommand ?? (removeItemsCommand = new DelegateCommand(RemoveItems));
        private DelegateCommand removeItemsCommand;

        private void RemoveItems() {
            Model.UnSelects(SelectedTo.Where(file => file.IsChecked).Select(file => file.File).ToList());
        }

        public DelegateCommand RemoveAllItemsCommand => removeAllItemsCommand ?? (removeAllItemsCommand = new DelegateCommand(RemoveAllItems));
        private DelegateCommand removeAllItemsCommand;

        private void RemoveAllItems() {
            Model.UnSelects(SelectedTo.Select(file => file.File).ToList());
        }

        public DelegateCommand SelectDestinationCommand => selectDestinationCommand ?? (selectDestinationCommand = new DelegateCommand(SelectDestination));
        private DelegateCommand selectDestinationCommand;

        [Required(ErrorMessage = "Choose a folder for the exported files.")]
        [PathExists(ErrorMessage = "Choose an existing folder", IsDirectory = true)]
        public string DestinationFolder {
            get {
                return destinationFolder;
            }

            set {
                if (SetProperty(ref destinationFolder, value)) {
                    if (!ContainsError(nameof(DestinationFolder))) {
                        Model.DestinationFolder = destinationFolder;
                    }
                }
            }
        }

        private string destinationFolder;

        public void SelectDestination() {
            var fbd = new Graphics.Window.SelectFolderDialog
            {
                Title = "Chose a export folder.",
            };
            if (fbd.ShowDialog() == Graphics.Window.DialogResult.OK) {
                DestinationFolder = fbd.SelectedPath;
            }
        }

        public ReadOnlyObservableCollection<SpectraType> ExportSpectraTypes { get; }

        public ReadOnlyObservableCollection<SpectraFormat> ExportSpectraFileFormats { get; }

        [Required(ErrorMessage = "Choose a spectra type.")]
        public SpectraType SelectedSpectraType {
            get {
                return selectedSpectraType;
            }

            set {
                if (SetProperty(ref selectedSpectraType, value)) {
                    if (!ContainsError(nameof(SelectedSpectraType))) {
                        Model.SelectedSpectraType = selectedSpectraType;
                    }
                }
            }
        }

        private SpectraType selectedSpectraType;

        [Required(ErrorMessage = "Choose a spectra format.")]
        public SpectraFormat SelectedFileFormat {
            get {
                return selectedFileFormat;
            }

            set {
                if (SetProperty(ref selectedFileFormat, value)) {
                    if (!ContainsError(nameof(SelectedFileFormat))) {
                        Model.SelectedFileFormat = selectedFileFormat;
                    }
                }
            }
        }

        private SpectraFormat selectedFileFormat;

        public int IsotopeExportMax {
            get {
                return isotopeExportMax;
            }
            set {
                if (SetProperty(ref isotopeExportMax, value)) {
                    if (!ContainsError(nameof(IsotopeExportMax))) {
                        Model.IsotopeExportMax = isotopeExportMax;
                    }
                }
            }
        }

        private int isotopeExportMax = 2;

        protected override void OnErrorsChanged([CallerMemberName] string propertyname = "") {
            base.OnErrorsChanged(propertyname);
            ExportPeakCommand.RaiseCanExecuteChanged();
        }
    }

    class FileBeanSelection : ViewModelBase
    {
        public FileBeanSelection(AnalysisFileBean file) {
            File = file;
        }

        public AnalysisFileBean File { get; }

        public string FileName => File.AnalysisFileName;

        public bool IsSelected {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        private bool isSelected = false;

        public bool IsChecked {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }

        private bool isChecked;
    }
}
