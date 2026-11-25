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
        private readonly object _syncObject = new object();
        private Predicate<T>[] _activePredicates = Array.Empty<Predicate<T>>();
        private Predicate<object>? _predicate;
        private readonly CompositeDisposable _disposables;

        public AttachedPeakFilters(ICollectionView view) {
            _view = view;
            _enabledPredicates = new List<Predicate<T>>();
            _disabledPredicates = new List<Predicate<T>>();
            _disposables = new CompositeDisposable();
        }

        // The view's Filter is installed once and never reassigned afterwards.
        // Assigning ICollectionView.Filter refreshes the view on the spot, and that throws
        // "'Filter' is not allowed during an AddNew or EditItem transaction" while a DataGrid row is being edited.
        // Turning a filter on or off only swaps the predicate list; the refresh itself is left to
        // PeakSpotNavigatorViewModel, which already waits for the edit to end.
        private void InstallFilter() {
            if (_predicate is null) {
                _predicate = obj => obj is T t && _activePredicates.All(pred => pred.Invoke(t));
                _view.Filter += _predicate;
            }
        }

        private void PublishPredicates() {
            _activePredicates = _enabledPredicates.ToArray();
        }

        public void Attatch(Predicate<T> predicate) {
            lock (_syncObject) {
                _enabledPredicates.Add(predicate);
                PublishPredicates();
            }
            InstallFilter();
        }

        public void Attatch(Predicate<T> predicate, IObservable<bool> enabled, bool initial) {
            lock (_syncObject) {
                if (initial) {
                    _enabledPredicates.Add(predicate);
                    PublishPredicates();
                }
                else {
                    _disabledPredicates.Add(predicate);
                }
            }
            InstallFilter();
            _disposables.Add(enabled.Subscribe(e => {
                lock (_syncObject) {
                    if (e) {
                        _disabledPredicates.Remove(predicate);
                        _enabledPredicates.Add(predicate);
                    }
                    else {
                        _enabledPredicates.Remove(predicate);
                        _disabledPredicates.Add(predicate);
                    }
                    PublishPredicates();
                }
            }));
        }

        public void Detatch() {
            _view.Filter -= _predicate;
            lock (_syncObject) {
                _enabledPredicates.Clear();
                _disabledPredicates.Clear();
                PublishPredicates();
            }
            _predicate = null;
            _disposables.Dispose();
            _disposables.Clear();
        }

        ~AttachedPeakFilters() {
            _disposables.Dispose();
        }
    }
}
