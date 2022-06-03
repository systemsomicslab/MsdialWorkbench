using CompMs.CommonMVVM;
using System;

namespace CompMs.App.Msdial.Model.Loader
{
    internal class BarItemsLoaderData : BindableBase
    {
        public BarItemsLoaderData(string label, IObservable<IBarItemsLoader> observableLoader) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            ObservableLoader = observableLoader ?? throw new ArgumentNullException(nameof(observableLoader));
        }

        public string Label { get; }

        public IObservable<IBarItemsLoader> ObservableLoader { get; }
    }
}
