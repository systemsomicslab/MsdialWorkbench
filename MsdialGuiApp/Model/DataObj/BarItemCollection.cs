using CompMs.App.Msdial.Model.Loader;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class BarItemCollection : ReadOnlyObservableCollection<BarItem>, IDisposable
    {
        public BarItemCollection(AlignmentSpotPropertyModel spot, IObservable<IBarItemsLoader> loader) : base(new ObservableCollection<BarItem>()) {
            unsubscriber = loader
                .Where(loader_ => !(loader_ is null))
                .Select(loader_ => loader_.LoadBarItemsAsObservable(spot))
                .Switch()
                .Subscribe(items => {
                    Items.Clear();
                    items.ForEach(Items.Add);
                });
        }

        private IDisposable unsubscriber;

        public void Dispose() {
            unsubscriber?.Dispose();
            unsubscriber = null;
        }
    }
}
