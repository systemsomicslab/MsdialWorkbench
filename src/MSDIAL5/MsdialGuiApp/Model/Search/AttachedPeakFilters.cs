using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class AttachedPeakFilters<T> {
        private readonly ICollectionView _view;
        private readonly List<Predicate<T>> _enabledPredicates;
        private readonly List<Predicate<T>> _disabledPredicates;
        private Predicate<object>? _predicate;
        private readonly CompositeDisposable _disposables;

        public AttachedPeakFilters(ICollectionView view) {
            _view = view;
            _enabledPredicates = new List<Predicate<T>>();
            _disabledPredicates = new List<Predicate<T>>();
            _disposables = new CompositeDisposable();
        }

        private void ReloadFilter() {
            _view.Filter -= _predicate;
            _predicate = obj => obj is T t && _enabledPredicates.All(pred => pred.Invoke(t));
            _view.Filter += _predicate;
        }

        public void Attatch(Predicate<T> predicate) {
            _enabledPredicates.Add(predicate);
            ReloadFilter();
        }

        public void Attatch(Predicate<T> predicate, IObservable<bool> enabled, bool initial) {
            if (initial) {
                _enabledPredicates.Add(predicate);
                ReloadFilter();
            }
            else {
                _disabledPredicates.Add(predicate);
            }
            _disposables.Add(enabled.Subscribe(e => {
                if (e) {
                    _disabledPredicates.Remove(predicate);
                    _enabledPredicates.Add(predicate);
                }
                else {
                    _enabledPredicates.Remove(predicate);
                    _disabledPredicates.Add(predicate);
                }
                ReloadFilter();
            }));
        }

        public void Detatch() {
            _view.Filter -= _predicate;
            _enabledPredicates.Clear();
            _disabledPredicates.Clear();
            _predicate = null;
            _disposables.Dispose();
            _disposables.Clear();
        }

        ~AttachedPeakFilters() {
            _disposables.Dispose();
        }
    }
}
