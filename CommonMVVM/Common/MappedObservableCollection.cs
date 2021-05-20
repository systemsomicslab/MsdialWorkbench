using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace CompMs.CommonMVVM.Common
{
    public class MappedObservableCollection<T, U> : ObservableCollection<U>, IDisposable
    {
        public MappedObservableCollection(ObservableCollection<T> collection, Func<T, U> mapTo, Func<U, T> mapFrom)
            :this(collection, Map(collection, mapTo), mapTo, mapFrom) {

        }

        public MappedObservableCollection(ObservableCollection<T> source, ObservableCollection<U> dest, Func<T, U> mapTo, Func<U, T> mapFrom) :base(dest) {
            this.mapTo = mapTo;
            this.mapFrom = mapFrom;
            this.source = source;
            this.dest = Items;

            this.source.CollectionChanged += source_CollectionChanged;
        }

        private readonly ObservableCollection<T> source;
        private readonly IList<U> dest;
        private readonly Func<T, U> mapTo;
        private readonly Func<U, T> mapFrom;
        private bool sourceEditting = false;

        protected override void InsertItem(int index, U item) {
            base.InsertItem(index, item);
            sourceEditting = true;
            if (index == source.Count) {
                source.Add(mapFrom(item));
            }
            else {
                source.Insert(index, mapFrom(item));
            }
            sourceEditting = false;
        }

        protected override void SetItem(int index, U item) {
            base.SetItem(index, item);
            sourceEditting = true;
            source[index] = mapFrom(item);
            sourceEditting = false;
        }

        protected override void MoveItem(int oldIndex, int newIndex) {
            base.MoveItem(oldIndex, newIndex);
            sourceEditting = true;
            source.Move(oldIndex, newIndex);
            sourceEditting = false;
        }

        protected override void RemoveItem(int index) {
            base.RemoveItem(index);
            sourceEditting = true;
            source.RemoveAt(index);
            sourceEditting = false;
        }

        protected override void ClearItems() {
            base.ClearItems();
            sourceEditting = true;
            source.Clear();
            sourceEditting = false;
        }

        private void source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (sourceEditting) {
                return;
            }
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    AddItemAction(e.NewItems.Cast<T>(), e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveItemAction(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    ReplaceItemAction(e.NewItems.Cast<T>(), e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Move:
                    MoveItemAction(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClearItemAction();
                    break;
            }
        }

        private void AddItemAction(IEnumerable<T> items, int idx) {
            if (idx == source.Count) {
                foreach (var item in items) {
                    dest.Add(mapTo(item));
                }
            }
            else {
                var i = 0;
                foreach (var item in items) {
                    dest.Insert(idx + i, mapTo(item));
                    ++i;
                }
            }
        }

        private void RemoveItemAction(int idx) {
            dest.RemoveAt(idx);
        }

        private void ReplaceItemAction(IEnumerable<T> items, int idx) {
            var i = 0;
            foreach (var item in items) {
                dest[idx + i] = mapTo(item);
                ++i;
            }
        }

        private void MoveItemAction(int oldIdx, int newIdx) {
            var item = dest[oldIdx];

            dest.RemoveAt(oldIdx);
            dest.Insert(newIdx, item);
        }

        private void ClearItemAction() {
            dest.Clear();
        }

        private static ObservableCollection<U> Map(IEnumerable<T> enumerable, Func<T, U> map) {
            return new ObservableCollection<U>(enumerable.Select(map));
        }

        public void Dispose() {
            source.CollectionChanged -= source_CollectionChanged;
            if (typeof(U).GetInterfaces().Contains(typeof(IDisposable))) {
                foreach (IDisposable item in this) {
                    item.Dispose();
                }
            }
        }
    }

    public static class MappedObservableCollectionExtention
    {
        public static MappedObservableCollection<T, U> ToMappedObservableCollection<T, U>(
            this ObservableCollection<T> collection,
            Func<T, U> mapTo,
            Func<U, T> mapFrom)
        {
            return new MappedObservableCollection<T, U>(collection, mapTo, mapFrom);
        } 
    }
}
