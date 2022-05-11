using CompMs.App.Msdial.Model.Loader;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class BarItemCollection : ReadOnlyObservableCollection<BarItem>, IDisposable
    {
        private BarItemCollection(AlignmentSpotPropertyModel spot, IObservable<IBarItemsLoader> loader, ReactiveCollection<BarItem> collection) : base(collection) {
            unsubscriber = loader
                .Where(loader_ => !(loader_ is null))
                .Select(loader_ => loader_.LoadBarItemsAsObservable(spot))
                .Switch()
                .Subscribe(items => {
                    collection.ClearOnScheduler();
                    collection.AddRangeOnScheduler(items);
                });
            this.collection = collection;
        }

        private IDisposable unsubscriber;
        private ReactiveCollection<BarItem> collection;

        public void Dispose() {
            unsubscriber.Dispose();
            unsubscriber = null;
            collection.Dispose();
            collection = null;
        }

        public static BarItemCollection Create(AlignmentSpotPropertyModel spot, IObservable<IBarItemsLoader> loader) {
            var collection = new ReactiveCollection<BarItem>();
            return new BarItemCollection(spot, loader, collection);
        }
    }
}
