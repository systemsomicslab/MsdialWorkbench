using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class AnalysisResultExportViewModel : ViewModelBase
    {
        private readonly AnalysisResultExportModel _model;

        public IMsdialAnalysisExportViewModel[] MsdialAnalysisExportViewModels { get; }

        public AnalysisResultExportViewModel(AnalysisResultExportModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            MsdialAnalysisExportViewModels = model.AnalysisExports.Select(m => MsdialAnalysisExportViewModelFactory.Create(m)?.AddTo(Disposables)).OfType<IMsdialAnalysisExportViewModel>().ToArray();

            SelectedFrom = model.UnSelectedFiles.ToReadOnlyReactiveCollection(file => new FileBeanSelection(file)).AddTo(Disposables);
            SelectedTo = model.SelectedFiles.ToReadOnlyReactiveCollection(file => new FileBeanSelection(file)).AddTo(Disposables);

            ExportPeakCommand = MsdialAnalysisExportViewModels.Select(vm => vm.CanExport)
                .Concat(new[]
                {
                    this.ErrorsChangedAsObservable().ToUnit().StartWith(Unit.Default).Select(_ => !HasValidationErrors),
                    SelectedTo.CollectionChangedAsObservable().Select(_ => SelectedTo.Any()),
                    MsdialAnalysisExportViewModels.Select(vm => vm.ShouldExport).CombineLatestValuesAreAllFalse().Inverse(),
                })
                .CombineLatestValuesAreAllTrue()
                .ToAsyncReactiveCommand()
                .WithSubscribe(ExportPeakAsync)
                .AddTo(Disposables);
            DestinationFolder = model.DestinationFolder;
        }

        public AsyncReactiveCommand ExportPeakCommand { get; }
        private Task ExportPeakAsync() => Task.Run(_model.Export);

        public ReadOnlyReactiveCollection<FileBeanSelection> SelectedFrom { get; }
        public ReadOnlyReactiveCollection<FileBeanSelection> SelectedTo { get; }

        public DelegateCommand AddItemsCommand => _addItemsCommand ??= new DelegateCommand(AddItems);
        private DelegateCommand? _addItemsCommand;

        private void AddItems() {
            _model.Selects(SelectedFrom.Where(file => file.IsChecked).Select(file => file.File));
        }

        public DelegateCommand AddAllItemsCommand => _addAllItemsCommand ??= new DelegateCommand(AddAllItems);
        private DelegateCommand? _addAllItemsCommand;

        private void AddAllItems() {
            _model.Selects(SelectedFrom.Select(file => file.File));
        }

        public DelegateCommand RemoveItemsCommand => _removeItemsCommand ??= new DelegateCommand(RemoveItems);
        private DelegateCommand? _removeItemsCommand;

        private void RemoveItems() {
            _model.UnSelects(SelectedTo.Where(file => file.IsChecked).Select(file => file.File));
        }

        public DelegateCommand RemoveAllItemsCommand => _removeAllItemsCommand ??= new DelegateCommand(RemoveAllItems);
        private DelegateCommand? _removeAllItemsCommand;

        private void RemoveAllItems() {
            _model.UnSelects(SelectedTo.Select(file => file.File));
        }

        public DelegateCommand SelectDestinationCommand => _selectDestinationCommand ??= new DelegateCommand(SelectDestination);
        private DelegateCommand? _selectDestinationCommand;

        [Required(ErrorMessage = "Choose a folder for the exported files.")]
        [PathExists(ErrorMessage = "Choose an existing folder", IsDirectory = true)]
        public string? DestinationFolder {
            get {
                return _destinationFolder;
            }
            set {
                if (SetProperty(ref _destinationFolder, value)) {
                    if (!ContainsError(nameof(DestinationFolder))) {
                        _model.DestinationFolder = _destinationFolder!; // Here is not null because of RequiredAttribute.
                    }
                }
            }
        }
        private string? _destinationFolder;

        public void SelectDestination() {
            var fbd = new Graphics.Window.SelectFolderDialog
            {
                Title = "Choose a export folder.",
            };
            if (fbd.ShowDialog() == Graphics.Window.DialogResult.OK) {
                DestinationFolder = fbd.SelectedPath;
            }
        }
    }

    internal sealed class FileBeanSelection : ViewModelBase
    {
        public FileBeanSelection(AnalysisFileBeanModel file) {
            File = file;
        }

        public AnalysisFileBeanModel File { get; }

        public string FileName => File.AnalysisFileName;

        public bool IsChecked {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }
        private bool _isChecked;
    }
}
