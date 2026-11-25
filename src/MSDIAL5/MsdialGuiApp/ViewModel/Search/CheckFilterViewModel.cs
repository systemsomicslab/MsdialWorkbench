using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive;

namespace CompMs.App.Msdial.ViewModel.Search
{
    internal sealed class CheckFilterViewModel : ViewModelBase
    {
        private readonly CheckFilterModel _model;

        public CheckFilterViewModel(CheckFilterModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            IsChecked = model.ToReactivePropertySlimAsSynchronized(m => m.IsEnabled).AddTo(Disposables);
        }

        public string Label => _model.Label;
        public string Description => _model.Description;
        public ReactivePropertySlim<bool> IsChecked { get; }

        public IObservable<Unit> ObserveChanged => IsChecked.ToUnit();
    }
}
