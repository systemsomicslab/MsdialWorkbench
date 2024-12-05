using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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

            AlignmentFiles = CollectionViewSource.GetDefaultView(model.AlignmentFilesForExport.Files);
            if (model.AlignmentFilesForExport.SelectedFile != null) {
                AlignmentFiles.MoveCurrentTo(model.AlignmentFilesForExport.SelectedFile);
            }

            Groups = model.Groups.ToReadOnlyReactiveCollection(MapToViewModel).AddTo(Disposables);
            if (Groups.Any()) {
                Groups.First().IsExpanded = true;
            }

            ExportCommand = Groups.Select(g => (IObservable<bool>)g.CanExport).Append(
                this.ErrorsChangedAsObservable().Select(_ => !HasValidationErrors).Prepend(!HasValidationErrors)
            ).CombineLatestValuesAreAllTrue()
            .ToAsyncReactiveCommand()
            .WithSubscribe(ExportAlignmentResultAsync)
            .AddTo(Disposables);

            AlignmentFile = model.AlignmentFilesForExport.SelectedFile;
            ExportDirectory = model.ExportDirectory;
        }

        [Required(ErrorMessage = "Please enter the folder which the results will be exported.")]
        [PathExists(ErrorMessage = "This folder does not exist.", IsDirectory = true)]
        public string ExportDirectory {
            get => _exportDirectory;
            set {
                if (SetProperty(ref _exportDirectory, value)) {
                    if (!ContainsError(nameof(ExportDirectory))) {
                        _model.ExportDirectory = _exportDirectory;
                    }
                }
            }
        }
        private string _exportDirectory = string.Empty;

        [Required(ErrorMessage = "Please select alignment file.")]
        public AlignmentFileBeanModel? AlignmentFile {
            get => _alignmentFile;
            set {
                if (SetProperty(ref _alignmentFile, value)) {
                    if (!ContainsError(nameof(AlignmentFile))) {
                        _model.AlignmentFilesForExport.SelectedFile = _alignmentFile;
                    }
                }
            }
        }
        private AlignmentFileBeanModel? _alignmentFile;

        public ICollectionView AlignmentFiles { get; }

        public ReadOnlyReactiveCollection<IAlignmentResultExportViewModel> Groups { get; }

        public bool UseFilter {
            get => _model.PeakSpotSupplyer.UseFilter;
            set {
                _model.PeakSpotSupplyer.UseFilter = value;
                OnPropertyChanged(nameof(UseFilter));
            }
        }

        public DelegateCommand BrowseDirectoryCommand => _browseDirectoryCommand ??= new DelegateCommand(BrowseDirectory);
        private DelegateCommand? _browseDirectoryCommand;

        private void BrowseDirectory() {
            var fbd = new Graphics.Window.SelectFolderDialog
            {
                Title = "Choose a export folder.",
            };

            if (fbd.ShowDialog() == Graphics.Window.DialogResult.OK) {
                ExportDirectory = fbd.SelectedPath!;
            }
        }

        public AsyncReactiveCommand ExportCommand { get; }

        private Task ExportAlignmentResultAsync() {
            return _model.ExportAlignmentResultAsync(_broker);
        }

        private static IAlignmentResultExportViewModel MapToViewModel(IAlignmentResultExportModel model) {
            switch (model) {
                case AlignmentExportGroupModel m:
                    return new AlignmentExportGroupViewModel(m);
                case ProteinGroupExportModel m:
                    return new ProteinGroupExportViewModel(m);
                case AlignmentSpectraExportGroupModel m:
                    return new AlignmentSpectraExportGroupViewModel(m);
                case AlignmentMatchedSpectraExportModel m:
                    return new AlignmentMatchedSpectraExportViewModel(m);
                case AlignmentResultMassBankRecordExportModel m:
                    return new AlignmentResultMassBankRecordExportViewModel(m);
                default:
                    throw new NotSupportedException(model.GetType().FullName);
            }
        }
    }
}
