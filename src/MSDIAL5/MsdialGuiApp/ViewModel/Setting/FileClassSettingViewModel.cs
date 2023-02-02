using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class FileClassSetViewModel : ViewModelBase
    {
        private readonly FileClassSetModel _model;

        public FileClassSetViewModel(FileClassSetModel model) {
            _model = model;
            FileClassProperties = _model.FileClassProperties.ToReadOnlyReactiveCollection(m => new FileClassPropertyViewModel(m)).AddTo(Disposables);
            ApplyCommand = new ReactiveCommand().WithSubscribe(Apply).AddTo(Disposables);
            CancelCommand = new ReactiveCommand().WithSubscribe(Cancel).AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<FileClassPropertyViewModel> FileClassProperties { get; }
        public ReactiveCommand ApplyCommand { get; }

        private void Apply() {
            foreach (var property in FileClassProperties) {
                property.Commit();
            }
        }

        public ReactiveCommand CancelCommand { get; }
        private void Cancel() {
            foreach (var property in FileClassProperties) {
                property.Discard();
            }
        }
    }
}
