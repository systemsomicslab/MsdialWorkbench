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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Statistics {
    internal sealed class NotameViewModel : SettingDialogViewModel {
        private readonly Notame _notame;
        private readonly IMessageBroker _broker;
        
        public NotameViewModel(Notame notame, IMessageBroker broker) {
            _notame = notame;
            _broker = broker;

            AlignmentFiles = CollectionViewSource.GetDefaultView(notame.AlignmentFilesForExport.Files);
            if (notame.AlignmentFilesForExport.SelectedFile != null) {
                AlignmentFiles.MoveCurrentTo(notame.AlignmentFilesForExport.SelectedFile);
            }

            RunNotameCommand = new DelegateCommand(RunNotame, () => !HasValidationErrors && !_isRunning);
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

        public DelegateCommand ShowPackageScript => _showScript ??= new DelegateCommand(ShowScript);
        private DelegateCommand? _showScript;

        private void ShowScript() {
            CustomPackageDialog.ShowDialog(Notame.PackageInstallScript);
        }

        public bool ExportReport {
            get => _notame.exportReport;
            set { 
                _notame.exportReport = value;
                OnPropertyChanged(nameof(ExportReport));
            }
        }
        
        public DelegateCommand RunNotameCommand { get; }

        private bool _isRunning;

        private async void RunNotame() {
            if (_isRunning) {
                return;
            }
            _isRunning = true;
            RunNotameCommand.RaiseCanExecuteChanged();
            var exportDirectory = _notame.ExportDirectory;
            try {
                var publisher = new TaskProgressPublisher(_broker, "Notame running in the background...");
                using (publisher.Start()) {
                    await _notame.RunAsync(_broker);
                }
                MessageBox.Show($"Notame output files were created in {exportDirectory}.", "Notame");
            }
            catch (NotamePackageException ex) {
                await OfferPackageInstallationAsync(ex);
            }
            catch (System.Exception ex) {
                MessageBox.Show(
                    $"Notame failed: {ex.Message}\r\n\r\nThe log of the run is in {System.IO.Path.Combine(exportDirectory, "notame_log.txt")}.",
                    "Notame", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally {
                _isRunning = false;
                RunNotameCommand.RaiseCanExecuteChanged();
            }
        }

        private async Task OfferPackageInstallationAsync(NotamePackageException requirements) {
            var packages = requirements.Packages.Select(package =>
                package == "notame" && requirements.OldNotameVersion.Length > 0
                    ? $"notame 1.0 or later (installed: {requirements.OldNotameVersion})"
                    : package).ToList();
            if (requirements.InstallTinyTex) {
                packages.Add("TinyTeX (LaTeX distribution for PDF reports, if needed)");
            }
            var answer = MessageBox.Show(
                "Notame needs these packages to continue.\r\n\r\n  " + string.Join("\r\n  ", packages) +
                $"\r\n\r\nDownload and install these packages and their required dependencies using R at {requirements.RHome}?" +
                "\r\n\r\nAfter setup, restart MS-DIAL and run Notame again.",
                "Install Notame packages", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) {
                return;
            }

            try {
                var publisher = new TaskProgressPublisher(_broker, "Installing Notame packages...");
                using (publisher.Start()) {
                    await Notame.InstallPackagesAsync(requirements);
                }
                MessageBox.Show("Setup completed. Restart MS-DIAL and run Notame again.", "Notame");
            }
            catch (System.Exception ex) {
                CustomPackageDialog.ShowDialog("Notame package setup failed.\r\n\r\n" + ex.Message);
            }
        }

        public DelegateCommand ShowSettingViewCommand { get; }

        private void ShowSettingView() {
            _broker.Publish(this);
        }

        public override ICommand? ApplyCommand => null;
        public override ICommand? FinishCommand => RunNotameCommand;
    }
}
