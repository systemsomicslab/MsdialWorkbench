using CompMs.App.Msdial.Model.Loader;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class SpotBarItemCollection : ReadOnlyObservableCollection<BarItem>, IDisposable
    {
        private SpotBarItemCollection(AlignmentSpotPropertyModel spot, IObservable<IBarItemsLoader> loader, ReactiveCollection<BarItem> collection) : base(collection) {
            _unsubscriber = loader
                .Where(loader_ => !(loader_ is null))
                .Select(loader_ => loader_.LoadBarItemsAsObservable(spot))
                .Switch()
                .Subscribe(items => {
                    collection.ClearOnScheduler();
                    collection.AddRangeOnScheduler(items);
                });
            _collection = collection;
        }

        private IDisposable _unsubscriber;
        private ReactiveCollection<BarItem> _collection;

        public void Dispose() {
            _unsubscriber.Dispose();
            _unsubscriber = null;
            _collection.Dispose();
            _collection = null;
        }

        public static SpotBarItemCollection Create(AlignmentSpotPropertyModel spot, IObservable<IBarItemsLoader> loader) {
            var collection = new ReactiveCollection<BarItem>();
            return new SpotBarItemCollection(spot, loader, collection);
        }
    }
}
