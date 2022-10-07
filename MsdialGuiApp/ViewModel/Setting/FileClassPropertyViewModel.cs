using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class FileClassPropertyViewModel : ViewModelBase {
        private readonly Subject<Unit> _commit;
        private readonly Subject<Unit> _discard;

        public FileClassPropertyViewModel(FileClassPropertyModel model) {
            _commit = new Subject<Unit>().AddTo(Disposables);
            _discard = new Subject<Unit>().AddTo(Disposables);

            Name = model.Name;
            Color = model.ToReactivePropertyAsSynchronized(
                m => m.Color,
                op => op.SelectMany(p => _discard.StartWith(Unit.Default).Select(_ => p)),
                or => or.Sample(_commit)
            ).AddTo(Disposables);
            Order = model.ToReactivePropertyAsSynchronized(
                m => m.Order,
                op => op.SelectMany(p => _discard.StartWith(Unit.Default).Select(_ => p)),
                or => or.Sample(_commit)
            ).AddTo(Disposables);
        }

        public string Name { get; }
        public ReactiveProperty<Color> Color { get; }
        public ReactiveProperty<int> Order { get; }

        public void Commit() {
            _commit.OnNext(Unit.Default);
        }

        public void Discard() {
            _discard.OnNext(Unit.Default);
        }
    }
}
