using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class AutoContinuousAxisManager<T> : ContinuousAxisManager<T> where T: IConvertible
    {
        public AutoContinuousAxisManager(ObservableCollection<T> source) : base(source) {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _source.CollectionChanged += Source_CollectionChanged;
        }

        private static readonly double EPS = 1e-10;

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Replace:
                    if (ShouldUpdate(e.NewItems, e.OldItems)) {
                        UpdateInitialRange((ICollection<T>)sender);
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (ShouldUpdateForNew(e.NewItems)) {
                        UpdateInitialRange((ICollection<T>)sender);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (ShouldUpdateForOld(e.OldItems)) {
                        UpdateInitialRange((ICollection<T>)sender);
                    }
                    break;
            }
        }

        private bool ShouldUpdate(IList newValues, IList oldValues) {
            return ShouldUpdateForOld(oldValues) || ShouldUpdateForNew(newValues);
        }

        private bool ShouldUpdateForNew(IList newValues) {
            if (newValues is null) {
                return true;
            }
            var newValues_ = newValues.Cast<T>().ToArray();
            if (newValues_.Any()) {
                var newMin = newValues_.Min();
                if (InitialRange.Minimum.Value - Convert.ToDouble(newMin) >= EPS) {
                    return true;
                }
                var newMax = newValues_.Max();
                if (Convert.ToDouble(newMax) - InitialRange.Maximum.Value >= EPS) {
                    return true;
                }
            }
            return false;
        }

        private bool ShouldUpdateForOld(IList oldValues) {
            if (oldValues is null) {
                return true;
            }
            var oldValues_ = oldValues.Cast<T>().ToArray();
            if (oldValues_.Any()) {
                var oldMin = oldValues_.Min();
                if (Math.Abs(Convert.ToDouble(oldMin) - InitialRange.Minimum.Value) <= EPS) {
                    return true;
                }
                var oldMax = oldValues_.Max();
                if (Math.Abs(Convert.ToDouble(oldMax) - InitialRange.Maximum.Value) <= EPS) {
                    return true;
                }
            }
            return false;
        }

        private readonly ObservableCollection<T> _source;

        private bool _disposed = false;

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    _source.CollectionChanged -= Source_CollectionChanged;
                }

                _disposed = true;
                base.Dispose(disposing);
            }
        }
    }

    public class AutoContinuousAxisManager<U, T> : ContinuousAxisManager<T> where T: IConvertible
    {
        public AutoContinuousAxisManager(ObservableCollection<U> source, Func<U, T> map) : base(source.Select(map).ToList()) {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _source.CollectionChanged += Source_CollectionChanged;
        }

        private static readonly double EPS = 1e-10;

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Replace:
                    if (ShouldUpdate(e.NewItems, e.OldItems)) {
                        UpdateInitialRange(((IEnumerable<U>)sender).Select(_map).ToList());
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (ShouldUpdateForNew(e.NewItems)) {
                        UpdateInitialRange(((IEnumerable<U>)sender).Select(_map).ToList());
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (ShouldUpdateForOld(e.OldItems)) {
                        UpdateInitialRange(((IEnumerable<U>)sender).Select(_map).ToList());
                    }
                    break;
            }
        }

        private bool ShouldUpdate(IList newValues, IList oldValues) {
            return ShouldUpdateForOld(oldValues) || ShouldUpdateForNew(newValues);
        }

        private bool ShouldUpdateForNew(IList newValues) {
            if (newValues is null) {
                return true;
            }
            var newValues_ = newValues.Cast<U>().ToArray();
            if (newValues_.Any()) {
                var newMin = newValues_.Min(_map);
                if (InitialRange.Minimum.Value - Convert.ToDouble(newMin) >= EPS) {
                    return true;
                }
                var newMax = newValues_.Max(_map);
                if (Convert.ToDouble(newMax) - InitialRange.Maximum.Value >= EPS) {
                    return true;
                }
            }
            return false;
        }

        private bool ShouldUpdateForOld(IList oldValues) {
            if (oldValues is null) {
                return true;
            }
            var oldValues_ = oldValues.Cast<U>().ToArray();
            if (oldValues_.Any()) {
                var oldMin = oldValues_.Min(_map);
                if (Math.Abs(Convert.ToDouble(oldMin) - InitialRange.Minimum.Value) <= EPS) {
                    return true;
                }
                var oldMax = oldValues_.Max(_map);
                if (Math.Abs(Convert.ToDouble(oldMax) - InitialRange.Maximum.Value) <= EPS) {
                    return true;
                }
            }
            return false;
        }

        private readonly ObservableCollection<U> _source;
        private readonly Func<U, T> _map;

        private bool _disposed = false;

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    _source.CollectionChanged -= Source_CollectionChanged;
                }

                _disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
