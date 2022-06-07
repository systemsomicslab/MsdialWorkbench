using CompMs.CommonMVVM;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    internal class BarItemsLoaderData : BindableBase
    {
        public BarItemsLoaderData(string label, IObservable<IBarItemsLoader> observableLoader) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            ObservableLoader = observableLoader ?? throw new ArgumentNullException(nameof(observableLoader));
            IsEnabled = Observable.Return(true);
        }

        public BarItemsLoaderData(string label, IObservable<IBarItemsLoader> observableLoader, IObservable<bool> isEnabled) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            ObservableLoader = observableLoader ?? throw new ArgumentNullException(nameof(observableLoader));
            IsEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
        }

        public string Label { get; }

        public IObservable<IBarItemsLoader> ObservableLoader { get; }
        public IObservable<bool> IsEnabled { get; }
    }
}
