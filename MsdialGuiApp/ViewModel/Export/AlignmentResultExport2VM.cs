using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Export
{
    public sealed class AlignmentResultExport2VM : ViewModelBase
    {
        private readonly IMessageBroker _broker;
        private readonly AlignmentResultExportModel _model;

        internal AlignmentResultExport2VM(AlignmentResultExportModel model, IMessageBroker broker) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));

            AlignmentFiles = CollectionViewSource.GetDefaultView(_model.AlignmentFiles);
            if (_model.AlignmentFile != null)
                AlignmentFiles.MoveCurrentTo(_model.AlignmentFile);
            _broker = broker ?? MessageBroker.Default;
        }

        public AlignmentResultExport2VM(
            AlignmentFileBean alignmentFile,
            IReadOnlyList<AlignmentFileBean> alignmentFiles,
            IMsdialDataStorage<ParameterBase> container,
            IMessageBroker broker) : this(new AlignmentResultExportModel(alignmentFile, alignmentFiles, container), broker) {

        }

        [PathExists(ErrorMessage = "This folder does not exist.", IsDirectory = true)]
        public string ExportDirectory {
            get => _exportDirectory;
            set {
                if (SetProperty(ref _exportDirectory, value)) {
                    if (!ContainsError(nameof(ExportDirectory))) {
                        _model.ExportDirectory = _exportDirectory;
                    }
                    ExportCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        private string _exportDirectory = string.Empty;

        [Required(ErrorMessage = "Please select format.")]
        public ExportFormat Format {
            get => _format;
            set {
                if (SetProperty(ref _format, value)) {
                    if (!ContainsError(nameof(Format))) {
                        _model.Format = _format;
                    }
                    ExportCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        private ExportFormat _format;
        public ReadOnlyObservableCollection<ExportFormat> Formats => _model.Formats;

        [Required(ErrorMessage = "Please select spectra type.")]
        public ExportspectraType SpectraType {
            get => _spectraType;
            set {
                if (SetProperty(ref _spectraType, value)) {
                    if (!ContainsError(nameof(SpectraType))) {
                        _model.SpectraType = _spectraType;
                    }
                    ExportCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        private ExportspectraType _spectraType = ExportspectraType.deconvoluted;
        public ReadOnlyObservableCollection<ExportspectraType> SpectraTypes => _model.SpectraTypes;

        [Required(ErrorMessage = "Please select alignment file.")]
        public AlignmentFileBean AlignmentFile {
            get => _alignmentFile;
            set {
                if (SetProperty(ref _alignmentFile, value)) {
                    if (!ContainsError(nameof(AlignmentFile))) {
                        _model.AlignmentFile = _alignmentFile;
                    }
                    ExportCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        private AlignmentFileBean _alignmentFile;

        public ICollectionView AlignmentFiles { get; }

        public ReadOnlyObservableCollection<ExportType> ExportTypes => _model.ExportTypes;

        public void AddExportTypes(params ExportType[] exportTypes) {
            _model.AddExportTypes(exportTypes);
        }

        public DelegateCommand BrowseDirectoryCommand => _browseDirectoryCommand ?? (_browseDirectoryCommand = new DelegateCommand(BrowseDirectory));
        private DelegateCommand _browseDirectoryCommand;

        private void BrowseDirectory() {
            var fbd = new Graphics.Window.SelectFolderDialog
            {
                Title = "Chose a export folder.",
            };

            if (fbd.ShowDialog() == Graphics.Window.DialogResult.OK) {
                ExportDirectory = fbd.SelectedPath;
            }
        }

        public DelegateCommand ExportCommand => _exportCommand ?? (_exportCommand = new DelegateCommand(ExportAlignmentResult, CanExportAlignmentResult));
        private DelegateCommand _exportCommand;

        private void ExportAlignmentResult() {
            var task = TaskNotification.Start($"Exporting {AlignmentFile.FileName}");
            _broker.Publish(task);
            _model.ExportAlignmentResult((progress, label) => _broker.Publish(TaskNotification.Progress(task, progress, label)));
            _broker.Publish(TaskNotification.End(task));
        }

        private bool CanExportAlignmentResult() {
            return !HasValidationErrors && _model.CanExportAlignmentResult();
        }

        public DelegateCommand<Window> CancelCommand => _cancelCommand ?? (_cancelCommand = new DelegateCommand<Window>(Cancel));
        private DelegateCommand<Window> _cancelCommand;

        private void Cancel(Window window) {
            window.DialogResult = false;
            window.Close();
        }
    }
}
