using CompMs.CommonMVVM;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class BarItemsLoaderData : BindableBase
    {
        public BarItemsLoaderData(string label, string axisLabel, IBarItemsLoader loader) : this(label, Observable.Return(axisLabel), loader, Observable.Return(true)) {

        }

        public BarItemsLoaderData(string label, IObservable<string> axisLabel, IBarItemsLoader loader, IObservable<bool> isEnabled) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            AxisLabel = axisLabel ?? throw new ArgumentNullException(nameof(axisLabel));
            IsEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
            Loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public string Label { get; }
        public IObservable<string> AxisLabel { get; }
        public IBarItemsLoader Loader { get; }
        public IObservable<bool> IsEnabled { get; }
    }
}
