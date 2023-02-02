using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class AlignmentResultExportViewModel : ViewModelBase
    {
        private readonly IMessageBroker _broker;
        private readonly AlignmentResultExportModel _model;

        internal AlignmentResultExportViewModel(AlignmentResultExportModel model, IMessageBroker broker) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _broker = broker ?? MessageBroker.Default;

            AlignmentFiles = CollectionViewSource.GetDefaultView(_model.AlignmentFiles);
            if (_model.AlignmentFile != null) {
                AlignmentFiles.MoveCurrentTo(_model.AlignmentFile);
            }

            Groups = model.Groups.ToReadOnlyReactiveCollection(m => MapToViewModel(m, ExportCommand)).AddTo(Disposables);
            if (Groups.Any()) {
                Groups.First().IsExpanded = true;
            }
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

        public ReadOnlyReactiveCollection<IAlignmentResultExportViewModel> Groups { get; }

        public bool UseFilter {
            get => _model.PeakSpotSupplyer.UseFilter;
            set {
                _model.PeakSpotSupplyer.UseFilter = value;
                OnPropertyChanged(nameof(UseFilter));
            }
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
            _model.ExportAlignmentResult((progress, label) => _broker.Publish(task.Progress(progress, label)));
            _broker.Publish(task.End());
        }

        private bool CanExportAlignmentResult() {
            return !HasValidationErrors && !Groups.Any(g => g.HasValidationErrors) && _model.CanExportAlignmentResult();
        }

        private static IAlignmentResultExportViewModel MapToViewModel(IAlignmentResultExportModel model, DelegateCommand exportCommand) {
            switch (model) {
                case AlignmentExportGroupModel m:
                    return new AlignmentExportGroupViewModel(m, exportCommand);
                case ProteinGroupExportModel m:
                    return new ProteinGroupExportViewModel(m);
                case AlignmentSpectraExportGroupModel m:
                    return new AlignmentSpectraExportGroupViewModel(m, exportCommand);
                default:
                    throw new NotSupportedException(model.GetType().FullName);
            }
        }
    }
}
