using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace CompMs.Graphics.Helper
{
    internal static class NotifyCollectionChangedHelper
    {
        public static IDisposable Manage(INotifyCollectionChanged collection, Func<object, object> getter,  NotifyCollectionChangedEventHandler handler) {
            var manager = new HandlerManager(collection, getter, handler);
            manager.Attach();
            return manager;
        }

        private sealed class HandlerManager : IDisposable {
            private NotifyCollectionChangedEventHandler _handler;
            private Func<object, object> _getter;
            INotifyCollectionChanged _collection;

            public HandlerManager(INotifyCollectionChanged collection, Func<object, object> getter, NotifyCollectionChangedEventHandler handler) {
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
                _getter = getter ?? throw new ArgumentNullException(nameof(getter));
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            public void Attach() {
                if (_collection is IList list) {
                    AddHandler(list);
                }
            }

            public void Detach() {
                if (_collection is IList list) {
                    RemoveHandler(list);
                }
            }

            private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
                switch (e.Action) {
                    case NotifyCollectionChangedAction.Add:
                        AddHandler(e.NewItems);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Reset:
                        RemoveHandler(e.OldItems);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        RemoveHandler(e.OldItems);
                        AddHandler(e.NewItems);
                        break;
                }
            }

            private void AddHandler(IList items) {
                foreach (var item in GetItems(items)) {
                    item.CollectionChanged += _handler;
                }
            }

            private void RemoveHandler(IList items) {
                foreach (var item in GetItems(items)) {
                    item.CollectionChanged -= _handler;
                }
            }

            private IEnumerable<INotifyCollectionChanged> GetItems(IList items) {
                return items.OfType<object>().Select(_getter).OfType<INotifyCollectionChanged>();
            }

            public void Dispose() {
                Detach();
                _collection = null;
                _getter = null;
                _handler = null;
            }
        }
    }
}
