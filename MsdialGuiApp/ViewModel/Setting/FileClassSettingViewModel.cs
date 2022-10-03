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
            FinishCommand = new ReactiveCommand().WithSubscribe(model.Apply).AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<FileClassPropertyViewModel> FileClassProperties { get; }
        public ReactiveCommand FinishCommand { get; }
    }
}
