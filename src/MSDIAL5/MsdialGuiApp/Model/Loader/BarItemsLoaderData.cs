using CompMs.CommonMVVM;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class BarItemsLoaderData : BindableBase
    {
        public BarItemsLoaderData(string label, string axisLabel, IObservable<IBarItemsLoader> observableLoader) : this(label, Observable.Return(axisLabel), observableLoader, Observable.Return(true)) {

        }

        public BarItemsLoaderData(string label, string axisLabel, IObservable<IBarItemsLoader> observableLoader, IObservable<bool> isEnabled) : this(label, Observable.Return(axisLabel), observableLoader, isEnabled) {

        }

        public BarItemsLoaderData(string label, IObservable<string> axisLabel, IObservable<IBarItemsLoader> observableLoader, IObservable<bool> isEnabled) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            AxisLabel = axisLabel ?? throw new ArgumentNullException(nameof(axisLabel));
            ObservableLoader = observableLoader ?? throw new ArgumentNullException(nameof(observableLoader));
            IsEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
        }

        public string Label { get; }
        public IObservable<string> AxisLabel { get; }
        public IObservable<IBarItemsLoader> ObservableLoader { get; }
        public IObservable<bool> IsEnabled { get; }
    }
}
