using CompMs.App.Msdial.Model.Setting;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class FileClassSetViewModel : SettingDialogViewModel
    {
        private readonly FileClassSetModel _model;
        private ReactiveCommand _applyCommand, _cancelCommand;

        public FileClassSetViewModel(FileClassSetModel model) {
            _model = model;
            FileClassProperties = _model.FileClassProperties.ToReadOnlyReactiveCollection(m => new FileClassPropertyViewModel(m)).AddTo(Disposables);
            _applyCommand = new ReactiveCommand().WithSubscribe(Apply).AddTo(Disposables);
            _cancelCommand = new ReactiveCommand().WithSubscribe(Cancel).AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<FileClassPropertyViewModel> FileClassProperties { get; }
        public override ICommand ApplyCommand => _applyCommand;
        public override ICommand FinishCommand => _applyCommand;

        private void Apply() {
            foreach (var property in FileClassProperties) {
                property.Commit();
            }
        }

        public override ICommand CancelCommand => _cancelCommand;
        private void Cancel() {
            foreach (var property in FileClassProperties) {
                property.Discard();
            }
        }
    }
}
