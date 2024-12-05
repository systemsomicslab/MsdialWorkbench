using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class SpotBarItemCollection : ReadOnlyObservableCollection<BarItem>, IDisposable
    {
        private IDisposable? _unsubscriber;
        private ReactiveCollection<BarItem>? _collection;

        private SpotBarItemCollection(AlignmentSpotPropertyModel spot, IObservable<IBarItemsLoader> loader, ReactiveCollection<BarItem> collection) : base(collection) {
            var collectionsAsObservable = loader
                .Where(loader_ => !(loader_ is null))
                .Select(loader_ => loader_.LoadBarItemsAsObservable(spot));
            _unsubscriber = collectionsAsObservable
                .SelectSwitch(collections => collections.ObservableItems)
                .Subscribe(items => {
                    collection.ClearOnScheduler();
                    collection.AddRangeOnScheduler(items);
                });
            IsLoading = collectionsAsObservable
                .SelectSwitch(collections => collections.ObservableLoading)
                .ToReadOnlyReactivePropertySlim();
            _collection = collection;
        }

        public ReadOnlyReactivePropertySlim<bool> IsLoading { get; }

        public void Dispose() {
            _unsubscriber?.Dispose();
            _unsubscriber = null;
            _collection?.Dispose();
            _collection = null;
            IsLoading.Dispose();
        }

        public static SpotBarItemCollection Create(AlignmentSpotPropertyModel spot, IObservable<IBarItemsLoader> loader) {
            var collection = new ReactiveCollection<BarItem>();
            return new SpotBarItemCollection(spot, loader, collection);
        }
    }
}
