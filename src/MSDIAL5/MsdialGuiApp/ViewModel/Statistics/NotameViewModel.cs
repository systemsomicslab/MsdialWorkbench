using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.Graphics.UI;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Data;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class NotameViewModel : SettingDialogViewModel
    {
        private readonly Notame _notame;
        private readonly IMessageBroker _broker;
        
        public NotameViewModel(Notame notame, IMessageBroker broker) {
            _notame = notame;
            _broker = broker;

            AlignmentFiles = CollectionViewSource.GetDefaultView(notame.AlignmentFilesForExport.Files);
            if (notame.AlignmentFilesForExport.SelectedFile != null) {
                AlignmentFiles.MoveCurrentTo(notame.AlignmentFilesForExport.SelectedFile);
            }

            RunNotameCommand = new DelegateCommand(RunNotame, () => !HasValidationErrors);
            ShowSettingViewCommand = new DelegateCommand(ShowSettingView);

            AlignmentFile = notame.AlignmentFilesForExport.SelectedFile;
            ExportDirectory = notame.ExportDirectory;
            RDirectory = notame.RDirectory;
            ExportViewModel = new AlignmentExportGroupViewModel(notame.ExportModel).AddTo(Disposables);
        }
        
        [Required(ErrorMessage = "Please browse R directory.")]
        [PathExists(ErrorMessage = "This folder does not exist.", IsDirectory = true)]
        public string RDirectory { 
            get => _rDirectory; 
            set {
                if (SetProperty(ref _rDirectory, value)) {
                    if (!ContainsError(nameof(_rDirectory))) {
                        _notame.RDirectory = _rDirectory;
                        RunNotameCommand?.RaiseCanExecuteChanged();
                    }
                }
            }
        }
        private string _rDirectory = string.Empty;

        [Required(ErrorMessage = "Please browse a folder for result export.")]
        [PathExists(ErrorMessage = "This folder does not exist.", IsDirectory = true)]
        public string ExportDirectory {
            get => _exportDirectory;
            set {
                if (SetProperty(ref _exportDirectory, value)) {
                    if (!ContainsError(nameof(_exportDirectory))) {
                        _notame.ExportDirectory = _exportDirectory;
                        RunNotameCommand?.RaiseCanExecuteChanged();
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
                        _notame.AlignmentFilesForExport.SelectedFile = _alignmentFile;
                        RunNotameCommand?.RaiseCanExecuteChanged();
                    }
                }
            }
        }
        private AlignmentFileBeanModel? _alignmentFile;

        public ICollectionView AlignmentFiles { get; }

        public AlignmentExportGroupViewModel ExportViewModel { get; }
        public ExportMethod ExportMethod => _notame.ExportMethod;
        public ReadOnlyObservableCollection<ExportType> ExportTypes => _notame.ExportTypes;

        public bool UseFilter {
            get => _notame.PeakSpotSupplyer.UseFilter;
            set {
                _notame.PeakSpotSupplyer.UseFilter = value;
                OnPropertyChanged(nameof(UseFilter));
            }
        }

        public DelegateCommand BrowseDirectoryCommand => _browseDirectoryCommand ??= new DelegateCommand(BrowseDirectory);
        private DelegateCommand? _browseDirectoryCommand;

        private void BrowseDirectory() {
            var win = new Graphics.Window.SelectFolderDialog {
                Title = "Choose an export folder.",
            };

            if (win.ShowDialog() == Graphics.Window.DialogResult.OK) {
                ExportDirectory = win.SelectedPath!;
            }
        }

        public DelegateCommand BrowseRDirectoryCommand => _browseRDirectoryCommand ??= new DelegateCommand(BrowseRDirectory);
        private DelegateCommand? _browseRDirectoryCommand;

        private void BrowseRDirectory() {
            var win = new Graphics.Window.SelectFolderDialog {
                Title = "Choose R directory.",
            };

            if (win.ShowDialog() == Graphics.Window.DialogResult.OK) {
                RDirectory = win.SelectedPath!;
            }
        }

        public DelegateCommand RunNotameCommand { get; }

        private async void RunNotame() {
            var task = TaskNotification.Start($"Notame running in the background...");
            _broker.Publish(task);
            await _notame.ExportAlignmentResultAsync(_broker).ConfigureAwait(false);
            _notame.Run();
        }

        public DelegateCommand ShowSettingViewCommand { get; }

        private void ShowSettingView() {
            _broker.Publish(this);
        }

        public override ICommand? ApplyCommand => null;
        public override ICommand? FinishCommand => RunNotameCommand;
    }
}