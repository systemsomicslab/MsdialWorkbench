using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace CompMs.CommonMVVM.Common
{
    public class MappedReadOnlyObservableCollection<T, U> : ReadOnlyObservableCollection<U>, IDisposable
    {
        public MappedReadOnlyObservableCollection(ObservableCollection<T> collection, Func<T, U> map) :base(Map(collection, map)) {
            this.map = map;
            this.source = collection;
            this.dest = (ObservableCollection<U>)Items;

            this.source.CollectionChanged += source_CollectionChanged;
        }

        private readonly ObservableCollection<T> source;
        private readonly ObservableCollection<U> dest;
        private readonly Func<T, U> map;

        private void source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
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
                    dest.Add(map(item));
                }
            }
            else {
                var i = 0;
                foreach (var item in items) {
                    dest.Insert(idx + i, map(item));
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
                dest[idx + i] = map(item);
                ++i;
            }
        }

        private void MoveItemAction(int oldIdx, int newIdx) {
            dest.Move(oldIdx, newIdx);
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

    public static class MappedReadOnlyObservableCollectionExtention
    {
        public static MappedReadOnlyObservableCollection<T, U> ToMappedReadOnlyObservableCollection<T, U>(this ObservableCollection<T> collection, Func<T, U> map) {
            return new MappedReadOnlyObservableCollection<T, U>(collection, map);
        } 
    }
}
