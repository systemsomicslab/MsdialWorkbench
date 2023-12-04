using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    public sealed class NotameViewModel : ViewModelBase
    {
        private readonly Notame _notame;

        public NotameViewModel(Notame notame) {
            RunNotameCommand = new DelegateCommand(RunNotame);
            _notame = notame;

            Path = notame.ToReactivePropertyAsSynchronized(m => m.Path).AddTo(Disposables);
            FileName = notame.ToReactivePropertyAsSynchronized(m => m.FileName).AddTo(Disposables);
            IonMode = notame.ToReactivePropertyAsSynchronized(m => m.IonMode).AddTo(Disposables);
            GroupingName = notame.ToReactivePropertyAsSynchronized(m => m.GroupingName).AddTo(Disposables);
        }

        public ReactiveProperty<string> Path { get; }

        public ReactiveProperty<string> FileName { get; }

        public ReactiveProperty<string> IonMode { get; }

        public ReactiveProperty<string> GroupingName { get; }

        public DelegateCommand RunNotameCommand { get; }

        private void RunNotame() {
            _notame.Run();
        }
    }
}