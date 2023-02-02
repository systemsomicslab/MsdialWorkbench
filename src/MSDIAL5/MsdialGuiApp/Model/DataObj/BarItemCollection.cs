using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class BarItemCollection : BindableBase
    {
        public BarItemCollection() {
            ObservableItems = Observable.Return(new List<BarItem>(0));
            ObservableLoading = Observable.Return(false);
        }

        public BarItemCollection(IObservable<List<BarItem>> observableItems, IObservable<bool> observableLoading) {
            ObservableItems = observableItems;
            ObservableLoading = observableLoading;
        }

        public IObservable<List<BarItem>> ObservableItems { get; }
        public IObservable<bool> ObservableLoading { get; }
    }
}
