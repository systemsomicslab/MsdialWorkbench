using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    public sealed class NotameViewModel : SettingDialogViewModel
    {
        private readonly Notame _notame;
        private readonly IMessageBroker _broker;

        public NotameViewModel(Notame notame, IMessageBroker broker) {
            _notame = notame;
            _broker = broker;
            RunNotameCommand = new DelegateCommand(RunNotame);
            ShowSettingViewCommand = new DelegateCommand(ShowSettingView);
            Path = notame.Path;
        }
        
        [Required(ErrorMessage = "Please browse a folder for result export.")]
        [PathExists(ErrorMessage = "This folder does not exist.", IsDirectory = true)]
        public string Path { 
            get => _path; 
            set
            {
                if (SetProperty(ref _path, value))
                {
                    if (!ContainsError(nameof(Path)))
                    {
                        _notame.Path = _path;
                    }
                }
            }
        }
        private string _path;

        public DelegateCommand BrowseDirectoryCommand => _browseDirectoryCommand ?? (_browseDirectoryCommand = new DelegateCommand(BrowseDirectory));
        private DelegateCommand _browseDirectoryCommand;

        private void BrowseDirectory()
        {
            var win = new Graphics.Window.SelectFolderDialog
            {
                Title = "Choose an export folder.",
            };

            if (win.ShowDialog() == Graphics.Window.DialogResult.OK)
            {
                Path = win.SelectedPath;
            }
        }

        public DelegateCommand RunNotameCommand { get; }

        private void RunNotame() {
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