using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class AutoContinuousAxisManager<T> : ContinuousAxisManager<T> where T: IConvertible
    {
        public AutoContinuousAxisManager(ObservableCollection<T> source) : base(source) {
            this.source = source;
            this.source.CollectionChanged += Source_CollectionChanged;
        }

        private static readonly double eps = 1e-10;

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Replace:
                    if (ShouldUpdate((ICollection<T>)e.NewItems, (ICollection<T>)e.OldItems)) {
                        UpdateInitialRange((ICollection<T>)sender);
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (ShouldUpdateForNew((ICollection<T>)e.NewItems)) {
                        UpdateInitialRange((ICollection<T>)sender);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (ShouldUpdateForOld((ICollection<T>)e.OldItems)) {
                        UpdateInitialRange((ICollection<T>)sender);
                    }
                    break;
            }
        }

        private bool ShouldUpdate(ICollection<T> newValues, ICollection<T> oldValues) {
            return ShouldUpdateForOld(oldValues) || ShouldUpdateForNew(newValues);
        }

        private bool ShouldUpdateForNew(ICollection<T> newValues) {
            if (newValues.Any()) {
                var newMin = newValues.Min();
                if (InitialRange.Minimum.Value - Convert.ToDouble(newMin) >= eps) {
                    return true;
                }
                var newMax = newValues.Max();
                if (Convert.ToDouble(newMax) - InitialRange.Maximum.Value >= eps) {
                    return true;
                }
            }
            return false;
        }

        private bool ShouldUpdateForOld(ICollection<T> oldValues) {
            if (oldValues.Any()) {
                var oldMin = oldValues.Min();
                if (Math.Abs(Convert.ToDouble(oldMin) - InitialRange.Minimum.Value) <= eps) {
                    return true;
                }
                var oldMax = oldValues.Max();
                if (Math.Abs(Convert.ToDouble(oldMax) - InitialRange.Maximum.Value) <= eps) {
                    return true;
                }
            }
            return false;
        }

        private readonly ObservableCollection<T> source;
    }

    public class AutoContinuousAxisManager<U, T> : ContinuousAxisManager<T> where T: IConvertible
    {
        public AutoContinuousAxisManager(ObservableCollection<U> source, Func<U, T> map) : base(source.Select(map).ToList()) {
            this.source = source;
            this.map = map;
            this.source.CollectionChanged += Source_CollectionChanged;
        }

        private static readonly double eps = 1e-10;

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Replace:
                    if (ShouldUpdate((ICollection<U>)e.NewItems, (ICollection<U>)e.OldItems)) {
                        UpdateInitialRange(((IEnumerable<U>)sender).Select(map).ToList());
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (ShouldUpdateForNew((ICollection<U>)e.NewItems)) {
                        UpdateInitialRange(((IEnumerable<U>)sender).Select(map).ToList());
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (ShouldUpdateForOld((ICollection<U>)e.OldItems)) {
                        UpdateInitialRange(((IEnumerable<U>)sender).Select(map).ToList());
                    }
                    break;
            }
        }

        private bool ShouldUpdate(ICollection<U> newValues, ICollection<U> oldValues) {
            return ShouldUpdateForOld(oldValues) || ShouldUpdateForNew(newValues);
        }

        private bool ShouldUpdateForNew(ICollection<U> newValues) {
            if (newValues.Any()) {
                var newMin = newValues.Min(map);
                if (InitialRange.Minimum.Value - Convert.ToDouble(newMin) >= eps) {
                    return true;
                }
                var newMax = newValues.Max(map);
                if (Convert.ToDouble(newMax) - InitialRange.Maximum.Value >= eps) {
                    return true;
                }
            }
            return false;
        }

        private bool ShouldUpdateForOld(ICollection<U> oldValues) {
            if (oldValues.Any()) {
                var oldMin = oldValues.Min(map);
                if (Math.Abs(Convert.ToDouble(oldMin) - InitialRange.Minimum.Value) <= eps) {
                    return true;
                }
                var oldMax = oldValues.Max(map);
                if (Math.Abs(Convert.ToDouble(oldMax) - InitialRange.Maximum.Value) <= eps) {
                    return true;
                }
            }
            return false;
        }

        private readonly ObservableCollection<U> source;
        private readonly Func<U, T> map;
    }
}
