using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class AlignmentResultExport2VM : ViewModelBase
    {
        private readonly IMessageBroker _broker;
        private readonly AlignmentResultExportModel _model;

        internal AlignmentResultExport2VM(AlignmentResultExportModel model, IMessageBroker broker) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            _broker = broker ?? MessageBroker.Default;

            AlignmentFiles = CollectionViewSource.GetDefaultView(_model.AlignmentFiles);
            if (_model.AlignmentFile != null) {
                AlignmentFiles.MoveCurrentTo(_model.AlignmentFile);
            }

            Groups = model.Groups.ToReadOnlyReactiveCollection(m => new AlignmentExportGroupViewModel(m, ExportCommand)).AddTo(Disposables);
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

        public ReadOnlyReactiveCollection<AlignmentExportGroupViewModel> Groups { get; }

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
            return !HasValidationErrors && !Groups.Any(g => g.HasValidationErrors) && _model.CanExportAlignmentResult();
        }
    }
}
