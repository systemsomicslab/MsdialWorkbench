using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class FileClassPropertyViewModel : ViewModelBase {
        public FileClassPropertyViewModel(FileClassPropertyModel model) {
            Name = model.Name;
            Color = model.ToReactivePropertyAsSynchronized(m => m.Color).AddTo(Disposables);
            Order = model.ToReactivePropertyAsSynchronized(m => m.Order).AddTo(Disposables);
        }

        public string Name { get; }
        public ReactiveProperty<Color> Color { get; }
        public ReactiveProperty<int> Order { get; }
    }
}
