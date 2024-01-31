using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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

            ExportMethod = notame.ExportMethod;
            ExportTypes = notame.ExportTypes;

            RunNotameCommand = new DelegateCommand(RunNotame);
            ShowSettingViewCommand = new DelegateCommand(ShowSettingView);

            AlignmentFile = notame.AlignmentFilesForExport.SelectedFile;
            ExportDirectory = notame.ExportDirectory;
        }
        
        [Required(ErrorMessage = "Please browse a folder for result export.")]
        [PathExists(ErrorMessage = "This folder does not exist.", IsDirectory = true)]
        public string ExportDirectory { 
            get => _exportDirectory; 
            set {
                if (SetProperty(ref _exportDirectory, value)) {
                    if (!ContainsError(nameof(_exportDirectory))) {
                        _notame.ExportDirectory = _exportDirectory;
                    }
                }
            }
        }
        private string _exportDirectory;

        [Required(ErrorMessage = "Please select alignment file.")]
        public AlignmentFileBeanModel AlignmentFile {
            get => _alignmentFile;
            set {
                if (SetProperty(ref _alignmentFile, value)) {
                    if (!ContainsError(nameof(AlignmentFile))) {
                        _notame.AlignmentFilesForExport.SelectedFile = _alignmentFile;
                    }
                }
            }
        }
        private AlignmentFileBeanModel _alignmentFile;

        public ICollectionView AlignmentFiles { get; }

        public ExportMethod ExportMethod { get; }
        public ExportType[] ExportTypes { get; }

        public bool UseFilter {
            get => _notame.PeakSpotSupplyer.UseFilter;
            set {
                _notame.PeakSpotSupplyer.UseFilter = value;
                OnPropertyChanged(nameof(UseFilter));
            }
        }

        public DelegateCommand BrowseDirectoryCommand => _browseDirectoryCommand ?? (_browseDirectoryCommand = new DelegateCommand(BrowseDirectory));
        private DelegateCommand _browseDirectoryCommand;

        private void BrowseDirectory() {
            var win = new Graphics.Window.SelectFolderDialog {
                Title = "Choose an export folder.",
            };

            if (win.ShowDialog() == Graphics.Window.DialogResult.OK) {
                ExportDirectory = win.SelectedPath;
            }
        }

        public DelegateCommand RunNotameCommand { get; }

        private void RunNotame() {
            _notame.ExportAlignmentResultAsync(_broker);
            _notame.Run();
        }

        public DelegateCommand ShowSettingViewCommand { get; }

        private void ShowSettingView() {
            _broker.Publish(this);
        }

        public override ICommand ApplyCommand => null;
        public override ICommand FinishCommand => RunNotameCommand;
    }
}