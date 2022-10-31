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
    internal sealed class AnalysisResultExportViewModel : ViewModelBase
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

            ValidateProperty(nameof(SelectedSpectraType), SelectedSpectraType);
            ValidateProperty(nameof(SelectedFileFormat), SelectedFileFormat);
            ValidateProperty(nameof(DestinationFolder), DestinationFolder);
        }

        public AnalysisResultExportModel Model { get; }

        public DelegateCommand ExportPeakCommand => _exportPeakCommand ?? (_exportPeakCommand = new DelegateCommand(ExportPeak, CanExportPeak));
        private DelegateCommand _exportPeakCommand;

        private void ExportPeak() {
            _result = Task.Run(Model.Export);
            ExportPeakCommand.RaiseCanExecuteChanged();
        }
        private Task _result;

        private bool CanExportPeak() {
            return _result?.Status != TaskStatus.Running && !HasValidationErrors;
        }

        public MappedReadOnlyObservableCollection<AnalysisFileBean, FileBeanSelection> SelectedFrom { get; }

        public MappedReadOnlyObservableCollection<AnalysisFileBean, FileBeanSelection> SelectedTo { get; }

        public DelegateCommand AddItemsCommand => _addItemsCommand ?? (_addItemsCommand = new DelegateCommand(AddItems));
        private DelegateCommand _addItemsCommand;

        private void AddItems() {
            Model.Selects(SelectedFrom.Where(file => file.IsChecked).Select(file => file.File).ToList());
        }

        public DelegateCommand AddAllItemsCommand => _addAllItemsCommand ?? (_addAllItemsCommand = new DelegateCommand(AddAllItems));
        private DelegateCommand _addAllItemsCommand;

        private void AddAllItems() {
            Model.Selects(SelectedFrom.Select(file => file.File).ToList());
        }

        public DelegateCommand RemoveItemsCommand => _removeItemsCommand ?? (_removeItemsCommand = new DelegateCommand(RemoveItems));
        private DelegateCommand _removeItemsCommand;

        private void RemoveItems() {
            Model.UnSelects(SelectedTo.Where(file => file.IsChecked).Select(file => file.File).ToList());
        }

        public DelegateCommand RemoveAllItemsCommand => _removeAllItemsCommand ?? (_removeAllItemsCommand = new DelegateCommand(RemoveAllItems));
        private DelegateCommand _removeAllItemsCommand;

        private void RemoveAllItems() {
            Model.UnSelects(SelectedTo.Select(file => file.File).ToList());
        }

        public DelegateCommand SelectDestinationCommand => _selectDestinationCommand ?? (_selectDestinationCommand = new DelegateCommand(SelectDestination));
        private DelegateCommand _selectDestinationCommand;

        [Required(ErrorMessage = "Choose a folder for the exported files.")]
        [PathExists(ErrorMessage = "Choose an existing folder", IsDirectory = true)]
        public string DestinationFolder {
            get {
                return _destinationFolder;
            }

            set {
                if (SetProperty(ref _destinationFolder, value)) {
                    if (!ContainsError(nameof(DestinationFolder))) {
                        Model.DestinationFolder = _destinationFolder;
                    }
                }
            }
        }

        private string _destinationFolder;

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
                return _selectedFileFormat;
            }

            set {
                if (SetProperty(ref _selectedFileFormat, value)) {
                    if (!ContainsError(nameof(SelectedFileFormat))) {
                        Model.SelectedFileFormat = _selectedFileFormat;
                    }
                }
            }
        }

        private SpectraFormat _selectedFileFormat;

        public int IsotopeExportMax {
            get {
                return _isotopeExportMax;
            }
            set {
                if (SetProperty(ref _isotopeExportMax, value)) {
                    if (!ContainsError(nameof(IsotopeExportMax))) {
                        Model.IsotopeExportMax = _isotopeExportMax;
                    }
                }
            }
        }

        private int _isotopeExportMax = 2;

        protected override void OnErrorsChanged([CallerMemberName] string propertyname = "") {
            base.OnErrorsChanged(propertyname);
            ExportPeakCommand.RaiseCanExecuteChanged();
        }
    }

    internal sealed class FileBeanSelection : ViewModelBase
    {
        public FileBeanSelection(AnalysisFileBean file) {
            File = file;
        }

        public AnalysisFileBean File { get; }

        public string FileName => File.AnalysisFileName;

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private bool _isSelected = false;

        public bool IsChecked {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        private bool _isChecked;
    }
}
